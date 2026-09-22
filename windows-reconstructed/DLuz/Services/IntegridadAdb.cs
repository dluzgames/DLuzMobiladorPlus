using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;

namespace DLuz.Services;

public static class IntegridadAdb
{
	public sealed record ArchivoDanado(string Nombre, string HashEsperado, string HashActual);

	private const string RecursoManifiesto = "DLuz.adb.manifest.txt";

	private const string RecursoRespaldo = "DLuz.adb.respaldo.zip";

	public static (EstadoIntegridad estado, List<ArchivoDanado> danados) Verificar(string carpetaAdb)
	{
		List<(string, string, long)> list = LeerManifiesto();
		if (list.Count == 0)
		{
			return (estado: EstadoIntegridad.SinManifiesto, danados: new List<ArchivoDanado>());
		}
		List<ArchivoDanado> list2 = new List<ArchivoDanado>();
		foreach (var item3 in list)
		{
			string item = item3.Item1;
			string item2 = item3.Item2;
			string text = Path.Combine(carpetaAdb, item);
			if (!File.Exists(text))
			{
				list2.Add(new ArchivoDanado(item, item2, "(ausente)"));
				continue;
			}
			string text2 = HashDeArchivo(text);
			if (!text2.Equals(item2, StringComparison.OrdinalIgnoreCase))
			{
				list2.Add(new ArchivoDanado(item, item2, text2));
			}
		}
		return (estado: (list2.Count != 0) ? EstadoIntegridad.Danado : EstadoIntegridad.Ok, danados: list2);
	}

	public static void Reparar(string carpetaAdb)
	{
		using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DLuz.adb.respaldo.zip") ?? throw new InvalidOperationException("El respaldo embebido de ADB no está disponible en este build.");
		using ZipArchive zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
		foreach (var item2 in LeerManifiesto())
		{
			string item = item2.nombre;
			ZipArchiveEntry zipArchiveEntry = zipArchive.GetEntry(item) ?? throw new InvalidOperationException("El respaldo embebido no contiene '" + item + "'.");
			string text = Path.Combine(carpetaAdb, item);
			string text2 = text + ".lyxel.tmp";
			try
			{
				using (Stream stream2 = zipArchiveEntry.Open())
				{
					using FileStream destination = new FileStream(text2, FileMode.Create, FileAccess.Write);
					stream2.CopyTo(destination);
				}
				if (File.Exists(text))
				{
					File.Replace(text2, text, null);
				}
				else
				{
					File.Move(text2, text);
				}
			}
			catch
			{
				try
				{
					if (File.Exists(text2))
					{
						File.Delete(text2);
					}
				}
				catch
				{
				}
				throw;
			}
		}
	}

	private static List<(string nombre, string hash, long tamano)> LeerManifiesto()
	{
		List<(string, string, long)> list = new List<(string, string, long)>();
		try
		{
			using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DLuz.adb.manifest.txt");
			if (stream == null)
			{
				return list;
			}
			using StreamReader streamReader = new StreamReader(stream);
			string text;
			while ((text = streamReader.ReadLine()) != null)
			{
				string[] array = text.Split('|');
				if (array.Length == 3 && long.TryParse(array[2], out var result))
				{
					list.Add((array[0].Trim(), array[1].Trim(), result));
				}
			}
		}
		catch
		{
			list.Clear();
		}
		return list;
	}

	private static string HashDeArchivo(string ruta)
	{
		using SHA256 sHA = SHA256.Create();
		using FileStream inputStream = File.OpenRead(ruta);
		return Convert.ToHexString(sHA.ComputeHash(inputStream));
	}
}

