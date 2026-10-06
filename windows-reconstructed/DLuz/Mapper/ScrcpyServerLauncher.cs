using System;
using System.Diagnostics;
using System.IO;
using DLuz.Helpers;

namespace DLuz.Mapper;

public sealed class ScrcpyServerLauncher
{
	private const string RemotePath = "/data/local/tmp/scrcpy-server-lyxel.jar";

	private readonly string _adbPath;

	private Process? _serverProc;

	private string _scid = "";

	private int _puertoForward;

	public int Factor { get; private set; } = 1;

	public ScrcpyServerLauncher(string adbPath)
	{
		_adbPath = adbPath;
	}

	public int Iniciar(string? serial, string versionServer, int displayId = -1)
	{
		string text = Path.Combine(Path.GetDirectoryName(ArquitecturaHelper.RutaScrcpy) ?? AppContext.BaseDirectory, "scrcpy-server");
		if (!File.Exists(text))
		{
			AppLogger.Error("ScrcpyServerLauncher: no se encontró scrcpy-server en " + text);
			return 0;
		}
		if (string.IsNullOrWhiteSpace(serial))
		{
			try
			{
				var (okList, list, _) = new ADBManager(_adbPath).ListarDispositivos();
				if (okList && list.Count > 0)
				{
					serial = list.Find(d => !d.Contains(':')) ?? list[0];
				}
			}
			catch { }
		}
		_scid = GenerarScid();
		string text2 = (string.IsNullOrWhiteSpace(serial) ? "" : ("-s " + serial.Trim() + " "));
		var (flag, _, text3) = EjecutarAdb($"{text2}push \"{text}\" {"/data/local/tmp/scrcpy-server-lyxel.jar"}", 15000);
		if (!flag)
		{
			AppLogger.Error("ScrcpyServerLauncher: push falló: " + text3);
			return 0;
		}
		var (flag2, text4, value) = EjecutarAdb(text2 + "forward tcp:0 localabstract:scrcpy_" + _scid, 8000);
		if (!flag2 || !int.TryParse(text4.Trim(), out _puertoForward) || _puertoForward <= 0)
		{
			AppLogger.Error($"ScrcpyServerLauncher: forward falló: {value} ({text4})");
			return 0;
		}
		string text5 = $"{text2}shell CLASSPATH={"/data/local/tmp/scrcpy-server-lyxel.jar"} app_process / com.genymobile.scrcpy.Server {versionServer} scid={_scid} log_level=warn video=false audio=false control=true tunnel_forward=true " + "send_device_meta=false send_dummy_byte=true cleanup=true";
		if (displayId >= 0)
		{
			text5 += $" display_id={displayId}";
		}
		try
		{
			ProcessStartInfo startInfo = FabricaProcesos.AdbCapturado(_adbPath, text5);
			_serverProc = new Process
			{
				StartInfo = startInfo,
				EnableRaisingEvents = true
			};
			_serverProc.ErrorDataReceived += delegate(object _, DataReceivedEventArgs e)
			{
				if (!string.IsNullOrWhiteSpace(e.Data))
				{
					AppLogger.Info("svc: " + e.Data);
				}
			};
			_serverProc.Start();
			_serverProc.BeginOutputReadLine();
			_serverProc.BeginErrorReadLine();
			try
			{
				Program.AsignarAlJob(_serverProc.Handle);
			}
			catch
			{
			}
		}
		catch (Exception ex)
		{
			AppLogger.Error("ScrcpyServerLauncher: no se pudo lanzar app_process", ex);
			RemoverForward(text2);
			return 0;
		}
		return _puertoForward;
	}

	public void Detener(string? serial)
	{
		string prefijoSerial = (string.IsNullOrWhiteSpace(serial) ? "" : ("-s " + serial + " "));
		try
		{
			Process serverProc = _serverProc;
			if (serverProc != null && !serverProc.HasExited)
			{
				_serverProc.Kill(entireProcessTree: true);
			}
		}
		catch
		{
		}
		finally
		{
			_serverProc?.Dispose();
			_serverProc = null;
		}
		RemoverForward(prefijoSerial);
	}

	private void RemoverForward(string prefijoSerial)
	{
		if (_puertoForward > 0)
		{
			try
			{
				EjecutarAdb($"{prefijoSerial}forward --remove tcp:{_puertoForward}", 5000);
			}
			catch
			{
			}
			_puertoForward = 0;
		}
	}

	private static string GenerarScid()
	{
		return ((Environment.TickCount ^ (Environment.ProcessId << 16)) & 0x7FFFFFFF).ToString("x8");
	}

	private (bool ok, string stdout, string stderr) EjecutarAdb(string args, int timeoutMs)
	{
		try
		{
			ProcessStartInfo startInfo = FabricaProcesos.AdbCapturado(_adbPath, args);
			using Process process = new Process
			{
				StartInfo = startInfo
			};
			process.Start();
			string item = process.StandardOutput.ReadToEnd();
			string item2 = process.StandardError.ReadToEnd();
			if (!process.WaitForExit(timeoutMs))
			{
				try
				{
					process.Kill(entireProcessTree: true);
				}
				catch
				{
				}
				return (ok: false, stdout: item, stderr: "timeout");
			}
			return (ok: process.ExitCode == 0, stdout: item, stderr: item2);
		}
		catch (Exception ex)
		{
			return (ok: false, stdout: "", stderr: ex.Message);
		}
	}
}

