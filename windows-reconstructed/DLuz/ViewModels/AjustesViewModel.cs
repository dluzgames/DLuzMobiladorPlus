using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLuz.Helpers;
using DLuz.Mapper;
using DLuz.Services;

namespace DLuz.ViewModels;

public sealed class AjustesViewModel : ObservableObject, IDisposable
{
	private static AjustesViewModel? _instance;
	public static AjustesViewModel Instance => _instance ??= new AjustesViewModel();

	private readonly SessionState _s = SessionState.Instance;
	private readonly MouseRateTester _mouseTester = new MouseRateTester();
	private readonly DispatcherTimer _uiTimer;

	private string _frequenciaSelecionada = "1000Hz (1ms - Competitivo)";
	public string FrequenciaSelecionada
	{
		get => _frequenciaSelecionada;
		set
		{
			if (string.IsNullOrEmpty(value)) return;
			if (SetProperty(ref _frequenciaSelecionada, value))
			{
				int hz = 1000;
				int ms = 1;
				if (value.StartsWith("500"))
				{
					hz = 500;
					ms = 2;
				}
				else if (value.StartsWith("250"))
				{
					hz = 250;
					ms = 4;
				}
				else if (value.StartsWith("125"))
				{
					hz = 125;
					ms = 8;
				}

				_s.MousePollingRateHz = hz;
				_s.PollingRate1ms = (hz == 1000);

				HzAtual = hz;
				TestadorStatus = $"Frequência configurada: {hz} Hz ({ms} ms por frame) • Mova o mouse para medir o sensor";

				OnPropertyChanged(nameof(Polling1000Hz));
				SalvarAjustes();
			}
		}
	}

	public bool Polling1000Hz
	{
		get => _frequenciaSelecionada != null && _frequenciaSelecionada.StartsWith("1000");
		set
		{
			if (value)
			{
				if (!FrequenciaSelecionada.StartsWith("1000"))
				{
					FrequenciaSelecionada = "1000Hz (1ms - Competitivo)";
				}
			}
			else
			{
				if (FrequenciaSelecionada.StartsWith("1000"))
				{
					FrequenciaSelecionada = "500Hz (2ms - Padrão)";
				}
			}
		}
	}

	public ObservableCollection<string> Frequencias { get; } = new ObservableCollection<string>
	{
		"1000Hz (1ms - Competitivo)",
		"500Hz (2ms - Padrão)",
		"250Hz (4ms)",
		"125Hz (8ms)"
	};

	private bool _timerMultimidiaAtivo = true;
	public bool TimerMultimidiaAtivo
	{
		get => _timerMultimidiaAtivo;
		set
		{
			if (SetProperty(ref _timerMultimidiaAtivo, value))
			{
				SalvarAjustes();
			}
		}
	}

	private bool _prioridadeMaximaThread = true;
	public bool PrioridadeMaximaThread
	{
		get => _prioridadeMaximaThread;
		set
		{
			if (SetProperty(ref _prioridadeMaximaThread, value))
			{
				SalvarAjustes();
			}
		}
	}

	private double _puxadaCapaY = 1.35;
	public double PuxadaCapaY
	{
		get => _puxadaCapaY;
		set
		{
			if (SetProperty(ref _puxadaCapaY, Math.Round(value, 2)))
			{
				SalvarAjustes();
			}
		}
	}

	private double _aceleracaoCamera = 0.6;
	public double AceleracaoCamera
	{
		get => _aceleracaoCamera;
		set
		{
			if (SetProperty(ref _aceleracaoCamera, Math.Round(value, 2)))
			{
				SalvarAjustes();
			}
		}
	}

	private double _sensibilidadeX = 2.8;
	public double SensibilidadeX
	{
		get => _sensibilidadeX;
		set
		{
			if (SetProperty(ref _sensibilidadeX, Math.Round(value, 2)))
			{
				SalvarAjustes();
			}
		}
	}

	private double _sensibilidadeY = 2.2;
	public double SensibilidadeY
	{
		get => _sensibilidadeY;
		set
		{
			if (SetProperty(ref _sensibilidadeY, Math.Round(value, 2)))
			{
				SalvarAjustes();
			}
		}
	}

	private double _suavizacaoMira = 0.1;
	public double SuavizacaoMira
	{
		get => _suavizacaoMira;
		set
		{
			if (SetProperty(ref _suavizacaoMira, Math.Round(value, 2)))
			{
				SalvarAjustes();
			}
		}
	}

	private string _testadorStatus = "Frequência ativa: 1000 Hz (1 ms por frame) • Mova o mouse para medir o sensor";
	public string TestadorStatus
	{
		get => _testadorStatus;
		set => SetProperty(ref _testadorStatus, value);
	}

	private double _hzAtual = 1000;
	public double HzAtual
	{
		get => _hzAtual;
		set => SetProperty(ref _hzAtual, value);
	}

	private RelayCommand? _restaurarPadroesCmd;
	public IRelayCommand RestaurarPadroesCommand => _restaurarPadroesCmd ??= new RelayCommand(RestaurarPadroes);

	private RelayCommand? _presetPadraoCmd;
	public IRelayCommand PresetPadraoCommand => _presetPadraoCmd ??= new RelayCommand(AplicarPresetPadrao);

	private RelayCommand? _presetCapaExtremoCmd;
	public IRelayCommand PresetCapaExtremoCommand => _presetCapaExtremoCmd ??= new RelayCommand(AplicarPresetCapaExtremo);

	private RelayCommand? _presetPrecisaoCmd;
	public IRelayCommand PresetPrecisaoCommand => _presetPrecisaoCmd ??= new RelayCommand(AplicarPresetPrecisao);

	public void AplicarPresetPadrao()
	{
		if (!LicenseService.Instance.VerificarOuBloquearPro("Presets Rápidos de Capa")) return;
		PuxadaCapaY = 1.35;
		AceleracaoCamera = 0.60;
		SensibilidadeX = 2.8;
		SensibilidadeY = 2.2;
		SuavizacaoMira = 0.10;
		SalvarAjustes();
		ToastService.Mostrar("🎯 Preset Padrão Free Fire (Capa Equilibrado) aplicado!", ToastTipo.Exito);
	}

	public void AplicarPresetCapaExtremo()
	{
		if (!LicenseService.Instance.VerificarOuBloquearPro("Preset Puxada Extrema (Capa Fácil)")) return;
		PuxadaCapaY = 1.85;
		AceleracaoCamera = 0.90;
		SensibilidadeX = 3.5;
		SensibilidadeY = 3.0;
		SuavizacaoMira = 0.05;
		SalvarAjustes();
		ToastService.Mostrar("🔥 Preset Puxada Extrema / Capa Fácil aplicado!", ToastTipo.Exito);
	}

	public void AplicarPresetPrecisao()
	{
		if (!LicenseService.Instance.VerificarOuBloquearPro("Preset Precisão Competitiva")) return;
		PuxadaCapaY = 1.00;
		AceleracaoCamera = 0.00;
		SensibilidadeX = 2.0;
		SensibilidadeY = 2.0;
		SuavizacaoMira = 0.20;
		SalvarAjustes();
		ToastService.Mostrar("🛡️ Preset Precisão Competitiva (PUBG / CODM / CS) aplicado!", ToastTipo.Exito);
	}

	public AjustesViewModel()
	{
		CarregarAjustes();
		_mouseTester.Iniciar();

		_uiTimer = new DispatcherTimer(DispatcherPriority.Render)
		{
			Interval = TimeSpan.FromMilliseconds(40) // 25 FPS UI update
		};
		_uiTimer.Tick += (s, e) =>
		{
			_mouseTester.VerificarInatividade();

			if (_mouseTester.MouseEmMovimento && _mouseTester.TaxaMediaHz > 0)
			{
				HzAtual = Math.Round(_mouseTester.TaxaMediaHz);
				double ms = 1000.0 / Math.Max(1.0, _mouseTester.TaxaMediaHz);
				TestadorStatus = $"Polling medido ao vivo: {_mouseTester.TaxaMediaHz:F0} Hz ({ms:F2} ms • Pico: {_mouseTester.TaxaPicoHz:F0} Hz)";
			}
			else if (!_mouseTester.MouseEmMovimento)
			{
				int hzConfigurado = _s.MousePollingRateHz > 0 ? _s.MousePollingRateHz : 1000;
				HzAtual = hzConfigurado;
				int msConfigurado = hzConfigurado == 1000 ? 1 : (hzConfigurado == 500 ? 2 : (hzConfigurado == 250 ? 4 : 8));
				TestadorStatus = $"Frequência configurada: {hzConfigurado} Hz ({msConfigurado} ms por frame) • Mova o mouse para medir";
			}
		};
		_uiTimer.Start();
	}

	private void RestaurarPadroes()
	{
		FrequenciaSelecionada = "1000Hz (1ms - Competitivo)";
		TimerMultimidiaAtivo = true;
		PrioridadeMaximaThread = true;
		AplicarPresetPadrao();
		HzAtual = 1000;
		TestadorStatus = "Frequência ativa: 1000 Hz (1 ms - Padrão Competitivo restaurado)";
	}

	private void SalvarAjustes()
	{
		try
		{
			if (_s.Mapeador != null)
			{
				_s.Mapeador.EstablecerCamaraAvanzada(PuxadaCapaY, AceleracaoCamera, SuavizacaoMira);
				_s.Mapeador.EstablecerCamara(SensibilidadeX, SensibilidadeY);
			}
			_s.GuardarConfig();
		}
		catch { }
	}

	private void CarregarAjustes()
	{
		try
		{
			int hz = _s.MousePollingRateHz > 0 ? _s.MousePollingRateHz : 1000;
			_hzAtual = hz;
			_frequenciaSelecionada = hz switch
			{
				500 => "500Hz (2ms - Padrão)",
				250 => "250Hz (4ms)",
				125 => "125Hz (8ms)",
				_ => "1000Hz (1ms - Competitivo)"
			};

			if (_s.Mapeador?.Keymap?.Camera != null)
			{
				var cam = _s.Mapeador.Keymap.Camera;
				if (cam.ExponentY > 0) _puxadaCapaY = cam.ExponentY;
				if (cam.Acceleration >= 0) _aceleracaoCamera = cam.Acceleration;
				if (cam.SensitivityX > 0) _sensibilidadeX = cam.SensitivityX;
				if (cam.SensitivityY > 0) _sensibilidadeY = cam.SensitivityY;
				if (cam.Smoothing >= 0) _suavizacaoMira = cam.Smoothing;
			}
		}
		catch { }
	}

	public void Dispose()
	{
		_uiTimer?.Stop();
		_mouseTester.Dispose();
	}
}
