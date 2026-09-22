using System;
using System.Collections.Generic;

namespace DLuz.Mapper;

public sealed class TouchInjector
{
	private readonly ScrcpyControlClient _control;

	private readonly Dictionary<string, long> _pointerIds = new Dictionary<string, long>
	{
		["joystick"] = 0L,
		["camera"] = 1L,
		["fire"] = 2L,
		["aim"] = 3L,
		["mouse_middle"] = 4L,
		["cursor"] = 5L,
		["camera2"] = 6L
	};

	private readonly Dictionary<string, (int x, int y)> _ultima = new Dictionary<string, (int, int)>();

	private readonly HashSet<string> _abajo = new HashSet<string>();

	private long _siguienteId = 10L;

	public int Ancho { get; private set; } = 1080;

	public int Alto { get; private set; } = 2400;

	public TouchInjector(ScrcpyControlClient control)
	{
		_control = control;
	}

	public void EstablecerResolucion(int ancho, int alto)
	{
		if (ancho > 0)
		{
			Ancho = ancho;
		}
		if (alto > 0)
		{
			Alto = alto;
		}
		MapperDiag.Log($"Resolución establecida {Ancho}x{Alto} orientación={((Alto >= Ancho) ? "portrait" : "landscape")} (solicitada {ancho}x{alto})");
	}

	public void Tocar(string dedo, int x, int y)
	{
		TocarInterno(dedo, Math.Clamp(x, 0, Ancho - 1), Math.Clamp(y, 0, Alto - 1));
	}

	public void TocarLibre(string dedo, int x, int y)
	{
		TocarInterno(dedo, x, y);
	}

	private void TocarInterno(string dedo, int x, int y)
	{
		long num = ObtenerId(dedo);
		bool flag = _abajo.Contains(dedo);
		byte action = (byte)(flag ? 2 : 0);
		bool flag2 = _control.EnviarTouch(action, num, x, y, Ancho, Alto, 1f);
		MapperDiag.Log($"{(flag ? "MOVE" : "DOWN")} dedo={dedo} id={num} xy=({x},{y}) res={Ancho}x{Alto} enviado={flag2}");
		if (flag2)
		{
			_abajo.Add(dedo);
			_ultima[dedo] = (x, y);
		}
	}

	public void Soltar(string dedo)
	{
		if (_abajo.Contains(dedo))
		{
			long num = ObtenerId(dedo);
			int num2;
			int num3;
			if (_ultima.TryGetValue(dedo, out (int, int) value))
			{
				(num2, num3) = value;
			}
			else
			{
				int num4 = Ancho / 2;
				int num5 = Alto / 2;
				num3 = num5;
				num2 = num4;
			}
			bool value2 = _control.EnviarTouch(1, num, num2, num3, Ancho, Alto, 0f);
			MapperDiag.Log($"UP   dedo={dedo} id={num} xy=({num2},{num3}) res={Ancho}x{Alto} enviado={value2}");
			_abajo.Remove(dedo);
			_ultima.Remove(dedo);
		}
	}

	public bool EstaAbajo(string dedo)
	{
		return _abajo.Contains(dedo);
	}

	public (int x, int y) UltimaPosicion(string dedo)
	{
		if (!_ultima.TryGetValue(dedo, out (int, int) value))
		{
			return (x: Ancho / 2, y: Alto / 2);
		}
		return value;
	}

	public void SoltarTodos()
	{
		foreach (string item in new List<string>(_abajo))
		{
			Soltar(item);
		}
	}

	private long ObtenerId(string dedo)
	{
		if (!_pointerIds.TryGetValue(dedo, out var value))
		{
			value = _siguienteId++;
			_pointerIds[dedo] = value;
		}
		return value;
	}
}

