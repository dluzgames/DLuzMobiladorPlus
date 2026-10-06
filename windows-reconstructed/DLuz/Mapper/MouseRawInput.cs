using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace DLuz.Mapper;

public sealed class MouseRawInput : IDisposable
{
	private struct RAWINPUTDEVICE
	{
		public ushort usUsagePage;

		public ushort usUsage;

		public uint dwFlags;

		public nint hwndTarget;
	}

	private struct RAWINPUTHEADER
	{
		public uint dwType;

		public uint dwSize;

		public nint hDevice;

		public nint wParam;
	}

	private struct RAWMOUSE
	{
		public ushort usFlags;

		public ushort usFlagsPad;

		public uint ulButtons;

		public uint ulRawButtons;

		public int lLastX;

		public int lLastY;

		public uint ulExtraInformation;
	}

	private struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	private const int WM_INPUT = 255;

	private const uint RID_INPUT = 268435459u;

	private const uint RIM_TYPEMOUSE = 0u;

	private const uint RIDEV_INPUTSINK = 256u;

	private const uint RIDEV_REMOVE = 1u;

	private const ushort HID_USAGE_PAGE_GENERIC = 1;

	private const ushort HID_USAGE_GENERIC_MOUSE = 2;

	private const ushort MOUSE_MOVE_ABSOLUTE = 1;

	private const ushort RI_MOUSE_LEFT_DOWN = 1;

	private const ushort RI_MOUSE_LEFT_UP = 2;

	private const ushort RI_MOUSE_RIGHT_DOWN = 4;

	private const ushort RI_MOUSE_RIGHT_UP = 8;

	private const ushort RI_MOUSE_MIDDLE_DOWN = 16;

	private const ushort RI_MOUSE_MIDDLE_UP = 32;

	private const ushort RI_MOUSE_BUTTON_4_DOWN = 64;

	private const ushort RI_MOUSE_BUTTON_4_UP = 128;

	private const ushort RI_MOUSE_BUTTON_5_DOWN = 256;

	private const ushort RI_MOUSE_BUTTON_5_UP = 512;

	private const ushort RI_MOUSE_WHEEL = 1024;

	private const int SM_CXSCREEN = 0;

	private const int SM_CYSCREEN = 1;

	private readonly InputState _estado;

	private HwndSource? _source;

	private volatile bool _capturando;

	private volatile bool _confinar;

	public MouseRawInput(InputState estado)
	{
		_estado = estado;
	}

	public void Iniciar()
	{
		if (_source == null)
		{
			HwndSourceParameters hwndSourceParameters = new HwndSourceParameters("DLuzMapperRawInput");
			hwndSourceParameters.Width = 0;
			hwndSourceParameters.Height = 0;
			hwndSourceParameters.ParentWindow = new IntPtr(-3);
			hwndSourceParameters.WindowStyle = 0;
			HwndSourceParameters parameters = hwndSourceParameters;
			_source = new HwndSource(parameters);
			_source.AddHook(WndProc);
			RAWINPUTDEVICE rAWINPUTDEVICE = new RAWINPUTDEVICE
			{
				usUsagePage = 1,
				usUsage = 2,
				dwFlags = 256u,
				hwndTarget = _source.Handle
			};
			RegisterRawInputDevices(new RAWINPUTDEVICE[1] { rAWINPUTDEVICE }, 1u, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
		}
	}

	public void Capturar(bool activo)
	{
		_capturando = activo;
		if (!activo)
		{
			_confinar = false;
			_estado.TeclaArriba(257);
			_estado.TeclaArriba(258);
			_estado.TeclaArriba(259);
			ClipCursor(IntPtr.Zero);
		}
	}

	public void Confinar(bool activo)
	{
		if (_confinar != activo)
		{
			_confinar = activo;
			if (activo)
			{
				ConfinarCursorAlCentro();
			}
			else
			{
				ClipCursor(IntPtr.Zero);
			}
		}
	}

	public void Detener()
	{
		if (_source != null)
		{
			RAWINPUTDEVICE rAWINPUTDEVICE = new RAWINPUTDEVICE
			{
				usUsagePage = 1,
				usUsage = 2,
				dwFlags = 1u,
				hwndTarget = IntPtr.Zero
			};
			try
			{
				RegisterRawInputDevices(new RAWINPUTDEVICE[1] { rAWINPUTDEVICE }, 1u, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());
			}
			catch
			{
			}
			ClipCursor(IntPtr.Zero);
			_source.RemoveHook(WndProc);
			_source.Dispose();
			_source = null;
		}
	}

	public void Dispose()
	{
		Detener();
	}

	private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
	{
		if (msg == 255)
		{
			ProcesarRawInput(lParam);
		}
		return IntPtr.Zero;
	}

	private void ProcesarRawInput(nint hRawInput)
	{
		uint num = (uint)Marshal.SizeOf<RAWINPUTHEADER>();
		uint pcbSize = 0u;
		if (GetRawInputData(hRawInput, 268435459u, IntPtr.Zero, ref pcbSize, num) != 0 || pcbSize == 0)
		{
			return;
		}
		nint num2 = Marshal.AllocHGlobal((int)pcbSize);
		try
		{
			if (GetRawInputData(hRawInput, 268435459u, num2, ref pcbSize, num) != pcbSize || Marshal.PtrToStructure<RAWINPUTHEADER>(num2).dwType != 0)
			{
				return;
			}
			RAWMOUSE rAWMOUSE = Marshal.PtrToStructure<RAWMOUSE>(num2 + (int)num);
			ushort num3 = (ushort)(rAWMOUSE.ulButtons & 0xFFFF);
			if ((num3 & 0x10) != 0)
			{
				_estado.TeclaAbajo(259);
			}
			if ((num3 & 0x20) != 0)
			{
				_estado.TeclaArriba(259);
			}
			if ((num3 & 0x40) != 0)
			{
				_estado.TeclaAbajo(260);
			}
			if ((num3 & 0x80) != 0)
			{
				_estado.TeclaArriba(260);
			}
			if ((num3 & 0x100) != 0)
			{
				_estado.TeclaAbajo(261);
			}
			if ((num3 & 0x200) != 0)
			{
				_estado.TeclaArriba(261);
			}
			if ((num3 & 4) != 0)
			{
				_estado.TeclaAbajo(258);
			}
			if ((num3 & 8) != 0)
			{
				_estado.TeclaArriba(258);
			}
			if (!_capturando)
			{
				return;
			}
			if ((num3 & 1) != 0)
			{
				_estado.TeclaAbajo(257);
			}
			if ((num3 & 2) != 0)
			{
				_estado.TeclaArriba(257);
			}
			if ((num3 & 0x400) != 0)
			{
				short num4 = (short)((rAWMOUSE.ulButtons >> 16) & 0xFFFF);
				if (num4 > 0)
				{
					_estado.Pulso(262);
				}
				else if (num4 < 0)
				{
					_estado.Pulso(263);
				}
			}
			if ((rAWMOUSE.usFlags & 1) == 0 && (rAWMOUSE.lLastX != 0 || rAWMOUSE.lLastY != 0))
			{
				_estado.AgregarDeltaMouse(rAWMOUSE.lLastX, rAWMOUSE.lLastY);
				if (_confinar)
				{
					ConfinarCursorAlCentro();
				}
			}
		}
		finally
		{
			Marshal.FreeHGlobal(num2);
		}
	}

	private static void ConfinarCursorAlCentro()
	{
		int num = GetSystemMetrics(0) / 2;
		int num2 = GetSystemMetrics(1) / 2;
		SetCursorPos(num, num2);
		RECT lpRect = new RECT
		{
			Left = num - 1,
			Top = num2 - 1,
			Right = num + 1,
			Bottom = num2 + 1
		};
		ClipCursor(ref lpRect);
	}

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern uint GetRawInputData(nint hRawInput, uint uiCommand, nint pData, ref uint pcbSize, uint cbSizeHeader);

	[DllImport("user32.dll")]
	private static extern int GetSystemMetrics(int nIndex);

	[DllImport("user32.dll")]
	private static extern bool SetCursorPos(int x, int y);

	[DllImport("user32.dll")]
	private static extern bool ClipCursor(ref RECT lpRect);

	[DllImport("user32.dll")]
	private static extern bool ClipCursor(nint lpRect);
}
