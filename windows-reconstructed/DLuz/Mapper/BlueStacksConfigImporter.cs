using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DLuz.Mapper;

/// <summary>
/// Converte a parte estática de um esquema BlueStacks para o formato do
/// Modo DLuzStacks. Condições visuais do emulador não são transportáveis:
/// elas dependem do reconhecimento de elementos da tela feito pelo BlueStacks.
/// </summary>
public static class BlueStacksConfigImporter
{
	private const int BaseWidth = 2400;
	private const int BaseHeight = 1080;

	/// <summary>
	/// Nome legível para o perfil criado a partir de um arquivo do BlueStacks.
	/// Os esquemas internos normalmente se chamam "Smart" ou "Standard", por
	/// isso o nome do pacote é mais útil para o usuário.
	/// </summary>
	public static string SugerirNomePerfil(string path)
	{
		string arquivo = Path.GetFileNameWithoutExtension(path);
		return arquivo.ToLowerInvariant() switch
		{
			"com.dts.freefireth" => "Free Fire",
			"com.dts.freefiremax" => "Free Fire MAX",
			_ => string.IsNullOrWhiteSpace(arquivo) ? "Perfil BlueStacks" : arquivo.Replace('.', ' ')
		};
	}

	public static bool TryImport(string path, out KeymapConfig keymap, out string resumo)
	{
		keymap = new KeymapConfig();
		resumo = "";
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(File.ReadAllText(path));
			if (!jsonDocument.RootElement.TryGetProperty("ControlSchemes", out JsonElement value) || value.ValueKind != JsonValueKind.Array)
			{
				resumo = "O arquivo não possui esquemas de controle BlueStacks.";
				return false;
			}
			JsonElement esquema = value.EnumerateArray().FirstOrDefault((JsonElement item) => Bool(item, "Selected"));
			if (esquema.ValueKind == JsonValueKind.Undefined)
			{
				esquema = value.EnumerateArray().FirstOrDefault();
			}
			if (esquema.ValueKind != JsonValueKind.Object || !esquema.TryGetProperty("GameControls", out JsonElement controles) || controles.ValueKind != JsonValueKind.Array)
			{
				resumo = "Nenhum controle foi encontrado no esquema BlueStacks.";
				return false;
			}

			keymap.ToggleKey = "F1";
			keymap.ExitKey = "Escape";
			keymap.OverlayOpacity = 0.85;
			keymap.Joystick = new JoystickConfig
			{
				CenterX = (int)(BaseWidth * 0.168),
				CenterY = (int)(BaseHeight * 0.751),
				Radius = (int)(BaseWidth * 0.09),
				Keys = new Dictionary<string, int[]>
				{
					["W"] = new int[2] { 0, -1 }, ["S"] = new int[2] { 0, 1 },
					["A"] = new int[2] { -1, 0 }, ["D"] = new int[2] { 1, 0 }
				}
			};
			keymap.Camera = new CameraConfig
			{
				ZoneX = (int)(BaseWidth * 0.7765), ZoneY = BaseHeight / 2,
				Sensitivity = 1.0, SensitivityX = 1.0, SensitivityY = 0.4,
				SensitivityRatioY = 0.4, FreeMouseKey = "X"
			};

			int ignorados = 0;
			int toques = 0;
			HashSet<string> teclasUsadas = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "W", "A", "S", "D", "F1", "X", "Escape" };
			foreach (JsonElement controle in controles.EnumerateArray())
			{
				string tipo = Texto(controle, "Type");
				if (string.Equals(tipo, "Dpad", StringComparison.OrdinalIgnoreCase))
				{
					AplicarDpad(controle, keymap);
					continue;
				}
				if (string.Equals(tipo, "Pan", StringComparison.OrdinalIgnoreCase))
				{
					AplicarPan(controle, keymap);
					continue;
				}
				if (string.Equals(tipo, "Script", StringComparison.OrdinalIgnoreCase))
				{
					AplicarAtalhoScript(controle, keymap);
					continue;
				}
				if (!string.Equals(tipo, "Tap", StringComparison.OrdinalIgnoreCase))
				{
					ignorados++;
					continue;
				}
				string chave = NormalizarTecla(Texto(controle, "Key"));
				if (string.IsNullOrEmpty(chave) || !KeyNames.Resolver(chave).HasValue || !teclasUsadas.Add(chave))
				{
					ignorados++;
					continue;
				}
				keymap.Buttons.Add(new ButtonConfig
				{
					Key = chave,
					X = Coordenada(controle, "X", BaseWidth),
					Y = Coordenada(controle, "Y", BaseHeight),
					Label = TextoGuia(controle, chave),
					Mode = chave.StartsWith("mouse_", StringComparison.Ordinal) ? "hold" : "tap",
					Size = 36.0
				});
				toques++;
			}
			if (toques == 0)
			{
				resumo = "O esquema BlueStacks não possui toques compatíveis.";
				return false;
			}
			string nome = Texto(esquema, "Name");
			resumo = $"Esquema {nome}: {toques} toques, D-pad e mira importados. {ignorados} controles dependentes do BlueStacks foram ignorados.";
			return true;
		}
		catch (Exception ex)
		{
			resumo = "Não foi possível ler o arquivo BlueStacks: " + ex.Message;
			return false;
		}
	}

	private static void AplicarDpad(JsonElement controle, KeymapConfig keymap)
	{
		keymap.Joystick.CenterX = Coordenada(controle, "X", BaseWidth);
		keymap.Joystick.CenterY = Coordenada(controle, "Y", BaseHeight);
		keymap.Joystick.Radius = Math.Max(40, Coordenada(controle, "XRadius", BaseWidth));
		Dictionary<string, int[]> keys = new Dictionary<string, int[]>();
		AdicionarDirecao(keys, NormalizarTecla(Texto(controle, "KeyUp")), 0, -1);
		AdicionarDirecao(keys, NormalizarTecla(Texto(controle, "KeyDown")), 0, 1);
		AdicionarDirecao(keys, NormalizarTecla(Texto(controle, "KeyLeft")), -1, 0);
		AdicionarDirecao(keys, NormalizarTecla(Texto(controle, "KeyRight")), 1, 0);
		if (keys.Count > 0)
		{
			keymap.Joystick.Keys = keys;
		}
	}

	private static void AplicarPan(JsonElement controle, KeymapConfig keymap)
	{
		keymap.Camera.ZoneX = Coordenada(controle, "X", BaseWidth);
		keymap.Camera.ZoneY = Coordenada(controle, "Y", BaseHeight);
		double sensibilidade = Numero(controle, "Sensitivity", 1.0);
		double proporcaoY = Numero(controle, "SensitivityRatioY", 1.0);
		keymap.Camera.Sensitivity = sensibilidade;
		keymap.Camera.SensitivityX = sensibilidade;
		keymap.Camera.SensitivityY = sensibilidade * proporcaoY;
		keymap.Camera.SensitivityRatioY = proporcaoY;
	}

	private static void AplicarAtalhoScript(JsonElement controle, KeymapConfig keymap)
	{
		string chave = NormalizarTecla(Texto(controle, "Key"));
		if (chave == "F1")
		{
			keymap.ToggleKey = chave;
		}
		else if (chave == "X")
		{
			keymap.Camera.FreeMouseKey = chave;
		}
	}

	private static void AdicionarDirecao(Dictionary<string, int[]> keys, string chave, int x, int y)
	{
		if (!string.IsNullOrEmpty(chave) && KeyNames.Resolver(chave).HasValue)
		{
			keys[chave] = new int[2] { x, y };
		}
	}

	private static string TextoGuia(JsonElement controle, string fallback)
	{
		if (controle.TryGetProperty("Guidance", out JsonElement guidance) && guidance.ValueKind == JsonValueKind.Object)
		{
			string texto = Texto(guidance, "Key");
			if (!string.IsNullOrWhiteSpace(texto))
			{
				return texto;
			}
		}
		return fallback;
	}

	private static int Coordenada(JsonElement element, string property, int limite)
	{
		return Math.Clamp((int)Math.Round(Numero(element, property, 50.0) * limite / 100.0), 0, limite - 1);
	}

	private static string Texto(JsonElement element, string property)
	{
		return element.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
	}

	private static bool Bool(JsonElement element, string property)
	{
		return element.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.True;
	}

	private static double Numero(JsonElement element, string property, double fallback)
	{
		if (!element.TryGetProperty(property, out JsonElement value))
		{
			return fallback;
		}
		return value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out double result) ? result : fallback;
	}

	private static string NormalizarTecla(string key)
	{
		key = key.Trim();
		if (key.Contains('+') || key.StartsWith("Gamepad", StringComparison.OrdinalIgnoreCase))
		{
			return "";
		}
		return key.ToLowerInvariant() switch
		{
			"mouselbutton" => "mouse_left",
			"mouserbutton" => "mouse_right",
			"mousembutton" => "mouse_middle",
			"mousexbutton1" => "mouse_x1",
			"mousexbutton2" => "mouse_x2",
			"mousewheelup" => "mouse_wheel_up",
			"mousewheeldown" => "mouse_wheel_down",
			_ => key
		};
	}
}

