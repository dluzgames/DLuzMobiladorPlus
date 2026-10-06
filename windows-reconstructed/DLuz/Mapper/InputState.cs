using System;
using System.Collections.Generic;
using System.Linq;

namespace DLuz.Mapper;

public sealed class InputState
{
	private const long PulsoMs = 70L;

	private readonly object _gate = new object();

	private readonly HashSet<int> _presionadas = new HashSet<int>();

	private readonly Dictionary<int, long> _pulsos = new Dictionary<int, long>();

	private int _mouseDx;

	private int _mouseDy;

	public void TeclaAbajo(int vk)
	{
		lock (_gate)
		{
			_presionadas.Add(vk);
			int num = KeyNames.NormalizarVirtualKey(vk);
			if (num != vk)
			{
				_presionadas.Add(num);
			}
		}
	}

	public void TeclaArriba(int vk)
	{
		lock (_gate)
		{
			_presionadas.Remove(vk);
			int num = KeyNames.NormalizarVirtualKey(vk);
			if (num != vk)
			{
				_presionadas.Remove(num);
			}
		}
	}

	public bool Presionada(int vk)
	{
		lock (_gate)
		{
			return _presionadas.Contains(vk);
		}
	}

	public void Pulso(int vk)
	{
		lock (_gate)
		{
			_presionadas.Add(vk);
			_pulsos[vk] = Environment.TickCount64;
		}
	}

	public void ExpirarPulsos()
	{
		lock (_gate)
		{
			if (_pulsos.Count == 0)
			{
				return;
			}
			long ahora = Environment.TickCount64;
			foreach (int item in (from p in _pulsos
				where ahora - p.Value >= 70
				select p.Key).ToList())
			{
				_pulsos.Remove(item);
				_presionadas.Remove(item);
			}
		}
	}

	public void AgregarDeltaMouse(int dx, int dy)
	{
		lock (_gate)
		{
			_mouseDx += dx;
			_mouseDy += dy;
		}
	}

	public (int dx, int dy) TomarDeltaMouse()
	{
		lock (_gate)
		{
			(int, int) result = (_mouseDx, _mouseDy);
			_mouseDx = 0;
			_mouseDy = 0;
			return result;
		}
	}

	public void Reiniciar()
	{
		lock (_gate)
		{
			_presionadas.Clear();
			_pulsos.Clear();
			_mouseDx = 0;
			_mouseDy = 0;
		}
	}
}
