using System;
using System.Collections.Generic;

namespace DLuz.Mapper;

public sealed class JoystickMapper
{
	private readonly struct Direccion(int vk, double dx, double dy)
	{
		public readonly int Vk = vk;

		public readonly double Dx = dx;

		public readonly double Dy = dy;
	}

	private const string Dedo = "joystick";

	private const long KeepAliveMs = 50L;

	private readonly List<Direccion> _direcciones = new List<Direccion>();

	private readonly int _centroX;

	private readonly int _centroY;

	private readonly int _radio;

	private bool _activo;

	private int _destX;

	private int _destY;

	private long _ultimoEnvioMs;

	public JoystickMapper(JoystickConfig cfg)
	{
		_centroX = cfg.CenterX;
		_centroY = cfg.CenterY;
		_radio = cfg.Radius;
		foreach (KeyValuePair<string, int[]> key in cfg.Keys)
		{
			int? num = KeyNames.Resolver(key.Key);
			if (num.HasValue)
			{
				int[] value = key.Value;
				if (value != null && value.Length >= 2)
				{
					_direcciones.Add(new Direccion(num.Value, key.Value[0], key.Value[1]));
				}
			}
		}
	}

	public void Actualizar(InputState estado, TouchInjector inj, long nowMs)
	{
		double num = 0.0;
		double num2 = 0.0;
		foreach (Direccion direccione in _direcciones)
		{
			if (estado.Presionada(direccione.Vk))
			{
				num += direccione.Dx;
				num2 += direccione.Dy;
			}
		}
		if (num == 0.0 && num2 == 0.0)
		{
			if (_activo)
			{
				inj.Tocar("joystick", _centroX, _centroY);
				inj.Soltar("joystick");
				_activo = false;
			}
			return;
		}
		double num3 = Math.Sqrt(num * num + num2 * num2);
		int num4 = _centroX + (int)Math.Round(num / num3 * (double)_radio);
		int num5 = _centroY + (int)Math.Round(num2 / num3 * (double)_radio);
		if (!_activo)
		{
			inj.Tocar("joystick", _centroX, _centroY);
			inj.Tocar("joystick", num4, num5);
			_activo = true;
			_destX = num4;
			_destY = num5;
			_ultimoEnvioMs = nowMs;
		}
		else if (num4 != _destX || num5 != _destY || nowMs - _ultimoEnvioMs >= 50)
		{
			inj.Tocar("joystick", num4, num5);
			_destX = num4;
			_destY = num5;
			_ultimoEnvioMs = nowMs;
		}
	}

	public void ForceReset(TouchInjector inj) => Liberar(inj);

	public void Liberar(TouchInjector inj)
	{
		if (_activo)
		{
			inj.Tocar("joystick", _centroX, _centroY);
			inj.Soltar("joystick");
			_activo = false;
		}
	}
}
