using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using DLuz.Helpers;

namespace DLuz.Services;

public sealed class LicenseService : ObservableObject
{
	private static LicenseService? _instance;
	public static LicenseService Instance => _instance ??= new LicenseService();

	private readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(6) };

	public string Hwid => HwidHelper.ObterHwid();

	public string ServerUrl { get; set; } = "https://lf7k749dplzo1qaa2au0izl1.dluz.com.br";

#if DEBUG
	private bool _isLicensed = true;
	public bool IsLicensed
	{
		get => _isLicensed;
		set => SetProperty(ref _isLicensed, value);
	}

	private bool _isTrial = false;
	public bool IsTrial
	{
		get => _isTrial;
		set => SetProperty(ref _isTrial, value);
	}

	private bool _isExpired = false;
	public bool IsExpired
	{
		get => _isExpired;
		set => SetProperty(ref _isExpired, value);
	}
#else
	private bool _isLicensed;
	public bool IsLicensed
	{
		get => _isLicensed;
		set => SetProperty(ref _isLicensed, value);
	}

	private bool _isTrial = true;
	public bool IsTrial
	{
		get => _isTrial;
		set => SetProperty(ref _isTrial, value);
	}

	private bool _isExpired;
	public bool IsExpired
	{
		get => _isExpired;
		set => SetProperty(ref _isExpired, value);
	}
#endif

	public bool TieneAccesoPro => IsLicensed;

	public bool VerificarOuBloquearPro(string nomeRecurso, System.Windows.Window? owner = null)
	{
		if (IsLicensed)
		{
			return true;
		}

		System.Windows.Application.Current?.Dispatcher?.BeginInvoke((Action)(() =>
		{
			try
			{
				ToastService.Mostrar($"🔒 '{nomeRecurso}' é exclusivo da Licença PRO Vitalícia!", ToastTipo.Advertencia);
				var win = new DLuz.Views.LicencaWindow
				{
					Owner = owner ?? System.Windows.Application.Current.MainWindow
				};
				win.ShowDialog();
			}
			catch { }
		}));
		return false;
	}

	private int _hoursLeft = 72;
	public int HoursLeft
	{
		get => _hoursLeft;
		set => SetProperty(ref _hoursLeft, value);
	}

	private string _tipoPlano = "vitalicio";
	public string TipoPlano
	{
		get => _tipoPlano;
		set => SetProperty(ref _tipoPlano, value);
	}

	private int _diasRestantes;
	public int DiasRestantes
	{
		get => _diasRestantes;
		set => SetProperty(ref _diasRestantes, value);
	}

	private int _minutesLeft;
	public int MinutesLeft
	{
		get => _minutesLeft;
		set => SetProperty(ref _minutesLeft, value);
	}

#if DEBUG
	private string _statusTexto = "⭐ Licença Pro Vitalícia (DLuz Games Dev)";
#else
	private string _statusTexto = "⚡ Teste Grátis: 3 dias restantes";
#endif
	public string StatusTexto
	{
		get => _statusTexto;
		set => SetProperty(ref _statusTexto, value);
	}

	private Brush _statusBrush = new SolidColorBrush(Color.FromRgb(56, 239, 125)); // Verde
	public Brush StatusBrush
	{
		get => _statusBrush;
		set => SetProperty(ref _statusBrush, value);
	}

	private LicenseService()
	{
		_ = ValidarInicialAsync();
	}

	public async Task ValidarInicialAsync()
	{
#if DEBUG
		IsLicensed = true;
		IsTrial = false;
		IsExpired = false;
		StatusTexto = "⭐ Licença Pro Vitalícia (DLuz Games Dev)";
		StatusBrush = new SolidColorBrush(Color.FromRgb(56, 239, 125));
		return;
#endif
		try
		{
			// Tentar checar no servidor
			var payload = new
			{
				hwid = Hwid,
				os = Environment.OSVersion.ToString(),
				version = AppInfo.Version
			};

			string json = JsonSerializer.Serialize(payload);
			var content = new StringContent(json, Encoding.UTF8, "application/json");

			var response = await _http.PostAsync($"{ServerUrl}/api/trial/check", content);
			if (response.IsSuccessStatusCode)
			{
				string respJson = await response.Content.ReadAsStringAsync();
				using var doc = JsonDocument.Parse(respJson);
				var root = doc.RootElement;

				string status = root.GetProperty("status").GetString() ?? "";

				if (status == "licensed")
				{
					string plan = root.TryGetProperty("plan", out var p) ? p.GetString() ?? "vitalicio" : "vitalicio";
					TipoPlano = plan;
					DiasRestantes = root.TryGetProperty("days_left", out var dl) ? dl.GetInt32() : 0;

					IsLicensed = true;
					IsTrial = false;
					IsExpired = false;

					if (plan == "membro_youtube")
					{
						StatusTexto = DiasRestantes > 0 
							? $"🔴 Membro do Canal DLuz ({DiasRestantes}d restantes)" 
							: "🔴 Membro do Canal DLuz Games";
						StatusBrush = new SolidColorBrush(Color.FromRgb(255, 68, 68));
					}
					else if (plan == "mensal")
					{
						StatusTexto = DiasRestantes > 0 
							? $"⭐ Assinatura Mensal Pro ({DiasRestantes}d restantes)" 
							: "⭐ Assinatura Mensal Pro";
						StatusBrush = new SolidColorBrush(Color.FromRgb(56, 239, 125));
					}
					else
					{
						StatusTexto = "⭐ Licença Pro Vitalícia";
						StatusBrush = new SolidColorBrush(Color.FromRgb(56, 239, 125));
					}

					SalvarCacheLocal(72, "licensed");
					return;
				}

				if (status == "expired_license")
				{
					IsLicensed = false;
					IsTrial = true;
					IsExpired = true;
					HoursLeft = 0;
					MinutesLeft = 0;
					StatusTexto = "❌ Assinatura / Membro Expirado (Renove)";
					StatusBrush = new SolidColorBrush(Color.FromRgb(255, 42, 58));
					SalvarCacheLocal(0, "expired");
					return;
				}

				if (status == "active")
				{
					IsLicensed = false;
					IsTrial = true;
					IsExpired = false;
					HoursLeft = root.TryGetProperty("hours_left", out var h) ? h.GetInt32() : 72;
					MinutesLeft = root.TryGetProperty("minutes_left", out var m) ? m.GetInt32() : 0;

					int dias = HoursLeft / 24;
					int horas = HoursLeft % 24;

					StatusTexto = dias > 0 
						? $"⚡ Teste Grátis: {dias}d {horas}h restantes"
						: $"⚡ Teste Grátis: {HoursLeft}h {MinutesLeft}m restantes";

					StatusBrush = HoursLeft > 24 
						? new SolidColorBrush(Color.FromRgb(56, 239, 125)) 
						: new SolidColorBrush(Color.FromRgb(255, 166, 87));

					SalvarCacheLocal(HoursLeft, "trial_active");
					return;
				}

				if (status == "expired" || status == "blocked")
				{
					IsLicensed = false;
					IsTrial = true;
					IsExpired = true;
					HoursLeft = 0;
					MinutesLeft = 0;
					StatusTexto = "❌ Teste Expirado (Adquira a Pro)";
					StatusBrush = new SolidColorBrush(Color.FromRgb(255, 42, 58));
					SalvarCacheLocal(0, "expired");
					return;
				}
			}
		}
		catch
		{
			// Se o servidor ainda não estiver no ar ou sem internet: usar o gerenciador local seguro de 3 dias
			ValidarLocalSeguro();
		}
	}

	public async Task<(bool ok, string mensagem)> AtivarChaveAsync(string chave)
	{
		if (string.IsNullOrWhiteSpace(chave))
		{
			return (false, "Por favor, digite a chave de licença.");
		}

		try
		{
			var payload = new
			{
				key = chave.Trim().ToUpperInvariant(),
				hwid = Hwid
			};

			string json = JsonSerializer.Serialize(payload);
			var content = new StringContent(json, Encoding.UTF8, "application/json");

			var response = await _http.PostAsync($"{ServerUrl}/api/license/activate", content);
			string respJson = await response.Content.ReadAsStringAsync();

			using var doc = JsonDocument.Parse(respJson);
			var root = doc.RootElement;

			if (response.IsSuccessStatusCode && root.TryGetProperty("status", out var st) && st.GetString() == "success")
			{
				string plan = root.TryGetProperty("plan", out var p) ? p.GetString() ?? "vitalicio" : "vitalicio";
				TipoPlano = plan;
				DiasRestantes = root.TryGetProperty("days_left", out var dl) ? dl.GetInt32() : 0;

				IsLicensed = true;
				IsTrial = false;
				IsExpired = false;

				if (plan == "membro_youtube")
				{
					StatusTexto = DiasRestantes > 0 
						? $"🔴 Membro do Canal DLuz ({DiasRestantes}d restantes)" 
						: "🔴 Membro do Canal DLuz Games";
					StatusBrush = new SolidColorBrush(Color.FromRgb(255, 68, 68));
				}
				else if (plan == "mensal")
				{
					StatusTexto = DiasRestantes > 0 
						? $"⭐ Assinatura Mensal Pro ({DiasRestantes}d restantes)" 
						: "⭐ Assinatura Mensal Pro";
					StatusBrush = new SolidColorBrush(Color.FromRgb(56, 239, 125));
				}
				else
				{
					StatusTexto = "⭐ Licença Pro Vitalícia";
					StatusBrush = new SolidColorBrush(Color.FromRgb(56, 239, 125));
				}

				SalvarCacheLocal(9999, "licensed");
				return (true, root.TryGetProperty("message", out var msg) ? msg.GetString() ?? "Licença Pro ativada!" : "Licença Pro ativada com sucesso!");
			}
			else
			{
				string err = root.TryGetProperty("error", out var e) ? e.GetString() ?? "Chave inválida" : "Falha na ativação";
				return (false, err);
			}
		}
		catch (Exception ex)
		{
			AppLogger.Error("Falha na requisição de ativação", ex);
			// Modo offline de validação de chave master se aplicável
			if (chave.StartsWith("DLUZ-PRO-") && chave.Length >= 16)
			{
				IsLicensed = true;
				IsTrial = false;
				IsExpired = false;
				StatusTexto = "⭐ Licença Pro Vitalícia";
				StatusBrush = new SolidColorBrush(Color.FromRgb(56, 239, 125));
				SalvarCacheLocal(9999, "licensed");
				return (true, "Licença ativada localmente!");
			}
			return (false, "Não foi possível conectar ao servidor de licenças. Verifique sua conexão.");
		}
	}

	private void ValidarLocalSeguro()
	{
		string cacheFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DLuz", ".lic.dat");
		try
		{
			if (!File.Exists(cacheFile))
			{
				// Primeiro acesso local
				DateTime inicio = DateTime.UtcNow;
				DateTime exp = inicio.AddDays(3);
				string data = $"{Hwid}|{inicio.Ticks}|{exp.Ticks}|trial";
				byte[] enc = ProtectedData.Protect(Encoding.UTF8.GetBytes(data), null, DataProtectionScope.CurrentUser);
				File.WriteAllBytes(cacheFile, enc);

				IsLicensed = false;
				IsTrial = true;
				IsExpired = false;
				HoursLeft = 72;
				StatusTexto = "⚡ Teste Grátis: 3 dias restantes";
				StatusBrush = new SolidColorBrush(Color.FromRgb(56, 239, 125));
				return;
			}

			byte[] encData = File.ReadAllBytes(cacheFile);
			byte[] decData = ProtectedData.Unprotect(encData, null, DataProtectionScope.CurrentUser);
			string[] parts = Encoding.UTF8.GetString(decData).Split('|');

			if (parts.Length >= 4)
			{
				string hwid = parts[0];
				long expTicks = long.Parse(parts[2]);
				string tipo = parts[3];

				if (tipo == "licensed")
				{
					IsLicensed = true;
					IsTrial = false;
					IsExpired = false;
					StatusTexto = "⭐ Licença Pro Ativa";
					StatusBrush = new SolidColorBrush(Color.FromRgb(56, 239, 125));
					return;
				}

				DateTime exp = new DateTime(expTicks, DateTimeKind.Utc);
				TimeSpan restante = exp - DateTime.UtcNow;

				if (restante.TotalSeconds <= 0)
				{
					IsLicensed = false;
					IsTrial = true;
					IsExpired = true;
					HoursLeft = 0;
					MinutesLeft = 0;
					StatusTexto = "❌ Teste Expirado (Adquira a Pro)";
					StatusBrush = new SolidColorBrush(Color.FromRgb(255, 42, 58));
				}
				else
				{
					IsLicensed = false;
					IsTrial = true;
					IsExpired = false;
					HoursLeft = (int)restante.TotalHours;
					MinutesLeft = restante.Minutes;

					int dias = HoursLeft / 24;
					int horas = HoursLeft % 24;

					StatusTexto = dias > 0 
						? $"⚡ Teste Grátis: {dias}d {horas}h restantes"
						: $"⚡ Teste Grátis: {HoursLeft}h {MinutesLeft}m restantes";

					StatusBrush = new SolidColorBrush(Color.FromRgb(56, 239, 125));
				}
			}
		}
		catch
		{
			IsLicensed = false;
			IsTrial = true;
			IsExpired = false;
			HoursLeft = 72;
			StatusTexto = "⚡ Teste Grátis: 3 dias restantes";
		}
	}

	private void SalvarCacheLocal(int horasRestantes, string tipo)
	{
		try
		{
			string cacheFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DLuz", ".lic.dat");
			DateTime exp = DateTime.UtcNow.AddHours(horasRestantes);
			string data = $"{Hwid}|{DateTime.UtcNow.Ticks}|{exp.Ticks}|{tipo}";
			byte[] enc = ProtectedData.Protect(Encoding.UTF8.GetBytes(data), null, DataProtectionScope.CurrentUser);
			File.WriteAllBytes(cacheFile, enc);
		}
		catch { }
	}
}



