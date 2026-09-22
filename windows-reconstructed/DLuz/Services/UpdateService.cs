using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLuz.Helpers;

namespace DLuz.Services;

public sealed class UpdateService : ObservableObject
{
	private static UpdateService? _instance;
	public static UpdateService Instance => _instance ??= new UpdateService();

	private readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };

	public string ServerUrl { get; set; } = "https://lf7k749dplzo1qaa2au0izl1.dluz.com.br";

	private bool _isUpdateAvailable;
	public bool IsUpdateAvailable
	{
		get => _isUpdateAvailable;
		set => SetProperty(ref _isUpdateAvailable, value);
	}

	private bool _isUpdating;
	public bool IsUpdating
	{
		get => _isUpdating;
		set => SetProperty(ref _isUpdating, value);
	}

	private string _newVersion = "";
	public string NewVersion
	{
		get => _newVersion;
		set => SetProperty(ref _newVersion, value);
	}

	private string _changelog = "";
	public string Changelog
	{
		get => _changelog;
		set => SetProperty(ref _changelog, value);
	}

	private string _downloadUrl = "";
	public string DownloadUrl
	{
		get => _downloadUrl;
		set => SetProperty(ref _downloadUrl, value);
	}

	private double _downloadProgress;
	public double DownloadProgress
	{
		get => _downloadProgress;
		set => SetProperty(ref _downloadProgress, value);
	}

	private string _statusTexto = "";
	public string StatusTexto
	{
		get => _statusTexto;
		set => SetProperty(ref _statusTexto, value);
	}

	private RelayCommand? _atualizarCmd;
	public IRelayCommand AtualizarCommand => _atualizarCmd ??= new RelayCommand(() => _ = ExecutarAtualizacaoAsync());

	private UpdateService()
	{
		_ = VerificarAtualizacoesAsync();
	}

	public async Task VerificarAtualizacoesAsync()
	{
		try
		{
			string respJson = await _http.GetStringAsync($"{ServerUrl}/api/version");
			using var doc = JsonDocument.Parse(respJson);
			var root = doc.RootElement;

			if (root.TryGetProperty("version", out var vProp))
			{
				string remoteVerStr = vProp.GetString() ?? "2.0.0";
				string localVerStr = AppInfo.Version;

				if (EversaoMaior(remoteVerStr, localVerStr))
				{
					NewVersion = remoteVerStr;
					Changelog = root.TryGetProperty("changelog", out var c) ? c.GetString() ?? "" : "";
					DownloadUrl = root.TryGetProperty("download_url", out var d) ? d.GetString() ?? "" : "";
					IsUpdateAvailable = true;
				}
			}
		}
		catch { }
	}

	public async Task ExecutarAtualizacaoAsync()
	{
		if (string.IsNullOrEmpty(DownloadUrl))
		{
			ToastService.Mostrar("Link de atualização não encontrado.", ToastTipo.Error);
			return;
		}

		try
		{
			IsUpdating = true;
			StatusTexto = "Baixando atualização...";
			ToastService.Mostrar("Baixando atualização do DLuzMobiladorPlus...", ToastTipo.Info);

			string tempDir = Path.Combine(Path.GetTempPath(), "DLuzUpdate");
			if (Directory.Exists(tempDir))
			{
				try { Directory.Delete(tempDir, true); } catch { }
			}
			Directory.CreateDirectory(tempDir);

			string zipPath = Path.Combine(tempDir, "update.zip");
			string extractDir = Path.Combine(tempDir, "extracted");

			// 1. Download do arquivo
			byte[] zipBytes = await _http.GetByteArrayAsync(DownloadUrl);
			await File.WriteAllBytesAsync(zipPath, zipBytes);

			// 2. Extração
			ZipFile.ExtractToDirectory(zipPath, extractDir, true);

			// 3. Criar Script de Atualização Autônoma (Substituição e Reinício)
			string appDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
			string updaterBat = Path.Combine(tempDir, "updater.cmd");

			string batContent = $@"@echo off
timeout /t 1 /nobreak > nul
taskkill /F /IM DLuzMobiladorPlus.exe > nul 2>&1
timeout /t 1 /nobreak > nul
xcopy /Y /E /I ""{extractDir}\*"" ""{appDir}\"" > nul
start """" ""{appDir}\DLuzMobiladorPlus.exe""
exit
";
			await File.WriteAllTextAsync(updaterBat, batContent);

			// 4. Executar updater e fechar processo atual
			Process.Start(new ProcessStartInfo
			{
				FileName = "cmd.exe",
				Arguments = $"/c \"{updaterBat}\"",
				CreateNoWindow = true,
				UseShellExecute = false
			});

			System.Windows.Application.Current.Shutdown();
		}
		catch (Exception ex)
		{
			IsUpdating = false;
			StatusTexto = "Falha ao atualizar.";
			AppLogger.Error("Falha na auto-atualização", ex);
			ToastService.Mostrar("Não foi possível concluir a atualização automática: " + ex.Message, ToastTipo.Error);
		}
	}

	private static bool EversaoMaior(string remota, string local)
	{
		try
		{
			var vRemota = new Version(remota.TrimStart('v'));
			var vLocal = new Version(local.TrimStart('v'));
			return vRemota > vLocal;
		}
		catch
		{
			return string.Compare(remota, local, StringComparison.OrdinalIgnoreCase) > 0;
		}
	}
}
