using System;
using System.IO;

namespace DLuz.Helpers;

public static class AppMigrator
{
	private enum EstadoArchivo
	{
		NoAplica,
		Copiado,
		Fallo
	}

	public static ResultadoMigracion EjecutarMigracionInicial()
	{
		EstadoArchivo estadoArchivo = MigrarArchivo(AppPaths.LegacyPerfilesPath, AppPaths.PerfilesPath, "perfiles.ini");
		EstadoArchivo estadoArchivo2 = MigrarArchivo(AppPaths.LegacyConfigPath, AppPaths.ConfigPath, "config.ini");
		if (estadoArchivo == EstadoArchivo.Copiado || estadoArchivo2 == EstadoArchivo.Copiado)
		{
			AppLogger.Info("Migrator: migración inicial completada");
		}
		return estadoArchivo switch
		{
			EstadoArchivo.Fallo => ResultadoMigracion.Fallo, 
			EstadoArchivo.Copiado => ResultadoMigracion.Migrado, 
			_ => ResultadoMigracion.NoAplica, 
		};
	}

	private static EstadoArchivo MigrarArchivo(string origen, string destino, string nombre)
	{
		if (File.Exists(destino))
		{
			return EstadoArchivo.NoAplica;
		}
		if (!File.Exists(origen))
		{
			return EstadoArchivo.NoAplica;
		}
		try
		{
			File.Copy(origen, destino, overwrite: false);
			AppLogger.Info($"Migrator: {nombre} copiado de {origen} a {destino}");
		}
		catch (Exception ex)
		{
			AppLogger.Error("Migrator: error crítico copiando " + nombre, ex);
			return EstadoArchivo.Fallo;
		}
		string text = origen + ".legacy";
		try
		{
			if (File.Exists(text))
			{
				File.Delete(text);
			}
			File.Move(origen, text);
			AppLogger.Info("Migrator: archivo viejo renombrado a " + text);
		}
		catch (UnauthorizedAccessException)
		{
			AppLogger.Warn("Migrator: no se pudo renombrar " + nombre + " viejo a .legacy (sin permisos en directorio del ejecutable). El archivo viejo queda intacto, no afecta funcionamiento.");
		}
		catch (Exception ex3)
		{
			AppLogger.Warn("Migrator: rename a .legacy de " + nombre + " falló: " + ex3.Message);
		}
		return EstadoArchivo.Copiado;
	}
}

