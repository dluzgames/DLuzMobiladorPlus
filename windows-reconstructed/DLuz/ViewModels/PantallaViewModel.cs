using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using DLuz.Services;

namespace DLuz.ViewModels;

public class PantallaViewModel : SeccionViewModel, IDisposable
{
	[ObservableProperty]
	private string _resStatus = "Detecta a resolução nativa do aparelho.";

	[ObservableProperty]
	private string _cropAplicadoTexto = "";

	[ObservableProperty]
	private string _adbStatus = "";

	[ObservableProperty]
	private string _wmStatus = "";

	[ObservableProperty]
	private string _dpiActualTexto = "DPI actual: Detectando...";

	[ObservableProperty]
	private string _dpiStatus = "";

	[ObservableProperty]
	private bool _calcularCropHabilitado;

	[ObservableProperty]
	private bool _restablecerCropHabilitado;

	[ObservableProperty]
	private bool _aplicarResHabilitado;

	[ObservableProperty]
	private bool _resetearResHabilitado;

	[ObservableProperty]
	private bool _aplicarWmHabilitado;

	[ObservableProperty]
	private bool _revertirWmHabilitado;

	[ObservableProperty]
	private bool _cropActivoWarningVisible;

	[ObservableProperty]
	private bool _adbActivaWarningVisible;

	[ObservableProperty]
	private bool _requiereResolucionVisible;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? detectarResolucionCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? calcularCropCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? restablecerCropCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? aplicarResolucionCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? resetearResolucionCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? aplicarWmCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? revertirWmCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? detectarDpiCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? aplicarDpiCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? resetearDpiCommand;

	protected override SeccionPerfil Seccion => SeccionPerfil.Pantalla;

	public string[] AspectRatios { get; } = new string[6] { "16:9", "16:10", "21:9", "18:9", "4:3", "Personalizado" };

	public bool PantallaControlesHabilitados => !base.S.PantallaVirtualDex;

	public bool PantallaBloqueadaPorDexVisible => base.S.PantallaVirtualDex;

	public bool CustomVisible => base.S.AspectRatio == "Personalizado";

	public string ResAncho
	{
		get
		{
			if (base.S.ResolucionAncho <= 0)
			{
				return "—";
			}
			return base.S.ResolucionAncho.ToString();
		}
	}

	public string ResAlto
	{
		get
		{
			if (base.S.ResolucionAlto <= 0)
			{
				return "—";
			}
			return base.S.ResolucionAlto.ToString();
		}
	}

	public bool UltimoDpiVisible => base.S.UltimoDpiAplicado > 0;

	public string UltimoDpiTexto => $"Último DPI aplicado: {base.S.UltimoDpiAplicado}";

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string ResStatus
	{
		get
		{
			return _resStatus;
		}
		[MemberNotNull("_resStatus")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_resStatus, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ResStatus);
				_resStatus = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ResStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string CropAplicadoTexto
	{
		get
		{
			return _cropAplicadoTexto;
		}
		[MemberNotNull("_cropAplicadoTexto")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_cropAplicadoTexto, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CropAplicadoTexto);
				_cropAplicadoTexto = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CropAplicadoTexto);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string AdbStatus
	{
		get
		{
			return _adbStatus;
		}
		[MemberNotNull("_adbStatus")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_adbStatus, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AdbStatus);
				_adbStatus = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AdbStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string WmStatus
	{
		get
		{
			return _wmStatus;
		}
		[MemberNotNull("_wmStatus")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_wmStatus, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.WmStatus);
				_wmStatus = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.WmStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string DpiActualTexto
	{
		get
		{
			return _dpiActualTexto;
		}
		[MemberNotNull("_dpiActualTexto")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_dpiActualTexto, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.DpiActualTexto);
				_dpiActualTexto = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.DpiActualTexto);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string DpiStatus
	{
		get
		{
			return _dpiStatus;
		}
		[MemberNotNull("_dpiStatus")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_dpiStatus, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.DpiStatus);
				_dpiStatus = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.DpiStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool CalcularCropHabilitado
	{
		get
		{
			return _calcularCropHabilitado;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_calcularCropHabilitado, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CalcularCropHabilitado);
				_calcularCropHabilitado = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CalcularCropHabilitado);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool RestablecerCropHabilitado
	{
		get
		{
			return _restablecerCropHabilitado;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_restablecerCropHabilitado, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.RestablecerCropHabilitado);
				_restablecerCropHabilitado = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.RestablecerCropHabilitado);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool AplicarResHabilitado
	{
		get
		{
			return _aplicarResHabilitado;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_aplicarResHabilitado, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AplicarResHabilitado);
				_aplicarResHabilitado = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AplicarResHabilitado);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ResetearResHabilitado
	{
		get
		{
			return _resetearResHabilitado;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_resetearResHabilitado, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ResetearResHabilitado);
				_resetearResHabilitado = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ResetearResHabilitado);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool AplicarWmHabilitado
	{
		get
		{
			return _aplicarWmHabilitado;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_aplicarWmHabilitado, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AplicarWmHabilitado);
				_aplicarWmHabilitado = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AplicarWmHabilitado);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool RevertirWmHabilitado
	{
		get
		{
			return _revertirWmHabilitado;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_revertirWmHabilitado, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.RevertirWmHabilitado);
				_revertirWmHabilitado = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.RevertirWmHabilitado);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool CropActivoWarningVisible
	{
		get
		{
			return _cropActivoWarningVisible;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_cropActivoWarningVisible, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CropActivoWarningVisible);
				_cropActivoWarningVisible = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CropActivoWarningVisible);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool AdbActivaWarningVisible
	{
		get
		{
			return _adbActivaWarningVisible;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_adbActivaWarningVisible, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AdbActivaWarningVisible);
				_adbActivaWarningVisible = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AdbActivaWarningVisible);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool RequiereResolucionVisible
	{
		get
		{
			return _requiereResolucionVisible;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_requiereResolucionVisible, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.RequiereResolucionVisible);
				_requiereResolucionVisible = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.RequiereResolucionVisible);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand DetectarResolucionCommand => detectarResolucionCommand ?? (detectarResolucionCommand = new AsyncRelayCommand(DetectarResolucionAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand CalcularCropCommand => calcularCropCommand ?? (calcularCropCommand = new AsyncRelayCommand(CalcularCropAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand RestablecerCropCommand => restablecerCropCommand ?? (restablecerCropCommand = new RelayCommand(RestablecerCrop));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand AplicarResolucionCommand => aplicarResolucionCommand ?? (aplicarResolucionCommand = new AsyncRelayCommand(AplicarResolucionAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ResetearResolucionCommand => resetearResolucionCommand ?? (resetearResolucionCommand = new AsyncRelayCommand(ResetearResolucionAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand AplicarWmCommand => aplicarWmCommand ?? (aplicarWmCommand = new AsyncRelayCommand(AplicarWmAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand RevertirWmCommand => revertirWmCommand ?? (revertirWmCommand = new AsyncRelayCommand(RevertirWmAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand DetectarDpiCommand => detectarDpiCommand ?? (detectarDpiCommand = new AsyncRelayCommand(DetectarDpiAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand AplicarDpiCommand => aplicarDpiCommand ?? (aplicarDpiCommand = new AsyncRelayCommand(AplicarDpiAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ResetearDpiCommand => resetearDpiCommand ?? (resetearDpiCommand = new AsyncRelayCommand(ResetearDpiAsync));

	public PantallaViewModel()
	{
		base.S.PropertyChanged += OnSessionChanged;
		base.S.Adb.OnDispositivoConectado += OnConectado;
		base.S.Adb.OnDispositivoDesconectado += OnDesconectado;
		base.S.Adb.OnDpiActualizado += OnDpiActualizado;
		base.S.Adb.OnResolucionActualizada += OnResolucionActualizada;
		_cropAplicadoTexto = (base.S.CropActivo ? ("✓ Crop activo: " + base.S.FullscreenCrop) : ((!string.IsNullOrEmpty(base.S.FullscreenCrop)) ? ("Crop en perfil: " + base.S.FullscreenCrop + " (no activo)") : "Sin crop aplicado"));
		_wmStatus = (base.S.WmSizeActivo ? "✓ Activo" : "");
		ActualizarEstados();
		DetectarDpiAlCargarAsync();
		if (base.S.HayDispositivo && !base.S.ResolucionNativaDetectada)
		{
			DetectarResolucionAlCargarAsync();
		}
	}

	private void OnSessionChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "AspectRatio")
		{
			OnPropertyChanged("CustomVisible");
		}
		if (e.PropertyName == "ResolucionAncho")
		{
			OnPropertyChanged("ResAncho");
		}
		if (e.PropertyName == "ResolucionAlto")
		{
			OnPropertyChanged("ResAlto");
		}
		string propertyName = e.PropertyName;
		if ((propertyName == "HayDispositivo" || propertyName == "ResolucionNativaDetectada") ? true : false)
		{
			ActualizarEstados();
		}
		if (e.PropertyName == "PantallaVirtualDex")
		{
			OnPropertyChanged("PantallaControlesHabilitados");
			OnPropertyChanged("PantallaBloqueadaPorDexVisible");
		}
	}

	private void ActualizarEstados()
	{
		bool cropActivo = base.S.CropActivo;
		bool resAdbActiva = base.S.ResAdbActiva;
		bool wmSizeActivo = base.S.WmSizeActivo;
		bool hayDispositivo = base.S.HayDispositivo;
		bool resolucionNativaDetectada = base.S.ResolucionNativaDetectada;
		CalcularCropHabilitado = hayDispositivo && resolucionNativaDetectada && !resAdbActiva && !wmSizeActivo && !cropActivo;
		RestablecerCropHabilitado = cropActivo;
		AplicarResHabilitado = hayDispositivo && resolucionNativaDetectada && !cropActivo && !wmSizeActivo && !resAdbActiva;
		ResetearResHabilitado = hayDispositivo && resAdbActiva;
		AplicarWmHabilitado = hayDispositivo && resolucionNativaDetectada && !cropActivo && !resAdbActiva && !wmSizeActivo;
		RevertirWmHabilitado = wmSizeActivo;
		CropActivoWarningVisible = cropActivo;
		AdbActivaWarningVisible = resAdbActiva;
		RequiereResolucionVisible = !resolucionNativaDetectada;
	}

	[RelayCommand]
	private async Task DetectarResolucionAsync()
	{
		if (!base.S.HayDispositivo)
		{
			ResStatus = "Conecta un dispositivo para detectar la resolución.";
			ToastService.Mostrar("Conecta un dispositivo para detectar la resolución.", ToastTipo.Advertencia);
			return;
		}
		ResStatus = "Detectando...";
		var (flag, resolucionAncho, resolucionAlto, text) = await base.S.Adb.DetectarResolucionAsync();
		if (flag)
		{
			base.S.ResolucionAncho = resolucionAncho;
			base.S.ResolucionAlto = resolucionAlto;
			base.S.ResolucionNativaDetectada = true;
			ResStatus = "✓ " + text;
			ActualizarEstados();
			ToastService.Mostrar("Resolución detectada correctamente.", ToastTipo.Exito);
		}
		else
		{
			ResStatus = "⚠ " + text;
			ToastService.Mostrar("No se pudo detectar la resolución del dispositivo.", ToastTipo.Advertencia);
		}
	}

	private async Task DetectarResolucionAlCargarAsync()
	{
		ResStatus = "Detectando...";
		var (flag, resolucionAncho, resolucionAlto, text) = await base.S.Adb.DetectarResolucionAsync();
		if (flag)
		{
			base.S.ResolucionAncho = resolucionAncho;
			base.S.ResolucionAlto = resolucionAlto;
			base.S.ResolucionNativaDetectada = true;
			ResStatus = "✓ " + text;
			ActualizarEstados();
		}
		else
		{
			ResStatus = "No detectado; conecta el cable y presiona el botón de detección.";
		}
	}

	[RelayCommand]
	private async Task CalcularCropAsync()
	{
		if (base.S.ResolucionAncho == 0 || base.S.ResolucionAlto == 0)
		{
			await DialogService.AdvertenciaAsync("Resolución no detectada", "Detecta a resolução nativa do aparelho.");
			return;
		}
		double num = base.S.ObtenerAspectRatioValor();
		if (!(num <= 0.0))
		{
			int num2 = (int)((double)base.S.ResolucionAncho * num);
			if (base.S.ResolucionAlto > num2)
			{
				int value = (base.S.ResolucionAlto - num2) / 2;
				base.S.FullscreenCrop = $"{base.S.ResolucionAncho}:{num2}:0:{value}";
				base.S.CropActivo = true;
				CropAplicadoTexto = "✓ Crop activo: " + base.S.FullscreenCrop;
				ActualizarEstados();
				await DialogService.InfoAsync("Crop calculado", $"Crop para {base.S.AspectRatio}:\n{base.S.FullscreenCrop}\n\nAlto ideal: {num2}px · Offset: {value}px");
			}
			else
			{
				base.S.FullscreenCrop = "";
				CropAplicadoTexto = "Tu resolución ya es " + base.S.AspectRatio + ", no se necesita crop";
			}
		}
	}

	[RelayCommand]
	private void RestablecerCrop()
	{
		base.S.FullscreenCrop = "";
		base.S.CropActivo = false;
		CropAplicadoTexto = "Sin crop aplicado";
		ActualizarEstados();
	}

	[RelayCommand]
	private async Task AplicarResolucionAsync()
	{
		if (!base.S.AvisoAdbVisto)
		{
			var (flag, flag2) = await DialogService.AvanzadoAsync("Resolución ADB", "Esta opción modifica la resolución física del dispositivo vía ADB. El comportamiento puede variar según el dispositivo: algunos fabricantes pueden bloquear este comando y en ciertos dispositivos puede causar caída de FPS o latencia. Si algo queda distorsionado, usa el botón Resetear.", new string[3] { "Entiendo que puede afectar el rendimiento de mi dispositivo", "Sé cómo usar el botón Resetear si algo sale mal", "Usaré conexión USB al probar por primera vez" });
			if (!flag)
			{
				return;
			}
			if (flag2)
			{
				base.S.AvisoAdbVisto = true;
				base.S.GuardarConfig();
			}
		}
		double num = base.S.ObtenerAspectRatioValor();
		if (num <= 0.0)
		{
			return;
		}
		if (base.S.ResolucionAncho == 0 || base.S.ResolucionAlto == 0)
		{
			AdbStatus = "⚠ Detecta la resolución nativa antes de aplicar.";
			return;
		}
		AdbStatus = "Aplicando...";
		var (flag3, text, text2) = await base.S.Adb.AplicarResolucionAsync(base.S.ResolucionAncho, base.S.ResolucionAlto, num);
		if (flag3)
		{
			AdbStatus = "✓ Resolución aplicada: " + text;
			base.S.ResAdbActiva = true;
			ActualizarEstados();
			ToastService.Mostrar("Resolución aplicada: " + text, ToastTipo.Exito);
		}
		else
		{
			bool flag4 = text2.Contains("WRITE_SECURE_SETTINGS") || text2.Contains("SecurityException");
			AdbStatus = (flag4 ? "⚠ Tu dispositivo no permite cambiar la resolución vía ADB." : ("✗ Error: " + text2));
			ToastService.Mostrar(flag4 ? "Tu dispositivo no permite cambiar la resolución vía ADB." : "No se pudo aplicar la resolución.", ToastTipo.Error);
		}
	}

	[RelayCommand]
	private async Task ResetearResolucionAsync()
	{
		(bool, string) obj = await base.S.Adb.ResetearResolucionAsync();
		bool item = obj.Item1;
		string item2 = obj.Item2;
		AdbStatus = (item ? "✓ Resolución restaurada" : ("✗ " + item2));
		if (item)
		{
			base.S.ResAdbActiva = false;
			ActualizarEstados();
			ToastService.Mostrar("Resolución restaurada.", ToastTipo.Exito);
		}
		else
		{
			ToastService.Mostrar(string.IsNullOrWhiteSpace(item2) ? "No se pudo restaurar la resolución." : item2, ToastTipo.Error);
		}
	}

	[RelayCommand]
	private async Task AplicarWmAsync()
	{
		string valor = (base.S.WmSizeValor ?? "").Trim();
		if (string.IsNullOrEmpty(valor) || !Regex.IsMatch(valor, "^\\d+x\\d+$"))
		{
			WmStatus = "⚠ Formato inválido. Usa: 1280x720";
			return;
		}
		if (!base.S.AvisoWmSizeVisto)
		{
			var (flag, flag2) = await DialogService.AvanzadoAsync("Resolución Personalizada (Función Avanzada)", "Esta opción fuerza una resolución personalizada en el dispositivo vía ADB. Puede causar desalineación del toque/clic respecto a los elementos en pantalla. Algunos fabricantes pueden bloquear este comando. Los cambios se revierten automáticamente al detener o cerrar la app.", new string[3] { "Entiendo que el toque puede desalinearse en algunos dispositivos", "Sé cómo usar el botón Revertir si algo sale mal", "Usaré conexión USB al probar por primera vez" });
			if (!flag)
			{
				return;
			}
			if (flag2)
			{
				base.S.AvisoWmSizeVisto = true;
				base.S.GuardarConfig();
			}
		}
		WmStatus = "Aplicando...";
		var (flag3, text) = await base.S.Adb.AplicarWmSizePersonalizadaAsync(valor);
		if (flag3)
		{
			base.S.WmSizeActivo = true;
			base.S.WmSizeValor = valor;
			base.S.FullscreenCrop = "";
			base.S.ResAdbActiva = false;
			ActualizarEstados();
			WmStatus = "✓ Aplicado";
			ToastService.Mostrar("Resolución personalizada aplicada: " + valor, ToastTipo.Exito);
		}
		else
		{
			string text2 = (string.IsNullOrEmpty(text) ? "Sin dispositivo conectado" : text);
			WmStatus = "✗ " + text2;
			ToastService.Mostrar("No se pudo aplicar la resolución personalizada: " + text2, ToastTipo.Error);
		}
	}

	[RelayCommand]
	private async Task RevertirWmAsync()
	{
		bool item = (await base.S.Adb.ResetearResolucionAsync()).Item1;
		if (item)
		{
			base.S.WmSizeActivo = false;
		}
		WmStatus = (item ? "✓ Revertido" : "Sin dispositivo conectado");
		ActualizarEstados();
		ToastService.Mostrar(item ? "Resolución personalizada revertida." : "Sin dispositivo conectado para revertir.", (!item) ? ToastTipo.Advertencia : ToastTipo.Exito);
	}

	private async Task DetectarDpiAlCargarAsync()
	{
		var (flag, num, _) = await base.S.Adb.DetectarDPIAsync();
		if (flag)
		{
			base.S.Dpi = num;
			DpiActualTexto = $"DPI actual: {num}";
		}
	}

	[RelayCommand]
	private async Task DetectarDpiAsync()
	{
		var (flag, num, _) = await base.S.Adb.DetectarDPIAsync();
		if (flag)
		{
			base.S.Dpi = num;
			DpiActualTexto = $"DPI actual: {num}";
		}
		else
		{
			DpiActualTexto = "DPI actual: No detectado";
		}
	}

	[RelayCommand]
	private async Task AplicarDpiAsync()
	{
		if (!base.S.HayDispositivo)
		{
			ToastService.Mostrar("Conecta un dispositivo primero.", ToastTipo.Advertencia);
		}
		else if (await DialogService.ConfirmarAsync("Confirmar", $"¿Aplicar DPI {base.S.Dpi}?", "Usa 'Resetear' si algo sale mal.", "Aplicar", "Cancelar"))
		{
			(bool, string, string) obj = await base.S.Adb.AplicarDPIAsync(base.S.Dpi);
			bool item = obj.Item1;
			string item2 = obj.Item2;
			string item3 = obj.Item3;
			DpiStatus = (item ? ("✓ " + item2) : ("✗ " + item2));
			if (item)
			{
				base.S.UltimoDpiAplicado = base.S.Dpi;
				base.S.DpiPendienteReset = base.S.Dpi;
				base.S.GuardarConfig();
				OnPropertyChanged("UltimoDpiVisible");
				OnPropertyChanged("UltimoDpiTexto");
				ToastService.Mostrar($"DPI aplicado: {base.S.Dpi}", ToastTipo.Exito);
			}
			else
			{
				ToastService.Mostrar(string.IsNullOrWhiteSpace(item3) ? item2 : item3, ToastTipo.Error);
			}
		}
	}

	[RelayCommand]
	private async Task ResetearDpiAsync()
	{
		if (!base.S.HayDispositivo)
		{
			ToastService.Mostrar("Conecta un dispositivo primero.", ToastTipo.Advertencia);
			return;
		}
		(bool, string, string) obj = await base.S.Adb.ResetearDPIAsync();
		bool item = obj.Item1;
		string item2 = obj.Item2;
		string item3 = obj.Item3;
		DpiStatus = (item ? ("✓ " + item2) : ("✗ " + item2));
		if (item)
		{
			base.S.DpiPendienteReset = 0;
			base.S.GuardarConfig();
			ToastService.Mostrar("DPI restablecido.", ToastTipo.Exito);
			var (flag, num, _) = await base.S.Adb.DetectarDPIAsync();
			if (flag)
			{
				base.S.Dpi = num;
				DpiActualTexto = $"DPI actual: {num}";
			}
		}
		else
		{
			ToastService.Mostrar(string.IsNullOrWhiteSpace(item3) ? item2 : item3, ToastTipo.Error);
		}
	}

	private void OnConectado(string serial)
	{
		SessionState.Marshal(delegate
		{
			base.S.ResolucionNativaDetectada = false;
			DpiActualTexto = "DPI actual: Detectando...";
			ResStatus = "Detectando...";
			ActualizarEstados();
			DetectarDpiAlCargarAsync();
			DetectarResolucionAlCargarAsync();
		});
	}

	private void OnDesconectado()
	{
		SessionState.Marshal(delegate
		{
			base.S.ResolucionNativaDetectada = false;
			DpiActualTexto = "DPI actual: Sin dispositivo";
			ResStatus = "Conecta un dispositivo para detectar la resolución.";
			OnPropertyChanged("ResAncho");
			OnPropertyChanged("ResAlto");
			ActualizarEstados();
		});
	}

	private void OnDpiActualizado(int dpi)
	{
		SessionState.Marshal(delegate
		{
			DpiActualTexto = $"DPI actual: {dpi}";
		});
	}

	private void OnResolucionActualizada(int w, int h)
	{
		SessionState.Marshal(delegate
		{
			OnPropertyChanged("ResAncho");
			OnPropertyChanged("ResAlto");
			ActualizarEstados();
		});
	}

	public void Dispose()
	{
		DesengancharSeccion();
		base.S.PropertyChanged -= OnSessionChanged;
		base.S.Adb.OnDispositivoConectado -= OnConectado;
		base.S.Adb.OnDispositivoDesconectado -= OnDesconectado;
		base.S.Adb.OnDpiActualizado -= OnDpiActualizado;
		base.S.Adb.OnResolucionActualizada -= OnResolucionActualizada;
	}
}


