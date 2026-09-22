using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace DLuz.Helpers;

public static class TeclaMod
{
	private static readonly Dictionary<Key, (string Token, string Nombre)> Especiales = new Dictionary<Key, (string, string)>
	{
		[Key.LeftCtrl] = ("lctrl", "Ctrl izquierdo"),
		[Key.RightCtrl] = ("rctrl", "Ctrl derecho"),
		[Key.LeftAlt] = ("lalt", "Alt izquierdo"),
		[Key.RightAlt] = ("ralt", "Alt derecho"),
		[Key.LWin] = ("lsuper", "Tecla Windows izquierda"),
		[Key.RWin] = ("rsuper", "Tecla Windows derecha"),
		[Key.LeftShift] = ("Left_Shift", "Shift izquierdo"),
		[Key.RightShift] = ("Right_Shift", "Shift derecho"),
		[Key.Pause] = ("Pause", "Pausa"),
		[Key.Scroll] = ("ScrollLock", "Bloq Despl"),
		[Key.Capital] = ("CapsLock", "Bloq Mayús"),
		[Key.NumLock] = ("Numlock", "Bloq Num"),
		[Key.Insert] = ("Insert", "Insert"),
		[Key.Home] = ("Home", "Inicio"),
		[Key.End] = ("End", "Fin"),
		[Key.Prior] = ("PageUp", "Re Pág"),
		[Key.Next] = ("PageDown", "Av Pág"),
		[Key.Delete] = ("Delete", "Supr"),
		[Key.Back] = ("Backspace", "Retroceso"),
		[Key.Apps] = ("Application", "Tecla Menú"),
		[Key.Space] = ("Space", "Espacio"),
		[Key.Tab] = ("Tab", "Tab"),
		[Key.Return] = ("Return", "Enter"),
		[Key.Divide] = ("Keypad_/", "Num /"),
		[Key.Multiply] = ("Keypad_*", "Num *"),
		[Key.Subtract] = ("Keypad_-", "Num -"),
		[Key.Decimal] = ("Keypad_.", "Num .")
	};

	private static readonly Dictionary<int, string> OemVkTokens = new Dictionary<int, string>
	{
		[186] = "Oem1",
		[187] = "OemPlus",
		[188] = "OemComma",
		[189] = "OemMinus",
		[190] = "OemPeriod",
		[191] = "Oem2",
		[192] = "Oem3",
		[219] = "Oem4",
		[220] = "Oem5",
		[221] = "Oem6",
		[222] = "Oem7",
		[223] = "Oem8",
		[226] = "Oem102"
	};

	public static (string Token, string Nombre)? DesdeKey(Key key)
	{
		if (Especiales.TryGetValue(key, out (string, string) value))
		{
			return value;
		}
		if (key >= Key.A && key <= Key.Z)
		{
			string text = key.ToString();
			return (text, "Tecla " + text);
		}
		if (key >= Key.D0 && key <= Key.D9)
		{
			string text2 = ((char)(48 + (key - 34))).ToString();
			return (text2, "Tecla " + text2);
		}
		if (key >= Key.NumPad0 && key <= Key.NumPad9)
		{
			int value2 = (int)(key - 74);
			return ($"Keypad_{value2}", $"Num {value2}");
		}
		if (key >= Key.F1 && key <= Key.F24)
		{
			return (key.ToString(), key.ToString());
		}
		return null;
	}

	public static (string Token, string Nombre)? DesdeKeyOem(Key key)
	{
		int num = KeyInterop.VirtualKeyFromKey(key);
		if ((num < 186 || num > 226) ? true : false)
		{
			return null;
		}
		uint num2 = MapVirtualKey((uint)num, 2u);
		if (num2 == 0 || (num2 & 0x80000000u) != 0)
		{
			return null;
		}
		char c = (char)(num2 & 0xFFFF);
		bool flag = char.IsWhiteSpace(c) || char.IsControl(c);
		if (!flag)
		{
			bool flag2 = ((c == '+' || c == ',' || c == '_') ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			return null;
		}
		string text = c.ToString();
		return (text, "Tecla " + text);
	}

	[DllImport("user32.dll")]
	private static extern uint MapVirtualKey(uint uCode, uint uMapType);

	public static string NombreBonito(string? token)
	{
		if (string.IsNullOrWhiteSpace(token))
		{
			return "Alt izquierdo";
		}
		foreach (var value in Especiales.Values)
		{
			if (string.Equals(value.Token, token, StringComparison.OrdinalIgnoreCase))
			{
				return value.Nombre;
			}
		}
		if (token.Length == 1)
		{
			return $"Tecla {char.ToUpperInvariant(token[0])}";
		}
		if (token.StartsWith("Keypad_", StringComparison.OrdinalIgnoreCase))
		{
			return "Num " + token.Substring("Keypad_".Length);
		}
		return token;
	}

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern short VkKeyScan(char ch);

	public static IEnumerable<string> EquivalentesMapeador(string? token)
	{
		string text = token?.Trim();
		switch (text)
		{
		case "lctrl":
			yield return "Control";
			yield return "LeftCtrl";
			yield break;
		case "rctrl":
			yield return "Control";
			yield return "RightCtrl";
			yield break;
		case "lalt":
			yield return "Alt";
			yield return "LeftAlt";
			yield break;
		case "ralt":
			yield return "Alt";
			yield return "RightAlt";
			yield break;
		case "lsuper":
			yield break;
		case "rsuper":
			yield break;
		case "Left_Shift":
			yield return "Shift";
			yield return "LeftShift";
			yield break;
		case "Right_Shift":
			yield return "Shift";
			yield return "RightShift";
			yield break;
		case "Space":
			yield return "Space";
			yield break;
		case "Tab":
			yield return "Tab";
			yield break;
		case "Return":
			yield return "Enter";
			yield break;
		}
		string text2 = text;
		if (text2.Length == 1 && char.IsAsciiLetterOrDigit(text2[0]))
		{
			yield return text2.ToUpperInvariant();
			yield break;
		}
		string text3 = text;
		int length = text3.Length;
		bool flag = (uint)(length - 2) <= 1u;
		if (flag && (text3[0] == 'F' || text3[0] == 'f') && int.TryParse(text3.Substring(1), out var result) && result >= 1 && result <= 24)
		{
			yield return "F" + result;
			yield break;
		}
		string text4 = text;
		if (text4.Length == 1)
		{
			short num = VkKeyScan(text4[0]);
			if (num != -1 && (num & 0xFF00) == 0 && OemVkTokens.TryGetValue(num & 0xFF, out string value))
			{
				yield return value;
			}
		}
	}

	public static bool BloqueadaEnMapeador(string? tokenMod, string nombreTecla)
	{
		if (string.IsNullOrWhiteSpace(tokenMod))
		{
			return false;
		}
		string[] array = tokenMod.Split(',');
		for (int i = 0; i < array.Length; i++)
		{
			foreach (string item in EquivalentesMapeador(array[i]))
			{
				if (string.Equals(item, nombreTecla, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
		}
		return false;
	}
}

