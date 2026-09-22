using System;
using DLuz.Helpers;

namespace DLuz.Mapper;

public static class MapperDiag
{
	public static bool Enabled { get; set; }

	public static void Log(string mensaje)
	{
		if (Enabled)
		{
			AppLogger.Info($"[Mapper {DateTime.Now:HH:mm:ss.fff}] {mensaje}");
		}
	}
}

