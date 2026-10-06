using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace DLuz.Mapper;

internal sealed class TemporizadorFino : IDisposable
{
	private const uint CREATE_WAITABLE_TIMER_HIGH_RESOLUTION = 2u;

	private const uint TIMER_ALL_ACCESS = 2031619u;

	private nint _timer;

	public TemporizadorFino()
	{
		_timer = CreateWaitableTimerExW(IntPtr.Zero, null, 2u, 2031619u);
	}

	public void Esperar(double ms)
	{
		if (ms <= 0.0)
		{
			return;
		}
		if (_timer == IntPtr.Zero)
		{
			Thread.Sleep(Math.Max(1, (int)Math.Round(ms)));
			return;
		}
		long lpDueTime = -(long)(ms * 10000.0);
		if (!SetWaitableTimerEx(_timer, ref lpDueTime, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0u))
		{
			Thread.Sleep(Math.Max(1, (int)Math.Round(ms)));
		}
		else
		{
			WaitForSingleObject(_timer, uint.MaxValue);
		}
	}

	public void Dispose()
	{
		if (_timer != IntPtr.Zero)
		{
			CloseHandle(_timer);
			_timer = IntPtr.Zero;
		}
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	private static extern nint CreateWaitableTimerExW(nint lpTimerAttributes, string? lpTimerName, uint dwFlags, uint dwDesiredAccess);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool SetWaitableTimerEx(nint hTimer, ref long lpDueTime, int lPeriod, nint pfnCompletionRoutine, nint lpArgToCompletionRoutine, nint wakeContext, uint tolerableDelay);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern uint WaitForSingleObject(nint hHandle, uint dwMilliseconds);

	[DllImport("kernel32.dll", SetLastError = true)]
	private static extern bool CloseHandle(nint hObject);
}
