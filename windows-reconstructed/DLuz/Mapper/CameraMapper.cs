using System;
using System.Diagnostics;

namespace DLuz.Mapper;

public sealed class CameraMapper
{
	private const string Dedo = "camera";

	private const double CoordMax = 100000000.0;

	private const double Base = 1080.0;

	private const double VentanaMs = 550.0;

	private const double TopePx = 50.0;

	private const double SueltaMs = 60.0;

	private const double Pendiente = 0.003799999999999999;

	private static readonly (double a, double b)[] Puntos = new(double, double)[12]
	{
		(0.0, 0.55),
		(40.0, 0.62),
		(115.0, 0.78),
		(190.0, 1.0),
		(265.0, 1.18),
		(340.0, 1.37),
		(415.0, 1.57),
		(490.0, 1.81),
		(640.0, 2.42),
		(790.0, 3.05),
		(1000.0, 4.11),
		(1700.0, 6.77)
	};

	private readonly int _anclaX;

	private readonly int _anclaY;

	private readonly double _sensibilidadX;

	private readonly double _sensibilidadY;

	private readonly int? _teclaLibre;

	private readonly int _signoX;

	private readonly int _signoY;

	private double _x;

	private double _y;

	private int _txX;

	private int _txY;

	private int _x0;

	private int _y0;

	private long _plantadoEn;

	private long _ultimoEnvio;

	private double _retenido;

	public bool Bloqueada { get; set; }

	private static (int x, int y) PuntoInicio(TouchInjector inj)
	{
		return (x: (int)Math.Round((double)inj.Ancho * 0.85), y: 0);
	}

	public CameraMapper(CameraConfig cfg)
	{
		_anclaX = cfg.ZoneX;
		_anclaY = cfg.ZoneY;
		_sensibilidadX = ((cfg.SensitivityX > 0.0) ? cfg.SensitivityX : ((cfg.Sensitivity > 0.0) ? cfg.Sensitivity : 1.0));
		_sensibilidadY = ((cfg.SensitivityY > 0.0) ? cfg.SensitivityY : (_sensibilidadX * ((cfg.SensitivityRatioY > 0.0) ? cfg.SensitivityRatioY : 1.0)));
		_teclaLibre = KeyNames.Resolver(cfg.FreeMouseKey);
		_signoX = ((!cfg.InvertX) ? 1 : (-1));
		_signoY = ((!cfg.InvertY) ? 1 : (-1));
		_x = _anclaX;
		_y = _anclaY;
	}

	public bool Actualizar(InputState estado, TouchInjector inj, long nowMs)
	{
		if (_teclaLibre.HasValue && estado.Presionada(_teclaLibre.Value))
		{
			estado.TomarDeltaMouse();
			if (inj.EstaAbajo("camera"))
			{
				inj.Soltar("camera");
			}
			return false;
		}
		if (!inj.EstaAbajo("camera"))
		{
			Plantar(inj);
		}
		var (num, num2) = estado.TomarDeltaMouse();
		if (Bloqueada)
		{
			return false;
		}
		if (num == 0 && num2 == 0)
		{
			return false;
		}
		double x = (double)(num * _signoX) * _sensibilidadX;
		double y = (double)(num2 * _signoY) * _sensibilidadY;
		(double, double) tuple2 = Ajustar(x, y, inj);
		x = tuple2.Item1;
		y = tuple2.Item2;
		_x = Math.Clamp(_x + x, -100000000.0, 100000000.0);
		_y = Math.Clamp(_y + y, -100000000.0, 100000000.0);
		if (Math.Abs(x) >= 0.5 || Math.Abs(y) >= 0.5)
		{
			MapperDiag.Log($"CAM delta=({num},{num2}) paso=({x:F1},{y:F1}) xy=({(int)_x},{(int)_y})");
		}
		return Enviar(inj);
	}

	public void Latido(TouchInjector inj)
	{
		if (inj.EstaAbajo("camera") && !Enviar(inj))
		{
			inj.TocarCrudo("camera", _txX, _txY);
		}
	}

	public void Desplazar(double dx, double dy, TouchInjector inj)
	{
		if (inj.EstaAbajo("camera"))
		{
			(double, double) tuple = Ajustar(dx, dy, inj);
			dx = tuple.Item1;
			dy = tuple.Item2;
			_x = Math.Clamp(_x + dx, -100000000.0, 100000000.0);
			_y = Math.Clamp(_y + dy, -100000000.0, 100000000.0);
			Enviar(inj);
		}
	}

	public void Liberar(TouchInjector inj)
	{
		_x = _anclaX;
		_y = _anclaY;
		if (inj.EstaAbajo("camera"))
		{
			inj.Soltar("camera");
		}
	}

	private void Plantar(TouchInjector inj)
	{
		(int x, int y) tuple = PuntoInicio(inj);
		int item = tuple.x;
		int item2 = tuple.y;
		_x = item;
		_y = item2;
		inj.Tocar("camera", item, item2);
		_txX = item * inj.Escala;
		_txY = item2 * inj.Escala;
		_x0 = item;
		_y0 = item2;
		_retenido = 0.0;
		_plantadoEn = Stopwatch.GetTimestamp();
		_ultimoEnvio = _plantadoEn;
		MapperDiag.Log($"CAM dedo plantado borde=({item},{item2})");
	}

	private (double, double) Ajustar(double x, double y, TouchInjector inj)
	{
		double num = (double)Math.Min(inj.Ancho, inj.Alto) / 1080.0;
		return (x / Valor(Math.Abs(_x - (double)_x0) / num), y);
	}

	private static double Valor(double d)
	{
		(double, double) tuple = Puntos[^1];
		if (d >= tuple.Item1)
		{
			return tuple.Item2 + (d - tuple.Item1) * 0.003799999999999999;
		}
		for (int i = 1; i < Puntos.Length; i++)
		{
			if (d <= Puntos[i].a)
			{
				return Puntos[i - 1].b + (Puntos[i].b - Puntos[i - 1].b) * (d - Puntos[i - 1].a) / (Puntos[i].a - Puntos[i - 1].a);
			}
		}
		return tuple.Item2;
	}

	private bool Enviar(TouchInjector inj)
	{
		double y = _y;
		long timestamp = Stopwatch.GetTimestamp();
		if (Stopwatch.GetElapsedTime(_plantadoEn, timestamp).TotalMilliseconds < 550.0)
		{
			double num = (double)_y0 + 50.0 * (double)Math.Min(inj.Ancho, inj.Alto) / 1080.0;
			_retenido = Math.Max(0.0, _y - num);
		}
		else if (_retenido > 0.0)
		{
			_retenido *= Math.Exp((0.0 - Stopwatch.GetElapsedTime(_ultimoEnvio, timestamp).TotalMilliseconds) / 60.0);
			if (_retenido < 0.5)
			{
				_retenido = 0.0;
			}
		}
		double num2 = y - _retenido;
		_ultimoEnvio = timestamp;
		int num3 = (int)Math.Round(_x * (double)inj.Escala);
		int num4 = (int)Math.Round(num2 * (double)inj.Escala);
		if (num3 == _txX && num4 == _txY)
		{
			return false;
		}
		inj.TocarCrudo("camera", num3, num4);
		_txX = num3;
		_txY = num4;
		return true;
	}
}
