using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using DLuz.Helpers;
using Wpf.Ui.Appearance;

namespace DLuz.Services;

public sealed class ThemeService
{
	private static readonly Lazy<ThemeService> _lazy = new Lazy<ThemeService>(() => new ThemeService());
	public static ThemeService Instance => _lazy.Value;

	public event Action<bool>? ThemeChanged;

	public bool IsDarkTheme { get; private set; } = true;

	private ThemeService()
	{
	}

	public void Initialize()
	{
		bool isDark = LoadThemePreference();
		ApplyTheme(isDark, persist: false);
	}

	public void ToggleTheme()
	{
		ApplyTheme(!IsDarkTheme, persist: true);
	}

	public void ApplyTheme(bool isDark, bool persist = true)
	{
		IsDarkTheme = isDark;

		try
		{
			// Atualiza motor nativo do Wpf.Ui
			ApplicationTheme wpfTheme = isDark ? ApplicationTheme.Dark : ApplicationTheme.Light;
			ApplicationThemeManager.Apply(wpfTheme);
			ApplicationAccentColorManager.Apply(Color.FromRgb(225, 29, 72), wpfTheme);
		}
		catch (Exception ex)
		{
			AppLogger.Error("ThemeService: erro ao aplicar tema Wpf.Ui", ex);
		}

		// Atualiza tokens dinâmicos Stex no Application.Current.Resources
		ApplyStexTokens(isDark);

		if (persist)
		{
			SaveThemePreference(isDark);
		}

		try
		{
			ThemeChanged?.Invoke(isDark);
		}
		catch (Exception ex)
		{
			AppLogger.Error("ThemeService: erro no callback ThemeChanged", ex);
		}
	}

	private void ApplyStexTokens(bool isDark)
	{
		var res = Application.Current?.Resources;
		if (res == null) return;

		if (isDark)
		{
			// ==========================================
			// MODERN DARK STUDIO (Linear / Discord Dark)
			// ==========================================
			SetColor(res, "Stex.BgPrimaryColor", "#FF0B0F19");
			SetColor(res, "Stex.BgSecondaryColor", "#FF131B2E");
			SetColor(res, "Stex.BgCardColor", "#FF131B2E");
			SetColor(res, "Stex.BgDarkColor", "#FF070A10");
			SetColor(res, "Stex.BgDarkMidColor", "#FF0D1322");
			SetColor(res, "Stex.BgDarkMid2Color", "#FF1A233A");
			SetColor(res, "Stex.BgTabActiveColor", "#FF2D1520");

			SetColor(res, "Stex.AccentColor", "#FFE11D48");
			SetColor(res, "Stex.AccentDarkColor", "#FFBE123C");
			SetColor(res, "Stex.AccentDeepColor", "#FF9F1239");
			SetColor(res, "Stex.AccentLightColor", "#FFFB7185");
			SetColor(res, "Stex.AccentLighterColor", "#FFF43F5E");
			SetColor(res, "Stex.AccentPaleColor", "#FF4C1D24");
			SetColor(res, "Stex.AccentTextColor", "#FFFB7185");
			SetColor(res, "Stex.AccentSubtleColor", "#FFE11D48");
			SetColor(res, "Stex.AccentMutedColor", "#FF9F1239");

			SetColor(res, "Stex.TextPrimaryColor", "#FFF8FAFC");
			SetColor(res, "Stex.TextSecondaryColor", "#FF94A3B8");
			SetColor(res, "Stex.TextMutedColor", "#FF64748B");
			SetColor(res, "Stex.TextDisabledColor", "#FF475569");
			SetColor(res, "Stex.TextDimmerColor", "#FF64748B");
			SetColor(res, "Stex.TextTertiaryColor", "#FF64748B");
			SetColor(res, "Stex.TextModerateColor", "#FF94A3B8");
			SetColor(res, "Stex.TextLightColor", "#FFE2E8F0");
			SetColor(res, "Stex.TextLighterColor", "#FFF8FAFC");

			SetColor(res, "Stex.BorderNeutralColor", "#FF1E293B");
			SetColor(res, "Stex.BorderSecondaryColor", "#FF334155");
			SetColor(res, "Stex.BorderSecondary2Color", "#FF4C1D24");

			SetColor(res, "Stex.SuccessColor", "#FF10B981");
			SetColor(res, "Stex.SuccessBrightColor", "#FF34D399");
			SetColor(res, "Stex.SuccessOtgColor", "#FF10B981");

			SetColor(res, "Stex.ErrorColor", "#FFEF4444");
			SetColor(res, "Stex.WarningColor", "#FFF59E0B");
			SetColor(res, "Stex.WarningTextColor", "#FFD97706");
			SetColor(res, "Stex.WarningOrangeColor", "#FFF97316");
			SetColor(res, "Stex.InfoColor", "#FF38BDF8");

			SetColor(res, "Stex.BtnSecondaryColor", "#FF1E293B");
			SetColor(res, "Stex.BtnInactiveColor", "#FF1A233A");
			SetColor(res, "Stex.BtnNavActiveColor", "#FF2D1520");
			SetColor(res, "Stex.BtnDisabledColor", "#FF131B2E");
			SetColor(res, "Stex.BtnDangerColor", "#FFE11D48");
			SetColor(res, "Stex.BtnDangerDarkColor", "#FFBE123C");
			SetColor(res, "Stex.BtnWarningColor", "#FFF59E0B");

			SetColor(res, "Stex.AccentHoverBgColor", "#25E11D48");
			SetColor(res, "Stex.AccentBorderPenColor", "#60E11D48");

			SetColor(res, "Stex.AndroidGreenColor", "#FF10B981");
			SetColor(res, "Stex.AndroidGreenSubtleColor", "#FF064E3B");
			SetColor(res, "Stex.AndroidGreenBorderColor", "#FF047857");

			SetColor(res, "Stex.WarningSubtleColor", "#FF291804");
			SetColor(res, "Stex.WarningBorderColor", "#FF78350F");
			SetColor(res, "Stex.SuccessSubtleColor", "#FF064E3B");
			SetColor(res, "Stex.SuccessBorderColor", "#FF047857");
			SetColor(res, "Stex.InfoSubtleColor", "#FF0C2138");
			SetColor(res, "Stex.InfoBorderColor", "#FF1E40AF");

			SetGradient(res, "Stex.CtaBgBrush", "#FF131B2E", "#FF1A233A");
			SetBrush(res, "Stex.CtaBorderBrush", "#FF334155");
		}
		else
		{
			// ==========================================
			// SOFT LIGHT STUDIO (Descansado e Confortável)
			// ==========================================
			SetColor(res, "Stex.BgPrimaryColor", "#FFF1F5F9");
			SetColor(res, "Stex.BgSecondaryColor", "#FFFFFFFF");
			SetColor(res, "Stex.BgCardColor", "#FFFFFFFF");
			SetColor(res, "Stex.BgDarkColor", "#FFE2E8F0");
			SetColor(res, "Stex.BgDarkMidColor", "#FFFFFFFF");
			SetColor(res, "Stex.BgDarkMid2Color", "#FFF8FAFC");
			SetColor(res, "Stex.BgTabActiveColor", "#FFFFF1F2");

			SetColor(res, "Stex.AccentColor", "#FFE11D48");
			SetColor(res, "Stex.AccentDarkColor", "#FFBE123C");
			SetColor(res, "Stex.AccentDeepColor", "#FF9F1239");
			SetColor(res, "Stex.AccentLightColor", "#FFDC2626");
			SetColor(res, "Stex.AccentLighterColor", "#FFEF4444");
			SetColor(res, "Stex.AccentPaleColor", "#FFFEE2E2");
			SetColor(res, "Stex.AccentTextColor", "#FFE11D48");
			SetColor(res, "Stex.AccentSubtleColor", "#FFF43F5E");
			SetColor(res, "Stex.AccentMutedColor", "#FFFB7185");

			SetColor(res, "Stex.TextPrimaryColor", "#FF0F172A");
			SetColor(res, "Stex.TextSecondaryColor", "#FF475569");
			SetColor(res, "Stex.TextMutedColor", "#FF94A3B8");
			SetColor(res, "Stex.TextDisabledColor", "#FFCBD5E1");
			SetColor(res, "Stex.TextDimmerColor", "#FF64748B");
			SetColor(res, "Stex.TextTertiaryColor", "#FF94A3B8");
			SetColor(res, "Stex.TextModerateColor", "#FF475569");
			SetColor(res, "Stex.TextLightColor", "#FF334155");
			SetColor(res, "Stex.TextLighterColor", "#FF1E293B");

			SetColor(res, "Stex.BorderNeutralColor", "#FFE2E8F0");
			SetColor(res, "Stex.BorderSecondaryColor", "#FFCBD5E1");
			SetColor(res, "Stex.BorderSecondary2Color", "#FFFDA4AF");

			SetColor(res, "Stex.SuccessColor", "#FF059669");
			SetColor(res, "Stex.SuccessBrightColor", "#FF10B981");
			SetColor(res, "Stex.SuccessOtgColor", "#FF059669");

			SetColor(res, "Stex.ErrorColor", "#FFDC2626");
			SetColor(res, "Stex.WarningColor", "#FFD97706");
			SetColor(res, "Stex.WarningTextColor", "#FFB45309");
			SetColor(res, "Stex.WarningOrangeColor", "#FFEA580C");
			SetColor(res, "Stex.InfoColor", "#FF2563EB");

			SetColor(res, "Stex.BtnSecondaryColor", "#FFE2E8F0");
			SetColor(res, "Stex.BtnInactiveColor", "#FFE2E8F0");
			SetColor(res, "Stex.BtnNavActiveColor", "#FFFFF1F2");
			SetColor(res, "Stex.BtnDisabledColor", "#FFF1F5F9");
			SetColor(res, "Stex.BtnDangerColor", "#FFDC2626");
			SetColor(res, "Stex.BtnDangerDarkColor", "#FFB91C1C");
			SetColor(res, "Stex.BtnWarningColor", "#FFD97706");

			SetColor(res, "Stex.AccentHoverBgColor", "#18E11D48");
			SetColor(res, "Stex.AccentBorderPenColor", "#80E11D48");

			SetColor(res, "Stex.AndroidGreenColor", "#FF059669");
			SetColor(res, "Stex.AndroidGreenSubtleColor", "#FFECFDF5");
			SetColor(res, "Stex.AndroidGreenBorderColor", "#FFA7F3D0");

			SetColor(res, "Stex.WarningSubtleColor", "#FFFFFBEB");
			SetColor(res, "Stex.WarningBorderColor", "#FFFDE68A");
			SetColor(res, "Stex.SuccessSubtleColor", "#FFECFDF5");
			SetColor(res, "Stex.SuccessBorderColor", "#FFA7F3D0");
			SetColor(res, "Stex.InfoSubtleColor", "#FFEFF6FF");
			SetColor(res, "Stex.InfoBorderColor", "#FF93C5FD");

			SetGradient(res, "Stex.CtaBgBrush", "#FFF8FAFC", "#FFFFFBEB");
			SetBrush(res, "Stex.CtaBorderBrush", "#FFE2E8F0");
		}
	}

	private static void SetColor(ResourceDictionary res, string colorKey, string hex)
	{
		try
		{
			Color c = (Color)ColorConverter.ConvertFromString(hex);
			res[colorKey] = c;

			string brushKey = colorKey.Replace("Color", "Brush");
			res[brushKey] = new SolidColorBrush(c);
		}
		catch
		{
		}
	}

	private static void SetBrush(ResourceDictionary res, string brushKey, string hex)
	{
		try
		{
			Color c = (Color)ColorConverter.ConvertFromString(hex);
			res[brushKey] = new SolidColorBrush(c);
		}
		catch
		{
		}
	}

	private static void SetGradient(ResourceDictionary res, string brushKey, string hex1, string hex2)
	{
		try
		{
			Color c1 = (Color)ColorConverter.ConvertFromString(hex1);
			Color c2 = (Color)ColorConverter.ConvertFromString(hex2);
			var grad = new LinearGradientBrush
			{
				StartPoint = new Point(0, 0),
				EndPoint = new Point(1, 1)
			};
			grad.GradientStops.Add(new GradientStop(c1, 0.0));
			grad.GradientStops.Add(new GradientStop(c2, 1.0));
			res[brushKey] = grad;
		}
		catch
		{
		}
	}

	private static bool LoadThemePreference()
	{
		try
		{
			string cfg = AppPaths.ConfigPath;
			if (File.Exists(cfg))
			{
				string text = File.ReadAllText(cfg);
				var m = Regex.Match(text, @"modo_tema\s*=\s*(\w+)", RegexOptions.IgnoreCase);
				if (m.Success)
				{
					string val = m.Groups[1].Value.Trim().ToLowerInvariant();
					if (val == "claro" || val == "light")
					{
						return false;
					}
					if (val == "escuro" || val == "dark")
					{
						return true;
					}
				}
			}
		}
		catch (Exception ex)
		{
			AppLogger.Error("ThemeService: erro ao ler preferência de tema", ex);
		}
		// Padrão: Modern Dark Studio (conforme pedido: 'deixe um pouco mais escuro')
		return true;
	}

	private static void SaveThemePreference(bool isDark)
	{
		try
		{
			string cfg = AppPaths.ConfigPath;
			if (!File.Exists(cfg))
			{
				Directory.CreateDirectory(Path.GetDirectoryName(cfg)!);
				File.WriteAllText(cfg, "[Tema]\nmodo_tema=" + (isDark ? "escuro" : "claro") + "\n");
				return;
			}

			string content = File.ReadAllText(cfg);
			if (Regex.IsMatch(content, @"modo_tema\s*=.*", RegexOptions.IgnoreCase))
			{
				content = Regex.Replace(content, @"modo_tema\s*=.*", "modo_tema=" + (isDark ? "escuro" : "claro"), RegexOptions.IgnoreCase);
			}
			else
			{
				if (content.Contains("[Tema]"))
				{
					content = content.Replace("[Tema]", "[Tema]\nmodo_tema=" + (isDark ? "escuro" : "claro"));
				}
				else
				{
					content += "\n[Tema]\nmodo_tema=" + (isDark ? "escuro" : "claro") + "\n";
				}
			}
			File.WriteAllText(cfg, content);
		}
		catch (Exception ex)
		{
			AppLogger.Error("ThemeService: erro ao salvar preferência de tema", ex);
		}
	}
}
