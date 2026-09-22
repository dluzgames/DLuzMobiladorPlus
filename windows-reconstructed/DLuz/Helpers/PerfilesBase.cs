using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using IniParser;
using IniParser.Model;

namespace DLuz.Helpers;

public static class PerfilesBase
{
	private static List<string>? _nombresCache;

	public static string LeerEmbebido()
	{
		try
		{
			using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DLuz.perfiles.base.ini");
			if (stream == null)
			{
				return "";
			}
			using StreamReader streamReader = new StreamReader(stream);
			return streamReader.ReadToEnd();
		}
		catch
		{
			return "";
		}
	}

	public static IReadOnlyList<string> Nombres()
	{
		if (_nombresCache != null)
		{
			return _nombresCache;
		}
		List<string> list = new List<string>();
		try
		{
			IniData iniData = ParsearEmbebido();
			if (iniData != null)
			{
				foreach (SectionData section in iniData.Sections)
				{
					list.Add(section.SectionName);
				}
			}
		}
		catch
		{
		}
		_nombresCache = list;
		return list;
	}

	public static bool EsNombreBase(string nombre)
	{
		foreach (string item in Nombres())
		{
			if (string.Equals(item, nombre, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	public static string LeerSeccionEmbebida(string nombre)
	{
		try
		{
			IniData iniData = ParsearEmbebido();
			if (iniData == null || !iniData.Sections.ContainsSection(nombre))
			{
				return "";
			}
			IniData iniData2 = new IniData();
			iniData2.Sections.AddSection(nombre);
			foreach (KeyData item in iniData[nombre])
			{
				iniData2[nombre][item.KeyName] = item.Value;
			}
			return iniData2.ToString();
		}
		catch
		{
			return "";
		}
	}

	private static IniData? ParsearEmbebido()
	{
		string text = LeerEmbebido();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		return new FileIniDataParser().Parser.Parse(text);
	}
}

