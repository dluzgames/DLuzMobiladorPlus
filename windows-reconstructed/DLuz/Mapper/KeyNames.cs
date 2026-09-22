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

	public const int MouseWheelUp = 262;

	public const int MouseWheelDown = 263;

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
		["mouse_left"] = 257,
		["mouse_right"] = 258,
		["mouse_middle"] = 259,
		["mouse_x1"] = 260,
		["mouse_x2"] = 261,
		["mouse_wheel_up"] = 262,
		["mousewheelup"] = 262,
		["wheel_up"] = 262,
		["mouse_wheel_down"] = 263,
		["mousewheeldown"] = 263,
		["wheel_down"] = 263
	};

	public static string NombreBonito(string? nombre)
	{
		string text = nombre?.Trim().ToLowerInvariant();
		switch (text)
		{
		default:
			if (text != null && text.StartsWith("oem"))
			{
				return EtiquetaOem(nombre);
			}
			return string.IsNullOrWhiteSpace(nombre) ? "F1" : nombre;
		case "mouse_left":
			return "Clic izquierdo";
		case "mouse_right":
			return "Clic derecho";
		case "mouse_middle":
			return "Clic medio";
		case "mouse_x1":
			return "Botón lateral 1";
		case "mouse_x2":
			return "Botón lateral 2";
		case "mouse_wheel_up":
		case "mousewheelup":
		case "wheel_up":
			return "Roda para cima";
		case "mouse_wheel_down":
		case "mousewheeldown":
		case "wheel_down":
			return "Roda para baixo";
		case "leftcontrol":
		case "leftctrl":
		case "lctrl":
			return "Ctrl izq";
		case "rightcontrol":
		case "rctrl":
		case "rightctrl":
			return "Ctrl der";
		case "leftalt":
		case "lalt":
			return "Alt izq";
		case "rightalt":
		case "ralt":
			return "Alt der";
		case "leftshift":
		case "lshift":
			return "Shift izq";
		case "rightshift":
		case "rshift":
			return "Shift der";
		case "control":
		case "ctrl":
			return "Ctrl (ambos)";
		case "alt":
			return "Alt (ambos)";
		case "shift":
			return "Shift (ambos)";
		case "space":
			return "Espacio";
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
		if (flag && int.TryParse(text.AsSpan(1), out var result) && result >= 1 && result <= 12)
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

