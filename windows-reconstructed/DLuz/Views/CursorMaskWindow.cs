using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace DLuz.Views;

public sealed class CursorMaskWindow : Window
{
	private const int GWL_EXSTYLE = -20;

	private const int WS_EX_NOACTIVATE = 134217728;

	private const int WS_EX_TOOLWINDOW = 128;

	private static readonly nint HWND_TOPMOST = new IntPtr(-1);

	private const uint SWP_NOMOVE = 2u;

	private const uint SWP_NOSIZE = 1u;

	private const uint SWP_NOACTIVATE = 16u;

	private readonly DispatcherTimer _mantener;

	private nint _hwnd;

	public CursorMaskWindow()
	{
		base.WindowStyle = WindowStyle.None;
		base.AllowsTransparency = true;
		base.ShowInTaskbar = false;
		base.Topmost = true;
		base.ResizeMode = ResizeMode.NoResize;
		base.Focusable = false;
		base.Cursor = Cursors.None;
		base.Width = 160.0;
		base.Height = 160.0;
		SolidColorBrush background = (SolidColorBrush)(base.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)));
		base.Content = new Grid
		{
			Background = background,
			Cursor = Cursors.None
		};
		base.SourceInitialized += delegate
		{
			_hwnd = new WindowInteropHelper(this).Handle;
			int windowLong = GetWindowLong(_hwnd, -20);
			SetWindowLong(_hwnd, -20, windowLong | 0x8000000 | 0x80);
		};
		base.Loaded += delegate
		{
			Centrar();
			ReafirmarTopmost();
			_mantener.Start();
		};
		base.Closed += delegate
		{
			_mantener.Stop();
		};
		_mantener = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(250.0)
		};
		_mantener.Tick += delegate
		{
			ReafirmarTopmost();
		};
	}

	private void Centrar()
	{
		base.Left = SystemParameters.PrimaryScreenWidth / 2.0 - base.Width / 2.0;
		base.Top = SystemParameters.PrimaryScreenHeight / 2.0 - base.Height / 2.0;
	}

	private void ReafirmarTopmost()
	{
		if (_hwnd != IntPtr.Zero)
		{
			SetWindowPos(_hwnd, HWND_TOPMOST, 0, 0, 0, 0, 19u);
		}
	}

	[DllImport("user32.dll")]
	private static extern int GetWindowLong(nint hWnd, int nIndex);

	[DllImport("user32.dll")]
	private static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);

	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(nint hWnd, nint after, int X, int Y, int cx, int cy, uint flags);
}

