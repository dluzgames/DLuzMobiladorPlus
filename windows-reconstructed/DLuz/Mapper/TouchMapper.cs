using System.Collections.Generic;

namespace DLuz.Mapper;

public sealed class TouchMapper
{
	private sealed class Boton
	{
		public int Vk;

		public int X;

		public int Y;

		public string Dedo = "";

		public string Label = "";

		public bool EsHold;

		public bool EsRepeat;

		public bool EsDoubleTap;

		public int FaseDoubleTap;

		public long DoubleTapStepMs;

		public long IntervaloMs = 150L;

		public bool EsMouse;

		public bool TeclaPrev;

		public long TapInicioMs = -1L;

		public long UltimoKeepMs;

		public long UltimoCicloMs;
	}

	private const long KeepAliveMs = 120L;

	private const long TapMs = 60L;

	private readonly JoystickMapper _joystick;

	private readonly CameraMapper _camera;

	private readonly List<Boton> _botones = new List<Boton>();

	private readonly HashSet<int> _vksTeclado = new HashSet<int>();

	private readonly TouchInjector _inj;

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
			bool flag = num3.Value >= 256;
			string text = button.Mode ?? (flag ? "hold" : "tap");
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
				EsDoubleTap = (text == "double_tap" || text == "doubletap" || text == "double"),
				IntervaloMs = ((button.RepeatMs > 0) ? button.RepeatMs : 150),
				EsMouse = flag
			});
			num2++;
			if (num3.Value < 256)
			{
				_vksTeclado.Add(num3.Value);
			}
			MapperDiag.Log($"Botón registrado: label='{button.Label}' key='{button.Key}' vk=0x{num3.Value:X} modo={text} xy=({button.X},{button.Y}) dedo={text2}");
		}
	}

	public void Frame(InputState estado, long nowMs)
	{
		_camera.Actualizar(estado, _inj, nowMs);
		_joystick.Actualizar(estado, _inj, nowMs);
		foreach (Boton botone in _botones)
		{
			bool flag = estado.Presionada(botone.Vk);
			if (botone.EsDoubleTap)
			{
				if (flag && !botone.TeclaPrev && botone.FaseDoubleTap == 0)
				{
					botone.FaseDoubleTap = 1;
					_inj.Tocar(botone.Dedo, botone.X, botone.Y);
					botone.DoubleTapStepMs = nowMs;
				}
				else if (botone.FaseDoubleTap == 1 && nowMs - botone.DoubleTapStepMs >= 35)
				{
					_inj.Soltar(botone.Dedo);
					botone.FaseDoubleTap = 2;
					botone.DoubleTapStepMs = nowMs;
				}
				else if (botone.FaseDoubleTap == 2 && nowMs - botone.DoubleTapStepMs >= 35)
				{
					_inj.Tocar(botone.Dedo, botone.X, botone.Y);
					botone.FaseDoubleTap = 3;
					botone.DoubleTapStepMs = nowMs;
				}
				else if (botone.FaseDoubleTap == 3 && nowMs - botone.DoubleTapStepMs >= 35)
				{
					_inj.Soltar(botone.Dedo);
					botone.FaseDoubleTap = 0;
					botone.DoubleTapStepMs = 0;
				}
			}
			else if (botone.EsRepeat)
			{
				bool flag2 = _inj.EstaAbajo(botone.Dedo);
				if (flag)
				{
					if (!flag2 && nowMs - botone.UltimoCicloMs >= botone.IntervaloMs)
					{
						_inj.Tocar(botone.Dedo, botone.X, botone.Y);
						botone.TapInicioMs = nowMs;
					}
					else if (flag2 && nowMs - botone.TapInicioMs >= 60)
					{
						_inj.Soltar(botone.Dedo);
						botone.UltimoCicloMs = nowMs;
					}
				}
				else if (flag2)
				{
					_inj.Soltar(botone.Dedo);
					botone.UltimoCicloMs = 0L;
				}
			}
			else if (botone.EsHold)
			{
				bool flag3 = _inj.EstaAbajo(botone.Dedo);
				if (flag && !flag3)
				{
					MapperDiag.Log($"HOLD down: '{botone.Label}' dedo={botone.Dedo} xy=({botone.X},{botone.Y})");
					_inj.Tocar(botone.Dedo, botone.X, botone.Y);
					botone.UltimoKeepMs = nowMs;
				}
				else if (!flag && flag3)
				{
					MapperDiag.Log("HOLD up: '" + botone.Label + "' dedo=" + botone.Dedo);
					_inj.Soltar(botone.Dedo);
				}
				else if (!botone.EsMouse && flag && flag3 && nowMs - botone.UltimoKeepMs >= 120)
				{
					_inj.Tocar(botone.Dedo, botone.X, botone.Y);
					botone.UltimoKeepMs = nowMs;
				}
			}
			else
			{
				if (flag && !botone.TeclaPrev)
				{
					MapperDiag.Log($"TAP down: '{botone.Label}' dedo={botone.Dedo} xy=({botone.X},{botone.Y})");
					_inj.Tocar(botone.Dedo, botone.X, botone.Y);
					botone.TapInicioMs = nowMs;
				}
				if (botone.TapInicioMs >= 0 && nowMs - botone.TapInicioMs >= 60)
				{
					MapperDiag.Log("TAP up: '" + botone.Label + "' dedo=" + botone.Dedo);
					_inj.Soltar(botone.Dedo);
					botone.TapInicioMs = -1L;
				}
			}
			botone.TeclaPrev = flag;
		}
	}

	public void Liberar()
	{
		_joystick.Liberar(_inj);
		_camera.Liberar(_inj);
		foreach (Boton botone in _botones)
		{
			botone.TeclaPrev = false;
			botone.TapInicioMs = -1L;
			if (_inj.EstaAbajo(botone.Dedo))
			{
				_inj.Soltar(botone.Dedo);
			}
		}
	}

	public void ForceReset()
	{
		_joystick.ForceReset(_inj);
		_camera.Resetar(_inj);
		foreach (Boton botone in _botones)
		{
			botone.TeclaPrev = false;
			botone.TapInicioMs = -1L;
			botone.FaseDoubleTap = 0;
			if (_inj.EstaAbajo(botone.Dedo))
				_inj.Soltar(botone.Dedo);
		}
	}

	public IReadOnlyCollection<int> TeclasTecladoMapeadas()
	{
		return _vksTeclado;
	}
}


