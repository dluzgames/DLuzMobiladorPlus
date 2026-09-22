using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using DLuz.Helpers;

namespace DLuz;

public static class FabricaProcesos
{
	public static ProcessStartInfo ScrcpyDirecto(string scrcpyPath, string adbPath, string args)
	{
		ProcessStartInfo processStartInfo = new ProcessStartInfo();
		processStartInfo.FileName = scrcpyPath;
		processStartInfo.Arguments = args;
		processStartInfo.UseShellExecute = false;
		processStartInfo.CreateNoWindow = true;
		processStartInfo.RedirectStandardOutput = true;
		processStartInfo.RedirectStandardError = true;
		processStartInfo.StandardOutputEncoding = Encoding.UTF8;
		processStartInfo.StandardErrorEncoding = Encoding.UTF8;
		processStartInfo.WorkingDirectory = DirectorioTrabajo(scrcpyPath);
		processStartInfo.Environment["ADB"] = adbPath;
		return processStartInfo;
	}

	public static ProcessStartInfo ScrcpyConsolaDebug(string scrcpyPath, string adbPath, string args, string titulo, string etiqueta, ProcessWindowStyle estilo)
	{
		return new ProcessStartInfo
		{
			FileName = "cmd.exe",
			Arguments = CrearArgumentosCmdDebug(scrcpyPath, adbPath, args, titulo, etiqueta),
			UseShellExecute = true,
			CreateNoWindow = false,
			WindowStyle = estilo,
			WorkingDirectory = DirectorioTrabajo(scrcpyPath)
		};
	}

	public static ProcessStartInfo AdbCapturado(string adbPath, string args)
	{
		return new ProcessStartInfo
		{
			FileName = adbPath,
			Arguments = args,
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			StandardOutputEncoding = Encoding.UTF8,
			StandardErrorEncoding = Encoding.UTF8,
			WorkingDirectory = DirectorioTrabajo(adbPath)
		};
	}

	public static ProcessStartInfo AdbSilencioso(string adbPath, string args)
	{
		return new ProcessStartInfo
		{
			FileName = adbPath,
			Arguments = args,
			UseShellExecute = false,
			CreateNoWindow = true,
			WorkingDirectory = DirectorioTrabajo(adbPath)
		};
	}

	private static string DirectorioTrabajo(string exePath)
	{
		return Path.GetDirectoryName(exePath) ?? AppDomain.CurrentDomain.BaseDirectory;
	}

	private static string CrearArgumentosCmdDebug(string scrcpyPath, string adbPath, string args, string titulo, string etiqueta)
	{
		string value = EscaparCmd(args);
		string value2 = EscaparCmd(SanitizarLinea(args));
		string value3 = EscaparValorEntreComillas(scrcpyPath);
		return $"/K \"title {EscaparCmd(titulo)} && set \"ADB={EscaparValorEntreComillas(adbPath)}\" && echo {EscaparCmd(etiqueta)} {value2} && call \"{value3}\" {value}\"";
	}

	private static string EscaparCmd(string valor)
	{
		valor = NeutralizarSinEscape(valor);
		return valor.Replace("^", "^^").Replace("&", "^&").Replace("|", "^|")
			.Replace("<", "^<")
			.Replace(">", "^>");
	}

	private static string EscaparValorEntreComillas(string valor)
	{
		return NeutralizarSinEscape(valor);
	}

	private static string NeutralizarSinEscape(string valor)
	{
		if (valor == null)
		{
			valor = "";
		}
		if (valor.IndexOf('%') >= 0 || valor.IndexOf('"') >= 0)
		{
			try
			{
				AppLogger.Warn("Vía debug: se eliminaron caracteres sin escape fiable en cmd (% o \") de: " + valor);
			}
			catch
			{
			}
			valor = valor.Replace("%", "").Replace("\"", "");
		}
		return valor;
	}

	private static string SanitizarLinea(string comando)
	{
		if (string.IsNullOrWhiteSpace(comando))
		{
			return "";
		}
		return comando.Replace("\r", " ").Replace("\n", " ").Trim();
	}
}

