using System;
using System.IO;

namespace DLuz;

public static class ArquitecturaHelper
{
	private static bool? _override;

	public static bool SistemaDe64Bits => Environment.Is64BitOperatingSystem;

	public static bool CompatibilidadForzada => !SistemaDe64Bits;

	public static bool ModoCompatibilidad
	{
		get
		{
			if (!CompatibilidadForzada)
			{
				return _override == true;
			}
			return true;
		}
		set
		{
			if (!CompatibilidadForzada)
			{
				_override = value;
			}
		}
	}

	public static string RutaAdb => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "adb", "adb.exe");

	public static string RutaScrcpy => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "scrcpy", ModoCompatibilidad ? "x86" : "x86_64", "scrcpy.exe");

	public static bool ScrcpyDisponible()
	{
		return File.Exists(RutaScrcpy);
	}

	public static string[] DriversValidos(bool x86)
	{
		if (x86)
		{
			return new string[5] { "", "direct3d12", "gpu", "direct3d", "software" };
		}
		return new string[7] { "", "direct3d12", "gpu", "opengl", "opengles2", "direct3d", "software" };
	}

	public static string NombreRender(string valor)
	{
		return valor switch
		{
			"direct3d12" => "DirectX 12", 
			"gpu" => "GPU", 
			"opengl" => "OpenGL", 
			"opengles2" => "OpenGL ES 2", 
			"direct3d" => "Direct3D 9 Legacy", 
			"software" => "Software", 
			_ => "Automático (DirectX 11)", 
		};
	}

	public static (string texto, string valor)[] ObtenerOpcionesRender(bool x86)
	{
		string[] array = DriversValidos(x86);
		(string, string)[] array2 = new(string, string)[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array2[i] = (NombreRender(array[i]), array[i]);
		}
		return array2;
	}

	public static bool EsRenderValido(string valor, bool x86)
	{
		return Array.IndexOf(DriversValidos(x86), valor ?? "") >= 0;
	}
}

