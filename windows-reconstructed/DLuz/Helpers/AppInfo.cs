using System;
using System.Reflection;

namespace DLuz.Helpers;

public static class AppInfo
{
	public const string Nombre = "DluzMobiladorPlus";

	public const string Edicion = "Edição Plus";

	public const string Canal = "Estável";

	public static string Version
	{
		get
		{
			Version version = Assembly.GetExecutingAssembly().GetName().Version;
			if (!(version != null))
			{
				return "2.1.0";
			}
			return version.Build == 0 ? $"{version.Major}.{version.Minor}" : $"{version.Major}.{version.Minor}.{version.Build}";
		}
	}

	public static string VersionConPrefijo => "v" + Version;
}

