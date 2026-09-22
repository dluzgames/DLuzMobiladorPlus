using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using DLuz.Mapper;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Threading;
using System.Windows.Media;
using DLuz.Helpers;
using DLuz.Services;
using Microsoft.Win32;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace DLuz.Views;

public partial class MapperSidePanel : Window, IComponentConnector
{
	private struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	private readonly MapeadorService _service;

	private readonly Func<nint> _hwndProvider;

	private readonly DispatcherTimer _timer;

	private readonly ContentDialogService _dialogos = new ContentDialogService();

	private RECT _ultima;

	private bool _actualizando;

	private bool _rapidoAbierto;

	private bool _perfilesCargados;

	private string _accionNombre = "";

	private string _rutaImportar = "";

	private bool _importarBlueStacks;

	private static readonly nint HWND_TOPMOST = new IntPtr(-1);

	private const uint SWP_NOMOVE = 2u;

	private const uint SWP_NOSIZE = 1u;

	private const uint SWP_NOACTIVATE = 16u;

	public MapperSidePanel(MapeadorService service, Func<nint> hwndProvider)
	{
		InitializeComponent();
		_service = service;
		_hwndProvider = hwndProvider;
		_dialogos.SetDialogHost(HostDialogos);
		BtnOverlays.Click += delegate
		{
			_service.AlternarControles();
			Actualizar();
		};
		BtnProbar.Click += delegate
		{
			_service.AlternarCaptura();
			Actualizar();
		};
		BtnFps.Click += delegate
		{
			_service.AlternarFpsModoDluzStacks();
			ToastService.Mostrar(_service.FpsModoDluzStacks ? "FPS ativado no Modo DLuzStacks." : "FPS ocultado no Modo DLuzStacks.", ToastTipo.Exito, 1800);
			Actualizar();
		};
		BtnDlss5.Click += delegate
		{
			_service.AlternarDlss5();
			Actualizar();
		};
		ToggleDlss5.Checked += delegate
		{
			if (!_actualizando && !_service.Dlss5Activo)
			{
				_service.AlternarDlss5();
				Actualizar();
			}
		};
		ToggleDlss5.Unchecked += delegate
		{
			if (!_actualizando && _service.Dlss5Activo)
			{
				_service.AlternarDlss5();
				Actualizar();
			}
		};
		ComboDlss5Preset.SelectionChanged += delegate
		{
			if (!_actualizando && ComboDlss5Preset.SelectedItem is ComboBoxItem item && item.Content is string presetTexto)
			{
				string preset = presetTexto.Split('(')[0].Trim();
				_service.EstablecerDlss5Preset(preset);
			}
		};
		BtnCapturaTela.Click += async delegate
		{
			(bool ok, string mensagem) = await _service.CapturarTelaDluzStacksAsync();
			ToastService.Mostrar(mensagem, ok ? ToastTipo.Exito : ToastTipo.Error, 4000);
		};
		BtnGravarTela.Click += async delegate
		{
			(bool ok, string mensagem) = await _service.AlternarGravacaoTelaDluzStacksAsync();
			ToastService.Mostrar(mensagem, ok ? ToastTipo.Exito : ToastTipo.Error, 4000);
			Actualizar();
		};
		BtnPantallaCompleta.Click += delegate
		{
			_service.AlternarPantallaCompleta();
		};
		BtnGirarTela.Click += async delegate
		{
			await _service.GirarTelaAsync(90);
		};
		BtnGirar90.Click += async delegate
		{
			await _service.GirarTelaAsync(90);
		};
		BtnGirar180.Click += async delegate
		{
			await _service.GirarTelaAsync(180);
		};
		BtnEditar.Click += delegate
		{
			if (_service.EditandoLayout)
			{
				_service.CerrarEditorLayout();
			}
			else
			{
				_rapidoAbierto = !_rapidoAbierto;
				Actualizar();
			}
		};
		BtnEditar.PreviewMouseRightButtonUp += delegate(object _, MouseButtonEventArgs e)
		{
			e.Handled = true;
			_rapidoAbierto = false;
			_service.AbrirEditorLayout();
		};
		BtnAbrirEditor.Click += delegate
		{
			_rapidoAbierto = false;
			_service.AbrirEditorLayout();
		};
		NumSensX.ValueChanged += delegate
		{
			AplicarCamara();
		};
		NumSensY.ValueChanged += delegate
		{
			AplicarCamara();
		};
		NumPuxadaCapa.ValueChanged += delegate
		{
			AplicarCamaraAvanzada();
		};
		NumAceleracao.ValueChanged += delegate
		{
			AplicarCamaraAvanzada();
		};
		NumSuavizacao.ValueChanged += delegate
		{
			AplicarCamaraAvanzada();
		};
		SliderPointerSpeed.ValueChanged += delegate
		{
			AtualizarTextoVelocidadeCursor();
		};
		BtnAplicarPointerSpeed.Click += async delegate
		{
			int velocidade = (int)Math.Round(SliderPointerSpeed.Value);
			(bool exito, string erro) = await _service.AplicarVelocidadeCursorAndroidAsync(velocidade);
			TxtPointerEstado.Text = exito ? $"✓ Aplicado no celular: {TextoVelocidadeCursor(velocidade)}" : "Não foi possível aplicar. Conecte o celular por ADB.";
			if (!exito && !string.IsNullOrWhiteSpace(erro))
			{
				AppLogger.Warn("Velocidade do cursor Android: " + erro);
			}
		};
		BtnRestaurarPointerSpeed.Click += async delegate
		{
			SliderPointerSpeed.Value = 0;
			(bool exito, string erro) = await _service.AplicarVelocidadeCursorAndroidAsync(0);
			TxtPointerEstado.Text = exito ? "✓ Restaurado para 0 (padrão) no celular." : "Não foi possível restaurar. Conecte o celular por ADB.";
			if (!exito && !string.IsNullOrWhiteSpace(erro))
			{
				AppLogger.Warn("Restauração da velocidade do cursor Android: " + erro);
			}
		};
		BtnDetener.Click += delegate
		{
			_service.DetenerSesion();
		};
		BtnAplicarPadraoFps.Click += delegate
		{
			_service.EditorAplicarPreConfigurados();
			ToastService.Mostrar("⚡ Controles padrão Free Fire / FPS aplicados na tela!", ToastTipo.Exito, 2500);
		};
		BtnAddWasd.Click += delegate
		{
			_service.EditorReposicionarWasd();
		};
		BtnAddCamera.Click += delegate
		{
			_service.EditorReposicionarCamera();
		};
		BtnAddAtirar.Click += delegate
		{
			_service.EditorAgregarAtirar();
		};
		BtnAddMira.Click += delegate
		{
			_service.EditorAgregarMira();
		};
		BtnAddBoton.Click += delegate
		{
			_service.EditorAgregarBoton();
		};
		BtnAddTap.Click += delegate
		{
			_service.EditorAgregarTap();
		};
		BtnAddRepetido.Click += delegate
		{
			_service.EditorAgregarRepetido();
		};
		BtnGuardarLayout.Click += delegate
		{
			_service.EditorGuardar();
		};
		BtnRestablecer.Click += async delegate
		{
			await _service.RestablecerPerfilAsync();
		};
		BtnCerrarEdicion.Click += delegate
		{
			if (_service.EditandoLayout)
			{
				_service.CerrarEditorLayout();
			}
			else
			{
				_rapidoAbierto = false;
				Actualizar();
			}
		};
		BtnExportar.Click += delegate
		{
			Exportar();
		};
		BtnImportar.Click += delegate
		{
			Importar();
		};
		BtnPerfilNuevo.Click += delegate
		{
			PedirNombre("nuevo", "");
		};
		BtnPerfilRenombrar.Click += delegate
		{
			PedirNombre("renombrar", _service.PerfilActivoNombre);
		};
		BtnPerfilEliminar.Click += async delegate
		{
			await EliminarPerfilAsync();
		};
		BtnConfirmarNombre.Click += delegate
		{
			ConfirmarNombre();
		};
		ComboPerfiles.SelectionChanged += delegate
		{
			if (!_actualizando && ComboPerfiles.SelectedItem is string text && !(text == _service.PerfilActivoNombre))
			{
				bool flag = _service.ActivarPerfil(text);
				ToastService.Mostrar(flag ? ("Perfil activo: " + text) : "No se pudo cambiar de perfil.", (!flag) ? ToastTipo.Error : ToastTipo.Exito, 2500);
				RefrescarPerfiles();
				Actualizar();
			}
		};
		_service.PropertyChanged += ServiceChanged;
		_timer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(350.0)
		};
		_timer.Tick += delegate
		{
			Ubicar();
		};
		base.Loaded += delegate
		{
			Actualizar();
			Ubicar();
			_timer.Start();
		};
		base.Closed += delegate
		{
			_timer.Stop();
			_service.PropertyChanged -= ServiceChanged;
		};
	}

	private void ServiceChanged(object? sender, PropertyChangedEventArgs e)
	{
		base.Dispatcher.BeginInvoke(new Action(Actualizar));
	}

	private void Actualizar()
	{
		_actualizando = true;
		NumSensX.Value = _service.SensibilidadCamaraX;
		NumSensY.Value = _service.SensibilidadCamaraY;
		NumPuxadaCapa.Value = _service.PuxadaCapaY;
		NumAceleracao.Value = _service.AceleracaoCamera;
		NumSuavizacao.Value = _service.SuavizacaoMira;
		SliderPointerSpeed.Value = _service.VelocidadeCursorAndroid;
		AtualizarTextoVelocidadeCursor();
		_actualizando = false;
		BtnOverlays.Icon = new SymbolIcon(_service.MostrandoControles ? SymbolRegular.EyeOff24 : SymbolRegular.Eye24);
		BtnOverlays.ToolTip = (_service.MostrandoControles ? "Ocultar controles" : "Mostrar controles");
		BtnProbar.Icon = new SymbolIcon(_service.Capturando ? SymbolRegular.Pause24 : SymbolRegular.Play24);
		_actualizando = false;
		BtnOverlays.Icon = new SymbolIcon(_service.MostrandoControles ? SymbolRegular.EyeOff24 : SymbolRegular.Eye24);
		BtnOverlays.ToolTip = (_service.MostrandoControles ? "Ocultar controles" : "Mostrar controles");
		BtnProbar.Icon = new SymbolIcon(_service.Capturando ? SymbolRegular.Pause24 : SymbolRegular.Play24);
		BtnProbar.ToolTip = (_service.Capturando ? "Suspender" : "Reanudar captura");
		BtnFps.ToolTip = _service.FpsModoDluzStacks ? "Ocultar FPS" : "Mostrar FPS";
		BtnFps.Opacity = _service.FpsModoDluzStacks ? 1.0 : 0.45;
		AtualizarEstadoDlss5();
		BtnGravarTela.ToolTip = _service.GravandoTela ? "Parar gravação e salvar" : "Iniciar gravação";
		BtnGravarTela.Appearance = _service.GravandoTela ? ControlAppearance.Danger : ControlAppearance.Secondary;
		bool editandoLayout = _service.EditandoLayout;
		if (editandoLayout)
		{
			_rapidoAbierto = false;
		}
		bool flag = editandoLayout || _rapidoAbierto;
		if (editandoLayout && !_perfilesCargados)
		{
			RefrescarPerfiles();
		}
		if (!editandoLayout)
		{
			_perfilesCargados = false;
		}
		PanelRapido.Visibility = (editandoLayout ? Visibility.Collapsed : Visibility.Visible);
		PanelEdicion.Visibility = ((!editandoLayout) ? Visibility.Collapsed : Visibility.Visible);
		PieEdicion.Visibility = ((!editandoLayout) ? Visibility.Collapsed : Visibility.Visible);
		Flyout.VerticalAlignment = (editandoLayout ? VerticalAlignment.Stretch : VerticalAlignment.Top);
		if (Flyout.Visibility == Visibility.Visible == flag)
		{
			return;
		}
		Flyout.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		_ultima = default(RECT);
		base.Dispatcher.BeginInvoke(new Action(Ubicar), DispatcherPriority.Loaded);
		if (!flag)
		{
			return;
		}
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			nint handle = new WindowInteropHelper(this).Handle;
			if (handle != IntPtr.Zero)
			{
				SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, 19u);
			}
		}, DispatcherPriority.Loaded);
	}

	private void AplicarCamara()
	{
		if (!_actualizando)
		{
			double sensibilidadX = NumSensX.Value ?? _service.SensibilidadCamaraX;
			double sensibilidadY = NumSensY.Value ?? _service.SensibilidadCamaraY;
			_service.EstablecerCamara(sensibilidadX, sensibilidadY);
		}
	}

	private void AplicarCamaraAvanzada()
	{
		if (!_actualizando)
		{
			_service.EstablecerCamaraAvanzada(NumPuxadaCapa.Value, NumAceleracao.Value, NumSuavizacao.Value);
		}
	}

	private bool MostrarDialogo(CommonDialog dlg)
	{
		_service.CederFrenteADialogo(ceder: true);
		try
		{
			return dlg.ShowDialog(this) == true;
		}
		finally
		{
			_service.CederFrenteADialogo(ceder: false);
		}
	}

	private void Exportar()
	{
		SaveFileDialog saveFileDialog = new SaveFileDialog
		{
			Title = "Exportar perfil de controles",
			FileName = _service.PerfilActivoNombre + ".cfg",
			Filter = "Configuração BlueStacks (*.cfg)|*.cfg|Perfil DLuzMObi (*.json)|*.json",
			FilterIndex = 1
		};
		if (MostrarDialogo(saveFileDialog))
		{
			bool blueStacks = string.Equals(Path.GetExtension(saveFileDialog.FileName), ".cfg", StringComparison.OrdinalIgnoreCase);
			bool flag = blueStacks ? _service.ExportarBlueStacks(saveFileDialog.FileName) : _service.ExportarPerfil(saveFileDialog.FileName);
			ToastService.Mostrar(flag ? (blueStacks ? "Configuração BlueStacks exportada." : "Perfil DLuzMObi exportado.") : "Não foi possível exportar o perfil.", (!flag) ? ToastTipo.Error : ToastTipo.Exito, 2500);
		}
	}

	private void Importar()
	{
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Title = "Importar perfil de controles",
			Filter = "Configuração BlueStacks (*.cfg)|*.cfg|Perfil DLuzMObi (*.json)|*.json|Todos os arquivos (*.*)|*.*",
			FilterIndex = 1
		};
		if (MostrarDialogo(openFileDialog))
		{
			_rutaImportar = openFileDialog.FileName;
			_importarBlueStacks = string.Equals(Path.GetExtension(_rutaImportar), ".cfg", StringComparison.OrdinalIgnoreCase);
			string nomeBase = _importarBlueStacks
				? BlueStacksConfigImporter.SugerirNomePerfil(_rutaImportar)
				: Path.GetFileNameWithoutExtension(_rutaImportar);
			string nome = NomeImportadoDisponivel(nomeBase);
			string mensagemBlueStacks = "";
			bool importado = _importarBlueStacks
				? _service.ImportarBlueStacksConNombre(_rutaImportar, nome, out mensagemBlueStacks)
				: _service.ImportarPerfilConNombre(_rutaImportar, nome);
			ToastService.Mostrar(importado
				? (_importarBlueStacks ? $"Perfil BlueStacks importado e ativado: {nome}. {mensagemBlueStacks}" : $"Perfil importado e ativado: {nome}.")
				: "Não foi possível importar o perfil. Verifique se o arquivo .cfg é válido.",
				importado ? ToastTipo.Exito : ToastTipo.Error, 4500);
			_rutaImportar = "";
			_importarBlueStacks = false;
			RefrescarPerfiles();
			if (importado)
			{
				Actualizar();
			}
		}
	}

	private void AtualizarTextoVelocidadeCursor()
	{
		if (TxtPointerValor == null || SliderPointerSpeed == null)
		{
			return;
		}
		TxtPointerValor.Text = TextoVelocidadeCursor((int)Math.Round(SliderPointerSpeed.Value));
	}

	private static string TextoVelocidadeCursor(int velocidade)
	{
		return velocidade == 0 ? "0 (padrão)" : velocidade.ToString("+0;-0");
	}

	private string NomeImportadoDisponivel(string baseNome)
	{
		baseNome = string.IsNullOrWhiteSpace(baseNome) ? "Perfil importado" : baseNome.Trim();
		HashSet<string> existentes = _service.ListarPerfiles().ToHashSet(StringComparer.OrdinalIgnoreCase);
		if (!existentes.Contains(baseNome))
		{
			return baseNome;
		}
		for (int i = 2; ; i++)
		{
			string candidato = $"{baseNome} {i}";
			if (!existentes.Contains(candidato))
			{
				return candidato;
			}
		}
	}

	private void PedirNombre(string accion, string inicial)
	{
		_accionNombre = accion;
		TxtNombrePerfil.Text = inicial;
		FilaNombrePerfil.Visibility = Visibility.Visible;
		TxtNombrePerfil.Focus();
		TxtNombrePerfil.SelectAll();
	}

	private void ConfirmarNombre()
	{
		string text = (TxtNombrePerfil.Text ?? "").Trim();
		FilaNombrePerfil.Visibility = Visibility.Collapsed;
		if (text.Length == 0)
		{
			_accionNombre = "";
			return;
		}
		string mensagemBlueStacks = "";
		bool flag = _accionNombre switch
		{
			"nuevo" => _service.CrearPerfil(text), 
			"renombrar" => _service.RenombrarPerfil(text), 
			"importar" => _importarBlueStacks ? _service.ImportarBlueStacksConNombre(_rutaImportar, text, out mensagemBlueStacks) : _service.ImportarPerfilConNombre(_rutaImportar, text), 
			_ => false, 
		};
		string mensaje;
		if (flag)
		{
			string accionNombre = _accionNombre;
			string text2 = ((accionNombre == "nuevo") ? ("Perfil criado: " + text) : ((!(accionNombre == "renombrar")) ? (_importarBlueStacks ? ("BlueStacks importado: " + text + ". " + mensagemBlueStacks) : ("Perfil importado: " + text)) : ("Perfil renomeado para " + text)));
			mensaje = text2;
		}
		else
		{
			mensaje = "No se pudo completar (¿nombre repetido o archivo inválido?).";
		}
		ToastService.Mostrar(mensaje, (!flag) ? ToastTipo.Error : ToastTipo.Exito);
		_accionNombre = "";
		_rutaImportar = "";
		_importarBlueStacks = false;
		RefrescarPerfiles();
		if (flag)
		{
			Actualizar();
		}
	}

	private async Task EliminarPerfilAsync()
	{
		object selectedItem = ComboPerfiles.SelectedItem;
		if (!(selectedItem is string { Length: not 0 } nombre))
		{
			return;
		}
		if (_service.ListarPerfiles().Count <= 1)
		{
			ToastService.Mostrar("Es el único perfil: crea otro antes de eliminarlo.", ToastTipo.Advertencia);
			return;
		}
		StackPanel stackPanel = new StackPanel
		{
			MaxWidth = 320.0
		};
		stackPanel.Children.Add(new System.Windows.Controls.TextBlock
		{
			Text = "Se eliminará el perfil «" + nombre + "» y su configuración de controles. Esta acción no se puede deshacer.",
			TextWrapping = TextWrapping.Wrap
		});
		if (await _dialogos.ShowAsync(new ContentDialog
		{
			Title = "Eliminar perfil",
			Content = stackPanel,
			PrimaryButtonText = "Eliminar",
			PrimaryButtonAppearance = ControlAppearance.Danger,
			CloseButtonText = "Cancelar"
		}, default(CancellationToken)) == ContentDialogResult.Primary)
		{
			bool flag = _service.EliminarPerfil(nombre);
			ToastService.Mostrar(flag ? ("Perfil eliminado: " + nombre) : "No se pudo eliminar el perfil.", (!flag) ? ToastTipo.Error : ToastTipo.Exito, 2500);
			RefrescarPerfiles();
			if (flag)
			{
				Actualizar();
			}
		}
	}

	private void RefrescarPerfiles()
	{
		_actualizando = true;
		List<string> list = _service.ListarPerfiles();
		ComboPerfiles.ItemsSource = list;
		ComboPerfiles.SelectedItem = (list.Contains(_service.PerfilActivoNombre) ? _service.PerfilActivoNombre : (list.Contains("LX Mapper") ? "LX Mapper" : list.FirstOrDefault()));
		_actualizando = false;
		_perfilesCargados = true;
	}

	public void CederFrente(bool ceder)
	{
		if (ceder)
		{
			_timer.Stop();
			base.Topmost = false;
			return;
		}
		base.Topmost = true;
		_ultima = default(RECT);
		Ubicar();
		_timer.Start();
	}

	private void Ubicar()
	{
		nint num = _hwndProvider();
		if (num != IntPtr.Zero && IsWindow(num) && GetWindowRect(num, out var lpRect) && (lpRect.Left != _ultima.Left || lpRect.Top != _ultima.Top || lpRect.Right != _ultima.Right || lpRect.Bottom != _ultima.Bottom))
		{
			_ultima = lpRect;
			double dpiScaleX = 1.0;
			double dpiScaleY = 1.0;
			try
			{
				uint dpi = GetDpiForWindow(num);
				if (dpi > 0)
				{
					dpiScaleX = (double)dpi / 96.0;
					dpiScaleY = (double)dpi / 96.0;
				}
			}
			catch
			{
				PresentationSource source = PresentationSource.FromVisual(this);
				if (source?.CompositionTarget != null)
				{
					dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
					dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
				}
			}
			if (dpiScaleX <= 0.0) dpiScaleX = 1.0;
			if (dpiScaleY <= 0.0) dpiScaleY = 1.0;

			Rect workArea = SystemParameters.WorkArea;
			double panelWidth = ((base.ActualWidth > 0.0) ? base.ActualWidth : 46.0);
			double gap = 4.0;

			double winLeftDip = lpRect.Left / dpiScaleX;
			double winTopDip = lpRect.Top / dpiScaleY;
			double winRightDip = lpRect.Right / dpiScaleX;
			double winBottomDip = lpRect.Bottom / dpiScaleY;
			double winHeightDip = winBottomDip - winTopDip;

			double maxPanelHeight = Math.Max(480.0, workArea.Height - 20.0);
			base.Height = Math.Clamp(winHeightDip, Math.Min(480.0, maxPanelHeight), maxPanelHeight);

			// Se a janela do jogo estiver colada na borda direita do monitor sem espaço para a barra externa,
			// redimensiona/move a janela do jogo ligeiramente para a esquerda para caber a barra lateral ao lado.
			if (winRightDip + panelWidth + gap > workArea.Right)
			{
				int pxEspacoNecessario = (int)Math.Ceiling((panelWidth + gap + 4.0) * dpiScaleX);
				int maxRightPx = (int)Math.Floor(workArea.Right * dpiScaleX) - pxEspacoNecessario;
				if (lpRect.Right > maxRightPx)
				{
					int shift = lpRect.Right - maxRightPx;
					int novoLeft = Math.Max((int)Math.Ceiling(workArea.Left * dpiScaleX), lpRect.Left - shift);
					int novoWidth = lpRect.Right - lpRect.Left;
					if (novoLeft + novoWidth > maxRightPx)
					{
						novoWidth = Math.Max((int)(480 * dpiScaleX), maxRightPx - novoLeft);
					}
					SetWindowPos(num, IntPtr.Zero, novoLeft, lpRect.Top, novoWidth, lpRect.Bottom - lpRect.Top, 0x0014u); // SWP_NOZORDER | SWP_NOACTIVATE
					GetWindowRect(num, out lpRect);
					_ultima = lpRect;
					winLeftDip = lpRect.Left / dpiScaleX;
					winTopDip = lpRect.Top / dpiScaleY;
					winRightDip = lpRect.Right / dpiScaleX;
					winBottomDip = lpRect.Bottom / dpiScaleY;
				}
			}

			// Posiciona a barra SEMPRE ao lado da janela do scrcpy (lado direito externo)
			double posX = winRightDip + gap;
			// Se ainda assim faltar espaço na direita, tenta colocar na esquerda externa
			if (posX + panelWidth > workArea.Right)
			{
				if (winLeftDip - panelWidth - gap >= workArea.Left)
				{
					posX = winLeftDip - panelWidth - gap;
				}
				else
				{
					posX = Math.Min(winRightDip + gap, workArea.Right - panelWidth);
				}
			}

			double posY = Math.Max(workArea.Top + 2.0, Math.Min(winTopDip, workArea.Bottom - base.Height - 2.0));
			base.Left = posX;
			base.Top = posY;
		}
	}

	private void BtnAjudaSensi_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
	{
		string msg = "🎯 GUIA DE SENSIBILIDADE AVANÇADA & CAPA\n\n" +
			"1. 🎯 Puxada de Capa (Eixo Y):\n" +
			"• O que faz: Multiplicador vertical exclusivo para quando você move o mouse para cima.\n" +
			"• No jogo: Faz a mira subir direto para a cabeça do adversário com muito menos esforço, sem alterar a sensibilidade lateral.\n" +
			"• Valores recomendados:\n" +
			"  - 1.0: Sensibilidade 1:1 natural (sem auxílio de puxada).\n" +
			"  - 1.30 a 1.40: (Recomendado) Padrão perfeito para capa controlado.\n" +
			"  - 1.80+: Puxada extrema (para quem joga de rush e quer capa no menor toque).\n\n" +
			"2. ⚡ Aceleração Câmera:\n" +
			"• O que faz: Ajusta a velocidade de giro baseada na rapidez do movimento da sua mão.\n" +
			"• No jogo: Movimento lento = mira calma e precisa (pixel a pixel). Movimento rápido = giro ágil de 180°/360°.\n" +
			"• Valores recomendados:\n" +
			"  - 0.0: Mira mecânica 1:1 sem aceleração (ideal para PUBG, CS e COD).\n" +
			"  - 0.6 a 1.1: Excelente para Free Fire, movimentação e gelo rápido.\n\n" +
			"3. 🛡️ Suavização (Anti-Tremor / Anti-Jitter):\n" +
			"• O que faz: Filtro que remove micro-tremores da mão e ruídos do sensor do mouse.\n" +
			"• No jogo: Deixa a mira cravada e firme sem tremidas, sem adicionar delay.\n" +
			"• Valores recomendados:\n" +
			"  - 0.0: Resposta crua do sensor.\n" +
			"  - 0.10 a 0.30: (Recomendado) Mira cravada sem perder tempo de resposta.\n\n" +
			"4. ⚡ Polling 1000Hz (1ms) Ativo:\n" +
			"• O que faz: Taxa de frequência de leitura do mouse por segundo.\n" +
			"• No jogo: Em 1000Hz, o computador lê o mouse 1.000 vezes por segundo (1ms) via Windows Raw Input, garantindo latência zero e resposta instantânea.";

		System.Windows.MessageBox.Show(msg, "🎯 Guia de Sensibilidade & Capa", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
	}

	private void AtualizarEstadoDlss5()
	{
		bool ativo = _service.Dlss5Activo;
		BtnDlss5.Foreground = ativo ? (Brush)new BrushConverter().ConvertFromString("#38EF7D")! : (Brush)new BrushConverter().ConvertFromString("#8E9AB8")!;
		BtnDlss5.Opacity = ativo ? 1.0 : 0.45;
		BtnDlss5.ToolTip = ativo ? "DLSS 5 AI Mode ATIVADO (1ms Ray Reconstruction)" : "DLSS 5 Desativado (Clique para Ativar)";
		if (TxtDlss5Estado != null)
		{
			TxtDlss5Estado.Text = ativo ? "🟢 ATIVO" : "⚪ DESATIVADO";
			TxtDlss5Estado.Foreground = ativo ? (Brush)new BrushConverter().ConvertFromString("#38EF7D")! : (Brush)new BrushConverter().ConvertFromString("#8E9AB8")!;
		}
		if (ToggleDlss5 != null && ToggleDlss5.IsChecked != ativo)
		{
			ToggleDlss5.IsChecked = ativo;
		}
	}

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

	[DllImport("user32.dll")]
	private static extern bool IsWindow(nint hWnd);

	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

	[DllImport("user32.dll")]
	private static extern uint GetDpiForWindow(nint hWnd);

}


