using System;
using System.Runtime.InteropServices;
using DLuz.Helpers;

namespace DLuz;

public static class DualLayoutHelper
{
	private struct RECTLAYOUT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	private struct MONITORINFO
	{
		public int cbSize;

		public RECTLAYOUT rcMonitor;

		public RECTLAYOUT rcWork;

		public uint dwFlags;
	}

	public const int MargenLayoutPx = 16;

	public const int EntradaVentanaAnchoPx = 176;

	public const int EntradaVentanaAltoPx = 132;

	public const double VisualAnchoFraccion = 0.6;

	private const uint SPI_GETWORKAREA = 48u;

	private const uint MONITOR_DEFAULTTONEAREST = 2u;

	public static DualLayout CalcularDesdeVentana(nint hwndReferencia)
	{
		try
		{
			if (hwndReferencia != IntPtr.Zero)
			{
				nint num = MonitorFromWindow(hwndReferencia, 2u);
				if (num != IntPtr.Zero)
				{
					MONITORINFO lpmi = new MONITORINFO
					{
						cbSize = Marshal.SizeOf<MONITORINFO>()
					};
					if (GetMonitorInfo(num, ref lpmi) && lpmi.rcWork.Right > lpmi.rcWork.Left && lpmi.rcWork.Bottom > lpmi.rcWork.Top)
					{
						return CalcularDesdeArea(lpmi.rcWork.Left, lpmi.rcWork.Top, lpmi.rcWork.Right, lpmi.rcWork.Bottom);
					}
				}
			}
		}
		catch
		{
		}
		try
		{
			AppLogger.Warn("Layout dual: no se pudo resolver el monitor del launcher; uso el monitor primario.");
		}
		catch
		{
		}
		return CalcularRespaldo();
	}

	public static DualLayout CalcularRespaldo()
	{
		try
		{
			RECTLAYOUT pvParam = default(RECTLAYOUT);
			if (SystemParametersInfo(48u, 0u, ref pvParam, 0u) && pvParam.Right > pvParam.Left && pvParam.Bottom > pvParam.Top)
			{
				return CalcularDesdeArea(pvParam.Left, pvParam.Top, pvParam.Right, pvParam.Bottom);
			}
		}
		catch
		{
		}
		try
		{
			AppLogger.Warn("Layout dual: sin área de trabajo disponible; las ventanas nacerán donde scrcpy decida.");
		}
		catch
		{
		}
		return new DualLayout
		{
			PosicionDisponible = false
		};
	}

	public static DualLayout CalcularDesdeArea(int left, int top, int right, int bottom)
	{
		int num = right - left;
		return new DualLayout
		{
			PosicionDisponible = true,
			VisualX = left + 16,
			VisualY = top + 16,
			EntradaX = Math.Max(left + 16, right - 176 - 16),
			EntradaY = top + 16,
			VisualAnchoSugerido = (int)Math.Round((double)num * 0.6)
		};
	}

	[DllImport("user32.dll", SetLastError = true)]
	private static extern nint MonitorFromWindow(nint hwnd, uint dwFlags);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFO lpmi);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref RECTLAYOUT pvParam, uint fWinIni);
}

