using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace DLuz.Helpers;

public sealed class MouseRateTester : IDisposable
{
	[StructLayout(LayoutKind.Sequential)]
	private struct POINT
	{
		public int x;
		public int y;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct MSLLHOOKSTRUCT
	{
		public POINT pt;
		public uint mouseData;
		public uint flags;
		public uint time;
		public nint dwExtraInfo;
	}

	private delegate nint LowLevelMouseProc(int nCode, nint wParam, nint lParam);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern nint SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, nint hMod, uint dwThreadId);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool UnhookWindowsHookEx(nint hhk);

	[DllImport("user32.dll")]
	private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

	[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern nint GetModuleHandle(string? lpModuleName);

	private const int WH_MOUSE_LL = 14;
	private const int WM_MOUSEMOVE = 0x0200;

	private nint _hookId = IntPtr.Zero;
	private LowLevelMouseProc? _proc;
	private readonly List<double> _rates = new();
	private long _lastTimestamp;
	private long _lastMoveTime;
	private readonly object _lock = new();

	public bool EmExecucao { get; private set; }
	public double TaxaAtualHz { get; private set; }
	public double TaxaMediaHz { get; private set; }
	public double TaxaPicoHz { get; private set; }
	public int TaxaRecomendadaHz { get; set; } = 1000;
	public int AmostrasColetadas { get; private set; }
	public bool MouseEmMovimento { get; private set; }

	public event Action? OnAtualizado;

	public void Iniciar()
	{
		if (EmExecucao) return;

		lock (_lock)
		{
			_rates.Clear();
			_lastTimestamp = 0;
			_lastMoveTime = 0;
			TaxaAtualHz = 0;
			TaxaMediaHz = 0;
			TaxaPicoHz = 0;
			AmostrasColetadas = 0;
			MouseEmMovimento = false;
		}

		_proc = HookCallback;
		using (var curProcess = Process.GetCurrentProcess())
		using (var curModule = curProcess.MainModule)
		{
			_hookId = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(curModule?.ModuleName), 0);
		}

		EmExecucao = true;
	}

	public void Parar()
	{
		if (!EmExecucao) return;

		if (_hookId != IntPtr.Zero)
		{
			UnhookWindowsHookEx(_hookId);
			_hookId = IntPtr.Zero;
		}
		EmExecucao = false;
	}

	private nint HookCallback(int nCode, nint wParam, nint lParam)
	{
		if (nCode >= 0 && wParam == WM_MOUSEMOVE && EmExecucao)
		{
			long now = Stopwatch.GetTimestamp();
			long nowMs = Environment.TickCount64;

			lock (_lock)
			{
				_lastMoveTime = nowMs;
				MouseEmMovimento = true;

				if (_lastTimestamp > 0)
				{
					double dtSeconds = (double)(now - _lastTimestamp) / Stopwatch.Frequency;
					if (dtSeconds > 0.00005 && dtSeconds < 0.08) // 12Hz até 20.000Hz
					{
						double hz = 1.0 / dtSeconds;
						_rates.Add(hz);
						if (_rates.Count > 30)
						{
							_rates.RemoveAt(0);
						}

						AmostrasColetadas++;
						TaxaAtualHz = hz;
						if (hz > TaxaPicoHz && hz < 9000)
						{
							TaxaPicoHz = hz;
						}

						double soma = 0;
						foreach (var r in _rates)
						{
							soma += r;
						}
						TaxaMediaHz = soma / _rates.Count;
					}
				}
				_lastTimestamp = now;
			}
		}

		return CallNextHookEx(_hookId, nCode, wParam, lParam);
	}

	public void VerificarInatividade()
	{
		long nowMs = Environment.TickCount64;
		lock (_lock)
		{
			if (nowMs - _lastMoveTime > 400)
			{
				MouseEmMovimento = false;
				_lastTimestamp = 0;
				_rates.Clear();
				TaxaAtualHz = 0;
			}
		}
	}

	public void Dispose()
	{
		Parar();
	}
}
