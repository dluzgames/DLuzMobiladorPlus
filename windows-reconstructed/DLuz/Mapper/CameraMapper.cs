using System;

namespace DLuz.Mapper;

public sealed class CameraMapper
{
	private const string Dedo = "camera";

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

	private readonly double _aceleracion;

	private readonly double _exponenteY;

	private readonly double _suavizado;

	private double _lastDeltaX;

	private double _lastDeltaY;

	public CameraMapper(CameraConfig cfg)
	{
		_anclaX = cfg.ZoneX;
		_anclaY = cfg.ZoneY;
		_sensibilidadX = ((cfg.SensitivityX > 0.0) ? cfg.SensitivityX : ((cfg.Sensitivity > 0.0) ? cfg.Sensitivity : 1.0));
		_sensibilidadY = ((cfg.SensitivityY > 0.0) ? cfg.SensitivityY : (_sensibilidadX * ((cfg.SensitivityRatioY > 0.0) ? cfg.SensitivityRatioY : 1.0)));
		_aceleracion = Math.Max(0.0, cfg.Acceleration);
		_exponenteY = Math.Max(1.0, cfg.ExponentY);
		_suavizado = Math.Clamp(cfg.Smoothing, 0.0, 0.5);
		_teclaLibre = KeyNames.Resolver(cfg.FreeMouseKey);
		_signoX = ((!cfg.InvertX) ? 1 : (-1));
		_signoY = ((!cfg.InvertY) ? 1 : (-1));
		_x = _anclaX;
		_y = _anclaY;
	}

	public void Actualizar(InputState estado, TouchInjector inj, long nowMs)
	{
		if (_teclaLibre.HasValue && estado.Presionada(_teclaLibre.Value))
		{
			estado.TomarDeltaMouse();
			if (inj.EstaAbajo("camera"))
			{
				inj.Soltar("camera");
			}
			return;
		}
		if (!inj.EstaAbajo("camera"))
		{
			_x = _anclaX;
			_y = _anclaY;
			inj.Tocar("camera", _anclaX, _anclaY);
			_txX = _anclaX;
			_txY = _anclaY;
			MapperDiag.Log($"CAM dedo plantado ancla=({_anclaX},{_anclaY})");
		}
		var (num, num2) = estado.TomarDeltaMouse();
		if (num != 0 || num2 != 0)
		{
			double dX = (double)num;
			double dY = (double)num2;

			// Suavização anti-jitter do sensor (ativa apenas se configurada > 0)
			if (_suavizado > 0.0)
			{
				dX = dX * (1.0 - _suavizado) + _lastDeltaX * _suavizado;
				dY = dY * (1.0 - _suavizado) + _lastDeltaY * _suavizado;
				_lastDeltaX = dX;
				_lastDeltaY = dY;
			}

			double stepX = (dX * _signoX) * _sensibilidadX;
			double stepY = (dY * _signoY) * _sensibilidadY;

			bool isPro = Services.LicenseService.Instance.TieneAccesoPro;

			// Curva de aceleração dinâmica (Exclusivo PRO)
			if (isPro && _aceleracion > 0.0)
			{
				double vel = Math.Sqrt(dX * dX + dY * dY);
				double boost = 1.0 + Math.Min(vel * 0.015, 2.0) * _aceleracion;
				stepX *= boost;
				stepY *= boost;
			}

			// Puxada de capa no Free Fire / FPS (movimento para cima - Exclusivo PRO)
			if (isPro && _exponenteY > 1.0 && num2 < 0)
			{
				double capaBoost = Math.Pow(Math.Abs(num2) / 6.0 + 1.0, _exponenteY - 1.0);
				stepY *= Math.Min(capaBoost, 2.5);
			}

			int ancho = (inj.Ancho > 0) ? inj.Ancho : 2400;
			int alto = (inj.Alto > 0) ? inj.Alto : 1080;

			// Raio de segurança da zona de giro da câmera (evita sair da tela do Android ou colidir com o joystick)
			double maxRadius = Math.Clamp(Math.Min(ancho, alto) * 0.32, 260.0, 400.0);
			int marginX = Math.Min(120, ancho / 10);
			int marginY = Math.Min(100, alto / 10);

			double nextX = _x + stepX;
			double nextY = _y + stepY;
			double diffX = nextX - _anclaX;
			double diffY = nextY - _anclaY;
			double distSq = diffX * diffX + diffY * diffY;

			if (distSq > maxRadius * maxRadius || nextX < marginX || nextX > (ancho - marginX) || nextY < marginY || nextY > (alto - marginY))
			{
				// Swipe Recenter Instantâneo (suspende e replanta na âncora em <1ms para giro infinito 360° sem travar)
				inj.Soltar("camera");
				_x = _anclaX;
				_y = _anclaY;
				inj.Tocar("camera", _anclaX, _anclaY);
				_txX = _anclaX;
				_txY = _anclaY;

				// Continua o movimento fluido a partir do ponto de ancoragem
				_x = Math.Clamp(_anclaX + stepX, marginX, ancho - marginX);
				_y = Math.Clamp(_anclaY + stepY, marginY, alto - marginY);
			}
			else
			{
				_x = Math.Clamp(nextX, marginX, ancho - marginX);
				_y = Math.Clamp(nextY, marginY, alto - marginY);
			}

			if (Math.Abs(stepX) >= 0.5 || Math.Abs(stepY) >= 0.5)
			{
				MapperDiag.Log($"CAM delta=({num},{num2}) paso=({stepX:F1},{stepY:F1}) xy=({(int)_x},{(int)_y})");
			}
			Enviar(inj);
		}
	}

	public void Resetar(TouchInjector inj)
	{
		Liberar(inj);
	}

	public void Liberar(TouchInjector inj)
	{
		_x = _anclaX;
		_y = _anclaY;
		_txX = _anclaX;
		_txY = _anclaY;
		_lastDeltaX = 0.0;
		_lastDeltaY = 0.0;
		if (inj.EstaAbajo("camera"))
		{
			inj.Soltar("camera");
		}
	}

	private void Enviar(TouchInjector inj)
	{
		int num = (int)Math.Round(_x);
		int num2 = (int)Math.Round(_y);
		if (num != _txX || num2 != _txY)
		{
			inj.Tocar("camera", num, num2);
			_txX = num;
			_txY = num2;
		}
	}
}
