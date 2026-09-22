using System;
using System.IO;
using System.Text;

namespace DLuz.Helpers;

public static class AppLogger
{
	private static readonly object _lock;

	private const long MaxBytes = 1048576L;

	public static string LogPath { get; private set; }

	static AppLogger()
	{
		_lock = new object();
		string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DLuz", "logs");
		LogPath = Path.Combine(text, "dluz.log");
		try
		{
			Directory.CreateDirectory(text);
		}
		catch
		{
		}
	}

	public static void Info(string mensaje)
	{
		Escribir("INFO", mensaje, null);
	}

	public static void Warn(string mensaje)
	{
		Escribir("WARN", mensaje, null);
	}

	public static void Error(string mensaje, Exception? ex = null)
	{
		Escribir("ERROR", mensaje, ex);
	}

	private static void Escribir(string nivel, string mensaje, Exception? ex)
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append('[').Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append("] ");
			stringBuilder.Append('[').Append(nivel).Append("] ");
			stringBuilder.AppendLine(mensaje);
			if (ex != null)
			{
				for (Exception? atual = ex; atual != null; atual = atual.InnerException)
				{
					stringBuilder.Append("    ").Append(atual.GetType().Name).Append(": ")
						.AppendLine(atual.Message);
					if (!string.IsNullOrEmpty(atual.StackTrace))
					{
						stringBuilder.AppendLine(atual.StackTrace);
					}
				}
			}
			lock (_lock)
			{
				RotarSiNecesario();
				File.AppendAllText(LogPath, stringBuilder.ToString(), Encoding.UTF8);
			}
		}
		catch
		{
		}
	}

	private static void RotarSiNecesario()
	{
		try
		{
			FileInfo fileInfo = new FileInfo(LogPath);
			if (fileInfo.Exists && fileInfo.Length > 1048576)
			{
				string text = LogPath + ".1";
				if (File.Exists(text))
				{
					File.Delete(text);
				}
				File.Move(LogPath, text);
			}
		}
		catch
		{
		}
	}
}

