using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DLuz.Helpers;

namespace DLuz;

public class ScrcpyManager
{
	private readonly record struct EsperaVentana(long? VentanaMs, bool Murio);

	private struct SystemPowerStatus
	{
		public byte ACLineStatus;

		public byte BatteryFlag;

		public byte BatteryLifePercent;

		public byte SystemStatusFlag;

		public int BatteryLifeTime;

		public int BatteryFullLifeTime;
	}

	private struct ProcessPowerThrottlingState
	{
		public uint Version;

		public uint ControlMask;

		public uint StateMask;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	private struct PROCESSENTRY32
	{
		public uint dwSize;

		public uint cntUsage;

		public uint th32ProcessID;

		public nint th32DefaultHeapID;

		public uint th32ModuleID;

		public uint cntThreads;

		public uint th32ParentProcessID;

		public int pcPriClassBase;

		public uint dwFlags;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		public string szExeFile;
	}

	private readonly string _adbPath;

	private Process? _proceso;

	private Process? _procesoScrcpyDebug;

	private Process? _dualVisual;

	private Process? _dualEntrada;

	private Process? _dualVisualHost;

	private Process? _dualEntradaHost;

	private bool _cerrandoDual;

	private bool _modoDebugActivo;

	private bool _dualDebugActivo;

	private bool _busquedaScrcpyDebugFinalizada;

	private ScrcpyDualBenchmark? _ultimoBenchmarkDual;

	private readonly RegistroSesion _registro = new RegistroSesion();

	private readonly object _lockSalidaDual = new object();

	private bool _derriboAvisado;

	private const int EntradaTimeoutMs = 5000;

	private const int VisualTimeoutMs = 30000;

	private const int ValidacionInicialMs = 1200;

	private const int GraciaCierreDualMs = 1000;

	private const int RetrasoAvisoPostDerriboMs = 800;

	private const byte BatteryFlagSinBateria = 128;

	private const byte AcLineDesconectado = 0;

	private const int ProcessPowerThrottling = 4;

	private const uint PowerThrottlingCurrentVersion = 1u;

	private const uint PowerThrottlingExecutionSpeed = 1u;

	private static readonly Regex RegexFps = new Regex("(\\d+)\\s*fps(?:\\s*\\(\\+(\\d+)\\s+frames?\\s+skipped\\))?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private const uint TH32CS_SNAPPROCESS = 2u;

	private const uint SWP_NOSIZE = 1u;

	private const uint SWP_NOMOVE = 2u;

	private const uint SWP_NOZORDER = 4u;

	private const uint SWP_NOACTIVATE = 16u;

	private static readonly nint HWND_TOP = IntPtr.Zero;

	private static readonly nint INVALID_HANDLE_VALUE = new IntPtr(-1);

	public ScrcpyDualBenchmark? UltimoBenchmarkDual => _ultimoBenchmarkDual;

	public bool EstaCorriendo
	{
		get
		{
			if (ProcesoVivo(_dualVisual) || ProcesoVivo(_dualEntrada))
			{
				return true;
			}
			if (_modoDebugActivo)
			{
				Process procesoScrcpyDebug = _procesoScrcpyDebug;
				if (procesoScrcpyDebug != null)
				{
					return ProcesoVivo(procesoScrcpyDebug);
				}
				if (_busquedaScrcpyDebugFinalizada)
				{
					return false;
				}
			}
			return ProcesoVivo(_proceso);
		}
	}

	public int UltimoDisplayVirtualId { get; private set; } = -1;

	public static bool RendimientoAlto => EstadoRendimiento() == "alta";

	public event Action<string>? OnFpsUpdate;

	public event Action<string, string>? OnError;

	public event Action<string, string, string, bool>? OnAvisoDetallado;

	public ScrcpyManager(string adbPath)
	{
		_adbPath = adbPath;
	}

	public nint ObtenerHandleVentana()
	{
		if (ProcesoVivo(_dualVisual))
		{
			try
			{
				_dualVisual.Refresh();
				return _dualVisual.MainWindowHandle;
			}
			catch
			{
				return IntPtr.Zero;
			}
		}
		Process process = (_modoDebugActivo ? _procesoScrcpyDebug : _proceso);
		if (!ProcesoVivo(process))
		{
			return IntPtr.Zero;
		}
		try
		{
			process.Refresh();
			return process.MainWindowHandle;
		}
		catch
		{
			return IntPtr.Zero;
		}
	}

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

	public bool EsVentanaDeSesion(nint hwnd)
	{
		if (hwnd == IntPtr.Zero)
		{
			return false;
		}
		if (GetWindowThreadProcessId(hwnd, out var lpdwProcessId) == 0 || lpdwProcessId == 0)
		{
			return false;
		}
		Process[] array = new Process[4] { _proceso, _procesoScrcpyDebug, _dualVisual, _dualEntrada };
		foreach (Process process in array)
		{
			try
			{
				if (process != null && !process.HasExited && process.Id == (int)lpdwProcessId)
				{
					return true;
				}
			}
			catch
			{
			}
		}
		return false;
	}

	public bool Lanzar(ScrcpyConfig config, bool dualExperimental = false, DualLayout? layoutDual = null)
	{
		if (config.UsarWifi && string.IsNullOrEmpty(config.WifiIp))
		{
			return false;
		}
		if (dualExperimental && !config.ModoOtg)
		{
			if (config.PantallaVirtualDex)
			{
				try
				{
					AppLogger.Warn("[VD-DEX] Modo dual activo: la pantalla virtual se ignora en esta sesión.");
				}
				catch
				{
				}
			}
			return LanzarDualExperimental(config, layoutDual);
		}
		string text = ConstruirArgumentos(config);
		if (config.PantallaVirtualDex)
		{
			if (config.ModoOtg)
			{
				try
				{
					AppLogger.Warn("[VD-DEX] Modo OTG activo: la pantalla virtual se ignora (OTG no transmite video).");
				}
				catch
				{
				}
			}
			else if (!config.Video)
			{
				try
				{
					AppLogger.Warn("[VD-DEX] Video desactivado: la pantalla virtual no se emite.");
				}
				catch
				{
				}
			}
			else
			{
				try
				{
					AppLogger.Info($"[VD-DEX] Pantalla virtual activa: resolución {config.PantallaVirtualResolucion}, DPI {DpiVirtualPara(config.PantallaVirtualResolucion)}. Comando: scrcpy.exe {text}");
				}
				catch
				{
				}
			}
		}
		return LanzarProceso(text, config);
	}

	public static int DpiVirtualPara(string resolucion)
	{
		if (!(resolucion == "1280x720"))
		{
			if (resolucion == "2560x1440")
			{
				return 200;
			}
			return 150;
		}
		return 100;
	}

	public static string ConstruirArgumentos(ScrcpyConfig config)
	{
		List<string> list = new List<string>();
		if (!string.IsNullOrEmpty(config.ShortcutMod))
		{
			list.Add("--shortcut-mod=" + config.ShortcutMod);
		}
		if (config.ModoOtg)
		{
			list.Add("--otg");
			if (!string.IsNullOrEmpty(config.OtgSerial))
			{
				list.AddRange(new string[2] { "-s", config.OtgSerial });
			}
			else
			{
				list.Add("-d");
			}
			if (config.DisableScreensaver)
			{
				list.Add("--disable-screensaver");
			}
			list.AddRange(new string[2] { "--window-title", "DLuzMObi v2 - OTG" });
			return UnirArgumentos(list);
		}
		if (!string.IsNullOrWhiteSpace(config.SerialDestino))
		{
			list.AddRange(new string[2] { "-s", config.SerialDestino.Trim() });
		}
		else if (config.UsarWifi && !string.IsNullOrEmpty(config.WifiIp))
		{
			list.Add($"--tcpip={config.WifiIp}:{config.WifiPuerto}");
		}
		else
		{
			// Evita a ambiguidade quando o mesmo telefone aparece por USB e ADB sem fio.
			list.Add("-d");
		}
		list.AddRange(new string[2]
		{
			"--window-title",
			config.UsarWifi ? "DLuzMObi v2 - Wi-Fi" : "DLuzMObi v2"
		});
		if (!config.Video)
		{
			list.Add("--no-video");
		}
		else
		{
			list.Add($"--max-fps={config.Fps}");
			list.AddRange(new string[2]
			{
				"-b",
				$"{config.Bitrate}M"
			});
			if (config.PantallaVirtualDex)
			{
				int value = ((config.PantallaVirtualDpi > 0) ? config.PantallaVirtualDpi : DpiVirtualPara(config.PantallaVirtualResolucion));
				list.Add($"--new-display={config.PantallaVirtualResolucion}/{value}");
			}
			else
			{
				if (config.MaxSize > 0)
				{
					list.AddRange(new string[2]
					{
						"-m",
						config.MaxSize.ToString()
					});
				}
				if (!string.IsNullOrEmpty(config.FullscreenCrop))
				{
					list.Add("--crop=" + config.FullscreenCrop);
				}
				else
				{
					if (config.WindowWidth > 0)
					{
						list.Add($"--window-width={config.WindowWidth}");
					}
					if (config.WindowHeight > 0)
					{
						list.Add($"--window-height={config.WindowHeight}");
					}
				}
			}
			if (config.UseAdvancedEncoder && !string.IsNullOrWhiteSpace(config.VideoEncoder))
			{
				string text = InferirCodecDeEncoder(config.VideoEncoder);
				list.Add("--video-codec=" + text);
				list.Add("--video-encoder=" + config.VideoEncoder);
			}
			else if (!string.IsNullOrEmpty(config.VideoCodec))
			{
				list.Add("--video-codec=" + config.VideoCodec);
			}
			if (config.VideoBuffer > 0)
			{
				list.Add($"--video-buffer={config.VideoBuffer}");
			}
			if (config.Fullscreen && !config.PantallaVirtualDex)
			{
				list.Add("-f");
			}
			if (config.PrintFps)
			{
				list.Add("--print-fps");
			}
			if (!string.IsNullOrEmpty(config.RenderDriver) && !AceleracionAplicable(config))
			{
				list.Add("--render-driver=" + config.RenderDriver);
			}
		}
		if (!config.Audio)
		{
			list.Add("--no-audio");
		}
		else
		{
			if (config.AudioBuffer > 0)
			{
				list.Add($"--audio-buffer={config.AudioBuffer}");
			}
			if (config.AudioDoble)
			{
				list.Add("--audio-dup");
			}
			string text2 = (string.IsNullOrEmpty(config.AudioCodec) ? "opus" : config.AudioCodec);
			if (text2 != "opus")
			{
				list.Add("--audio-codec=" + text2);
			}
			if (config.AudioBitrate > 0 && config.AudioBitrate != 128)
			{
				list.Add($"--audio-bit-rate={config.AudioBitrate}K");
			}
		}
		if (config.SoloEspejoSinControl)
		{
			list.Add("--no-control");
		}
		else
		{
			list.Add("--keyboard=" + config.TecladoModo);
			list.Add("--mouse=" + config.MouseModo);
			if (config.GamepadModo == "uhid")
			{
				list.Add("--gamepad=uhid");
			}
			if (config.ForwardAllClicks)
			{
				list.Add("--mouse-bind=++++:++++");
			}
		}
		if (config.DisableScreensaver)
		{
			list.Add("--disable-screensaver");
		}
		if (config.KeepActive && !config.SoloEspejoSinControl)
		{
			list.Add("--keep-active");
		}
		if (config.TurnScreenOff && !config.SoloEspejoSinControl)
		{
			list.Add("--turn-screen-off");
		}
		if (config.Video && config.FreeWindowResize)
		{
			list.Add("--no-window-aspect-ratio-lock");
		}
		if (config.Video && !string.IsNullOrEmpty(config.BackgroundColorHex))
		{
			list.Add("--background-color=" + config.BackgroundColorHex);
		}
		return UnirArgumentos(list);
	}

	public static bool UsaInstanciaVisualExperimental(ScrcpyConfig config)
	{
		if (!config.Video)
		{
			return config.Audio;
		}
		return true;
	}

	public static bool UsaInstanciaEntradaExperimental(ScrcpyConfig config)
	{
		return RequiereEntrada(config);
	}

	public static string ConstruirArgumentosVisualesExperimental(ScrcpyConfig config, string serial)
	{
		return ConstruirArgumentosVisualesExperimental(config, serial, DualLayoutHelper.CalcularRespaldo());
	}

	public static string ConstruirArgumentosVisualesExperimental(ScrcpyConfig config, string serial, DualLayout layout)
	{
		List<string> list = new List<string>();
		AgregarDestino(list, config, serial);
		if (!string.IsNullOrEmpty(config.ShortcutMod))
		{
			list.Add("--shortcut-mod=" + config.ShortcutMod);
		}
		list.AddRange(new string[2] { "--window-title", "DLuzMObi v2 - Tela" });
		if (!config.Fullscreen && layout.PosicionDisponible)
		{
			list.Add($"--window-x={layout.VisualX}");
			list.Add($"--window-y={layout.VisualY}");
		}
		if (!config.Video)
		{
			list.Add("--no-video");
		}
		else
		{
			list.Add($"--max-fps={config.Fps}");
			list.AddRange(new string[2]
			{
				"-b",
				$"{config.Bitrate}M"
			});
			if (config.MaxSize > 0)
			{
				list.AddRange(new string[2]
				{
					"-m",
					config.MaxSize.ToString()
				});
			}
			if (!string.IsNullOrEmpty(config.FullscreenCrop))
			{
				list.Add("--crop=" + config.FullscreenCrop);
			}
			else if (config.WindowWidth > 0 || config.WindowHeight > 0)
			{
				if (config.WindowWidth > 0)
				{
					list.Add($"--window-width={config.WindowWidth}");
				}
				if (config.WindowHeight > 0)
				{
					list.Add($"--window-height={config.WindowHeight}");
				}
			}
			else if (!config.Fullscreen && layout.PosicionDisponible && layout.VisualAnchoSugerido > 0)
			{
				list.Add($"--window-width={layout.VisualAnchoSugerido}");
			}
			if (config.UseAdvancedEncoder && !string.IsNullOrWhiteSpace(config.VideoEncoder))
			{
				string text = InferirCodecDeEncoder(config.VideoEncoder);
				list.Add("--video-codec=" + text);
				list.Add("--video-encoder=" + config.VideoEncoder);
			}
			else if (!string.IsNullOrEmpty(config.VideoCodec))
			{
				list.Add("--video-codec=" + config.VideoCodec);
			}
			if (config.VideoBuffer > 0)
			{
				list.Add($"--video-buffer={config.VideoBuffer}");
			}
			if (config.Fullscreen)
			{
				list.Add("-f");
			}
			if (config.PrintFps)
			{
				list.Add("--print-fps");
			}
			if (!string.IsNullOrEmpty(config.RenderDriver) && !AceleracionAplicable(config))
			{
				list.Add("--render-driver=" + config.RenderDriver);
			}
			if (config.FreeWindowResize)
			{
				list.Add("--no-window-aspect-ratio-lock");
			}
			if (!string.IsNullOrEmpty(config.BackgroundColorHex))
			{
				list.Add("--background-color=" + config.BackgroundColorHex);
			}
		}
		if (!config.Audio)
		{
			list.Add("--no-audio");
		}
		else
		{
			if (config.AudioBuffer > 0)
			{
				list.Add($"--audio-buffer={config.AudioBuffer}");
			}
			if (config.AudioDoble)
			{
				list.Add("--audio-dup");
			}
			string text2 = (string.IsNullOrEmpty(config.AudioCodec) ? "opus" : config.AudioCodec);
			if (text2 != "opus")
			{
				list.Add("--audio-codec=" + text2);
			}
			if (config.AudioBitrate > 0 && config.AudioBitrate != 128)
			{
				list.Add($"--audio-bit-rate={config.AudioBitrate}K");
			}
		}
		list.Add("--no-control");
		if (config.DisableScreensaver)
		{
			list.Add("--disable-screensaver");
		}
		return UnirArgumentos(list);
	}

	public static string ConstruirArgumentosEntradaExperimental(ScrcpyConfig config, string serial)
	{
		return ConstruirArgumentosEntradaExperimental(config, serial, DualLayoutHelper.CalcularRespaldo());
	}

	public static string ConstruirArgumentosEntradaExperimental(ScrcpyConfig config, string serial, DualLayout layout)
	{
		List<string> list = new List<string>();
		AgregarDestino(list, config, serial);
		if (!string.IsNullOrEmpty(config.ShortcutMod))
		{
			list.Add("--shortcut-mod=" + config.ShortcutMod);
		}
		list.AddRange(new string[2] { "--window-title", "DLuzMObi v2 - Entrada" });
		list.Add("--always-on-top");
		if (layout.PosicionDisponible)
		{
			list.Add($"--window-x={layout.EntradaX}");
			list.Add($"--window-y={layout.EntradaY}");
		}
		list.Add($"--window-width={176}");
		list.Add($"--window-height={132}");
		list.Add("--no-video");
		list.Add("--no-audio");
		list.Add("--keyboard=" + config.TecladoModo);
		list.Add("--mouse=" + config.MouseModo);
		if (config.GamepadModo == "uhid")
		{
			list.Add("--gamepad=uhid");
		}
		if (config.ForwardAllClicks)
		{
			list.Add("--mouse-bind=++++:++++");
		}
		if (config.KeepActive)
		{
			list.Add("--keep-active");
		}
		if (config.TurnScreenOff)
		{
			list.Add("--turn-screen-off");
		}
		return UnirArgumentos(list);
	}

	private static string UnirArgumentos(IEnumerable<string> argumentos)
	{
		return string.Join(" ", argumentos.Select(delegate(string argumento)
		{
			if (string.IsNullOrEmpty(argumento))
			{
				return "\"\"";
			}
			if (argumento.Any(char.IsWhiteSpace) || argumento.Contains('"'))
			{
				return "\"" + argumento.Replace("\"", "\\\"") + "\"";
			}
			return argumento;
		}));
	}

	private static void AgregarDestino(List<string> cmd, ScrcpyConfig config, string serial)
	{
		if (!string.IsNullOrWhiteSpace(serial))
		{
			cmd.Add("-s");
			cmd.Add(serial.Trim());
		}
		else if (config.UsarWifi && !string.IsNullOrEmpty(config.WifiIp))
		{
			cmd.Add($"--tcpip={config.WifiIp}:{config.WifiPuerto}");
		}
	}

	private void EmitirAvisoDual(string titulo, string mensaje, string detalle, bool urgente)
	{
		Action<string, string, string, bool> action = this.OnAvisoDetallado;
		if (action != null)
		{
			action(titulo, mensaje, detalle, urgente);
		}
		else
		{
			this.OnError?.Invoke(titulo, mensaje);
		}
	}

	private static string ConstruirDetalleDual(string familia, string rol, ScrcpyProcessBenchmark metricas, Process? proceso)
	{
		int? num = null;
		try
		{
			if (proceso != null && proceso.HasExited)
			{
				num = proceso.ExitCode;
			}
		}
		catch
		{
		}
		int? num2 = num;
		if (!num2.HasValue)
		{
			num = metricas.ExitCode;
		}
		long? num3 = null;
		DateTimeOffset? inicioTimestamp = metricas.InicioTimestamp;
		if (inicioTimestamp.HasValue)
		{
			DateTimeOffset valueOrDefault = inicioTimestamp.GetValueOrDefault();
			num3 = (long)(DateTimeOffset.Now - valueOrDefault).TotalMilliseconds;
		}
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(32, 5, stringBuilder2);
		handler.AppendLiteral("familia=");
		handler.AppendFormatted(familia);
		handler.AppendLiteral(" rol=");
		handler.AppendFormatted(rol);
		handler.AppendLiteral(" pid=");
		handler.AppendFormatted(metricas.ProcessId?.ToString() ?? "?");
		handler.AppendLiteral(" exit=");
		handler.AppendFormatted(num?.ToString() ?? "?");
		handler.AppendLiteral(" vidaMs=");
		handler.AppendFormatted(num3?.ToString() ?? "?");
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(13, 2, stringBuilder2);
		handler.AppendLiteral("scrcpy=");
		handler.AppendFormatted(VersionScrcpyDetectada(metricas));
		handler.AppendLiteral(" arch=");
		handler.AppendFormatted(ArquitecturaHelper.ModoCompatibilidad ? "x86" : "x86_64");
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
		handler.AppendLiteral("timestamp=");
		handler.AppendFormatted(DateTimeOffset.Now, "O");
		stringBuilder5.AppendLine(ref handler);
		if (!string.IsNullOrWhiteSpace(metricas.Error))
		{
			stringBuilder2 = stringBuilder;
			StringBuilder stringBuilder6 = stringBuilder2;
			handler = new StringBuilder.AppendInterpolatedStringHandler(6, 1, stringBuilder2);
			handler.AppendLiteral("error=");
			handler.AppendFormatted(metricas.Error);
			stringBuilder6.AppendLine(ref handler);
		}
		stringBuilder.AppendLine("stderr tail:");
		stringBuilder.Append(string.IsNullOrWhiteSpace(metricas.StderrTail) ? "(vacío)" : metricas.StderrTail);
		return stringBuilder.ToString();
	}

	private static string VersionScrcpyDetectada(ScrcpyProcessBenchmark metricas)
	{
		string[] array = (metricas.StdoutTail + "\n" + metricas.StderrTail).Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if (text.StartsWith("scrcpy ", StringComparison.OrdinalIgnoreCase))
			{
				return text.Split('<')[0].Trim();
			}
		}
		return "desconocida";
	}

	private void RegistrarPostMortemDual(string familia, string rol, ScrcpyProcessBenchmark metricas, Process? proceso, string contexto)
	{
		string text = "[DUAL-POSTMORTEM] " + contexto + "\n" + ConstruirDetalleDual(familia, rol, metricas, proceso);
		try
		{
			AppLogger.Warn(text);
		}
		catch
		{
		}
		string text2 = _ultimoBenchmarkDual?.LogPath;
		if (!string.IsNullOrEmpty(text2))
		{
			try
			{
				File.AppendAllText(text2, $"[{DateTimeOffset.Now:O}] {text}\n\n", Encoding.UTF8);
			}
			catch
			{
			}
		}
	}

	private bool LanzarProceso(string args, ScrcpyConfig config)
	{
		string rutaScrcpy = ArquitecturaHelper.RutaScrcpy;
		if (!File.Exists(rutaScrcpy))
		{
			this.OnError?.Invoke("Archivo no encontrado", "No se encontró scrcpy.exe.\n\nVerifica que los archivos de la aplicación estén completos. Si el problema persiste, reinstala la aplicación.");
			return false;
		}
		try
		{
			UltimoDisplayVirtualId = -1;
			_modoDebugActivo = config.ModoDebug;
			_busquedaScrcpyDebugFinalizada = !config.ModoDebug;
			_procesoScrcpyDebug?.Dispose();
			_procesoScrcpyDebug = null;
			ProcessStartInfo startInfo = (config.ModoDebug ? FabricaProcesos.ScrcpyConsolaDebug(rutaScrcpy, _adbPath, args, "DLuzMObi v2 - Debug", "scrcpy.exe", ProcessWindowStyle.Normal) : FabricaProcesos.ScrcpyDirecto(rutaScrcpy, _adbPath, args));
			AplicarBackendVideo(startInfo, config);
			_proceso = new Process
			{
				StartInfo = startInfo
			};
			_proceso.Start();
			Program.AsignarAlJob(_proceso.Handle);
			AplicarRendimiento(_proceso);
			if (config.ModoDebug)
			{
				if (config.PrintFps)
				{
					try
					{
						AppLogger.Info("Registro de sesión omitido: el Modo Debug envía la salida de scrcpy a su propia consola y la app no puede leerla. Desactívalo para medir.");
					}
					catch
					{
					}
				}
				Process procesoCmd = _proceso;
				Task.Run(async delegate
				{
					Process scrcpyChild = null;
					try
					{
						scrcpyChild = await EsperarScrcpyDebugAsync(procesoCmd);
						if (scrcpyChild != null && _modoDebugActivo && _proceso == procesoCmd && ProcesoVivo(procesoCmd))
						{
							_procesoScrcpyDebug = scrcpyChild;
							Program.AsignarAlJob(scrcpyChild.Handle);
							AplicarRendimiento(scrcpyChild);
							scrcpyChild = null;
						}
					}
					finally
					{
						scrcpyChild?.Dispose();
						if (_proceso == procesoCmd)
						{
							_busquedaScrcpyDebugFinalizada = true;
						}
					}
				});
			}
			else
			{
				bool capturarFps = config.PrintFps;
				Process procesoRef = _proceso;
				if (capturarFps)
				{
					_registro.Iniciar(ResumenConfig(config, "normal"), procesoRef);
				}
				Task.Run(delegate
				{
					LeerStream(procesoRef.StandardOutput, capturarFps);
				});
				Task.Run(delegate
				{
					LeerStream(procesoRef.StandardError, capturarFps);
				});
			}
			return true;
		}
		catch (Exception ex)
		{
			if (ex is UnauthorizedAccessException || ex is Win32Exception || ex.Message.Contains("Access", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("denied", StringComparison.OrdinalIgnoreCase))
			{
				this.OnError?.Invoke("Sin permisos", "scrcpy no pudo iniciarse.\n\nIntenta ejecutar DLuz como administrador.");
			}
			else
			{
				this.OnError?.Invoke("Error al iniciar", "Error al iniciar scrcpy:\n" + ex.Message);
			}
			_proceso = null;
			_procesoScrcpyDebug = null;
			_modoDebugActivo = false;
			_busquedaScrcpyDebugFinalizada = true;
			return false;
		}
	}

	public bool LanzarDualExperimental(ScrcpyConfig config, DualLayout? layout = null)
	{
		string rutaScrcpy = ArquitecturaHelper.RutaScrcpy;
		if (!File.Exists(rutaScrcpy))
		{
			this.OnError?.Invoke("Archivo no encontrado", "No se encontró scrcpy.exe para la prueba dual experimental.");
			return false;
		}
		Detener();
		_modoDebugActivo = false;
		_dualDebugActivo = config.ModoDebug;
		_busquedaScrcpyDebugFinalizada = true;
		string text = ResolverSerialExperimental(config);
		bool flag = UsaInstanciaVisualExperimental(config);
		bool flag2 = UsaInstanciaEntradaExperimental(config);
		if (!flag && !flag2)
		{
			return LanzarProceso(ConstruirArgumentos(config), config);
		}
		if (layout == null)
		{
			layout = DualLayoutHelper.CalcularRespaldo();
		}
		string args = (flag ? ConstruirArgumentosVisualesExperimental(config, text, layout) : "");
		string args2 = (flag2 ? ConstruirArgumentosEntradaExperimental(config, text, layout) : "");
		ScrcpyDualBenchmark scrcpyDualBenchmark = (_ultimoBenchmarkDual = new ScrcpyDualBenchmark
		{
			Modo = "dual-experimental",
			Serial = (string.IsNullOrWhiteSpace(text) ? "(auto)" : text),
			LogPath = Path.Combine(AppContext.BaseDirectory, "dual_scrcpy_benchmark.log")
		});
		try
		{
			_cerrandoDual = false;
			_derriboAvisado = false;
			if (flag2)
			{
				_dualEntrada = LanzarProcesoDual(rutaScrcpy, args2, "entrada", capturarFps: false, config, scrcpyDualBenchmark.Entrada, out _dualEntradaHost);
				if (_dualEntrada == null)
				{
					return AbortarArranqueDual(scrcpyDualBenchmark, "arranque-lanzamiento", "entrada", scrcpyDualBenchmark.Entrada, null, "No se pudo iniciar la instancia de control (entrada).");
				}
				EsperaVentana esperaVentana = EsperarVentanaRobusta(_dualEntrada, 5000);
				scrcpyDualBenchmark.Entrada.VentanaMs = esperaVentana.VentanaMs;
				if (esperaVentana.Murio)
				{
					return AbortarArranqueDual(scrcpyDualBenchmark, "arranque-muerte", "entrada", scrcpyDualBenchmark.Entrada, _dualEntrada, "La instancia de control (entrada) terminó durante el arranque (código " + CodigoSalida(_dualEntrada) + ").");
				}
				if (!esperaVentana.VentanaMs.HasValue)
				{
					return AbortarArranqueDual(scrcpyDualBenchmark, "arranque-timeout", "entrada", scrcpyDualBenchmark.Entrada, _dualEntrada, $"La ventana de control no apareció en {5} segundos. Cierra sesiones scrcpy previas e inténtalo de nuevo.");
				}
			}
			if (flag)
			{
				_dualVisual = LanzarProcesoDual(rutaScrcpy, args, "visual", config.PrintFps, config, scrcpyDualBenchmark.Visual, out _dualVisualHost);
				if (_dualVisual == null)
				{
					return AbortarArranqueDual(scrcpyDualBenchmark, "arranque-lanzamiento", "visual", scrcpyDualBenchmark.Visual, null, "No se pudo iniciar la instancia del espejo (visual).");
				}
				if (config.Video)
				{
					EsperaVentana esperaVentana2 = EsperarVentanaRobusta(_dualVisual, 30000);
					scrcpyDualBenchmark.Visual.VentanaMs = esperaVentana2.VentanaMs;
					if (esperaVentana2.Murio)
					{
						return AbortarArranqueDual(scrcpyDualBenchmark, "arranque-muerte", "visual", scrcpyDualBenchmark.Visual, _dualVisual, "La instancia del espejo (visual) terminó durante el arranque (código " + CodigoSalida(_dualVisual) + ").");
					}
					if (!esperaVentana2.VentanaMs.HasValue)
					{
						return AbortarArranqueDual(scrcpyDualBenchmark, "arranque-timeout", "visual", scrcpyDualBenchmark.Visual, _dualVisual, $"La ventana del espejo no apareció en {30} segundos. El equipo puede estar saturado: vuelve a intentarlo o usa el modo normal.");
					}
					if (_dualEntrada != null)
					{
						TraerVentanaAlFrente(_dualEntrada, "entrada");
					}
				}
			}
			if (flag && config.PrintFps && !config.ModoDebug && _dualVisual != null)
			{
				_registro.Iniciar(ResumenConfig(config, "dual"), _dualVisual);
			}
			Task.Delay(1200).Wait();
			scrcpyDualBenchmark.Visual.VivoTrasLanzar = !flag || ProcesoVivo(_dualVisual);
			scrcpyDualBenchmark.Entrada.VivoTrasLanzar = !flag2 || ProcesoVivo(_dualEntrada);
			scrcpyDualBenchmark.AmbosVivos = scrcpyDualBenchmark.Visual.VivoTrasLanzar && scrcpyDualBenchmark.Entrada.VivoTrasLanzar;
			(scrcpyDualBenchmark.AdbPingOk, scrcpyDualBenchmark.AdbPingMs, scrcpyDualBenchmark.AdbPingStdout, scrcpyDualBenchmark.AdbPingStderr) = MedirAdbPing(text);
			if (!scrcpyDualBenchmark.AmbosVivos)
			{
				string text2 = (scrcpyDualBenchmark.Visual.VivoTrasLanzar ? "entrada" : "visual");
				ScrcpyProcessBenchmark metricas = ((text2 == "visual") ? scrcpyDualBenchmark.Visual : scrcpyDualBenchmark.Entrada);
				Process proceso = ((text2 == "visual") ? _dualVisual : _dualEntrada);
				return AbortarArranqueDual(scrcpyDualBenchmark, "arranque-muerte", text2, metricas, proceso, $"La instancia {((text2 == "visual") ? "del espejo (visual)" : "de control (entrada)")} terminó durante la validación inicial (código {CodigoSalida(proceso)}).");
			}
			if (_dualVisual != null)
			{
				VigilarDual(_dualVisual, "visual");
			}
			if (_dualEntrada != null)
			{
				VigilarDual(_dualEntrada, "entrada");
			}
			if (_dualVisual != null && _dualEntrada != null)
			{
				Task.Run(() => VigilarDualLoop(_dualVisual, _dualEntrada));
			}
			RegistrarBenchmarkDual(scrcpyDualBenchmark);
			try
			{
				AppLogger.Info("scrcpy dual iniciado");
			}
			catch
			{
			}
			return true;
		}
		catch (Exception ex)
		{
			scrcpyDualBenchmark.Error = ex.Message;
			DetenerDual();
			RegistrarBenchmarkDual(scrcpyDualBenchmark);
			EmitirAvisoDual("Prueba dual fallida", ex.Message, $"excepción durante el arranque dual:\n{ex}", urgente: true);
			return false;
		}
	}

	private bool AbortarArranqueDual(ScrcpyDualBenchmark benchmark, string familia, string rol, ScrcpyProcessBenchmark metricas, Process? proceso, string mensajeUsuario)
	{
		string detalle = ConstruirDetalleDual(familia, rol, metricas, proceso);
		benchmark.Error = mensajeUsuario;
		RegistrarPostMortemDual(familia, rol, metricas, proceso, mensajeUsuario);
		CompletarExitCodes(benchmark);
		DetenerDual();
		RegistrarBenchmarkDual(benchmark);
		EmitirAvisoDual("Modo dual: fallo al iniciar", mensajeUsuario, detalle, urgente: true);
		return false;
	}

	private static string CodigoSalida(Process? proceso)
	{
		try
		{
			if (proceso != null && proceso.HasExited)
			{
				return proceso.ExitCode.ToString();
			}
		}
		catch
		{
		}
		return "desconocido";
	}

	private static bool RequiereEntrada(ScrcpyConfig config)
	{
		if (!config.SoloEspejoSinControl)
		{
			if (!(config.TecladoModo != "disabled") && !(config.MouseModo != "disabled") && !(config.GamepadModo == "uhid") && !config.ForwardAllClicks && !config.KeepActive)
			{
				return config.TurnScreenOff;
			}
			return true;
		}
		return false;
	}

	private void VigilarDual(Process proceso, string rol)
	{
		proceso.Exited += delegate
		{
			if (!_cerrandoDual && ((rol == "visual") ? (_dualVisual == proceso) : (_dualEntrada == proceso)))
			{
				try
				{
					AppLogger.Warn("scrcpy dual finalizado: " + rol);
				}
				catch
				{
				}
				Task.Run(() => GestionarSalidaDualAsync(rol, proceso));
			}
		};
	}

	private async Task VigilarDualLoop(Process visual, Process entrada)
	{
		while (!_cerrandoDual)
		{
			await Task.Delay(500).ConfigureAwait(continueOnCapturedContext: false);
			bool flag = ProcesoVivo(visual);
			bool flag2 = ProcesoVivo(entrada);
			if (flag && flag2)
			{
				continue;
			}
			if ((_dualVisual == visual || _dualEntrada == entrada) && (flag || flag2))
			{
				try
				{
					AppLogger.Warn("scrcpy dual parcial");
				}
				catch
				{
				}
				await GestionarSalidaDualAsync(flag ? "entrada" : "visual", flag ? entrada : visual).ConfigureAwait(continueOnCapturedContext: false);
			}
			break;
		}
	}

	private async Task GestionarSalidaDualAsync(string rol, Process proceso)
	{
		if (_cerrandoDual)
		{
			return;
		}
		Process process;
		lock (_lockSalidaDual)
		{
			if (rol == "visual")
			{
				if (_dualVisual != proceso)
				{
					return;
				}
				process = _dualVisual;
				_dualVisual = null;
			}
			else
			{
				if (_dualEntrada != proceso)
				{
					return;
				}
				process = _dualEntrada;
				_dualEntrada = null;
			}
		}
		ScrcpyProcessBenchmark scrcpyProcessBenchmark = ((!(rol == "visual")) ? _ultimoBenchmarkDual?.Entrada : _ultimoBenchmarkDual?.Visual);
		string exit = CodigoSalida(process);
		bool anomala = exit != "0";
		string detalle = ((scrcpyProcessBenchmark != null) ? ConstruirDetalleDual("sesion-" + rol, rol, scrcpyProcessBenchmark, process) : $"rol={rol} exit={exit} (sin métricas de la sesión)");
		if (scrcpyProcessBenchmark != null)
		{
			RegistrarPostMortemDual("sesion-" + rol, rol, scrcpyProcessBenchmark, process, anomala ? "muerte anómala: derribo simétrico de la sesión dual y aviso al usuario" : "cierre intencional (exit 0): derribo simétrico silencioso");
		}
		try
		{
			process?.Dispose();
		}
		catch
		{
		}
		await Task.Delay(1000).ConfigureAwait(continueOnCapturedContext: false);
		if (_cerrandoDual)
		{
			return;
		}
		if (_dualDebugActivo)
		{
			DetenerProceso((rol == "entrada") ? _dualVisual : _dualEntrada, 1500);
			_dualVisual = null;
			_dualEntrada = null;
		}
		else
		{
			DetenerDual();
		}
		if (!anomala)
		{
			return;
		}
		lock (_lockSalidaDual)
		{
			if (_derriboAvisado)
			{
				return;
			}
			_derriboAvisado = true;
		}
		await Task.Delay(800).ConfigureAwait(continueOnCapturedContext: false);
		EmitirAvisoDual("Modo dual interrumpido", $"La sesión dual se detuvo: la instancia {((rol == "visual") ? "del espejo (visual)" : "de control (entrada)")} terminó inesperadamente (código {exit}). " + "Puedes volver a lanzar el modo dual o usar el modo normal.", detalle, urgente: true);
	}

	private static void TraerVentanaAlFrente(Process proceso, string rol)
	{
		try
		{
			proceso.Refresh();
			nint mainWindowHandle = proceso.MainWindowHandle;
			if (mainWindowHandle == IntPtr.Zero || SetWindowPos(mainWindowHandle, HWND_TOP, 0, 0, 0, 0, 19u))
			{
				return;
			}
			try
			{
				AppLogger.Warn("Layout dual: SetWindowPos no pudo traer la ventana de " + rol + " al frente.");
			}
			catch
			{
			}
		}
		catch (Exception ex)
		{
			try
			{
				AppLogger.Warn("Layout dual: fallo al traer la ventana de " + rol + " al frente: " + ex.Message);
			}
			catch
			{
			}
		}
	}

	private Process? LanzarProcesoDual(string scrcpyPath, string args, string rol, bool capturarFps, ScrcpyConfig config, ScrcpyProcessBenchmark metricas, out Process? host)
	{
		host = null;
		if (!config.ModoDebug)
		{
			return LanzarProcesoBenchmark(scrcpyPath, args, rol, capturarFps, config, metricas);
		}
		Stopwatch stopwatch = Stopwatch.StartNew();
		metricas.Rol = rol;
		metricas.ComandoSanitizado = SanitizarComando("scrcpy.exe " + args);
		try
		{
			string titulo = ((rol == "visual") ? "DLuzMObi v2 - Debug Tela" : "DLuzMObi v2 - Debug Entrada");
			ProcessStartInfo processStartInfo = FabricaProcesos.ScrcpyConsolaDebug(scrcpyPath, _adbPath, args, titulo, "scrcpy.exe " + rol, ProcessWindowStyle.Minimized);
			if (rol == "visual")
			{
				AplicarBackendVideo(processStartInfo, config);
			}
			else if (rol == "entrada")
			{
				processStartInfo.Environment["SC_CAPTURE_BACK"] = "1";
			}
			Process process = new Process
			{
				StartInfo = processStartInfo,
				EnableRaisingEvents = true
			};
			process.Start();
			host = process;
			Process result = EsperarScrcpyDebugAsync(process).GetAwaiter().GetResult();
			stopwatch.Stop();
			if (result == null)
			{
				metricas.InicioMs = stopwatch.ElapsedMilliseconds;
				metricas.Error = "No se encontró el proceso scrcpy.";
				return null;
			}
			metricas.InicioOk = true;
			metricas.InicioMs = stopwatch.ElapsedMilliseconds;
			metricas.InicioTimestamp = DateTimeOffset.Now;
			metricas.ProcessId = result.Id;
			metricas.StdoutTail = "Consola de depuración visible.";
			Program.AsignarAlJob(result.Handle);
			AplicarRendimiento(result);
			return result;
		}
		catch (Exception ex)
		{
			stopwatch.Stop();
			metricas.InicioMs = stopwatch.ElapsedMilliseconds;
			metricas.Error = ex.Message;
			try
			{
				host?.Dispose();
			}
			catch
			{
			}
			host = null;
			return null;
		}
	}

	private Process? LanzarProcesoBenchmark(string scrcpyPath, string args, string rol, bool capturarFps, ScrcpyConfig config, ScrcpyProcessBenchmark metricas)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		metricas.Rol = rol;
		metricas.ComandoSanitizado = SanitizarComando("scrcpy.exe " + args);
		try
		{
			ProcessStartInfo processStartInfo = FabricaProcesos.ScrcpyDirecto(scrcpyPath, _adbPath, args);
			if (rol == "visual")
			{
				AplicarBackendVideo(processStartInfo, config);
			}
			else if (rol == "entrada")
			{
				processStartInfo.Environment["SC_CAPTURE_BACK"] = "1";
			}
			Queue<string> stdout = new Queue<string>();
			Queue<string> stderr = new Queue<string>();
			Process proceso = new Process
			{
				StartInfo = processStartInfo,
				EnableRaisingEvents = true
			};
			proceso.OutputDataReceived += delegate(object _, DataReceivedEventArgs e)
			{
				if (e.Data != null)
				{
					AgregarLinea(stdout, e.Data);
					metricas.StdoutTail = UnirLineas(stdout);
					if (capturarFps)
					{
						ProcesarLineaScrcpy(e.Data, capturarFps: true);
					}
				}
			};
			proceso.ErrorDataReceived += delegate(object _, DataReceivedEventArgs e)
			{
				if (e.Data != null)
				{
					AgregarLinea(stderr, e.Data);
					metricas.StderrTail = UnirLineas(stderr);
					if (capturarFps)
					{
						ProcesarLineaScrcpy(e.Data, capturarFps: true);
					}
				}
			};
			proceso.Exited += delegate
			{
				metricas.VivoTrasLanzar = false;
				try
				{
					metricas.ExitCode = proceso.ExitCode;
				}
				catch
				{
				}
				metricas.StdoutTail = string.Join("\n", stdout);
				metricas.StderrTail = string.Join("\n", stderr);
			};
			metricas.InicioTimestamp = DateTimeOffset.Now;
			proceso.Start();
			stopwatch.Stop();
			metricas.InicioOk = true;
			metricas.InicioMs = stopwatch.ElapsedMilliseconds;
			metricas.ProcessId = proceso.Id;
			Program.AsignarAlJob(proceso.Handle);
			AplicarRendimiento(proceso);
			proceso.BeginOutputReadLine();
			proceso.BeginErrorReadLine();
			return proceso;
		}
		catch (Exception ex)
		{
			stopwatch.Stop();
			metricas.InicioMs = stopwatch.ElapsedMilliseconds;
			metricas.Error = ex.Message;
			return null;
		}
	}

	private static void AgregarLinea(Queue<string> buffer, string linea)
	{
		lock (buffer)
		{
			buffer.Enqueue(linea);
			while (buffer.Count > 80)
			{
				buffer.Dequeue();
			}
		}
	}

	private static string UnirLineas(Queue<string> buffer)
	{
		lock (buffer)
		{
			return string.Join("\n", buffer);
		}
	}

	private static EsperaVentana EsperarVentanaRobusta(Process proceso, int timeoutMs)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		while (stopwatch.ElapsedMilliseconds < timeoutMs)
		{
			if (!ProcesoVivo(proceso))
			{
				return new EsperaVentana(null, Murio: true);
			}
			try
			{
				proceso.Refresh();
				if (proceso.MainWindowHandle != IntPtr.Zero)
				{
					return new EsperaVentana(stopwatch.ElapsedMilliseconds, Murio: false);
				}
			}
			catch
			{
				return new EsperaVentana(null, Murio: true);
			}
			Task.Delay(120).Wait();
		}
		return new EsperaVentana(null, !ProcesoVivo(proceso));
	}

	private string ResolverSerialExperimental(ScrcpyConfig config)
	{
		if (config.UsarWifi && !string.IsNullOrWhiteSpace(config.WifiIp))
		{
			return "";
		}
		try
		{
			using Process process = new Process
			{
				StartInfo = FabricaProcesos.AdbCapturado(_adbPath, "devices")
			};
			process.Start();
			string text = process.StandardOutput.ReadToEnd();
			process.WaitForExit(5000);
			List<string> list = new List<string>();
			string[] array = text.Split('\n');
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = array[i].Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
				if (array2.Length >= 2 && array2[1] == "device")
				{
					list.Add(array2[0]);
				}
			}
			foreach (string item in list)
			{
				if (!item.Contains(':'))
				{
					return item;
				}
			}
			return (list.Count > 0) ? list[0] : "";
		}
		catch
		{
			return "";
		}
	}

	private (bool ok, long ms, string stdout, string stderr) MedirAdbPing(string serial)
	{
		List<string> list = new List<string>();
		if (!string.IsNullOrWhiteSpace(serial))
		{
			list.Add("-s");
			list.Add(serial.Trim());
		}
		list.Add("shell");
		list.Add("echo");
		list.Add("lyxel_ping");
		Stopwatch stopwatch = Stopwatch.StartNew();
		try
		{
			using Process process = new Process
			{
				StartInfo = FabricaProcesos.AdbCapturado(_adbPath, string.Join(" ", list))
			};
			process.Start();
			string text = process.StandardOutput.ReadToEnd();
			string text2 = process.StandardError.ReadToEnd();
			bool num = process.WaitForExit(5000);
			stopwatch.Stop();
			if (!num)
			{
				try
				{
					process.Kill(entireProcessTree: true);
				}
				catch
				{
				}
				return (ok: false, ms: stopwatch.ElapsedMilliseconds, stdout: text, stderr: "timeout");
			}
			return (ok: process.ExitCode == 0, ms: stopwatch.ElapsedMilliseconds, stdout: text.Trim(), stderr: text2.Trim());
		}
		catch (Exception ex)
		{
			stopwatch.Stop();
			return (ok: false, ms: stopwatch.ElapsedMilliseconds, stdout: "", stderr: ex.Message);
		}
	}

	private void CompletarExitCodes(ScrcpyDualBenchmark benchmark)
	{
		try
		{
			if (_dualVisual != null && _dualVisual.HasExited)
			{
				benchmark.Visual.ExitCode = _dualVisual.ExitCode;
			}
		}
		catch
		{
		}
		try
		{
			if (_dualEntrada != null && _dualEntrada.HasExited)
			{
				benchmark.Entrada.ExitCode = _dualEntrada.ExitCode;
			}
		}
		catch
		{
		}
	}

	private void RegistrarBenchmarkDual(ScrcpyDualBenchmark benchmark)
	{
		CompletarExitCodes(benchmark);
		string text = $"[{DateTimeOffset.Now:O}] modo={benchmark.Modo} serial={benchmark.Serial}\nvisual: pid={benchmark.Visual.ProcessId} startMs={benchmark.Visual.InicioMs} windowMs={benchmark.Visual.VentanaMs} alive={benchmark.Visual.VivoTrasLanzar} exit={benchmark.Visual.ExitCode} error={benchmark.Visual.Error}\nvisual cmd: {benchmark.Visual.ComandoSanitizado}\nentrada: pid={benchmark.Entrada.ProcessId} startMs={benchmark.Entrada.InicioMs} windowMs={benchmark.Entrada.VentanaMs} alive={benchmark.Entrada.VivoTrasLanzar} exit={benchmark.Entrada.ExitCode} error={benchmark.Entrada.Error}\nentrada cmd: {benchmark.Entrada.ComandoSanitizado}\nadb_ping: ok={benchmark.AdbPingOk} ms={benchmark.AdbPingMs} stdout={benchmark.AdbPingStdout} stderr={benchmark.AdbPingStderr}\nambos_vivos={benchmark.AmbosVivos} error={benchmark.Error}\nvisual stderr tail:\n{benchmark.Visual.StderrTail}\nentrada stderr tail:\n{benchmark.Entrada.StderrTail}\n";
		try
		{
			File.AppendAllText(benchmark.LogPath, text + "\n", Encoding.UTF8);
		}
		catch
		{
		}
		try
		{
			AppLogger.Info("dual-scrcpy benchmark\n" + text);
		}
		catch
		{
		}
	}

	private static string SanitizarComando(string comando)
	{
		if (string.IsNullOrWhiteSpace(comando))
		{
			return "";
		}
		return comando.Replace("\r", " ").Replace("\n", " ").Trim();
	}

	private static async Task<Process?> EsperarScrcpyDebugAsync(Process procesoCmd)
	{
		for (int i = 0; i < 30; i++)
		{
			if (!ProcesoVivo(procesoCmd))
			{
				try
				{
					AppLogger.Warn("Vía debug: la consola cmd terminó antes de poder identificar su scrcpy hijo.");
				}
				catch
				{
				}
				return null;
			}
			Process process = BuscarScrcpyDebug(procesoCmd.Id);
			if (process != null)
			{
				return process;
			}
			await Task.Delay(100).ConfigureAwait(continueOnCapturedContext: false);
		}
		try
		{
			AppLogger.Warn("Vía debug: ningún scrcpy resultó descendiente de la consola cmd lanzada; se aborta la identificación (sin heurísticas de ruta/tiempo).");
		}
		catch
		{
		}
		return null;
	}

	private static bool ProcesoVivo(Process? proceso)
	{
		try
		{
			return proceso != null && !proceso.HasExited;
		}
		catch
		{
			return false;
		}
	}

	private static Process? BuscarScrcpyDebug(int cmdPid)
	{
		Process[] processesByName = Process.GetProcessesByName("scrcpy");
		foreach (Process process in processesByName)
		{
			try
			{
				if (!process.HasExited && EsDescendienteDe(process.Id, cmdPid))
				{
					return process;
				}
			}
			catch
			{
			}
			process.Dispose();
		}
		return null;
	}

	private static bool EsDescendienteDe(int processId, int ancestorPid)
	{
		HashSet<int> hashSet = new HashSet<int>();
		int num = processId;
		while (num > 0 && hashSet.Add(num))
		{
			int num2 = ObtenerParentProcessId(num);
			if (num2 == ancestorPid)
			{
				return true;
			}
			num = num2;
		}
		return false;
	}

	private static int ObtenerParentProcessId(int processId)
	{
		nint num = CreateToolhelp32Snapshot(2u, 0u);
		if (num == IntPtr.Zero || num == INVALID_HANDLE_VALUE)
		{
			try
			{
				AppLogger.Warn("Vía debug: no se pudo tomar el snapshot de procesos para probar descendencia.");
			}
			catch
			{
			}
			return 0;
		}
		try
		{
			PROCESSENTRY32 lppe = new PROCESSENTRY32
			{
				dwSize = (uint)Marshal.SizeOf<PROCESSENTRY32>()
			};
			if (!Process32First(num, ref lppe))
			{
				return 0;
			}
			do
			{
				if (lppe.th32ProcessID == (uint)processId)
				{
					return (int)lppe.th32ParentProcessID;
				}
			}
			while (Process32Next(num, ref lppe));
		}
		finally
		{
			CloseHandle(num);
		}
		return 0;
	}

	public static string EstadoRendimiento()
	{
		if (string.Equals(Environment.GetEnvironmentVariable("LYXEL_PRIO"), "off", StringComparison.OrdinalIgnoreCase))
		{
			return "normal";
		}
		return ModoDesempenhoService.Carregar() switch
		{
			ModoDesempenhoService.AltaPerformance => "alta",
			ModoDesempenhoService.MemoriaBaixa => "memoria-baixa",
			_ => "equilibrado"
		};
	}

	[DllImport("kernel32.dll")]
	private static extern bool GetSystemPowerStatus(out SystemPowerStatus estado);

	private static bool EnBateria()
	{
		try
		{
			if (!GetSystemPowerStatus(out var estado))
			{
				return false;
			}
			if ((estado.BatteryFlag & 0x80) != 0)
			{
				return false;
			}
			return estado.ACLineStatus == 0;
		}
		catch
		{
			return false;
		}
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool SetProcessInformation(nint hProcess, int clase, ref ProcessPowerThrottlingState info, uint tamano);

	public static bool AceleracionAplicable(ScrcpyConfig config)
	{
		if (config.AceleracionHardware && config.Video)
		{
			return !config.ModoOtg;
		}
		return false;
	}

	private static void AplicarBackendVideo(ProcessStartInfo startInfo, ScrcpyConfig config)
	{
		if (AceleracionAplicable(config))
		{
			startInfo.Environment["SC_GPU"] = "d3d11";
			startInfo.Environment["SC_GPU_DIRECT"] = "1";
		}
	}

	private static void AplicarRendimiento(Process? proceso)
	{
		if (proceso == null)
		{
			return;
		}
		string modo = ModoDesempenhoService.Carregar();
		try
		{
			proceso.PriorityClass = modo switch
			{
				ModoDesempenhoService.AltaPerformance => ProcessPriorityClass.High,
				ModoDesempenhoService.MemoriaBaixa => ProcessPriorityClass.BelowNormal,
				_ => ProcessPriorityClass.Normal
			};
		}
		catch
		{
		}
		try
		{
			ProcessPowerThrottlingState info = new ProcessPowerThrottlingState
			{
				Version = 1u,
				ControlMask = 1u,
				StateMask = modo == ModoDesempenhoService.MemoriaBaixa ? 1u : 0u
			};
			SetProcessInformation(proceso.Handle, 4, ref info, (uint)Marshal.SizeOf<ProcessPowerThrottlingState>());
		}
		catch
		{
		}
	}

	private static string ResumenConfig(ScrcpyConfig config, string modo)
	{
		string value = (AceleracionAplicable(config) ? "d3d11-directo" : (string.IsNullOrWhiteSpace(config.RenderDriver) ? "auto" : config.RenderDriver));
		string value2 = ((config.UseAdvancedEncoder && !string.IsNullOrWhiteSpace(config.VideoEncoder)) ? config.VideoEncoder : "auto");
		string value3 = (config.Audio ? $"on/{config.AudioBuffer}" : "off");
		string value4 = (config.UsarWifi ? "wifi" : "usb");
		string value5 = (config.PantallaVirtualDex ? " vd=on" : "");
		return $"modo={modo} prio={EstadoRendimiento()} render={value} codec={config.VideoCodec} enc={value2} br={config.Bitrate}M size={config.MaxSize} cap={config.Fps} vbuf={config.VideoBuffer} audio={value3} in={config.TecladoModo}/{config.MouseModo} link={value4}{value5}";
	}

	private void LeerStream(StreamReader reader, bool capturarFps)
	{
		try
		{
			while (!reader.EndOfStream)
			{
				string text = reader.ReadLine();
				if (text != null)
				{
					ProcesarLineaScrcpy(text, capturarFps);
					continue;
				}
				break;
			}
		}
		catch
		{
		}
	}

	private void ProcesarLineaScrcpy(string linea, bool capturarFps)
	{
		if (linea.Contains("ERROR", StringComparison.OrdinalIgnoreCase) || linea.Contains("WARN", StringComparison.OrdinalIgnoreCase))
		{
			AppLogger.Info("scrcpy: " + linea.Trim());
		}
		if (UltimoDisplayVirtualId < 0 && linea.Contains("New display", StringComparison.OrdinalIgnoreCase))
		{
			Match match = Regex.Match(linea, "\\(id=(\\d+)\\)");
			if (match.Success && int.TryParse(match.Groups[1].Value, out var result))
			{
				UltimoDisplayVirtualId = result;
				try
				{
					AppLogger.Info($"[VD-DEX] Display virtual detectado: id={result}");
				}
				catch
				{
				}
			}
		}
		if (!capturarFps)
		{
			return;
		}
		Match match2 = RegexFps.Match(linea);
		if (match2.Success)
		{
			if (int.TryParse(match2.Groups[1].Value, out var result2))
			{
				int result3;
				int skip = ((match2.Groups[2].Success && int.TryParse(match2.Groups[2].Value, out result3)) ? result3 : 0);
				_registro.Registrar(result2, skip);
			}
			this.OnFpsUpdate?.Invoke(match2.Groups[1].Value + " fps");
		}
	}

	public void Detener()
	{
		_registro.Cerrar();
		DetenerDual();
		Process proceso = _proceso;
		Process procesoScrcpyDebug = _procesoScrcpyDebug;
		if (proceso == null && procesoScrcpyDebug == null)
		{
			return;
		}
		_modoDebugActivo = false;
		try
		{
			DetenerProceso(procesoScrcpyDebug, 2000);
			DetenerProceso(proceso, 1500);
		}
		finally
		{
			procesoScrcpyDebug?.Dispose();
			proceso?.Dispose();
			_procesoScrcpyDebug = null;
			_proceso = null;
			_modoDebugActivo = false;
			_busquedaScrcpyDebugFinalizada = true;
		}
	}

	private void DetenerDual()
	{
		_registro.Cerrar();
		bool flag = _dualEntrada != null || _dualVisual != null || _dualEntradaHost != null || _dualVisualHost != null;
		_cerrandoDual = true;
		Process dualEntrada = _dualEntrada;
		Process dualVisual = _dualVisual;
		Process dualEntradaHost = _dualEntradaHost;
		Process dualVisualHost = _dualVisualHost;
		_dualEntrada = null;
		_dualVisual = null;
		_dualEntradaHost = null;
		_dualVisualHost = null;
		try
		{
			DetenerProceso(dualEntrada, 1500);
		}
		catch
		{
		}
		try
		{
			DetenerProceso(dualVisual, 2000);
		}
		catch
		{
		}
		try
		{
			DetenerProceso(dualEntradaHost, 1000);
		}
		catch
		{
		}
		try
		{
			DetenerProceso(dualVisualHost, 1000);
		}
		catch
		{
		}
		try
		{
			dualEntrada?.Dispose();
		}
		catch
		{
		}
		try
		{
			dualVisual?.Dispose();
		}
		catch
		{
		}
		try
		{
			dualEntradaHost?.Dispose();
		}
		catch
		{
		}
		try
		{
			dualVisualHost?.Dispose();
		}
		catch
		{
		}
		_dualDebugActivo = false;
		if (flag)
		{
			try
			{
				AppLogger.Info("scrcpy dual detenido");
			}
			catch
			{
			}
		}
	}

	private static void DetenerProceso(Process? proceso, int timeoutMs)
	{
		if (!ProcesoVivo(proceso))
		{
			return;
		}
		try
		{
			proceso.CloseMainWindow();
		}
		catch
		{
		}
		try
		{
			if (!proceso.WaitForExit(timeoutMs) && ProcesoVivo(proceso))
			{
				proceso.Kill(entireProcessTree: true);
			}
		}
		catch
		{
			try
			{
				if (ProcesoVivo(proceso))
				{
					proceso.Kill(entireProcessTree: true);
				}
			}
			catch
			{
			}
		}
	}

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern nint CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool Process32First(nint hSnapshot, ref PROCESSENTRY32 lppe);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool Process32Next(nint hSnapshot, ref PROCESSENTRY32 lppe);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool CloseHandle(nint hObject);

	public async Task<(bool exito, List<string> encoders, List<string> displayLabels, string output)> DetectarEncodersAsync()
	{
		return await Task.Run(delegate
		{
			try
			{
				Process process = new Process
				{
					StartInfo = FabricaProcesos.ScrcpyDirecto(ArquitecturaHelper.RutaScrcpy, _adbPath, "--list-encoders")
				};
				process.Start();
				Task<string> task = process.StandardOutput.ReadToEndAsync();
				Task<string> task2 = process.StandardError.ReadToEndAsync();
				Task.WaitAll(task, task2);
				process.WaitForExit(10000);
				string text = task.Result + "\n" + task2.Result;
				var (list, item) = ParsearEncoders(text);
				return (list.Count > 0, list, item, text.Trim());
			}
			catch (Exception ex)
			{
				return (false, new List<string>(), new List<string>(), "Error: " + ex.Message);
			}
		});
	}

	private (List<string> encoders, List<string> displayLabels) ParsearEncoders(string output)
	{
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		bool flag = false;
		string[] array = output.Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			if (text.Contains("List of video encoders") || text.Contains("Video encoders"))
			{
				flag = true;
			}
			else if (text.Contains("List of audio encoders") || text.Contains("Audio encoders"))
			{
				flag = false;
			}
			else
			{
				if (!flag)
				{
					continue;
				}
				string value = "h264";
				int num = text.IndexOf("--video-codec=", StringComparison.Ordinal);
				if (num >= 0)
				{
					value = text.Substring(num + "--video-codec=".Length).Trim().Split(' ')[0].Trim();
				}
				int num2 = text.IndexOf("--video-encoder=", StringComparison.Ordinal);
				if (num2 >= 0)
				{
					string text2 = text.Substring(num2 + "--video-encoder=".Length).Trim();
					string text3 = text2.Split(' ')[0].Trim();
					if (!string.IsNullOrWhiteSpace(text3) && text3.Contains(".") && !list.Contains(text3))
					{
						string value2 = (text2.Contains("(hw)") ? "hw" : "sw");
						list.Add(text3);
						list2.Add($"{text3}  [{value2}] [{value}]");
					}
				}
			}
		}
		return (encoders: list, displayLabels: list2);
	}

	public Task<bool> LanzarAsync(ScrcpyConfig config)
	{
		return Task.Run(() => Lanzar(config));
	}

	public Task DetenerAsync()
	{
		return Task.Run(delegate
		{
			Detener();
		});
	}

	private static string InferirCodecDeEncoder(string encoderName)
	{
		string text = encoderName.ToLower();
		if (text.Contains("hevc") || text.Contains("h265") || text.Contains("h.265"))
		{
			return "h265";
		}
		if (text.Contains("av01") || text.Contains("av1"))
		{
			return "av1";
		}
		return "h264";
	}
	public static string InferirProcessador(string? encoder)
	{
		if (string.IsNullOrWhiteSpace(encoder)) return "Desconhecido";
		string enc = encoder.ToLowerInvariant();
		if (enc.Contains("qcom") || enc.Contains("qualcomm")) return "Snapdragon";
		if (enc.Contains("exynos") || enc.Contains("samsung")) return "Exynos";
		if (enc.Contains("mtk") || enc.Contains("mediatek")) return "MediaTek / Dimensity";
		if (enc.Contains("kirin") || enc.Contains("hisilicon")) return "Kirin";
		if (enc.Contains("unisoc") || enc.Contains("sprd")) return "Unisoc";
		if (enc.Contains("google") || enc.Contains("android") || enc.Contains("c2.android")) return "Software / Android";
		return "Hardware";
	}
}