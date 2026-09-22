using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DLuz.Mapper;

public sealed class KeymapConfig
{
	private const string RecursoEmbebido = "DLuz.keymap.freefire.json";

	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		DictionaryKeyPolicy = null,
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		WriteIndented = true
	};

	public string DeviceResolution { get; set; } = "auto";

	public string ToggleKey { get; set; } = "F1";

	public string ExitKey { get; set; } = "Escape";

	public double OverlayOpacity { get; set; } = 0.85;

	public JoystickConfig Joystick { get; set; } = new JoystickConfig();

	public CameraConfig Camera { get; set; } = new CameraConfig();

	[JsonConverter(typeof(ButtonListConverter))]
	public List<ButtonConfig> Buttons { get; set; } = new List<ButtonConfig>();

	public static readonly Dictionary<string, string> PresetsDisponiveis = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
	{
		["Free Fire / FF MAX"] = "DLuz.keymap.freefire.json",
		["Blood Strike"] = "DLuz.keymap.bloodstrike.json",
		["Call of Duty Mobile"] = "DLuz.keymap.codm.json",
		["PUBG Mobile"] = "DLuz.keymap.pubg.json",
		["Roblox Mobile"] = "DLuz.keymap.roblox.json",
		["Stumble Guys"] = "DLuz.keymap.stumbleguys.json",
		["Minecraft Bedrock"] = "DLuz.keymap.minecraft.json"
	};

	public static KeymapConfig Cargar(string path)
	{
		try
		{
			if (!File.Exists(path))
			{
				string text = LeerEmbebido();
				if (!string.IsNullOrWhiteSpace(text))
				{
					File.WriteAllText(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
				}
			}
			if (File.Exists(path))
			{
				KeymapConfig keymapConfig = JsonSerializer.Deserialize<KeymapConfig>(File.ReadAllText(path), JsonOptions);
				if (keymapConfig != null)
				{
					return keymapConfig;
				}
			}
		}
		catch
		{
		}
		return JsonSerializer.Deserialize<KeymapConfig>(LeerEmbebido(), JsonOptions) ?? new KeymapConfig();
	}

	public static KeymapConfig CargarPreset(string nomeJogo)
	{
		string recurso = "DLuz.keymap.freefire.json";
		if (PresetsDisponiveis.TryGetValue(nomeJogo, out string? res))
		{
			recurso = res;
		}
		else
		{
			foreach (var kvp in PresetsDisponiveis)
			{
				if (kvp.Key.Contains(nomeJogo, System.StringComparison.OrdinalIgnoreCase))
				{
					recurso = kvp.Value;
					break;
				}
			}
		}
		string text = LeerRecurso(recurso);
		return JsonSerializer.Deserialize<KeymapConfig>(text, JsonOptions) ?? new KeymapConfig();
	}

	public bool Guardar(string path)
	{
		try
		{
			string contents = JsonSerializer.Serialize(this, JsonOptions);
			File.WriteAllText(path, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static string LeerEmbebido()
	{
		return LeerRecurso(RecursoEmbebido);
	}

	public static string LeerRecurso(string recurso)
	{
		try
		{
			using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(recurso);
			if (stream == null)
			{
				return "";
			}
			using StreamReader streamReader = new StreamReader(stream, Encoding.UTF8);
			return streamReader.ReadToEnd();
		}
		catch
		{
			return "";
		}
	}
}

