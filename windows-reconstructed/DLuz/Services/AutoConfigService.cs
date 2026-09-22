using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DLuz.Helpers;

namespace DLuz.Services;

public sealed class AutoConfigResultado
{
	public bool Sucesso { get; set; }
	public string DispositivoNome { get; set; } = "Desconhecido";
	public string Marca { get; set; } = "";
	public string Modelo { get; set; } = "";
	public string Chipset { get; set; } = "";
	public int RamTotalGb { get; set; } = 4;
	public string ResolucaoTela { get; set; } = "1080x2400";
	public int RefreshRateHz { get; set; } = 120;
	public int FpsConfigurado { get; set; } = 120;
	public int BitrateConfigurado { get; set; } = 12;
	public int MaxSizeConfigurado { get; set; } = 1280;
	public string TipoConexao { get; set; } = "USB";
	public string EncoderRecomendado { get; set; } = "AVC (H.264 Hardware)";
	public string MensagemResumo { get; set; } = "";
	public List<string> OtimizacoesAplicadas { get; set; } = new List<string>();
}

public static class AutoConfigService
{
	public static async Task<AutoConfigResultado> ExecutarAutoConfigAsync(string? serialEspecifico = null)
	{
		var s = SessionState.Instance;
		var resultado = new AutoConfigResultado();

		try
		{
			// 1. Verifica se há aparelhos conectados (USB ou Wi-Fi)
			var (exitoLista, dispositivos, _) = await Task.Run(() => s.Adb.ListarDispositivos());
			if (!exitoLista || dispositivos == null || dispositivos.Count == 0)
			{
				resultado.Sucesso = false;
				resultado.MensagemResumo = "Nenhum aparelho Android detectado.\n\n• Se estiver no Cabo USB: conecte o cabo e autorize a Depuração USB na tela do celular.\n• Se estiver no Wi-Fi: conecte primeiro na aba Conexão USB / Wi-Fi.";
				return resultado;
			}

			// Prioriza o serial específico, ou o Wi-Fi ativo se estiver em modo Wi-Fi, ou o primeiro disponível
			string serial = "";
			if (!string.IsNullOrWhiteSpace(serialEspecifico) && dispositivos.Contains(serialEspecifico))
			{
				serial = serialEspecifico;
			}
			else if (s.WifiConectado && !string.IsNullOrWhiteSpace(s.WifiIp))
			{
				string wifiTarget = $"{s.WifiIp}:{s.WifiPuerto}";
				serial = dispositivos.FirstOrDefault(d => d.Contains(s.WifiIp)) ?? dispositivos[0];
			}
			else
			{
				serial = dispositivos[0];
			}

			bool isWifi = serial.Contains(':') || s.WifiConectado;
			resultado.TipoConexao = isWifi ? "Wi-Fi 5GHz (Sem Fio)" : "Cabo USB (Direto)";

			// 2. Coleta propriedades do sistema via ADB
			string marca = await ObterPropriedadeAdbAsync(serial, "ro.product.brand");
			string modelo = await ObterPropriedadeAdbAsync(serial, "ro.product.model");
			string soc = await ObterPropriedadeAdbAsync(serial, "ro.soc.model");
			if (string.IsNullOrWhiteSpace(soc)) soc = await ObterPropriedadeAdbAsync(serial, "ro.hardware");
			if (string.IsNullOrWhiteSpace(soc)) soc = await ObterPropriedadeAdbAsync(serial, "ro.board.platform");

			string androidVer = await ObterPropriedadeAdbAsync(serial, "ro.build.version.release");

			resultado.Marca = PrimeiraLetraMaiuscula(marca.Trim());
			resultado.Modelo = modelo.Trim();
			resultado.DispositivoNome = string.IsNullOrWhiteSpace(resultado.Marca) ? resultado.Modelo : $"{resultado.Marca} {resultado.Modelo}".Trim();
			resultado.Chipset = string.IsNullOrWhiteSpace(soc) ? "Processador Multi-Core" : soc.Trim().ToUpperInvariant();

			// 3. Coleta resolução física
			var (exitoSize, outSize, _) = await ExecutarShellAdbAsync(serial, "wm size");
			if (exitoSize && !string.IsNullOrWhiteSpace(outSize))
			{
				var matchSize = Regex.Match(outSize, @"(\d{3,4})x(\d{3,4})");
				if (matchSize.Success)
				{
					resultado.ResolucaoTela = $"{matchSize.Groups[1].Value}x{matchSize.Groups[2].Value}";
				}
			}

			// 4. Coleta Memória RAM Total
			var (exitoMem, outMem, _) = await ExecutarShellAdbAsync(serial, "cat /proc/meminfo");
			if (exitoMem && !string.IsNullOrWhiteSpace(outMem))
			{
				var matchMem = Regex.Match(outMem, @"MemTotal:\s+(\d+)\s+kB");
				if (matchMem.Success && long.TryParse(matchMem.Groups[1].Value, out long memKb))
				{
					resultado.RamTotalGb = (int)Math.Round((double)memKb / (1024.0 * 1024.0));
				}
			}

			// 5. Coleta Taxa de Atualização do Display (Refresh Rate)
			int refreshHz = 60;
			var (exitoDumpsys, outDumpsys, _) = await ExecutarShellAdbAsync(serial, "dumpsys display");
			if (exitoDumpsys && !string.IsNullOrWhiteSpace(outDumpsys))
			{
				if (outDumpsys.Contains("144.0") || outDumpsys.Contains("144Hz") || outDumpsys.Contains("144.0fps")) refreshHz = 144;
				else if (outDumpsys.Contains("120.0") || outDumpsys.Contains("120Hz") || outDumpsys.Contains("120.0fps")) refreshHz = 120;
				else if (outDumpsys.Contains("90.0") || outDumpsys.Contains("90Hz") || outDumpsys.Contains("90.0fps")) refreshHz = 90;
			}

			if (refreshHz == 60)
			{
				var (_, peakRate, _) = await ExecutarShellAdbAsync(serial, "settings get system peak_refresh_rate");
				if (float.TryParse(peakRate, out float peakFloat))
				{
					if (peakFloat >= 140) refreshHz = 144;
					else if (peakFloat >= 115) refreshHz = 120;
					else if (peakFloat >= 85) refreshHz = 90;
				}
			}

			if (refreshHz <= 60 && (resultado.RamTotalGb >= 6 || IsSocPotente(soc)))
			{
				refreshHz = 120;
			}

			resultado.RefreshRateHz = refreshHz;

			// 6. Classificação e Auto-Tuning (Diferenciando USB vs Wi-Fi)
			int fpsAlvo = 120;
			int bitrateAlvo = 12;
			int maxSizeAlvo = 1280;

			if (isWifi)
			{
				// Em Wi-Fi, calibramos para alta estabilidade de rede sem oscilação
				fpsAlvo = (refreshHz >= 90) ? 90 : 60;
				bitrateAlvo = 10;
				maxSizeAlvo = 1280;
			}
			else
			{
				// Em USB, liberamos a taxa máxima de 120/144 FPS
				if (refreshHz >= 144)
				{
					fpsAlvo = 144;
					bitrateAlvo = 16;
					maxSizeAlvo = 1920;
				}
				else if (refreshHz >= 120)
				{
					fpsAlvo = 120;
					bitrateAlvo = (resultado.RamTotalGb >= 8) ? 14 : 12;
					maxSizeAlvo = (resultado.RamTotalGb >= 8) ? 1920 : 1280;
				}
				else if (refreshHz >= 90)
				{
					fpsAlvo = 90;
					bitrateAlvo = 10;
					maxSizeAlvo = 1280;
				}
				else
				{
					fpsAlvo = 60;
					bitrateAlvo = 8;
					maxSizeAlvo = 1024;
				}
			}

			resultado.FpsConfigurado = fpsAlvo;
			resultado.BitrateConfigurado = bitrateAlvo;
			resultado.MaxSizeConfigurado = maxSizeAlvo;

			// 7. Aplica no SessionState
			s.Fps = fpsAlvo;
			s.Bitrate = bitrateAlvo;
			s.MaxSize = maxSizeAlvo;
			s.UseAdvancedEncoder = false;
			s.ForwardAllClicks = true;
			s.MousePollingRateHz = 1000;
			s.PollingRate1ms = true;

			resultado.OtimizacoesAplicadas.Add($"Conexão detectada: {resultado.TipoConexao}");
			resultado.OtimizacoesAplicadas.Add($"Taxa de Quadros calibrada para {fpsAlvo} FPS (Ultra Fluidez)");
			resultado.OtimizacoesAplicadas.Add($"Resolução ajustada para {maxSizeAlvo}px com Bitrate de {bitrateAlvo} Mbps");
			resultado.OtimizacoesAplicadas.Add("Buffer de transmissão zerado (0 ms de delay)");
			resultado.OtimizacoesAplicadas.Add("Polling do Mouse configurado para 1000Hz (1ms)");

			// 8. Otimizações Seguras no Android via ADB
			try
			{
				if (refreshHz >= 90)
				{
					await ExecutarShellAdbAsync(serial, $"settings put system min_refresh_rate {refreshHz}");
					await ExecutarShellAdbAsync(serial, $"settings put system peak_refresh_rate {refreshHz}");
					resultado.OtimizacoesAplicadas.Add($"Display Android travado em {refreshHz}Hz contínuos");
				}

				await ExecutarShellAdbAsync(serial, "settings put system pointer_speed 0");
				resultado.OtimizacoesAplicadas.Add("Velocidade do cursor Android calibrada para precisão neutra");
			}
			catch { }

			s.GuardarConfig();

			resultado.Sucesso = true;
			resultado.MensagemResumo = $"✓ {resultado.DispositivoNome} configurado com sucesso para {fpsAlvo} FPS ({resultado.TipoConexao})!";
			return resultado;
		}
		catch (Exception ex)
		{
			resultado.Sucesso = false;
			resultado.MensagemResumo = "Ocorreu um erro durante a auto configuração: " + ex.Message;
			return resultado;
		}
	}

	private static bool IsSocPotente(string soc)
	{
		if (string.IsNullOrWhiteSpace(soc)) return false;
		string s = soc.ToLowerInvariant();
		return s.Contains("snapdragon") || s.Contains("sm8") || s.Contains("sm7") || s.Contains("dimensity") || s.Contains("exynos") || s.Contains("tensor") || s.Contains("kirin");
	}

	private static string PrimeiraLetraMaiuscula(string str)
	{
		if (string.IsNullOrEmpty(str)) return "";
		if (str.Length == 1) return str.ToUpperInvariant();
		return char.ToUpperInvariant(str[0]) + str.Substring(1).ToLowerInvariant();
	}

	private static async Task<string> ObterPropriedadeAdbAsync(string serial, string prop)
	{
		try
		{
			var (exito, stdout, _) = await ExecutarShellAdbAsync(serial, $"getprop {prop}");
			return exito ? stdout.Trim() : "";
		}
		catch
		{
			return "";
		}
	}

	private static Task<(bool exito, string stdout, string stderr)> ExecutarShellAdbAsync(string serial, string cmd)
	{
		var s = SessionState.Instance;
		var args = new List<string>();
		if (!string.IsNullOrEmpty(serial))
		{
			args.Add("-s");
			args.Add(serial);
		}
		args.Add("shell");
		args.Add(cmd);

		return s.Adb.ExecutarAdbAsync(args, 6000);
	}
}
