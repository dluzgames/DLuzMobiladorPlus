using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using DLuz.Services;
using QRCoder;

namespace DLuz.ViewModels;

public class ConexionViewModel : ObservableObject, IDisposable
{
	private readonly SessionState _s = SessionState.Instance;

	private const string OtgOpcionVacia = "Seleccionar dispositivo…";

	private bool _cargando;

	[ObservableProperty]
	private string _adbEstadoTexto = "●  Verificando...";

	[ObservableProperty]
	private Brush _adbEstadoBrush = Brushes.Gray;

	[ObservableProperty]
	private string _otgConsolaTexto = "Pressione 'Detectar' para listar aparelhos";

	private bool _otgActivo;

	private string _selectedSerial;

	[ObservableProperty]
	private string _wifiStatusTexto = "";

	[ObservableProperty]
	private Brush _wifiStatusBrush = Brushes.Gray;

	[ObservableProperty]
	private bool _detectarIpHabilitado;

	[ObservableProperty]
	private bool _habilitarPuertoHabilitado;

	[ObservableProperty]
	private bool _conectarWifiHabilitado;

	[ObservableProperty]
	private bool _cerrarPuertoHabilitado;

	[ObservableProperty]
	private string _habilitarPuertoTexto = "Habilitar Porta";

	[ObservableProperty]
	private string _conectarWifiTexto = "Conectar Wi-Fi";

	private string _puerto = "5555";

	private string _ip = "";

	private bool _wifiActivo;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? reiniciarAdbCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? limpiarHuerfanasCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? detectarOtgCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? detectarIpCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? habilitarPuertoCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? conectarWifiCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? cerrarPuertoCommand;

	private AsyncRelayCommand? novoQrCommand;

	private AsyncRelayCommand? iniciarPareamentoQrCommand;

	private ImageSource? _qrCodeImage;

	private string _qrStatusTexto = "Gere um QR Code para iniciar.";

	private bool _qrPareando;

	private string _qrServiceName = "";

	private string _qrPassword = "";

	public bool OtgHabilitado
	{
		get
		{
			if (!_s.WifiConectado)
			{
				return !_s.UsarWifi;
			}
			return false;
		}
	}

	public bool OtgSesionVisible => !string.IsNullOrEmpty(_s.OtgSerial);

	public string OtgSesionTexto => "\ud83d\udce1 Sesión anterior: OTG (Serial: " + _s.OtgSerial + "); activa el toggle para reconectar.";

	public ObservableCollection<string> Seriales { get; } = new ObservableCollection<string>();

	public bool OtgActivo
	{
		get
		{
			return _otgActivo;
		}
		set
		{
			if (_otgActivo != value)
			{
				HandleOtgToggleAsync(value);
			}
		}
	}

	public string SelectedSerial
	{
		get
		{
			return _selectedSerial;
		}
		set
		{
			if (!(_selectedSerial == value))
			{
				_selectedSerial = value;
				OnPropertyChanged("SelectedSerial");
				if (!_cargando)
				{
					string text = value ?? "";
					_s.OtgSerial = ((text == "Seleccionar dispositivo…" || string.IsNullOrEmpty(text)) ? "" : (text.Contains(" — ") ? text.Split(new string[1] { " — " }, StringSplitOptions.None)[0].Trim() : text.Trim()));
					_s.GuardarConfig();
				}
			}
		}
	}

	public bool WifiSesionVisible => !string.IsNullOrEmpty(_s.WifiIp);

	public string WifiSesionTexto => "\ud83d\udce1 Sesión anterior guardada" + ((!string.IsNullOrEmpty(_s.WifiIp)) ? (" · " + _s.WifiIp) : "") + "; activa el toggle para reconectar rápido.";

	public string Puerto
	{
		get
		{
			return _puerto;
		}
		set
		{
			if (!(_puerto == value))
			{
				_puerto = value;
				OnPropertyChanged("Puerto");
				if (int.TryParse(value, out var result) && result >= 1024 && result <= 65535)
				{
					_s.WifiPuerto = result;
				}
			}
		}
	}

	public string Ip
	{
		get
		{
			return _ip;
		}
		set
		{
			if (!(_ip == value))
			{
				_ip = value;
				OnPropertyChanged("Ip");
				_s.WifiIp = value;
			}
		}
	}

	public bool WifiActivo
	{
		get
		{
			return _wifiActivo;
		}
		set
		{
			if (_wifiActivo != value)
			{
				HandleWifiToggleAsync(value);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string AdbEstadoTexto
	{
		get
		{
			return _adbEstadoTexto;
		}
		[MemberNotNull("_adbEstadoTexto")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_adbEstadoTexto, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AdbEstadoTexto);
				_adbEstadoTexto = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AdbEstadoTexto);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public Brush AdbEstadoBrush
	{
		get
		{
			return _adbEstadoBrush;
		}
		[MemberNotNull("_adbEstadoBrush")]
		set
		{
			if (!EqualityComparer<Brush>.Default.Equals(_adbEstadoBrush, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AdbEstadoBrush);
				_adbEstadoBrush = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AdbEstadoBrush);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string OtgConsolaTexto
	{
		get
		{
			return _otgConsolaTexto;
		}
		[MemberNotNull("_otgConsolaTexto")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_otgConsolaTexto, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.OtgConsolaTexto);
				_otgConsolaTexto = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.OtgConsolaTexto);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string WifiStatusTexto
	{
		get
		{
			return _wifiStatusTexto;
		}
		[MemberNotNull("_wifiStatusTexto")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_wifiStatusTexto, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.WifiStatusTexto);
				_wifiStatusTexto = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.WifiStatusTexto);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public Brush WifiStatusBrush
	{
		get
		{
			return _wifiStatusBrush;
		}
		[MemberNotNull("_wifiStatusBrush")]
		set
		{
			if (!EqualityComparer<Brush>.Default.Equals(_wifiStatusBrush, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.WifiStatusBrush);
				_wifiStatusBrush = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.WifiStatusBrush);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool DetectarIpHabilitado
	{
		get
		{
			return _detectarIpHabilitado;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_detectarIpHabilitado, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.DetectarIpHabilitado);
				_detectarIpHabilitado = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.DetectarIpHabilitado);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool HabilitarPuertoHabilitado
	{
		get
		{
			return _habilitarPuertoHabilitado;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_habilitarPuertoHabilitado, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HabilitarPuertoHabilitado);
				_habilitarPuertoHabilitado = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HabilitarPuertoHabilitado);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ConectarWifiHabilitado
	{
		get
		{
			return _conectarWifiHabilitado;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_conectarWifiHabilitado, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ConectarWifiHabilitado);
				_conectarWifiHabilitado = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ConectarWifiHabilitado);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool CerrarPuertoHabilitado
	{
		get
		{
			return _cerrarPuertoHabilitado;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_cerrarPuertoHabilitado, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CerrarPuertoHabilitado);
				_cerrarPuertoHabilitado = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CerrarPuertoHabilitado);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string HabilitarPuertoTexto
	{
		get
		{
			return _habilitarPuertoTexto;
		}
		[MemberNotNull("_habilitarPuertoTexto")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_habilitarPuertoTexto, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HabilitarPuertoTexto);
				_habilitarPuertoTexto = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HabilitarPuertoTexto);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string ConectarWifiTexto
	{
		get
		{
			return _conectarWifiTexto;
		}
		[MemberNotNull("_conectarWifiTexto")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_conectarWifiTexto, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ConectarWifiTexto);
				_conectarWifiTexto = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ConectarWifiTexto);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ReiniciarAdbCommand => reiniciarAdbCommand ?? (reiniciarAdbCommand = new AsyncRelayCommand(ReiniciarAdbAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand LimpiarHuerfanasCommand => limpiarHuerfanasCommand ?? (limpiarHuerfanasCommand = new AsyncRelayCommand(LimpiarHuerfanasAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand DetectarOtgCommand => detectarOtgCommand ?? (detectarOtgCommand = new AsyncRelayCommand(DetectarOtgAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand DetectarIpCommand => detectarIpCommand ?? (detectarIpCommand = new AsyncRelayCommand(DetectarIpAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand HabilitarPuertoCommand => habilitarPuertoCommand ?? (habilitarPuertoCommand = new AsyncRelayCommand(HabilitarPuertoAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ConectarWifiCommand => conectarWifiCommand ?? (conectarWifiCommand = new AsyncRelayCommand(ConectarWifiAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand CerrarPuertoCommand => cerrarPuertoCommand ?? (cerrarPuertoCommand = new AsyncRelayCommand(CerrarPuertoAsync));

	public IAsyncRelayCommand NovoQrCommand => novoQrCommand ?? (novoQrCommand = new AsyncRelayCommand(GerarNovoQrAsync));

	public IAsyncRelayCommand IniciarPareamentoQrCommand => iniciarPareamentoQrCommand ?? (iniciarPareamentoQrCommand = new AsyncRelayCommand(IniciarPareamentoQrAsync));

	public ImageSource? QrCodeImage
	{
		get => _qrCodeImage;
		private set { SetProperty(ref _qrCodeImage, value); }
	}

	public string QrStatusTexto
	{
		get => _qrStatusTexto;
		private set { SetProperty(ref _qrStatusTexto, value); }
	}

	public bool QrPareando
	{
		get => _qrPareando;
		private set
		{
			if (SetProperty(ref _qrPareando, value))
			{
				OnPropertyChanged(nameof(QrBotoesHabilitados));
			}
		}
	}

	public bool QrBotoesHabilitados => !QrPareando;

	public ConexionViewModel()
	{
		_s.Adb.OnEstadoConexionCambiado += ActualizarEstadoConexion;
		Seriales.Add("Seleccionar dispositivo…");
		_selectedSerial = "Seleccionar dispositivo…";
		bool flag = _s.WifiConectado || _s.PuertoTcpActivo;
		_s.UsarWifi = flag;
		_wifiActivo = flag;
		_otgActivo = _s.ModoOtg;
		_puerto = _s.WifiPuerto.ToString();
		_ip = _s.WifiIp;
		RecalcularWifiEstadoInicial();
		ActualizarEstadoAdbAsync();
		PopularSerialesAsync(silencioso: true);
		_ = GerarNovoQrAsync();
	}

	private Task GerarNovoQrAsync()
	{
		const string alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
		_qrServiceName = "studio-" + TokenSeguro(10, alfabeto);
		_qrPassword = TokenSeguro(12, alfabeto);
		string payload = $"WIFI:T:ADB;S:{_qrServiceName};P:{_qrPassword};;";
		using QRCodeGenerator gerador = new QRCodeGenerator();
		using QRCodeData dados = gerador.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
		using PngByteQRCode qr = new PngByteQRCode(dados);
		byte[] png = qr.GetGraphic(12);
		BitmapImage imagem = new BitmapImage();
		using MemoryStream stream = new MemoryStream(png);
		imagem.BeginInit();
		imagem.CacheOption = BitmapCacheOption.OnLoad;
		imagem.StreamSource = stream;
		imagem.EndInit();
		imagem.Freeze();
		QrCodeImage = imagem;
		QrStatusTexto = "QR Code pronto. Clique em Iniciar espera e escaneie pelo celular.";
		return Task.CompletedTask;
	}

	private static string TokenSeguro(int tamanho, string alfabeto)
	{
		byte[] bytes = RandomNumberGenerator.GetBytes(tamanho);
		return new string(bytes.Select(b => alfabeto[b % alfabeto.Length]).ToArray());
	}

	private async Task IniciarPareamentoQrAsync()
	{
		if (QrPareando || string.IsNullOrWhiteSpace(_qrServiceName)) return;
		QrPareando = true;
		QrStatusTexto = "Aguardando o celular escanear o QR Code…";
		try
		{
			for (int tentativa = 0; tentativa < 90; tentativa++)
			{
				var descoberta = await _s.Adb.ExecutarAdbAsync(new[] { "mdns", "services" }, 5000);
				string? endpoint = descoberta.stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
					.Where(l => l.Contains("_adb-tls-pairing._tcp", StringComparison.OrdinalIgnoreCase))
					.OrderByDescending(l => l.Contains(_qrServiceName, StringComparison.OrdinalIgnoreCase))
					.Select(l => l.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).LastOrDefault())
					.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e));
				if (!string.IsNullOrWhiteSpace(endpoint))
				{
					QrStatusTexto = "Celular encontrado. Realizando o pareamento…";
					var parear = await _s.Adb.ExecutarAdbAsync(new[] { "pair", endpoint, _qrPassword }, 15000);
					string resposta = (parear.stdout + " " + parear.stderr).Trim();
					if (!parear.exito || !resposta.Contains("Successfully paired", StringComparison.OrdinalIgnoreCase))
					{
						QrStatusTexto = "Falha no pareamento: " + resposta;
						return;
					}
					string host = endpoint.Contains(':') ? endpoint[..endpoint.LastIndexOf(':')] : endpoint;
					for (int busca = 0; busca < 15; busca++)
					{
						var servicos = await _s.Adb.ExecutarAdbAsync(new[] { "mdns", "services" }, 5000);
						string? conectar = servicos.stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
							.Where(l => l.Contains("_adb-tls-connect._tcp", StringComparison.OrdinalIgnoreCase))
							.Select(l => l.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).LastOrDefault())
							.FirstOrDefault(e => e != null && e.StartsWith(host, StringComparison.OrdinalIgnoreCase));
						if (!string.IsNullOrWhiteSpace(conectar))
						{
							var conectado = await _s.Adb.ExecutarAdbAsync(new[] { "connect", conectar }, 15000);
							if (conectado.exito && !conectado.stdout.Contains("failed", StringComparison.OrdinalIgnoreCase))
							{
								AplicarEndpointQr(conectar);
								QrStatusTexto = "Pareado e conectado por QR Code com sucesso.";
								await ActualizarEstadoAdbAsync();
								return;
							}
						}
						await Task.Delay(1000);
					}
					QrStatusTexto = "Pareado com sucesso. Ative novamente a Depuração sem fio caso a conexão não apareça.";
					return;
				}
				await Task.Delay(1000);
			}
			QrStatusTexto = "Tempo esgotado. Verifique se PC e celular estão na mesma rede Wi-Fi.";
		}
		catch (Exception ex)
		{
			QrStatusTexto = "Erro no pareamento por QR Code: " + ex.Message;
		}
		finally
		{
			QrPareando = false;
		}
	}

	private void AplicarEndpointQr(string endpoint)
	{
		int separador = endpoint.LastIndexOf(':');
		if (separador <= 0 || !int.TryParse(endpoint[(separador + 1)..], out int porta)) return;
		Ip = endpoint[..separador];
		Puerto = porta.ToString();
		_s.WifiIp = Ip;
		_s.WifiPuerto = porta;
		_s.WifiConectado = true;
		_s.PuertoTcpActivo = true;
		_s.UsarWifi = true;
		EstablecerWifi(v: true);
		RecalcularWifiEstadoInicial();
		OnPropertyChanged(nameof(OtgHabilitado));
		_s.GuardarConfig();
	}

	private async Task ActualizarEstadoAdbAsync()
	{
		var (flag, list, _) = await Task.Run(() => _s.Adb.ListarDispositivos());
		if (flag && list.Count > 0)
		{
			AdbEstadoTexto = $"●  {list.Count} dispositivo(s): {string.Join(", ", list)}";
			AdbEstadoBrush = Rec("Stex.SuccessBrush");
		}
		else
		{
			AdbEstadoTexto = "●  Sin dispositivo detectado";
			AdbEstadoBrush = Rec("Stex.ErrorBrush");
		}
	}

	private void ActualizarEstadoConexion(bool conectado, string serial)
	{
		SessionState.Marshal(delegate
		{
			if (conectado)
			{
				string text = (string.IsNullOrEmpty(serial) ? "1 dispositivo(s) conectado(s)" : ("1 dispositivo(s): " + serial));
				AdbEstadoTexto = "●  " + text;
				AdbEstadoBrush = Rec("Stex.SuccessBrush");
			}
			else
			{
				AdbEstadoTexto = "●  Sin dispositivo detectado";
				AdbEstadoBrush = Rec("Stex.ErrorBrush");
			}
		});
	}

	[RelayCommand]
	private async Task ReiniciarAdbAsync()
	{
		AdbEstadoTexto = "●  Reiniciando servidor ADB...";
		AdbEstadoBrush = Rec("Stex.WarningBrush");
		await _s.Adb.ReiniciarServidorAsync();
		await ActualizarEstadoAdbAsync();
	}

	[RelayCommand]
	private async Task LimpiarHuerfanasAsync()
	{
		var (flag, _, mensaje) = await _s.Adb.LimpiarConexionesWifiAsync();
		if (flag)
		{
			await DialogService.ExitoAsync("✓ Limpieza completada", mensaje);
		}
		else
		{
			await DialogService.ErrorAsync("Error al limpiar conexión WiFi", mensaje);
		}
	}

	private void EstablecerOtg(bool v)
	{
		_otgActivo = v;
		OnPropertyChanged("OtgActivo");
	}

	private async Task HandleOtgToggleAsync(bool nuevo)
	{
		if (_cargando)
		{
			EstablecerOtg(nuevo);
			return;
		}
		if (nuevo && (_s.WifiConectado || _s.UsarWifi))
		{
			EstablecerOtg(v: false);
			await DialogService.AdvertenciaAsync("OTG não disponível", "O modo OTG só funciona com cabo USB.\nDesative o Wi-Fi primeiro.");
			return;
		}
		if (nuevo && !string.IsNullOrEmpty(_s.OtgSerial))
		{
			switch (await DialogService.TresOpcionesAsync("Sesión anterior: OTG (Serial: " + _s.OtgSerial + ")", "O que deseja fazer?", "Usar serial salvo", "Selecionar da lista"))
			{
			case DialogService.TresOpciones.Cancelar:
				EstablecerOtg(v: false);
				return;
			case DialogService.TresOpciones.Secundaria:
				_s.OtgSerial = "";
				_s.GuardarConfig();
				OnPropertyChanged("OtgSesionVisible");
				await PopularSerialesAsync(silencioso: true);
				break;
			}
		}
		EstablecerOtg(nuevo);
		_s.ModoOtg = nuevo;
	}

	[RelayCommand]
	private async Task DetectarOtgAsync()
	{
		await PopularSerialesAsync(silencioso: false);
	}

	private async Task PopularSerialesAsync(bool silencioso)
	{
		if (!silencioso)
		{
			OtgConsolaTexto = "Detectando...";
		}
		List<string> item = (await Task.Run(() => _s.Adb.ListarDispositivos())).Item2;
		System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
		{
			_cargando = true;
			Seriales.Clear();
			Seriales.Add("Seleccionar dispositivo...");
			foreach (string item2 in item)
			{
				Seriales.Add(item2);
			}
			bool flag = false;
			if (!string.IsNullOrEmpty(_s.OtgSerial))
			{
				string text = Seriales.FirstOrDefault((string x) => x.Trim() == _s.OtgSerial);
				if (text != null)
				{
					EstablecerSerial(text);
					flag = true;
				}
			}
			if (!flag && item.Count == 1)
			{
				EstablecerSerial(item[0]);
				flag = true;
			}
			if (!flag)
			{
				EstablecerSerial("Seleccionar dispositivo...");
			}
			_cargando = false;
			if (!silencioso)
			{
				OtgConsolaTexto = ((item.Count > 0) ? $"✓ {item.Count} dispositivo(s) detectado(s)" : "❌ No se detectaron dispositivos\n• Verifica USB y depuración USB habilitada");
			}
		});
	}
	private void EstablecerSerial(string v)
	{
		_selectedSerial = v;
		OnPropertyChanged("SelectedSerial");
	}

	private void EstablecerWifi(bool v)
	{
		_wifiActivo = v;
		OnPropertyChanged("WifiActivo");
	}

	private void RecalcularWifiEstadoInicial()
	{
		DetectarIpHabilitado = _s.HayDispositivo;
		HabilitarPuertoHabilitado = _wifiActivo && !_s.PuertoTcpActivo;
		ConectarWifiHabilitado = _s.PuertoTcpActivo && !_s.WifiConectado;
		CerrarPuertoHabilitado = _s.PuertoTcpActivo;
		HabilitarPuertoTexto = (_s.PuertoTcpActivo ? "Porta Habilitada" : "Habilitar Porta");
		ConectarWifiTexto = (_s.WifiConectado ? "Conectado" : "Conectar Wi-Fi");
		if (_s.WifiConectado)
		{
			SetStatus($"\ud83d\udfe2 Conectado a {_s.WifiIp}:{_s.WifiPuerto}", "Stex.SuccessBrush");
		}
		else if (_s.PuertoTcpActivo)
		{
			SetStatus($"\ud83d\udd35 Puerto {_s.WifiPuerto} habilitado. Ingresa la IP y pulsa Conectar WiFi", "Stex.InfoBrush");
		}
		else if (!_s.HayDispositivo)
		{
			SetStatus("⚪ Sin dispositivo. Conecta el cable USB para empezar", "Stex.WarningBrush");
		}
		else
		{
			SetStatus("⚪ Listo. Activa el toggle WiFi para comenzar", "Stex.TextSecondaryBrush");
		}
	}

	private async Task HandleWifiToggleAsync(bool nuevo)
	{
		if (_cargando)
		{
			EstablecerWifi(nuevo);
			return;
		}
		if (nuevo && !_s.HayDispositivo)
		{
			EstablecerWifi(v: false);
			SetStatus("⚪ Sin dispositivo. Conecta el cable USB primero", "Stex.WarningBrush");
			await DialogService.AdvertenciaAsync("Cabo USB necessário", "Você precisa conectar o celular por cabo USB antes de ativar o modo Wi-Fi.\n\nO cabo é necessário para a primeira configuração.");
			return;
		}
		EstablecerWifi(nuevo);
		_s.UsarWifi = nuevo;
		if (nuevo && _s.ModoOtg)
		{
			_s.ModoOtg = false;
			EstablecerOtg(v: false);
			OnPropertyChanged("OtgHabilitado");
			await DialogService.InfoAsync("Aviso", "Wi-Fi é incompatível com OTG. OTG desativado.");
		}
		if (nuevo)
		{
			if (!string.IsNullOrEmpty(_s.WifiIp) && !_s.PuertoTcpActivo)
			{
				string ipMostrar = ((_s.WifiPuerto > 0) ? $"{_s.WifiIp}:{_s.WifiPuerto}" : _s.WifiIp);
				switch (await DialogService.TresOpcionesAsync("Dados de sessão anterior encontrados (IP: " + ipMostrar + ")" + ipMostrar + ")", "O que deseja fazer?", "Usar dados salvos", "Configurar novamente"))
				{
				case DialogService.TresOpciones.Cancelar:
					EstablecerWifi(v: false);
					_s.UsarWifi = false;
					return;
				case DialogService.TresOpciones.Primaria:
					Ip = _s.WifiIp;
					_s.PuertoTcpActivo = true;
					HabilitarPuertoTexto = "Porta Habilitada";
					HabilitarPuertoHabilitado = false;
					ConectarWifiHabilitado = true;
					SetStatus("\ud83d\udd35 Datos cargados (" + ipMostrar + "); pulsa Conectar WiFi", "Stex.InfoBrush");
					return;
				}
			}
			HabilitarPuertoHabilitado = !_s.PuertoTcpActivo;
			SetStatus(_s.PuertoTcpActivo ? $"\ud83d\udd35 Puerto {_s.WifiPuerto} habilitado; continúa con Conectar WiFi" : "⚪ Listo; pulsa Habilitar Puerto para comenzar", _s.PuertoTcpActivo ? "Stex.InfoBrush" : "Stex.TextSecondaryBrush");
		}
		else
		{
			await Task.Delay(150);
			SetStatus("⏳ Desconectando WiFi...", "Stex.WarningBrush");
			await _s.Adb.DesconectarTodoAsync();
			_s.PuertoTcpActivo = false;
			_s.WifiConectado = false;
			HabilitarPuertoTexto = "Habilitar Porta";
			HabilitarPuertoHabilitado = false;
			ConectarWifiTexto = "Conectar Wi-Fi";
			ConectarWifiHabilitado = false;
			CerrarPuertoHabilitado = false;
			SetStatus(_s.HayDispositivo ? "⚪ WiFi desactivado. Activa el toggle para volver a conectar" : "⚪ WiFi desactivado. Conecta el cable USB para continuar", "Stex.TextSecondaryBrush");
		}
	}

	[RelayCommand]
	private async Task DetectarIpAsync()
	{
		DetectarIpHabilitado = false;
		try
		{
			SetStatus("⏳ Verificando dispositivo...", "Stex.WarningBrush");
			var (flag, list, mensaje) = await Task.Run(() => _s.Adb.ListarDispositivos());
			if (!flag)
			{
				SetStatus(FormatearErrorDeteccionIp(mensaje), "Stex.WarningBrush");
				return;
			}
			if (list.Count == 0)
			{
				SetStatus("⚠ Conecta tu teléfono por USB antes de detectar la IP", "Stex.WarningBrush");
				return;
			}
			string serial = SeleccionarSerialParaWifi(list);
			SetStatus("⏳ Detectando IP del teléfono...", "Stex.WarningBrush");
			var (flag2, text, mensaje2) = await _s.Adb.DetectarIPDispositivoAsync(serial);
			if (flag2)
			{
				Ip = text;
				SetStatus("✓ IP detectada: " + text + "; ahora pulsa Conectar WiFi", "Stex.SuccessBrush");
			}
			else
			{
				SetStatus(FormatearErrorDeteccionIp(mensaje2), "Stex.WarningBrush");
			}
		}
		finally
		{
			DetectarIpHabilitado = true;
		}
	}

	[RelayCommand]
	private async Task HabilitarPuertoAsync()
	{
		HabilitarPuertoHabilitado = false;
		HabilitarPuertoTexto = "Habilitando...";
		SetStatus("⏳ Habilitando puerto...", "Stex.WarningBrush");
		_s.OperacionWifiEnCurso = true;
		try
		{
			var (flag, list, _) = await Task.Run(() => _s.Adb.ListarDispositivos());
			if (!flag || list.Count == 0)
			{
				SetStatus("❌ Conecta el USB primero", "Stex.ErrorBrush");
				HabilitarPuertoTexto = "Habilitar Porta";
				HabilitarPuertoHabilitado = true;
				await DialogService.AdvertenciaAsync("USB Necessário", "Conecte o celular por USB antes de habilitar a porta Wi-Fi.");
				return;
			}
			string serial = SeleccionarSerialParaWifi(list);
			if ((await _s.Adb.HabilitarTcpipAsync(_s.WifiPuerto, serial)).Item1)
			{
				_s.PuertoTcpActivo = true;
				HabilitarPuertoTexto = "Porta Habilitada";
				HabilitarPuertoHabilitado = false;
				CerrarPuertoHabilitado = true;
				SetStatus("⏳ Detectando IP del dispositivo...", "Stex.InfoBrush");
				await Task.Delay(3000);
				var (flag2, seriales) = await EsperarDispositivoAdbAsync();
				bool flag3;
				string text;
				string mensaje;
				if (flag2)
				{
					(flag3, text, mensaje) = await _s.Adb.DetectarIPDispositivoAsync(SeleccionarSerialParaWifi(seriales));
				}
				else
				{
					flag3 = false;
					text = "";
					mensaje = "O aparelho não respondeu. Tente novamente em alguns segundos.";
				}
				if (flag3)
				{
					Ip = text;
					SetStatus($"\ud83d\udd35 Puerto {_s.WifiPuerto} habilitado (IP: {text}); pulsa Conectar WiFi", "Stex.InfoBrush");
				}
				else
				{
					SetStatus($"\ud83d\udd35 Puerto {_s.WifiPuerto} habilitado.\n" + FormatearErrorDeteccionIp(mensaje).Replace("⚠ ", ""), "Stex.InfoBrush");
				}
				ConectarWifiHabilitado = true;
			}
			else
			{
				SetStatus("❌ Error habilitando puerto", "Stex.ErrorBrush");
				HabilitarPuertoTexto = "Habilitar Porta";
				HabilitarPuertoHabilitado = true;
			}
		}
		finally
		{
			_s.OperacionWifiEnCurso = false;
		}
	}

	[RelayCommand]
	private async Task ConectarWifiAsync()
	{
		if (!LicenseService.Instance.VerificarOuBloquearPro("Conexão Wi-Fi Sem Fio"))
		{
			return;
		}
		if (string.IsNullOrWhiteSpace(_s.WifiIp))
		{
			await DialogService.AdvertenciaAsync("IP Necessário", "Insira ou detecte o IP do aparelho primeiro.");
			return;
		}
		ConectarWifiHabilitado = false;
		ConectarWifiTexto = "Conectando...";
		SetStatus("⏳ Conectando via WiFi...", "Stex.WarningBrush");
		_s.OperacionWifiEnCurso = true;
		try
		{
			if (!(await _s.Adb.PingDispositivoTcpAsync(_s.WifiIp, _s.WifiPuerto)))
			{
				SetStatus("❌ No se pudo alcanzar el dispositivo", "Stex.ErrorBrush");
				ConectarWifiTexto = "Conectar Wi-Fi";
				ConectarWifiHabilitado = true;
				await DialogService.AdvertenciaAsync("Sin conexión WiFi", $"Não foi possível alcançar o aparelho em {_s.WifiIp}:{_s.WifiPuerto}.\n\n⚠ Verifique se o celular e o PC estão na mesma rede Wi-Fi\n⚠ Confirme se o IP está correto (use 🔄 para detectar)\n⚠ Certifique-se de ter completado o passo ① Habilitar Porta com o cabo conectado" + "• Verifica que el teléfono y el PC estén en la misma red WiFi\n• Confirma que la IP sea correcta (usa \ud83d\udd04 para detectarla)\n• Asegúrate de haber completado el paso ③ Habilitar Puerto con el cable conectado");
			}
			else if ((await _s.Adb.ConectarWifiAsync(_s.WifiIp, _s.WifiPuerto)).Item1)
			{
				_s.WifiConectado = true;
				_s.MonitorearUsbConWifiAsync();
				SetStatus($"\ud83d\udfe2 Conectado a {_s.WifiIp}:{_s.WifiPuerto}", "Stex.SuccessBrush");
				ConectarWifiTexto = "Conectado";
				ConectarWifiHabilitado = false;
				CerrarPuertoHabilitado = true;
				ActualizarEstadoAdbAsync();
				OnPropertyChanged("OtgHabilitado");
				await DialogService.ExitoAsync("✓ WiFi Conectado", $"✓ O celular está conectado por Wi-Fi ({_s.WifiIp}:{_s.WifiPuerto}).\n\nVocê já pode desconectar o cabo USB com segurança.\nA conexão permanecerá ativa enquanto estiver na mesma rede Wi-Fi." + "Ya puedes desconectar el cable USB con seguridad.\nLa conexión seguirá activa mientras estén en la misma red WiFi.");
			}
			else
			{
				SetStatus("❌ Error de conexión", "Stex.ErrorBrush");
				ConectarWifiTexto = "Conectar Wi-Fi";
				ConectarWifiHabilitado = true;
			}
		}
		finally
		{
			_s.OperacionWifiEnCurso = false;
		}
	}

	[RelayCommand]
	private async Task CerrarPuertoAsync()
	{
		if (!(await DialogService.ConfirmarAsync("Confirmar fechamento Wi-Fi", "Fechar a porta Wi-Fi?", "Isso desconectará a sessão Wi-Fi atual e o aparelho voltará ao modo USB. Você precisará do cabo USB para continuar usando o app.")))
		{
			return;
		}
		CerrarPuertoHabilitado = false;
		SetStatus("⏳ Cerrando puerto, por favor espera...", "Stex.WarningBrush");
		_s.OperacionWifiEnCurso = true;
		try
		{
			(bool, string) tuple = await _s.Adb.DesconectarTodoAsync();
			bool disconnectOk = tuple.Item1;
			string disconnectMsg = tuple.Item2;
			(bool, string, string) obj = await _s.Adb.CerrarTcpipAsync();
			bool item = obj.Item1;
			string item2 = obj.Item2;
			string item3 = obj.Item3;
			bool cierreOk = disconnectOk && item;
			if (cierreOk)
			{
				_s.PuertoTcpActivo = false;
				_s.WifiConectado = false;
				SetStatus("⚪ Puerto cerrado; reconecta el cable USB para continuar", "Stex.TextSecondaryBrush");
				HabilitarPuertoTexto = "Habilitar Porta";
				HabilitarPuertoHabilitado = false;
				ConectarWifiTexto = "Conectar Wi-Fi";
				ConectarWifiHabilitado = false;
				CerrarPuertoHabilitado = false;
				OnPropertyChanged("OtgHabilitado");
				await DialogService.ExitoAsync("✓ Puerto Cerrado", "Porta fechada com sucesso.\n\nPara reconectar por Wi-Fi, clique em ① Habilitar Porta.");
			}
			else
			{
				string text = "";
				if (!disconnectOk)
				{
					text = "disconnect: " + disconnectMsg;
				}
				if (!item)
				{
					if (!string.IsNullOrWhiteSpace(text))
					{
						text += "\n";
					}
					text += (string.IsNullOrWhiteSpace(item3) ? item2 : item3);
				}
				SetStatus("Fechar a porta Wi-Fi?", "Stex.WarningBrush");
				CerrarPuertoHabilitado = true;
				await DialogService.AdvertenciaAsync("⚠ Advertencia", "Não foi possível fechar automaticamente.\nVocê pode reiniciar o celular para fechar a porta.\n\nErro: " + text + text);
			}
			if (cierreOk && _wifiActivo && _s.HayDispositivo)
			{
				HabilitarPuertoHabilitado = true;
				SetStatus("⚪ Puerto cerrado; pulsa ③ Habilitar Puerto para reconectar", "Stex.TextSecondaryBrush");
			}
		}
		finally
		{
			_s.OperacionWifiEnCurso = false;
		}
	}

	private void SetStatus(string texto, string brushKey)
	{
		WifiStatusTexto = texto;
		WifiStatusBrush = Rec(brushKey);
	}

	private async Task<(bool exito, List<string> seriales)> EsperarDispositivoAdbAsync()
	{
		for (int intento = 0; intento < 3; intento++)
		{
			var (flag, list, _) = await Task.Run(() => _s.Adb.ListarDispositivos());
			if (flag && list.Count > 0)
			{
				return (exito: true, seriales: list);
			}
			if (intento < 2)
			{
				await Task.Delay(1000);
			}
		}
		return (exito: false, seriales: new List<string>());
	}

	private static string SeleccionarSerialParaWifi(List<string> seriales)
	{
		foreach (string seriale in seriales)
		{
			if (!seriale.Contains(':'))
			{
				return seriale;
			}
		}
		if (seriales.Count <= 0)
		{
			return "";
		}
		return seriales[0];
	}

	private static string FormatearErrorDeteccionIp(string mensaje)
	{
		if (mensaje.Contains("No se detectó WiFi activo", StringComparison.OrdinalIgnoreCase))
		{
			return "⚠ Activa el WiFi en tu teléfono";
		}
		if (mensaje.Contains("no respondió", StringComparison.OrdinalIgnoreCase) || mensaje.Contains("timeout", StringComparison.OrdinalIgnoreCase))
		{
			return "⚠ El dispositivo no respondió. Intenta de nuevo en unos segundos";
		}
		if (mensaje.Contains("Conecta tu teléfono", StringComparison.OrdinalIgnoreCase) || mensaje.Contains("no devices", StringComparison.OrdinalIgnoreCase))
		{
			return "⚠ Conecta tu teléfono por USB";
		}
		return "⚠ No se pudo detectar la IP. Puedes escribirla manualmente";
	}

	private static Brush Rec(string clave)
	{
		return DLuz.Helpers.ResourceHelper.GetBrush(clave);
	}

	public void Dispose()
	{
		_s.Adb.OnEstadoConexionCambiado -= ActualizarEstadoConexion;
	}
}





