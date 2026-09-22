using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DLuz.Services;

public static class ScrcpyVersionService
{
	private const int TimeoutMs = 3000;

	private static Task<string?>? _deteccion;

	public static Task<string?> ObtenerAsync()
	{
		return _deteccion ?? (_deteccion = DetectarAsync());
	}

	private static async Task<string?> DetectarAsync()
	{
		_ = 1;
		try
		{
			if (!ArquitecturaHelper.ScrcpyDisponible())
			{
				return null;
			}
			ProcessStartInfo startInfo = FabricaProcesos.ScrcpyDirecto(ArquitecturaHelper.RutaScrcpy, ArquitecturaHelper.RutaAdb, "--version");
			using Process proceso = Process.Start(startInfo);
			if (proceso == null)
			{
				return null;
			}
			Program.AsignarAlJob(proceso.Handle);
			Task<string> salidaTask = proceso.StandardOutput.ReadToEndAsync();
			using CancellationTokenSource cts = new CancellationTokenSource(3000);
			try
			{
				await proceso.WaitForExitAsync(cts.Token);
			}
			catch (OperationCanceledException)
			{
				try
				{
					proceso.Kill(entireProcessTree: true);
				}
				catch
				{
				}
				return null;
			}
			Match match = Regex.Match(await salidaTask, "scrcpy\\s+(\\d[\\w.\\-]*)", RegexOptions.IgnoreCase);
			return match.Success ? match.Groups[1].Value : null;
		}
		catch
		{
			return null;
		}
	}
}

