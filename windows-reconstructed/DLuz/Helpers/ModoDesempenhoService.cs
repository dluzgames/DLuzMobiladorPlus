using System;
using System.IO;

namespace DLuz.Helpers;

public static class ModoDesempenhoService
{
	public const string Equilibrado = "balanced";
	public const string AltaPerformance = "high";
	public const string MemoriaBaixa = "low_memory";

	private static string Caminho => Path.Combine(AppPaths.LocalAppDataDir, "performance_mode.txt");

	public static string Carregar()
	{
		try
		{
			string modo = File.Exists(Caminho) ? File.ReadAllText(Caminho).Trim() : Equilibrado;
			return EhValido(modo) ? modo : Equilibrado;
		}
		catch
		{
			return Equilibrado;
		}
	}

	public static void Salvar(string modo)
	{
		if (!EhValido(modo))
		{
			modo = Equilibrado;
		}
		AppPaths.EnsureDirectoriesExist();
		File.WriteAllText(Caminho, modo);
	}

	private static bool EhValido(string modo)
	{
		return string.Equals(modo, Equilibrado, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(modo, AltaPerformance, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(modo, MemoriaBaixa, StringComparison.OrdinalIgnoreCase);
	}
}

