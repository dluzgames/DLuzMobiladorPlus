using System;
using System.IO;

namespace DLuz.Helpers;

public static class AppPaths
{
	public static readonly string LocalAppDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DLuz");

	public static readonly string LogsDir = Path.Combine(LocalAppDataDir, "logs");

	public static readonly string PerfilesPath = Path.Combine(LocalAppDataDir, "perfiles.ini");

	public static readonly string ConfigPath = Path.Combine(LocalAppDataDir, "config.ini");

	public static readonly string PerfilesMetaPath = Path.Combine(LocalAppDataDir, "profiles_meta.json");

	public static readonly string KeymapPath = Path.Combine(LocalAppDataDir, "keymap_freefire.json");

	public static readonly string MapperProfilePath = Path.Combine(LocalAppDataDir, "mapper_profile.json");

	public static readonly string MapperProfilesDir = Path.Combine(LocalAppDataDir, "mapper_profiles");

	public static readonly string BackupsDir = Path.Combine(LocalAppDataDir, "profile_backups");

	public static readonly string CaptureSettingsPath = Path.Combine(LocalAppDataDir, "capture_settings.txt");

	public static readonly string LegacyBaseDir = AppDomain.CurrentDomain.BaseDirectory;

	public static readonly string LegacyPerfilesPath = Path.Combine(LegacyBaseDir, "perfiles.ini");

	public static readonly string LegacyConfigPath = Path.Combine(LegacyBaseDir, "config.ini");

	public static void EnsureDirectoriesExist()
	{
		try
		{
			Directory.CreateDirectory(LocalAppDataDir);
			Directory.CreateDirectory(LogsDir);
		}
		catch (Exception ex)
		{
			AppLogger.Error("AppPaths.EnsureDirectoriesExist falló", ex);
			throw;
		}
	}
}

