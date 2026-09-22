using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IniParser;
using IniParser.Model;
using DLuz.Helpers;

namespace DLuz;

public class PerfilManager
{
	private readonly string _perfilesPath;

	private IniData _data;

	private readonly FileIniDataParser _parser;

	private const int MaxBackups = 15;

	public static string? UltimoError { get; private set; }

	public PerfilManager(string perfilesPath)
	{
		_perfilesPath = perfilesPath;
		_parser = new FileIniDataParser();
		_data = CargarIni();
	}

	private IniData CargarIni()
	{
		UltimoError = null;
		if (!File.Exists(_perfilesPath))
		{
			return new IniData();
		}
		try
		{
			if (new FileInfo(_perfilesPath).Length > 102400)
			{
				return Task.Run(() => _parser.ReadFile(_perfilesPath)).GetAwaiter().GetResult();
			}
			return _parser.ReadFile(_perfilesPath);
		}
		catch (Exception ex)
		{
			AppLogger.Error("PerfilManager.CargarIni: fallo leyendo " + _perfilesPath, ex);
			UltimoError = ex.Message;
			return new IniData();
		}
	}

	private void GuardarIni()
	{
		string directoryName = Path.GetDirectoryName(_perfilesPath);
		if (!string.IsNullOrEmpty(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
		_parser.WriteFile(_perfilesPath, _data);
	}

	public (bool ok, string ruta, string error) HacerBackup()
	{
		try
		{
			if (!File.Exists(_perfilesPath))
			{
				return (ok: true, ruta: "", error: "");
			}
			string text = _perfilesPath + ".bak";
			File.Copy(_perfilesPath, text, overwrite: true);
			return (ok: true, ruta: text, error: "");
		}
		catch (Exception ex)
		{
			AppLogger.Error("PerfilManager.HacerBackup", ex);
			return (ok: false, ruta: "", error: ex.Message);
		}
	}

	public (bool ok, string ruta, string error) HacerBackup(string dirDestino, string marcaTiempo)
	{
		try
		{
			if (!File.Exists(_perfilesPath))
			{
				return (ok: true, ruta: "", error: "");
			}
			Directory.CreateDirectory(dirDestino);
			string text = Path.Combine(dirDestino, "perfiles_" + marcaTiempo + ".ini");
			File.Copy(_perfilesPath, text, overwrite: true);
			PurgarBackups(dirDestino);
			return (ok: true, ruta: text, error: "");
		}
		catch (Exception ex)
		{
			AppLogger.Error("PerfilManager.HacerBackup(dir)", ex);
			return (ok: false, ruta: "", error: ex.Message);
		}
	}

	private static void PurgarBackups(string dir)
	{
		try
		{
			foreach (FileInfo item in (from f in new DirectoryInfo(dir).GetFiles("perfiles_*.ini")
				orderby f.CreationTimeUtc descending
				select f).Skip(15).ToList())
			{
				try
				{
					item.Delete();
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
	}

	public (bool ok, int agregados, string error) AgregarPerfilesFaltantes(string iniContenido)
	{
		try
		{
			IniData iniData = _parser.Parser.Parse(iniContenido);
			int num = 0;
			foreach (SectionData section in iniData.Sections)
			{
				if (_data.Sections.ContainsSection(section.SectionName))
				{
					continue;
				}
				_data.Sections.AddSection(section.SectionName);
				foreach (KeyData key in section.Keys)
				{
					_data[section.SectionName][key.KeyName] = key.Value;
				}
				num++;
			}
			if (num > 0)
			{
				GuardarIni();
			}
			return (ok: true, agregados: num, error: "");
		}
		catch (Exception ex)
		{
			AppLogger.Error("PerfilManager.AgregarPerfilesFaltantes", ex);
			return (ok: false, agregados: 0, error: ex.Message);
		}
	}

	public (bool ok, int restaurados, int agregados, string error) RestaurarPerfilesBase(string iniContenido)
	{
		try
		{
			IniData iniData = _parser.Parser.Parse(iniContenido);
			int num = 0;
			int num2 = 0;
			foreach (SectionData section in iniData.Sections)
			{
				if (_data.Sections.ContainsSection(section.SectionName))
				{
					KeyDataCollection keyDataCollection = _data.Sections[section.SectionName];
					keyDataCollection.RemoveAllKeys();
					foreach (KeyData key in section.Keys)
					{
						keyDataCollection.AddKey(key.KeyName, key.Value);
					}
					num++;
					continue;
				}
				_data.Sections.AddSection(section.SectionName);
				foreach (KeyData key2 in section.Keys)
				{
					_data[section.SectionName][key2.KeyName] = key2.Value;
				}
				num2++;
			}
			GuardarIni();
			return (ok: true, restaurados: num, agregados: num2, error: "");
		}
		catch (Exception ex)
		{
			AppLogger.Error("PerfilManager.RestaurarPerfilesBase", ex);
			return (ok: false, restaurados: 0, agregados: 0, error: ex.Message);
		}
	}

	public List<string> ListarPerfiles()
	{
		List<string> list = new List<string>();
		foreach (SectionData section in _data.Sections)
		{
			list.Add(section.SectionName);
		}
		return list;
	}

	public bool QuitarPerfilTecnico(string nombre, string clave)
	{
		try
		{
			if (!_data.Sections.ContainsSection(nombre))
			{
				return false;
			}
			if (!_data[nombre].ContainsKey(clave))
			{
				return false;
			}
			_data.Sections.RemoveSection(nombre);
			GuardarIni();
			return true;
		}
		catch (Exception ex)
		{
			AppLogger.Error("PerfilManager.QuitarPerfilTecnico: fallo guardando perfiles.ini", ex);
			return false;
		}
	}

	public (bool exito, string error) AgregarPerfil(string nombre, ScrcpyConfig config)
	{
		try
		{
			if (_data.Sections.ContainsSection(nombre))
			{
				_data.Sections.RemoveSection(nombre);
			}
			_data.Sections.AddSection(nombre);
			KeyDataCollection keyDataCollection = _data[nombre];
			keyDataCollection["fps"] = config.Fps.ToString();
			keyDataCollection["bitrate"] = config.Bitrate.ToString();
			keyDataCollection["audio_buffer"] = config.AudioBuffer.ToString();
			keyDataCollection["audio_codec"] = config.AudioCodec ?? "opus";
			keyDataCollection["audio_bitrate"] = config.AudioBitrate.ToString();
			keyDataCollection["video"] = config.Video.ToString().ToLower();
			keyDataCollection["audio"] = config.Audio.ToString().ToLower();
			keyDataCollection["audio_doble"] = config.AudioDoble.ToString().ToLower();
			keyDataCollection["max_size"] = config.MaxSize.ToString();
			keyDataCollection["window_width"] = config.WindowWidth.ToString();
			keyDataCollection["window_height"] = config.WindowHeight.ToString();
			keyDataCollection["video_codec"] = config.VideoCodec ?? "h264";
			keyDataCollection["video_buffer"] = config.VideoBuffer.ToString();
			keyDataCollection["print_fps"] = config.PrintFps.ToString().ToLower();
			keyDataCollection["forward_all_clicks"] = config.ForwardAllClicks.ToString().ToLower();
			keyDataCollection["mostrar_flotante"] = config.MostrarFlotante.ToString().ToLower();
			keyDataCollection["overlay_fps"] = config.OverlayFps.ToString().ToLower();
			keyDataCollection["overlay_esquina"] = config.OverlayEsquina.ToString();
			keyDataCollection["wm_size_activo"] = config.WmSizeActivo.ToString().ToLower();
			keyDataCollection["wm_size_valor"] = config.WmSizeValor ?? "";
			keyDataCollection["use_advanced_encoder"] = config.UseAdvancedEncoder.ToString().ToLower();
			keyDataCollection["video_encoder"] = config.VideoEncoder ?? "";
			keyDataCollection["disable_screensaver"] = config.DisableScreensaver.ToString().ToLower();
			keyDataCollection["keep_active"] = config.KeepActive.ToString().ToLower();
			keyDataCollection["turn_screen_off"] = config.TurnScreenOff.ToString().ToLower();
			keyDataCollection["free_window_resize"] = config.FreeWindowResize.ToString().ToLower();
			keyDataCollection["background_color"] = config.BackgroundColorHex ?? "";
			keyDataCollection["fullscreen"] = config.Fullscreen.ToString().ToLower();
			keyDataCollection["shortcut_mod"] = config.ShortcutMod ?? "lalt";
			keyDataCollection["dpi"] = config.Dpi.ToString();
			keyDataCollection["teclado_modo"] = config.TecladoModo ?? "uhid";
			keyDataCollection["mouse_modo"] = config.MouseModo ?? "uhid";
			keyDataCollection["gamepad_modo"] = config.GamepadModo ?? "disabled";
			keyDataCollection["pointer_speed"] = config.PointerSpeed.ToString();
			keyDataCollection["render_driver"] = config.RenderDriver ?? "";
			GuardarIni();
			return (exito: true, error: "");
		}
		catch (Exception ex)
		{
			AppLogger.Error("PerfilManager.AgregarPerfil: fallo guardando perfiles.ini", ex);
			return (exito: false, error: "No se pudo guardar el archivo de perfiles. Detalles: " + ex.Message);
		}
	}

	public (bool exito, string error) EliminarPerfil(string nombre)
	{
		try
		{
			if (!_data.Sections.ContainsSection(nombre))
			{
				return (exito: false, error: "El perfil '" + nombre + "' no existe");
			}
			_data.Sections.RemoveSection(nombre);
			GuardarIni();
			return (exito: true, error: "");
		}
		catch (Exception ex)
		{
			AppLogger.Error("PerfilManager.EliminarPerfil: fallo guardando perfiles.ini", ex);
			return (exito: false, error: "No se pudo guardar el archivo de perfiles. Detalles: " + ex.Message);
		}
	}

	public ScrcpyConfig ObtenerPerfil(string nombre)
	{
		if (!_data.Sections.ContainsSection(nombre))
		{
			return null;
		}
		KeyDataCollection keyDataCollection = _data[nombre];
		return new ScrcpyConfig
		{
			Fps = ParseInt(keyDataCollection["fps"], 60),
			Bitrate = ParseInt(keyDataCollection["bitrate"], 16),
			AudioBuffer = ParseInt(keyDataCollection["audio_buffer"], 50),
			AudioCodec = (keyDataCollection["audio_codec"] ?? "opus"),
			AudioBitrate = ParseInt(keyDataCollection["audio_bitrate"], 128),
			Video = ParseBool(keyDataCollection["video"], defecto: true),
			Audio = ParseBool(keyDataCollection["audio"], defecto: true),
			AudioDoble = ParseBool(keyDataCollection["audio_doble"], defecto: false),
			MaxSize = ParseInt(keyDataCollection["max_size"], 0),
			WindowWidth = ParseInt(keyDataCollection["window_width"], 0),
			WindowHeight = ParseInt(keyDataCollection["window_height"], 0),
			VideoCodec = (keyDataCollection["video_codec"] ?? "h264"),
			VideoBuffer = ParseInt(keyDataCollection["video_buffer"], 0),
			AceleracionHardware = ParseBool(keyDataCollection["aceleracion_hardware"], defecto: false),
			PrintFps = ParseBool(keyDataCollection["print_fps"], defecto: false),
			ForwardAllClicks = ParseBool(keyDataCollection["forward_all_clicks"], defecto: false),
			MostrarFlotante = ParseBool(keyDataCollection["mostrar_flotante"], defecto: true),
			OverlayFps = ParseBool(keyDataCollection["overlay_fps"], defecto: true),
			OverlayEsquina = ParseInt(keyDataCollection["overlay_esquina"], 2),
			WmSizeActivo = ParseBool(keyDataCollection["wm_size_activo"], defecto: false),
			WmSizeValor = (keyDataCollection.ContainsKey("wm_size_valor") ? keyDataCollection["wm_size_valor"] : ""),
			UseAdvancedEncoder = ParseBool(keyDataCollection["use_advanced_encoder"], defecto: false),
			VideoEncoder = (keyDataCollection["video_encoder"] ?? ""),
			DisableScreensaver = ParseBool(keyDataCollection["disable_screensaver"], defecto: false),
			KeepActive = (keyDataCollection.ContainsKey("keep_active") ? ParseBool(keyDataCollection["keep_active"], defecto: false) : ParseBool(keyDataCollection["stay_awake"], defecto: false)),
			TurnScreenOff = ParseBool(keyDataCollection["turn_screen_off"], defecto: false),
			FreeWindowResize = ParseBool(keyDataCollection["free_window_resize"], defecto: false),
			BackgroundColorHex = (keyDataCollection.ContainsKey("background_color") ? (keyDataCollection["background_color"] ?? "") : ""),
			Fullscreen = ParseBool(keyDataCollection["fullscreen"], defecto: false),
			FullscreenCrop = (keyDataCollection["fullscreen_crop"] ?? ""),
			ShortcutMod = (keyDataCollection["shortcut_mod"] ?? "lalt"),
			ModoOtg = ParseBool(keyDataCollection["modo_otg"], defecto: false),
			OtgSerial = (keyDataCollection["otg_serial"] ?? ""),
			ResolucionAncho = ParseInt(keyDataCollection["resolucion_ancho"], 1080),
			ResolucionAlto = ParseInt(keyDataCollection["resolucion_alto"], 2400),
			AspectRatio = (keyDataCollection["aspect_ratio"] ?? "16:9"),
			CustomRatioW = ParseInt(keyDataCollection["custom_ratio_w"], 16),
			CustomRatioH = ParseInt(keyDataCollection["custom_ratio_h"], 9),
			Dpi = ParseInt(keyDataCollection["dpi"], 420),
			TecladoModo = (keyDataCollection.ContainsKey("teclado_modo") ? (keyDataCollection["teclado_modo"] ?? "uhid") : (keyDataCollection["input_mode"] ?? "uhid")),
			MouseModo = (keyDataCollection.ContainsKey("mouse_modo") ? (keyDataCollection["mouse_modo"] ?? "uhid") : (keyDataCollection["input_mode"] ?? "uhid")),
			GamepadModo = (keyDataCollection.ContainsKey("gamepad_modo") ? (keyDataCollection["gamepad_modo"] ?? "disabled") : "disabled"),
			PointerSpeed = ParseInt(keyDataCollection["pointer_speed"], 0),
			RenderDriver = (keyDataCollection.ContainsKey("render_driver") ? (keyDataCollection["render_driver"] ?? "") : "")
		};
	}

	public bool ExistePerfil(string nombre)
	{
		return _data.Sections.ContainsSection(nombre);
	}

	public (bool exito, string nombre, string error) ImportarDesdeArchivo(string rutaArchivo)
	{
		try
		{
			IniData iniData = _parser.ReadFile(rutaArchivo);
			string text = "";
			foreach (SectionData section in iniData.Sections)
			{
				if (string.IsNullOrEmpty(text))
				{
					text = section.SectionName;
				}
				if (_data.Sections.ContainsSection(section.SectionName))
				{
					_data.Sections.RemoveSection(section.SectionName);
				}
				_data.Sections.AddSection(section.SectionName);
				foreach (KeyData key in section.Keys)
				{
					_data[section.SectionName][key.KeyName] = key.Value;
				}
			}
			GuardarIni();
			return (exito: true, nombre: text, error: "");
		}
		catch (Exception ex)
		{
			AppLogger.Error("PerfilManager.ImportarDesdeArchivo: fallo importando/guardando perfiles", ex);
			return (exito: false, nombre: "", error: "No se pudo guardar el archivo de perfiles. Detalles: " + ex.Message);
		}
	}

	public (bool exito, string error) ExportarPerfil(string nombre, string rutaArchivo)
	{
		try
		{
			if (!_data.Sections.ContainsSection(nombre))
			{
				return (exito: false, error: "El perfil '" + nombre + "' no existe");
			}
			IniData iniData = new IniData();
			iniData.Sections.AddSection(nombre);
			foreach (KeyData item in _data[nombre])
			{
				iniData[nombre][item.KeyName] = item.Value;
			}
			_parser.WriteFile(rutaArchivo, iniData);
			return (exito: true, error: "");
		}
		catch (Exception ex)
		{
			AppLogger.Error("PerfilManager.ExportarPerfil: fallo escribiendo a " + rutaArchivo, ex);
			return (exito: false, error: ex.Message);
		}
	}

	public (bool exito, string error) RenombrarPerfil(string nombreActual, string nuevoNombre)
	{
		try
		{
			if (!_data.Sections.ContainsSection(nombreActual))
			{
				return (exito: false, error: "El perfil '" + nombreActual + "' no existe");
			}
			if (_data.Sections.ContainsSection(nuevoNombre))
			{
				return (exito: false, error: "Ya existe un perfil con el nombre '" + nuevoNombre + "'");
			}
			KeyDataCollection keyDataCollection = _data.Sections[nombreActual];
			_data.Sections.AddSection(nuevoNombre);
			KeyDataCollection keyDataCollection2 = _data.Sections[nuevoNombre];
			foreach (KeyData item in keyDataCollection)
			{
				keyDataCollection2.AddKey(item.KeyName, item.Value);
			}
			_data.Sections.RemoveSection(nombreActual);
			GuardarIni();
			return (exito: true, error: "");
		}
		catch (Exception ex)
		{
			AppLogger.Error("PerfilManager.RenombrarPerfil: fallo guardando perfiles.ini", ex);
			return (exito: false, error: "No se pudo guardar el archivo de perfiles. Detalles: " + ex.Message);
		}
	}

	public (bool exito, string error) GuardarConfigEnPerfil(string nombre, ScrcpyConfig config)
	{
		try
		{
			if (!_data.Sections.ContainsSection(nombre))
			{
				_data.Sections.AddSection(nombre);
			}
			KeyDataCollection keyDataCollection = _data.Sections[nombre];
			keyDataCollection.RemoveAllKeys();
			keyDataCollection.AddKey("video", config.Video.ToString());
			keyDataCollection.AddKey("audio", config.Audio.ToString());
			keyDataCollection.AddKey("audio_doble", config.AudioDoble.ToString());
			keyDataCollection.AddKey("fps", config.Fps.ToString());
			keyDataCollection.AddKey("bitrate", config.Bitrate.ToString());
			keyDataCollection.AddKey("max_size", config.MaxSize.ToString());
			keyDataCollection.AddKey("window_width", config.WindowWidth.ToString());
			keyDataCollection.AddKey("window_height", config.WindowHeight.ToString());
			keyDataCollection.AddKey("video_codec", config.VideoCodec ?? "h264");
			keyDataCollection.AddKey("video_buffer", config.VideoBuffer.ToString());
			keyDataCollection.AddKey("audio_buffer", config.AudioBuffer.ToString());
			keyDataCollection.AddKey("audio_codec", config.AudioCodec ?? "opus");
			keyDataCollection.AddKey("audio_bitrate", config.AudioBitrate.ToString());
			keyDataCollection.AddKey("print_fps", config.PrintFps.ToString());
			keyDataCollection.AddKey("forward_all_clicks", config.ForwardAllClicks.ToString());
			keyDataCollection.AddKey("mostrar_flotante", config.MostrarFlotante.ToString());
			keyDataCollection.AddKey("overlay_fps", config.OverlayFps.ToString());
			keyDataCollection.AddKey("overlay_esquina", config.OverlayEsquina.ToString());
			keyDataCollection.AddKey("wm_size_activo", config.WmSizeActivo.ToString());
			keyDataCollection.AddKey("wm_size_valor", config.WmSizeValor ?? "");
			keyDataCollection.AddKey("use_advanced_encoder", config.UseAdvancedEncoder.ToString());
			keyDataCollection.AddKey("video_encoder", config.VideoEncoder ?? "");
			keyDataCollection.AddKey("disable_screensaver", config.DisableScreensaver.ToString());
			keyDataCollection.AddKey("keep_active", config.KeepActive.ToString());
			keyDataCollection.AddKey("turn_screen_off", config.TurnScreenOff.ToString());
			keyDataCollection.AddKey("free_window_resize", config.FreeWindowResize.ToString());
			keyDataCollection.AddKey("background_color", config.BackgroundColorHex ?? "");
			keyDataCollection.AddKey("shortcut_mod", config.ShortcutMod ?? "lalt");
			keyDataCollection.AddKey("fullscreen", config.Fullscreen.ToString());
			keyDataCollection.AddKey("dpi", config.Dpi.ToString());
			keyDataCollection.AddKey("teclado_modo", config.TecladoModo ?? "uhid");
			keyDataCollection.AddKey("mouse_modo", config.MouseModo ?? "uhid");
			keyDataCollection.AddKey("gamepad_modo", config.GamepadModo ?? "disabled");
			keyDataCollection.AddKey("pointer_speed", config.PointerSpeed.ToString());
			keyDataCollection.AddKey("render_driver", config.RenderDriver ?? "");
			GuardarIni();
			return (exito: true, error: "");
		}
		catch (Exception ex)
		{
			AppLogger.Error("PerfilManager.GuardarConfigEnPerfil: fallo guardando perfiles.ini", ex);
			return (exito: false, error: "No se pudo guardar el archivo de perfiles. Detalles: " + ex.Message);
		}
	}

	private static int ParseInt(string valor, int defecto)
	{
		if (string.IsNullOrEmpty(valor))
		{
			return defecto;
		}
		if (!int.TryParse(valor, out var result))
		{
			return defecto;
		}
		return result;
	}

	private static bool ParseBool(string valor, bool defecto)
	{
		if (string.IsNullOrEmpty(valor))
		{
			return defecto;
		}
		if (!bool.TryParse(valor, out var result))
		{
			return defecto;
		}
		return result;
	}
}

