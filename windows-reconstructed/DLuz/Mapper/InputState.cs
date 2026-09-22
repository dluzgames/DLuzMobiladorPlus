using System;
using System.Collections.Generic;

namespace DLuz.Mapper;

public sealed class InputState
{
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
			if (_presionadas.Contains(vk))
			{
				return true;
			}
			if (_pulsos.TryGetValue(vk, out long fim))
			{
				if (Environment.TickCount64 < fim)
				{
					return true;
				}
				_pulsos.Remove(vk);
			}
			return false;
		}
	}

	public void Pulsar(int vk, int duracaoMs = 90)
	{
		lock (_gate)
		{
			_pulsos[vk] = Environment.TickCount64 + Math.Max(30, duracaoMs);
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

