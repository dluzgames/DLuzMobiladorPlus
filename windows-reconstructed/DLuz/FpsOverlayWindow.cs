using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace DLuz;

public class FpsOverlayWindow : Window
{
	private struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	private struct MONITORINFO
	{
		public int cbSize;

		public RECT rcMonitor;

		public RECT rcWork;

		public uint dwFlags;
	}

	private readonly ScrcpyManager _scrcpy;

	private readonly int _esquina;

	private readonly DispatcherTimer _timer;

	private readonly Action<string> _fpsHandler;

	private readonly TextBlock _texto;

	private readonly double _fontBase;

	private const string TextoSinDato = "FPS ---";

	private const uint MONITOR_DEFAULTTONEAREST = 2u;

	private const int GWL_EXSTYLE = -20;

	private const int WS_EX_TRANSPARENT = 32;

	private const int WS_EX_LAYERED = 524288;

	private const int WS_EX_NOACTIVATE = 134217728;

	private const int WS_EX_TOOLWINDOW = 128;

	public FpsOverlayWindow(ScrcpyManager scrcpy, int esquina)
	{
		_scrcpy = scrcpy;
		_esquina = esquina;
		base.WindowStyle = WindowStyle.None;
		base.AllowsTransparency = true;
		base.Background = Brushes.Transparent;
		base.Topmost = true;
		base.ShowInTaskbar = false;
		base.ResizeMode = ResizeMode.NoResize;
		base.ShowActivated = false;
		base.SizeToContent = SizeToContent.WidthAndHeight;
		base.WindowStartupLocation = WindowStartupLocation.Manual;
		base.Left = -4000.0;
		base.Top = -4000.0;
		_fontBase = (double)Application.Current.Resources["Stex.OverlayFpsFontSize"];
		_texto = new TextBlock
		{
			Text = "FPS ---",
			FontFamily = (FontFamily)Application.Current.Resources["Stex.OverlayFpsFontFamily"],
			FontSize = _fontBase,
			FontWeight = FontWeights.Bold,
			Foreground = (Brush)Application.Current.Resources["Stex.OverlayFpsTextoBrush"]
		};
		base.Content = new Border
		{
			Background = (Brush)Application.Current.Resources["Stex.OverlayFpsFondoBrush"],
			CornerRadius = new CornerRadius(0.0),
			Padding = new Thickness(10.0, 4.0, 10.0, 4.0),
			Child = _texto
		};
		_fpsHandler = delegate(string fps)
		{
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				_texto.Text = ComponerTexto(fps);
			});
		};
		_scrcpy.OnFpsUpdate += _fpsHandler;
		_timer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(250.0)
		};
		_timer.Tick += delegate
		{
			Reposicionar();
		};
		_timer.Start();
		base.SizeChanged += delegate
		{
			Reposicionar();
		};
		base.Closed += delegate
		{
			_timer.Stop();
			_scrcpy.OnFpsUpdate -= _fpsHandler;
		};
	}

	protected override void OnSourceInitialized(EventArgs e)
	{
		base.OnSourceInitialized(e);
		nint handle = new WindowInteropHelper(this).Handle;
		int windowLong = GetWindowLong(handle, -20);
		SetWindowLong(handle, -20, windowLong | 0x20 | 0x80000 | 0x8000000 | 0x80);
	}

	private void Reposicionar()
	{
		nint num = _scrcpy.ObtenerHandleVentana();
		if (num == IntPtr.Zero || !GetWindowRect(num, out var lpRect) || lpRect.Right - lpRect.Left <= 0 || lpRect.Left <= -30000)
		{
			if (base.Visibility == Visibility.Visible)
			{
				Hide();
			}
			return;
		}
		if (!_scrcpy.EsVentanaDeSesion(GetForegroundWindow()))
		{
			if (base.Visibility == Visibility.Visible)
			{
				Hide();
			}
			return;
		}
		bool flag = IsZoomed(num) || EsPantallaCompleta(num, lpRect);
		double num2 = _fontBase * (flag ? 1.5 : 1.0);
		if (Math.Abs(_texto.FontSize - num2) > 0.1)
		{
			_texto.FontSize = num2;
		}
		double num3 = EscalaDpi();
		double actualWidth = base.ActualWidth;
		double actualHeight = base.ActualHeight;
		double num4 = (double)lpRect.Left / num3;
		double num5 = (double)lpRect.Right / num3;
		double num6 = (double)lpRect.Top / num3;
		double num7 = (double)lpRect.Bottom / num3;
		int esquina = _esquina;
		double num8 = ((esquina != 1 && esquina != 3) ? (num4 + 12.0) : (num5 - actualWidth - 12.0));
		double left = num8;
		esquina = _esquina;
		num8 = (((uint)(esquina - 2) > 1u) ? (num6 + 12.0) : (num7 - actualHeight - 12.0));
		double top = num8;
		base.Left = left;
		base.Top = top;
		if (base.Visibility != Visibility.Visible)
		{
			Show();
		}
	}

	private double EscalaDpi()
	{
		return PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
	}

	private static string ComponerTexto(string fps)
	{
		Match match = Regex.Match(fps, "\\d+");
		if (!match.Success)
		{
			return "FPS ---";
		}
		return "FPS " + match.Value.PadLeft(3);
	}

	public void Cerrar()
	{
		try
		{
			Close();
		}
		catch
		{
		}
	}

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

	[DllImport("user32.dll")]
	private static extern nint GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern bool IsZoomed(nint hWnd);

	[DllImport("user32.dll")]
	private static extern nint MonitorFromWindow(nint hwnd, uint dwFlags);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFO lpmi);

	private static bool EsPantallaCompleta(nint hwnd, RECT ventana)
	{
		nint num = MonitorFromWindow(hwnd, 2u);
		if (num == IntPtr.Zero)
		{
			return false;
		}
		MONITORINFO lpmi = new MONITORINFO
		{
			cbSize = Marshal.SizeOf<MONITORINFO>()
		};
		if (!GetMonitorInfo(num, ref lpmi))
		{
			return false;
		}
		if (ventana.Left <= lpmi.rcMonitor.Left && ventana.Top <= lpmi.rcMonitor.Top && ventana.Right >= lpmi.rcMonitor.Right)
		{
			return ventana.Bottom >= lpmi.rcMonitor.Bottom;
		}
		return false;
	}

	[DllImport("user32.dll")]
	private static extern int GetWindowLong(nint hWnd, int nIndex);

	[DllImport("user32.dll")]
	private static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);
}

