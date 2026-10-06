using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DLuz.Helpers;

namespace DLuz.Mapper.Profile;

public static class ProfileStore
{
	private const int BaseW = 2400;

	private const int BaseH = 1080;

	private static readonly Dictionary<string, int[]> KeysWasd = new Dictionary<string, int[]>
	{
		["W"] = new int[2] { 0, -1 },
		["S"] = new int[2] { 0, 1 },
		["A"] = new int[2] { -1, 0 },
		["D"] = new int[2] { 1, 0 }
	};

	private static readonly JsonSerializerOptions Json = new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		WriteIndented = true
	};

	public static KeymapConfig Cargar(string path, int ancho, int alto)
	{
		ancho = ((ancho > 0) ? ancho : 2400);
		alto = ((alto > 0) ? alto : 1080);
		NProfile nProfile = LeerPerfil(path);
		if (nProfile == null)
		{
			nProfile = ANormalizado(KeymapConfig.Cargar(AppPaths.KeymapPath), 2400, 1080);
			nProfile.Origen = "oficial";
			Escribir(path, nProfile);
		}
		return APixeles(nProfile, ancho, alto);
	}

	public static bool Guardar(string path, KeymapConfig km, int ancho, int alto, bool? modificado = null)
	{
		ancho = ((ancho > 0) ? ancho : 2400);
		alto = ((alto > 0) ? alto : 1080);
		NProfile nProfile = ANormalizado(km, ancho, alto);
		NProfile nProfile2 = LeerPerfil(path);
		nProfile.Origen = nProfile2?.Origen ?? "";
		nProfile.Modificado = modificado ?? nProfile2?.Modificado ?? false;
		return Escribir(path, nProfile);
	}

	public static (string origen, bool modificado) LeerMeta(string path)
	{
		NProfile nProfile = LeerPerfil(path);
		return (origen: nProfile?.Origen ?? "", modificado: nProfile?.Modificado ?? false);
	}

	public static bool MarcarOrigen(string path, string origen)
	{
		NProfile nProfile = LeerPerfil(path);
		if (nProfile == null)
		{
			return false;
		}
		nProfile.Origen = origen;
		nProfile.Modificado = false;
		return Escribir(path, nProfile);
	}

	public static bool EsPerfilValido(string path)
	{
		return LeerPerfil(path) != null;
	}

	private static NProfile? LeerPerfil(string path)
	{
		try
		{
			if (!File.Exists(path))
			{
				return null;
			}
			return JsonSerializer.Deserialize<NProfile>(File.ReadAllText(path), Json);
		}
		catch
		{
			return null;
		}
	}

	private static bool Escribir(string path, NProfile p)
	{
		try
		{
			File.WriteAllText(path, JsonSerializer.Serialize(p, Json), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static KeymapConfig APixeles(NProfile p, int w, int h)
	{
		KeymapConfig keymapConfig = new KeymapConfig
		{
			ToggleKey = p.ToggleKey,
			ExitKey = p.ExitKey,
			OverlayOpacity = ((p.OverlayOpacity > 0.0) ? p.OverlayOpacity : 0.85)
		};
		double num = (double)w / (double)h;
		double factor = ((p.AspectoOrigen > 0.0 && Math.Abs(num / p.AspectoOrigen - 1.0) > 0.02) ? (p.AspectoOrigen / num) : 1.0);
		JoystickConfig obj = new JoystickConfig
		{
			CenterX = R(AX(p.Joystick.Cx) * (double)w),
			CenterY = R(p.Joystick.Cy * (double)h),
			Radius = Math.Max(1, R(p.Joystick.Radius * factor * (double)w))
		};
		Dictionary<string, int[]> keys = p.Joystick.Keys;
		obj.Keys = ((keys != null && keys.Count > 0) ? p.Joystick.Keys : new Dictionary<string, int[]>(KeysWasd));
		keymapConfig.Joystick = obj;
		double num2 = ((p.Camera.SensitivityX > 0.0) ? p.Camera.SensitivityX : ((p.Camera.Sensitivity > 0.0) ? p.Camera.Sensitivity : 1.0));
		double num3 = ((p.Camera.SensitivityY > 0.0) ? p.Camera.SensitivityY : (num2 * ((p.Camera.SensitivityRatioY > 0.0) ? p.Camera.SensitivityRatioY : 1.0)));
		keymapConfig.Camera = new CameraConfig
		{
			ZoneX = R(AX(p.Camera.Cx) * (double)w),
			ZoneY = R(p.Camera.Cy * (double)h),
			Sensitivity = num2,
			SensitivityX = num2,
			SensitivityY = num3,
			SensitivityRatioY = ((num2 > 0.0) ? (num3 / num2) : 1.25),
			FreeMouseKey = p.Camera.FreeMouseKey,
			Smoothing = Math.Clamp(p.Camera.Smoothing, 0.0, 0.3),
			InvertX = p.Camera.InvertX,
			InvertY = p.Camera.InvertY,
			ZoneSize = ((p.Camera.ZoneSize > 0.0) ? p.Camera.ZoneSize : 0.18)
		};
		foreach (NButton button in p.Buttons)
		{
			keymapConfig.Buttons.Add(new ButtonConfig
			{
				Key = button.Key,
				X = R(AX(button.X) * (double)w),
				Y = R(button.Y * (double)h),
				Label = button.Label,
				Mode = button.Mode,
				Size = button.Size,
				RepeatMs = button.RepeatMs,
				X2 = R(AX(button.X2) * (double)w),
				Y2 = R(button.Y2 * (double)h),
				DurationMs = button.DurationMs,
				AngleDeg = button.AngleDeg,
				Script = ((factor == 1.0) ? button.Script : AdaptarScript(button.Script, AX))
			});
		}
		return keymapConfig;
		double AX(double x)
		{
			if (factor != 1.0)
			{
				return AdaptarX(x, factor);
			}
			return x;
		}
	}

	private static NProfile ANormalizado(KeymapConfig km, int w, int h)
	{
		NProfile nProfile = new NProfile
		{
			AspectoOrigen = (double)w / (double)h,
			ToggleKey = km.ToggleKey,
			ExitKey = km.ExitKey,
			OverlayOpacity = ((km.OverlayOpacity > 0.0) ? km.OverlayOpacity : 0.85)
		};
		NJoystick obj = new NJoystick
		{
			Cx = (double)km.Joystick.CenterX / (double)w,
			Cy = (double)km.Joystick.CenterY / (double)h,
			Radius = (double)km.Joystick.Radius / (double)w
		};
		Dictionary<string, int[]> keys = km.Joystick.Keys;
		obj.Keys = ((keys != null && keys.Count > 0) ? km.Joystick.Keys : new Dictionary<string, int[]>(KeysWasd));
		nProfile.Joystick = obj;
		nProfile.Camera = new NCamera
		{
			Cx = (double)km.Camera.ZoneX / (double)w,
			Cy = (double)km.Camera.ZoneY / (double)h,
			Sensitivity = ((km.Camera.SensitivityX > 0.0) ? km.Camera.SensitivityX : km.Camera.Sensitivity),
			SensitivityX = ((km.Camera.SensitivityX > 0.0) ? km.Camera.SensitivityX : km.Camera.Sensitivity),
			SensitivityY = ((km.Camera.SensitivityY > 0.0) ? km.Camera.SensitivityY : (((km.Camera.SensitivityX > 0.0) ? km.Camera.SensitivityX : km.Camera.Sensitivity) * ((km.Camera.SensitivityRatioY > 0.0) ? km.Camera.SensitivityRatioY : 1.0))),
			SensitivityRatioY = ((((km.Camera.SensitivityX > 0.0) ? km.Camera.SensitivityX : km.Camera.Sensitivity) > 0.0) ? (((km.Camera.SensitivityY > 0.0) ? km.Camera.SensitivityY : (((km.Camera.SensitivityX > 0.0) ? km.Camera.SensitivityX : km.Camera.Sensitivity) * ((km.Camera.SensitivityRatioY > 0.0) ? km.Camera.SensitivityRatioY : 1.0))) / ((km.Camera.SensitivityX > 0.0) ? km.Camera.SensitivityX : km.Camera.Sensitivity)) : 1.0),
			FreeMouseKey = km.Camera.FreeMouseKey,
			Smoothing = Math.Clamp(km.Camera.Smoothing, 0.0, 0.3),
			InvertX = km.Camera.InvertX,
			InvertY = km.Camera.InvertY,
			ZoneSize = km.Camera.ZoneSize
		};
		foreach (ButtonConfig button in km.Buttons)
		{
			if (!string.IsNullOrWhiteSpace(button.Key))
			{
				nProfile.Buttons.Add(new NButton
				{
					Key = button.Key,
					X = (double)button.X / (double)w,
					Y = (double)button.Y / (double)h,
					Label = button.Label,
					Mode = button.Mode,
					Size = button.Size,
					RepeatMs = button.RepeatMs,
					X2 = (double)button.X2 / (double)w,
					Y2 = (double)button.Y2 / (double)h,
					DurationMs = button.DurationMs,
					AngleDeg = button.AngleDeg,
					Script = button.Script
				});
			}
		}
		return nProfile;
	}

	private static int R(double v)
	{
		return (int)Math.Round(v);
	}

	private static string? AdaptarScript(string? script, Func<double, double> ax)
	{
		if (string.IsNullOrEmpty(script))
		{
			return script;
		}
		string[] array = script.Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			Match match = Regex.Match(array[i], "^(\\s*(?:tap|down|move)\\s+)(-?\\d+(?:\\.\\d+)?)(\\s.*)$", RegexOptions.IgnoreCase);
			if (match.Success)
			{
				double arg = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) / 100.0;
				array[i] = match.Groups[1].Value + (ax(arg) * 100.0).ToString("0.####", CultureInfo.InvariantCulture) + match.Groups[3].Value;
			}
		}
		return string.Join("\n", array);
	}

	private static double AdaptarX(double x, double factor)
	{
		if (x < 1.0 / 3.0)
		{
			return x * factor;
		}
		if (x > 2.0 / 3.0)
		{
			return 1.0 - (1.0 - x) * factor;
		}
		return 0.5 + (x - 0.5) * factor;
	}

	public static bool GuardarNormalizado(string path, NProfile p)
	{
		return Escribir(path, p);
	}
}
