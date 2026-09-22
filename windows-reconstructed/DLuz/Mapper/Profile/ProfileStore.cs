using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
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
		ancho = ((ancho > 0) ? ancho : 1920);
		alto = ((alto > 0) ? alto : 1080);
		NProfile nProfile = LeerPerfil(path);
		if (nProfile == null || nProfile.Buttons == null || nProfile.Buttons.Count == 0 || nProfile.Joystick == null || nProfile.Joystick.Radius < 0.02)
		{
			nProfile = ANormalizado(KeymapConfig.Cargar(AppPaths.KeymapPath), ancho, alto);
			Escribir(path, nProfile);
		}
		return APixeles(nProfile, ancho, alto);
	}

	public static bool Guardar(string path, KeymapConfig km, int ancho, int alto)
	{
		ancho = ((ancho > 0) ? ancho : 2400);
		alto = ((alto > 0) ? alto : 1080);
		return Escribir(path, ANormalizado(km, ancho, alto));
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
		JoystickConfig obj = new JoystickConfig
		{
			CenterX = R(p.Joystick.Cx * (double)w),
			CenterY = R(p.Joystick.Cy * (double)h),
			Radius = Math.Max(1, R(p.Joystick.Radius * (double)w))
		};
		Dictionary<string, int[]> keys = p.Joystick.Keys;
		obj.Keys = ((keys != null && keys.Count > 0) ? p.Joystick.Keys : new Dictionary<string, int[]>(KeysWasd));
		keymapConfig.Joystick = obj;
		double num = ((p.Camera.SensitivityX > 0.0) ? p.Camera.SensitivityX : ((p.Camera.Sensitivity > 0.0) ? p.Camera.Sensitivity : 1.0));
		double num2 = ((p.Camera.SensitivityY > 0.0) ? p.Camera.SensitivityY : (num * ((p.Camera.SensitivityRatioY > 0.0) ? p.Camera.SensitivityRatioY : 1.0)));
		keymapConfig.Camera = new CameraConfig
		{
			ZoneX = R(p.Camera.Cx * (double)w),
			ZoneY = R(p.Camera.Cy * (double)h),
			Sensitivity = num,
			SensitivityX = num,
			SensitivityY = num2,
			SensitivityRatioY = ((num > 0.0) ? (num2 / num) : 1.25),
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
				X = R(button.X * (double)w),
				Y = R(button.Y * (double)h),
				Label = button.Label,
				Mode = button.Mode,
				Size = button.Size,
				RepeatMs = button.RepeatMs
			});
		}
		return keymapConfig;
	}

	private static NProfile ANormalizado(KeymapConfig km, int w, int h)
	{
		NProfile nProfile = new NProfile
		{
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
					RepeatMs = button.RepeatMs
				});
			}
		}
		return nProfile;
	}

	private static int R(double v)
	{
		return (int)Math.Round(v);
	}
}

