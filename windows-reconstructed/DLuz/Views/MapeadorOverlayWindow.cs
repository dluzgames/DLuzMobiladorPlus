using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using DLuz.Helpers;
using DLuz.Mapper;
using DLuz.Services;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace DLuz.Views;

public partial class MapeadorOverlayWindow : Window, IComponentConnector
{
	private enum Tipo
	{
		Joystick,
		Camara,
		Boton
	}

	private sealed class Item
	{
		public Tipo Tipo;

		public string Key = "";

		public string Label = "";

		public string Modo = "tap";

		public int IntervaloMs = 150;

		public int DevX;

		public int DevY;

		public int Radio = 105;

		public double Tamano = 36.0;

		public Border El;
	}

	private struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	private struct POINT
	{
		public int X;

		public int Y;
	}

	private readonly KeymapConfig _keymap;

	private readonly MapeadorService _servicio;

	private readonly int _devW;

	private readonly int _devH;

	private readonly Func<nint> _hwndProvider;

	private readonly Action _onGuardar;

	private readonly DispatcherTimer _track;

	private readonly ContentDialogService _dialogos = new ContentDialogService();

	private readonly List<Item> _items = new List<Item>();

	private Ellipse? _anillo;

	private nint _overlayHwnd;

	private Item? _sel;

	private Item? _arrastrando;

	private Point _grab;

	private int _fallos;

	private bool _capturandoTecla;

	private bool _capturandoLibre;

	private bool _capturandoFree;

	private bool _suprimir;

	private bool _huboArrastre;

	private readonly Dictionary<string, int[]> _joyKeys;

	private string? _capturaJoyDir;

	private static readonly (string Dir, string Flecha, int Dx, int Dy)[] DirsJoy = new(string, string, int, int)[4]
	{
		("arriba", "↑", 0, -1),
		("abajo", "↓", 0, 1),
		("izquierda", "←", -1, 0),
		("derecha", "→", 1, 0)
	};

	private static readonly nint HWND_TOPMOST = new IntPtr(-1);

	private static readonly nint HWND_NOTOPMOST = new IntPtr(-2);

	private const uint SWP_NOACTIVATE = 16u;

	private const uint SWP_NOMOVE = 2u;

	private const uint SWP_NOSIZE = 1u;

	public MapeadorOverlayWindow(KeymapConfig keymap, MapeadorService servicio, int devW, int devH, Func<nint> hwndProvider, Action onGuardar)
	{
		InitializeComponent();
		_keymap = keymap;
		_servicio = servicio;
		_devW = ((devW > 0) ? devW : 1080);
		_devH = ((devH > 0) ? devH : 2400);
		_hwndProvider = hwndProvider;
		_onGuardar = onGuardar;
		_joyKeys = new Dictionary<string, int[]>(keymap.Joystick.Keys);
		_dialogos.SetDialogHost(HostDialogos);
		BtnJoyArriba.Click += delegate
		{
			IniciarCapturaJoy("arriba");
		};
		BtnJoyAbajo.Click += delegate
		{
			IniciarCapturaJoy("abajo");
		};
		BtnJoyIzquierda.Click += delegate
		{
			IniciarCapturaJoy("izquierda");
		};
		BtnJoyDerecha.Click += delegate
		{
			IniciarCapturaJoy("derecha");
		};
		BtnEliminar.Click += delegate
		{
			EliminarSeleccion();
		};
		BtnTeclaFree.Click += delegate
		{
			IniciarCapturaFree();
		};
		BtnCapturar.Click += delegate
		{
			IniciarCapturaTeclaBoton();
		};
		BtnClicIzq.Click += delegate
		{
			AsignarTecla("mouse_left");
		};
		BtnClicDer.Click += delegate
		{
			AsignarTecla("mouse_right");
		};
		BtnClicMedio.Click += delegate
		{
			AsignarTecla("mouse_middle");
		};
		BtnLateral1.Click += delegate
		{
			AsignarTecla("mouse_x1");
		};
		BtnLateral2.Click += delegate
		{
			AsignarTecla("mouse_x2");
		};
		BtnRodaCima.Click += delegate
		{
			AsignarTecla("mouse_wheel_up");
		};
		BtnRodaBaixo.Click += delegate
		{
			AsignarTecla("mouse_wheel_down");
		};
		BtnCerrarProps.Click += delegate
		{
			CerrarPropiedades();
		};
		PropEtiqueta.TextChanged += delegate
		{
			if (!_suprimir && _sel != null)
			{
				_sel.Label = PropEtiqueta.Text;
				ActualizarTexto(_sel);
			}
		};
		PropModo.SelectionChanged += delegate
		{
			if (!_suprimir && _sel != null)
			{
				Item sel = _sel;
				sel.Modo = PropModo.SelectedIndex switch
				{
					0 => "hold", 
					2 => "repeat", 
					_ => "tap", 
				};
				FilaIntervalo.Visibility = ((!(_sel.Modo == "repeat")) ? Visibility.Collapsed : Visibility.Visible);
			}
		};
		PropIntervalo.ValueChanged += delegate
		{
			if (!_suprimir && _sel != null)
			{
				_sel.IntervaloMs = (int)PropIntervalo.Value;
				TxtIntervalo.Text = $"{_sel.IntervaloMs}";
			}
		};
		PropRadio.ValueChanged += delegate
		{
			if (!_suprimir && _sel != null)
			{
				_sel.Radio = (int)PropRadio.Value;
				ReposicionarUno(_sel);
			}
		};
		PropTamano.ValueChanged += delegate
		{
			if (!_suprimir && _sel != null)
			{
				_sel.Tamano = PropTamano.Value;
				AplicarTamano(_sel);
				ReposicionarUno(_sel);
			}
		};
		NumMouseSensX.ValueChanged += delegate
		{
			AplicarSensMouse();
		};
		NumMouseSensY.ValueChanged += delegate
		{
			AplicarSensMouse();
		};
		BtnTeclaLibre.Click += delegate
		{
			IniciarCapturaLibre();
		};
		MarkerCanvas.MouseLeftButtonDown += Canvas_Down;
		MarkerCanvas.MouseLeftButtonUp += delegate(object _, MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2)
			{
				AgregarTapEn(e.GetPosition(MarkerCanvas));
			}
		};
		base.PreviewKeyDown += OnKey;
		base.PreviewMouseDown += OnBotonMouse;
		ConstruirDesdeKeymap();
		MarkerCanvas.SizeChanged += delegate
		{
			Reposicionar();
		};
		_track = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(120.0)
		};
		_track.Tick += delegate
		{
			SeguirEspejo();
		};
		base.SourceInitialized += delegate
		{
			_overlayHwnd = new WindowInteropHelper(this).Handle;
		};
		base.Loaded += delegate
		{
			SeguirEspejo();
			_track.Start();
		};
		base.Closed += delegate
		{
			_track.Stop();
		};
	}

	public Task GuardarCerrarAsync()
	{
		return GuardarAsync(cerrar: true);
	}

	public Task GuardarSinCerrarAsync()
	{
		return GuardarAsync(cerrar: false);
	}

	public void Cancelar()
	{
		Close();
	}

	public void AgregarBotonNuevo()
	{
		AgregarBoton("hold");
	}

	public void AgregarTapNuevo()
	{
		AgregarBoton("tap");
	}

	public void AgregarRepetidoNuevo()
	{
		AgregarBoton("repeat");
	}

	public void AgregarAtirarMouseLeft()
	{
		var existente = _items.FirstOrDefault(i => i.Tipo == Tipo.Boton && (i.Key == "mouse_left" || i.Label?.ToLowerInvariant() == "atirar"));
		if (existente != null)
		{
			existente.DevX = (int)(_devW * 0.85);
			existente.DevY = (int)(_devH * 0.65);
			ReposicionarUno(existente);
			Seleccionar(existente);
			AbrirPropiedades(existente);
			return;
		}
		Item item = new Item
		{
			Tipo = Tipo.Boton,
			Key = "mouse_left",
			Label = "Atirar",
			Modo = "hold",
			DevX = (int)(_devW * 0.85),
			DevY = (int)(_devH * 0.65),
			Tamano = 38.0
		};
		Crear(item);
		ReposicionarUno(item);
		AbrirPropiedades(item);
	}

	public void AgregarMirarMouseRight()
	{
		var existente = _items.FirstOrDefault(i => i.Tipo == Tipo.Boton && (i.Key == "mouse_right" || i.Label?.ToLowerInvariant() == "mira"));
		if (existente != null)
		{
			existente.DevX = (int)(_devW * 0.76);
			existente.DevY = (int)(_devH * 0.80);
			ReposicionarUno(existente);
			Seleccionar(existente);
			AbrirPropiedades(existente);
			return;
		}
		Item item = new Item
		{
			Tipo = Tipo.Boton,
			Key = "mouse_right",
			Label = "Mira",
			Modo = "hold",
			DevX = (int)(_devW * 0.76),
			DevY = (int)(_devH * 0.80),
			Tamano = 38.0
		};
		Crear(item);
		ReposicionarUno(item);
		AbrirPropiedades(item);
	}

	public void ReposicionarJoystickWASD()
	{
		var joy = _items.FirstOrDefault(i => i.Tipo == Tipo.Joystick);
		if (joy != null)
		{
			joy.DevX = (int)(_devW * 0.18);
			joy.DevY = (int)(_devH * 0.75);
			joy.Radio = (int)(_devW * 0.08);
			ReposicionarUno(joy);
			Seleccionar(joy);
			AbrirPropiedades(joy);
		}
	}

	public void ReposicionarMouseCamera()
	{
		var cam = _items.FirstOrDefault(i => i.Tipo == Tipo.Camara);
		if (cam != null)
		{
			cam.DevX = (int)(_devW * 0.58);
			cam.DevY = (int)(_devH * 0.50);
			ReposicionarUno(cam);
			Seleccionar(cam);
			AbrirPropiedades(cam);
		}
	}

	public void AplicarControlesPreConfiguradosPadrao()
	{
		foreach (var it in _items.Where(i => i.Tipo == Tipo.Boton).ToList())
		{
			MarkerCanvas.Children.Remove(it.El);
			_items.Remove(it);
		}

		ReposicionarJoystickWASD();
		ReposicionarMouseCamera();

		var padroes = new (string key, string label, string modo, double px, double py)[]
		{
			("mouse_left", "Atirar", "hold", 0.85, 0.65),
			("mouse_right", "Mira", "hold", 0.76, 0.80),
			("space", "Pular", "tap", 0.80, 0.90),
			("c", "Agachar", "tap", 0.70, 0.90),
			("r", "Recarregar", "tap", 0.85, 0.78),
			("f", "Interagir", "tap", 0.78, 0.65),
			("m", "Mapa", "tap", 0.88, 0.10),
			("1", "Arma 1", "tap", 0.75, 0.20),
			("2", "Arma 2", "tap", 0.82, 0.20),
			("g", "Granada", "tap", 0.68, 0.20),
			("tab", "Mochila", "tap", 0.88, 0.20)
		};

		foreach (var p in padroes)
		{
			Item item = new Item
			{
				Tipo = Tipo.Boton,
				Key = p.key,
				Label = p.label,
				Modo = p.modo,
				DevX = (int)(_devW * p.px),
				DevY = (int)(_devH * p.py),
				Tamano = 36.0,
				IntervaloMs = 150
			};
			Crear(item);
			ReposicionarUno(item);
		}

		Reposicionar();
	}

	private void ConstruirDesdeKeymap()
	{
		_anillo = new Ellipse
		{
			Stroke = (Brush)FindResource("Stex.AccentLightBrush"),
			StrokeThickness = 2.0,
			StrokeDashArray = new DoubleCollection { 4.0, 3.0 },
			IsHitTestVisible = false
		};
		MarkerCanvas.Children.Add(_anillo);
		Crear(new Item
		{
			Tipo = Tipo.Joystick,
			Label = "Joystick",
			DevX = (_keymap.Joystick.CenterX > 0) ? _keymap.Joystick.CenterX : (int)(_devW * 0.18),
			DevY = (_keymap.Joystick.CenterY > 0) ? _keymap.Joystick.CenterY : (int)(_devH * 0.75),
			Radio = (_keymap.Joystick.Radius > 0) ? _keymap.Joystick.Radius : (int)(_devW * 0.08),
			Tamano = 40.0
		});
		Crear(new Item
		{
			Tipo = Tipo.Camara,
			Label = "Mouse",
			DevX = (_keymap.Camera.ZoneX > 0) ? _keymap.Camera.ZoneX : (int)(_devW * 0.58),
			DevY = (_keymap.Camera.ZoneY > 0) ? _keymap.Camera.ZoneY : (int)(_devH * 0.50)
		});

		if (_keymap.Buttons == null || _keymap.Buttons.Count == 0)
		{
			AplicarControlesPreConfiguradosPadrao();
			return;
		}

		foreach (ButtonConfig button in _keymap.Buttons)
		{
			Crear(new Item
			{
				Tipo = Tipo.Boton,
				Key = button.Key,
				Label = (string.IsNullOrWhiteSpace(button.Label) ? button.Key : button.Label),
				DevX = button.X,
				DevY = button.Y,
				Modo = (button.Mode ?? (button.Key.StartsWith("mouse_") ? "hold" : "tap")),
				IntervaloMs = ((button.RepeatMs > 0) ? button.RepeatMs : 150),
				Tamano = ((button.Size > 0.0) ? button.Size : 36.0)
			});
		}
	}

	private void Crear(Item m)
	{
		double num = Math.Clamp((m.Tamano > 0.0) ? m.Tamano : 36.0, 28.0, 44.0);
		string resourceKey = ((m.Tipo == Tipo.Camara) ? "Stex.OverlayMarkerCameraBrush" : "Stex.OverlayMarkerBrush");
		Border border = new Border
		{
			Width = num,
			Height = num,
			CornerRadius = new CornerRadius(num / 2.0),
			Background = (Brush)FindResource(resourceKey),
			BorderBrush = (Brush)FindResource("Stex.OverlayMarkerBorderBrush"),
			BorderThickness = new Thickness(1.5),
			Cursor = Cursors.SizeAll,
			Child = new System.Windows.Controls.TextBlock
			{
				FontSize = 10.0,
				FontWeight = FontWeights.SemiBold,
				TextWrapping = TextWrapping.Wrap,
				TextAlignment = TextAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Foreground = (Brush)FindResource("Stex.OverlayMarkerTextBrush")
			}
		};
		m.El = border;
		border.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2)
			{
				AbrirPropiedades(m);
				e.Handled = true;
			}
			else
			{
				Seleccionar(m);
				IniciarArrastre(m, e);
				e.Handled = true;
			}
		};
		border.MouseRightButtonDown += delegate(object s, MouseButtonEventArgs e)
		{
			AbrirPropiedades(m);
			e.Handled = true;
		};
		border.MouseMove += delegate(object s, MouseEventArgs e)
		{
			MoverArrastre(m, e);
		};
		border.MouseLeftButtonUp += delegate(object s, MouseButtonEventArgs e)
		{
			if (_arrastrando == m)
			{
				m.El.ReleaseMouseCapture();
				_arrastrando = null;
				if (m.Tipo == Tipo.Camara && !_huboArrastre)
				{
					IniciarCapturaLibre();
					e.Handled = true;
				}
			}
		};
		MarkerCanvas.Children.Add(border);
		_items.Add(m);
		ActualizarTexto(m);
	}

	private void ActualizarTexto(Item m)
	{
		string text = m.Tipo switch
		{
			Tipo.Boton => string.IsNullOrEmpty(m.Key) ? "?" : Bonito(m.Key), 
			Tipo.Joystick => TeclasJoystick(), 
			_ => _capturandoLibre ? "…" : Bonito(_servicio.TeclaCaptura), 
		};
		((System.Windows.Controls.TextBlock)m.El.Child).Text = text;
	}

	private string TeclasJoystick()
	{
		string[] array = new string[4] { "arriba", "abajo", "izquierda", "derecha" };
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, int[]> key in _keymap.Joystick.Keys)
		{
			int[] value = key.Value;
			if (value != null && value.Length >= 2)
			{
				string value2 = ((value[1] < 0) ? "arriba" : ((value[1] > 0) ? "abajo" : ((value[0] < 0) ? "izquierda" : "derecha")));
				list.Add($"{Array.IndexOf(array, value2)}{key.Key}");
			}
		}
		list.Sort(StringComparer.Ordinal);
		if (list.Count <= 0)
		{
			return "Joystick";
		}
		return string.Concat(list.Select((string t) => t.Substring(1)));
	}

	private void AplicarTamano(Item m)
	{
		double num = Math.Clamp(m.Tamano, 28.0, 44.0);
		m.El.Width = num;
		m.El.Height = num;
		m.El.CornerRadius = new CornerRadius(num / 2.0);
	}

	private void AgregarBoton(string modo)
	{
		Item item = new Item();
		item.Tipo = Tipo.Boton;
		Item item2 = item;
		string label = ((modo == "hold") ? "Botón" : ((!(modo == "repeat")) ? "Tap" : "Repetido"));
		item2.Label = label;
		item.Modo = modo;
		item.DevX = _devW / 2;
		item.DevY = _devH / 2;
		Item m = item;
		Crear(m);
		ReposicionarUno(m);
		AbrirPropiedades(m);
	}

	private void AgregarTapEn(Point p)
	{
		var (num, num2, num3) = Contenido();
		if (!(num3 <= 0.0))
		{
			Item item = new Item
			{
				Tipo = Tipo.Boton,
				Label = "Tap",
				Modo = "tap"
			};
			item.DevX = Math.Clamp((int)Math.Round((p.X - num) / num3), 0, _devW - 1);
			item.DevY = Math.Clamp((int)Math.Round((p.Y - num2) / num3), 0, _devH - 1);
			Crear(item);
			ReposicionarUno(item);
			AbrirPropiedades(item);
		}
	}

	private void EliminarSeleccion()
	{
		if (_sel != null && _sel.Tipo == Tipo.Boton)
		{
			MarkerCanvas.Children.Remove(_sel.El);
			_items.Remove(_sel);
			_sel = null;
			PanelProps.Visibility = Visibility.Collapsed;
		}
	}

	private void Seleccionar(Item? m)
	{
		_sel = m;
		foreach (Item item in _items)
		{
			item.El.BorderBrush = (Brush)FindResource((item == m) ? "Stex.AccentLightBrush" : "Stex.TextPrimaryBrush");
		}
		if (m == null)
		{
			CerrarPropiedades();
		}
	}

	private void CerrarPropiedades()
	{
		_capturandoTecla = false;
		PanelProps.Visibility = Visibility.Collapsed;
	}

	private void AbrirPropiedades(Item m)
	{
		Seleccionar(m);
		_suprimir = true;
		System.Windows.Controls.TextBlock propTitulo = PropTitulo;
		propTitulo.Text = m.Tipo switch
		{
			Tipo.Joystick => "Joystick", 
			Tipo.Camara => "Mouse", 
			_ => "Botón", 
		};
		FilaEtiqueta.Visibility = ((m.Tipo != Tipo.Boton) ? Visibility.Collapsed : Visibility.Visible);
		FilaTecla.Visibility = ((m.Tipo != Tipo.Boton) ? Visibility.Collapsed : Visibility.Visible);
		FilaModo.Visibility = ((m.Tipo != Tipo.Boton) ? Visibility.Collapsed : Visibility.Visible);
		FilaIntervalo.Visibility = ((m.Tipo != Tipo.Boton || !(m.Modo == "repeat")) ? Visibility.Collapsed : Visibility.Visible);
		BtnEliminar.Visibility = ((m.Tipo != Tipo.Boton) ? Visibility.Collapsed : Visibility.Visible);
		FilaRadio.Visibility = ((m.Tipo != Tipo.Joystick) ? Visibility.Collapsed : Visibility.Visible);
		FilaJoyTeclas.Visibility = ((m.Tipo != Tipo.Joystick) ? Visibility.Collapsed : Visibility.Visible);
		FilaSens.Visibility = ((m.Tipo != Tipo.Camara) ? Visibility.Collapsed : Visibility.Visible);
		FilaTamano.Visibility = Visibility.Visible;
		_capturaJoyDir = null;
		_capturandoLibre = false;
		_capturandoFree = false;
		TxtJoyCaptura.Text = "";
		TxtMouseMsg.Text = "";
		if (m.Tipo == Tipo.Joystick)
		{
			RefrescarJoyBotones();
		}
		if (m.Tipo == Tipo.Camara)
		{
			NumMouseSensX.Value = _servicio.SensibilidadCamaraX;
			NumMouseSensY.Value = _servicio.SensibilidadCamaraY;
			TxtTeclaLibre.Text = Bonito(_servicio.TeclaCaptura);
			TxtTeclaFree.Text = Bonito(_servicio.TeclaLibre);
		}
		PropEtiqueta.Text = m.Label;
		PropTecla.Text = (string.IsNullOrEmpty(m.Key) ? "—" : Bonito(m.Key));
		ComboBox propModo = PropModo;
		string modo = m.Modo;
		int selectedIndex = ((!(modo == "hold")) ? ((!(modo == "repeat")) ? 1 : 2) : 0);
		propModo.SelectedIndex = selectedIndex;
		PropIntervalo.Value = Math.Clamp(m.IntervaloMs, 60, 1000);
		TxtIntervalo.Text = $"{(int)PropIntervalo.Value}";
		PropRadio.Value = Math.Clamp(m.Radio, 40, 320);
		PropTamano.Value = Math.Clamp(m.Tamano, 28.0, 44.0);
		_capturandoTecla = false;
		_suprimir = false;
		PanelProps.Visibility = Visibility.Visible;
	}

	private void AplicarSensMouse()
	{
		if (!_suprimir)
		{
			double sensibilidadX = NumMouseSensX.Value ?? _servicio.SensibilidadCamaraX;
			double sensibilidadY = NumMouseSensY.Value ?? _servicio.SensibilidadCamaraY;
			_servicio.EstablecerCamara(sensibilidadX, sensibilidadY);
		}
	}

	private void IniciarCapturaLibre()
	{
		_capturandoLibre = true;
		_capturandoFree = false;
		_capturandoTecla = false;
		_capturaJoyDir = null;
		TxtMouseMsg.Text = "Pressione a tecla ou botão do mouse para pausar a captura (Esc cancela).";
		RefrescarCirculoMouse();
		Activate();
		Focus();
	}

	private void IniciarCapturaFree()
	{
		_capturandoFree = true;
		_capturandoLibre = false;
		_capturandoTecla = false;
		_capturaJoyDir = null;
		TxtMouseMsg.Text = "Pressione a tecla que liberará o cursor enquanto estiver pressionada (Esc cancela).";
		Activate();
		Focus();
	}

	private static bool VetadaPorMod(string tecla)
	{
		return TeclaMod.BloqueadaEnMapeador(SessionState.Instance.ShortcutMod, tecla);
	}

	private static string MotivoVetoMod(string tecla)
	{
		return "\"" + Bonito(tecla) + "\" está reservada como tecla de atalhos (MOD) do espelho. Escolha outra ou altere a tecla MOD em Opções extras.";
	}

	private void AsignarTeclaLibre(string tecla)
	{
		_capturandoLibre = false;
		if (VetadaPorMod(tecla))
		{
			TxtMouseMsg.Text = MotivoVetoMod(tecla);
			RefrescarCirculoMouse();
			return;
		}
		string text = _servicio.EstablecerTeclaCaptura(tecla);
		TxtTeclaLibre.Text = Bonito(_servicio.TeclaCaptura);
		TxtMouseMsg.Text = text ?? "";
		RefrescarCirculoMouse();
	}

	private void AsignarTeclaFree(string tecla)
	{
		_capturandoFree = false;
		if (VetadaPorMod(tecla))
		{
			TxtMouseMsg.Text = MotivoVetoMod(tecla);
			return;
		}
		string text = _servicio.EstablecerTeclaLibre(tecla);
		TxtTeclaFree.Text = Bonito(_servicio.TeclaLibre);
		TxtMouseMsg.Text = text ?? "";
	}

	private void RefrescarCirculoMouse()
	{
		Item item = _items.FirstOrDefault((Item i) => i.Tipo == Tipo.Camara);
		if (item != null)
		{
			ActualizarTexto(item);
		}
	}

	private void IniciarCapturaTeclaBoton()
	{
		if (_sel == null || _sel.Tipo != Tipo.Boton)
		{
			return;
		}
		_capturandoTecla = true;
		_capturaJoyDir = null;
		_capturandoLibre = false;
		_capturandoFree = false;

		BtnCapturar.Content = "⏳ Pressionando...";
		BtnCapturar.Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0));
		PropTecla.Text = "⏳ Digite a tecla...";
		PropTecla.Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0));
		if (BorderTecla != null)
		{
			BorderTecla.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 215, 0));
		}
		if (TxtAvisoTecla != null)
		{
			TxtAvisoTecla.Visibility = Visibility.Visible;
		}

		_sel.El.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 215, 0));
		((System.Windows.Controls.TextBlock)_sel.El.Child).Text = "⏳";

		Activate();
		Focus();
	}

	private void AsignarTecla(string nombre)
	{
		if (_sel != null && _sel.Tipo == Tipo.Boton)
		{
			if (VetadaPorMod(nombre))
			{
				PropTecla.Text = "\"" + Bonito(nombre) + "\" é a tecla MOD do espelho — pressione outra";
				PropTecla.Foreground = Brushes.OrangeRed;
				return;
			}
			_sel.Key = nombre;
			PropTecla.Text = Bonito(nombre);
			PropTecla.Foreground = (Brush)FindResource("Stex.TextPrimaryBrush");
			BtnCapturar.Content = "Atribuir";
			BtnCapturar.Foreground = (Brush)FindResource("Stex.TextPrimaryBrush");
			if (BorderTecla != null)
			{
				BorderTecla.BorderBrush = (Brush)FindResource("Stex.BorderSecondaryBrush");
			}
			if (TxtAvisoTecla != null)
			{
				TxtAvisoTecla.Visibility = Visibility.Collapsed;
			}
			_sel.El.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 230, 118));
			ActualizarTexto(_sel);
			_capturandoTecla = false;
		}
	}

	private void IniciarCapturaJoy(string dir)
	{
		_capturaJoyDir = dir;
		_capturandoTecla = false;
		TxtJoyCaptura.Text = "Pressione a nova tecla (Esc cancela).";
		RefrescarJoyBotones();
		Activate();
		Focus();
	}

	private string? TeclaDeDir(int dx, int dy)
	{
		foreach (KeyValuePair<string, int[]> joyKey in _joyKeys)
		{
			int[] value = joyKey.Value;
			if (value != null && value.Length >= 2 && Math.Sign(value[0]) == dx && Math.Sign(value[1]) == dy)
			{
				return joyKey.Key;
			}
		}
		return null;
	}

	private void RefrescarJoyBotones()
	{
		Wpf.Ui.Controls.Button[] array = new Wpf.Ui.Controls.Button[4] { BtnJoyArriba, BtnJoyAbajo, BtnJoyIzquierda, BtnJoyDerecha };
		for (int i = 0; i < DirsJoy.Length; i++)
		{
			(string, string, int, int) tuple = DirsJoy[i];
			object obj;
			if (!(_capturaJoyDir == tuple.Item1))
			{
				string text = TeclaDeDir(tuple.Item3, tuple.Item4);
				obj = ((text != null) ? Bonito(text) : "—");
			}
			else
			{
				obj = "…";
			}
			string text2 = (string)obj;
			array[i].Content = tuple.Item2 + "  " + text2;
		}
	}

	private void AsignarTeclaJoy(string dir, string tecla)
	{
		(string, string, int, int) tuple = Array.Find(DirsJoy, ((string Dir, string Flecha, int Dx, int Dy) x) => x.Dir == dir);
		if (new string[3]
		{
			_keymap.ToggleKey,
			_keymap.ExitKey,
			_keymap.Camera.FreeMouseKey
		}.Any((string r) => KeyNames.MismaTecla(r, tecla)))
		{
			TxtJoyCaptura.Text = "\"" + Bonito(tecla) + "\" está reservada para controlar o Mapeador.";
			_capturaJoyDir = null;
			RefrescarJoyBotones();
			return;
		}
		if (_items.Any((Item i) => i.Tipo == Tipo.Boton && KeyNames.MismaTecla(i.Key, tecla)))
		{
			TxtJoyCaptura.Text = "\"" + Bonito(tecla) + "\" já está sendo usada por um botão do jogo.";
			_capturaJoyDir = null;
			RefrescarJoyBotones();
			return;
		}
		if (VetadaPorMod(tecla))
		{
			TxtJoyCaptura.Text = MotivoVetoMod(tecla);
			_capturaJoyDir = null;
			RefrescarJoyBotones();
			return;
		}
		string text = TeclaDeDir(tuple.Item3, tuple.Item4);
		if (text != null)
		{
			_joyKeys.Remove(text);
		}
		_joyKeys.Remove(tecla);
		_joyKeys[tecla] = new int[2] { tuple.Item3, tuple.Item4 };
		_capturaJoyDir = null;
		TxtJoyCaptura.Text = "";
		RefrescarJoyBotones();
	}

	private void OnKey(object sender, KeyEventArgs e)
	{
		if (_capturandoLibre)
		{
			if (e.Key == Key.Escape)
			{
				_capturandoLibre = false;
				TxtMouseMsg.Text = "";
				RefrescarCirculoMouse();
				e.Handled = true;
				return;
			}
			string text = NombreDeKey((e.Key == Key.System) ? e.SystemKey : e.Key);
			if (text != null)
			{
				AsignarTeclaLibre(text);
			}
			e.Handled = true;
			return;
		}
		if (_capturaJoyDir != null)
		{
			if (e.Key == Key.Escape)
			{
				_capturaJoyDir = null;
				TxtJoyCaptura.Text = "";
				RefrescarJoyBotones();
				e.Handled = true;
				return;
			}
			string text2 = NombreDeKey((e.Key == Key.System) ? e.SystemKey : e.Key);
			if (text2 != null)
			{
				AsignarTeclaJoy(_capturaJoyDir, text2);
			}
			e.Handled = true;
			return;
		}
		if (_capturandoFree)
		{
			if (e.Key == Key.Escape)
			{
				_capturandoFree = false;
				TxtMouseMsg.Text = "";
				e.Handled = true;
				return;
			}
			string text3 = NombreDeKey((e.Key == Key.System) ? e.SystemKey : e.Key);
			if (text3 != null)
			{
				AsignarTeclaFree(text3);
			}
			e.Handled = true;
			return;
		}
		if (_capturandoTecla)
		{
			Item? sel = _sel;
			if (sel != null && sel.Tipo == Tipo.Boton)
			{
				if (e.Key == Key.Escape)
				{
					_capturandoTecla = false;
					e.Handled = true;
					return;
				}
				string text4 = NombreDeKey((e.Key == Key.System) ? e.SystemKey : e.Key);
				if (text4 != null)
				{
					AsignarTecla(text4);
				}
				e.Handled = true;
				return;
			}
		}
		if (e.Key == Key.Escape)
		{
			CerrarPropiedades();
			e.Handled = true;
		}
		else if (e.Key == Key.Delete)
		{
			EliminarSeleccion();
			e.Handled = true;
		}
	}

	private void OnBotonMouse(object sender, MouseButtonEventArgs e)
	{
		string text = e.ChangedButton switch
		{
			MouseButton.Right => "mouse_right", 
			MouseButton.Middle => "mouse_middle", 
			MouseButton.XButton1 => "mouse_x1", 
			MouseButton.XButton2 => "mouse_x2", 
			_ => null, 
		};
		if (text == null)
		{
			return;
		}
		if (_capturandoLibre)
		{
			AsignarTeclaLibre(text);
			e.Handled = true;
		}
		else if (_capturandoFree)
		{
			AsignarTeclaFree(text);
			e.Handled = true;
		}
		else if (_capturandoTecla)
		{
			Item? sel = _sel;
			if (sel != null && sel.Tipo == Tipo.Boton)
			{
				AsignarTecla(text);
				e.Handled = true;
			}
		}
	}

	private void Canvas_Down(object sender, MouseButtonEventArgs e)
	{
		if (e.OriginalSource == MarkerCanvas)
		{
			Seleccionar(null);
		}
	}

	private void IniciarArrastre(Item m, MouseButtonEventArgs e)
	{
		_arrastrando = m;
		_huboArrastre = false;
		Point position = e.GetPosition(MarkerCanvas);
		_grab = new Point(position.X - (Canvas.GetLeft(m.El) + m.El.Width / 2.0), position.Y - (Canvas.GetTop(m.El) + m.El.Height / 2.0));
		m.El.CaptureMouse();
	}

	private (double ox, double oy, double esc) Contenido()
	{
		double actualWidth = MarkerCanvas.ActualWidth;
		double actualHeight = MarkerCanvas.ActualHeight;
		if (actualWidth <= 0.0 || actualHeight <= 0.0)
		{
			return (ox: 0.0, oy: 0.0, esc: 0.0);
		}
		double num = Math.Min(actualWidth / (double)_devW, actualHeight / (double)_devH);
		return (ox: (actualWidth - (double)_devW * num) / 2.0, oy: (actualHeight - (double)_devH * num) / 2.0, esc: num);
	}

	private void MoverArrastre(Item m, MouseEventArgs e)
	{
		if (_arrastrando != m || e.LeftButton != MouseButtonState.Pressed)
		{
			return;
		}
		var (num, num2, num3) = Contenido();
		if (!(num3 <= 0.0))
		{
			Point position = e.GetPosition(MarkerCanvas);
			int num4 = Math.Clamp((int)Math.Round((position.X - _grab.X - num) / num3), 0, _devW - 1);
			int num5 = Math.Clamp((int)Math.Round((position.Y - _grab.Y - num2) / num3), 0, _devH - 1);
			if (Math.Abs(num4 - m.DevX) > 3 || Math.Abs(num5 - m.DevY) > 3)
			{
				_huboArrastre = true;
			}
			m.DevX = num4;
			m.DevY = num5;
			ReposicionarUno(m);
		}
	}

	private void Reposicionar()
	{
		foreach (Item item in _items)
		{
			ReposicionarUno(item);
		}
	}

	private void ReposicionarUno(Item m)
	{
		var (num, num2, num3) = Contenido();
		if (!(num3 <= 0.0))
		{
			double num4 = num + (double)m.DevX * num3;
			double num5 = num2 + (double)m.DevY * num3;
			Canvas.SetLeft(m.El, num4 - m.El.Width / 2.0);
			Canvas.SetTop(m.El, num5 - m.El.Height / 2.0);
			if (m.Tipo == Tipo.Joystick)
			{
				ActualizarAnillo(num4, num5, m.Radio);
			}
		}
	}

	private void ActualizarAnillo(double cx, double cy, int radioDev)
	{
		if (_anillo != null)
		{
			double item = Contenido().esc;
			if (!(item <= 0.0))
			{
				double num = (double)radioDev * item;
				_anillo.Width = num * 2.0;
				_anillo.Height = num * 2.0;
				Canvas.SetLeft(_anillo, cx - num);
				Canvas.SetTop(_anillo, cy - num);
			}
		}
	}

	private Task AdvertenciaAsync(string titulo, string mensaje)
	{
		StackPanel stackPanel = new StackPanel
		{
			MaxWidth = 380.0
		};
		stackPanel.Children.Add(new System.Windows.Controls.TextBlock
		{
			Text = mensaje,
			TextWrapping = TextWrapping.Wrap
		});
		return _dialogos.ShowAsync(new ContentDialog
		{
			Title = "⚠ " + titulo,
			Content = stackPanel,
			CloseButtonText = "Entendido",
			CloseButtonAppearance = ControlAppearance.Primary
		}, default(CancellationToken));
	}

	private async Task GuardarAsync(bool cerrar)
	{
		Item item = _items.FirstOrDefault((Item i) => i.Tipo == Tipo.Joystick);
		Item item2 = _items.FirstOrDefault((Item i) => i.Tipo == Tipo.Camara);
		if (item != null)
		{
			_keymap.Joystick.CenterX = item.DevX;
			_keymap.Joystick.CenterY = item.DevY;
			_keymap.Joystick.Radius = item.Radio;
		}
		if (_joyKeys.Count > 0)
		{
			_keymap.Joystick.Keys = new Dictionary<string, int[]>(_joyKeys);
			if (item != null)
			{
				ActualizarTexto(item);
			}
		}
		if (item2 != null)
		{
			_keymap.Camera.ZoneX = item2.DevX;
			_keymap.Camera.ZoneY = item2.DevY;
		}
		Item item3 = _items.FirstOrDefault((Item i) => i.Tipo == Tipo.Boton && string.IsNullOrEmpty(i.Key));
		if (item3 != null)
		{
			AbrirPropiedades(item3);
			IniciarCapturaTeclaBoton();
			await AdvertenciaAsync("Falta atribuir uma tecla", "O controle \"" + (string.IsNullOrWhiteSpace(item3.Label) ? "(sem nome)" : item3.Label) + "\" está marcado com ⚠️ na tela.\n\nPressione a tecla desejada no teclado ou clique do mouse para atribuir, ou clique em 'Excluir' na barra lateral se não for usar este botão.");
			return;
		}
		string[] reservadas = new string[3]
		{
			_keymap.ToggleKey,
			_keymap.ExitKey,
			_keymap.Camera.FreeMouseKey
		};
		Item item4 = _items.FirstOrDefault((Item i) => i.Tipo == Tipo.Boton && !string.IsNullOrEmpty(i.Key) && reservadas.Any((string r) => KeyNames.MismaTecla(r, i.Key)));
		if (item4 != null)
		{
			AbrirPropiedades(item4);
			await AdvertenciaAsync("Tecla em conflito", $"O controle \"{(string.IsNullOrWhiteSpace(item4.Label) ? "(sem nome)" : item4.Label)}\" usa \"{Bonito(item4.Key)}\", " + "que já está reservada para controlar o Mapeador (alternar captura, sair ou liberar o cursor). Atribua outra tecla ou botão e salve novamente.");
			_capturandoTecla = true;
			return;
		}
		Item item5 = _items.FirstOrDefault((Item i) => i.Tipo == Tipo.Boton && !string.IsNullOrEmpty(i.Key) && VetadaPorMod(i.Key));
		if (item5 != null)
		{
			AbrirPropiedades(item5);
			await AdvertenciaAsync("Tecla reservada para o espelho", $"O controle \"{(string.IsNullOrWhiteSpace(item5.Label) ? "(sem nome)" : item5.Label)}\" usa \"{Bonito(item5.Key)}\", " + "que é a tecla de atalhos (MOD) do espelho e provocaria ações cruzadas. Atribua outra tecla ou altere a tecla MOD em Opções extras e salve novamente.");
			_capturandoTecla = true;
			return;
		}
		string text = _joyKeys.Keys.FirstOrDefault((string k) => VetadaPorMod(k));
		if (text != null)
		{
			await AdvertenciaAsync("Tecla reservada para o espelho", "O joystick usa \"" + Bonito(text) + "\", que é a tecla de atalhos (MOD) do espelho e provocaria ações cruzadas. Atribua outra tecla a essa direção ou altere a tecla MOD em Opções extras e salve novamente.");
			return;
		}
		_keymap.Buttons.Clear();
		foreach (Item item6 in _items.Where((Item i) => i.Tipo == Tipo.Boton && !string.IsNullOrEmpty(i.Key)))
		{
			_keymap.Buttons.Add(new ButtonConfig
			{
				Key = item6.Key,
				X = item6.DevX,
				Y = item6.DevY,
				Label = item6.Label,
				Mode = item6.Modo,
				Size = item6.Tamano,
				RepeatMs = ((item6.Modo == "repeat") ? item6.IntervaloMs : 0)
			});
		}
		_onGuardar();
		if (cerrar)
		{
			Close();
		}
	}

	private static string Bonito(string key)
	{
		return key switch
		{
			"mouse_left" => "Clique esq.", 
			"mouse_right" => "Clique dir.", 
			"mouse_middle" => "Clique do meio", 
			"mouse_x1" => "Lateral 1", 
			"mouse_x2" => "Lateral 2", 
			_ => KeyNames.NombreBonito(key), 
		};
	}

	private static string? NombreDeKey(Key k)
	{
		if (k >= Key.A && k <= Key.Z)
		{
			return k.ToString();
		}
		if (k >= Key.D0 && k <= Key.D9)
		{
			return ((char)(48 + (k - 34))).ToString();
		}
		if (k >= Key.NumPad0 && k <= Key.NumPad9)
		{
			return ((char)(48 + (k - 74))).ToString();
		}
		if (k >= Key.F2 && k <= Key.F12)
		{
			return k.ToString();
		}
		return k switch
		{
			Key.Space => "Space", 
			Key.Tab => "Tab", 
			Key.Return => "Enter", 
			Key.LeftShift => "LeftShift", 
			Key.RightShift => "RightShift", 
			Key.LeftCtrl => "LeftCtrl", 
			Key.RightCtrl => "RightCtrl", 
			Key.LeftAlt => "LeftAlt", 
			Key.RightAlt => "RightAlt", 
			Key.OemComma => "OemComma", 
			Key.OemPeriod => "OemPeriod", 
			Key.OemMinus => "OemMinus", 
			Key.OemPlus => "OemPlus", 
			Key.Oem1 => "Oem1", 
			Key.Oem2 => "Oem2", 
			Key.Oem3 => "Oem3", 
			Key.Oem4 => "Oem4", 
			Key.Oem5 => "Oem5", 
			Key.Oem6 => "Oem6", 
			Key.Oem7 => "Oem7", 
			Key.Oem8 => "Oem8", 
			Key.Oem102 => "Oem102", 
			_ => null, 
		};
	}

	private void SeguirEspejo()
	{
		nint num = _hwndProvider();
		if (num == IntPtr.Zero || !IsWindow(num))
		{
			if (++_fallos >= 16)
			{
				Close();
			}
			return;
		}
		_fallos = 0;
		if (!GetClientRect(num, out var lpRect))
		{
			return;
		}
		POINT lpPoint = new POINT
		{
			X = 0,
			Y = 0
		};
		if (ClientToScreen(num, ref lpPoint))
		{
			int num2 = lpRect.Right - lpRect.Left;
			int num3 = lpRect.Bottom - lpRect.Top;
			if (num2 > 0 && num3 > 0 && _overlayHwnd != IntPtr.Zero)
			{
				SetWindowPos(_overlayHwnd, HWND_TOPMOST, lpPoint.X, lpPoint.Y, num2, num3, 16u);
			}
		}
	}

	public void CederFrente(bool ceder)
	{
		if (ceder)
		{
			_track.Stop();
			base.Topmost = false;
			if (_overlayHwnd != IntPtr.Zero)
			{
				SetWindowPos(_overlayHwnd, HWND_NOTOPMOST, 0, 0, 0, 0, 19u);
			}
		}
		else
		{
			base.Topmost = true;
			SeguirEspejo();
			_track.Start();
		}
	}

	[DllImport("user32.dll")]
	private static extern bool GetClientRect(nint hWnd, out RECT lpRect);

	[DllImport("user32.dll")]
	private static extern bool ClientToScreen(nint hWnd, ref POINT lpPoint);

	[DllImport("user32.dll")]
	private static extern bool IsWindow(nint hWnd);

	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(nint hWnd, nint after, int X, int Y, int cx, int cy, uint flags);
}


