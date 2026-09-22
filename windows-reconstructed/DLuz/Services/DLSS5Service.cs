using System;
using System.IO;
using System.Text;
using DLuz.Helpers;

namespace DLuz.Services;

public static class DLSS5Service
{
	private static bool _activo = false;
	private static string _preset = "Qualidade Ultra (2K/4K Sharp)";

	public static bool Activo => _activo;
	public static string Preset => _preset;

	public static event Action<bool, string>? EstadoCambiado;

	public static string RuntimeDlss5Dir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RuntimeAssets", "dlss5");
	public static string TargetScrcpy64Dir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "scrcpy", "x86_64");

	public static void Inicializar(bool habilitarPorDefecto = true, string presetInicial = "Qualidade Ultra (2K/4K Sharp)")
	{
		_preset = string.IsNullOrWhiteSpace(presetInicial) ? "Qualidade Ultra (2K/4K Sharp)" : presetInicial;
		_activo = habilitarPorDefecto;
		try
		{
			if (_activo)
			{
				InstalarArchivosEnScrcpy();
			}
			else
			{
				DesactivarEnScrcpy();
			}
		}
		catch (Exception ex)
		{
			AppLogger.Warn("DLSS5Service.Inicializar: " + ex.Message);
		}
	}

	public static bool Alternar()
	{
		_activo = !_activo;
		if (_activo)
		{
			InstalarArchivosEnScrcpy();
		}
		else
		{
			DesactivarEnScrcpy();
		}
		EstadoCambiado?.Invoke(_activo, _preset);
		return _activo;
	}

	public static void EstablecerPreset(string novoPreset)
	{
		if (!string.IsNullOrWhiteSpace(novoPreset))
		{
			_preset = novoPreset;
		}
		if (_activo)
		{
			ActualizarConfigFile();
		}
		EstadoCambiado?.Invoke(_activo, _preset);
	}

	public static void InstalarArchivosEnScrcpy()
	{
		try
		{
			string sourceDir = Directory.Exists(RuntimeDlss5Dir)
				? RuntimeDlss5Dir
				: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dlss5");

			if (!Directory.Exists(sourceDir))
			{
				string desktopFallback = @"C:\Users\dluzgg\Desktop\dll5\bluestacks-msi5-dlss5-feeder";
				if (Directory.Exists(desktopFallback))
				{
					sourceDir = desktopFallback;
				}
			}

			if (!Directory.Exists(sourceDir)) return;

			string destDir = TargetScrcpy64Dir;
			if (!Directory.Exists(destDir))
			{
				Directory.CreateDirectory(destDir);
			}

			// Restaura dxgi.dll se estava desativado
			string dxgiDisabled = Path.Combine(destDir, "dxgi.dll.disabled");
			string dxgiActive = Path.Combine(destDir, "dxgi.dll");
			if (File.Exists(dxgiDisabled) && !File.Exists(dxgiActive))
			{
				File.Move(dxgiDisabled, dxgiActive);
			}

			CopiarSiExiste(Path.Combine(sourceDir, "dxgi.dll"), dxgiActive);
			CopiarSiExiste(Path.Combine(sourceDir, "nvngx_dlss.dll"), Path.Combine(destDir, "nvngx_dlss.dll"));
			CopiarSiExiste(Path.Combine(sourceDir, "nvngx_dlssnr.dll"), Path.Combine(destDir, "nvngx_dlssnr.dll"));
			CopiarSiExiste(Path.Combine(sourceDir, "dlss5-feed.addon64"), Path.Combine(destDir, "dlss5-feed.addon64"));
			CopiarSiExiste(Path.Combine(sourceDir, "renodx-dlss5.addon64"), Path.Combine(destDir, "renodx-dlss5.addon64"));

			string shadersSource = Path.Combine(sourceDir, "reshade-shaders");
			string shadersDest = Path.Combine(destDir, "reshade-shaders");
			if (Directory.Exists(shadersSource))
			{
				CopiarDirectorioRecursivo(shadersSource, shadersDest);
			}

			ActualizarConfigFile();
		}
		catch (Exception ex)
		{
			AppLogger.Error("DLSS5Service.InstalarArchivosEnScrcpy: " + ex.Message, ex);
		}
	}

	public static void DesactivarEnScrcpy()
	{
		try
		{
			string destDir = TargetScrcpy64Dir;
			string[] files = new[]
			{
				"dxgi.dll", "dxgi.dll.disabled", "dlss5-feed.addon64", "renodx-dlss5.addon64",
				"nvngx_dlss.dll", "nvngx_dlssnr.dll", "ReShade.ini", "ReShadePreset.ini",
				"dlss5-feed.cfg", "dlss5-feed.log", "ReShade.log", "ReShade.log1"
			};
			foreach (var f in files)
			{
				string p = Path.Combine(destDir, f);
				if (File.Exists(p))
				{
					try { File.Delete(p); } catch { }
				}
			}
			string shaders = Path.Combine(destDir, "reshade-shaders");
			if (Directory.Exists(shaders))
			{
				try { Directory.Delete(shaders, true); } catch { }
			}
		}
		catch (Exception ex)
		{
			AppLogger.Warn("DLSS5Service.DesactivarEnScrcpy: " + ex.Message);
		}
	}

	public static void ActualizarConfigFile()
	{
		try
		{
			string destDir = TargetScrcpy64Dir;
			if (!Directory.Exists(destDir))
			{
				Directory.CreateDirectory(destDir);
			}

			int workRes = 88;
			int presetNum = 12;
			double mvX = 1.333;
			double mvY = 1.618;
			double nrIntensity = 2.0;
			double nrStructure = 2.2;
			double nrTone = 1.9;
			double nrPaperWhite = 7.924;
			int nrPreset = 2;
			int nrStyle = 2;
			double lumeniteIntensity = 1.45;
			double sharpenAmt = 1.65;
			double solarisContrast = 1.20;

			if (_preset.Contains("Performance", StringComparison.OrdinalIgnoreCase))
			{
				workRes = 50;
				presetNum = 8;
				mvX = 2.0;
				mvY = 2.4;
				nrIntensity = 1.5;
				nrStructure = 1.5;
				nrTone = 1.6;
				nrPreset = 1;
				nrStyle = 1;
				lumeniteIntensity = 1.20;
				sharpenAmt = 1.35;
			}
			else if (_preset.Contains("HDR", StringComparison.OrdinalIgnoreCase) || _preset.Contains("RenoDX", StringComparison.OrdinalIgnoreCase))
			{
				workRes = 85;
				presetNum = 12;
				mvX = 1.400;
				mvY = 1.700;
				nrIntensity = 2.2;
				nrStructure = 2.2;
				nrTone = 2.1;
				nrPaperWhite = 8.5;
				nrPreset = 2;
				nrStyle = 2;
				lumeniteIntensity = 1.60;
				sharpenAmt = 1.55;
				solarisContrast = 1.30;
			}
			else if (_preset.Contains("Equilibrado", StringComparison.OrdinalIgnoreCase))
			{
				workRes = 77;
				presetNum = 10;
				mvX = 1.667;
				mvY = 2.021;
				nrIntensity = 1.8;
				nrStructure = 1.8;
				nrTone = 1.8;
				nrPreset = 2;
				nrStyle = 2;
				lumeniteIntensity = 1.35;
				sharpenAmt = 1.40;
			}
			else // Qualidade Ultra (2K/4K Sharp) - Padrão DLSS 5
			{
				workRes = 88;
				presetNum = 12;
				mvX = 1.333;
				mvY = 1.618;
				nrIntensity = 2.0;
				nrStructure = 2.2;
				nrTone = 1.9;
				nrPreset = 2;
				nrStyle = 2;
				lumeniteIntensity = 1.45;
				sharpenAmt = 1.65;
			}

			// 1. dlss5-feed.cfg
			string cfgPath = Path.Combine(destDir, "dlss5-feed.cfg");
			StringBuilder sbCfg = new StringBuilder();
			sbCfg.AppendLine("enabled=1");
			sbCfg.AppendLine("mode=2");
			sbCfg.AppendLine("hdr=-1");
			sbCfg.AppendLine("depth_inverted=-1");
			sbCfg.AppendLine("flags=-1");
			sbCfg.AppendLine("reset_every=0");
			sbCfg.AppendLine("warmup_rebuild=60");
			sbCfg.AppendLine("rebuild=0");
			sbCfg.AppendLine("log_frames=3");
			sbCfg.AppendLine("create_delay=30");
			sbCfg.AppendLine($"preset={presetNum}");
			sbCfg.AppendLine($"work_resolution={workRes}");
			sbCfg.AppendLine($"mv_scale_x={mvX.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)}");
			sbCfg.AppendLine($"mv_scale_y={mvY.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)}");
			File.WriteAllText(cfgPath, sbCfg.ToString(), Encoding.UTF8);

			// 2. ReShade.ini (Rotas de shaders corrigidas para evitar Erro 123 + Tecla F6/End + RenoDX DLSS5 configurado)
			string reshadeIniPath = Path.Combine(destDir, "ReShade.ini");
			StringBuilder sbIni = new StringBuilder();
			sbIni.AppendLine("[GENERAL]");
			sbIni.AppendLine(@"EffectSearchPaths=.\reshade-shaders\Shaders,.\reshade-shaders\Shaders\include,.\reshade-shaders\Shaders\Lilium,.\reshade-shaders\Shaders\Lilium\lilium__include,.\reshade-shaders\Shaders\Lilium\lilium__include\colour_space,.\reshade-shaders\Shaders\MartysMods");
			sbIni.AppendLine(@"TextureSearchPaths=.\reshade-shaders\Textures,.\reshade-shaders\Textures\Lilium");
			sbIni.AppendLine(@"IntermediateCachePath=" + Path.Combine(Path.GetTempPath(), "ReShade"));
			sbIni.AppendLine("NoDebugInfo=1");
			sbIni.AppendLine("NoEffectCache=0");
			sbIni.AppendLine("NoReloadOnInit=0");
			sbIni.AppendLine("PerformanceMode=0");
			sbIni.AppendLine("PreprocessorDefinitions=RESHADE_DEPTH_LINEARIZATION_FAR_PLANE=1000.0,RESHADE_DEPTH_INPUT_IS_UPSIDE_DOWN=0,RESHADE_DEPTH_INPUT_IS_REVERSED=0,RESHADE_DEPTH_INPUT_IS_LOGARITHMIC=0,DLSS5_MV_PROVIDER=3");
			sbIni.AppendLine(@"PresetPath=.\ReShadePreset.ini");
			sbIni.AppendLine("PresetTransitionDuration=0");
			sbIni.AppendLine("SkipLoadingDisabledEffects=0");
			sbIni.AppendLine();
			sbIni.AppendLine("[INPUT]");
			sbIni.AppendLine("ForceShortcutModifiers=0");
			sbIni.AppendLine("GamepadNavigation=0");
			sbIni.AppendLine("InputProcessing=2");
			sbIni.AppendLine("KeyEffects=117,0,0,0"); // F6 = Alternar Efeitos na tela (ReShade + RenoDX DLSS5)
			sbIni.AppendLine("KeyOverlay=36,0,0,0");  // Home = Menu ReShade / DLSS 5
			sbIni.AppendLine("KeyScreenshot=44,0,0,0");
			sbIni.AppendLine();
			sbIni.AppendLine("[OVERLAY]");
			sbIni.AppendLine("AutoSavePreset=1");
			sbIni.AppendLine("ShowFPS=0");
			sbIni.AppendLine("TutorialProgress=4");
			sbIni.AppendLine();
			sbIni.AppendLine("[PROXY]");
			sbIni.AppendLine("EnableProxyLibrary=0");
			sbIni.AppendLine("ProxyLibrary=");
			sbIni.AppendLine();
			sbIni.AppendLine("[RenoDX.DLSS5]");
			sbIni.AppendLine("NeuralUplift=1");
			sbIni.AppendLine("NRAutoMask=1");
			sbIni.AppendLine($"NRIntensity={nrIntensity.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture)}");
			sbIni.AppendLine($"NRLocalStructure={nrStructure.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture)}");
			sbIni.AppendLine($"NRLocalTone={nrTone.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture)}");
			sbIni.AppendLine("NRMVecScaleX=-1.780000");
			sbIni.AppendLine("NRMVecScaleY=-2.000000");
			sbIni.AppendLine($"NRPaperWhiteScale={nrPaperWhite.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture)}");
			sbIni.AppendLine($"NRPreset={nrPreset}");
			sbIni.AppendLine($"NRStyle={nrStyle}");
			File.WriteAllText(reshadeIniPath, sbIni.ToString(), Encoding.UTF8);

			// 3. ReShadePreset.ini (Ordem estrita: Launchpad -> DLSS5_Feed -> Lumenite -> Sharpen -> Solaris)
			string presetIniPath = Path.Combine(destDir, "ReShadePreset.ini");
			StringBuilder sbPreset = new StringBuilder();
			sbPreset.AppendLine("Techniques=MartysMods_Launchpad@MartysMods_LAUNCHPAD.fx,DLSS5_Feed@DLSS5_Feed.fx,Lumenite_Kernel@lumenite_Kernel.fx,MartyMods_Sharpen@MartysMods_SHARPEN.fx,MartysMods_SOLARIS@MartysMods_SOLARIS.fx");
			sbPreset.AppendLine("TechniqueSorting=MartysMods_Launchpad@MartysMods_LAUNCHPAD.fx,DLSS5_Feed@DLSS5_Feed.fx,Lumenite_Kernel@lumenite_Kernel.fx,MartyMods_Sharpen@MartysMods_SHARPEN.fx,MartysMods_SOLARIS@MartysMods_SOLARIS.fx");
			sbPreset.AppendLine();
			sbPreset.AppendLine("[DLSS5_Feed.fx]");
			sbPreset.AppendLine("DEBUG_VIEW=0");
			sbPreset.AppendLine("DEPTH_TOLERANCE=0.100000");
			sbPreset.AppendLine("GEOM_AGREE_PX=1.500000");
			sbPreset.AppendLine("GEOM_DYNAMIC_MARGIN=0.250000");
			sbPreset.AppendLine("GEOM_ENABLE=0");
			sbPreset.AppendLine("GEOM_MASK_REJECTED=0.350000");
			sbPreset.AppendLine("GEOM_OUTLIER_PX=4.000000");
			sbPreset.AppendLine("GEOM_PARALLAX=0.020000");
			sbPreset.AppendLine("LUMA_TOLERANCE=0.250000");
			sbPreset.AppendLine("MASK_STRENGTH=1.000000");
			sbPreset.AppendLine("MV_CONSISTENCY=1.400000");
			sbPreset.AppendLine("MV_LOWRES_FILTER=0");
			sbPreset.AppendLine("MV_PROVIDER_INFO=0");
			sbPreset.AppendLine("MV_SCALE=1.000000");
			sbPreset.AppendLine("MV_SIGN=1.000000,1.000000");
			sbPreset.AppendLine("MV_VALIDATE=1");
			sbPreset.AppendLine("PreprocessorDefinitions=DLSS5_MV_PROVIDER=3");
			sbPreset.AppendLine("STATIC_BIAS=0.150000");
			sbPreset.AppendLine("STATIC_MIN_CONTRAST=0.012000");
			sbPreset.AppendLine("VALIDATE_DEPTH=1");
			sbPreset.AppendLine("VALIDATE_LUMA=0");
			sbPreset.AppendLine("VALIDATE_MV=1");
			sbPreset.AppendLine("VALIDATE_STATIC=1");
			sbPreset.AppendLine();
			sbPreset.AppendLine("[MartysMods_LAUNCHPAD.fx]");
			sbPreset.AppendLine("LAUNCHPAD_OPTICAL_FLOW_RESOLUTION=1");
			sbPreset.AppendLine("LAUNCHPAD_OPTICAL_FLOW_SMOOTHING=0.500000");
			sbPreset.AppendLine();
			sbPreset.AppendLine("[lumenite_Kernel.fx]");
			sbPreset.AppendLine($"LUMENITE_INTENSITY={lumeniteIntensity.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture)}");
			sbPreset.AppendLine("LUMENITE_RADIUS=1.500000");
			sbPreset.AppendLine();
			sbPreset.AppendLine("[MartyMods_Sharpen.fx]");
			sbPreset.AppendLine($"SHARP_AMT={sharpenAmt.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture)}");
			sbPreset.AppendLine("SHARP_RAD=1.000000");
			sbPreset.AppendLine();
			sbPreset.AppendLine("[MartysMods_SOLARIS.fx]");
			sbPreset.AppendLine("SOLARIS_ENABLE_BLOOM=0");
			sbPreset.AppendLine("SOLARIS_HDR_MODE=1");
			sbPreset.AppendLine($"SOLARIS_SHOULDER_CONTRAST={solarisContrast.ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture)}");
			File.WriteAllText(presetIniPath, sbPreset.ToString(), Encoding.UTF8);

			// Também sincroniza os arquivos modelo no RuntimeAssets para inicialização limpa
			if (Directory.Exists(RuntimeDlss5Dir))
			{
				try
				{
					File.Copy(cfgPath, Path.Combine(RuntimeDlss5Dir, "dlss5-feed.cfg"), true);
					File.Copy(reshadeIniPath, Path.Combine(RuntimeDlss5Dir, "ReShade.ini"), true);
					File.Copy(presetIniPath, Path.Combine(RuntimeDlss5Dir, "ReShadePreset.ini"), true);
				}
				catch { }
			}
		}
		catch (Exception ex)
		{
			AppLogger.Warn("DLSS5Service.ActualizarConfigFile: " + ex.Message);
		}
	}

	private static void CopiarSiExiste(string origen, string destino)
	{
		if (File.Exists(origen))
		{
			string dir = Path.GetDirectoryName(destino)!;
			if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
			File.Copy(origen, destino, true);
		}
	}

	private static void CopiarDirectorioRecursivo(string origen, string destino)
	{
		Directory.CreateDirectory(destino);
		foreach (string file in Directory.GetFiles(origen, "*.*", SearchOption.AllDirectories))
		{
			string rel = Path.GetRelativePath(origen, file);
			string dest = Path.Combine(destino, rel);
			string destFolder = Path.GetDirectoryName(dest)!;
			if (!Directory.Exists(destFolder)) Directory.CreateDirectory(destFolder);
			File.Copy(file, dest, true);
		}
	}
}