using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DLuz.Helpers;

namespace DLuz;

public class ADBManager
{
	public const int ExitCodeCrashNativo = -1073741819;

	public const int ErrorBadExeFormat = 193;

	private readonly string _adbPath;

	private Process? _trackProcess;

	private volatile bool _ultimoEstadoDispositivo;

	private volatile bool _ultimoEstadoDispositivoUsb;

	private volatile bool _trackActivo;

	private volatile bool _trackSuspendidoPorFallos;

	private volatile int _trackGeneracion;

	private string _ultimoDetalleTrack = "";

	public event Action<bool>? OnDispositivoCambio;

	public event Action<bool>? OnDispositivoUsbCambio;

	public event Action<string>? OnDispositivoConectado;

	public event Action? OnDispositivoDesconectado;

	public event Action<bool, string>? OnEstadoConexionCambiado;

	public event Action<int>? OnDpiActualizado;

	public event Action<int, int>? OnResolucionActualizada;

	public event Action<string, EstadoDispositivo>? OnEstadoDetalladoCambio;

	public event Action? OnTrackSuspendido;

	public ADBManager(string adbPath)
	{
		_adbPath = adbPath;
	}

	private static EstadoDispositivo ClasificarEstado(string estado)
	{
		return estado switch
		{
			"device" => EstadoDispositivo.Conectado, 
			"unauthorized" => EstadoDispositivo.NoAutorizado, 
			"offline" => EstadoDispositivo.Offline, 
			"recovery" => EstadoDispositivo.Recovery, 
			"sideload" => EstadoDispositivo.Sideload, 
			_ => EstadoDispositivo.Desconocido, 
		};
	}

	private static string Truncar(string texto, int max = 500)
	{
		texto = (texto ?? "").Trim();
		if (texto.Length > max)
		{
			return texto.Substring(0, max) + "…";
		}
		return texto;
	}

	private static void LogFallo(string comando, int exitCode, string stderr)
	{
		if (exitCode == -1073741819)
		{
			AppLogger.Error($"[ADB-CRASH-NATIVO] adb {comando} terminó con exit code {exitCode} (0xC0000005). stderr: {Truncar(stderr)}");
		}
		else
		{
			AppLogger.Warn($"ADB falló: adb {comando} → exit code {exitCode}. stderr: {Truncar(stderr)}");
		}
	}

	private static bool EsBadExeFormat(Exception ex)
	{
		if (ex is Win32Exception ex2)
		{
			return ex2.NativeErrorCode == 193;
		}
		return false;
	}

	private (bool exito, string stdout, string stderr) EjecutarComando(List<string> args, int timeoutMs = 10000)
	{
		try
		{
			return EjecutarComandoAsync(args, timeoutMs).GetAwaiter().GetResult();
		}
		catch (Exception ex)
		{
			if (EsBadExeFormat(ex))
			{
				AppLogger.Error("[ADB-CRASH-NATIVO] adb no es un ejecutable válido (ERROR_BAD_EXE_FORMAT, 193). args: " + string.Join(" ", args));
				return (exito: false, stdout: "", stderr: "[ADB-CRASH-NATIVO] ERROR_BAD_EXE_FORMAT (193)");
			}
			AppLogger.Error("Não foi possível executar o ADB (" + string.Join(" ", args) + ")", ex);
			string item = ((ex is UnauthorizedAccessException || ex is Win32Exception) ? ("[PERMISOS] " + ex.Message) : ex.Message);
			return (exito: false, stdout: "", stderr: item);
		}
	}

	private async Task<(bool exito, string stdout, string stderr)> EjecutarComandoAsync(List<string> args, int timeoutMs)
	{
		List<string> finalArgs = new List<string>(args);
		if (finalArgs.Count > 0 &&
			(finalArgs[0] == "shell" || finalArgs[0] == "push" || finalArgs[0] == "pull" || finalArgs[0] == "install" || finalArgs[0] == "uninstall") &&
			!finalArgs.Contains("-s") && !finalArgs.Contains("-d") && !finalArgs.Contains("-e"))
		{
			string? serialActivo = ResolverSerialActivo(null);
			if (!string.IsNullOrWhiteSpace(serialActivo))
			{
				finalArgs.Insert(0, serialActivo);
				finalArgs.Insert(0, "-s");
			}
		}
		ProcessStartInfo startInfo = FabricaProcesos.AdbCapturado(_adbPath, string.Join(" ", finalArgs));
		using Process proceso = new Process
		{
			StartInfo = startInfo
		};
		proceso.Start();
		Task<string> stdoutTask = proceso.StandardOutput.ReadToEndAsync();
		Task<string> stderrTask = proceso.StandardError.ReadToEndAsync();
		using CancellationTokenSource cts = new CancellationTokenSource(timeoutMs);
		try
		{
			await proceso.WaitForExitAsync(cts.Token).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OperationCanceledException)
		{
			try
			{
				if (!proceso.HasExited)
				{
					proceso.Kill();
				}
			}
			catch
			{
			}
			try
			{
				await proceso.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(2.0)).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch
			{
			}
			int value = Math.Max(1, (int)Math.Ceiling((double)timeoutMs / 1000.0));
			AppLogger.Warn($"ADB sin respuesta ({value}s): adb {startInfo.Arguments}");
			return (exito: false, stdout: "", stderr: $"ADB no respondió en {value} segundos");
		}
		string stdout = await stdoutTask.ConfigureAwait(continueOnCapturedContext: false);
		string text = await stderrTask.ConfigureAwait(continueOnCapturedContext: false);
		if (proceso.ExitCode != 0)
		{
			LogFallo(startInfo.Arguments, proceso.ExitCode, text);
		}
		return (exito: proceso.ExitCode == 0, stdout: stdout, stderr: text);
	}

	public static bool EsSerialWifi(string serial)
	{
		return !string.IsNullOrWhiteSpace(serial) &&
			(serial.Contains(':') || serial.Contains("_adb-tls-connect._tcp", StringComparison.OrdinalIgnoreCase));
	}

	public Task<(bool exito, string stdout, string stderr)> ExecutarAdbAsync(IEnumerable<string> argumentos, int timeoutMs = 10000)
	{
		return EjecutarComandoAsync(argumentos.ToList(), timeoutMs);
	}

	public (bool exito, int ancho, int alto, string mensaje) DetectarResolucion()
	{
		var (flag, text, _) = EjecutarComando(new List<string> { "shell", "wm", "size" });
		if (!flag)
		{
			return (exito: false, ancho: 1080, alto: 2400, mensaje: "No se pudo conectar con el dispositivo");
		}
		try
		{
			string[] array = text.Split('\n');
			foreach (string text2 in array)
			{
				if (text2.Contains("Physical size:") || text2.Contains("Override size:"))
				{
					string[] array2 = text2.Split(':')[^1].Trim().Split('x');
					int num = int.Parse(array2[0]);
					int num2 = int.Parse(array2[1]);
					this.OnResolucionActualizada?.Invoke(num, num2);
					return (exito: true, ancho: num, alto: num2, mensaje: $"Detectado: {num}x{num2}");
				}
			}
			return (exito: false, ancho: 1080, alto: 2400, mensaje: "No se pudo parsear la resolución");
		}
		catch (Exception ex)
		{
			return (exito: false, ancho: 1080, alto: 2400, mensaje: "Error procesando respuesta: " + ex.Message);
		}
	}

	public (bool exito, int ancho, int alto) DetectarTamanoActual()
	{
		try
		{
			var (flag, input, _) = EjecutarShell("dumpsys window displays");
			if (flag)
			{
				Match match = Regex.Match(input, "cur=(\\d+)x(\\d+)");
				if (match.Success && int.TryParse(match.Groups[1].Value, out var result) && int.TryParse(match.Groups[2].Value, out var result2) && result > 0 && result2 > 0)
				{
					return (exito: true, ancho: result, alto: result2);
				}
			}
		}
		catch
		{
		}
		var (item, item2, item3, _) = DetectarResolucion();
		return (exito: item, ancho: item2, alto: item3);
	}

	public Task<(bool, int, int)> DetectarTamanoActualAsync()
	{
		return Task.Run(() => DetectarTamanoActual());
	}

	public (bool exito, string wmSize, string error) AplicarResolucion(int ancho, int alto, double aspectRatio)
	{
		int num = (int)((double)ancho * aspectRatio);
		string text = $"{ancho}x{num}";
		var (flag, _, item) = EjecutarComando(new List<string> { "shell", "wm", "size", text });
		if (flag)
		{
			this.OnResolucionActualizada?.Invoke(ancho, num);
		}
		return (exito: flag, wmSize: text, error: item);
	}

	public (bool exito, string error) ResetearResolucion()
	{
		var (item, _, item2) = EjecutarComando(new List<string> { "shell", "wm", "size", "reset" });
		return (exito: item, error: item2);
	}

	public (bool exito, string error) AplicarWmSizePersonalizada(string resolucion)
	{
		if (string.IsNullOrWhiteSpace(resolucion))
		{
			return (exito: false, error: "Resolución vacía");
		}
		var (flag, _, item) = EjecutarComando(new List<string>
		{
			"shell",
			"wm",
			"size",
			resolucion.Trim()
		});
		if (flag)
		{
			string[] array = resolucion.Trim().Split('x');
			if (array.Length == 2 && int.TryParse(array[0], out var result) && int.TryParse(array[1], out var result2))
			{
				this.OnResolucionActualizada?.Invoke(result, result2);
			}
		}
		return (exito: flag, error: item);
	}

	public Task<(bool, string)> AplicarWmSizePersonalizadaAsync(string resolucion)
	{
		return Task.Run(() => AplicarWmSizePersonalizada(resolucion));
	}

	public bool HayDispositivoConectado()
	{
		try
		{
			return EjecutarComando(new List<string> { "devices" }).stdout.Split('\n').Skip(1).Any((string l) => l.Contains("\tdevice"));
		}
		catch
		{
			return false;
		}
	}

	public (bool exito, int dpi, string mensaje) DetectarDPI()
	{
		var (flag, text, _) = EjecutarComando(new List<string> { "shell", "wm", "density" });
		if (!flag)
		{
			return (exito: false, dpi: 420, mensaje: "No se pudo conectar con el dispositivo");
		}
		try
		{
			string[] array = text.Split('\n');
			foreach (string text2 in array)
			{
				if (text2.ToLower().Contains("density:"))
				{
					int num = int.Parse(text2.Split(':')[^1].Trim());
					this.OnDpiActualizado?.Invoke(num);
					return (exito: true, dpi: num, mensaje: $"Detectado: {num} DPI");
				}
			}
			return (exito: false, dpi: 420, mensaje: "No se pudo parsear el DPI");
		}
		catch (Exception ex)
		{
			return (exito: false, dpi: 420, mensaje: "Error procesando respuesta: " + ex.Message);
		}
	}

	public (bool exito, string mensaje, string error) AplicarDPI(int nuevoDpi)
	{
		if (nuevoDpi <= 0)
		{
			return (exito: false, mensaje: "El DPI debe ser un número mayor que 0", error: "Validación fallida");
		}
		var (flag, _, item) = EjecutarComando(new List<string>
		{
			"shell",
			"wm",
			"density",
			nuevoDpi.ToString()
		});
		if (flag)
		{
			this.OnDpiActualizado?.Invoke(nuevoDpi);
		}
		if (!flag)
		{
			return (exito: false, mensaje: "Error aplicando DPI", error: item);
		}
		return (exito: true, mensaje: $"DPI cambiado a {nuevoDpi}", error: "");
	}

	public (bool exito, string mensaje, string error) ResetearDPI()
	{
		var (flag, _, item) = EjecutarComando(new List<string> { "shell", "wm", "density", "reset" });
		if (!flag)
		{
			return (exito: false, mensaje: "Error reseteando DPI", error: item);
		}
		return (exito: true, mensaje: "DPI restaurado a valor de fábrica", error: "");
	}

	private static bool EsPuertoAbierto(string host, int puerto, int timeoutMs = 120)
	{
		try
		{
			using var client = new System.Net.Sockets.TcpClient();
			var result = client.BeginConnect(host, puerto, null, null);
			bool exito = result.AsyncWaitHandle.WaitOne(timeoutMs);
			if (!exito) return false;
			client.EndConnect(result);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static DateTime _ultimoIntentoWsa = DateTime.MinValue;

	public bool IntentarAutoConectarWsa(bool forzar = false)
	{
		try
		{
			if (!forzar && (DateTime.UtcNow - _ultimoIntentoWsa).TotalSeconds < 4)
			{
				return false;
			}
			_ultimoIntentoWsa = DateTime.UtcNow;

			// WSA escuta por padrão na porta 58526 no Windows
			if (EsPuertoAbierto("127.0.0.1", 58526, 120))
			{
				var (exito, salida, _) = EjecutarComando(new List<string> { "connect", "127.0.0.1:58526" }, 2500);
				if (exito && (salida.Contains("connected", StringComparison.OrdinalIgnoreCase) || salida.Contains("already", StringComparison.OrdinalIgnoreCase)))
				{
					AppLogger.Info("ADB: WSA conectado automaticamente em 127.0.0.1:58526");
					return true;
				}
			}
		}
		catch (Exception ex)
		{
			AppLogger.Warn("IntentarAutoConectarWsa: " + ex.Message);
		}
		return false;
	}

	public (bool exito, List<(string serial, EstadoDispositivo estado)> dispositivos, string output) ListarDispositivosDetallado()
	{
		var (flag, text, item) = EjecutarComando(new List<string> { "devices" });
		if (!flag)
		{
			return (exito: false, dispositivos: new List<(string, EstadoDispositivo)>(), output: item);
		}
		List<(string, EstadoDispositivo)> list = new List<(string, EstadoDispositivo)>();
		string[] subArray = text.Split('\n')[1..];
		for (int i = 0; i < subArray.Length; i++)
		{
			string[] array = subArray[i].Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
			if (array.Length >= 2 && !string.IsNullOrEmpty(array[0]))
			{
				list.Add((array[0].Trim(), ClasificarEstado(array[1].Trim())));
			}
		}

		// Só tentar auto-conectar WSA se nenhum aparelho USB estiver conectado
		bool temDispositivoUsb = list.Exists(d => d.Item2 == EstadoDispositivo.Conectado && !d.Item1.Contains(':'));
		if (!temDispositivoUsb && list.Count == 0)
		{
			if (IntentarAutoConectarWsa())
			{
				var (flag2, text2, _) = EjecutarComando(new List<string> { "devices" });
				if (flag2)
				{
					list.Clear();
					string[] sub2 = text2.Split('\n')[1..];
					for (int i = 0; i < sub2.Length; i++)
					{
						string[] array2 = sub2[i].Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
						if (array2.Length >= 2 && !string.IsNullOrEmpty(array2[0]))
						{
							list.Add((array2[0].Trim(), ClasificarEstado(array2[1].Trim())));
						}
					}
					text = text2;
				}
			}
		}

		return (exito: true, dispositivos: list, output: text);
	}

	public (bool exito, List<string> seriales, string output) ListarDispositivos()
	{
		(bool exito, List<(string serial, EstadoDispositivo estado)> dispositivos, string output) tuple = ListarDispositivosDetallado();
		bool item = tuple.exito;
		List<(string, EstadoDispositivo)> item2 = tuple.dispositivos;
		string item3 = tuple.output;
		List<string> item4 = (from d in item2
			where d.Item2 == EstadoDispositivo.Conectado
			select d.Item1).ToList();
		return (exito: item, seriales: item4, output: item3);
	}

	public (bool exito, string salida) Reconectar(string? serial = null)
	{
		List<string> args = (string.IsNullOrWhiteSpace(serial) ? new List<string> { "reconnect", "offline" } : new List<string>
		{
			"-s",
			serial.Trim(),
			"reconnect"
		});
		var (flag, text, text2) = EjecutarComando(args, 5000);
		return (exito: flag, salida: flag ? text : text2);
	}

	public Task<(bool, string)> ReconectarAsync(string? serial = null)
	{
		return Task.Run(() => Reconectar(serial));
	}

	public async Task<(bool exito, int exitCode, string salida, string stderr)> VerificarVersionAsync(int timeoutMs = 5000)
	{
		_ = 2;
		try
		{
			ProcessStartInfo startInfo = FabricaProcesos.AdbCapturado(_adbPath, "version");
			using Process proceso = new Process
			{
				StartInfo = startInfo
			};
			proceso.Start();
			Task<string> stdoutTask = proceso.StandardOutput.ReadToEndAsync();
			Task<string> stderrTask = proceso.StandardError.ReadToEndAsync();
			using CancellationTokenSource cts = new CancellationTokenSource(timeoutMs);
			try
			{
				await proceso.WaitForExitAsync(cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (OperationCanceledException)
			{
				try
				{
					if (!proceso.HasExited)
					{
						proceso.Kill();
					}
				}
				catch
				{
				}
				AppLogger.Warn("ADB smoke test: adb version no respondió dentro del timeout");
				return (exito: false, exitCode: int.MinValue, salida: "", stderr: "timeout");
			}
			string salida = await stdoutTask.ConfigureAwait(continueOnCapturedContext: false);
			string text = await stderrTask.ConfigureAwait(continueOnCapturedContext: false);
			if (proceso.ExitCode != 0)
			{
				LogFallo("version", proceso.ExitCode, text);
			}
			return (exito: proceso.ExitCode == 0, exitCode: proceso.ExitCode, salida: salida, stderr: text);
		}
		catch (Exception ex2)
		{
			if (EsBadExeFormat(ex2))
			{
				AppLogger.Error("[ADB-CRASH-NATIVO] adb version: ERROR_BAD_EXE_FORMAT (193) — el binario no es un ejecutable válido.");
				return (exito: false, exitCode: -1073741819, salida: "", stderr: "ERROR_BAD_EXE_FORMAT (193): el archivo no es un ejecutable Windows válido");
			}
			return (exito: false, exitCode: int.MinValue, salida: "", stderr: ex2.Message);
		}
	}

	public (bool exito, string mensaje) ReiniciarServidor()
	{
		try
		{
			EjecutarComando(new List<string> { "kill-server" }, 5000);
			MatarProcesosAdbPropios();
			Thread.Sleep(1500);
			var (flag, _, text) = EjecutarComando(new List<string> { "start-server" }, 15000);
			if (flag && _trackSuspendidoPorFallos)
			{
				IniciarTrackDevices();
			}
			return flag ? (exito: true, mensaje: "Servidor ADB reiniciado com sucesso") : (exito: false, mensaje: "Servidor ADB não iniciou: " + text);
		}
		catch (Exception ex)
		{
			return (exito: false, mensaje: "Erro ao reiniciar ADB: " + ex.Message);
		}
	}

	public (bool exito, string mensaje, string error) HabilitarTcpip(int puerto = 5555, string? serial = null)
	{
		var (flag, _, item) = EjecutarComando(CrearArgsConSerial(serial, "tcpip", puerto.ToString()));
		if (!flag)
		{
			return (exito: false, mensaje: "Error habilitando puerto TCP/IP", error: item);
		}
		return (exito: true, mensaje: $"Puerto {puerto} habilitado correctamente", error: "");
	}

	public (bool exito, string ip, string mensaje) DetectarIPDispositivo(string? serial = null)
	{
		string serial2 = ResolverSerialActivo(serial);
		var (flag, stdout, mensaje) = EjecutarComando(CrearArgsConSerial(serial2, "shell", "ip", "addr", "show", "wlan0"));
		if (flag && TryParseIpAddr(stdout, out string ip))
		{
			return (exito: true, ip: ip, mensaje: "IP detectada: " + ip);
		}
		if (EsTimeoutAdb(mensaje))
		{
			return (exito: false, ip: "", mensaje: "O celular não respondeu ao ADB.");
		}
		var (flag2, stdout2, mensaje2) = EjecutarComando(CrearArgsConSerial(serial2, "shell", "ip", "route"));
		if (flag2 && TryParseIpRoute(stdout2, out string ip2))
		{
			return (exito: true, ip: ip2, mensaje: "IP detectada: " + ip2);
		}
		if (EsErrorSinDispositivo(mensaje) || EsErrorSinDispositivo(mensaje2))
		{
			return (exito: false, ip: "", mensaje: "Conecte o seu celular por USB");
		}
		return (exito: false, ip: "", mensaje: "Nenhum Wi-Fi ativo detectado no aparelho");
	}

	private List<string> CrearArgsConSerial(string? serial, params string[] args)
	{
		List<string> list = new List<string>();
		if (!string.IsNullOrWhiteSpace(serial))
		{
			list.Add("-s");
			list.Add(serial.Trim());
		}
		list.AddRange(args);
		return list;
	}

	private string? ResolverSerialActivo(string? serial)
	{
		if (!string.IsNullOrWhiteSpace(serial))
		{
			return serial.Trim();
		}
		var (flag, list, _) = ListarDispositivos();
		if (!flag || list.Count == 0)
		{
			return null;
		}
		foreach (string item in list)
		{
			if (!item.Contains(':'))
			{
				return item;
			}
		}
		return list[0];
	}

	private static bool TryParseIpAddr(string stdout, out string ip)
	{
		ip = "";
		string[] array = stdout.Split('\n');
		for (int k = 0; k < array.Length; k++)
		{
			Match match = Regex.Match(array[k], "\\binet\\s+(\\d{1,3}(?:\\.\\d{1,3}){3})/\\d+");
			if (match.Success && EsIpv4Util(match.Groups[1].Value))
			{
				ip = match.Groups[1].Value;
				return true;
			}
		}
		return false;
	}

	private static bool TryParseIpRoute(string stdout, out string ip)
	{
		ip = "";
		string[] array = stdout.Split('\n');
		foreach (string text in array)
		{
			if (EsRutaWifi(text))
			{
				Match match = Regex.Match(text, "\\bsrc\\s+(\\d{1,3}(?:\\.\\d{1,3}){3})\\b");
				if (match.Success && EsIpv4Util(match.Groups[1].Value))
				{
					ip = match.Groups[1].Value;
					return true;
				}
			}
		}
		return false;
	}

	private static bool EsRutaWifi(string linea)
	{
		Match match = Regex.Match(linea, "\\bdev\\s+(\\S+)", RegexOptions.IgnoreCase);
		if (!match.Success)
		{
			return false;
		}
		string text = match.Groups[1].Value.ToLowerInvariant();
		if (!text.Contains("wlan"))
		{
			return text.Contains("wifi");
		}
		return true;
	}

	private static bool EsIpv4Util(string ip)
	{
		if (!string.IsNullOrWhiteSpace(ip) && !ip.StartsWith("127."))
		{
			return ip != "0.0.0.0";
		}
		return false;
	}

	private static bool EsTimeoutAdb(string mensaje)
	{
		if (!mensaje.Contains("no respondió", StringComparison.OrdinalIgnoreCase))
		{
			return mensaje.Contains("timeout", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static bool EsErrorSinDispositivo(string mensaje)
	{
		if (!mensaje.Contains("no devices", StringComparison.OrdinalIgnoreCase) && !mensaje.Contains("device offline", StringComparison.OrdinalIgnoreCase))
		{
			return mensaje.Contains("more than one device", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	public (bool exito, string mensaje, string error) ConectarWifi(string ip, int puerto = 5555)
	{
		string text = $"{ip}:{puerto}";
		var (flag, text2, item) = EjecutarComando(new List<string> { "connect", text });
		if (!flag && !text2.ToLower().Contains("connected"))
		{
			return (exito: false, mensaje: "Não foi possível conectar a " + text, error: item);
		}
		return (exito: true, mensaje: "Conectado com sucesso a " + text, error: "");
	}

	public (bool exito, string mensaje, string error) DesconectarWifi(string ip, int puerto = 5555)
	{
		string text = $"{ip}:{puerto}";
		var (flag, text2, item) = EjecutarComando(new List<string> { "disconnect", text });
		if (!flag && !text2.ToLower().Contains("disconnected"))
		{
			return (exito: false, mensaje: "Erro ao desconectar de " + text, error: item);
		}
		return (exito: true, mensaje: "Desconectado de " + text, error: "");
	}

	public (bool exito, string mensaje) DesconectarTodo()
	{
		var (flag, text, text2) = EjecutarComando(new List<string> { "disconnect" });
		if (!flag && !text.ToLower().Contains("disconnected"))
		{
			return (exito: false, mensaje: "Erro: " + text2);
		}
		return (exito: true, mensaje: "Todas as conexões Wi-Fi desconectadas");
	}

	public async Task<(bool exito, string mensaje, string error)> ParearWifiAsync(string ip, int porta, string codigo)
	{
		string target = $"{ip.Trim()}:{porta}";
		string code = codigo.Trim();
		var (flag, stdout, stderr) = await EjecutarComandoAsync(new List<string> { "pair", target, code }, 12000);
		if (flag && (stdout.Contains("Successfully paired", StringComparison.OrdinalIgnoreCase) || stdout.Contains("paired to", StringComparison.OrdinalIgnoreCase)))
		{
			return (exito: true, mensaje: $"Pareado com sucesso a {target}!", error: "");
		}
		string err = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
		return (exito: false, mensaje: $"Falha ao parear com {target}", error: err);
	}

	public async Task<int> MedirLatenciaAdbAsync()
	{
		try
		{
			var sw = System.Diagnostics.Stopwatch.StartNew();
			var (flag, _, _) = await EjecutarShellAsync("echo 1");
			sw.Stop();
			return flag ? (int)sw.ElapsedMilliseconds : -1;
		}
		catch
		{
			return -1;
		}
	}

	public async Task<(bool exito, List<string> encoders, string melhor)> DescobrirMelhoresEncodersAsync()
	{
		try
		{
			var (flag, stdout, _) = await EjecutarShellAsync("cat /vendor/etc/media_codecs.xml /system/etc/media_codecs.xml | grep -i 'encoder' | grep -i 'avc\\|h264'");
			List<string> encs = new List<string>();
			if (flag && !string.IsNullOrWhiteSpace(stdout))
			{
				var matches = System.Text.RegularExpressions.Regex.Matches(stdout, @"name\s*=\s*""([^""]+)""");
				foreach (System.Text.RegularExpressions.Match m in matches)
				{
					string name = m.Groups[1].Value.Trim();
					if (!string.IsNullOrEmpty(name) && !encs.Contains(name, StringComparer.OrdinalIgnoreCase))
					{
						encs.Add(name);
					}
				}
			}

			// Prioriza hardware encoders de baixa latência (Qualcomm, MediaTek, Exynos, Unisoc)
			string melhor = encs.FirstOrDefault(e => e.StartsWith("c2.qti", StringComparison.OrdinalIgnoreCase))
				?? encs.FirstOrDefault(e => e.StartsWith("c2.mtk", StringComparison.OrdinalIgnoreCase))
				?? encs.FirstOrDefault(e => e.StartsWith("c2.exynos", StringComparison.OrdinalIgnoreCase))
				?? encs.FirstOrDefault(e => e.StartsWith("OMX.qcom", StringComparison.OrdinalIgnoreCase))
				?? encs.FirstOrDefault(e => e.StartsWith("OMX.MTK", StringComparison.OrdinalIgnoreCase))
				?? encs.FirstOrDefault(e => e.StartsWith("OMX.Exynos", StringComparison.OrdinalIgnoreCase))
				?? encs.FirstOrDefault(e => !e.Contains("google", StringComparison.OrdinalIgnoreCase) && !e.Contains("android", StringComparison.OrdinalIgnoreCase))
				?? (encs.Count > 0 ? encs[0] : "c2.android.avc.encoder");

			return (exito: true, encoders: encs, melhor: melhor);
		}
		catch
		{
			return (exito: false, encoders: new List<string>(), melhor: "c2.android.avc.encoder");
		}
	}

	public bool PingDispositivoTcp(string ip, int puerto = 5555, int timeoutMs = 2000)
	{
		try
		{
			using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			IAsyncResult asyncResult = socket.BeginConnect(ip, puerto, null, null);
			bool num = asyncResult.AsyncWaitHandle.WaitOne(timeoutMs, exitContext: true);
			if (num)
			{
				socket.EndConnect(asyncResult);
			}
			return num && socket.Connected;
		}
		catch
		{
			return false;
		}
	}

	public (bool exito, int quantidade, string mensagem) LimpiarConexionesWifi(bool excluirActivas = true)
	{
		try
		{
			var (flag, text, _) = EjecutarComando(new List<string> { "devices", "-l" });
			if (!flag)
			{
				return (exito: false, quantidade: 0, mensagem: "Não foi possível listar os dispositivos");
			}
			List<(string, string)> list = new List<(string, string)>();
			string[] array = text.Split('\n');
			foreach (string text2 in array)
			{
				if (!text2.Contains(':'))
				{
					continue;
				}
				Match match = Regex.Match(text2, "^([\\d\\.]+:\\d+)\\s+(device|offline|unauthorized)");
				if (match.Success)
				{
					string value = match.Groups[1].Value;
					string value2 = match.Groups[2].Value;
					if ((value2 == "offline" || value2 == "unauthorized") ? true : false)
					{
						list.Add((value, value2));
					}
					else if (value2 == "device" && !excluirActivas)
					{
						list.Add((value, value2));
					}
				}
			}
			int num = 0;
			foreach (var item2 in list)
			{
				string item = item2.Item1;
				if (EjecutarComando(new List<string> { "disconnect", item }).exito)
				{
					num++;
				}
			}
			return (num > 0) ? (exito: true, quantidade: num, mensagem: $"Foram removidas {num} conexão(ões) Wi-Fi órfãs") : (exito: true, quantidade: 0, mensagem: "Nenhuma conexão Wi-Fi para limpar");
		}
		catch (Exception ex)
		{
			return (exito: false, quantidade: 0, mensagem: "Erro durante a limpeza: " + ex.Message);
		}
	}

	public (bool exito, string mensaje, string error) CerrarTcpip()
	{
		try
		{
			var (flag, _, text) = EjecutarComando(new List<string> { "kill-server" });
			Thread.Sleep(1000);
			var (flag2, text2, text3) = EjecutarComando(new List<string> { "start-server" }, 15000);
			Thread.Sleep(1500);
			if (!flag)
			{
				string item = (string.IsNullOrWhiteSpace(text) ? "adb kill-server falhou." : text.Trim());
				return (exito: false, mensaje: "Não foi possível reiniciar o servidor ADB.", error: item);
			}
			if (!flag2)
			{
				string item2 = ((!string.IsNullOrWhiteSpace(text3)) ? text3.Trim() : (string.IsNullOrWhiteSpace(text2) ? "adb start-server falhou." : text2.Trim()));
				return (exito: false, mensaje: "Não foi possível iniciar o servidor ADB.", error: item2);
			}
			return (exito: true, mensaje: "Porta Wi-Fi fechada. Servidor ADB reiniciado.", error: "");
		}
		catch (Exception ex)
		{
			return (exito: false, mensaje: "Erro ao fechar porta Wi-Fi: " + ex.Message, error: ex.Message);
		}
	}

	public Task<(bool, string)> ReiniciarServidorAsync()
	{
		return Task.Run(() => ReiniciarServidor());
	}

	public Task<(bool, int, string)> LimpiarConexionesWifiAsync(bool excluirActivas = true)
	{
		return Task.Run(() => {
			var r = LimpiarConexionesWifi(excluirActivas);
			return (r.exito, r.quantidade, r.mensagem);
		});
	}

	public Task<(bool, string, string)> HabilitarTcpipAsync(int puerto = 5555, string? serial = null)
	{
		return Task.Run(() => HabilitarTcpip(puerto, serial));
	}

	public Task<(bool, string, string)> ConectarWifiAsync(string ip, int puerto = 5555)
	{
		return Task.Run(() => ConectarWifi(ip, puerto));
	}

	public Task<(bool, string, string)> DesconectarWifiAsync(string ip, int puerto = 5555)
	{
		return Task.Run(() => DesconectarWifi(ip, puerto));
	}

	public Task<(bool, string)> DesconectarTodoAsync()
	{
		return Task.Run(() => DesconectarTodo());
	}

	public Task<(bool, string, string)> CerrarTcpipAsync()
	{
		return Task.Run(() => CerrarTcpip());
	}

	public Task<(bool, string, string)> AplicarDPIAsync(int nuevoDpi)
	{
		return Task.Run(() => AplicarDPI(nuevoDpi));
	}

	public Task<(bool, string, string)> ResetearDPIAsync()
	{
		return Task.Run(() => ResetearDPI());
	}

	public Task<(bool, string, string)> AplicarResolucionAsync(int ancho, int alto, double aspectRatio)
	{
		return Task.Run(() => AplicarResolucion(ancho, alto, aspectRatio));
	}

	public Task<(bool, string)> ResetearResolucionAsync()
	{
		return Task.Run(() => ResetearResolucion());
	}

	public Task<bool> PingDispositivoTcpAsync(string ip, int puerto = 5555)
	{
		return Task.Run(() => PingDispositivoTcp(ip, puerto));
	}
	public Task<(bool, string, string)> DetectarIPDispositivoAsync(string? serial = null)
	{
		return Task.Run(delegate
		{
			var (item, item2, item3) = DetectarIPDispositivo(serial);
			return (e: item, ip: item2, m: item3);
		});
	}

	public (bool exito, string error) AplicarPointerSpeed(int speed)
	{
		speed = Math.Max(-7, Math.Min(7, speed));
		var (item, _, item2) = EjecutarComando(new List<string>
		{
			"shell",
			"settings",
			"put",
			"system",
			"pointer_speed",
			speed.ToString()
		});
		return (exito: item, error: item2);
	}

	public Task<(bool, string)> AplicarPointerSpeedAsync(int speed)
	{
		return Task.Run(() => AplicarPointerSpeed(speed));
	}

	public async Task<bool> PuxarArquivoAsync(string remoto, string local)
	{
		try
		{
			using Process processo = new Process { StartInfo = FabricaProcesos.AdbCapturado(_adbPath, "pull \"" + remoto + "\" \"" + local + "\"") };
			processo.Start();
			await processo.WaitForExitAsync();
			return processo.ExitCode == 0 && File.Exists(local);
		}
		catch (Exception ex)
		{
			AppLogger.Error("ADB: falha ao transferir arquivo", ex);
			return false;
		}
	}

	public Process? IniciarGravacaoTela(string remoto)
	{
		try
		{
			Process processo = new Process { StartInfo = FabricaProcesos.AdbCapturado(_adbPath, "shell screenrecord --time-limit 180 --bit-rate 16000000 " + remoto) };
			processo.Start();
			return processo;
		}
		catch (Exception ex)
		{
			AppLogger.Error("ADB: falha ao iniciar gravação", ex);
			return null;
		}
	}

	public (bool exito, string stdout, string stderr) EjecutarShell(string comando)
	{
		return EjecutarComando(new List<string> { "shell", comando }, 15000);
	}

	public async Task<(bool, string, string)> EjecutarShellAsync(string comando)
	{
		_ = 2;
		try
		{
			ProcessStartInfo startInfo = FabricaProcesos.AdbCapturado(_adbPath, "shell " + comando);
			using Process proceso = new Process
			{
				StartInfo = startInfo,
				EnableRaisingEvents = true
			};
			proceso.Start();
			Task<string> stdoutTask = proceso.StandardOutput.ReadToEndAsync();
			Task<string> stderrTask = proceso.StandardError.ReadToEndAsync();
			using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(15.0));
			try
			{
				await proceso.WaitForExitAsync(cts.Token);
			}
			catch (OperationCanceledException)
			{
				try
				{
					proceso.Kill();
				}
				catch
				{
				}
				AppLogger.Warn("ADB sem resposta (15s): adb shell " + comando);
				return (false, "", "timeout");
			}
			string stdout = await stdoutTask;
			string text = await stderrTask;
			if (proceso.ExitCode != 0)
			{
				LogFallo("shell " + comando, proceso.ExitCode, text);
			}
			return (proceso.ExitCode == 0, stdout, text);
		}
		catch (Exception ex2)
		{
			if (EsBadExeFormat(ex2))
			{
				AppLogger.Error("[ADB-CRASH-NATIVO] adb no es un ejecutable válido (ERROR_BAD_EXE_FORMAT, 193). shell " + comando);
				return (false, "", "[ADB-CRASH-NATIVO] ERROR_BAD_EXE_FORMAT (193)");
			}
			AppLogger.Error("Não foi possível executar o ADB (" + comando + ")", ex2);
			string item = ((ex2 is UnauthorizedAccessException || ex2 is Win32Exception) ? ("[PERMISOS] " + ex2.Message) : ex2.Message);
			return (false, "", item);
		}
	}

	public (bool exito, string mensaje) AplicarUsb()
	{
		var (flag, _, text) = EjecutarComando(new List<string> { "usb" }, 5000);
		if (!flag)
		{
			return (exito: false, mensaje: "Não foi possível mudar para USB: " + text);
		}
		return (exito: true, mensaje: "Dispositivo vuelto a modo USB");
	}

	public void CerrarDaemonLocal()
	{
		try
		{
			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = _adbPath,
				Arguments = "kill-server",
				UseShellExecute = false,
				CreateNoWindow = true
			};
			using Process process = new Process
			{
				StartInfo = startInfo
			};
			process.Start();
			process.WaitForExit(3000);
		}
		catch (Exception)
		{
		}
		MatarProcesosAdbPropios();
	}

	private void MatarProcesosAdbPropios()
	{
		try
		{
			string value = Path.GetDirectoryName(_adbPath) ?? "";
			Process[] processesByName = Process.GetProcessesByName("adb");
			foreach (Process process in processesByName)
			{
				try
				{
					if ((process.MainModule?.FileName ?? "").StartsWith(value, StringComparison.OrdinalIgnoreCase) && !process.HasExited)
					{
						process.Kill();
						process.WaitForExit(1000);
					}
				}
				catch (Exception)
				{
				}
				finally
				{
					process.Dispose();
				}
			}
		}
		catch (Exception)
		{
		}
	}

	public void IniciarTrackDevices()
	{
		if (_trackActivo)
		{
			return;
		}
		_trackActivo = true;
		_trackSuspendidoPorFallos = false;
		int generacion = ++_trackGeneracion;
		Task.Run(delegate
		{
			int num = 0;
			int num2 = 2000;
			while (_trackActivo && generacion == _trackGeneracion)
			{
				Stopwatch stopwatch = Stopwatch.StartNew();
				bool flag = false;
				try
				{
					ProcessStartInfo startInfo = new ProcessStartInfo
					{
						FileName = _adbPath,
						Arguments = "track-devices",
						UseShellExecute = false,
						CreateNoWindow = true,
						RedirectStandardOutput = true,
						StandardOutputEncoding = Encoding.UTF8
					};
					_trackProcess = new Process
					{
						StartInfo = startInfo
					};
					_trackProcess.Start();
					Process trackProcess = _trackProcess;
					List<string> snapshot = new List<string>();
					while (_trackActivo && generacion == _trackGeneracion)
					{
						string text;
						try
						{
							if (!trackProcess.StandardOutput.EndOfStream)
							{
								text = trackProcess.StandardOutput.ReadLine();
								goto IL_00b4;
							}
						}
						catch
						{
						}
						break;
						IL_00b4:
						if (text == null)
						{
							break;
						}
						flag = true;
						if (string.IsNullOrWhiteSpace(text))
						{
							ProcesarSnapshotTrack(snapshot);
							snapshot.Clear();
						}
						else
						{
							snapshot.Add(text);
							// Algumas versões do ADB não encerram o primeiro snapshot
							// imediatamente. Publica o estado conectado sem esperar a linha vazia.
							ProcesarSnapshotTrack(snapshot);
						}
					}
					if (snapshot.Count > 0)
					{
						ProcesarSnapshotTrack(snapshot);
					}
				}
				catch
				{
				}
				finally
				{
					_trackProcess?.Dispose();
					_trackProcess = null;
				}
				if (!_trackActivo || generacion != _trackGeneracion)
				{
					break;
				}
				if (stopwatch.ElapsedMilliseconds < 3000 && !flag)
				{
					num++;
					if (num >= 5)
					{
						AppLogger.Error("[ADB-CRASH-NATIVO] track-devices murió de inmediato 5 veces consecutivas — suspendo el monitoreo de dispositivos (posible binario dañado).");
						_trackSuspendidoPorFallos = true;
						_trackActivo = false;
						this.OnTrackSuspendido?.Invoke();
						break;
					}
					AppLogger.Warn($"ADB track-devices: fallo inmediato #{num}, reintento en {num2} ms.");
					Thread.Sleep(num2);
					num2 = Math.Min(num2 * 2, 32000);
				}
				else
				{
					num = 0;
					num2 = 2000;
					Thread.Sleep(2000);
				}
			}
		});
	}

	private void ProcesarSnapshotTrack(IReadOnlyList<string> linhas)
	{
		List<(string serial, EstadoDispositivo estado)> dispositivos = new List<(string, EstadoDispositivo)>();
		foreach (string linha in linhas)
		{
			string[] partes = linha.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
			if (partes.Length >= 2)
			{
				dispositivos.Add((partes[0].Trim(), ClasificarEstado(partes[1].Trim())));
			}
		}
		var conectado = dispositivos.FirstOrDefault(d => d.estado == EstadoDispositivo.Conectado);
		bool flag = dispositivos.Any(d => d.estado == EstadoDispositivo.Conectado);
		bool flag2 = dispositivos.Any(d => d.estado == EstadoDispositivo.Conectado && !EsSerialWifi(d.serial));
		if (flag != _ultimoEstadoDispositivo)
		{
			_ultimoEstadoDispositivo = flag;
			this.OnDispositivoCambio?.Invoke(flag);
			if (flag)
			{
				this.OnDispositivoConectado?.Invoke(conectado.serial);
				this.OnEstadoConexionCambiado?.Invoke(arg1: true, conectado.serial);
			}
			else
			{
				this.OnDispositivoDesconectado?.Invoke();
				this.OnEstadoConexionCambiado?.Invoke(arg1: false, "");
			}
		}
		if (flag2 != _ultimoEstadoDispositivoUsb)
		{
			_ultimoEstadoDispositivoUsb = flag2;
			this.OnDispositivoUsbCambio?.Invoke(flag2);
		}
		var detalhe = flag ? conectado : dispositivos.FirstOrDefault();
		string text2 = string.Join(";", dispositivos.Select(d => $"{d.serial}|{d.estado}"));
		if (text2 != _ultimoDetalleTrack)
		{
			_ultimoDetalleTrack = text2;
			this.OnEstadoDetalladoCambio?.Invoke(detalhe.serial ?? "", flag ? EstadoDispositivo.Conectado : detalhe.estado);
		}
	}

	public void DetenerTrackDevices()
	{
		_trackActivo = false;
		_trackGeneracion++;
		try
		{
			_trackProcess?.Kill();
		}
		catch
		{
		}
		_trackProcess?.Dispose();
		_trackProcess = null;
	}

	public Task<(bool, int, int, string)> DetectarResolucionAsync() => Task.Run(() => DetectarResolucion());
	public Task<(bool, int, string)> DetectarDPIAsync() => Task.Run(() => DetectarDPI());
}



