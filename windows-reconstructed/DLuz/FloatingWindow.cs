using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using DLuz.Services;
using Wpf.Ui.Controls;

namespace DLuz;

public class FloatingWindow : Window
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

	private readonly Action _onDetener;

	private readonly Action _onMostrarApp;

	private readonly bool _modoDebug;

	private readonly DispatcherTimer _timer;

	private System.Windows.Controls.TextBlock? _lblFps;

	private System.Windows.Controls.TextBlock? _lblFpsMini;

	private Action<string>? _fpsHandler;

	private FrameworkElement? _rootNormal;

	private FrameworkElement? _rootMini;

	private bool _modoMini;

	private const uint MONITOR_DEFAULTTONEAREST = 2u;

	[DllImport("user32.dll")]
	private static extern nint GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern bool GetWindowRect(nint hWnd, out RECT rect);

	[DllImport("user32.dll")]
	private static extern bool IsZoomed(nint hWnd);

	public FloatingWindow(ScrcpyManager scrcpy, string infoText, Action onDetener, Action onMostrarApp, bool printFps = false, bool modoDebug = false)
	{
		_scrcpy = scrcpy;
		_onDetener = onDetener;
		_onMostrarApp = onMostrarApp;
		_modoDebug = modoDebug;
		base.WindowStyle = WindowStyle.None;
		base.AllowsTransparency = true;
		base.ResizeMode = ResizeMode.NoResize;
		base.ShowInTaskbar = false;
		base.Topmost = true;
		base.Background = Brushes.Transparent;
		base.Opacity = 0.92;
		base.Width = 240.0;
		base.Height = (printFps ? 208 : 180);
		base.WindowStartupLocation = WindowStartupLocation.Manual;
		Rect workArea = SystemParameters.WorkArea;
		base.Left = workArea.Right - base.Width - 24.0;
		base.Top = workArea.Top + workArea.Height / 2.0 - base.Height / 2.0;
		_rootNormal = (FrameworkElement)BuildContent(infoText, printFps, modoDebug);
		_rootMini = BuildMini(infoText, printFps, modoDebug);
		_rootMini.Visibility = Visibility.Collapsed;
		base.Content = new Grid
		{
			Children = 
			{
				(UIElement)_rootNormal,
				(UIElement)_rootMini
			}
		};
		base.MouseLeftButtonDown += delegate
		{
			DragMove();
		};
		_timer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(500.0)
		};
		_timer.Tick += OnTick;
		_timer.Start();
		if (printFps && !modoDebug)
		{
			_fpsHandler = delegate(string fps)
			{
				base.Dispatcher.BeginInvoke((Action)delegate
				{
					if (_lblFps != null)
					{
						_lblFps.Text = "● " + fps;
					}
					if (_lblFpsMini != null)
					{
						_lblFpsMini.Text = fps;
					}
				});
			};
			_scrcpy.OnFpsUpdate += _fpsHandler;
		}
		base.Closed += delegate
		{
			_timer.Stop();
			if (_fpsHandler != null)
			{
				_scrcpy.OnFpsUpdate -= _fpsHandler;
				_fpsHandler = null;
			}
		};
	}

	private static Brush B(string key)
	{
		return DLuz.Helpers.ResourceHelper.GetBrush(key);
	}

	private UIElement BuildContent(string infoText, bool printFps, bool modoDebug)
	{
		StackPanel stackPanel = new StackPanel
		{
			Margin = new Thickness(12.0, 10.0, 12.0, 10.0)
		};
		if (printFps)
		{
			_lblFps = new System.Windows.Controls.TextBlock
			{
				Text = (modoDebug ? "● FPS en consola debug" : "● esperando fps..."),
				FontWeight = FontWeights.Bold,
				FontSize = 12.0,
				Foreground = B("Stex.AccentLightBrush"),
				Margin = new Thickness(0.0, 0.0, 0.0, 4.0)
			};
			stackPanel.Children.Add(_lblFps);
		}
		stackPanel.Children.Add(new System.Windows.Controls.TextBlock
		{
			Text = "▶  scrcpy corriendo",
			FontWeight = FontWeights.Bold,
			FontSize = 12.0,
			Foreground = B("Stex.AccentLightBrush")
		});
		stackPanel.Children.Add(new System.Windows.Controls.TextBlock
		{
			Text = infoText,
			FontSize = 11.0,
			TextWrapping = TextWrapping.Wrap,
			Foreground = B("Stex.TextLightBrush"),
			Margin = new Thickness(0.0, 4.0, 0.0, 8.0)
		});
		stackPanel.Children.Add(new Border
		{
			Height = 1.0,
			Background = B("Stex.AccentDarkBrush"),
			Margin = new Thickness(0.0, 0.0, 0.0, 8.0)
		});
		Wpf.Ui.Controls.Button btnDlss5 = new Wpf.Ui.Controls.Button
		{
			Content = DLSS5Service.Activo ? "✨ DLSS 5 AI: ATIVO" : "✨ DLSS 5: DESATIVADO",
			Appearance = DLSS5Service.Activo ? ControlAppearance.Primary : ControlAppearance.Secondary,
			Width = 208.0,
			Margin = new Thickness(0.0, 0.0, 0.0, 8.0)
		};
		btnDlss5.Click += delegate
		{
			bool estado = DLSS5Service.Alternar();
			btnDlss5.Content = estado ? "✨ DLSS 5 AI: ATIVO" : "✨ DLSS 5: DESATIVADO";
			btnDlss5.Appearance = estado ? ControlAppearance.Primary : ControlAppearance.Secondary;
			ToastService.Mostrar(estado ? "🚀 DLSS 5 Ultra AI Ativado no Espelhamento!" : "DLSS 5 Desativado.", estado ? ToastTipo.Exito : ToastTipo.Info, 2200);
		};
		stackPanel.Children.Add(btnDlss5);
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal
		};
		Wpf.Ui.Controls.Button button = new Wpf.Ui.Controls.Button
		{
			Content = "⏹ Detener",
			Appearance = ControlAppearance.Danger,
			Width = 100.0,
			Margin = new Thickness(0.0, 0.0, 8.0, 0.0)
		};
		button.Click += delegate
		{
			_timer.Stop();
			_onDetener?.Invoke();
			Close();
		};
		Wpf.Ui.Controls.Button button2 = new Wpf.Ui.Controls.Button
		{
			Content = "↑ Mostrar",
			Width = 100.0
		};
		button2.Click += delegate
		{
			_onMostrarApp?.Invoke();
			Hide();
		};
		stackPanel2.Children.Add(button);
		stackPanel2.Children.Add(button2);
		stackPanel.Children.Add(stackPanel2);
		return new Border
		{
			Background = B("Stex.BgPrimaryBrush"),
			BorderBrush = B("Stex.AccentDarkBrush"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(8.0),
			Child = stackPanel
		};
	}

	private FrameworkElement BuildMini(string infoText, bool printFps, bool modoDebug)
	{
		StackPanel obj = new StackPanel
		{
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Top
		};
		StackPanel stackPanel = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			HorizontalAlignment = HorizontalAlignment.Right
		};
		if (printFps)
		{
			_lblFpsMini = new System.Windows.Controls.TextBlock
			{
				Text = (modoDebug ? "FPS en consola" : "..."),
				FontWeight = FontWeights.Bold,
				FontSize = 18.0,
				Foreground = B("Stex.AccentLightBrush"),
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(0.0, 0.0, 10.0, 0.0),
				Effect = new DropShadowEffect
				{
					ShadowDepth = 0.0,
					BlurRadius = 6.0,
					Color = Colors.Black,
					Opacity = 0.85
				}
			};
			stackPanel.Children.Add(_lblFpsMini);
		}
		Wpf.Ui.Controls.Button button = new Wpf.Ui.Controls.Button
		{
			Content = "⏹",
			Appearance = ControlAppearance.Danger,
			Width = 34.0,
			Height = 34.0,
			Padding = new Thickness(0.0),
			FontSize = 14.0,
			Opacity = 0.85,
			Margin = new Thickness(0.0, 0.0, 6.0, 0.0),
			ToolTip = "Detener la sesión"
		};
		button.Click += delegate
		{
			_timer.Stop();
			_onDetener?.Invoke();
			Close();
		};
		Wpf.Ui.Controls.Button button2 = new Wpf.Ui.Controls.Button
		{
			Content = "↑",
			Width = 34.0,
			Height = 34.0,
			Padding = new Thickness(0.0),
			FontSize = 14.0,
			Opacity = 0.85,
			ToolTip = "Mostrar launcher"
		};
		button2.Click += delegate
		{
			_onMostrarApp?.Invoke();
			Hide();
		};
		stackPanel.Children.Add(button);
		stackPanel.Children.Add(button2);
		obj.Children.Add(stackPanel);
		obj.Children.Add(new System.Windows.Controls.TextBlock
		{
			Text = infoText,
			FontSize = 11.0,
			TextWrapping = TextWrapping.Wrap,
			TextAlignment = TextAlignment.Right,
			MaxWidth = 240.0,
			Foreground = B("Stex.TextLightBrush"),
			Opacity = 0.7,
			Margin = new Thickness(0.0, 5.0, 0.0, 0.0),
			Effect = new DropShadowEffect
			{
				ShadowDepth = 0.0,
				BlurRadius = 5.0,
				Color = Colors.Black,
				Opacity = 0.85
			}
		});
		return obj;
	}

	private void OnTick(object? sender, EventArgs e)
	{
		if (!_scrcpy.EstaCorriendo)
		{
			_timer.Stop();
			if (!_modoDebug)
			{
				_onDetener?.Invoke();
			}
			else
			{
				_onMostrarApp?.Invoke();
			}
			Close();
			return;
		}
		nint foregroundWindow = GetForegroundWindow();
		bool num = foregroundWindow != IntPtr.Zero && foregroundWindow == new WindowInteropHelper(this).Handle;
		bool flag = _scrcpy.EsVentanaDeSesion(foregroundWindow);
		if (!num && !flag)
		{
			if (base.IsVisible)
			{
				Hide();
			}
			return;
		}
		nint num2 = _scrcpy.ObtenerHandleVentana();
		if (num2 != IntPtr.Zero)
		{
			_modoMini = IsZoomed(num2) || EsPantallaCompleta(num2);
		}
		if (_rootNormal != null && _rootMini != null)
		{
			_rootNormal.Visibility = (_modoMini ? Visibility.Collapsed : Visibility.Visible);
			_rootMini.Visibility = ((!_modoMini) ? Visibility.Collapsed : Visibility.Visible);
		}
		if (!base.IsVisible)
		{
			Show();
		}
	}

	private static bool EsPantallaCompleta(nint hwnd)
	{
		if (hwnd == IntPtr.Zero)
		{
			return false;
		}
		if (!GetWindowRect(hwnd, out var rect))
		{
			return false;
		}
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
		if (rect.Left <= lpmi.rcMonitor.Left && rect.Top <= lpmi.rcMonitor.Top && rect.Right >= lpmi.rcMonitor.Right)
		{
			return rect.Bottom >= lpmi.rcMonitor.Bottom;
		}
		return false;
	}

	[DllImport("user32.dll")]
	private static extern nint MonitorFromWindow(nint hwnd, uint dwFlags);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFO lpmi);
}


