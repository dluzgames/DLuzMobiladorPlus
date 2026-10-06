using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DLuz.Mapper;

public static class KeyNames
{
	public const int MouseLeft = 257;

	public const int MouseRight = 258;

	public const int MouseMiddle = 259;

	public const int MouseX1 = 260;

	public const int MouseX2 = 261;

	public const int WheelUp = 262;

	public const int WheelDown = 263;

	private static readonly Dictionary<string, int> Nombrados = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
	{
		["space"] = 32,
		["escape"] = 27,
		["esc"] = 27,
		["tab"] = 9,
		["enter"] = 13,
		["return"] = 13,
		["shift"] = 16,
		["leftshift"] = 160,
		["lshift"] = 160,
		["rightshift"] = 161,
		["rshift"] = 161,
		["control"] = 17,
		["ctrl"] = 17,
		["leftcontrol"] = 162,
		["leftctrl"] = 162,
		["lctrl"] = 162,
		["rightcontrol"] = 163,
		["rightctrl"] = 163,
		["rctrl"] = 163,
		["alt"] = 18,
		["leftalt"] = 164,
		["lalt"] = 164,
		["rightalt"] = 165,
		["ralt"] = 165,
		["oem1"] = 186,
		["oemplus"] = 187,
		["oemcomma"] = 188,
		["oemminus"] = 189,
		["oemperiod"] = 190,
		["oem2"] = 191,
		["oem3"] = 192,
		["oem4"] = 219,
		["oem5"] = 220,
		["oem6"] = 221,
		["oem7"] = 222,
		["oem8"] = 223,
		["oem102"] = 226,
		["backspace"] = 8,
		["capslock"] = 20,
		["insert"] = 45,
		["delete"] = 46,
		["del"] = 46,
		["home"] = 36,
		["end"] = 35,
		["pageup"] = 33,
		["pgup"] = 33,
		["pagedown"] = 34,
		["pgdn"] = 34,
		["left"] = 37,
		["up"] = 38,
		["right"] = 39,
		["down"] = 40,
		["leftmeta"] = 91,
		["lwin"] = 91,
		["rightmeta"] = 92,
		["rwin"] = 92,
		["apps"] = 93,
		["numlock"] = 144,
		["scrolllock"] = 145,
		["pause"] = 19,
		["numpad0"] = 96,
		["numpad1"] = 97,
		["numpad2"] = 98,
		["numpad3"] = 99,
		["numpad4"] = 100,
		["numpad5"] = 101,
		["numpad6"] = 102,
		["numpad7"] = 103,
		["numpad8"] = 104,
		["numpad9"] = 105,
		["multiply"] = 106,
		["add"] = 107,
		["subtract"] = 109,
		["decimal"] = 110,
		["divide"] = 111,
		["mouse_left"] = 257,
		["mouse_right"] = 258,
		["mouse_middle"] = 259,
		["mouse_x1"] = 260,
		["mouse_x2"] = 261,
		["wheel_up"] = 262,
		["wheel_down"] = 263
	};

	public static string NombreBonito(string? nombre)
	{
		string text = nombre?.Trim().ToLowerInvariant();
		switch (text)
		{
		default:
			if (text != null)
			{
				if (text.StartsWith("numpad") && text.Length == 7)
				{
					return "Num " + text[6];
				}
				if (text.StartsWith("oem"))
				{
					return EtiquetaOem(nombre);
				}
			}
			return string.IsNullOrWhiteSpace(nombre) ? "F1" : nombre;
		case "mouse_left":
			return "Left Click";
		case "mouse_right":
			return "Right Click";
		case "mouse_middle":
			return "Middle Click";
		case "mouse_x1":
			return "Mouse 4";
		case "mouse_x2":
			return "Mouse 5";
		case "wheel_up":
			return "Wheel Up";
		case "wheel_down":
			return "Wheel Down";
		case "leftcontrol":
		case "leftctrl":
		case "lctrl":
			return "L Ctrl";
		case "rightcontrol":
		case "rctrl":
		case "rightctrl":
			return "R Ctrl";
		case "leftalt":
		case "lalt":
			return "L Alt";
		case "rightalt":
		case "ralt":
			return "R Alt";
		case "leftshift":
		case "lshift":
			return "L Shift";
		case "rightshift":
		case "rshift":
			return "R Shift";
		case "control":
		case "ctrl":
			return "Ctrl";
		case "alt":
			return "Alt";
		case "shift":
			return "Shift";
		case "space":
			return "Space";
		case "up":
			return "↑";
		case "down":
			return "↓";
		case "left":
			return "←";
		case "right":
			return "→";
		case "capslock":
			return "Caps Lock";
		case "numlock":
			return "Num Lock";
		case "scrolllock":
			return "Scroll Lock";
		case "backspace":
			return "Backspace";
		case "insert":
			return "Insert";
		case "delete":
		case "del":
			return "Delete";
		case "home":
			return "Home";
		case "end":
			return "End";
		case "pgup":
		case "pageup":
			return "Page Up";
		case "pagedown":
		case "pgdn":
			return "Page Down";
		case "leftmeta":
		case "lwin":
			return "L Win";
		case "rightmeta":
		case "rwin":
			return "R Win";
		case "apps":
			return "Menu";
		case "pause":
			return "Pause";
		case "multiply":
			return "Num *";
		case "add":
			return "Num +";
		case "subtract":
			return "Num −";
		case "decimal":
			return "Num .";
		case "divide":
			return "Num /";
		}
	}

	private static string EtiquetaOem(string nombre)
	{
		int? num = Resolver(nombre);
		if (num.HasValue)
		{
			uint num2 = MapVirtualKey((uint)num.Value, 2u);
			if (num2 != 0 && (num2 & 0x80000000u) == 0)
			{
				char c = (char)(num2 & 0xFFFF);
				if (!char.IsControl(c) && !char.IsWhiteSpace(c))
				{
					return c.ToString();
				}
			}
		}
		return nombre;
	}

	[DllImport("user32.dll")]
	private static extern uint MapVirtualKey(uint uCode, uint uMapType);

	public static bool MismaTecla(string? a, string? b)
	{
		if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
		{
			return false;
		}
		if (string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		int? num = Resolver(a);
		int? num2 = Resolver(b);
		if (!num.HasValue || !num2.HasValue)
		{
			return false;
		}
		if (!Coincide(num.Value, num2.Value))
		{
			return Coincide(num2.Value, num.Value);
		}
		return true;
	}

	public static int? Resolver(string? nombre)
	{
		if (string.IsNullOrWhiteSpace(nombre))
		{
			return null;
		}
		string text = nombre.Trim();
		if (Nombrados.TryGetValue(text, out var value))
		{
			return value;
		}
		if (text.Length == 1)
		{
			char c = char.ToUpperInvariant(text[0]);
			if (c >= 'A' && c <= 'Z')
			{
				return c;
			}
			if (c >= '0' && c <= '9')
			{
				return c;
			}
		}
		char c2 = text[0];
		bool flag = ((c2 == 'F' || c2 == 'f') ? true : false);
		if (flag && int.TryParse(text.AsSpan(1), out var result) && result >= 1 && result <= 24)
		{
			return 112 + (result - 1);
		}
		return null;
	}

	public static int NormalizarVirtualKey(int vk)
	{
		switch (vk)
		{
		case 160:
		case 161:
			return 16;
		case 162:
		case 163:
			return 17;
		case 164:
		case 165:
			return 18;
		default:
			return vk;
		}
	}

	public static bool Coincide(int vkEvento, int vkObjetivo)
	{
		if (vkEvento != vkObjetivo)
		{
			return NormalizarVirtualKey(vkEvento) == vkObjetivo;
		}
		return true;
	}

	public static IEnumerable<int> Variantes(int vk)
	{
		return vk switch
		{
			16 => new int[3] { 16, 160, 161 }, 
			17 => new int[3] { 17, 162, 163 }, 
			18 => new int[3] { 18, 164, 165 }, 
			_ => new int[1] { vk }, 
		};
	}
}
