using System;
using System.Collections.Generic;
using System.Globalization;

namespace DLuz.Mapper;

public sealed class TouchMapper
{
	private sealed class PasoScript
	{
		public string Tipo = "";

		public int X;

		public int Y;

		public double Dx;

		public double Dy;

		public int Ms;
	}

	private sealed class Boton
	{
		public int Vk;

		public int X;

		public int Y;

		public string Dedo = "";

		public string Label = "";

		public bool EsHold;

		public bool EsRepeat;

		public long IntervaloMs = 150L;

		public bool EsMouse;

		public bool TeclaPrev;

		public long TapInicioMs = -1L;

		public long UltimoKeepMs;

		public long UltimoCicloMs;

		public bool EsSwipe;

		public bool EsZoom;

		public bool EsScroll;

		public bool EsRotate;

		public bool EsRadial;

		public bool EsScript;

		public int X2;

		public int Y2;

		public long DuracionMs = 220L;

		public int AnguloDeg = 90;

		public string DedoB = "";

		public long GestoInicioMs = -1L;

		public long PausaHastaMs;

		public double RadialX;

		public double RadialY;

		public int RadialTxX;

		public int RadialTxY;

		public List<PasoScript> Pasos = new List<PasoScript>();

		public int PasoActual = -1;

		public long PasoHastaMs;

		public bool TapPendiente;

		public bool BloqueaMouse;

		public double CamDx;

		public double CamDy;

		public double CamHecho;

		public long CamInicioMs = -1L;

		public long CamMs;
	}

	private const long KeepAliveMs = 120L;

	private const long TapMs = 60L;

	private const long PausaScrollMs = 40L;

	private readonly JoystickMapper _joystick;

	private readonly CameraMapper _camera;

	private readonly List<Boton> _botones = new List<Boton>();

	private readonly HashSet<int> _vksTeclado = new HashSet<int>();

	private readonly TouchInjector _inj;

	public bool CamaraExterna { get; set; }

	public void CamaraTick(InputState estado, long nowMs)
	{
		if (!_camera.Actualizar(estado, _inj, nowMs))
		{
			_camera.Latido(_inj);
		}
	}

	public TouchMapper(KeymapConfig keymap, TouchInjector inj)
	{
		_inj = inj;
		_joystick = new JoystickMapper(keymap.Joystick);
		_camera = new CameraMapper(keymap.Camera);
		foreach (KeyValuePair<string, int[]> key in keymap.Joystick.Keys)
		{
			int? num = KeyNames.Resolver(key.Key);
			if (num.HasValue && num.Value < 256)
			{
				_vksTeclado.Add(num.Value);
			}
		}
		int num2 = 0;
		foreach (ButtonConfig button in keymap.Buttons)
		{
			int? num3 = KeyNames.Resolver(button.Key);
			if (!num3.HasValue)
			{
				MapperDiag.Log($"Botón ignorado (tecla no reconocida): label='{button.Label}' key='{button.Key}'");
				continue;
			}
			int value = num3.Value;
			bool flag = (uint)(value - 262) <= 1u;
			bool flag2 = flag;
			bool flag3 = num3.Value >= 256;
			string text = button.Mode ?? ((flag3 && !flag2) ? "hold" : "tap");
			string text2 = num3.Value switch
			{
				257 => "fire", 
				258 => "aim", 
				259 => "mouse_middle", 
				_ => $"btn{num2}", 
			};
			_botones.Add(new Boton
			{
				Vk = num3.Value,
				X = button.X,
				Y = button.Y,
				Label = button.Label,
				Dedo = text2,
				EsHold = (text == "hold"),
				EsRepeat = (text == "repeat"),
				IntervaloMs = ((button.RepeatMs > 0) ? button.RepeatMs : 150),
				EsMouse = flag3,
				EsSwipe = (text == "swipe"),
				EsZoom = (text == "zoom"),
				EsScroll = (text == "scroll"),
				EsRotate = (text == "rotate"),
				EsRadial = CatalogoControles.EsRadial(text),
				EsScript = (text == "script"),
				X2 = button.X2,
				Y2 = button.Y2,
				DuracionMs = ((button.DurationMs > 0) ? button.DurationMs : 220),
				AnguloDeg = ((button.AngleDeg != 0) ? button.AngleDeg : 90),
				DedoB = text2 + "_b",
				Pasos = ((text == "script") ? ParsearScript(button.Script, _inj) : new List<PasoScript>())
			});
			num2++;
			if (num3.Value < 256)
			{
				_vksTeclado.Add(num3.Value);
			}
			MapperDiag.Log($"Botón registrado: label='{button.Label}' key='{button.Key}' vk=0x{num3.Value:X} modo={text} xy=({button.X},{button.Y}) dedo={text2}");
		}
	}

	public static List<string> ValidarScript(string? texto)
	{
		List<string> list = new List<string>();
		if (string.IsNullOrWhiteSpace(texto))
		{
			return list;
		}
		int num = 0;
		string[] array = texto.Split('\n');
		foreach (string obj in array)
		{
			num++;
			string text = obj.Trim();
			if (text.Length != 0 && !text.StartsWith("#"))
			{
				string[] array2 = text.Split(new char[3] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
				bool flag;
				int result;
				switch (array2[0].ToLowerInvariant())
				{
				case "tap":
					flag = array2.Length >= 3 && EsNumero(array2[1]) && EsNumero(array2[2]) && (array2.Length < 4 || int.TryParse(array2[3], out result));
					break;
				case "down":
				case "move":
					flag = array2.Length >= 3 && EsNumero(array2[1]) && EsNumero(array2[2]);
					break;
				case "up":
				case "lockmouse":
				case "unlockmouse":
					flag = true;
					break;
				case "wait":
					flag = array2.Length >= 2 && int.TryParse(array2[1], out result);
					break;
				case "cam":
					flag = array2.Length >= 3 && EsNumero(array2[1]) && EsNumero(array2[2]) && (array2.Length < 4 || int.TryParse(array2[3], out result));
					break;
				default:
					flag = false;
					break;
				}
				if (!flag)
				{
					list.Add($"{num}: {text}");
				}
			}
		}
		return list;
	}

	private static bool EsNumero(string s)
	{
		double result;
		return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
	}

	private static List<PasoScript> ParsearScript(string? texto, TouchInjector inj)
	{
		List<PasoScript> list = new List<PasoScript>();
		if (string.IsNullOrWhiteSpace(texto))
		{
			return list;
		}
		string[] array = texto.Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].Trim();
			if (text.Length == 0 || text.StartsWith("#"))
			{
				continue;
			}
			string[] array2 = text.Split(new char[3] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
			string text2 = array2[0].ToLowerInvariant();
			switch (text2)
			{
			case "tap":
			case "down":
			case "move":
			{
				if (array2.Length >= 3 && double.TryParse(array2[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var result2) && double.TryParse(array2[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var result3))
				{
					list.Add(new PasoScript
					{
						Tipo = text2,
						X = (int)Math.Round(result2 / 100.0 * (double)inj.Ancho),
						Y = (int)Math.Round(result3 / 100.0 * (double)inj.Alto),
						Ms = ((text2 == "tap" && array2.Length >= 4 && int.TryParse(array2[3], out var result4)) ? Math.Max(1, result4) : 0)
					});
				}
				break;
			}
			case "up":
			case "lockmouse":
			case "unlockmouse":
				list.Add(new PasoScript
				{
					Tipo = text2
				});
				break;
			case "cam":
			{
				if (array2.Length >= 3 && double.TryParse(array2[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var result5) && double.TryParse(array2[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var result6))
				{
					int ms = ((array2.Length >= 4 && int.TryParse(array2[3], out var result7)) ? Math.Max(0, result7) : 0);
					list.Add(new PasoScript
					{
						Tipo = "cam",
						Dx = result5 / 100.0 * (double)inj.Ancho,
						Dy = result6 / 100.0 * (double)inj.Alto,
						Ms = ms
					});
				}
				break;
			}
			case "wait":
			{
				if (array2.Length >= 2 && int.TryParse(array2[1], out var result))
				{
					list.Add(new PasoScript
					{
						Tipo = "wait",
						Ms = Math.Max(0, result)
					});
				}
				break;
			}
			}
		}
		return list;
	}

	public void Frame(InputState estado, long nowMs, bool soloTeclado = false)
	{
		bool flag = false;
		if (!soloTeclado)
		{
			foreach (Boton botone in _botones)
			{
				if (botone.EsRadial)
				{
					bool flag2 = estado.Presionada(botone.Vk);
					ActualizarRadial(botone, flag2, estado, nowMs);
					if (flag2)
					{
						flag = true;
					}
				}
			}
		}
		if (!soloTeclado)
		{
			_camera.Bloqueada = _botones.Exists((Boton boton) => boton.BloqueaMouse);
			if (!CamaraExterna)
			{
				_camera.Actualizar(estado, _inj, nowMs);
			}
		}
		if (!flag)
		{
			_joystick.Actualizar(estado, _inj, nowMs);
		}
		foreach (Boton botone2 in _botones)
		{
			if ((soloTeclado && botone2.EsMouse) || botone2.EsRadial)
			{
				continue;
			}
			bool flag3 = estado.Presionada(botone2.Vk);
			if (botone2.EsScript)
			{
				ActualizarScript(botone2, flag3, nowMs);
			}
			else if (botone2.EsScroll)
			{
				ActualizarScroll(botone2, flag3, nowMs);
			}
			else if (botone2.EsSwipe || botone2.EsZoom || botone2.EsRotate)
			{
				if (((botone2.GestoInicioMs < 0) & flag3) && !botone2.TeclaPrev)
				{
					botone2.GestoInicioMs = nowMs;
				}
				if (botone2.GestoInicioMs >= 0)
				{
					double num = (double)(nowMs - botone2.GestoInicioMs) / (double)botone2.DuracionMs;
					if (num > 1.0)
					{
						num = 1.0;
					}
					if (botone2.EsZoom)
					{
						int num2 = (botone2.X + botone2.X2) / 2;
						int num3 = (botone2.Y + botone2.Y2) / 2;
						_inj.Tocar(botone2.Dedo, (int)((double)num2 + (double)(botone2.X - num2) * num), (int)((double)num3 + (double)(botone2.Y - num3) * num));
						_inj.Tocar(botone2.DedoB, (int)((double)num2 + (double)(botone2.X2 - num2) * num), (int)((double)num3 + (double)(botone2.Y2 - num3) * num));
					}
					else if (botone2.EsRotate)
					{
						var (x, y) = PuntoArco(botone2, num);
						_inj.Tocar(botone2.Dedo, x, y);
					}
					else
					{
						_inj.Tocar(botone2.Dedo, (int)((double)botone2.X + (double)(botone2.X2 - botone2.X) * num), (int)((double)botone2.Y + (double)(botone2.Y2 - botone2.Y) * num));
					}
					if (num >= 1.0)
					{
						_inj.Soltar(botone2.Dedo);
						if (botone2.EsZoom)
						{
							_inj.Soltar(botone2.DedoB);
						}
						botone2.GestoInicioMs = -1L;
					}
				}
			}
			else if (botone2.EsRepeat)
			{
				bool flag4 = _inj.EstaAbajo(botone2.Dedo);
				if (flag3)
				{
					if (!flag4 && nowMs - botone2.UltimoCicloMs >= botone2.IntervaloMs)
					{
						_inj.Tocar(botone2.Dedo, botone2.X, botone2.Y);
						botone2.TapInicioMs = nowMs;
					}
					else if (flag4 && nowMs - botone2.TapInicioMs >= 60)
					{
						_inj.Soltar(botone2.Dedo);
						botone2.UltimoCicloMs = nowMs;
					}
				}
				else if (flag4)
				{
					_inj.Soltar(botone2.Dedo);
					botone2.UltimoCicloMs = 0L;
				}
			}
			else if (botone2.EsHold)
			{
				bool flag5 = _inj.EstaAbajo(botone2.Dedo);
				if (flag3 && !flag5)
				{
					MapperDiag.Log($"HOLD down: '{botone2.Label}' dedo={botone2.Dedo} xy=({botone2.X},{botone2.Y})");
					_inj.Tocar(botone2.Dedo, botone2.X, botone2.Y);
					botone2.UltimoKeepMs = nowMs;
				}
				else if (!flag3 & flag5)
				{
					MapperDiag.Log("HOLD up: '" + botone2.Label + "' dedo=" + botone2.Dedo);
					_inj.Soltar(botone2.Dedo);
				}
				else if ((!botone2.EsMouse & flag3 & flag5) && nowMs - botone2.UltimoKeepMs >= 120)
				{
					_inj.Tocar(botone2.Dedo, botone2.X, botone2.Y);
					botone2.UltimoKeepMs = nowMs;
				}
			}
			else
			{
				if (flag3 && !botone2.TeclaPrev)
				{
					MapperDiag.Log($"TAP down: '{botone2.Label}' dedo={botone2.Dedo} xy=({botone2.X},{botone2.Y})");
					_inj.Tocar(botone2.Dedo, botone2.X, botone2.Y);
					botone2.TapInicioMs = nowMs;
				}
				if (botone2.TapInicioMs >= 0 && nowMs - botone2.TapInicioMs >= 60)
				{
					MapperDiag.Log("TAP up: '" + botone2.Label + "' dedo=" + botone2.Dedo);
					_inj.Soltar(botone2.Dedo);
					botone2.TapInicioMs = -1L;
				}
			}
			botone2.TeclaPrev = flag3;
		}
	}

	private static (int x, int y) PuntoArco(Boton b, double t)
	{
		double num = b.X2 - b.X;
		double num2 = b.Y2 - b.Y;
		double num3 = Math.Sqrt(num * num + num2 * num2);
		double num4 = Math.Atan2(num2, num) + t * (double)b.AnguloDeg * Math.PI / 180.0;
		return (x: (int)Math.Round((double)b.X + num3 * Math.Cos(num4)), y: (int)Math.Round((double)b.Y + num3 * Math.Sin(num4)));
	}

	private void ActualizarScroll(Boton b, bool presionado, long nowMs)
	{
		if (!presionado)
		{
			if (b.GestoInicioMs >= 0)
			{
				_inj.Soltar(b.Dedo);
				b.GestoInicioMs = -1L;
			}
			b.TeclaPrev = false;
			return;
		}
		if (b.GestoInicioMs < 0)
		{
			if (nowMs < b.PausaHastaMs)
			{
				return;
			}
			b.GestoInicioMs = nowMs;
		}
		double num = (double)(nowMs - b.GestoInicioMs) / (double)b.DuracionMs;
		if (num > 1.0)
		{
			num = 1.0;
		}
		_inj.Tocar(b.Dedo, (int)((double)b.X + (double)(b.X2 - b.X) * num), (int)((double)b.Y + (double)(b.Y2 - b.Y) * num));
		if (num >= 1.0)
		{
			_inj.Soltar(b.Dedo);
			b.GestoInicioMs = -1L;
			b.PausaHastaMs = nowMs + 40;
		}
	}

	private void ActualizarRadial(Boton b, bool presionado, InputState estado, long nowMs)
	{
		bool flag = _inj.EstaAbajo(b.Dedo);
		if (presionado && !flag)
		{
			b.RadialX = b.X;
			b.RadialY = b.Y;
			b.RadialTxX = b.X;
			b.RadialTxY = b.Y;
			estado.TomarDeltaMouse();
			_inj.Tocar(b.Dedo, b.X, b.Y);
			MapperDiag.Log($"RADIAL down: '{b.Label}' dedo={b.Dedo} xy=({b.X},{b.Y})");
			return;
		}
		if (!presionado)
		{
			if (flag)
			{
				_inj.Soltar(b.Dedo);
				MapperDiag.Log("RADIAL up: '" + b.Label + "' dedo=" + b.Dedo);
			}
			return;
		}
		var (num, num2) = estado.TomarDeltaMouse();
		if (num == 0 && num2 == 0)
		{
			if (nowMs - b.UltimoKeepMs >= 120)
			{
				_inj.Tocar(b.Dedo, b.RadialTxX, b.RadialTxY);
				b.UltimoKeepMs = nowMs;
			}
			return;
		}
		double num3 = b.X2 - b.X;
		double num4 = b.Y2 - b.Y;
		double num5 = Math.Max(20.0, Math.Sqrt(num3 * num3 + num4 * num4));
		double num6 = b.RadialX + (double)num - (double)b.X;
		double num7 = b.RadialY + (double)num2 - (double)b.Y;
		double num8 = Math.Sqrt(num6 * num6 + num7 * num7);
		if (num8 > num5)
		{
			num6 = num6 / num8 * num5;
			num7 = num7 / num8 * num5;
		}
		b.RadialX = (double)b.X + num6;
		b.RadialY = (double)b.Y + num7;
		int num9 = (int)Math.Round(b.RadialX);
		int num10 = (int)Math.Round(b.RadialY);
		if (num9 != b.RadialTxX || num10 != b.RadialTxY)
		{
			_inj.Tocar(b.Dedo, num9, num10);
			b.RadialTxX = num9;
			b.RadialTxY = num10;
			b.UltimoKeepMs = nowMs;
		}
	}

	private void ActualizarScript(Boton b, bool presionado, long nowMs)
	{
		if (((b.PasoActual < 0) & presionado) && !b.TeclaPrev && b.Pasos.Count > 0)
		{
			b.PasoActual = 0;
			b.PasoHastaMs = nowMs;
			b.TapPendiente = false;
			MapperDiag.Log($"SCRIPT inicio: '{b.Label}' pasos={b.Pasos.Count}");
		}
		b.TeclaPrev = presionado;
		if (b.PasoActual < 0)
		{
			return;
		}
		AvanzarCam(b, nowMs);
		while (b.PasoActual < b.Pasos.Count && nowMs >= b.PasoHastaMs)
		{
			if (b.TapPendiente)
			{
				if (_inj.EstaAbajo(b.Dedo))
				{
					_inj.Soltar(b.Dedo);
				}
				b.TapPendiente = false;
				b.PasoActual++;
				continue;
			}
			PasoScript pasoScript = b.Pasos[b.PasoActual];
			switch (pasoScript.Tipo)
			{
			case "tap":
				if (_inj.EstaAbajo(b.Dedo))
				{
					_inj.Soltar(b.Dedo);
				}
				_inj.Tocar(b.Dedo, pasoScript.X, pasoScript.Y);
				b.TapPendiente = true;
				b.PasoHastaMs = nowMs + ((pasoScript.Ms > 0) ? pasoScript.Ms : 60);
				continue;
			case "down":
				if (_inj.EstaAbajo(b.Dedo))
				{
					_inj.Soltar(b.Dedo);
				}
				_inj.Tocar(b.Dedo, pasoScript.X, pasoScript.Y);
				break;
			case "move":
				_inj.Tocar(b.Dedo, pasoScript.X, pasoScript.Y);
				break;
			case "up":
				if (_inj.EstaAbajo(b.Dedo))
				{
					_inj.Soltar(b.Dedo);
				}
				break;
			case "wait":
				b.PasoHastaMs = nowMs + pasoScript.Ms;
				break;
			case "lockmouse":
				b.BloqueaMouse = true;
				_camera.Bloqueada = true;
				break;
			case "unlockmouse":
				b.BloqueaMouse = false;
				break;
			case "cam":
				b.CamDx = pasoScript.Dx;
				b.CamDy = pasoScript.Dy;
				b.CamHecho = 0.0;
				b.CamMs = pasoScript.Ms;
				b.CamInicioMs = nowMs;
				b.PasoHastaMs = nowMs + pasoScript.Ms;
				AvanzarCam(b, nowMs);
				break;
			}
			b.PasoActual++;
		}
		if (b.PasoActual >= b.Pasos.Count && !b.TapPendiente && b.CamInicioMs < 0)
		{
			if (_inj.EstaAbajo(b.Dedo))
			{
				_inj.Soltar(b.Dedo);
			}
			b.PasoActual = -1;
			b.BloqueaMouse = false;
			MapperDiag.Log("SCRIPT fin: '" + b.Label + "'");
		}
	}

	private void AvanzarCam(Boton b, long nowMs)
	{
		if (b.CamInicioMs >= 0)
		{
			double num = ((b.CamMs <= 0) ? 1.0 : Math.Min(1.0, (double)(nowMs - b.CamInicioMs) / (double)b.CamMs));
			double num2 = num - b.CamHecho;
			if (num2 > 0.0)
			{
				_camera.Desplazar(b.CamDx * num2, b.CamDy * num2, _inj);
			}
			b.CamHecho = num;
			if (num >= 1.0)
			{
				b.CamInicioMs = -1L;
			}
		}
	}

	public void LiberarMouse()
	{
		_camera.Liberar(_inj);
		_camera.Bloqueada = false;
		foreach (Boton botone in _botones)
		{
			botone.BloqueaMouse = false;
			botone.CamInicioMs = -1L;
			if (botone.EsMouse || botone.EsRadial)
			{
				botone.TeclaPrev = false;
				botone.TapInicioMs = -1L;
				botone.GestoInicioMs = -1L;
				if (_inj.EstaAbajo(botone.Dedo))
				{
					_inj.Soltar(botone.Dedo);
				}
				if (botone.DedoB.Length > 0 && _inj.EstaAbajo(botone.DedoB))
				{
					_inj.Soltar(botone.DedoB);
				}
			}
		}
	}

	public void ForceReset() => Liberar();

	public void Liberar()
	{
		_joystick.Liberar(_inj);
		_camera.Liberar(_inj);
		_camera.Bloqueada = false;
		foreach (Boton botone in _botones)
		{
			botone.TeclaPrev = false;
			botone.TapInicioMs = -1L;
			botone.GestoInicioMs = -1L;
			botone.PasoActual = -1;
			botone.TapPendiente = false;
			botone.BloqueaMouse = false;
			botone.CamInicioMs = -1L;
			if (_inj.EstaAbajo(botone.Dedo))
			{
				_inj.Soltar(botone.Dedo);
			}
			if (botone.DedoB.Length > 0 && _inj.EstaAbajo(botone.DedoB))
			{
				_inj.Soltar(botone.DedoB);
			}
		}
	}

	public IReadOnlyCollection<int> TeclasTecladoMapeadas()
	{
		return _vksTeclado;
	}
}
