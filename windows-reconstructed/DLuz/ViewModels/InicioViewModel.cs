using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLuz.Services;
using DLuz.Views;
using Wpf.Ui.Controls;

namespace DLuz.ViewModels;

public class InicioViewModel : ObservableObject, IDisposable
{
	private readonly SessionState _s = SessionState.Instance;
	private readonly DispatcherTimer _timer;
	private Window? _flotante;
	private FpsOverlayWindow? _overlay;
	private bool _sincronizandoPerfil;
	private string? _perfilActivoNombre;

	private RelayCommand? administrarPerfilesCommand;
	private AsyncRelayCommand? reconectarAdbCommand;
	private RelayCommand? copiarComandoCommand;
	private AsyncRelayCommand? iniciarScrcpyCommand;
	private AsyncRelayCommand? detenerScrcpyCommand;
	private AsyncRelayCommand? autoConfigurarCommand;
	private RelayCommand? abrirTutoriaisCommand;

	public string EstadoTexto => _s.EstadoTexto;

	public Brush IndicadorBrush
	{
		get
		{
			if (!_s.InicializacionCompleta)
			{
				return Recurso("Stex.TextSecondaryBrush");
			}
			if (_s.HayDispositivo)
			{
				return Recurso("Stex.SuccessBrush");
			}
			if (_s.EstadoDetallado != EstadoDispositivo.NoAutorizado)
			{
				return Recurso("Stex.ErrorBrush");
			}
			return Recurso("Stex.WarningBrush");
		}
	}

	public ControlAppearance ReconectarAppearance
	{
		get
		{
			if (_s.EstadoDetallado != EstadoDispositivo.NoAutorizado)
			{
				return ControlAppearance.Secondary;
			}
			return ControlAppearance.Caution;
		}
	}

	public bool EstaCorriendo => _s.Scrcpy.EstaCorriendo;

	public bool PuedeIniciar
	{
		get
		{
			if (_s.InicializacionCompleta && !EstaCorriendo && !_s.MapeadorActivo)
			{
				if (!_s.HayDispositivo && !_s.ModoOtg)
				{
					return _s.UsarWifi;
				}
				return true;
			}
			return false;
		}
	}

	public string BotonIniciarTexto
	{
		get
		{
			if (_s.InicializacionCompleta)
			{
				if (!_s.MapeadorActivo)
				{
					if (!PuedeIniciar && !EstaCorriendo)
					{
						if (_s.EstadoDetallado != EstadoDispositivo.NoAutorizado)
						{
							return "Sem aparelho";
						}
						return "Falta autorizar depuração";
					}
					return "INICIAR ESPELHAMENTO";
				}
				return "Sessão do mapeador ativa";
			}
			return "Detectando aparelho...";
		}
	}

	public Brush IniciarFill => PuedeIniciar ? Recurso("Stex.AccentLightBrush") : Recurso("Stex.BtnDisabledBrush");
	public Brush IniciarFore => PuedeIniciar ? Recurso("Stex.TextPrimaryBrush") : Recurso("Stex.TextDimmerBrush");
	public Brush DetenerFill => EstaCorriendo ? Recurso("Stex.BtnDangerBrush") : Recurso("Stex.BtnDisabledBrush");
	public Brush DetenerFore => EstaCorriendo ? Recurso("Stex.TextPrimaryBrush") : Recurso("Stex.TextDimmerBrush");

	public bool CompatibilidadForzada => ArquitecturaHelper.CompatibilidadForzada;
	public bool CompatibilidadEditable => !ArquitecturaHelper.CompatibilidadForzada;

	public string ModoCompatibilidadDesc => !ArquitecturaHelper.CompatibilidadForzada
		? "Usa binários compatíveis com 32 bits. Pode ajudar em computadores modestos ou com problemas de compatibilidade. Será aplicado na próxima inicialização."
		: "Activado automáticamente en sistemas de 32 bits.";

	public string ModoCompatibilidadEstado => !ArquitecturaHelper.CompatibilidadForzada ? "" : "Forzada (32 bits)";

	public bool ModoCompatibilidad
	{
		get => ArquitecturaHelper.ModoCompatibilidad;
		set
		{
			if (ArquitecturaHelper.CompatibilidadForzada || ArquitecturaHelper.ModoCompatibilidad == value)
			{
				OnPropertyChanged("ModoCompatibilidad");
				return;
			}
			ArquitecturaHelper.ModoCompatibilidad = value;
			_s.GuardarConfig();
			OnPropertyChanged("ModoCompatibilidad");
			OnPropertyChanged("ComandoPreview");
			OnPropertyChanged("HayCambios");
			ToastService.Mostrar("Compatibilidade x86 " + (value ? "ativada" : "desativada") + "\nSerá aplicada na próxima inicialização do espelhamento.");
		}
	}

	public bool DualHabilitado => !_s.PantallaVirtualDex;
	public bool DualBloqueadoAvisoVisible => _s.PantallaVirtualDex;

	public bool ModoDualExperimental
	{
		get => _s.ModoDualExperimental;
		set
		{
			if (_s.ModoDualExperimental != value)
			{
				_s.ModoDualExperimental = value;
				_s.GuardarConfig();
				OnPropertyChanged("ModoDualExperimental");
				OnPropertyChanged("ComandoPreview");
				OnPropertyChanged("ChipModo");
				OnPropertyChanged("HayCambios");
				ToastService.Mostrar("Modo duplo experimental " + (value ? "ativado" : "desativado") + "\nSerá aplicado na próxima inicialização do espelhamento.");
			}
		}
	}

	public string ComandoPreview
	{
		get
		{
			string text = (ArquitecturaHelper.ModoCompatibilidad ? "x86" : "x86_64");
			ScrcpyConfig scrcpyConfig = _s.ObtenerConfigActual();
			if (!_s.ModoDualExperimental || scrcpyConfig.ModoOtg)
			{
				return "scrcpy.exe (" + text + ") " + ScrcpyManager.ConstruirArgumentos(scrcpyConfig);
			}
			Collection<string> collection = new Collection<string>();
			if (ScrcpyManager.UsaInstanciaVisualExperimental(scrcpyConfig))
			{
				collection.Add("scrcpy.exe (" + text + ") visual " + ScrcpyManager.ConstruirArgumentosVisualesExperimental(scrcpyConfig, ""));
			}
			if (ScrcpyManager.UsaInstanciaEntradaExperimental(scrcpyConfig))
			{
				collection.Add("scrcpy.exe (" + text + ") entrada " + ScrcpyManager.ConstruirArgumentosEntradaExperimental(scrcpyConfig, ""));
			}
			return string.Join("\n", collection);
		}
	}

	public bool HayCambios => _s.HayCambiosSinGuardar;

	public string ChipPerfil => !string.IsNullOrEmpty(_s.PerfilSeleccionado) ? _s.PerfilSeleccionado : "—";
	public string ChipFps => _s.Fps.ToString();
	public string ChipMb => _s.Bitrate.ToString();
	public string ChipCodec => _s.VideoCodec;

	public string ChipModo
	{
		get
		{
			if (!_s.ModoDualExperimental || _s.ModoOtg)
			{
				if (!_s.ModoOtg)
				{
					if (!_s.WifiConectado)
					{
						return (_s.Video || _s.Audio) ? "USB" : "Control Only";
					}
					return "WiFi";
				}
				return "OTG";
			}
			return "Dual";
		}
	}

	public ObservableCollection<string> PerfilesDisponibles { get; } = new ObservableCollection<string>();

	public string? PerfilActivoNombre
	{
		get => _perfilActivoNombre;
		set
		{
			if (!(_perfilActivoNombre == value))
			{
				_perfilActivoNombre = value;
				OnPropertyChanged("PerfilActivoNombre");
				if (!_sincronizandoPerfil && !string.IsNullOrEmpty(value) && value != _s.PerfilSeleccionado)
				{
					AplicarPerfilAsync(value);
				}
			}
		}
	}

	public IRelayCommand AdministrarPerfilesCommand => administrarPerfilesCommand ??= new RelayCommand(AdministrarPerfiles);
	public IAsyncRelayCommand ReconectarAdbCommand => reconectarAdbCommand ??= new AsyncRelayCommand(ReconectarAdbAsync);
	public IRelayCommand CopiarComandoCommand => copiarComandoCommand ??= new RelayCommand(CopiarComando);
	public IAsyncRelayCommand IniciarScrcpyCommand => iniciarScrcpyCommand ??= new AsyncRelayCommand(IniciarScrcpyAsync);
	public IAsyncRelayCommand DetenerScrcpyCommand => detenerScrcpyCommand ??= new AsyncRelayCommand(DetenerScrcpyAsync);
	public IAsyncRelayCommand AutoConfigurarCommand => autoConfigurarCommand ??= new AsyncRelayCommand(AutoConfigurarAsync);
	public IRelayCommand AbrirTutoriaisCommand => abrirTutoriaisCommand ??= new RelayCommand(AbrirTutoriais);

	private void CerrarOverlay()
	{
		FpsOverlayWindow ov = _overlay;
		_overlay = null;
		if (ov != null)
		{
			SessionState.Marshal(delegate
			{
				ov.Cerrar();
			});
		}
	}

	public InicioViewModel()
	{
		_s.PropertyChanged += SessionChanged;
		RefrescarPerfiles();
		_timer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(500.0)
		};
		_timer.Tick += delegate
		{
			RefrescarEstado();
		};
		_timer.Start();
	}

	private void SessionChanged(object? sender, PropertyChangedEventArgs e)
	{
		RefrescarEstado();
	}

	public void Dispose()
	{
		_timer.Stop();
		_s.PropertyChanged -= SessionChanged;
	}

	public void RefrescarPerfiles()
	{
		PerfilesDisponibles.Clear();
		foreach (string item in _s.Perfiles.ListarPerfiles())
		{
			PerfilesDisponibles.Add(item);
		}
		SincronizarSeleccion(_s.PerfilSeleccionado);
	}

	private void SincronizarSeleccion(string? activo)
	{
		string text = ((!string.IsNullOrEmpty(activo) && PerfilesDisponibles.Contains(activo)) ? activo : ((PerfilesDisponibles.Count > 0) ? PerfilesDisponibles[0] : null));
		if (!(text == _perfilActivoNombre))
		{
			_sincronizandoPerfil = true;
			_perfilActivoNombre = text;
			OnPropertyChanged("PerfilActivoNombre");
			_sincronizandoPerfil = false;
		}
	}

	private async Task AplicarPerfilAsync(string nombre)
	{
		bool descartarConfig = false;
		if (_s.HayCambiosPerfil)
		{
			switch (await DialogService.TresOpcionesAsync("Alterações não salvas", "Há alterações que ainda não foram salvas. O que deseja fazer antes de aplicar o perfil '" + nombre + "'?", "Salvar e aplicar", "Aplicar sem salvar"))
			{
			case DialogService.TresOpciones.Cancelar:
				SincronizarSeleccion(_s.PerfilSeleccionado);
				return;
			case DialogService.TresOpciones.Primaria:
				_s.GuardarSecciones();
				break;
			default:
				descartarConfig = _s.HayCambiosConfig;
				break;
			}
		}
		ScrcpyConfig scrcpyConfig = _s.Perfiles.ObtenerPerfil(nombre);
		if (scrcpyConfig == null)
		{
			SincronizarSeleccion(_s.PerfilSeleccionado);
			return;
		}
		if (descartarConfig)
		{
			_s.RevertirCambiosConfig();
		}
		_s.CargarPerfilEnApp(scrcpyConfig);
		_s.PerfilSeleccionado = nombre;
		_s.GuardarConfig();
		SincronizarSeleccion(nombre);
		ToastService.Mostrar("Perfil '" + nombre + "' aplicado.", ToastTipo.Exito);
	}

	[RelayCommand]
	private void AdministrarPerfiles()
	{
		if (Application.Current.MainWindow is MainWindow mainWindow)
		{
			mainWindow.RootNav.Navigate(typeof(PerfilesPage));
		}
	}

	[RelayCommand]
	private async Task ReconectarAdbAsync()
	{
		await _s.Adb.ReiniciarServidorAsync();
		await _s.ActualizarEstadoAsync(mostrarToast: true);
		RefrescarEstado();
	}

	[RelayCommand]
	private void CopiarComando()
	{
		try
		{
			Clipboard.SetText(ComandoPreview);
			ToastService.Mostrar("Comando copiado al portapapeles", ToastTipo.Exito, 2000);
		}
		catch
		{
			ToastService.Mostrar("No se pudo copiar el comando", ToastTipo.Error);
		}
	}

	[RelayCommand]
	private async Task IniciarScrcpyAsync()
	{
		if (!LicenseService.Instance.VerificarOuBloquearPro("Modo Espelhar / GG Mouse"))
		{
			return;
		}
		try
		{
			if (_s.HayCambiosPerfil)
			{
				switch (await DialogService.TresOpcionesAsync("Alterações não salvas", "Há alterações não salvas na configuração. O que deseja fazer?", "Salvar e iniciar", "Iniciar sem salvar"))
				{
				case DialogService.TresOpciones.Cancelar:
					return;
				case DialogService.TresOpciones.Primaria:
					_s.GuardarSecciones();
					break;
				default:
					if (_s.HayCambiosConfig)
					{
						_s.RevertirCambiosConfig();
					}
					break;
				}
			}
			if (!_s.ModoOtg)
			{
				var (flag, list, _) = await Task.Run(() => _s.Adb.ListarDispositivos());
				if (!flag || list.Count == 0)
				{
					await DialogService.AdvertenciaAsync("Dispositivo no detectado", "Verifica que:\n• El teléfono esté conectado por USB o WiFi\n• La depuración USB esté habilitada\n• ADB reconozca el dispositivo (botón Reconectar)");
					return;
				}
			}
			else if (string.IsNullOrWhiteSpace(_s.OtgSerial))
			{
				List<string> item = (await Task.Run(() => _s.Adb.ListarDispositivos())).Item2;
				if (item.Count == 1)
				{
					_s.OtgSerial = item[0];
					_s.GuardarConfig();
				}
				else if (item.Count > 1)
				{
					await DialogService.AdvertenciaAsync("Varios dispositivos detectados", "Ve a Conexión → Modo OTG → Detectar Dispositivos\ny selecciona el serial del dispositivo a usar.");
					return;
				}
			}
			ScrcpyConfig config = _s.ObtenerConfigActual();
			bool dual = _s.ModoDualExperimental;
			DualLayout layoutDual = null;
			if (dual && !config.ModoOtg)
			{
				Window mainWindow = Application.Current.MainWindow;
				nint hwndReferencia = ((mainWindow != null) ? new WindowInteropHelper(mainWindow).Handle : IntPtr.Zero);
				layoutDual = DualLayoutHelper.CalcularDesdeVentana(hwndReferencia);
			}
			if (!(await Task.Run(() => _s.Scrcpy.Lanzar(config, dual, layoutDual))))
			{
				if (!dual || config.ModoOtg)
				{
					await DialogService.ErrorAsync("Erro ao iniciar", "Não foi possível iniciar o espelhamento.\n\nReconecte o ADB e tente novamente.");
				}
				return;
			}
			_s.ScrcpyEstabaActivo = true;
			_s.UltimaSesionWifi = _s.WifiConectado;
			_s.UltimaSesionOtg = _s.ModoOtg;
			_s.GuardarConfig();
			RefrescarEstado();
			Window win = Application.Current.MainWindow;
			if (win != null)
			{
				win.WindowState = WindowState.Minimized;
			}
			if (_s.ModoOtg)
			{
				Task.Run(async delegate
				{
					await Task.Delay(3000);
					if (!_s.Scrcpy.EstaCorriendo)
					{
						SessionState.Marshal(delegate
						{
							if (win != null)
							{
								win.Show();
								win.WindowState = WindowState.Normal;
								win.Activate();
							}
							DialogService.AdvertenciaAsync("OTG no compatible", "OTG no pudo iniciarse. Verifica que tu cable soporte modo OTG y que el dispositivo sea compatible.\n\nSi usas cable normal, intenta desactivar e reactivar la depuración USB antes de lanzar.");
						});
					}
				});
			}
			string value = (_s.ModoOtg ? "OTG" : (_s.WifiConectado ? "WiFi" : ((!_s.Video && !_s.Audio) ? "Control Only" : "USB")));
			string infoText = $"{_s.PerfilSeleccionado}   {_s.Fps} FPS   {_s.Bitrate} Mb   {value}";
			if (_s.MostrarFlotante)
			{
				Action onDetener = delegate
				{
					Task.Run(delegate
					{
						_s.Scrcpy.Detener();
						_s.ScrcpyEstabaActivo = false;
						CerrarOverlay();
						if (_s.Adb.HayDispositivoConectado())
						{
							if (_s.WmSizeActivo)
							{
								_s.Adb.ResetearResolucion();
							}
							if (_s.ResAdbActiva)
							{
								_s.Adb.ResetearResolucion();
							}
						}
						SessionState.Marshal(delegate
						{
							_flotante = null;
							if (win != null)
							{
								win.WindowState = WindowState.Normal;
								win.Activate();
							}
							RefrescarEstado();
						});
					});
				};
				Action onMostrarApp = delegate
				{
					SessionState.Marshal(delegate
					{
						if (win != null)
						{
							win.WindowState = WindowState.Normal;
							win.Activate();
						}
					});
				};
				_flotante = (_s.ModoOtg ? ((Window)new FloatingWindowOtg(_s.Scrcpy, _s.OtgSerial, _s.ShortcutMod, onDetener, onMostrarApp)) : ((Window)new FloatingWindow(_s.Scrcpy, infoText, onDetener, onMostrarApp, _s.PrintFps, _s.ModoDebug)));
				_flotante.Show();
			}
			else
			{
				Task.Run(async delegate
				{
					while (_s.Scrcpy.EstaCorriendo)
					{
						await Task.Delay(500);
					}
					_s.ScrcpyEstabaActivo = false;
					CerrarOverlay();
					SessionState.Marshal(delegate
					{
						if (win != null)
						{
							win.WindowState = WindowState.Normal;
							win.Activate();
						}
						RefrescarEstado();
					});
				});
			}
			if (_s.OverlayFps)
			{
				_overlay = new FpsOverlayWindow(_s.Scrcpy, _s.OverlayEsquina);
				_overlay.Show();
			}
		}
		catch (Exception ex)
		{
			await DialogService.ErrorAsync("Error inesperado", "Error inesperado al iniciar:\n" + ex.Message);
			RefrescarEstado();
		}
	}

	[RelayCommand]
	private async Task DetenerScrcpyAsync()
	{
		await _s.Scrcpy.DetenerAsync();
		_s.ScrcpyEstabaActivo = false;
		CerrarOverlay();
		if (_s.Adb.HayDispositivoConectado())
		{
			if (_s.WmSizeActivo)
			{
				_s.Adb.ResetearResolucionAsync();
			}
			if (_s.ResAdbActiva)
			{
				_s.Adb.ResetearResolucionAsync();
			}
		}
		RefrescarEstado();
	}

	private void RefrescarEstado()
	{
		OnPropertyChanged("EstadoTexto");
		OnPropertyChanged("IndicadorBrush");
		OnPropertyChanged("ReconectarAppearance");
		OnPropertyChanged("EstaCorriendo");
		OnPropertyChanged("PuedeIniciar");
		OnPropertyChanged("BotonIniciarTexto");
		OnPropertyChanged("IniciarFill");
		OnPropertyChanged("IniciarFore");
		OnPropertyChanged("DetenerFill");
		OnPropertyChanged("DetenerFore");
		OnPropertyChanged("ModoDualExperimental");
		OnPropertyChanged("DualHabilitado");
		OnPropertyChanged("DualBloqueadoAvisoVisible");
		OnPropertyChanged("ComandoPreview");
		OnPropertyChanged("HayCambios");
		OnPropertyChanged("ChipPerfil");
		OnPropertyChanged("ChipFps");
		OnPropertyChanged("ChipMb");
		OnPropertyChanged("ChipCodec");
		OnPropertyChanged("ChipModo");
		SincronizarSeleccion(_s.PerfilSeleccionado);
	}

	[RelayCommand]
	private async Task AutoConfigurarAsync()
	{
		ToastService.Mostrar("⚡ Analisando celular e calibrando melhor configuração...", ToastTipo.Info, 2500);
		var resultado = await AutoConfigService.ExecutarAutoConfigAsync();
		if (resultado.Sucesso)
		{
			RefrescarEstado();
			string detalhes = string.Join("\n• ", resultado.OtimizacoesAplicadas);
			await DialogService.ExitoAsync("⚡ Auto Configuração Concluída!", 
				$"Aparelho: {resultado.DispositivoNome}\n" +
				$"Processador: {resultado.Chipset}\n" +
				$"Memória RAM: {resultado.RamTotalGb} GB\n" +
				$"Display: {resultado.ResolucaoTela} @ {resultado.RefreshRateHz}Hz\n\n" +
				$"Configuração Aplicada:\n• {detalhes}\n\n" +
				"✓ Você já pode iniciar com o melhor desempenho possível! (Pode alterar manualmente quando quiser).");
			
			ToastService.Mostrar($"✓ {resultado.DispositivoNome} configurado para {resultado.FpsConfigurado} FPS Zero Lag!", ToastTipo.Exito, 4000);
		}
		else
		{
			await DialogService.AdvertenciaAsync("Auto Configuração", resultado.MensagemResumo);
		}
	}

	[RelayCommand]
	private void AbrirTutoriais()
	{
		Application.Current?.Dispatcher?.Invoke(() =>
		{
			if (Application.Current.MainWindow is MainWindow win)
			{
				win.NavegarPara(typeof(TutoriaisPage));
			}
		});
	}

	private static Brush Recurso(string clave)
	{
		return DLuz.Helpers.ResourceHelper.GetBrush(clave);
	}
}
