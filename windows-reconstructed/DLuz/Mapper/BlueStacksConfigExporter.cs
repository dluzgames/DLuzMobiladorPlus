using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace DLuz.Mapper;

/// <summary>Exporta o subconjunto estático do Modo DLuzStacks para um .cfg BlueStacks.</summary>
public static class BlueStacksConfigExporter
{
	public static bool TryExport(string path, KeymapConfig keymap, int width, int height)
	{
		try
		{
			width = width > 0 ? width : 2400;
			height = height > 0 ? height : 1080;
			List<Dictionary<string, object>> controls = new List<Dictionary<string, object>>
			{
				Dpad(keymap, width, height), Pan(keymap, width, height)
			};
			foreach (ButtonConfig button in keymap.Buttons)
			{
				if (!string.IsNullOrWhiteSpace(button.Key))
				{
					controls.Add(Tap(button, width, height));
				}
			}
			controls.Add(Script(keymap.ToggleKey, "keyDown NUM1", "keyUp NUM1"));
			if (!string.IsNullOrWhiteSpace(keymap.Camera.FreeMouseKey))
			{
				controls.Add(Script(keymap.Camera.FreeMouseKey, "keyDown NUM2", "keyUp NUM2"));
			}
			Dictionary<string, object> root = new Dictionary<string, object>
			{
				["MetaData"] = new Dictionary<string, object> { ["ParserVersion"] = "17", ["UpdateVersion"] = "1", ["UpdateArticleKey"] = "", ["CloudUpdateTimeUTC"] = "" },
				["ControlSchemes"] = new List<Dictionary<string, object>>
				{
					new Dictionary<string, object>
					{
						["Name"] = "DLuzMObi v2", ["BuiltIn"] = false, ["Selected"] = true,
						["IsBookMarked"] = false, ["IsCategoryVisible"] = true,
						["KeyboardLayout"] = "United States - india", ["GameControls"] = controls
					}
				}
			};
			File.WriteAllText(path, JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static Dictionary<string, object> Dpad(KeymapConfig keymap, int width, int height)
	{
		return new Dictionary<string, object>
		{
			["$type"] = "Dpad, Bluestacks", ["Type"] = "Dpad", ["Tweaks"] = 0, ["Exclusive"] = false,
			["ShowOnOverlay"] = true, ["X"] = Percent(keymap.Joystick.CenterX, width), ["Y"] = Percent(keymap.Joystick.CenterY, height),
			["KeyUp"] = Direction(keymap, 0, -1, "W"), ["KeyDown"] = Direction(keymap, 0, 1, "S"),
			["KeyLeft"] = Direction(keymap, -1, 0, "A"), ["KeyRight"] = Direction(keymap, 1, 0, "D"),
			["XRadius"] = Percent(keymap.Joystick.Radius, width), ["DeadzoneRadius"] = 0.0, ["Speed"] = 200.0
		};
	}

	private static Dictionary<string, object> Pan(KeymapConfig keymap, int width, int height)
	{
		double x = Percent(keymap.Camera.ZoneX, width);
		double y = Percent(keymap.Camera.ZoneY, height);
		return new Dictionary<string, object>
		{
			["$type"] = "Pan, Bluestacks", ["Type"] = "Pan", ["Tweaks"] = 16450, ["Exclusive"] = false,
			["ShowOnOverlay"] = true, ["X"] = x, ["Y"] = y, ["LookAroundX"] = x, ["LookAroundY"] = y,
			["Sensitivity"] = CameraX(keymap), ["SensitivityRatioY"] = keymap.Camera.SensitivityRatioY,
			["IsLookAroundEnabled"] = true, ["IsShootOnClickEnabled"] = true, ["KeyStartStop"] = "Num1", ["KeySuspend"] = "Num2", ["KeyLookAround"] = "V"
		};
	}

	private static Dictionary<string, object> Tap(ButtonConfig button, int width, int height)
	{
		return new Dictionary<string, object>
		{
			["$type"] = "Tap, Bluestacks", ["Type"] = "Tap", ["Tweaks"] = 0, ["Exclusive"] = false,
			["ShowOnOverlay"] = true, ["X"] = Percent(button.X, width), ["Y"] = Percent(button.Y, height), ["Key"] = BlueStacksKey(button.Key)
		};
	}

	private static Dictionary<string, object> Script(string key, string down, string up)
	{
		return new Dictionary<string, object>
		{
			["$type"] = "Script, Bluestacks", ["Type"] = "Script", ["Tweaks"] = 0, ["Exclusive"] = false,
			["ShowOnOverlay"] = false, ["X"] = 0.0, ["Y"] = 0.0, ["Key"] = BlueStacksKey(key),
			["Commands"] = new[] { down, "onRelease", up }
		};
	}

	private static string Direction(KeymapConfig keymap, int x, int y, string fallback)
	{
		foreach (KeyValuePair<string, int[]> pair in keymap.Joystick.Keys)
		{
			if (pair.Value?.Length >= 2 && pair.Value[0] == x && pair.Value[1] == y)
			{
				return BlueStacksKey(pair.Key);
			}
		}
		return fallback;
	}

	private static double CameraX(KeymapConfig keymap)
	{
		return keymap.Camera.SensitivityX > 0.0 ? keymap.Camera.SensitivityX : keymap.Camera.Sensitivity;
	}

	private static double Percent(int value, int total)
	{
		return Math.Round(Math.Clamp((double)value / total * 100.0, 0.0, 100.0), 2);
	}

	private static string BlueStacksKey(string key)
	{
		return key.Trim().ToLowerInvariant() switch
		{
			"mouse_left" => "MouseLButton", "mouse_right" => "MouseRButton", "mouse_middle" => "MouseMButton",
			"mouse_x1" => "MouseXButton1", "mouse_x2" => "MouseXButton2",
			"mouse_wheel_up" => "MouseWheelUp", "mouse_wheel_down" => "MouseWheelDown", _ => key
		};
	}
}

