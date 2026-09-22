using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using DLuz.Helpers;

namespace DLuz.Services;

/// <summary>Captura imagens e grava a tela do Android por ADB para o Modo DLuzStacks.</summary>
public sealed class MediaCaptureService
{
	private const string RemoteVideo = "/sdcard/DLuzMObi_record.mp4";
	private readonly ADBManager _adb;
	private Process? _recording;

	public string PastaDestino { get; private set; }

	public string Formato { get; private set; } = "mp4";

	public bool Gravando => _recording != null && !_recording.HasExited;

	public MediaCaptureService(ADBManager adb)
	{
		_adb = adb;
		PastaDestino = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "DLuzMObi");
		Carregar();
	}

	public void Configurar(string pasta, string formato)
	{
		if (!string.IsNullOrWhiteSpace(pasta))
		{
			PastaDestino = pasta.Trim();
		}
		Formato = string.Equals(formato, "png", StringComparison.OrdinalIgnoreCase) ? "png" : "mp4";
		Directory.CreateDirectory(PastaDestino);
		File.WriteAllLines(AppPaths.CaptureSettingsPath, new[] { PastaDestino, Formato });
	}

	public async Task<(bool ok, string mensagem)> CapturarTelaAsync()
	{
		try
		{
			Directory.CreateDirectory(PastaDestino);
			string arquivo = Path.Combine(PastaDestino, "DLuzMObi_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture) + ".png");
			string remoto = "/sdcard/DLuzMObi_screenshot.png";
			var criar = await _adb.EjecutarShellAsync("screencap -p " + remoto);
			if (!criar.Item1)
			{
				return (false, "Não foi possível criar a captura no celular.");
			}
			return await PuxarArquivoAsync(remoto, arquivo, "Captura salva: ");
		}
		catch (Exception ex)
		{
			AppLogger.Error("Captura de tela falhou", ex);
			return (false, "Erro ao salvar a captura.");
		}
	}

	public async Task<(bool ok, string mensagem)> AlternarGravacaoAsync()
	{
		if (Gravando)
		{
			return await PararGravacaoAsync();
		}
		try
		{
			Directory.CreateDirectory(PastaDestino);
			await _adb.EjecutarShellAsync("rm -f " + RemoteVideo);
			// --time-limit do Android é limitado a 180 segundos para evitar arquivos corrompidos.
			_recording = _adb.IniciarGravacaoTela(RemoteVideo);
			if (_recording == null) return (false, "Não foi possível iniciar a gravação.");
			return (true, "Gravando a tela do celular…");
		}
		catch (Exception ex)
		{
			AppLogger.Error("Início da gravação falhou", ex);
			_recording = null;
			return (false, "Não foi possível iniciar a gravação.");
		}
	}

	private async Task<(bool ok, string mensagem)> PararGravacaoAsync()
	{
		try
		{
			if (_recording != null && !_recording.HasExited) _recording.Kill(entireProcessTree: true);
			_recording?.Dispose();
			_recording = null;
			await Task.Delay(500);
			Directory.CreateDirectory(PastaDestino);
			string arquivo = Path.Combine(PastaDestino, "DLuzMObi_gravacao_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture) + ".mp4");
			return await PuxarArquivoAsync(RemoteVideo, arquivo, "Vídeo salvo: ");
		}
		catch (Exception ex)
		{
			AppLogger.Error("Parada da gravação falhou", ex);
			return (false, "Não foi possível finalizar a gravação.");
		}
	}

	private async Task<(bool ok, string mensagem)> PuxarArquivoAsync(string remoto, string local, string prefixo)
	{
		if (!await _adb.PuxarArquivoAsync(remoto, local)) return (false, "Não foi possível transferir o arquivo do celular.");
		await _adb.EjecutarShellAsync("rm -f " + remoto);
		return (true, prefixo + local);
	}

	private void Carregar()
	{
		try
		{
			if (!File.Exists(AppPaths.CaptureSettingsPath)) return;
			string[] linhas = File.ReadAllLines(AppPaths.CaptureSettingsPath);
			if (linhas.Length > 0 && !string.IsNullOrWhiteSpace(linhas[0])) PastaDestino = linhas[0];
			if (linhas.Length > 1) Formato = linhas[1];
		}
		catch { }
	}
}

