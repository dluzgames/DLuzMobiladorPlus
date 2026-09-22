using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using DLuz.Helpers;
using DLuz.Mapper;
using DLuz.Mapper.Profile;
using DLuz.Views;

namespace DLuz.Services;

public sealed class MapeadorService : ObservableObject
{
	private struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	private struct MONITORINFO
	{
		public int cbSize;

		public RECT rcMonitor;

		public RECT rcWork;

		public uint dwFlags;
	}

	private readonly SessionState _s;

	private readonly MapeadorEngine _engine;

	private MapeadorOverlayWindow? _overlay;

	private MapeadorControlesOverlay? _controles;

	private MapeadorCursorOverlay? _cursor;

	private CursorMaskWindow? _cursorMask;

	private MapperSidePanel? _panel;

	private readonly DispatcherTimer _watch;

	private bool _restableciendoLayout;

	private bool _restaurarControlesAlCerrarEditor;

	private bool _inmersivo;

	private bool _controlesVisiblesPreInmersivo;

	private bool _mapperEnfocado = true;

	private bool _cursorInteractivo = true;

	[ObservableProperty]
	private bool _sesionActiva;

	[ObservableProperty]
	private bool _capturando;

	[ObservableProperty]
	private bool _editandoLayout;

	[ObservableProperty]
	private bool _mostrandoControles;

	[ObservableProperty]
	private int _resAncho;

	[ObservableProperty]
	private int _resAlto;

	public const string PerfilPredeterminado = "LX Mapper";

	private RECT _espejoRectPrevio;

	private bool _espejoRedimensionado;

	private const int AnchoPanelDip = 372;

	private const uint SWP_NOZORDER = 4u;

	private const uint SWP_NOACTIVATE = 16u;

	private const uint MONITOR_DEFAULTTONEAREST = 2u;

	private const byte VK_F11 = 122;

	private const uint KEYEVENTF_KEYUP = 2u;

	public void EstablecerPollingRate(int hz)
	{
		int ms = hz switch
		{
			>= 1000 => 1,
			>= 500 => 2,
			>= 250 => 4,
			_ => 8
		};
		_engine.PeriodoLoopMs = ms;
	}

	public KeymapConfig Keymap { get; private set; }

	public double SensibilidadCamaraX
	{
		get
		{
			if (!(Keymap.Camera.SensitivityX > 0.0))
			{
				if (!(Keymap.Camera.Sensitivity > 0.0))
				{
					return 1.0;
				}
				return Keymap.Camera.Sensitivity;
			}
			return Keymap.Camera.SensitivityX;
		}
	}

	public double SensibilidadCamaraY
	{
		get
		{
			if (!(Keymap.Camera.SensitivityY > 0.0))
			{
				return SensibilidadCamaraX * ((Keymap.Camera.SensitivityRatioY > 0.0) ? Keymap.Camera.SensitivityRatioY : 1.0);
			}
			return Keymap.Camera.SensitivityY;
		}
	}

	public string TeclaCaptura
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(Keymap.ToggleKey))
			{
				return Keymap.ToggleKey;
			}
			return "F1";
		}
	}

	public string TeclaLibre
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(Keymap.Camera.FreeMouseKey))
			{
				return Keymap.Camera.FreeMouseKey;
			}
			return "Alt";
		}
	}

	public string PerfilActivoNombre
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(_s.MapeadorPerfilNombre))
			{
				return _s.MapeadorPerfilNombre;
			}
			return "LX Mapper";
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool SesionActiva
	{
		get
		{
			return _sesionActiva;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_sesionActiva, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SesionActiva);
				_sesionActiva = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SesionActiva);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool Capturando
	{
		get
		{
			return _capturando;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_capturando, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Capturando);
				_capturando = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Capturando);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool EditandoLayout
	{
		get
		{
			return _editandoLayout;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_editandoLayout, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.EditandoLayout);
				_editandoLayout = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.EditandoLayout);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool MostrandoControles
	{
		get
		{
			return _mostrandoControles;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_mostrandoControles, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.MostrandoControles);
				_mostrandoControles = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.MostrandoControles);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int ResAncho
	{
		get
		{
			return _resAncho;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_resAncho, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ResAncho);
				_resAncho = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ResAncho);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int ResAlto
	{
		get
		{
			return _resAlto;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_resAlto, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ResAlto);
				_resAlto = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ResAlto);
			}
		}
	}

	public MapeadorService(SessionState s)
	{
		_s = s;
		Keymap = ProfileStore.Cargar(AppPaths.MapperProfilePath, 0, 0);
		_engine = new MapeadorEngine(s.AdbPath, Keymap);
		_engine.CapturaCambiada += delegate(bool activo)
		{
			SessionState.Marshal(delegate
			{
				Capturando = activo;
				ActualizarCursorOverlay();
			});
		};
		_engine.ConfinamientoCambiado += delegate(bool confinado)
		{
			SessionState.Marshal(delegate
			{
				ActualizarMascaraCursor(confinado);
			});
		};
		_engine.TogglePorMousePermitido = delegate
		{
			if (EditandoLayout)
			{
				return false;
			}
			nint foregroundWindow = GetForegroundWindow();
			return foregroundWindow != IntPtr.Zero && (foregroundWindow == _s.Scrcpy.ObtenerHandleVentana() || EsVentanaDeEsteProceso(foregroundWindow));
		};
		_watch = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(500.0)
		};
		_watch.Tick += delegate
		{
			if (SesionActiva && !_s.Scrcpy.EstaCorriendo)
			{
				DetenerSesion();
				ToastService.Mostrar("La sesión del Mapeador terminó.", ToastTipo.Advertencia);
			}
			else if (SesionActiva)
			{
				ActualizarModoInmersivo();
				ActualizarFocoOverlays();
			}
		};
	}

	public async Task<bool> IniciarSesionAsync()
	{
		if (SesionActiva)
		{
			return true;
		}
		if (_s.Scrcpy.EstaCorriendo)
		{
			await DialogService.AdvertenciaAsync("Espejo en curso", "Ya hay una sesión de scrcpy activa. Deténla antes de iniciar el Mapeador.");
			return false;
		}
		if (!_s.HayDispositivo && !_s.WifiConectado)
		{
			ToastService.Mostrar("Conecta tu teléfono antes de iniciar el Mapeador.", ToastTipo.Advertencia);
			return false;
		}
		List<string> item = (await Task.Run(() => _s.Adb.ListarDispositivos())).Item2;
		string serial = _s.WifiConectado
			? item.FirstOrDefault(ADBManager.EsSerialWifi) ?? item.FirstOrDefault()
			: item.FirstOrDefault((string x) => !ADBManager.EsSerialWifi(x)) ?? item.FirstOrDefault();
		var (flag, ancho, alto) = await Task.Run(() => _s.Adb.DetectarTamanoActual());
		if (!flag)
		{
			ToastService.Mostrar("No se pudo leer la resolución; se usará un valor por defecto.", ToastTipo.Advertencia);
		}
		// A captura do DLuzStacks é travada na horizontal. O motor HID deve usar
		// as mesmas dimensões da imagem rotacionada para manter os toques alinhados.
		if (alto > ancho)
		{
			(ancho, alto) = (alto, ancho);
		}
		bool vd = _s.MapeadorVd;
		(int, int) tuple2 = ParsearResolucionVd(_s.MapeadorVdResolucion);
		int vdAncho = tuple2.Item1;
		int vdAlto = tuple2.Item2;
		MapperDiag.Enabled = _s.ModoDebug;
		MapperDiag.Log($"Sesión: serial={serial ?? "(auto)"} resolución={ancho}x{alto} vd={vd} vdRes={vdAncho}x{vdAlto}");
		if (!LanzarEspejoVista(vd, _s.MapeadorVdResolucion))
		{
			await DialogService.ErrorAsync("Error al iniciar", "No se pudo abrir el espejo. Reconecta ADB e inténtalo de nuevo.");
			return false;
		}
		if (await EsperarVentanaEspejoAsync(12000) == IntPtr.Zero)
		{
			_s.Scrcpy.Detener();
			await DialogService.ErrorAsync("O espelhamento não apareceu", "A janela do espelhamento não foi exibida. Verifique o arquivo dluz.log e tente novamente.\n\nFeche outras sessões do scrcpy e verifique a conexão do aparelho.");
			return false;
		}
		int displayId = -1;
		if (vd)
		{
			displayId = await EsperarDisplayVirtualAsync(8000);
			if (displayId < 0)
			{
				_s.Scrcpy.Detener();
				await DialogService.ErrorAsync("Pantalla virtual no disponible", "El display virtual no se creó a tiempo. Revisa dluz.log e inténtalo de nuevo, o desactiva 'Jugar en pantalla virtual' para usar el modo clásico.");
				return false;
			}
			ancho = vdAncho;
			alto = vdAlto;
			await LanzarJuegoEnDisplayVirtualAsync(_s.MapeadorVdJuegoEfectivo(), displayId, vdAncho, vdAlto);
		}
		string versionScrcpy = await ScrcpyVersionService.ObtenerAsync();
		if (versionScrcpy == null)
		{
			versionScrcpy = "4.1";
			AppLogger.Warn("MapeadorService: no se detectó la versión de scrcpy; se asume " + versionScrcpy);
		}
		if (!(await Task.Run(() => _engine.ConectarServer(serial, ancho, alto, versionScrcpy, displayId))))
		{
			_s.Scrcpy.Detener();
			await DialogService.ErrorAsync("Error de control", "El espejo se abrió pero no se pudo conectar el control. Revisa dluz.log e inténtalo de nuevo.");
			return false;
		}
		Keymap = ProfileStore.Cargar(AppPaths.MapperProfilePath, _engine.Ancho, _engine.Alto);
		_engine.RecargarKeymap(Keymap);
		_engine.ActivarEntrada();
		// Abrir a sessão prepara o espelho e o editor, mas nunca captura o
		// teclado ou o mouse. A captura HID só começa por Play/F1.
		_engine.SetCaptura(activo: false);
		Capturando = false;
		EditandoLayout = false;
		ResAncho = _engine.Ancho;
		ResAlto = _engine.Alto;
		SesionActiva = true;
		_s.MapeadorActivo = true;
		_watch.Start();
		MostrarControles();
		MostrarCursor();
		AbrirPanel();
		ToastService.Mostrar("Mapeador listo. " + KeyNames.NombreBonito(TeclaCaptura) + " alterna la captura; sin captura, el clic sobre el espejo toca la pantalla.", ToastTipo.Exito, 4500);
		return true;
	}

	private async Task<nint> EsperarVentanaEspejoAsync(int timeoutMs)
	{
		for (int transcurrido = 0; transcurrido < timeoutMs; transcurrido += 200)
		{
			if (!_s.Scrcpy.EstaCorriendo)
			{
				return IntPtr.Zero;
			}
			nint num = _s.Scrcpy.ObtenerHandleVentana();
			if (num != IntPtr.Zero)
			{
				return num;
			}
			await Task.Delay(200);
		}
		return _s.Scrcpy.ObtenerHandleVentana();
	}

	public void DetenerSesion()
	{
		if (SesionActiva || _s.MapeadorActivo)
		{
			try
			{
				_overlay?.Close();
			}
			catch
			{
			}
			CerrarControles();
			try
			{
				_cursor?.Close();
			}
			catch
			{
			}
			_cursor = null;
			try
			{
				_cursorMask?.Close();
			}
			catch
			{
			}
			_cursorMask = null;
			try
			{
				_panel?.Close();
			}
			catch
			{
			}
			_panel = null;
			try
			{
				_engine.Detener();
			}
			catch (Exception ex)
			{
				AppLogger.Error("MapeadorService: error deteniendo engine", ex);
			}
			try
			{
				_s.Scrcpy.Detener();
			}
			catch
			{
			}
			_watch.Stop();
			_inmersivo = false;
			_controlesVisiblesPreInmersivo = false;
			_mapperEnfocado = true;
			Capturando = false;
			SesionActiva = false;
			_s.MapeadorActivo = false;
		}
	}

	public void AlternarCaptura()
	{
		if (SesionActiva)
		{
			if (!Capturando && EditandoLayout)
			{
				CerrarEditorLayout();
			}
			_engine.SetCaptura(!Capturando);
		}
	}

	/// <summary>Velocidade do cursor físico gravada pelo Android (-7 a +7).</summary>
	public int VelocidadeCursorAndroid
	{
		get => Math.Clamp(_s.PointerSpeed, -7, 7);
		set => _s.PointerSpeed = Math.Clamp(value, -7, 7);
	}

	public bool FpsModoDluzStacks => _s.OverlayFps;

	public bool GravandoTela => _s.CapturaMidia.Gravando;

	public Task<(bool ok, string mensagem)> CapturarTelaDluzStacksAsync() => _s.CapturaMidia.CapturarTelaAsync();

	public async Task<(bool ok, string mensagem)> AlternarGravacaoTelaDluzStacksAsync()
	{
		var resultado = await _s.CapturaMidia.AlternarGravacaoAsync();
		OnPropertyChanged(nameof(GravandoTela));
		return resultado;
	}

	public void AlternarFpsModoDluzStacks()
	{
		_s.OverlayFps = !_s.OverlayFps;
		_s.GuardarConfig();
		_controles?.EstablecerFpsVisible(_s.OverlayFps);
		OnPropertyChanged(nameof(FpsModoDluzStacks));
	}

	public bool Dlss5Activo => DLSS5Service.Activo;
	public string Dlss5Preset => DLSS5Service.Preset;

	public void AlternarDlss5()
	{
		bool estado = DLSS5Service.Alternar();
		_s.Dlss5Modo = estado;
		_s.GuardarConfig();
		OnPropertyChanged(nameof(Dlss5Activo));
		ToastService.Mostrar(estado ? "🚀 DLSS 5 Ultra AI Ativado no Espelhamento!" : "DLSS 5 Desativado.", estado ? ToastTipo.Exito : ToastTipo.Info, 2500);
	}

	public void EstablecerDlss5Preset(string preset)
	{
		DLSS5Service.EstablecerPreset(preset);
		_s.Dlss5Preset = preset;
		_s.GuardarConfig();
		OnPropertyChanged(nameof(Dlss5Preset));
		ToastService.Mostrar($"Preset DLSS 5: {preset}", ToastTipo.Exito, 2000);
	}

	public async Task<(bool exito, string erro)> AplicarVelocidadeCursorAndroidAsync(int velocidade)
	{
		VelocidadeCursorAndroid = velocidade;
		var resultado = await _s.Adb.AplicarPointerSpeedAsync(VelocidadeCursorAndroid);
		if (resultado.Item1)
		{
			_s.UltimaVelocidadCursor = VelocidadeCursorAndroid;
			_s.GuardarConfig();
		}
		return (resultado.Item1, resultado.Item2);
	}

	public void AlternarControles()
	{
		if (_controles != null)
		{
			try
			{
				_controles.Close();
			}
			catch
			{
			}
			_controles = null;
			MostrandoControles = false;
		}
		else
		{
			MostrarControles();
		}
	}

	private void MostrarControles()
	{
		if (_controles != null)
		{
			return;
		}
		if (!SesionActiva || _s.Scrcpy.ObtenerHandleVentana() == IntPtr.Zero)
		{
			ToastService.Mostrar("Inicia la sesión y espera a que aparezca el juego para mostrar los controles.", ToastTipo.Advertencia);
			return;
		}
		_controles = new MapeadorControlesOverlay(Keymap, _engine.Ancho, _engine.Alto, () => _s.Scrcpy.ObtenerHandleVentana(), Keymap.OverlayOpacity, _s.Scrcpy, FpsModoDluzStacks);
		_controles.Closed += delegate
		{
			_controles = null;
			MostrandoControles = false;
		};
		_controles.Show();
		MostrandoControles = true;
	}

	private void MostrarCursor()
	{
		if (_cursor == null && SesionActiva && _s.Scrcpy.ObtenerHandleVentana() != IntPtr.Zero)
		{
			_cursor = new MapeadorCursorOverlay(_engine, () => _s.Scrcpy.ObtenerHandleVentana());
			_cursor.Closed += delegate
			{
				_cursor = null;
			};
			_cursor.Show();
			ActualizarCursorOverlay();
		}
	}

	private void ActualizarCursorOverlay()
	{
		if (_cursor == null)
		{
			return;
		}
		if (SesionActiva && !Capturando && !EditandoLayout && _mapperEnfocado)
		{
			if (!_cursor.IsVisible)
			{
				_cursor.Show();
			}
		}
		else
		{
			_cursor.OcultarYSoltar();
		}
		ActualizarMascaraCursor(Capturando && !EditandoLayout);
	}

	private void AbrirPanel()
	{
		if (_panel != null)
		{
			_panel.Activate();
			return;
		}
		_panel = new MapperSidePanel(this, () => _s.Scrcpy.ObtenerHandleVentana());
		_panel.Closed += delegate
		{
			_panel = null;
		};
		_panel.Show();
	}

	public void EstablecerOpacidadControles(double v)
	{
		Keymap.OverlayOpacity = v;
		_controles?.EstablecerOpacidad(v);
		GuardarKeymap();
	}

		public double PuxadaCapaY => Keymap?.Camera?.ExponentY ?? 1.35;
	public double AceleracaoCamera => Keymap?.Camera?.Acceleration ?? 0.6;
	public double SuavizacaoMira => Keymap?.Camera?.Smoothing ?? 0.1;

	public void EstablecerCamaraAvanzada(double? puxadaCapa, double? aceleracion, double? suavizacao)
	{
		if (Keymap?.Camera == null) return;
		if (puxadaCapa.HasValue)
			Keymap.Camera.ExponentY = Math.Clamp(puxadaCapa.Value, 1.0, 2.5);
		if (aceleracion.HasValue)
			Keymap.Camera.Acceleration = Math.Clamp(aceleracion.Value, 0.0, 1.5);
		if (suavizacao.HasValue)
			Keymap.Camera.Smoothing = Math.Clamp(suavizacao.Value, 0.0, 0.5);

		GuardarKeymap();
		OnPropertyChanged(nameof(PuxadaCapaY));
		OnPropertyChanged(nameof(AceleracaoCamera));
		OnPropertyChanged(nameof(SuavizacaoMira));
	}

	public void EstablecerCamara(double sensibilidadX, double sensibilidadY)
	{
		double num = Math.Clamp(sensibilidadX, 0.01, 20.0);
		double num2 = Math.Clamp(sensibilidadY, 0.01, 20.0);
		Keymap.Camera.Sensitivity = num;
		Keymap.Camera.SensitivityX = num;
		Keymap.Camera.SensitivityY = num2;
		Keymap.Camera.SensitivityRatioY = ((num > 0.0) ? (num2 / num) : 1.0);
		GuardarKeymap();
		OnPropertyChanged("SensibilidadCamaraX");
		OnPropertyChanged("SensibilidadCamaraY");
	}

	public string? EstablecerTeclaLibre(string tecla)
	{
		if (string.IsNullOrWhiteSpace(tecla))
		{
			return "Tecla vacía.";
		}
		string text = tecla.Trim().ToLowerInvariant();
		if (text == "mouse_left" || text == "mouse_right")
		{
			return "Los clics izquierdo y derecho del mouse no se pueden usar aquí.";
		}
		if (KeyNames.MismaTecla(tecla, Keymap.ToggleKey))
		{
			return "Choca con la tecla de suspender la captura (" + KeyNames.NombreBonito(Keymap.ToggleKey) + ").";
		}
		if (KeyNames.MismaTecla(tecla, Keymap.ExitKey))
		{
			return "Choca con la tecla para salir del Mapeador (" + KeyNames.NombreBonito(Keymap.ExitKey) + ").";
		}
		if (Keymap.Buttons.Any((ButtonConfig b) => KeyNames.MismaTecla(b.Key, tecla)))
		{
			return "\"" + KeyNames.NombreBonito(tecla) + "\" ya la usa un botón del juego.";
		}
		if (Keymap.Joystick.Keys.Keys.Any((string k) => KeyNames.MismaTecla(k, tecla)))
		{
			return "\"" + KeyNames.NombreBonito(tecla) + "\" ya la usa el joystick.";
		}
		Keymap.Camera.FreeMouseKey = tecla;
		GuardarKeymap();
		OnPropertyChanged("TeclaLibre");
		return null;
	}

	public bool ExportarPerfil(string ruta)
	{
		int ancho = (SesionActiva ? _engine.Ancho : 0);
		int alto = (SesionActiva ? _engine.Alto : 0);
		return ProfileStore.Guardar(ruta, Keymap, ancho, alto);
	}

	public bool ExportarBlueStacks(string ruta)
	{
		int ancho = (SesionActiva ? _engine.Ancho : 2400);
		int alto = (SesionActiva ? _engine.Alto : 1080);
		return BlueStacksConfigExporter.TryExport(ruta, Keymap, ancho, alto);
	}

	public void CederFrenteADialogo(bool ceder)
	{
		try
		{
			_panel?.CederFrente(ceder);
		}
		catch
		{
		}
		try
		{
			_overlay?.CederFrente(ceder);
		}
		catch
		{
		}
		try
		{
			_controles?.CederFrente(ceder);
		}
		catch
		{
		}
		try
		{
			_cursor?.CederFrente(ceder);
		}
		catch
		{
		}
		try
		{
			if (ceder)
			{
				_cursorMask?.Hide();
			}
			else
			{
				ActualizarMascaraCursor(Capturando && !EditandoLayout);
			}
		}
		catch
		{
		}
	}

	private static string RutaPerfil(string nombre)
	{
		return Path.Combine(AppPaths.MapperProfilesDir, nombre + ".json");
	}

	private static string SanearNombre(string nombre)
	{
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		foreach (char oldChar in invalidFileNameChars)
		{
			nombre = nombre.Replace(oldChar, '_');
		}
		return nombre.Trim();
	}

	public List<string> ListarPerfiles()
	{
		try
		{
			Directory.CreateDirectory(AppPaths.MapperProfilesDir);
			List<string> list = (from n in Directory.GetFiles(AppPaths.MapperProfilesDir, "*.json").Select(Path.GetFileNameWithoutExtension)
				where !string.IsNullOrWhiteSpace(n)
				select (n)).OrderBy<string, string>((string n) => n, StringComparer.OrdinalIgnoreCase).ToList();
			if (!list.Contains(PerfilActivoNombre))
			{
				ExportarPerfil(RutaPerfil(PerfilActivoNombre));
				list.Add(PerfilActivoNombre);
				list.Sort(StringComparer.OrdinalIgnoreCase);
			}
			return list;
		}
		catch
		{
			return new List<string> { PerfilActivoNombre };
		}
	}

	public bool ActivarPerfil(string nombre)
	{
		nombre = SanearNombre(nombre);
		if (nombre.Length == 0 || nombre == PerfilActivoNombre)
		{
			return false;
		}
		string text = RutaPerfil(nombre);
		if (!File.Exists(text))
		{
			return false;
		}
		ExportarPerfil(RutaPerfil(PerfilActivoNombre));
		if (!ImportarPerfil(text))
		{
			return false;
		}
		_s.MapeadorPerfilNombre = nombre;
		_s.GuardarConfig();
		return true;
	}

	public bool CrearPerfil(string nombre)
	{
		nombre = SanearNombre(nombre);
		if (nombre.Length == 0)
		{
			return false;
		}
		Directory.CreateDirectory(AppPaths.MapperProfilesDir);
		if (File.Exists(RutaPerfil(nombre)))
		{
			return false;
		}
		ExportarPerfil(RutaPerfil(PerfilActivoNombre));
		if (!ExportarPerfil(RutaPerfil(nombre)))
		{
			return false;
		}
		_s.MapeadorPerfilNombre = nombre;
		_s.GuardarConfig();
		return true;
	}

	public bool RenombrarPerfil(string nuevo)
	{
		nuevo = SanearNombre(nuevo);
		if (nuevo.Length == 0 || nuevo == PerfilActivoNombre)
		{
			return false;
		}
		if (File.Exists(RutaPerfil(nuevo)))
		{
			return false;
		}
		try
		{
			Directory.CreateDirectory(AppPaths.MapperProfilesDir);
			string text = RutaPerfil(PerfilActivoNombre);
			if (File.Exists(text))
			{
				File.Move(text, RutaPerfil(nuevo));
			}
			else
			{
				ExportarPerfil(RutaPerfil(nuevo));
			}
		}
		catch (Exception ex)
		{
			AppLogger.Error("MapeadorService: no se pudo renombrar el perfil", ex);
			return false;
		}
		_s.MapeadorPerfilNombre = nuevo;
		_s.GuardarConfig();
		return true;
	}

	public bool EliminarPerfil(string nombre)
	{
		nombre = SanearNombre(nombre);
		if (nombre.Length == 0)
		{
			return false;
		}
		List<string> list = ListarPerfiles();
		if (list.Count <= 1 || !list.Contains(nombre))
		{
			return false;
		}
		try
		{
			File.Delete(RutaPerfil(nombre));
		}
		catch (Exception ex)
		{
			AppLogger.Error("MapeadorService: no se pudo eliminar el perfil", ex);
			return false;
		}
		if (nombre != PerfilActivoNombre)
		{
			return true;
		}
		list.Remove(nombre);
		string text = (list.Contains("LX Mapper") ? "LX Mapper" : list[0]);
		_s.MapeadorPerfilNombre = text;
		_s.GuardarConfig();
		ImportarPerfil(RutaPerfil(text));
		return true;
	}

	public bool ImportarPerfilConNombre(string ruta, string nombre)
	{
		nombre = SanearNombre(nombre);
		if (nombre.Length == 0)
		{
			return false;
		}
		if (!ProfileStore.EsPerfilValido(ruta))
		{
			return false;
		}
		try
		{
			Directory.CreateDirectory(AppPaths.MapperProfilesDir);
			ExportarPerfil(RutaPerfil(PerfilActivoNombre));
			File.Copy(ruta, RutaPerfil(nombre), overwrite: true);
		}
		catch (Exception ex)
		{
			AppLogger.Error("MapeadorService: no se pudo copiar el perfil importado", ex);
			return false;
		}
		if (!ImportarPerfil(RutaPerfil(nombre)))
		{
			return false;
		}
		_s.MapeadorPerfilNombre = nombre;
		_s.GuardarConfig();
		return true;
	}

	public bool ImportarBlueStacksConNombre(string ruta, string nombre, out string mensaje)
	{
		mensaje = "";
		nombre = SanearNombre(nombre);
		if (nombre.Length == 0 || !BlueStacksConfigImporter.TryImport(ruta, out KeymapConfig keymap, out mensaje))
		{
			return false;
		}
		try
		{
			Directory.CreateDirectory(AppPaths.MapperProfilesDir);
			ExportarPerfil(RutaPerfil(PerfilActivoNombre));
			if (!ProfileStore.Guardar(RutaPerfil(nombre), keymap, 2400, 1080) || !ImportarPerfil(RutaPerfil(nombre)))
			{
				mensaje = "Não foi possível salvar o perfil convertido.";
				return false;
			}
			_s.MapeadorPerfilNombre = nombre;
			_s.GuardarConfig();
			return true;
		}
		catch (Exception ex)
		{
			AppLogger.Error("MapeadorService: não foi possível importar o esquema BlueStacks", ex);
			mensaje = "Não foi possível importar o esquema BlueStacks.";
			return false;
		}
	}

	public bool ImportarPerfil(string ruta)
	{
		if (!ProfileStore.EsPerfilValido(ruta))
		{
			return false;
		}
		try
		{
			File.Copy(ruta, AppPaths.MapperProfilePath, overwrite: true);
		}
		catch (Exception ex)
		{
			AppLogger.Error("MapeadorService: não foi possível copiar o perfil importado", ex);
			return false;
		}
		bool flag = _overlay != null;
		_restableciendoLayout = true;
		try
		{
			_overlay?.Close();
		}
		catch
		{
		}
		_overlay = null;
		EditandoLayout = false;
		bool flag2 = _controles != null || MostrandoControles;
		CerrarControles();
		RecargarKeymapDesdeDisco();
		_restableciendoLayout = false;
		OnPropertyChanged("SensibilidadCamaraX");
		OnPropertyChanged("SensibilidadCamaraY");
		OnPropertyChanged("TeclaCaptura");
		OnPropertyChanged("TeclaLibre");
		if (SesionActiva)
		{
			if (flag)
			{
				AbrirEditorLayout();
			}
			else if (flag2)
			{
				MostrarControles();
			}
		}
		return true;
	}

	public bool AplicarPresetJogo(string nomeJogo)
	{
		try
		{
			KeymapConfig preset = KeymapConfig.CargarPreset(nomeJogo);
			Keymap = preset;
			GuardarKeymap();
			RecargarKeymapDesdeDisco();
			OnPropertyChanged("SensibilidadCamaraX");
			OnPropertyChanged("SensibilidadCamaraY");
			OnPropertyChanged("TeclaCaptura");
			OnPropertyChanged("TeclaLibre");
			return true;
		}
		catch (Exception ex)
		{
			AppLogger.Error("MapeadorService: falha ao aplicar preset " + nomeJogo, ex);
			return false;
		}
	}

	public string? EstablecerTeclaCaptura(string tecla)
	{
		if (string.IsNullOrWhiteSpace(tecla))
		{
			return "Tecla vazia.";
		}
		if (tecla.Trim().ToLowerInvariant() == "mouse_left")
		{
			return "O clique esquerdo do mouse não pode ser usado aqui: é o clique de tiro.";
		}
		if (KeyNames.MismaTecla(tecla, Keymap.ExitKey))
		{
			return "Conflito com a tecla de sair do Mapeador (" + KeyNames.NombreBonito(Keymap.ExitKey) + ").";
		}
		if (KeyNames.MismaTecla(tecla, Keymap.Camera.FreeMouseKey))
		{
			return "Conflito com a tecla que libera o cursor ao mantê-la pressionada (" + KeyNames.NombreBonito(Keymap.Camera.FreeMouseKey) + ").";
		}
		if (Keymap.Buttons.Any((ButtonConfig b) => KeyNames.MismaTecla(b.Key, tecla)))
		{
			return "\"" + KeyNames.NombreBonito(tecla) + "\" já é usada por um botão do jogo.";
		}
		if (Keymap.Joystick.Keys.Keys.Any((string k) => KeyNames.MismaTecla(k, tecla)))
		{
			return "\"" + KeyNames.NombreBonito(tecla) + "\" já é usada pelo analógico.";
		}
		Keymap.ToggleKey = tecla;
		GuardarKeymap();
		OnPropertyChanged("TeclaCaptura");
		return null;
	}

	private void CerrarControles()
	{
		if (_controles != null)
		{
			try
			{
				_controles.Close();
			}
			catch
			{
			}
			_controles = null;
			MostrandoControles = false;
		}
	}

	private void ActualizarMascaraCursor(bool confinado)
	{
		if (confinado && SesionActiva)
		{
			if (_cursorMask == null)
			{
				_cursorMask = new CursorMaskWindow();
			}
			if (!_cursorMask.IsVisible)
			{
				_cursorMask.Show();
			}
			return;
		}
		try
		{
			_cursorMask?.Hide();
		}
		catch
		{
		}
	}

	public void AbrirEditorLayout()
	{
		if (!SesionActiva)
		{
			ToastService.Mostrar("Inicie a sessão antes de editar os controles.", ToastTipo.Advertencia);
			return;
		}
		if (_overlay != null)
		{
			_overlay.Activate();
			return;
		}
		if (_s.Scrcpy.ObtenerHandleVentana() == IntPtr.Zero)
		{
			ToastService.Mostrar("Espera a que aparezca la ventana del juego antes de editar.", ToastTipo.Advertencia);
			return;
		}
		if (Capturando)
		{
			_engine.SetCaptura(activo: false);
		}
		_restaurarControlesAlCerrarEditor = _controles != null || MostrandoControles;
		CerrarControles();
		HacerEspacioParaPanel();
		_overlay = new MapeadorOverlayWindow(Keymap, this, _engine.Ancho, _engine.Alto, () => _s.Scrcpy.ObtenerHandleVentana(), delegate
		{
			GuardarKeymap();
			ToastService.Mostrar("Layout guardado.", ToastTipo.Exito, 2500);
		});
		_overlay.Closed += delegate
		{
			_overlay = null;
			EditandoLayout = false;
			RestaurarEspejoTrasEdicion();
			if (!_restableciendoLayout && _restaurarControlesAlCerrarEditor && SesionActiva)
			{
				MostrarControles();
			}
			_restaurarControlesAlCerrarEditor = false;
			ActualizarCursorOverlay();
		};
		EditandoLayout = true;
		ActualizarCursorOverlay();
		_overlay.Show();
		nint numOverlay = _s.Scrcpy.ObtenerHandleVentana();
		if (numOverlay != IntPtr.Zero)
		{
			ShowWindow(numOverlay, 5);
			SetForegroundWindow(numOverlay);
		}
		_overlay.Activate();
	}

	public void CerrarEditorLayout()
	{
		if (_overlay == null)
		{
			return;
		}
		try
		{
			_overlay.Close();
		}
		catch
		{
		}
	}

	public void EditorAgregarBoton()
	{
		_overlay?.AgregarBotonNuevo();
	}

	public void EditorAgregarTap()
	{
		_overlay?.AgregarTapNuevo();
	}

	public void EditorAgregarRepetido()
	{
		_overlay?.AgregarRepetidoNuevo();
	}

	public void EditorAgregarAtirar()
	{
		_overlay?.AgregarAtirarMouseLeft();
	}

	public void EditorAgregarMira()
	{
		_overlay?.AgregarMirarMouseRight();
	}

	public void EditorReposicionarWasd()
	{
		_overlay?.ReposicionarJoystickWASD();
	}

	public void EditorReposicionarCamera()
	{
		_overlay?.ReposicionarMouseCamera();
	}

	public void EditorAplicarPreConfigurados()
	{
		_overlay?.AplicarControlesPreConfiguradosPadrao();
	}

	public async void EditorGuardar()
	{
		MapeadorOverlayWindow overlay = _overlay;
		if (overlay == null)
		{
			return;
		}
		try
		{
			await overlay.GuardarSinCerrarAsync();
		}
		catch (Exception ex)
		{
			AppLogger.Error("MapeadorService: error guardando el layout", ex);
		}
	}

	public void EditorCancelar()
	{
		_overlay?.Cancelar();
	}

	private void ActualizarFocoOverlays()
	{
		nint foregroundWindow = GetForegroundWindow();
		nint num = _s.Scrcpy.ObtenerHandleVentana();
		bool flag = foregroundWindow != IntPtr.Zero && (foregroundWindow == num || EsVentanaDeEsteProceso(foregroundWindow));
		bool flag2 = foregroundWindow != IntPtr.Zero && foregroundWindow == num;
		if (flag2 != _cursorInteractivo)
		{
			_cursorInteractivo = flag2;
			_cursor?.EstablecerClickThrough(!flag2);
		}
		if (flag == _mapperEnfocado)
		{
			return;
		}
		_mapperEnfocado = flag;
		if (!flag)
		{
			if (Capturando)
			{
				_engine.SetCaptura(activo: false);
			}
			try
			{
				_controles?.Hide();
			}
			catch
			{
			}
			try
			{
				_panel?.Hide();
			}
			catch
			{
			}
			try
			{
				_overlay?.Hide();
			}
			catch
			{
			}
			try
			{
				_cursor?.OcultarYSoltar();
				return;
			}
			catch
			{
				return;
			}
		}
		if (!_inmersivo)
		{
			try
			{
				_panel?.Show();
			}
			catch
			{
			}
			if (MostrandoControles)
			{
				try
				{
					_controles?.Show();
				}
				catch
				{
				}
			}
		}
		try
		{
			_overlay?.Show();
		}
		catch
		{
		}
		ActualizarCursorOverlay();
	}

	private void ActualizarModoInmersivo()
	{
		if (EditandoLayout)
		{
			return;
		}
		nint num = _s.Scrcpy.ObtenerHandleVentana();
		bool flag = num != IntPtr.Zero && EsVentanaFullscreen(num);
		if (flag == _inmersivo)
		{
			return;
		}
		_inmersivo = flag;
		if (flag)
		{
			_controlesVisiblesPreInmersivo = _controles != null;
			try
			{
				_controles?.Hide();
			}
			catch
			{
			}
			try
			{
				_panel?.Hide();
			}
			catch
			{
			}
			return;
		}
		try
		{
			_panel?.Show();
		}
		catch
		{
		}
		if (!_controlesVisiblesPreInmersivo)
		{
			return;
		}
		try
		{
			_controles?.Show();
		}
		catch
		{
		}
	}

	private static bool EsVentanaDeEsteProceso(nint hwnd)
	{
		GetWindowThreadProcessId(hwnd, out var lpdwProcessId);
		return lpdwProcessId == (uint)Environment.ProcessId;
	}

	public async void AlternarPantallaCompleta()
	{
		if (SesionActiva)
		{
			nint num = _s.Scrcpy.ObtenerHandleVentana();
			if (num == IntPtr.Zero)
			{
				ToastService.Mostrar("La ventana del juego aún no está lista.", ToastTipo.Advertencia);
				return;
			}
			SetForegroundWindow(num);
			await Task.Delay(80);
			keybd_event(122, 0, 0u, UIntPtr.Zero);
			keybd_event(122, 0, 2u, UIntPtr.Zero);
		}
	}

	public async Task GirarTelaAsync(int graus = 90)
	{
		if (!SesionActiva)
		{
			ToastService.Mostrar("Inicie a sessão antes de girar a tela.", ToastTipo.Advertencia);
			return;
		}
		nint num = _s.Scrcpy.ObtenerHandleVentana();
		if (num == IntPtr.Zero)
		{
			ToastService.Mostrar("A janela do jogo ainda não está pronta.", ToastTipo.Advertencia);
			return;
		}

		SetForegroundWindow(num);
		await Task.Delay(80);

		int passos = (graus == 180) ? 2 : 1;
		for (int i = 0; i < passos; i++)
		{
			keybd_event(18, 0, 0u, UIntPtr.Zero);
			await Task.Delay(35);
			keybd_event(82, 0, 0u, UIntPtr.Zero);
			await Task.Delay(35);
			keybd_event(82, 0, 2u, UIntPtr.Zero);
			await Task.Delay(35);
			keybd_event(18, 0, 2u, UIntPtr.Zero);

			if (i < passos - 1)
			{
				await Task.Delay(80);
			}
		}

		if (_s.MapeadorOrientacao == "@90")
		{
			_s.MapeadorOrientacao = "@270";
		}
		else if (_s.MapeadorOrientacao == "@270")
		{
			_s.MapeadorOrientacao = "@90";
		}
		else
		{
			_s.MapeadorOrientacao = "@270";
		}

		ToastService.Mostrar("🔄 Tela girada com sucesso! (Clique novamente se precisar girar 180°)", ToastTipo.Exito, 2500);
	}

	private void HacerEspacioParaPanel()
	{
		nint num = _s.Scrcpy.ObtenerHandleVentana();
		if (num == IntPtr.Zero || !GetWindowRect(num, out var lpRect))
		{
			return;
		}
		nint num2 = MonitorFromWindow(num, 2u);
		if (num2 == IntPtr.Zero)
		{
			return;
		}
		MONITORINFO lpmi = new MONITORINFO
		{
			cbSize = Marshal.SizeOf<MONITORINFO>()
		};
		if (GetMonitorInfo(num2, ref lpmi))
		{
			double num3 = 1.0;
			try
			{
				num3 = (double)GetDpiForWindow(num) / 96.0;
			}
			catch
			{
			}
			int num4 = (int)Math.Round(372.0 * num3) + 20;
			int num5 = lpmi.rcWork.Right - num4;
			if (lpRect.Right > num5)
			{
				_espejoRectPrevio = lpRect;
				_espejoRedimensionado = true;
				int cx = Math.Max(480, num5 - lpRect.Left);
				SetWindowPos(num, IntPtr.Zero, lpRect.Left, lpRect.Top, cx, lpRect.Bottom - lpRect.Top, 20u);
			}
		}
	}

	private void RestaurarEspejoTrasEdicion()
	{
		if (_espejoRedimensionado)
		{
			_espejoRedimensionado = false;
			nint num = _s.Scrcpy.ObtenerHandleVentana();
			if (num != IntPtr.Zero)
			{
				RECT espejoRectPrevio = _espejoRectPrevio;
				SetWindowPos(num, IntPtr.Zero, espejoRectPrevio.Left, espejoRectPrevio.Top, espejoRectPrevio.Right - espejoRectPrevio.Left, espejoRectPrevio.Bottom - espejoRectPrevio.Top, 20u);
			}
		}
	}

	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

	[DllImport("user32.dll")]
	private static extern uint GetDpiForWindow(nint hwnd);

	private static bool EsVentanaFullscreen(nint hwnd)
	{
		if (!GetWindowRect(hwnd, out var lpRect))
		{
			return false;
		}
		nint num = MonitorFromWindow(hwnd, 2u);
		if (num == IntPtr.Zero)
		{
			return false;
		}
		MONITORINFO lpmi = new MONITORINFO
		{
			cbSize = Marshal.SizeOf<MONITORINFO>()
		};
		if (!GetMonitorInfo(num, ref lpmi))
		{
			return false;
		}
		RECT rcMonitor = lpmi.rcMonitor;
		if (lpRect.Left <= rcMonitor.Left && lpRect.Top <= rcMonitor.Top && lpRect.Right >= rcMonitor.Right)
		{
			return lpRect.Bottom >= rcMonitor.Bottom;
		}
		return false;
	}

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

	[DllImport("user32.dll")]
	private static extern nint MonitorFromWindow(nint hwnd, uint dwFlags);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFO lpmi);

	[DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(nint hWnd);

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(nint hWnd, int nCmdShow);

	[DllImport("user32.dll")]
	private static extern nint GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

	[DllImport("user32.dll")]
	private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, nuint dwExtraInfo);

	public bool GuardarKeymap()
	{
		int ancho = (SesionActiva ? _engine.Ancho : 0);
		int alto = (SesionActiva ? _engine.Alto : 0);
		bool flag = ProfileStore.Guardar(AppPaths.MapperProfilePath, Keymap, ancho, alto);
		if (flag)
		{
			_engine.RecargarKeymap(Keymap);
			try
			{
				Directory.CreateDirectory(AppPaths.MapperProfilesDir);
				ProfileStore.Guardar(RutaPerfil(PerfilActivoNombre), Keymap, ancho, alto);
			}
			catch
			{
			}
		}
		return flag;
	}

	public async Task RestablecerPerfilAsync()
	{
		bool confirmado;
		CederFrenteADialogo(ceder: true);
		try
		{
			confirmado = await DialogService.ConfirmarAsync("Redefinir perfil", "Isso apagará o layout atual e restaurará o perfil padrão. Deseja continuar?", "As posições e teclas personalizadas serão substituídas.", "Redefinir", "Cancelar");
		}
		finally
		{
			CederFrenteADialogo(ceder: false);
		}
		if (!confirmado)
		{
			return;
		}
		bool flag = _overlay != null || EditandoLayout;
		bool flag2 = _controles != null || MostrandoControles;
		_restableciendoLayout = true;
		try
		{
			_overlay?.Close();
		}
		catch
		{
		}
		_overlay = null;
		EditandoLayout = false;
		CerrarControles();
		try
		{
			File.Delete(AppPaths.MapperProfilePath);
		}
		catch
		{
		}
		try
		{
			File.Delete(AppPaths.KeymapPath);
		}
		catch
		{
		}
		int ancho = (SesionActiva ? _engine.Ancho : 0);
		int alto = (SesionActiva ? _engine.Alto : 0);
		Keymap = ProfileStore.Cargar(AppPaths.MapperProfilePath, ancho, alto);
		_engine.RecargarKeymap(Keymap);
		_restableciendoLayout = false;
		if (SesionActiva)
		{
			if (flag)
			{
				AbrirEditorLayout();
			}
			else if (flag2)
			{
				MostrarControles();
			}
		}
		ToastService.Mostrar("Perfil restablecido.", ToastTipo.Exito, 2500);
	}

	public void RecargarKeymapDesdeDisco()
	{
		int ancho = (SesionActiva ? _engine.Ancho : 0);
		int alto = (SesionActiva ? _engine.Alto : 0);
		Keymap = ProfileStore.Cargar(AppPaths.MapperProfilePath, ancho, alto);
		_engine.RecargarKeymap(Keymap);
	}

	private bool LanzarEspejoVista(bool pantallaVirtual, string vdResolucion)
	{
		ScrcpyConfig scrcpyConfig = _s.ObtenerConfigActual();
		scrcpyConfig.SoloEspejoSinControl = true;
		scrcpyConfig.ModoOtg = false;
		scrcpyConfig.Video = true;
		List<string> conectados = _s.Adb.ListarDispositivos().seriales;
		string serialEfectivo = _s.WifiConectado
			? conectados.FirstOrDefault(ADBManager.EsSerialWifi) ?? conectados.FirstOrDefault() ?? ""
			: conectados.FirstOrDefault((string x) => !ADBManager.EsSerialWifi(x)) ?? conectados.FirstOrDefault() ?? "";
		scrcpyConfig.SerialDestino = serialEfectivo;

		bool esWsaOEmulador = !string.IsNullOrEmpty(serialEfectivo) && (serialEfectivo.StartsWith("127.0.0.1:") || serialEfectivo.StartsWith("localhost:"));
		if (esWsaOEmulador)
		{
			scrcpyConfig.ForcarOrientacaoHorizontal = false;
			scrcpyConfig.OrientacaoCaptura = "";
		}
		else
		{
			scrcpyConfig.ForcarOrientacaoHorizontal = true;
			scrcpyConfig.OrientacaoCaptura = string.IsNullOrWhiteSpace(_s.MapeadorOrientacao) ? "@270" : _s.MapeadorOrientacao;
		}

		// Calibração de Alta Estabilidade e Zero Travamentos para Modo Wi-Fi (Exclusivo PRO)
		if (_s.WifiConectado)
		{
			if (!LicenseService.Instance.VerificarOuBloquearPro("Modo Wi-Fi de Alta Performance"))
			{
				return false;
			}
			if (scrcpyConfig.Bitrate <= 0 || scrcpyConfig.Bitrate > 10)
			{
				scrcpyConfig.Bitrate = 10;
			}
			if (scrcpyConfig.MaxSize <= 0 || scrcpyConfig.MaxSize > 1280)
			{
				scrcpyConfig.MaxSize = 1280;
			}
			if (scrcpyConfig.Fps <= 0 || scrcpyConfig.Fps > 90)
			{
				scrcpyConfig.Fps = 90;
			}
			if (scrcpyConfig.VideoBuffer <= 0)
			{
				scrcpyConfig.VideoBuffer = 15;
			}
			if (scrcpyConfig.Audio && scrcpyConfig.AudioBuffer <= 0)
			{
				scrcpyConfig.AudioBuffer = 50;
			}
		}
		else
		{
			// Modo USB Zero Delay: Reduz buffer de áudio para 15ms para eliminar o atraso artificial de exibição do vídeo
			if (scrcpyConfig.Audio && (scrcpyConfig.AudioBuffer <= 0 || scrcpyConfig.AudioBuffer > 20))
			{
				scrcpyConfig.AudioBuffer = 15;
			}
			scrcpyConfig.VideoBuffer = 0; // Exibição imediata sem buffer
		}

		if (pantallaVirtual)
		{
			scrcpyConfig.Audio = _s.Audio;
		}
		scrcpyConfig.PantallaVirtualDex = pantallaVirtual;
		if (pantallaVirtual)
		{
			scrcpyConfig.PantallaVirtualResolucion = vdResolucion;
			scrcpyConfig.PantallaVirtualDpi = ((_s.MapeadorVdDpi > 0) ? _s.MapeadorVdDpi : 240);
			scrcpyConfig.ModoDebug = false;
		}
		if (_s.Dlss5Modo)
		{
			DLSS5Service.EstablecerPreset(_s.Dlss5Preset ?? "Qualidade Ultra (2K/4K Sharp)");
			DLSS5Service.InstalarArchivosEnScrcpy();
		}
		else
		{
			DLSS5Service.DesactivarEnScrcpy();
		}
		return _s.Scrcpy.Lanzar(scrcpyConfig);
	}

	private static (int ancho, int alto) ParsearResolucionVd(string resolucion)
	{
		string[] array = (resolucion ?? "").Split('x');
		if (array.Length == 2 && int.TryParse(array[0], out var result) && result > 0 && int.TryParse(array[1], out var result2) && result2 > 0)
		{
			return (ancho: result, alto: result2);
		}
		return (ancho: 1920, alto: 1080);
	}

	private async Task<int> EsperarDisplayVirtualAsync(int timeoutMs)
	{
		for (int transcurrido = 0; transcurrido < timeoutMs; transcurrido += 200)
		{
			int ultimoDisplayVirtualId = _s.Scrcpy.UltimoDisplayVirtualId;
			if (ultimoDisplayVirtualId >= 0)
			{
				return ultimoDisplayVirtualId;
			}
			if (!_s.Scrcpy.EstaCorriendo)
			{
				return -1;
			}
			await Task.Delay(200);
		}
		return _s.Scrcpy.UltimoDisplayVirtualId;
	}

	private async Task LanzarJuegoEnDisplayVirtualAsync(string paquete, int displayId, int ancho, int alto)
	{
		if (string.IsNullOrWhiteSpace(paquete))
		{
			AppLogger.Warn("[VD-MAPPER] Sin paquete de juego; ábrelo desde el launcher de la ventana.");
			ToastService.Mostrar("No hay juego seleccionado. Ábrelo desde el launcher de la ventana.", ToastTipo.Advertencia, 4500);
			return;
		}
		(bool, string, string) obj = await _s.Adb.EjecutarShellAsync("cmd package resolve-activity --brief " + paquete);
		bool item = obj.Item1;
		string item2 = obj.Item2;
		string actividad = (item ? ((from l in item2.Split('\n')
			select l.Trim()).LastOrDefault((string l) => l.Contains('/')) ?? "") : "");
		if (string.IsNullOrEmpty(actividad))
		{
			AppLogger.Warn("[VD-MAPPER] No se resolvió la actividad de " + paquete + "; el juego debe abrirse desde el launcher.");
			ToastService.Mostrar("No se encontró el juego en el teléfono. Ábrelo desde el launcher de la ventana.", ToastTipo.Advertencia, 4500);
			return;
		}
		var (flag, _, value) = await _s.Adb.EjecutarShellAsync($"am start --display {displayId} -n {actividad}");
		if (!flag)
		{
			AppLogger.Warn($"[VD-MAPPER] am start en display {displayId} falló: {value}");
			ToastService.Mostrar("No se pudo abrir el juego automáticamente. Ábrelo desde el launcher de la ventana.", ToastTipo.Advertencia, 4500);
			return;
		}
		await Task.Delay(1500);
		await _s.Adb.EjecutarShellAsync($"input -d {displayId} tap {ancho / 2} {alto / 2}");
		AppLogger.Info($"[VD-MAPPER] {paquete} lanzado en display {displayId} ({actividad}) con tap de foco.");
	}
}







