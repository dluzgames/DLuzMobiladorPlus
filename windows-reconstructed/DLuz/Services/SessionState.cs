using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using IniParser;
using IniParser.Model;
using DLuz.Helpers;

namespace DLuz.Services;

public class SessionState : ObservableObject
{
	private enum ResultadoReparacion
	{
		Reparado,
		Fallo,
		NoDisponible
	}

	private struct FLASHWINFO
	{
		public uint cbSize;

		public nint hwnd;

		public uint dwFlags;

		public uint uCount;

		public uint dwTimeout;
	}

	private MapeadorService? _mapeador;

	private readonly string _configPath;

	[ObservableProperty]
	private bool _video = true;

	[ObservableProperty]
	private bool _audio = true;

	[ObservableProperty]
	private bool _audioDoble;

	[ObservableProperty]
	private string _audioCodec = "opus";

	[ObservableProperty]
	private int _audioBitrate = 128;

	[ObservableProperty]
	private int _fps = 90;

	[ObservableProperty]
	private int _bitrate = 32;

	[ObservableProperty]
	private int _maxSize = 1600;

	[ObservableProperty]
	private int _windowWidth;

	[ObservableProperty]
	private int _windowHeight;

	[ObservableProperty]
	private string _videoCodec = "h264";

	[ObservableProperty]
	private int _videoBuffer;

	[ObservableProperty]
	private bool _aceleracionHardware;

	[ObservableProperty]
	private int _audioBuffer = 50;

	[ObservableProperty]
	private bool _disableScreensaver;

	[ObservableProperty]
	private bool _keepActive;

	[ObservableProperty]
	private bool _turnScreenOff;

	[ObservableProperty]
	private bool _freeWindowResize;

	[ObservableProperty]
	private string _backgroundColorHex = "";

	[ObservableProperty]
	private string _shortcutMod = "lalt";

	[ObservableProperty]
	private bool _fullscreen;

	[ObservableProperty]
	private string _fullscreenCrop = "";

	[ObservableProperty]
	private int _resolucionAncho = 1080;

	[ObservableProperty]
	private int _resolucionAlto = 2400;

	[ObservableProperty]
	private string _aspectRatio = "16:9";

	[ObservableProperty]
	private int _customRatioW = 16;

	[ObservableProperty]
	private int _customRatioH = 9;

	[ObservableProperty]
	private int _dpi = 420;

	[ObservableProperty]
	private bool _printFps;

	[ObservableProperty]
	private bool _forwardAllClicks;

	[ObservableProperty]
	private bool _mostrarFlotante = true;

	[ObservableProperty]
	private bool _overlayFps = true;

	[ObservableProperty]
	private int _overlayEsquina = 2;

	[ObservableProperty]
	private bool _wmSizeActivo;

	[ObservableProperty]
	private string _wmSizeValor = "";

	[ObservableProperty]
	private bool _useAdvancedEncoder;

	[ObservableProperty]
	private string _videoEncoder = "";

	[ObservableProperty]
	private string _renderDriver = "";

	[ObservableProperty]
	private string _inputMode = "uhid";

	[ObservableProperty]
	private string _tecladoModo = "uhid";

	[ObservableProperty]
	private string _mouseModo = "uhid";

	[ObservableProperty]
	private string _gamepadModo = "disabled";

	[ObservableProperty]
	private int _pointerSpeed;

	[ObservableProperty]
	private bool _modoDebug;

	[ObservableProperty]
	private bool _modoDualExperimental;

	[ObservableProperty]
	private bool _modoOtg;

	[ObservableProperty]
	private string _otgSerial = "";

	[ObservableProperty]
	private bool _usarWifi;

	[ObservableProperty]
	private string _wifiIp = "";

	[ObservableProperty]
	private int _wifiPuerto = 5555;

	[ObservableProperty]
	private bool _wifiConectado;

	[ObservableProperty]
	private bool _puertoTcpActivo;

	[ObservableProperty]
	private bool _hayDispositivo;

	[ObservableProperty]
	private bool _hayUsbDispositivo;

	[ObservableProperty]
	private bool _cropActivo;

	[ObservableProperty]
	private bool _resAdbActiva;

	[ObservableProperty]
	private string _perfilSeleccionado = "";

	[ObservableProperty]
	private bool _mapeadorActivo;

	[ObservableProperty]
	private bool _dlss5Modo = false;

	[ObservableProperty]
	private string _dlss5Preset = "Equilibrado";

	[ObservableProperty]
	private bool _pantallaVirtualDex;

	[ObservableProperty]
	private string _pantallaVirtualResolucion = "1920x1080";

	[ObservableProperty]
	private bool _mapeadorVd;

	[ObservableProperty]
	private string _mapeadorVdJuego = "com.dts.freefiremax";

	[ObservableProperty]
	private string _mapeadorVdJuegoPersonalizado = "";

	[ObservableProperty]
	private string _mapeadorVdResolucion = "1920x1080";

	[ObservableProperty]
	private int _mapeadorVdDpi = 240;

	[ObservableProperty]
	private bool _mapeadorVdDpiEsCustom;

	[ObservableProperty]
	private string _mapeadorPerfilNombre = "";

	[ObservableProperty]
	private string _estadoTexto = "Verificando...";

	[ObservableProperty]
	private bool _resolucionNativaDetectada;

	[ObservableProperty]
	private EstadoDispositivo _estadoDetallado = EstadoDispositivo.Desconocido;

	[ObservableProperty]
	private string _serialDetallado = "";

	private bool _deteccionIniciada;

	private bool _smokeTestEjecutado;

	private bool _avisoTrackSuspendidoMostrado;

	private readonly object _lockReparacion = new object();

	private bool _reparacionIntentada;

	private bool _reparacionFueExitosa;

	private readonly object _lockNoAutorizado = new object();

	private readonly HashSet<string> _reconnectIntentados = new HashSet<string>();

	private readonly HashSet<string> _avisosNoAutorizado = new HashSet<string>();

	private readonly HashSet<string> _noAutorizadoEnCurso = new HashSet<string>();

	private bool _estadoNotificado;

	private bool _algunaVezNotificado;

	private const uint FLASHW_ALL = 3u;

	private const uint FLASHW_TIMERNOFG = 12u;

	[ObservableProperty]
	private bool _hayCambiosSinGuardar;

	[ObservableProperty]
	private bool _configDirty;

	internal bool _cargandoPerfil;

	private int _cargaSilenciosa;

	private bool _snapshotModoDualExperimental;

	private bool _snapshotModoCompatibilidad;

	private static readonly HashSet<string> _propiedadesPerfil = new HashSet<string>
	{
		"Video", "Audio", "AudioDoble", "AudioCodec", "AudioBitrate", "Fps", "Bitrate", "MaxSize", "WindowWidth", "WindowHeight",
		"VideoCodec", "VideoBuffer", "AudioBuffer", "DisableScreensaver", "KeepActive", "TurnScreenOff", "FreeWindowResize", "BackgroundColorHex", "ShortcutMod", "Fullscreen",
		"PrintFps", "ForwardAllClicks", "MostrarFlotante", "OverlayFps", "OverlayEsquina", "UseAdvancedEncoder", "VideoEncoder", "RenderDriver", "InputMode", "TecladoModo",
		"MouseModo", "GamepadModo", "AspectRatio", "CustomRatioW", "CustomRatioH", "WmSizeValor", "FullscreenCrop"
	};

	private static readonly Dictionary<string, SeccionPerfil> _propSeccion = new Dictionary<string, SeccionPerfil>
	{
		["Video"] = SeccionPerfil.Video,
		["Audio"] = SeccionPerfil.Video,
		["AudioDoble"] = SeccionPerfil.Video,
		["AudioCodec"] = SeccionPerfil.Video,
		["AudioBitrate"] = SeccionPerfil.Video,
		["Fps"] = SeccionPerfil.Video,
		["Bitrate"] = SeccionPerfil.Video,
		["MaxSize"] = SeccionPerfil.Video,
		["VideoCodec"] = SeccionPerfil.Video,
		["VideoBuffer"] = SeccionPerfil.Video,
		["AudioBuffer"] = SeccionPerfil.Video,
		["UseAdvancedEncoder"] = SeccionPerfil.Video,
		["VideoEncoder"] = SeccionPerfil.Video,
		["RenderDriver"] = SeccionPerfil.Video,
		["Fullscreen"] = SeccionPerfil.Pantalla,
		["WindowWidth"] = SeccionPerfil.Pantalla,
		["WindowHeight"] = SeccionPerfil.Pantalla,
		["AspectRatio"] = SeccionPerfil.Pantalla,
		["CustomRatioW"] = SeccionPerfil.Pantalla,
		["CustomRatioH"] = SeccionPerfil.Pantalla,
		["WmSizeValor"] = SeccionPerfil.Pantalla,
		["FullscreenCrop"] = SeccionPerfil.Pantalla,
		["DisableScreensaver"] = SeccionPerfil.Extras,
		["KeepActive"] = SeccionPerfil.Extras,
		["TurnScreenOff"] = SeccionPerfil.Extras,
		["FreeWindowResize"] = SeccionPerfil.Extras,
		["BackgroundColorHex"] = SeccionPerfil.Extras,
		["ShortcutMod"] = SeccionPerfil.Extras,
		["PrintFps"] = SeccionPerfil.Extras,
		["MostrarFlotante"] = SeccionPerfil.Extras,
		["OverlayFps"] = SeccionPerfil.Extras,
		["OverlayEsquina"] = SeccionPerfil.Extras,
		["InputMode"] = SeccionPerfil.Controles,
		["TecladoModo"] = SeccionPerfil.Controles,
		["MouseModo"] = SeccionPerfil.Controles,
		["GamepadModo"] = SeccionPerfil.Controles,
		["ForwardAllClicks"] = SeccionPerfil.Controles
	};

	[ObservableProperty]
	private bool _videoDirty;

	[ObservableProperty]
	private bool _pantallaDirty;

	[ObservableProperty]
	private bool _extrasDirty;

	[ObservableProperty]
	private bool _controlesDirty;

	[ObservableProperty]
	private bool _videoAvisoOculto;

	[ObservableProperty]
	private bool _pantallaAvisoOculto;

	[ObservableProperty]
	private bool _extrasAvisoOculto;

	[ObservableProperty]
	private bool _controlesAvisoOculto;

	private ScrcpyConfig _snapshot = new ScrcpyConfig();

	public static SessionState Instance { get; } = new SessionState();

	public ADBManager Adb { get; }

	public ScrcpyManager Scrcpy { get; }

	public PerfilManager Perfiles { get; }

	public PerfilMetaService PerfilesMeta { get; }

	public MapeadorService Mapeador => _mapeador ?? (_mapeador = new MapeadorService(this));

	public string AdbPath { get; }

	public bool UltimaSesionWifi { get; set; }

	public bool UltimaSesionOtg { get; set; }

	public int UltimoDpiAplicado { get; set; }

	public int UltimaVelocidadCursor { get; set; } = int.MinValue;

	public bool ResolucionPendienteReset { get; set; }

	public int DpiPendienteReset { get; set; }

	public bool AvisoAdbVisto { get; set; }

	public bool AvisoWmSizeVisto { get; set; }

	public bool FirstRunFinalizado { get; set; }

	public bool PrimerArranquePendiente { get; set; }

	public bool MostrarAvisoPrimerArranque { get; set; }

	public bool OptimizacionAceptada { get; set; }

	public Dictionary<string, bool> OptimizacionEstado { get; } = new Dictionary<string, bool>();

	public Dictionary<string, string> OptimizacionDatos { get; } = new Dictionary<string, string>();

	public List<string> EncodersDetectados { get; set; } = new List<string>();

	public List<string> EncodersDisplayLabels { get; set; } = new List<string>();

	public bool InicializacionCompleta { get; set; }

	public bool OperacionWifiEnCurso { get; set; }

	public bool PrimeraDeteccionCompletada { get; set; }

	public bool ScrcpyEstabaActivo { get; set; }

	public string GamepadPrevTeclado { get; set; } = "uhid";

	public string GamepadPrevMouse { get; set; } = "uhid";

	public string PerfilInicialNombre { get; private set; } = "";

	private bool DirtySilenciado
	{
		get
		{
			if (!_cargandoPerfil)
			{
				return _cargaSilenciosa > 0;
			}
			return true;
		}
	}

	public bool HayCambiosPerfil
	{
		get
		{
			if (!VideoDirty && !PantallaDirty && !ExtrasDirty)
			{
				return ControlesDirty;
			}
			return true;
		}
	}

	public bool HayCambiosConfig => ConfigDirty;

	public bool PuedeGuardadoRapido
	{
		get
		{
			if (!ConfigDirty)
			{
				if (HayCambiosPerfil)
				{
					return !string.IsNullOrEmpty(PerfilSeleccionado);
				}
				return false;
			}
			return true;
		}
	}

	public string ResumenCambios
	{
		get
		{
			List<string> list = new List<string>();
			if (VideoDirty)
			{
				list.Add("Video");
			}
			if (PantallaDirty)
			{
				list.Add("Pantalla");
			}
			if (ExtrasDirty)
			{
				list.Add("Extras");
			}
			if (ControlesDirty)
			{
				list.Add("Controles");
			}
			if (list.Count != 0)
			{
				return "Cambios pendientes en " + string.Join(", ", list);
			}
			return "";
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool Video
	{
		get
		{
			return _video;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_video, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Video);
				_video = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Video);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool Audio
	{
		get
		{
			return _audio;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_audio, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Audio);
				_audio = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Audio);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool AudioDoble
	{
		get
		{
			return _audioDoble;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_audioDoble, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AudioDoble);
				_audioDoble = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AudioDoble);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string AudioCodec
	{
		get
		{
			return _audioCodec;
		}
		[MemberNotNull("_audioCodec")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_audioCodec, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AudioCodec);
				_audioCodec = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AudioCodec);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int AudioBitrate
	{
		get
		{
			return _audioBitrate;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_audioBitrate, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AudioBitrate);
				_audioBitrate = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AudioBitrate);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int Fps
	{
		get
		{
			return _fps;
		}
		set
		{
			int fpsLimitado = Math.Clamp(value, 30, 240);
			if (!EqualityComparer<int>.Default.Equals(_fps, fpsLimitado))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Fps);
				_fps = fpsLimitado;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Fps);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int Bitrate
	{
		get
		{
			return _bitrate;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_bitrate, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Bitrate);
				_bitrate = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Bitrate);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int MaxSize
	{
		get
		{
			return _maxSize;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_maxSize, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.MaxSize);
				_maxSize = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.MaxSize);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int WindowWidth
	{
		get
		{
			return _windowWidth;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_windowWidth, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.WindowWidth);
				_windowWidth = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.WindowWidth);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int WindowHeight
	{
		get
		{
			return _windowHeight;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_windowHeight, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.WindowHeight);
				_windowHeight = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.WindowHeight);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string VideoCodec
	{
		get
		{
			return _videoCodec;
		}
		[MemberNotNull("_videoCodec")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_videoCodec, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.VideoCodec);
				_videoCodec = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.VideoCodec);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int VideoBuffer
	{
		get
		{
			return _videoBuffer;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_videoBuffer, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.VideoBuffer);
				_videoBuffer = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.VideoBuffer);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool AceleracionHardware
	{
		get
		{
			return _aceleracionHardware;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_aceleracionHardware, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AceleracionHardware);
				_aceleracionHardware = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AceleracionHardware);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int AudioBuffer
	{
		get
		{
			return _audioBuffer;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_audioBuffer, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AudioBuffer);
				_audioBuffer = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AudioBuffer);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool DisableScreensaver
	{
		get
		{
			return _disableScreensaver;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_disableScreensaver, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.DisableScreensaver);
				_disableScreensaver = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.DisableScreensaver);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool KeepActive
	{
		get
		{
			return _keepActive;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_keepActive, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.KeepActive);
				_keepActive = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.KeepActive);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool TurnScreenOff
	{
		get
		{
			return _turnScreenOff;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_turnScreenOff, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.TurnScreenOff);
				_turnScreenOff = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.TurnScreenOff);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool FreeWindowResize
	{
		get
		{
			return _freeWindowResize;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_freeWindowResize, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.FreeWindowResize);
				_freeWindowResize = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.FreeWindowResize);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string BackgroundColorHex
	{
		get
		{
			return _backgroundColorHex;
		}
		[MemberNotNull("_backgroundColorHex")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_backgroundColorHex, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.BackgroundColorHex);
				_backgroundColorHex = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.BackgroundColorHex);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string ShortcutMod
	{
		get
		{
			return _shortcutMod;
		}
		[MemberNotNull("_shortcutMod")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_shortcutMod, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ShortcutMod);
				_shortcutMod = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ShortcutMod);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool Fullscreen
	{
		get
		{
			return _fullscreen;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_fullscreen, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Fullscreen);
				_fullscreen = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Fullscreen);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string FullscreenCrop
	{
		get
		{
			return _fullscreenCrop;
		}
		[MemberNotNull("_fullscreenCrop")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_fullscreenCrop, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.FullscreenCrop);
				_fullscreenCrop = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.FullscreenCrop);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int ResolucionAncho
	{
		get
		{
			return _resolucionAncho;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_resolucionAncho, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ResolucionAncho);
				_resolucionAncho = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ResolucionAncho);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int ResolucionAlto
	{
		get
		{
			return _resolucionAlto;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_resolucionAlto, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ResolucionAlto);
				_resolucionAlto = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ResolucionAlto);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string AspectRatio
	{
		get
		{
			return _aspectRatio;
		}
		[MemberNotNull("_aspectRatio")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_aspectRatio, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AspectRatio);
				_aspectRatio = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AspectRatio);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int CustomRatioW
	{
		get
		{
			return _customRatioW;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_customRatioW, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CustomRatioW);
				_customRatioW = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CustomRatioW);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int CustomRatioH
	{
		get
		{
			return _customRatioH;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_customRatioH, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CustomRatioH);
				_customRatioH = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CustomRatioH);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int Dpi
	{
		get
		{
			return _dpi;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_dpi, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Dpi);
				_dpi = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Dpi);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool PrintFps
	{
		get
		{
			return _printFps;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_printFps, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PrintFps);
				_printFps = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PrintFps);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ForwardAllClicks
	{
		get
		{
			return _forwardAllClicks;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_forwardAllClicks, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ForwardAllClicks);
				_forwardAllClicks = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ForwardAllClicks);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool MostrarFlotante
	{
		get
		{
			return _mostrarFlotante;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_mostrarFlotante, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.MostrarFlotante);
				_mostrarFlotante = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.MostrarFlotante);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool OverlayFps
	{
		get
		{
			return _overlayFps;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_overlayFps, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.OverlayFps);
				_overlayFps = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.OverlayFps);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int OverlayEsquina
	{
		get
		{
			return _overlayEsquina;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_overlayEsquina, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.OverlayEsquina);
				_overlayEsquina = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.OverlayEsquina);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool WmSizeActivo
	{
		get
		{
			return _wmSizeActivo;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_wmSizeActivo, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.WmSizeActivo);
				_wmSizeActivo = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.WmSizeActivo);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string WmSizeValor
	{
		get
		{
			return _wmSizeValor;
		}
		[MemberNotNull("_wmSizeValor")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_wmSizeValor, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.WmSizeValor);
				_wmSizeValor = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.WmSizeValor);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool UseAdvancedEncoder
	{
		get
		{
			return _useAdvancedEncoder;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_useAdvancedEncoder, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.UseAdvancedEncoder);
				_useAdvancedEncoder = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.UseAdvancedEncoder);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string VideoEncoder
	{
		get
		{
			return _videoEncoder;
		}
		[MemberNotNull("_videoEncoder")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_videoEncoder, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.VideoEncoder);
				_videoEncoder = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.VideoEncoder);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string RenderDriver
	{
		get
		{
			return _renderDriver;
		}
		[MemberNotNull("_renderDriver")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_renderDriver, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.RenderDriver);
				_renderDriver = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.RenderDriver);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string InputMode
	{
		get
		{
			return _inputMode;
		}
		[MemberNotNull("_inputMode")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_inputMode, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.InputMode);
				_inputMode = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.InputMode);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string TecladoModo
	{
		get
		{
			return _tecladoModo;
		}
		[MemberNotNull("_tecladoModo")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_tecladoModo, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.TecladoModo);
				_tecladoModo = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.TecladoModo);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string MouseModo
	{
		get
		{
			return _mouseModo;
		}
		[MemberNotNull("_mouseModo")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_mouseModo, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.MouseModo);
				_mouseModo = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.MouseModo);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string GamepadModo
	{
		get
		{
			return _gamepadModo;
		}
		[MemberNotNull("_gamepadModo")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_gamepadModo, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.GamepadModo);
				_gamepadModo = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.GamepadModo);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int PointerSpeed
	{
		get
		{
			return _pointerSpeed;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_pointerSpeed, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PointerSpeed);
				_pointerSpeed = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PointerSpeed);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ModoDebug
	{
		get
		{
			return _modoDebug;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_modoDebug, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ModoDebug);
				_modoDebug = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ModoDebug);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ModoDualExperimental
	{
		get
		{
			return _modoDualExperimental;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_modoDualExperimental, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ModoDualExperimental);
				_modoDualExperimental = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ModoDualExperimental);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ModoOtg
	{
		get
		{
			return _modoOtg;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_modoOtg, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ModoOtg);
				_modoOtg = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ModoOtg);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string OtgSerial
	{
		get
		{
			return _otgSerial;
		}
		[MemberNotNull("_otgSerial")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_otgSerial, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.OtgSerial);
				_otgSerial = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.OtgSerial);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool UsarWifi
	{
		get
		{
			return _usarWifi;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_usarWifi, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.UsarWifi);
				_usarWifi = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.UsarWifi);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string WifiIp
	{
		get
		{
			return _wifiIp;
		}
		[MemberNotNull("_wifiIp")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_wifiIp, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.WifiIp);
				_wifiIp = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.WifiIp);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int WifiPuerto
	{
		get
		{
			return _wifiPuerto;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_wifiPuerto, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.WifiPuerto);
				_wifiPuerto = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.WifiPuerto);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool WifiConectado
	{
		get
		{
			return _wifiConectado;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_wifiConectado, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.WifiConectado);
				_wifiConectado = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.WifiConectado);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool PuertoTcpActivo
	{
		get
		{
			return _puertoTcpActivo;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_puertoTcpActivo, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PuertoTcpActivo);
				_puertoTcpActivo = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PuertoTcpActivo);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool HayDispositivo
	{
		get
		{
			return _hayDispositivo;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_hayDispositivo, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HayDispositivo);
				_hayDispositivo = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HayDispositivo);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool HayUsbDispositivo
	{
		get
		{
			return _hayUsbDispositivo;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_hayUsbDispositivo, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HayUsbDispositivo);
				_hayUsbDispositivo = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HayUsbDispositivo);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool CropActivo
	{
		get
		{
			return _cropActivo;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_cropActivo, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CropActivo);
				_cropActivo = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CropActivo);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ResAdbActiva
	{
		get
		{
			return _resAdbActiva;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_resAdbActiva, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ResAdbActiva);
				_resAdbActiva = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ResAdbActiva);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string PerfilSeleccionado
	{
		get
		{
			return _perfilSeleccionado;
		}
		[MemberNotNull("_perfilSeleccionado")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_perfilSeleccionado, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PerfilSeleccionado);
				_perfilSeleccionado = value;
				OnPerfilSeleccionadoChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PerfilSeleccionado);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool MapeadorActivo
	{
		get
		{
			return _mapeadorActivo;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_mapeadorActivo, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.MapeadorActivo);
				_mapeadorActivo = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.MapeadorActivo);
			}
		}
	}

	public bool Dlss5Modo
	{
		get => _dlss5Modo;
		set => SetProperty(ref _dlss5Modo, value);
	}

	public string Dlss5Preset
	{
		get => _dlss5Preset;
		set => SetProperty(ref _dlss5Preset, value);
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool PantallaVirtualDex
	{
		get
		{
			return _pantallaVirtualDex;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_pantallaVirtualDex, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PantallaVirtualDex);
				_pantallaVirtualDex = value;
				OnPantallaVirtualDexChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PantallaVirtualDex);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string PantallaVirtualResolucion
	{
		get
		{
			return _pantallaVirtualResolucion;
		}
		[MemberNotNull("_pantallaVirtualResolucion")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_pantallaVirtualResolucion, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PantallaVirtualResolucion);
				_pantallaVirtualResolucion = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PantallaVirtualResolucion);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool MapeadorVd
	{
		get
		{
			return _mapeadorVd;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_mapeadorVd, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.MapeadorVd);
				_mapeadorVd = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.MapeadorVd);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string MapeadorVdJuego
	{
		get
		{
			return _mapeadorVdJuego;
		}
		[MemberNotNull("_mapeadorVdJuego")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_mapeadorVdJuego, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.MapeadorVdJuego);
				_mapeadorVdJuego = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.MapeadorVdJuego);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string MapeadorVdJuegoPersonalizado
	{
		get
		{
			return _mapeadorVdJuegoPersonalizado;
		}
		[MemberNotNull("_mapeadorVdJuegoPersonalizado")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_mapeadorVdJuegoPersonalizado, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.MapeadorVdJuegoPersonalizado);
				_mapeadorVdJuegoPersonalizado = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.MapeadorVdJuegoPersonalizado);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string MapeadorVdResolucion
	{
		get
		{
			return _mapeadorVdResolucion;
		}
		[MemberNotNull("_mapeadorVdResolucion")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_mapeadorVdResolucion, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.MapeadorVdResolucion);
				_mapeadorVdResolucion = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.MapeadorVdResolucion);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int MapeadorVdDpi
	{
		get
		{
			return _mapeadorVdDpi;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_mapeadorVdDpi, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.MapeadorVdDpi);
				_mapeadorVdDpi = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.MapeadorVdDpi);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool MapeadorVdDpiEsCustom
	{
		get
		{
			return _mapeadorVdDpiEsCustom;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_mapeadorVdDpiEsCustom, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.MapeadorVdDpiEsCustom);
				_mapeadorVdDpiEsCustom = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.MapeadorVdDpiEsCustom);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string MapeadorPerfilNombre
	{
		get
		{
			return _mapeadorPerfilNombre;
		}
		[MemberNotNull("_mapeadorPerfilNombre")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_mapeadorPerfilNombre, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.MapeadorPerfilNombre);
				_mapeadorPerfilNombre = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.MapeadorPerfilNombre);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string EstadoTexto
	{
		get
		{
			return _estadoTexto;
		}
		[MemberNotNull("_estadoTexto")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_estadoTexto, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.EstadoTexto);
				_estadoTexto = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.EstadoTexto);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ResolucionNativaDetectada
	{
		get
		{
			return _resolucionNativaDetectada;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_resolucionNativaDetectada, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ResolucionNativaDetectada);
				_resolucionNativaDetectada = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ResolucionNativaDetectada);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public EstadoDispositivo EstadoDetallado
	{
		get
		{
			return _estadoDetallado;
		}
		set
		{
			if (!EqualityComparer<EstadoDispositivo>.Default.Equals(_estadoDetallado, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.EstadoDetallado);
				_estadoDetallado = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.EstadoDetallado);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string SerialDetallado
	{
		get
		{
			return _serialDetallado;
		}
		[MemberNotNull("_serialDetallado")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_serialDetallado, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SerialDetallado);
				_serialDetallado = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SerialDetallado);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool HayCambiosSinGuardar
	{
		get
		{
			return _hayCambiosSinGuardar;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_hayCambiosSinGuardar, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HayCambiosSinGuardar);
				_hayCambiosSinGuardar = value;
				OnHayCambiosSinGuardarChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HayCambiosSinGuardar);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ConfigDirty
	{
		get
		{
			return _configDirty;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_configDirty, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ConfigDirty);
				_configDirty = value;
				OnConfigDirtyChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ConfigDirty);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool VideoDirty
	{
		get
		{
			return _videoDirty;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_videoDirty, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.VideoDirty);
				_videoDirty = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.VideoDirty);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool PantallaDirty
	{
		get
		{
			return _pantallaDirty;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_pantallaDirty, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PantallaDirty);
				_pantallaDirty = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PantallaDirty);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ExtrasDirty
	{
		get
		{
			return _extrasDirty;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_extrasDirty, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ExtrasDirty);
				_extrasDirty = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ExtrasDirty);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ControlesDirty
	{
		get
		{
			return _controlesDirty;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_controlesDirty, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ControlesDirty);
				_controlesDirty = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ControlesDirty);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool VideoAvisoOculto
	{
		get
		{
			return _videoAvisoOculto;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_videoAvisoOculto, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.VideoAvisoOculto);
				_videoAvisoOculto = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.VideoAvisoOculto);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool PantallaAvisoOculto
	{
		get
		{
			return _pantallaAvisoOculto;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_pantallaAvisoOculto, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PantallaAvisoOculto);
				_pantallaAvisoOculto = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PantallaAvisoOculto);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ExtrasAvisoOculto
	{
		get
		{
			return _extrasAvisoOculto;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_extrasAvisoOculto, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ExtrasAvisoOculto);
				_extrasAvisoOculto = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ExtrasAvisoOculto);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ControlesAvisoOculto
	{
		get
		{
			return _controlesAvisoOculto;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_controlesAvisoOculto, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ControlesAvisoOculto);
				_controlesAvisoOculto = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ControlesAvisoOculto);
			}
		}
	}

	private SessionState()
	{
		AdbPath = ArquitecturaHelper.RutaAdb;
		_configPath = AppPaths.ConfigPath;
		Adb = new ADBManager(AdbPath);
		Scrcpy = new ScrcpyManager(AdbPath);
		Scrcpy.OnError += delegate(string titulo, string mensaje)
		{
			AvisoService.Info(titulo + ": " + mensaje, ToastTipo.Error);
		};
		Scrcpy.OnAvisoDetallado += delegate(string titulo, string mensaje, string detalle, bool urgente)
		{
			if (urgente)
			{
				AvisoService.Urgente(titulo, mensaje, detalle);
			}
			else
			{
				AvisoService.InfoConDiagnostico(titulo, mensaje, detalle);
			}
		};
		bool num = !File.Exists(AppPaths.PerfilesPath) && !File.Exists(AppPaths.PerfilesMetaPath);
		Perfiles = new PerfilManager(AppPaths.PerfilesPath);
		PerfilesMeta = new PerfilMetaService(AppPaths.PerfilesMetaPath);
		if (num)
		{
			SembrarPerfilesBaseSiVacio();
		}
		Perfiles.QuitarPerfilTecnico("Dual Experimental", "modo_dual_experimental");
		PerfilesMeta.Sincronizar(Perfiles.ListarPerfiles());
	}

	private void SembrarPerfilesBaseSiVacio()
	{
		if (Perfiles.ListarPerfiles().Count <= 0)
		{
			string text = PerfilesBase.LeerEmbebido();
			if (!string.IsNullOrWhiteSpace(text))
			{
				Perfiles.AgregarPerfilesFaltantes(text);
			}
		}
	}

	public string MapeadorVdJuegoEfectivo()
	{
		if (!(MapeadorVdJuego == "personalizado"))
		{
			return MapeadorVdJuego;
		}
		return (MapeadorVdJuegoPersonalizado ?? "").Trim();
	}

	public ScrcpyConfig ObtenerConfigActual()
	{
		return new ScrcpyConfig
		{
			Video = Video,
			Audio = Audio,
			AudioDoble = AudioDoble,
			AudioCodec = AudioCodec,
			AudioBitrate = AudioBitrate,
			Fps = Fps,
			Bitrate = Bitrate,
			MaxSize = MaxSize,
			WindowWidth = WindowWidth,
			WindowHeight = WindowHeight,
			VideoCodec = VideoCodec,
			VideoBuffer = VideoBuffer,
			AceleracionHardware = AceleracionHardware,
			AudioBuffer = AudioBuffer,
			DisableScreensaver = DisableScreensaver,
			KeepActive = KeepActive,
			TurnScreenOff = TurnScreenOff,
			FreeWindowResize = FreeWindowResize,
			BackgroundColorHex = BackgroundColorHex,
			ShortcutMod = ShortcutMod,
			Fullscreen = Fullscreen,
			FullscreenCrop = FullscreenCrop,
			ModoOtg = ModoOtg,
			OtgSerial = OtgSerial,
			UsarWifi = WifiConectado,
			WifiIp = WifiIp,
			WifiPuerto = WifiPuerto,
			ResolucionAncho = ResolucionAncho,
			ResolucionAlto = ResolucionAlto,
			AspectRatio = AspectRatio,
			CustomRatioW = CustomRatioW,
			CustomRatioH = CustomRatioH,
			Dpi = Dpi,
			PrintFps = ((PrintFps && MostrarFlotante) || OverlayFps),
			ForwardAllClicks = ForwardAllClicks,
			MostrarFlotante = MostrarFlotante,
			OverlayFps = OverlayFps,
			OverlayEsquina = OverlayEsquina,
			WmSizeActivo = WmSizeActivo,
			WmSizeValor = WmSizeValor,
			UseAdvancedEncoder = UseAdvancedEncoder,
			VideoEncoder = VideoEncoder,
			RenderDriver = RenderDriver,
			TecladoModo = TecladoModo,
			MouseModo = MouseModo,
			GamepadModo = GamepadModo,
			PointerSpeed = PointerSpeed,
			ModoDebug = ModoDebug,
			PantallaVirtualDex = PantallaVirtualDex,
			PantallaVirtualResolucion = PantallaVirtualResolucion
		};
	}

	public void CargarPerfilEnApp(ScrcpyConfig cfg)
	{
		if (cfg == null)
		{
			return;
		}
		BeginLoad();
		try
		{
			Video = cfg.Video;
			Audio = cfg.Audio;
			AudioDoble = cfg.AudioDoble;
			AudioCodec = cfg.AudioCodec ?? "opus";
			AudioBitrate = cfg.AudioBitrate;
			Fps = cfg.Fps;
			Bitrate = cfg.Bitrate;
			MaxSize = cfg.MaxSize;
			WindowWidth = cfg.WindowWidth;
			WindowHeight = cfg.WindowHeight;
			VideoCodec = cfg.VideoCodec;
			VideoBuffer = cfg.VideoBuffer;
			AudioBuffer = cfg.AudioBuffer;
			DisableScreensaver = cfg.DisableScreensaver;
			KeepActive = cfg.KeepActive;
			TurnScreenOff = cfg.TurnScreenOff;
			FreeWindowResize = cfg.FreeWindowResize;
			BackgroundColorHex = cfg.BackgroundColorHex ?? "";
			ShortcutMod = cfg.ShortcutMod;
			Fullscreen = cfg.Fullscreen;
			FullscreenCrop = cfg.FullscreenCrop ?? "";
			CropActivo = false;
			ModoOtg = false;
			Dpi = cfg.Dpi;
			PrintFps = cfg.PrintFps;
			ForwardAllClicks = cfg.ForwardAllClicks;
			MostrarFlotante = cfg.MostrarFlotante;
			OverlayFps = cfg.OverlayFps;
			OverlayEsquina = cfg.OverlayEsquina;
			WmSizeActivo = cfg.WmSizeActivo;
			WmSizeValor = cfg.WmSizeValor ?? "";
			UseAdvancedEncoder = cfg.UseAdvancedEncoder;
			VideoEncoder = cfg.VideoEncoder;
			RenderDriver = cfg.RenderDriver ?? "";
			InputMode = ModoEntradaDesde(cfg);
			TecladoModo = cfg.TecladoModo ?? "uhid";
			MouseModo = cfg.MouseModo ?? "uhid";
			GamepadModo = cfg.GamepadModo ?? "disabled";
			PointerSpeed = cfg.PointerSpeed;
			ValidarRenderDriverContraArquitectura();
		}
		finally
		{
			EndLoad(limpiarDirty: true, tomarSnapshot: true, limpiarConfig: false);
		}
	}

	private static string ModoEntradaDesde(ScrcpyConfig cfg)
	{
		string text = cfg.TecladoModo ?? "uhid";
		if (text != "disabled")
		{
			return text;
		}
		string text2 = cfg.MouseModo ?? "uhid";
		if (!(text2 != "disabled"))
		{
			return "uhid";
		}
		return text2;
	}

	public double ObtenerAspectRatioValor()
	{
		return AspectRatio switch
		{
			"16:9" => 1.7777777777777777, 
			"16:10" => 1.6, 
			"21:9" => 2.3333333333333335, 
			"18:9" => 2.0, 
			"4:3" => 1.3333333333333333, 
			"Personalizado" => (CustomRatioH > 0) ? ((double)CustomRatioW / (double)CustomRatioH) : 1.7777777777777777, 
			_ => 1.7777777777777777, 
		};
	}

	public bool ValidarRenderDriverContraArquitectura()
	{
		bool modoCompatibilidad = ArquitecturaHelper.ModoCompatibilidad;
		if (ArquitecturaHelper.EsRenderValido(RenderDriver, modoCompatibilidad))
		{
			return false;
		}
		RenderDriver = "";
		return true;
	}

	public void CargarConfig()
	{
		BeginLoad();
		if (!File.Exists(_configPath))
		{
			try
			{
				PrimerArranquePendiente = true;
				return;
			}
			finally
			{
				EndLoad(limpiarDirty: true, tomarSnapshot: true);
				TomarSnapshotConfig();
			}
		}
		try
		{
			IniData iniData = new FileIniDataParser().ReadFile(_configPath);
			bool flag = false;
			bool flag2 = false;
			string text = "";
			bool aceleracionHardware = false;
			bool flag3 = false;
			KeyDataCollection keyDataCollection = (iniData.Sections.ContainsSection("Tema") ? iniData["Tema"] : null);
			if (keyDataCollection != null)
			{
				if (keyDataCollection.ContainsKey("first_run"))
				{
					flag = string.Equals(keyDataCollection["first_run"], "false", StringComparison.OrdinalIgnoreCase);
				}
				if (keyDataCollection.ContainsKey("aviso_adb_visto"))
				{
					AvisoAdbVisto = ParseBool(keyDataCollection["aviso_adb_visto"], AvisoAdbVisto);
				}
				if (keyDataCollection.ContainsKey("aviso_wmsize_visto"))
				{
					AvisoWmSizeVisto = ParseBool(keyDataCollection["aviso_wmsize_visto"], AvisoWmSizeVisto);
				}
				if (keyDataCollection.ContainsKey("background_color"))
				{
					string text2 = keyDataCollection["background_color"] ?? "";
					if (Regex.IsMatch(text2, "^#?[0-9A-Fa-f]{6}$"))
					{
						BackgroundColorHex = (text2.StartsWith("#") ? text2 : ("#" + text2));
						text = BackgroundColorHex;
					}
				}
				string text3 = (keyDataCollection.ContainsKey("ultimo_perfil") ? keyDataCollection["ultimo_perfil"] : "");
				if (!string.IsNullOrEmpty(text3))
				{
					PerfilManager perfiles = Perfiles;
					if (perfiles != null && perfiles.ExistePerfil(text3))
					{
						flag2 = true;
						PerfilSeleccionado = text3;
						ScrcpyConfig scrcpyConfig = Perfiles.ObtenerPerfil(text3);
						if (scrcpyConfig != null)
						{
							if (string.IsNullOrEmpty(scrcpyConfig.BackgroundColorHex) && !string.IsNullOrEmpty(text))
							{
								scrcpyConfig.BackgroundColorHex = text;
							}
							aceleracionHardware = scrcpyConfig.AceleracionHardware;
							CargarPerfilEnApp(scrcpyConfig);
						}
					}
				}
			}
			FirstRunFinalizado = flag || flag2;
			PrimerArranquePendiente = !FirstRunFinalizado;
			if (iniData.Sections.ContainsSection("Dispositivo"))
			{
				KeyDataCollection keyDataCollection2 = iniData["Dispositivo"];
				if (keyDataCollection2.ContainsKey("resolucion_pendiente_reset"))
				{
					ResolucionPendienteReset = ParseBool(keyDataCollection2["resolucion_pendiente_reset"], ResolucionPendienteReset);
				}
				if (keyDataCollection2.ContainsKey("dpi_pendiente_reset") && int.TryParse(keyDataCollection2["dpi_pendiente_reset"], out var result) && result > 0)
				{
					DpiPendienteReset = result;
				}
			}
			KeyDataCollection keyDataCollection3 = (iniData.Sections.ContainsSection("Video") ? iniData["Video"] : null);
			string text4 = ((keyDataCollection3 != null && keyDataCollection3.ContainsKey("encoders_detectados")) ? keyDataCollection3["encoders_detectados"] : "");
			string text5 = ((keyDataCollection3 != null && keyDataCollection3.ContainsKey("encoders_display_labels")) ? keyDataCollection3["encoders_display_labels"] : "");
			EncodersDetectados = (string.IsNullOrWhiteSpace(text4) ? new List<string>() : (from x in text4.Split('|')
				where !string.IsNullOrWhiteSpace(x)
				select x).ToList());
			EncodersDisplayLabels = (string.IsNullOrWhiteSpace(text5) ? new List<string>() : (from x in text5.Split('|')
				where !string.IsNullOrWhiteSpace(x)
				select x).ToList());
			if (iniData.Sections.ContainsSection("Sesion"))
			{
				KeyDataCollection keyDataCollection4 = iniData["Sesion"];
				if (keyDataCollection4.ContainsKey("ultima_sesion_wifi"))
				{
					UltimaSesionWifi = ParseBool(keyDataCollection4["ultima_sesion_wifi"], UltimaSesionWifi);
				}
				if (keyDataCollection4.ContainsKey("ultima_sesion_otg"))
				{
					UltimaSesionOtg = ParseBool(keyDataCollection4["ultima_sesion_otg"], UltimaSesionOtg);
				}
				if (keyDataCollection4.ContainsKey("wifi_ip") && !string.IsNullOrEmpty(keyDataCollection4["wifi_ip"]))
				{
					WifiIp = keyDataCollection4["wifi_ip"];
				}
				if (keyDataCollection4.ContainsKey("wifi_puerto") && int.TryParse(keyDataCollection4["wifi_puerto"], out var result2) && result2 >= 1024 && result2 <= 65535)
				{
					WifiPuerto = result2;
				}
				if (keyDataCollection4.ContainsKey("otg_serial") && !string.IsNullOrEmpty(keyDataCollection4["otg_serial"]))
				{
					OtgSerial = keyDataCollection4["otg_serial"];
				}
				if (keyDataCollection4.ContainsKey("ultimo_dpi_aplicado") && int.TryParse(keyDataCollection4["ultimo_dpi_aplicado"], out var result3) && result3 > 0)
				{
					UltimoDpiAplicado = result3;
				}
				if (keyDataCollection4.ContainsKey("ultima_velocidad_cursor") && int.TryParse(keyDataCollection4["ultima_velocidad_cursor"], out var result4))
				{
					UltimaVelocidadCursor = result4;
				}
				if (keyDataCollection4.ContainsKey("modo_debug"))
				{
					ModoDebug = ParseBool(keyDataCollection4["modo_debug"], ModoDebug);
				}
				if (keyDataCollection4.ContainsKey("modo_dual_experimental"))
				{
					ModoDualExperimental = ParseBool(keyDataCollection4["modo_dual_experimental"], ModoDualExperimental);
				}
				if (keyDataCollection4.ContainsKey("modo_compatibilidad"))
				{
					ArquitecturaHelper.ModoCompatibilidad = ParseBool(keyDataCollection4["modo_compatibilidad"], ArquitecturaHelper.ModoCompatibilidad);
				}
				if (keyDataCollection4.ContainsKey("aceleracion_hardware"))
				{
					AceleracionHardware = ParseBool(keyDataCollection4["aceleracion_hardware"], AceleracionHardware);
					flag3 = true;
				}
				if (keyDataCollection4.ContainsKey("mapeador_perfil") && !string.IsNullOrWhiteSpace(keyDataCollection4["mapeador_perfil"]))
				{
					MapeadorPerfilNombre = keyDataCollection4["mapeador_perfil"];
				}
				if (keyDataCollection4.ContainsKey("dlss5_modo"))
				{
					Dlss5Modo = ParseBool(keyDataCollection4["dlss5_modo"], Dlss5Modo);
				}
				if (keyDataCollection4.ContainsKey("dlss5_preset") && !string.IsNullOrWhiteSpace(keyDataCollection4["dlss5_preset"]))
				{
					Dlss5Preset = keyDataCollection4["dlss5_preset"];
				}
				DLSS5Service.Inicializar(Dlss5Modo, Dlss5Preset);
			}
			if (!flag3)
			{
				AceleracionHardware = aceleracionHardware;
			}
			if (iniData.Sections.ContainsSection("optimizacion") && iniData["optimizacion"].ContainsKey("aceptado"))
			{
				OptimizacionAceptada = ParseBool(iniData["optimizacion"]["aceptado"], OptimizacionAceptada);
			}
			if (iniData.Sections.ContainsSection("optimizacion_estado"))
			{
				foreach (KeyData item in iniData["optimizacion_estado"])
				{
					if (!(item.KeyName == "aceptado") && bool.TryParse(item.Value, out var result5))
					{
						OptimizacionEstado[item.KeyName] = result5;
					}
				}
			}
			if (!iniData.Sections.ContainsSection("optimizacion_datos"))
			{
				return;
			}
			foreach (KeyData item2 in iniData["optimizacion_datos"])
			{
				OptimizacionDatos[item2.KeyName] = item2.Value ?? "";
			}
		}
		catch (Exception ex)
		{
			AppLogger.Error("SessionState.CargarConfig: fallo leyendo config.ini", ex);
		}
		finally
		{
			EndLoad(limpiarDirty: true, tomarSnapshot: true);
			TomarSnapshotConfig();
		}
	}

	public void GuardarConfig()
	{
		try
		{
			FileIniDataParser fileIniDataParser = new FileIniDataParser();
			IniData data = (File.Exists(_configPath) ? fileIniDataParser.ReadFile(_configPath) : new IniData());
			Reemplazar("Tema", delegate(IniData d)
			{
				d["Tema"]["ultimo_perfil"] = PerfilSeleccionado ?? "";
				d["Tema"]["first_run"] = (FirstRunFinalizado ? "false" : "true");
				d["Tema"]["aviso_adb_visto"] = AvisoAdbVisto.ToString().ToLower();
				d["Tema"]["aviso_wmsize_visto"] = AvisoWmSizeVisto.ToString().ToLower();
				d["Tema"]["background_color"] = BackgroundColorHex ?? "";
			});
			Reemplazar("Video", delegate(IniData d)
			{
				d["Video"]["encoders_detectados"] = string.Join("|", EncodersDetectados);
				d["Video"]["encoders_display_labels"] = string.Join("|", EncodersDisplayLabels);
			});
			Reemplazar("Dispositivo", delegate(IniData d)
			{
				d["Dispositivo"]["resolucion_pendiente_reset"] = (WmSizeActivo || ResAdbActiva).ToString().ToLower();
				d["Dispositivo"]["dpi_pendiente_reset"] = DpiPendienteReset.ToString();
			});
			Reemplazar("Sesion", delegate(IniData d)
			{
				d["Sesion"]["ultima_sesion_wifi"] = UltimaSesionWifi.ToString().ToLower();
				d["Sesion"]["ultima_sesion_otg"] = UltimaSesionOtg.ToString().ToLower();
				d["Sesion"]["wifi_ip"] = WifiIp ?? "";
				d["Sesion"]["wifi_puerto"] = WifiPuerto.ToString();
				d["Sesion"]["otg_serial"] = OtgSerial ?? "";
				d["Sesion"]["ultimo_dpi_aplicado"] = ((UltimoDpiAplicado > 0) ? UltimoDpiAplicado.ToString() : "");
				d["Sesion"]["ultima_velocidad_cursor"] = ((UltimaVelocidadCursor != int.MinValue) ? UltimaVelocidadCursor.ToString() : "");
				d["Sesion"]["modo_debug"] = ModoDebug.ToString().ToLower();
				d["Sesion"]["modo_dual_experimental"] = ModoDualExperimental.ToString().ToLower();
				d["Sesion"]["modo_compatibilidad"] = ArquitecturaHelper.ModoCompatibilidad.ToString().ToLower();
				d["Sesion"]["aceleracion_hardware"] = AceleracionHardware.ToString().ToLower();
				d["Sesion"]["mapeador_perfil"] = MapeadorPerfilNombre ?? "";
				d["Sesion"]["dlss5_modo"] = Dlss5Modo.ToString().ToLower();
				d["Sesion"]["dlss5_preset"] = Dlss5Preset ?? "Equilibrado";
			});
			fileIniDataParser.WriteFile(_configPath, data);
			TomarSnapshotConfig();
			ConfigDirty = false;
			void Reemplazar(string seccion, Action<IniData> poblar)
			{
				if (data.Sections.ContainsSection(seccion))
				{
					data.Sections.RemoveSection(seccion);
				}
				data.Sections.AddSection(seccion);
				poblar(data);
			}
		}
		catch (Exception ex)
		{
			AppLogger.Error("SessionState.GuardarConfig: fallo guardando config.ini", ex);
		}
	}

	public void AplicarPerfilInicialSiPrimerArranque()
	{
		if (!PrimerArranquePendiente)
		{
			return;
		}
		BeginLoad();
		PerfilManager perfiles = Perfiles;
		string text = ((perfiles != null && perfiles.ExistePerfil("Gaming USB")) ? "Gaming USB" : (Perfiles?.ListarPerfiles().FirstOrDefault() ?? ""));
		try
		{
			FirstRunFinalizado = true;
			PrimerArranquePendiente = false;
			ScrcpyConfig scrcpyConfig = (string.IsNullOrEmpty(text) ? null : Perfiles?.ObtenerPerfil(text));
			if (scrcpyConfig != null)
			{
				PerfilSeleccionado = text;
				PerfilInicialNombre = text;
				CargarPerfilEnApp(scrcpyConfig);
				MostrarAvisoPrimerArranque = true;
			}
		}
		finally
		{
			EndLoad(limpiarDirty: true, tomarSnapshot: true);
		}
		GuardarConfig();
	}

	public void GuardarEstadoOptimizacion()
	{
		try
		{
			FileIniDataParser fileIniDataParser = new FileIniDataParser();
			IniData iniData = (File.Exists(_configPath) ? fileIniDataParser.ReadFile(_configPath) : new IniData());
			if (iniData.Sections.ContainsSection("optimizacion_estado"))
			{
				iniData.Sections.RemoveSection("optimizacion_estado");
			}
			iniData.Sections.AddSection("optimizacion_estado");
			foreach (KeyValuePair<string, bool> item in OptimizacionEstado)
			{
				iniData["optimizacion_estado"][item.Key] = item.Value.ToString().ToLower();
			}
			if (iniData.Sections.ContainsSection("optimizacion_datos"))
			{
				iniData.Sections.RemoveSection("optimizacion_datos");
			}
			iniData.Sections.AddSection("optimizacion_datos");
			foreach (KeyValuePair<string, string> optimizacionDato in OptimizacionDatos)
			{
				iniData["optimizacion_datos"][optimizacionDato.Key] = optimizacionDato.Value ?? "";
			}
			fileIniDataParser.WriteFile(_configPath, iniData);
		}
		catch (Exception ex)
		{
			AppLogger.Error("SessionState.GuardarEstadoOptimizacion: fallo guardando config.ini", ex);
		}
	}

	public void GuardarAceptacionOptimizacion()
	{
		try
		{
			FileIniDataParser fileIniDataParser = new FileIniDataParser();
			IniData iniData = (File.Exists(_configPath) ? fileIniDataParser.ReadFile(_configPath) : new IniData());
			if (iniData.Sections.ContainsSection("optimizacion"))
			{
				iniData.Sections.RemoveSection("optimizacion");
			}
			iniData.Sections.AddSection("optimizacion");
			iniData["optimizacion"]["aceptado"] = OptimizacionAceptada.ToString().ToLower();
			fileIniDataParser.WriteFile(_configPath, iniData);
		}
		catch (Exception ex)
		{
			AppLogger.Error("SessionState.GuardarAceptacionOptimizacion: fallo guardando config.ini", ex);
		}
	}

	private static bool ParseBool(string? valor, bool defecto)
	{
		if (!bool.TryParse(valor, out var result))
		{
			return defecto;
		}
		return result;
	}

	public async Task IniciarDeteccionAsync()
	{
		if (!_deteccionIniciada)
		{
			_deteccionIniciada = true;
			Adb.OnDispositivoCambio += OnDispositivoCambio;
			Adb.OnDispositivoUsbCambio += OnDispositivoUsbCambio;
			Adb.OnEstadoDetalladoCambio += OnEstadoDetallado;
			Adb.OnTrackSuspendido += OnTrackSuspendido;
			if (await VerificarIntegridadAlArrancarAsync())
			{
				SmokeTestAdbAsync();
			}
			Adb.DetenerTrackDevices();
			await Adb.DesconectarTodoAsync();
			await Task.Delay(1500);
			if ((await Task.Run(() => Adb.ListarDispositivos())).Item2.Any((string s) => s.Contains(':')))
			{
				await Adb.DesconectarTodoAsync();
				await Task.Delay(500);
			}
			await ActualizarEstadoAsync(mostrarToast: true);
			if (!ArquitecturaHelper.ScrcpyDisponible())
			{
				ToastService.Mostrar("No se encontró scrcpy (" + (ArquitecturaHelper.ModoCompatibilidad ? "32 bits" : "64 bits") + "). Reinstala la aplicación.", ToastTipo.Error);
			}
			Adb.IntentarAutoConectarWsa();
			Adb.IniciarTrackDevices();
		}
	}

	public async Task ActualizarEstadoAsync(bool mostrarToast = false)
	{
		(bool, List<(string, EstadoDispositivo)>, string) obj = await Task.Run(() => Adb.ListarDispositivosDetallado());
		bool item = obj.Item1;
		List<(string, EstadoDispositivo)> item2 = obj.Item2;
		List<string> list = (item ? (from d in item2
			where d.Item2 == EstadoDispositivo.Conectado
			select d.Item1).ToList() : new List<string>());
		string serialNoAutorizado = (item ? ((from d in item2
			where d.Item2 == EstadoDispositivo.NoAutorizado
			select d.Item1).FirstOrDefault() ?? "") : "");
		if (list.Count > 0)
		{
			HayDispositivo = true;
			EstadoDetallado = EstadoDispositivo.Conectado;
			SerialDetallado = list[0];
			EstadoTexto = ((list.Count == 1) ? ("Conectado: " + list[0]) : $"{list.Count} dispositivos conectados");
			if (ResolucionPendienteReset)
			{
				await Task.Run(() => Adb.ResetearResolucion());
				WmSizeActivo = false;
				ResAdbActiva = false;
				ResolucionPendienteReset = false;
				GuardarConfig();
			}
			if (DpiPendienteReset != 0)
			{
				if ((await Task.Run(() => Adb.ResetearDPI())).Item1)
				{
					DpiPendienteReset = 0;
				}
				GuardarConfig();
			}
		}
		else if (serialNoAutorizado.Length > 0)
		{
			HayDispositivo = false;
			EstadoDetallado = EstadoDispositivo.NoAutorizado;
			SerialDetallado = serialNoAutorizado;
			EstadoTexto = "Conectado sin autorizar: " + serialNoAutorizado;
			ManejarNoAutorizadoAsync(serialNoAutorizado);
		}
		else
		{
			HayDispositivo = false;
			EstadoDetallado = EstadoDispositivo.Desconocido;
			SerialDetallado = "";
			EstadoTexto = "Sin dispositivo detectado";
		}
		InicializacionCompleta = true;
		if (mostrarToast)
		{
			if (serialNoAutorizado.Length == 0 || HayDispositivo)
			{
				ToastService.Mostrar(HayDispositivo ? "Aparelho conectado" : "Nenhum aparelho detectado", (!HayDispositivo) ? ToastTipo.Advertencia : ToastTipo.Exito, 2500);
			}
			_algunaVezNotificado = true;
			_estadoNotificado = HayDispositivo;
		}
	}

	private async Task<bool> VerificarIntegridadAlArrancarAsync()
	{
		string carpeta = Path.GetDirectoryName(AdbPath) ?? "";
		var (estadoIntegridad, list) = await Task.Run(() => IntegridadAdb.Verificar(carpeta));
		switch (estadoIntegridad)
		{
		case EstadoIntegridad.SinManifiesto:
			AppLogger.Warn("Integridad ADB: manifiesto embebido no disponible — caigo al chequeo de existencia.");
			return VerificarArchivosAdb();
		case EstadoIntegridad.Ok:
			return true;
		default:
			foreach (IntegridadAdb.ArchivoDanado item in list)
			{
				AppLogger.Error($"Integridad ADB: {item.Nombre} dañado. Esperado {item.HashEsperado}, encontrado {item.HashActual}.");
			}
			return await IntentarReparacionUnicaAsync("verificación de arranque") == ResultadoReparacion.Reparado;
		}
	}

	private async Task<ResultadoReparacion> IntentarReparacionUnicaAsync(string motivo)
	{
		lock (_lockReparacion)
		{
			if (_reparacionIntentada)
			{
				return ResultadoReparacion.NoDisponible;
			}
			_reparacionIntentada = true;
		}
		string carpeta = Path.GetDirectoryName(AdbPath) ?? "";
		AppLogger.Warn("Integridad ADB: reparación automática iniciada (" + motivo + ").");
		try
		{
			await Task.Run(delegate
			{
				Adb.CerrarDaemonLocal();
				IntegridadAdb.Reparar(carpeta);
			});
			var (estadoIntegridad, source) = await Task.Run(() => IntegridadAdb.Verificar(carpeta));
			if (estadoIntegridad == EstadoIntegridad.Ok)
			{
				_reparacionFueExitosa = true;
				AppLogger.Info("Integridad ADB: reparación completada y verificada (" + motivo + ").");
				AvisoService.Info("LyXel reparó automáticamente un componente dañado de ADB. Si este aviso se repite en días distintos, tu antivirus podría estar modificando los archivos: considera excluir la carpeta de DLuz.", ToastTipo.Exito);
				return ResultadoReparacion.Reparado;
			}
			string text = string.Join("\n", source.Select((IntegridadAdb.ArchivoDanado d) => $"{d.Nombre}: esperado {d.HashEsperado}, encontrado {d.HashActual}"));
			AppLogger.Error("Integridad ADB: el archivo volvió a diferir tras la reparación.\n" + text);
			AvisoService.Urgente("ADB vuelve a dañarse tras repararlo", "LyXel restauró los componentes de ADB, pero el archivo volvió a quedar dañado de inmediato. Lo más probable es que un antivirus lo esté modificando en cuanto se escribe.\n\nAgrega una exclusión para la carpeta de LyXel en tu antivirus (y restaura adb.exe desde la cuarentena si aparece ahí) y vuelve a abrir la aplicación.", "Motivo de la reparación: " + motivo + "\nRe-verificación tras reparar:\n" + text);
			return ResultadoReparacion.Fallo;
		}
		catch (UnauthorizedAccessException ex)
		{
			AppLogger.Error("Integridad ADB: reparación sin permisos de escritura", ex);
			AvisoService.Urgente("No se pudo reparar ADB (sin permisos)", "Un componente de ADB está dañado y LyXel no pudo reemplazarlo porque no tiene permisos de escritura en su carpeta de instalación (típico cuando está en Archivos de programa).\n\nCierra LyXel y ejecútalo una vez como administrador para completar la reparación, o reinstala la aplicación.", $"Motivo: {motivo}\nCarpeta: {carpeta}\n{ex.GetType().Name}: {ex.Message}");
			return ResultadoReparacion.Fallo;
		}
		catch (Exception ex2)
		{
			AppLogger.Error("Integridad ADB: la reparación falló", ex2);
			AvisoService.Urgente("No se pudo reparar ADB", "Un componente de ADB está dañado y la reparación automática falló al escribir el archivo. Puede ser un bloqueo del antivirus o un problema de disco.\n\nReinstala LyXel; si el problema persiste, revisa tu antivirus.", $"Motivo: {motivo}\nCarpeta: {carpeta}\n{ex2.GetType().Name}: {ex2.Message}");
			return ResultadoReparacion.Fallo;
		}
	}

	private bool VerificarArchivosAdb()
	{
		string carpeta = Path.GetDirectoryName(AdbPath) ?? "";
		List<string> list = new string[3] { "adb.exe", "AdbWinApi.dll", "AdbWinUsbApi.dll" }.Where((string n) => !File.Exists(Path.Combine(carpeta, n))).ToList();
		if (list.Count == 0)
		{
			return true;
		}
		string text = string.Join(", ", list);
		AppLogger.Error($"Arranque: faltan archivos de ADB: {text} (carpeta: {carpeta})");
		AvisoService.Urgente("Faltan componentes de ADB", "No se encontró: " + text + ".\n\nLa causa más común es un antivirus que puso los archivos en cuarentena. Restáuralos desde tu antivirus o reinstala LyXel para recuperarlos. Mientras tanto, la detección de dispositivos no va a funcionar.", "Archivos faltantes: " + text + "\nCarpeta esperada: " + carpeta);
		return false;
	}

	private async Task SmokeTestAdbAsync()
	{
		if (_smokeTestEjecutado)
		{
			return;
		}
		_smokeTestEjecutado = true;
		var (flag, num, text, text2) = await Adb.VerificarVersionAsync();
		if (flag && text.StartsWith("Android Debug Bridge", StringComparison.Ordinal))
		{
			AppLogger.Info("ADB operativo: " + text.Split('\n')[0].Trim());
			return;
		}
		string detalle = $"Comando: adb version\nExit code: {num}\nSalida: {text.Trim()}\nStderr: {text2.Trim()}";
		if (num == -1073741819)
		{
			ResultadoReparacion r = await IntentarReparacionUnicaAsync("smoke test detectó binario dañado");
			if (r == ResultadoReparacion.Reparado)
			{
				var (flag2, _, text3, _) = await Adb.VerificarVersionAsync();
				if (flag2 && text3.StartsWith("Android Debug Bridge", StringComparison.Ordinal))
				{
					AppLogger.Info("ADB operativo tras la reparación: " + text3.Split('\n')[0].Trim());
					return;
				}
				AppLogger.Error("Integridad ADB: el binario verificado sigue fallando tras la reparación.");
			}
			if (r != ResultadoReparacion.Fallo)
			{
				MostrarUrgenteBinarioDanado(detalle);
			}
		}
		else
		{
			AppLogger.Warn("Smoke test de adb falló. " + detalle.Replace('\n', ' '));
			AvisoService.Info("ADB no respondió correctamente al iniciar. Si la detección de dispositivos falla, usa Reconectar ADB.");
		}
	}

	private void MostrarUrgenteBinarioDanado(string detalle)
	{
		if (_reparacionFueExitosa)
		{
			AvisoService.Urgente("ADB volvió a dañarse tras la reparación", "LyXel ya reparó el componente ADB en esta sesión, pero volvió a fallar. Lo más probable es que un antivirus lo esté modificando.\n\nAgrega una exclusión para la carpeta de LyXel en tu antivirus y vuelve a abrir la aplicación.", detalle);
		}
		else
		{
			AvisoService.Urgente("El componente ADB está dañado", "El programa que LyXel usa para comunicarse con tu teléfono (adb.exe) se cierra con un error apenas arranca. Las causas más comunes son un antivirus que modificó el archivo o una actualización interrumpida.\n\nReinstala LyXel para reparar el componente.", detalle);
		}
	}

	private async Task ManejarNoAutorizadoAsync(string serial)
	{
		lock (_lockNoAutorizado)
		{
			if (!_noAutorizadoEnCurso.Add(serial))
			{
				return;
			}
		}
		try
		{
			bool flag;
			lock (_lockNoAutorizado)
			{
				flag = _reconnectIntentados.Add(serial);
			}
			if (flag)
			{
				AppLogger.Info("ADB: dispositivo " + serial + " sin autorizar — reconnect automático para re-emitir el prompt de depuración.");
				await Task.Run(() => Adb.Reconectar(serial));
				await Task.Delay(3000);
				var (flag2, source, _) = await Task.Run(() => Adb.ListarDispositivosDetallado());
				if (!flag2 || !source.Any<(string, EstadoDispositivo)>(((string serial, EstadoDispositivo estado) d) => d.serial == serial && d.estado == EstadoDispositivo.NoAutorizado))
				{
					return;
				}
			}
			bool flag3;
			lock (_lockNoAutorizado)
			{
				flag3 = _avisosNoAutorizado.Add(serial);
			}
			if (flag3)
			{
				AvisoService.Info("Tu teléfono está conectado, pero falta autorizar la depuración. Desbloquéalo y acepta el aviso 'Permitir depuración USB'. Si no aparece, toca Reconectar ADB en Inicio.");
			}
		}
		catch (Exception ex)
		{
			AppLogger.Error("ManejarNoAutorizado: fallo en el flujo de reconnect", ex);
		}
		finally
		{
			lock (_lockNoAutorizado)
			{
				_noAutorizadoEnCurso.Remove(serial);
			}
		}
	}

	private void OnEstadoDetallado(string serial, EstadoDispositivo estado)
	{
		if (OperacionWifiEnCurso)
		{
			return;
		}
		Marshal(delegate
		{
			EstadoDetallado = estado;
			SerialDetallado = serial;
			if (estado == EstadoDispositivo.Conectado)
			{
				if (ADBManager.EsSerialWifi(serial))
				{
					WifiConectado = true;
					UsarWifi = true;
				}
				lock (_lockNoAutorizado)
				{
					_avisosNoAutorizado.Remove(serial);
					return;
				}
			}
			if (!ModoOtg && InicializacionCompleta)
			{
				switch (estado)
				{
				case EstadoDispositivo.NoAutorizado:
					EstadoTexto = "Conectado sin autorizar: " + serial;
					ManejarNoAutorizadoAsync(serial);
					break;
				case EstadoDispositivo.Offline:
					EstadoTexto = "Aparelho offline: " + serial;
					AvisoService.Info("O aparelho aparece offline. Reconecte o cabo ou verifique o modo USB do celular (o modo 'Transferência de arquivos' costuma funcionar melhor).");
					break;
				case EstadoDispositivo.Recovery:
					EstadoTexto = "Dispositivo en recovery: " + serial;
					AvisoService.Info("El dispositivo está en modo recovery. LyXel podrá usarlo cuando Android arranque normalmente.", ToastTipo.Info);
					break;
				case EstadoDispositivo.Sideload:
					EstadoTexto = "Dispositivo en sideload: " + serial;
					AvisoService.Info("El dispositivo está en modo sideload (instalando un paquete). Espera a que termine.", ToastTipo.Info);
					break;
				}
			}
		});
	}

	private async void OnTrackSuspendido()
	{
		try
		{
			switch (await IntentarReparacionUnicaAsync("circuit breaker de track-devices"))
			{
			case ResultadoReparacion.Reparado:
				Adb.IntentarAutoConectarWsa();
			Adb.IniciarTrackDevices();
				return;
			case ResultadoReparacion.Fallo:
				return;
			}
			if (!_avisoTrackSuspendidoMostrado)
			{
				_avisoTrackSuspendidoMostrado = true;
				if (_reparacionFueExitosa)
				{
					AvisoService.Urgente("ADB volvió a dañarse tras la reparación", "LyXel ya reparó el componente ADB en esta sesión, pero la detección de dispositivos volvió a fallar repetidamente. Lo más probable es que un antivirus esté modificando los archivos.\n\nAgrega una exclusión para la carpeta de LyXel en tu antivirus y vuelve a abrir la aplicación.", "track-devices murió de inmediato 5 veces consecutivas después de una reparación exitosa. Busca [ADB-CRASH-NATIVO] en el log para los exit codes.");
				}
				else
				{
					AvisoService.Urgente("La detección de dispositivos se detuvo", "El componente ADB falló repetidamente al arrancar y LyXel dejó de reintentarlo para no saturar el sistema. Esto suele indicar un archivo dañado por el antivirus o una instalación incompleta.\n\nToca Reconectar ADB para reintentar; si vuelve a ocurrir, reinstala DLuz.", "track-devices murió de inmediato 5 veces consecutivas (circuit breaker activado). Busca [ADB-CRASH-NATIVO] en el log para los exit codes.");
				}
			}
		}
		catch (Exception ex)
		{
			AppLogger.Error("OnTrackSuspendido: fallo manejando la suspensión del track", ex);
		}
	}

	private async void NotificarEstadoDispositivo(bool conectado)
	{
		if (!InicializacionCompleta || (_algunaVezNotificado && _estadoNotificado == conectado))
		{
			return;
		}
		_algunaVezNotificado = true;
		_estadoNotificado = conectado;
		if (!conectado)
		{
			ToastService.Mostrar("Dispositivo desconectado. La conexión con el dispositivo se perdió.", ToastTipo.Advertencia);
			return;
		}
		string detalle = "Dispositivo listo para usar.";
		try
		{
			var (flag, list, _) = await Task.Run(() => Adb.ListarDispositivos());
			if (flag && list.Count > 1)
			{
				ToastService.Mostrar("Se detectaron varios dispositivos. Selecciona el que quieres usar.", ToastTipo.Advertencia, 3500);
				return;
			}
			if (flag && list.Count == 1)
			{
				detalle = "Se detectó: " + list[0];
			}
		}
		catch
		{
		}
		ToastService.Mostrar("Aparelho conectado. " + detalle, ToastTipo.Exito, 2800);
	}

	private void OnDispositivoCambio(bool hay)
	{
		if (OperacionWifiEnCurso)
		{
			return;
		}
		Marshal(delegate
		{
			bool flag = hay || ModoOtg;
			HayDispositivo = flag;
			if (!flag)
			{
				ResolucionNativaDetectada = false;
				if (WifiConectado)
				{
					WifiConectado = false;
					UsarWifi = false;
				}
			}
			if (hay)
			{
				EstadoTexto = "Aparelho conectado";
			}
			else if (!ModoOtg)
			{
				EstadoTexto = "Sin dispositivo detectado";
			}
			NotificarEstadoDispositivo(flag);
		});
	}

	private void OnDispositivoUsbCambio(bool hayUsb)
	{
		if (OperacionWifiEnCurso || !InicializacionCompleta || WifiConectado || ModoOtg)
		{
			return;
		}
		bool num = !PrimeraDeteccionCompletada;
		PrimeraDeteccionCompletada = true;
		if (num)
		{
			return;
		}
		Marshal(delegate
		{
			if (hayUsb)
			{
				if (!PuertoTcpActivo)
				{
					NotificarEstadoDispositivo(conectado: true);
				}
			}
			else if (ScrcpyEstabaActivo)
			{
				ScrcpyEstabaActivo = false;
				Window window = Application.Current?.MainWindow;
				if (window != null)
				{
					if (window.WindowState == WindowState.Minimized)
					{
						window.WindowState = WindowState.Normal;
					}
					FlashVentana(window);
				}
				_algunaVezNotificado = true;
				_estadoNotificado = false;
				ToastService.Mostrar("Dispositivo desconectado; si fue inesperado, repórtalo en Discord", ToastTipo.Advertencia, 5000);
			}
			else
			{
				NotificarEstadoDispositivo(conectado: false);
			}
		});
	}

	internal static void Marshal(Action accion)
	{
		Dispatcher dispatcher = Application.Current?.Dispatcher;
		if (dispatcher == null || dispatcher.CheckAccess())
		{
			accion();
		}
		else
		{
			dispatcher.BeginInvoke(accion);
		}
	}

	[DllImport("user32.dll")]
	private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

	private static void FlashVentana(Window win)
	{
		try
		{
			nint handle = new WindowInteropHelper(win).Handle;
			if (handle != IntPtr.Zero)
			{
				FLASHWINFO pwfi = new FLASHWINFO
				{
					cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<FLASHWINFO>(),
					hwnd = handle,
					dwFlags = 15u,
					uCount = 5u,
					dwTimeout = 0u
				};
				FlashWindowEx(ref pwfi);
			}
		}
		catch
		{
		}
	}

	protected override void OnPropertyChanged(PropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);
		if (!DirtySilenciado && e.PropertyName != null && _propiedadesPerfil.Contains(e.PropertyName))
		{
			HayCambiosSinGuardar = true;
			MarcarSeccionPorPropiedad(e.PropertyName);
		}
	}

	internal void BeginLoad()
	{
		_cargaSilenciosa++;
	}

	internal void EndLoad(bool limpiarDirty = false, bool tomarSnapshot = false, bool limpiarConfig = true)
	{
		if (_cargaSilenciosa > 0)
		{
			_cargaSilenciosa--;
		}
		if (limpiarDirty && _cargaSilenciosa <= 0)
		{
			if (limpiarConfig)
			{
				LimpiarIndicadorCambios();
			}
			LimpiarTodasSecciones();
			if (tomarSnapshot)
			{
				TomarSnapshot();
			}
		}
	}

	public void MarcarCambiosSinGuardar()
	{
		if (!DirtySilenciado)
		{
			HayCambiosSinGuardar = true;
		}
	}

	public void MarcarCambiosConfig()
	{
		if (!DirtySilenciado)
		{
			ConfigDirty = true;
		}
	}

	public void LimpiarIndicadorCambios()
	{
		ConfigDirty = false;
		HayCambiosSinGuardar = false;
	}

	public void TomarSnapshotConfig()
	{
		_snapshotModoDualExperimental = ModoDualExperimental;
		_snapshotModoCompatibilidad = ArquitecturaHelper.ModoCompatibilidad;
	}

	public void RevertirCambiosConfig()
	{
		BeginLoad();
		try
		{
			ModoDualExperimental = _snapshotModoDualExperimental;
			ArquitecturaHelper.ModoCompatibilidad = _snapshotModoCompatibilidad;
			ConfigDirty = false;
			RecalcularGlobalDirty();
			OnPropertyChanged("HayCambiosConfig");
			OnPropertyChanged("PuedeGuardadoRapido");
		}
		finally
		{
			if (_cargaSilenciosa > 0)
			{
				_cargaSilenciosa--;
			}
		}
	}

	public (bool exito, string error) GuardadoRapido()
	{
		bool hayCambiosPerfil = HayCambiosPerfil;
		bool configDirty = ConfigDirty;
		if (!hayCambiosPerfil && !configDirty)
		{
			return (exito: true, error: "");
		}
		ScrcpyConfig scrcpyConfig = null;
		string perfilSeleccionado = PerfilSeleccionado;
		if (hayCambiosPerfil)
		{
			if (string.IsNullOrEmpty(perfilSeleccionado))
			{
				return (exito: false, error: "No hay perfil seleccionado.");
			}
			scrcpyConfig = Perfiles.ObtenerPerfil(perfilSeleccionado);
			var (flag, item) = Perfiles.GuardarConfigEnPerfil(perfilSeleccionado, ObtenerConfigActual());
			if (!flag)
			{
				return (exito: false, error: item);
			}
		}
		if (configDirty)
		{
			GuardarConfig();
			ConfigDirty = false;
		}
		if (hayCambiosPerfil && scrcpyConfig != null)
		{
			PerfilesMeta.RegistrarGuardado(perfilSeleccionado, scrcpyConfig);
		}
		RecalcularGlobalDirty();
		return (exito: true, error: "");
	}

	public bool SeccionDirty(SeccionPerfil s)
	{
		return s switch
		{
			SeccionPerfil.Video => VideoDirty, 
			SeccionPerfil.Pantalla => PantallaDirty, 
			SeccionPerfil.Extras => ExtrasDirty, 
			_ => ControlesDirty, 
		};
	}

	public bool SeccionAvisoOculto(SeccionPerfil s)
	{
		return s switch
		{
			SeccionPerfil.Video => VideoAvisoOculto, 
			SeccionPerfil.Pantalla => PantallaAvisoOculto, 
			SeccionPerfil.Extras => ExtrasAvisoOculto, 
			_ => ControlesAvisoOculto, 
		};
	}

	private void SetSeccionDirty(SeccionPerfil s, bool dirty)
	{
		switch (s)
		{
		case SeccionPerfil.Video:
			VideoDirty = dirty;
			break;
		case SeccionPerfil.Pantalla:
			PantallaDirty = dirty;
			break;
		case SeccionPerfil.Extras:
			ExtrasDirty = dirty;
			break;
		default:
			ControlesDirty = dirty;
			break;
		}
		OnPropertyChanged("HayCambiosPerfil");
		OnPropertyChanged("PuedeGuardadoRapido");
	}

	private void SetSeccionOculto(SeccionPerfil s, bool oculto)
	{
		switch (s)
		{
		case SeccionPerfil.Video:
			VideoAvisoOculto = oculto;
			break;
		case SeccionPerfil.Pantalla:
			PantallaAvisoOculto = oculto;
			break;
		case SeccionPerfil.Extras:
			ExtrasAvisoOculto = oculto;
			break;
		default:
			ControlesAvisoOculto = oculto;
			break;
		}
	}

	private void MarcarSeccionPorPropiedad(string propertyName)
	{
		if (_propSeccion.TryGetValue(propertyName, out var value))
		{
			SetSeccionDirty(value, dirty: true);
			SetSeccionOculto(value, oculto: false);
			OnPropertyChanged("ResumenCambios");
		}
	}

	public void CerrarAvisoSeccion(SeccionPerfil s)
	{
		SetSeccionOculto(s, oculto: true);
	}

	public void RevertirSeccion(SeccionPerfil s)
	{
		AplicarSeccion(_snapshot, s, sinDirty: true);
		SetSeccionDirty(s, dirty: false);
		RecalcularGlobalDirty();
		OnPropertyChanged("ResumenCambios");
	}

	public void RestaurarPredeterminadoSeccion(SeccionPerfil s)
	{
		AplicarSeccion(new ScrcpyConfig(), s, sinDirty: false);
		OnPropertyChanged("ResumenCambios");
	}

	public (bool exito, string error) GuardarSecciones()
	{
		(bool exito, string error) result = GuardadoRapido();
		if (result.exito)
		{
			LimpiarTodasSecciones();
			TomarSnapshot();
		}
		return result;
	}

	internal void TomarSnapshot()
	{
		_snapshot = ObtenerConfigActual();
	}

	internal void LimpiarTodasSecciones()
	{
		bool flag = (ControlesDirty = false);
		bool flag3 = (ExtrasDirty = flag);
		bool videoDirty = (PantallaDirty = flag3);
		VideoDirty = videoDirty;
		flag = (ControlesAvisoOculto = false);
		flag3 = (ExtrasAvisoOculto = flag);
		videoDirty = (PantallaAvisoOculto = flag3);
		VideoAvisoOculto = videoDirty;
		RecalcularGlobalDirty();
		OnPropertyChanged("HayCambiosPerfil");
		OnPropertyChanged("PuedeGuardadoRapido");
		OnPropertyChanged("ResumenCambios");
	}

	private void RecalcularGlobalDirty()
	{
		HayCambiosSinGuardar = ConfigDirty || VideoDirty || PantallaDirty || ExtrasDirty || ControlesDirty;
	}

	private void AplicarSeccion(ScrcpyConfig src, SeccionPerfil s, bool sinDirty)
	{
		bool cargandoPerfil = _cargandoPerfil;
		if (sinDirty)
		{
			_cargandoPerfil = true;
		}
		try
		{
			switch (s)
			{
			case SeccionPerfil.Video:
				Video = src.Video;
				Audio = src.Audio;
				AudioDoble = src.AudioDoble;
				AudioCodec = src.AudioCodec ?? "opus";
				AudioBitrate = src.AudioBitrate;
				Fps = src.Fps;
				Bitrate = src.Bitrate;
				MaxSize = src.MaxSize;
				VideoCodec = src.VideoCodec;
				VideoBuffer = src.VideoBuffer;
				AudioBuffer = src.AudioBuffer;
				UseAdvancedEncoder = src.UseAdvancedEncoder;
				VideoEncoder = src.VideoEncoder;
				RenderDriver = src.RenderDriver ?? "";
				break;
			case SeccionPerfil.Pantalla:
				Fullscreen = src.Fullscreen;
				WindowWidth = src.WindowWidth;
				WindowHeight = src.WindowHeight;
				AspectRatio = src.AspectRatio;
				CustomRatioW = src.CustomRatioW;
				CustomRatioH = src.CustomRatioH;
				WmSizeValor = src.WmSizeValor ?? "";
				FullscreenCrop = src.FullscreenCrop ?? "";
				break;
			case SeccionPerfil.Extras:
				DisableScreensaver = src.DisableScreensaver;
				KeepActive = src.KeepActive;
				TurnScreenOff = src.TurnScreenOff;
				FreeWindowResize = src.FreeWindowResize;
				BackgroundColorHex = src.BackgroundColorHex ?? "";
				ShortcutMod = src.ShortcutMod ?? "lalt";
				PrintFps = src.PrintFps;
				MostrarFlotante = src.MostrarFlotante;
				OverlayFps = src.OverlayFps;
				OverlayEsquina = src.OverlayEsquina;
				break;
			default:
				InputMode = ModoEntradaDesde(src);
				TecladoModo = src.TecladoModo ?? "uhid";
				MouseModo = src.MouseModo ?? "uhid";
				GamepadModo = src.GamepadModo ?? "disabled";
				ForwardAllClicks = src.ForwardAllClicks;
				break;
			}
		}
		finally
		{
			_cargandoPerfil = cargandoPerfil;
		}
	}

	public async Task MonitorearUsbConWifiAsync()
	{
		List<string> item = (await Task.Run(() => Adb.ListarDispositivos())).Item2;
		if (!WifiConectado)
		{
			return;
		}
		HayUsbDispositivo = item.Any((string s) => !s.Contains(':'));
		while (WifiConectado)
		{
			await Task.Delay(2000);
			if (!WifiConectado)
			{
				break;
			}
			if (MapeadorActivo || Scrcpy.EstaCorriendo)
			{
				await Task.Delay(6000);
				continue;
			}
			List<string> item2 = (await Task.Run(() => Adb.ListarDispositivos())).Item2;
			if (!WifiConectado)
			{
				break;
			}
			bool hayUsb = item2.Any((string s) => !s.Contains(':'));
			if (hayUsb == HayUsbDispositivo)
			{
				continue;
			}
			HayUsbDispositivo = hayUsb;
			Marshal(delegate
			{
				if (WifiConectado)
				{
					if (hayUsb)
					{
						ToastService.Mostrar("Cable USB detectado. Para volver a modo USB, ve a la sección Conexión y cierra el puerto.", ToastTipo.Info, 5000);
					}
					else
					{
						ToastService.Mostrar("Conectado por WiFi. Ya puedes usar la app sin cable.", ToastTipo.Exito, 4000);
					}
				}
			});
		}
		HayUsbDispositivo = false;
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnPerfilSeleccionadoChanged(string value)
	{
		OnPropertyChanged("PuedeGuardadoRapido");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnPantallaVirtualDexChanged(bool value)
	{
		if (value)
		{
			AppLogger.Info($"[VD-DEX] Modo activado: resolución {PantallaVirtualResolucion}, DPI {ScrcpyManager.DpiVirtualPara(PantallaVirtualResolucion)}.");
		}
		else
		{
			AppLogger.Info("[VD-DEX] Modo desactivado.");
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnHayCambiosSinGuardarChanged(bool value)
	{
		OnPropertyChanged("PuedeGuardadoRapido");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnConfigDirtyChanged(bool value)
	{
		RecalcularGlobalDirty();
		OnPropertyChanged("HayCambiosConfig");
		OnPropertyChanged("PuedeGuardadoRapido");
	}

	public CapturaMidiaService CapturaMidia { get; } = new CapturaMidiaService();
	public bool PollingRate1ms { get; set; } = true;
	public int MousePollingRateHz { get; set; } = 1000;

	private string _mapeadorOrientacao = "@270";
	public string MapeadorOrientacao
	{
		get => _mapeadorOrientacao ?? "@270";
		set
		{
			if (_mapeadorOrientacao != value)
			{
				_mapeadorOrientacao = value;
				OnPropertyChanged(nameof(MapeadorOrientacao));
			}
		}
	}
}
