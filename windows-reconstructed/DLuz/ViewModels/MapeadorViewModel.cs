using DLuz.Helpers;
using System.Collections.Generic;
using System;
using System.CodeDom.Compiler;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLuz.Mapper;
using DLuz.Services;

namespace DLuz.ViewModels;

public class MapeadorViewModel : ObservableObject, IDisposable
{
	private readonly SessionState _s = SessionState.Instance;

	private readonly MapeadorService _svc;

	private readonly DispatcherTimer _timer;

	private bool _conectando;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? principalCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? alternarCapturaCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? editarLayoutCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? mostrarTeclasCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? guardarPerfilCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? restablecerPerfilCommand;

	public ObservableCollection<ControlChip> Controles { get; } = new ObservableCollection<ControlChip>();

	public bool SesionActiva => _svc.SesionActiva;

	public bool Capturando => _svc.Capturando;

	public bool Editando => _svc.EditandoLayout;

	public bool HayDispositivo
	{
		get
		{
			if (!_s.HayDispositivo)
			{
				return _s.WifiConectado;
			}
			return true;
		}
	}

	public bool PuedeIniciar
	{
		get
		{
			if (!_svc.SesionActiva && !_conectando && !_s.Scrcpy.EstaCorriendo)
			{
				return HayDispositivo;
			}
			return false;
		}
	}

	public bool PuedePrincipal
	{
		get
		{
			if (!SesionActiva)
			{
				return PuedeIniciar;
			}
			return true;
		}
	}

	public string PrincipalTexto
	{
		get
		{
			if (!_conectando)
			{
				if (!SesionActiva)
				{
					return "Iniciar";
				}
				return "Parar";
			}
			return "Conectando…";
		}
	}

	public Brush PrincipalFill
	{
		get
		{
			if (PuedePrincipal)
			{
				if (!SesionActiva)
				{
					return Recurso("Stex.AccentLightBrush");
				}
				return Recurso("Stex.BtnDangerBrush");
			}
			return Recurso("Stex.BtnDisabledBrush");
		}
	}

	public Brush PrincipalFore
	{
		get
		{
			if (!PuedePrincipal)
			{
				return Recurso("Stex.TextDimmerBrush");
			}
			return Recurso("Stex.TextPrimaryBrush");
		}
	}

	public bool PuedeAcciones => _svc.SesionActiva;

	public bool PuedeEditarLayout => _svc.SesionActiva;

	public string EstadoSesionTexto
	{
		get
		{
			if (!_conectando)
			{
				if (HayDispositivo)
				{
					if (_svc.SesionActiva)
					{
						if (!_svc.EditandoLayout)
						{
							if (!_svc.Capturando)
							{
								return "Pronto para jogar; pressione " + KeyNames.NombreBonito(_svc.TeclaCaptura) + " ou Play para começar";
							}
							return $"Jogando ({KeyNames.NombreBonito(_svc.TeclaCaptura)} pausa, {KeyNames.NombreBonito(_svc.TeclaLibre)} libera o cursor)";
						}
						return "Editando controles (HID desativado)";
					}
					return "Pronto para iniciar";
				}
				return "Nenhum aparelho";
			}
			return "Conectando…";
		}
	}

	public Brush IndicadorBrush
	{
		get
		{
			if (!_conectando)
			{
				if (HayDispositivo)
				{
					if (_svc.SesionActiva)
					{
						if (!_svc.Capturando)
						{
							return Recurso("Stex.WarningBrush");
						}
						return Recurso("Stex.SuccessBrush");
					}
					return Recurso("Stex.TextSecondaryBrush");
				}
				return Recurso("Stex.ErrorBrush");
			}
			return Recurso("Stex.WarningBrush");
		}
	}

	public string ResolucionTexto
	{
		get
		{
			if (!_svc.SesionActiva || _svc.ResAncho <= 0)
			{
				return "";
			}
			return $"{_svc.ResAncho} × {_svc.ResAlto}";
		}
	}

	public bool SinDispositivoVisible
	{
		get
		{
			if (!_s.HayDispositivo)
			{
				return !_s.WifiConectado;
			}
			return false;
		}
	}

	public bool EspejoNormalVisible
	{
		get
		{
			if (_s.Scrcpy.EstaCorriendo)
			{
				return !_svc.SesionActiva;
			}
			return false;
		}
	}

	public string TextoToggleCaptura
	{
		get
		{
			if (!_svc.Capturando)
			{
				return "Jugar (" + KeyNames.NombreBonito(_svc.TeclaCaptura) + ")";
			}
			return "Pausar (" + KeyNames.NombreBonito(_svc.TeclaCaptura) + ")";
		}
	}

	public JuegoVdOpcion[] JuegosVd { get; } = new JuegoVdOpcion[3]
	{
		new JuegoVdOpcion("Free Fire MAX", "com.dts.freefiremax"),
		new JuegoVdOpcion("Free Fire", "com.dts.freefireth"),
		new JuegoVdOpcion("Personalizado…", "personalizado")
	};

	public ObservableCollection<string> PerfilesDisponibles { get; } = new ObservableCollection<string>();

	public string? PerfilActivoNombre
	{
		get => _s.PerfilSeleccionado;
		set
		{
			if (!string.IsNullOrEmpty(value) && value != _s.PerfilSeleccionado)
			{
				AplicarPerfilRapido(value);
			}
		}
	}

	public void RefrescarPerfiles()
	{
		PerfilesDisponibles.Clear();
		foreach (string item in _s.Perfiles.ListarPerfiles())
		{
			PerfilesDisponibles.Add(item);
		}
		NotificarDetalhesUI();
	}

	public void NotificarDetalhesUI()
	{
		Application.Current?.Dispatcher?.InvokeAsync(() =>
		{
			try
			{
				var listaAtual = _s.Perfiles.ListarPerfiles();
				if (PerfilesDisponibles.Count != listaAtual.Count || !PerfilesDisponibles.SequenceEqual(listaAtual))
				{
					PerfilesDisponibles.Clear();
					foreach (var p in listaAtual) PerfilesDisponibles.Add(p);
				}
			}
			catch {}

			OnPropertyChanged(nameof(PerfilActivoNombre));
			OnPropertyChanged(nameof(PerfilResumoTexto));
			OnPropertyChanged(nameof(PerfilFpsTexto));
			OnPropertyChanged(nameof(PerfilResolucaoTexto));
			OnPropertyChanged(nameof(PerfilBitrateTexto));
			OnPropertyChanged(nameof(PerfilBufferTexto));
			OnPropertyChanged(nameof(PerfilCodecTexto));
			OnPropertyChanged(nameof(EncoderActivoTexto));
			OnPropertyChanged(nameof(EncoderChipNome));
			OnPropertyChanged(nameof(EncoderCodigoTexto));
			OnPropertyChanged(nameof(EncoderStatusBadge));
			OnPropertyChanged(nameof(PresetDetalhesTexto));
			OnPropertyChanged(nameof(AudioAtivo));
		});
	}

	private void AplicarPerfilRapido(string nombre)
	{
		try
		{
			ScrcpyConfig cfg = _s.Perfiles.ObtenerPerfil(nombre);
			if (cfg != null)
			{
				_s.CargarPerfilEnApp(cfg);
				_s.PerfilSeleccionado = nombre;
				_s.GuardarConfig();
				NotificarDetalhesUI();
				ToastService.Mostrar($"Perfil '{nombre}' aplicado com sucesso!", ToastTipo.Exito);
			}
		}
		catch (Exception ex)
		{
			AppLogger.Error("Falha ao aplicar perfil rápido", ex);
		}
	}

	public string PerfilResumoTexto
	{
		get
		{
			string res = _s.MaxSize > 0 ? $"{_s.MaxSize}p" : "Nativa";
			return $"{res} • {_s.Fps} FPS • {_s.Bitrate} Mbps • Buffer {_s.VideoBuffer}ms";
		}
	}

	public string PerfilFpsTexto => $"{_s.Fps} FPS";
	public string PerfilResolucaoTexto => _s.MaxSize > 0 ? $"{_s.MaxSize}p" : "Nativa (100%)";
	public string PerfilBitrateTexto => $"{_s.Bitrate} Mbps";
	public string PerfilBufferTexto => _s.VideoBuffer == 0 ? "0 ms (Zero Delay)" : $"{_s.VideoBuffer} ms";
	public string PerfilCodecTexto => string.IsNullOrEmpty(_s.VideoCodec) ? "H.264" : _s.VideoCodec.ToUpperInvariant();

	private RelayCommand? _selecionarCompetitivoCmd;
	public RelayCommand SelecionarCompetitivoCommand => _selecionarCompetitivoCmd ??= new RelayCommand(() => AplicarPerfilRapido("Competitivo USB"));

	private RelayCommand? _selecionarEquilibradoCmd;
	public RelayCommand SelecionarEquilibradoCommand => _selecionarEquilibradoCmd ??= new RelayCommand(() => AplicarPerfilRapido("Equilibrado USB"));

	private RelayCommand? _selecionarQualidadeCmd;
	public RelayCommand SelecionarQualidadeCommand => _selecionarQualidadeCmd ??= new RelayCommand(() => AplicarPerfilRapido("Qualidade USB"));

	public string EncoderChipNome
	{
		get
		{
			if (!_s.UseAdvancedEncoder || string.IsNullOrEmpty(_s.VideoEncoder))
			{
				return "Padrão Android (H.264)";
			}
			return ScrcpyManager.InferirProcessador(_s.VideoEncoder);
		}
	}

	public string EncoderCodigoTexto
	{
		get
		{
			if (!_s.UseAdvancedEncoder || string.IsNullOrEmpty(_s.VideoEncoder))
			{
				return "Automático pelo sistema";
			}
			return _s.VideoEncoder;
		}
	}

	public string EncoderStatusBadge
	{
		get
		{
			if (!_s.UseAdvancedEncoder || string.IsNullOrEmpty(_s.VideoEncoder))
			{
				return "ℹ️ Codec Padrão";
			}
			if (_s.VideoEncoder.Contains("google", StringComparison.OrdinalIgnoreCase) || _s.VideoEncoder.Contains("sw", StringComparison.OrdinalIgnoreCase))
			{
				return "⚠️ Modo Software / CPU";
			}
			return "⚡ Hardware Ativo (Zero Delay)";
		}
	}

	public string EncoderActivoTexto
	{
		get
		{
			if (!_s.UseAdvancedEncoder || string.IsNullOrEmpty(_s.VideoEncoder))
			{
				return "Padrão H.264";
			}
			string proc = ScrcpyManager.InferirProcessador(_s.VideoEncoder);
			return $"{proc} ({_s.VideoEncoder})";
		}
	}

	private AsyncRelayCommand? _detectarEncoderCmd;
	public IAsyncRelayCommand DetectarEncoderCommand => _detectarEncoderCmd ??= new AsyncRelayCommand(DetectarEncoderAsync);

	private async Task DetectarEncoderAsync()
	{
		if (!HayDispositivo)
		{
			ToastService.Mostrar("Conecte um aparelho para detectar os encoders.", ToastTipo.Advertencia);
			return;
		}

		var (ok, _, best) = await _s.Adb.DescobrirMelhoresEncodersAsync();
		if (ok && !string.IsNullOrEmpty(best))
		{
			_s.UseAdvancedEncoder = true;
			_s.VideoEncoder = best;
			_s.GuardarConfig();
			NotificarDetalhesUI();
			string proc = ScrcpyManager.InferirProcessador(best);
			ToastService.Mostrar($"✓ Encoder '{proc}' detectado e ativado com sucesso!", ToastTipo.Exito, 4000);
		}
		else
		{
			ToastService.Mostrar("Nenhum encoder de hardware específico encontrado. Usando H.264 padrão.", ToastTipo.Info);
		}
	}

	private RelayCommand? _setSnapdragonCmd;
	public RelayCommand SetSnapdragonCommand => _setSnapdragonCmd ??= new RelayCommand(() => SetEncoderManual("c2.qti.avc.encoder", "Snapdragon (Qualcomm)"));

	private RelayCommand? _setMediaTekCmd;
	public RelayCommand SetMediaTekCommand => _setMediaTekCmd ??= new RelayCommand(() => SetEncoderManual("c2.mtk.video.encoder.avc", "MediaTek"));

	private RelayCommand? _setExynosCmd;
	public RelayCommand SetExynosCommand => _setExynosCmd ??= new RelayCommand(() => SetEncoderManual("c2.exynos.h264.encoder", "Exynos"));

	private RelayCommand? _setUnisocCmd;
	public RelayCommand SetUnisocCommand => _setUnisocCmd ??= new RelayCommand(() => SetEncoderManual("c2.unisoc.avc.encoder", "Unisoc"));

	private void SetEncoderManual(string encoder, string chip)
	{
		_s.UseAdvancedEncoder = true;
		_s.VideoEncoder = encoder;
		_s.GuardarConfig();
		NotificarDetalhesUI();
		ToastService.Mostrar($"Encoder {chip} ({encoder}) ativado!", ToastTipo.Exito);
	}

	private readonly MouseRateTester _rateTester = new MouseRateTester();

	public int MousePollingRateHz
	{
		get => _s.MousePollingRateHz <= 0 ? 1000 : _s.MousePollingRateHz;
		set
		{
			if (_s.MousePollingRateHz != value)
			{
				_s.MousePollingRateHz = value;
				_s.PollingRate1ms = (value >= 1000);
				_svc.EstablecerPollingRate(value);
				OnPropertyChanged(nameof(MousePollingRateHz));
				OnPropertyChanged(nameof(Is1000Hz));
				OnPropertyChanged(nameof(Is500Hz));
				OnPropertyChanged(nameof(Is250Hz));
				OnPropertyChanged(nameof(Is125Hz));
				OnPropertyChanged(nameof(PollingRateStatusTexto));
				_s.GuardarConfig();
				ToastService.Mostrar($"⚡ Polling Rate ajustado para {value} Hz ({1000.0/value:F0} ms)", ToastTipo.Exito);
			}
		}
	}

	public bool Is1000Hz => MousePollingRateHz == 1000;
	public bool Is500Hz => MousePollingRateHz == 500;
	public bool Is250Hz => MousePollingRateHz == 250;
	public bool Is125Hz => MousePollingRateHz == 125;

	public string PollingRateStatusTexto => MousePollingRateHz switch
	{
		>= 1000 => "⚡ 1000 Hz (1 ms) • Ultra Gamer Pro (Zero Delay)",
		>= 500 => "⚡ 500 Hz (2 ms) • Equilibrado Gamer",
		>= 250 => "4 ms (250 Hz) • Mouses Standard",
		_ => "8 ms (125 Hz) • Padrão de Escritório"
	};

	private bool _testandoPolling;
	public bool TestandoPolling
	{
		get => _testandoPolling;
		set => SetProperty(ref _testandoPolling, value);
	}

	private string _pollingRateMedidoTexto = "Clique em 'Testar' e mova o mouse na tela...";
	public string PollingRateMedidoTexto
	{
		get => _pollingRateMedidoTexto;
		set => SetProperty(ref _pollingRateMedidoTexto, value);
	}

	private string _pollingRateRecomendadoTexto = "";
	public string PollingRateRecomendadoTexto
	{
		get => _pollingRateRecomendadoTexto;
		set => SetProperty(ref _pollingRateRecomendadoTexto, value);
	}

	private int _taxaRecomendadaHz = 1000;
	public int TaxaRecomendadaHz
	{
		get => _taxaRecomendadaHz;
		set => SetProperty(ref _taxaRecomendadaHz, value);
	}

	private RelayCommand? _iniciarTestePollingCmd;
	public IRelayCommand IniciarTestePollingCommand => _iniciarTestePollingCmd ??= new RelayCommand(IniciarTestePolling);

	private RelayCommand? _pararTestePollingCmd;
	public IRelayCommand PararTestePollingCommand => _pararTestePollingCmd ??= new RelayCommand(PararTestePolling);

	private RelayCommand? _aplicarTaxaRecomendadaCmd;
	public IRelayCommand AplicarTaxaRecomendadaCommand => _aplicarTaxaRecomendadaCmd ??= new RelayCommand(() =>
	{
		MousePollingRateHz = TaxaRecomendadaHz;
		PararTestePolling();
	});

	private RelayCommand<string>? _selecionarHzCmd;
	public IRelayCommand<string> SelecionarHzCommand => _selecionarHzCmd ??= new RelayCommand<string>((hzStr) =>
	{
		if (int.TryParse(hzStr, out int hz))
		{
			MousePollingRateHz = hz;
		}
	});

	private IAsyncRelayCommand? _autoConfigurarCommand;
	public IAsyncRelayCommand AutoConfigurarCommand => _autoConfigurarCommand ??= new AsyncRelayCommand(AutoConfigurarAsync);

	private void IniciarTestePolling()
	{
		TestandoPolling = true;
		PollingRateMedidoTexto = "Mova o mouse rapidamente aqui na tela...";
		PollingRateRecomendadoTexto = "";
		_rateTester.OnAtualizado -= AtualizarBenchmarkUI;
		_rateTester.OnAtualizado += AtualizarBenchmarkUI;
		_rateTester.Iniciar();
	}

	private void PararTestePolling()
	{
		TestandoPolling = false;
		_rateTester.Parar();
		_rateTester.OnAtualizado -= AtualizarBenchmarkUI;
	}

	private void AtualizarBenchmarkUI()
	{
		Application.Current?.Dispatcher?.InvokeAsync(() =>
		{
			if (!TestandoPolling) return;
			double media = _rateTester.TaxaMediaHz;
			double pico = _rateTester.TaxaPicoHz;
			int rec = _rateTester.TaxaRecomendadaHz;
			TaxaRecomendadaHz = rec;

			PollingRateMedidoTexto = $"⚡ Ao vivo: {media:F0} Hz (~{1000.0/Math.Max(1, media):F1} ms) | Pico: {pico:F0} Hz ({_rateTester.AmostrasColetadas} amostras)";
			PollingRateRecomendadoTexto = $"✓ Recomendado para seu mouse: {rec} Hz ({1000.0/rec:F0} ms)";
		});
	}

	public bool AudioAtivo
	{
		get => _s.Audio;
		set
		{
			if (_s.Audio != value)
			{
				_s.Audio = value;
				_s.GuardarConfig();
				NotificarDetalhesUI();
				ToastService.Mostrar(_s.Audio ? "🔊 Áudio do celular ativado" : "🔇 Áudio do celular desativado", ToastTipo.Info, 2000);
			}
		}
	}

	public ObservableCollection<string> Seriales { get; } = new ObservableCollection<string>();

	private string _selectedSerial = "Seleccionar dispositivo…";
	public string SelectedSerial
	{
		get => _selectedSerial;
		set
		{
			if (SetProperty(ref _selectedSerial, value))
			{
				if (!string.IsNullOrEmpty(value) && !value.Contains("Seleccionar") && !value.Contains("Nenhum"))
				{
					_s.OtgSerial = value;
					_s.OtgSerial = value;
					_s.GuardarConfig();
				}
			}
		}
	}

	private string _consolaAparelhoTexto = "Pressione 'Detectar' para listar aparelhos";
	public string ConsolaAparelhoTexto
	{
		get => _consolaAparelhoTexto;
		set => SetProperty(ref _consolaAparelhoTexto, value);
	}

	private AsyncRelayCommand? _detectarAparelhosCmd;
	public IAsyncRelayCommand DetectarAparelhosCommand => _detectarAparelhosCmd ??= new AsyncRelayCommand(() => PopularSerialesAsync(silencioso: false));

	public async Task PopularSerialesAsync(bool silencioso)
	{
		if (!silencioso)
		{
			ConsolaAparelhoTexto = "Detectando aparelhos USB...";
		}
		List<string> item = (await Task.Run(() => _s.Adb.ListarDispositivos())).Item2;
		System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
		{
			Seriales.Clear();
			if (item.Count == 0)
			{
				Seriales.Add("Nenhum aparelho detectado");
			}
			else
			{
				foreach (string d in item)
				{
					Seriales.Add(d);
				}
			}

			if (item.Count > 0)
			{
				string preferido = (!string.IsNullOrEmpty(_s.OtgSerial) && item.Contains(_s.OtgSerial)) ? _s.OtgSerial : item[0];
				SelectedSerial = preferido;
				if (!silencioso)
				{
					ConsolaAparelhoTexto = $"✓ {item.Count} aparelho(s) detectado(s): {preferido}";
				}
			}
			else
			{
				SelectedSerial = "Nenhum aparelho detectado";
				if (!silencioso)
				{
					ConsolaAparelhoTexto = "❌ Nenhum aparelho detectado. Verifique o cabo USB e a Depuração USB.";
				}
			}
			NotificarDetalhesUI();
		});
	}

	private string _encoderBenchmarkStatus = "";
	public string EncoderBenchmarkStatus
	{
		get => _encoderBenchmarkStatus;
		set => SetProperty(ref _encoderBenchmarkStatus, value);
	}

	private AsyncRelayCommand? _latenciaUltraCmd;
	public IAsyncRelayCommand LatenciaUltraCommand => _latenciaUltraCmd ??= new AsyncRelayCommand(AplicarLatenciaUltraAsync);

	private AsyncRelayCommand? _testarEncodersCmd;
	public IAsyncRelayCommand TestarEncodersCommand => _testarEncodersCmd ??= new AsyncRelayCommand(TestarEncodersAsync);

	private async Task AplicarLatenciaUltraAsync()
	{
		try
		{
			// 1. Configura perfil de Ultra Baixa Latência (Competitivo USB)
			_s.VideoCodec = "h264";
			_s.VideoBuffer = 0;
			_s.AudioBuffer = 0;
			_s.Fps = 120;
			_s.Bitrate = 10;
			_s.MaxSize = 1280;
			_s.AceleracionHardware = true;
			_s.PerfilSeleccionado = "Competitivo USB";
			_s.GuardarConfig();

			// 2. Aplica Zero Delay no Android (escalas de animação = 0 e 120Hz)
			await Task.Run(async () =>
			{
				await _s.Adb.EjecutarShellAsync("settings put global window_animation_scale 0");
				await _s.Adb.EjecutarShellAsync("settings put global transition_animation_scale 0");
				await _s.Adb.EjecutarShellAsync("settings put global animator_duration_scale 0");
				await _s.Adb.EjecutarShellAsync("settings put system peak_refresh_rate 120");
				await _s.Adb.EjecutarShellAsync("settings put system min_refresh_rate 120");
			});

			NotificarDetalhesUI();
			ToastService.Mostrar("⚡ Modo Latência Ultra USB ativado! 120 FPS, GPU D3D11VA (Scrcpy 5.0.1), Buffer 0 e Zero Delay configurados.", ToastTipo.Exito, 4500);
		}
		catch (Exception ex)
		{
			AppLogger.Error("Falha ao aplicar Latência Ultra USB", ex);
			ToastService.Mostrar("Não foi possível aplicar o modo Latência Ultra USB.", ToastTipo.Error);
		}
	}

	private async Task TestarEncodersAsync()
	{
		if (!HayDispositivo)
		{
			ToastService.Mostrar("Conecte um aparelho para testar os encoders.", ToastTipo.Advertencia);
			return;
		}

		EncoderBenchmarkStatus = "Identificando encoders de hardware no aparelho...";
		var (ok, encoders, best) = await _s.Adb.DescobrirMelhoresEncodersAsync();
		if (ok && !string.IsNullOrEmpty(best))
		{
			string proc = ScrcpyManager.InferirProcessador(best);
			EncoderBenchmarkStatus = $"✓ Encoder recomendado: {proc} ({best})\n(Hardware de ultra baixa latência detectado)";
			_s.UseAdvancedEncoder = true;
			_s.VideoEncoder = best;
			_s.GuardarConfig();
			NotificarDetalhesUI();
			ToastService.Mostrar($"✓ Encoder '{proc}' ({best}) selecionado automaticamente!", ToastTipo.Exito, 4000);
		}
		else
		{
			EncoderBenchmarkStatus = "Encoders padrão H.264 ativos (compatibilidade máxima).";
			NotificarDetalhesUI();
			ToastService.Mostrar("Encoders padrão validados com sucesso.", ToastTipo.Info);
		}
	}

	public string[] PresetsDisponiveis => KeymapConfig.PresetsDisponiveis.Keys.ToArray();

	private string _selectedPreset = "Free Fire / FF MAX";
	public string SelectedPreset
	{
		get => _selectedPreset;
		set
		{
			if (SetProperty(ref _selectedPreset, value))
			{
				NotificarDetalhesUI();
			}
		}
	}

	public string PresetDetalhesTexto
	{
		get
		{
			try
			{
				var km = KeymapConfig.CargarPreset(SelectedPreset);
				int botoes = km.Buttons?.Count ?? 0;
				double sensX = km.Camera?.SensitivityX ?? km.Camera?.Sensitivity ?? 2.8;
				double sensY = km.Camera?.SensitivityY ?? 2.2;
				double acel = km.Camera?.Acceleration ?? 0.5;
				return $"Sens X: {sensX:F1} • Y: {sensY:F1} (Puxada: {acel:F1})\nTeclas: {botoes} botões configurados";
			}
			catch
			{
				return "Preset pronto para calibrar";
			}
		}
	}

	private RelayCommand? aplicarPresetCommand;
	public IRelayCommand AplicarPresetCommand => aplicarPresetCommand ??= new RelayCommand(AplicarPreset);

	private void AplicarPreset()
	{
		if (!string.IsNullOrWhiteSpace(SelectedPreset))
		{
			bool ok = _svc.AplicarPresetJogo(SelectedPreset);
			if (ok)
			{
				NotificarDetalhesUI();
				ToastService.Mostrar($"Preset '{SelectedPreset}' aplicado com sucesso!", ToastTipo.Exito);
			}
			else
			{
				ToastService.Mostrar($"Falha ao carregar preset '{SelectedPreset}'", ToastTipo.Error);
			}
		}
	}

	public string[] ResolucionesVd { get; } = new string[6] { "960x540", "1280x720", "1600x900", "1920x1080", "2560x1440", "3840x2160" };

	public DpiVdOpcion[] DpisVd { get; } = new DpiVdOpcion[4]
	{
		new DpiVdOpcion("160 (Baja)", 160),
		new DpiVdOpcion("240 (Media)", 240),
		new DpiVdOpcion("320 (Alta)", 320),
		new DpiVdOpcion("Personalizado…", 0)
	};

	public bool VdActivo
	{
		get
		{
			return _s.MapeadorVd;
		}
		set
		{
			if (_s.MapeadorVd != value)
			{
				_s.MapeadorVd = value;
				OnPropertyChanged("VdActivo");
				OnPropertyChanged("VdJuegoVisible");
				OnPropertyChanged("VdCustomVisible");
				OnPropertyChanged("VdDpiCustomVisible");
			}
		}
	}

	public JuegoVdOpcion? SelectedJuegoVd
	{
		get
		{
			return JuegosVd.FirstOrDefault((JuegoVdOpcion j) => j.Paquete == _s.MapeadorVdJuego) ?? JuegosVd[0];
		}
		set
		{
			if (!(value == null) && !(value.Paquete == _s.MapeadorVdJuego))
			{
				_s.MapeadorVdJuego = value.Paquete;
				OnPropertyChanged("SelectedJuegoVd");
				OnPropertyChanged("VdCustomVisible");
			}
		}
	}

	public string VdJuegoPersonalizado
	{
		get
		{
			return _s.MapeadorVdJuegoPersonalizado;
		}
		set
		{
			_s.MapeadorVdJuegoPersonalizado = value;
			OnPropertyChanged("VdJuegoPersonalizado");
		}
	}

	public string? SelectedResolucionVd
	{
		get
		{
			return _s.MapeadorVdResolucion;
		}
		set
		{
			if (!string.IsNullOrEmpty(value) && !(value == _s.MapeadorVdResolucion))
			{
				_s.MapeadorVdResolucion = value;
				OnPropertyChanged("SelectedResolucionVd");
			}
		}
	}

	public DpiVdOpcion? SelectedDpiVd
	{
		get
		{
			DpiVdOpcion dpiVdOpcion;
			if (!_s.MapeadorVdDpiEsCustom)
			{
				dpiVdOpcion = DpisVd.FirstOrDefault((DpiVdOpcion d) => d.Valor == _s.MapeadorVdDpi);
				if ((object)dpiVdOpcion == null)
				{
					return DpisVd.First((DpiVdOpcion d) => d.Valor == 240);
				}
			}
			else
			{
				dpiVdOpcion = DpisVd.First((DpiVdOpcion d) => d.Valor == 0);
			}
			return dpiVdOpcion;
		}
		set
		{
			if (!(value == null))
			{
				if (value.Valor == 0)
				{
					_s.MapeadorVdDpiEsCustom = true;
				}
				else
				{
					_s.MapeadorVdDpiEsCustom = false;
					_s.MapeadorVdDpi = value.Valor;
				}
				OnPropertyChanged("SelectedDpiVd");
				OnPropertyChanged("VdDpiCustomVisible");
				OnPropertyChanged("VdDpiCustom");
			}
		}
	}

	public int VdDpiCustom
	{
		get
		{
			return _s.MapeadorVdDpi;
		}
		set
		{
			_s.MapeadorVdDpi = value;
			OnPropertyChanged("VdDpiCustom");
		}
	}

	public bool VdDpi160
	{
		get
		{
			if (!_s.MapeadorVdDpiEsCustom)
			{
				return _s.MapeadorVdDpi == 160;
			}
			return false;
		}
		set
		{
			if (value)
			{
				SeleccionarDpi(160);
			}
		}
	}

	public bool VdDpi240
	{
		get
		{
			if (!_s.MapeadorVdDpiEsCustom)
			{
				return _s.MapeadorVdDpi == 240;
			}
			return false;
		}
		set
		{
			if (value)
			{
				SeleccionarDpi(240);
			}
		}
	}

	public bool VdDpi320
	{
		get
		{
			if (!_s.MapeadorVdDpiEsCustom)
			{
				return _s.MapeadorVdDpi == 320;
			}
			return false;
		}
		set
		{
			if (value)
			{
				SeleccionarDpi(320);
			}
		}
	}

	public bool VdDpiEsCustom
	{
		get
		{
			return _s.MapeadorVdDpiEsCustom;
		}
		set
		{
			if (value)
			{
				_s.MapeadorVdDpiEsCustom = true;
				NotificarDpi();
			}
		}
	}

	public bool VdDpiCustomVisible
	{
		get
		{
			if (_s.MapeadorVd)
			{
				return _s.MapeadorVdDpiEsCustom;
			}
			return false;
		}
	}

	public bool VdJuegoVisible => _s.MapeadorVd;

	public bool VdCustomVisible
	{
		get
		{
			if (_s.MapeadorVd)
			{
				return _s.MapeadorVdJuego == "personalizado";
			}
			return false;
		}
	}

	public bool VdEditable
	{
		get
		{
			if (!_svc.SesionActiva)
			{
				return !_conectando;
			}
			return false;
		}
	}

	public bool MostrandoControles => _svc.MostrandoControles;

	public string TextoMostrarTeclas
	{
		get
		{
			if (!_svc.MostrandoControles)
			{
				return "Mostrar teclas";
			}
			return "Ocultar teclas";
		}
	}

	public double OpacidadControles
	{
		get
		{
			return _svc.Keymap.OverlayOpacity;
		}
		set
		{
			_svc.EstablecerOpacidadControles(value);
			OnPropertyChanged("OpacidadControles");
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand PrincipalCommand => principalCommand ?? (principalCommand = new AsyncRelayCommand(PrincipalAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand AlternarCapturaCommand => alternarCapturaCommand ?? (alternarCapturaCommand = new RelayCommand(AlternarCaptura));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand EditarLayoutCommand => editarLayoutCommand ?? (editarLayoutCommand = new RelayCommand(EditarLayout));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand MostrarTeclasCommand => mostrarTeclasCommand ?? (mostrarTeclasCommand = new RelayCommand(MostrarTeclas));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand GuardarPerfilCommand => guardarPerfilCommand ?? (guardarPerfilCommand = new RelayCommand(GuardarPerfil));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand RestablecerPerfilCommand => restablecerPerfilCommand ?? (restablecerPerfilCommand = new AsyncRelayCommand(RestablecerPerfilAsync));

	public MapeadorViewModel()
	{
		_svc = _s.Mapeador;
		_svc.PropertyChanged += SvcChanged;
		_s.PropertyChanged += SessionChanged;
		RefrescarControles();
		RefrescarPerfiles();
		_ = PopularSerialesAsync(silencioso: true);
		_s.Adb.OnDispositivoConectado += (s) => { _ = PopularSerialesAsync(silencioso: true); };
		_s.Adb.OnDispositivoDesconectado += () => { _ = PopularSerialesAsync(silencioso: true); };
		_s.PropertyChanged += (s, e) => NotificarDetalhesUI();
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

	private void SvcChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "EditandoLayout" && !_svc.EditandoLayout)
		{
			RefrescarControles();
		}
		RefrescarEstado();
	}

	private void SessionChanged(object? sender, PropertyChangedEventArgs e)
	{
		RefrescarEstado();
	}

	private void SeleccionarDpi(int valor)
	{
		_s.MapeadorVdDpiEsCustom = false;
		_s.MapeadorVdDpi = valor;
		NotificarDpi();
	}

	private void NotificarDpi()
	{
		OnPropertyChanged("VdDpi160");
		OnPropertyChanged("VdDpi240");
		OnPropertyChanged("VdDpi320");
		OnPropertyChanged("VdDpiEsCustom");
		OnPropertyChanged("VdDpiCustom");
		OnPropertyChanged("VdDpiCustomVisible");
		OnPropertyChanged("SelectedDpiVd");
	}

	[RelayCommand]
	private async Task PrincipalAsync()
	{
		if (_svc.SesionActiva)
		{
			_svc.DetenerSesion();
			RefrescarEstado();
		}
		else if (PuedeIniciar)
		{
			_conectando = true;
			RefrescarEstado();
			try
			{
				await _svc.IniciarSesionAsync();
			}
			finally
			{
				_conectando = false;
				RefrescarEstado();
			}
		}
	}

	[RelayCommand]
	private void AlternarCaptura()
	{
		_svc.AlternarCaptura();
		RefrescarEstado();
	}

	[RelayCommand]
	private void EditarLayout()
	{
		_svc.AbrirEditorLayout();
		RefrescarEstado();
	}

	[RelayCommand]
	private void MostrarTeclas()
	{
		_svc.AlternarControles();
		RefrescarEstado();
	}

	[RelayCommand]
	private void GuardarPerfil()
	{
		if (_svc.GuardarKeymap())
		{
			ToastService.Mostrar("Perfil de controles guardado.", ToastTipo.Exito, 2500);
		}
		else
		{
			ToastService.Mostrar("No se pudo guardar el perfil de controles.", ToastTipo.Error);
		}
		RefrescarControles();
	}

	[RelayCommand]
	private async Task RestablecerPerfilAsync()
	{
		await _svc.RestablecerPerfilAsync();
		RefrescarControles();
	}

	private void RefrescarControles()
	{
		Controles.Clear();
		Controles.Add(new ControlChip
		{
			Etiqueta = "Movimento",
			Tecla = "W A S D"
		});
		Controles.Add(new ControlChip
		{
			Etiqueta = "Cámara / Aim",
			Tecla = "Mouse"
		});
		foreach (ButtonConfig button in _svc.Keymap.Buttons)
		{
			Controles.Add(new ControlChip
			{
				Etiqueta = (string.IsNullOrWhiteSpace(button.Label) ? button.Key : button.Label),
				Tecla = Bonito(button.Key)
			});
		}
	}

	private static string Bonito(string k)
	{
		switch (k)
		{
		case "mouse_left":
			return "Clique esq.";
		case "mouse_right":
			return "Clique dir.";
		case "mouse_middle":
			return "Clique do meio";
		case "Space":
			return "Espaço";
		case "Shift":
		case "LeftShift":
			return "Shift izq";
		default:
			return k;
		}
	}

	private void RefrescarEstado()
	{
		OnPropertyChanged("SesionActiva");
		OnPropertyChanged("Capturando");
		OnPropertyChanged("Editando");
		OnPropertyChanged("HayDispositivo");
		OnPropertyChanged("PuedeIniciar");
		OnPropertyChanged("PuedePrincipal");
		OnPropertyChanged("PrincipalTexto");
		OnPropertyChanged("PrincipalFill");
		OnPropertyChanged("PrincipalFore");
		OnPropertyChanged("PuedeAcciones");
		OnPropertyChanged("PuedeEditarLayout");
		OnPropertyChanged("EstadoSesionTexto");
		OnPropertyChanged("IndicadorBrush");
		OnPropertyChanged("ResolucionTexto");
		OnPropertyChanged("SinDispositivoVisible");
		OnPropertyChanged("EspejoNormalVisible");
		OnPropertyChanged("TextoToggleCaptura");
		OnPropertyChanged("MostrandoControles");
		OnPropertyChanged("TextoMostrarTeclas");
		OnPropertyChanged("VdEditable");
	}

	[RelayCommand]
	private async Task AutoConfigurarAsync()
	{
		ToastService.Mostrar("⚡ Analisando celular e calibrando modo DLuzStacks...", ToastTipo.Info, 2500);
		string? serial = (SelectedSerial != "Detectando..." && !string.IsNullOrWhiteSpace(SelectedSerial)) ? SelectedSerial : null;
		var resultado = await AutoConfigService.ExecutarAutoConfigAsync(serial);
		if (resultado.Sucesso)
		{
			RefrescarEstado();
			string detalhes = string.Join("\n• ", resultado.OtimizacoesAplicadas);
			await DialogService.ExitoAsync("⚡ Calibração DLuzStacks Aplicada!", 
				$"Aparelho: {resultado.DispositivoNome}\n" +
				$"Processador: {resultado.Chipset}\n" +
				$"Display: {resultado.ResolucaoTela} @ {resultado.RefreshRateHz}Hz\n\n" +
				$"Otimizações Ativas:\n• {detalhes}\n\n" +
				"✓ Mapeamento e transmissão configurados para 120 FPS e 0ms de delay!");
			
			ToastService.Mostrar($"✓ DLuzStacks calibrado para {resultado.DispositivoNome} ({resultado.FpsConfigurado} FPS)!", ToastTipo.Exito, 4000);
		}
		else
		{
			await DialogService.AdvertenciaAsync("Auto Configuração", resultado.MensagemResumo);
		}
	}

	private static Brush Recurso(string clave)
	{
		return DLuz.Helpers.ResourceHelper.GetBrush(clave);
	}

	public void Dispose()
	{
		_timer.Stop();
		_svc.PropertyChanged -= SvcChanged;
		_s.PropertyChanged -= SessionChanged;
	}
}









