using System;
using System.Runtime.InteropServices;

namespace DLuz.Mapper;

public sealed class KeyboardHook : IDisposable
{
	private delegate nint LowLevelKeyboardProc(int nCode, nint wParam, nint lParam);

	private const int WH_KEYBOARD_LL = 13;

	private const int WM_KEYDOWN = 256;

	private const int WM_KEYUP = 257;

	private const int WM_SYSKEYDOWN = 260;

	private const int WM_SYSKEYUP = 261;

	private readonly LowLevelKeyboardProc _proc;

	private nint _hook = IntPtr.Zero;

	public Func<int, bool, bool>? Handler { get; set; }

	public bool Instalado => _hook != IntPtr.Zero;

	public KeyboardHook()
	{
		_proc = HookCallback;
	}

	public void Instalar()
	{
		if (_hook == IntPtr.Zero)
		{
			_hook = SetWindowsHookEx(13, _proc, GetModuleHandle(null), 0u);
		}
	}

	public void Desinstalar()
	{
		if (_hook != IntPtr.Zero)
		{
			UnhookWindowsHookEx(_hook);
			_hook = IntPtr.Zero;
		}
	}

	private nint HookCallback(int nCode, nint wParam, nint lParam)
	{
		if (nCode >= 0 && Handler != null)
		{
			int num = (int)wParam;
			bool flag = ((num == 256 || num == 260) ? true : false);
			bool flag2 = flag;
			flag = ((num == 257 || num == 261) ? true : false);
			bool flag3 = flag;
			if (flag2 | flag3)
			{
				int arg = Marshal.ReadInt32(lParam);
				try
				{
					if (Handler(arg, flag2))
					{
						return 1;
					}
				}
				catch
				{
				}
			}
		}
		return CallNextHookEx(_hook, nCode, wParam, lParam);
	}

	public void Dispose()
	{
		Desinstalar();
	}

	[DllImport("user32.dll", SetLastError = true)]
	private static extern nint SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, nint hMod, uint dwThreadId);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool UnhookWindowsHookEx(nint hhk);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

	[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern nint GetModuleHandle(string? lpModuleName);
}
