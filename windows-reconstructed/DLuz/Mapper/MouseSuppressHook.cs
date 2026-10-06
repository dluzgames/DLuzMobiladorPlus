using System;
using System.Runtime.InteropServices;

namespace DLuz.Mapper;

public sealed class MouseSuppressHook : IDisposable
{
	private delegate nint LowLevelMouseProc(int nCode, nint wParam, nint lParam);

	private const int WH_MOUSE_LL = 14;

	private const int WM_LBUTTONDOWN = 513;

	private const int WM_LBUTTONUP = 514;

	private const int WM_LBUTTONDBLCLK = 515;

	private const int WM_RBUTTONDOWN = 516;

	private const int WM_RBUTTONUP = 517;

	private const int WM_RBUTTONDBLCLK = 518;

	private const int WM_MBUTTONDOWN = 519;

	private const int WM_MBUTTONUP = 520;

	private const int WM_MBUTTONDBLCLK = 521;

	private const int WM_XBUTTONDOWN = 523;

	private const int WM_XBUTTONUP = 524;

	private const int WM_XBUTTONDBLCLK = 525;

	private readonly LowLevelMouseProc _proc;

	private nint _hook = IntPtr.Zero;

	private volatile bool _suprimir;

	public bool Instalado => _hook != IntPtr.Zero;

	public MouseSuppressHook()
	{
		_proc = HookCallback;
	}

	public void Instalar()
	{
		if (_hook == IntPtr.Zero)
		{
			_hook = SetWindowsHookEx(14, _proc, GetModuleHandle(null), 0u);
		}
	}

	public void Desinstalar()
	{
		_suprimir = false;
		if (_hook != IntPtr.Zero)
		{
			try
			{
				UnhookWindowsHookEx(_hook);
			}
			catch
			{
			}
			_hook = IntPtr.Zero;
		}
	}

	public void Suprimir(bool activo)
	{
		_suprimir = activo;
	}

	private static bool EsClic(int msg)
	{
		if ((uint)(msg - 513) <= 8u || (uint)(msg - 523) <= 2u)
		{
			return true;
		}
		return false;
	}

	private nint HookCallback(int nCode, nint wParam, nint lParam)
	{
		if (nCode >= 0 && _suprimir && EsClic((int)wParam))
		{
			return 1;
		}
		return CallNextHookEx(_hook, nCode, wParam, lParam);
	}

	public void Dispose()
	{
		Desinstalar();
	}

	[DllImport("user32.dll", SetLastError = true)]
	private static extern nint SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, nint hMod, uint dwThreadId);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool UnhookWindowsHookEx(nint hhk);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

	[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern nint GetModuleHandle(string? lpModuleName);
}
