using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using DLuz.Helpers;
using Wpf.Ui.Controls;

namespace DLuz;

public class FloatingWindowOtg : Window
{
	private readonly ScrcpyManager _scrcpy;

	private readonly Action _onDetener;

	private readonly Action _onMostrarApp;

	private readonly DispatcherTimer _timer;

	[DllImport("user32.dll")]
	private static extern nint GetForegroundWindow();

	public FloatingWindowOtg(ScrcpyManager scrcpy, string serial, string shortcutMod, Action onDetener, Action onMostrarApp)
	{
		_scrcpy = scrcpy;
		_onDetener = onDetener;
		_onMostrarApp = onMostrarApp;
		base.WindowStyle = WindowStyle.None;
		base.AllowsTransparency = true;
		base.ResizeMode = ResizeMode.NoResize;
		base.ShowInTaskbar = false;
		base.Topmost = true;
		base.Background = Brushes.Transparent;
		base.Opacity = 0.92;
		base.Width = 280.0;
		base.Height = 168.0;
		base.WindowStartupLocation = WindowStartupLocation.Manual;
		Rect workArea = SystemParameters.WorkArea;
		base.Left = workArea.Right - base.Width - 24.0;
		base.Top = workArea.Top + workArea.Height / 2.0 - base.Height / 2.0;
		base.Content = BuildContent(serial, shortcutMod);
		base.MouseLeftButtonDown += delegate
		{
			DragMove();
		};
		_timer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(500.0)
		};
		_timer.Tick += delegate
		{
			if (!_scrcpy.EstaCorriendo)
			{
				_timer.Stop();
				_onDetener?.Invoke();
				Close();
			}
			else
			{
				nint foregroundWindow = GetForegroundWindow();
				if ((foregroundWindow != IntPtr.Zero && foregroundWindow == new WindowInteropHelper(this).Handle) || _scrcpy.EsVentanaDeSesion(foregroundWindow))
				{
					if (!base.IsVisible)
					{
						Show();
					}
				}
				else if (base.IsVisible)
				{
					Hide();
				}
			}
		};
		_timer.Start();
		base.Closed += delegate
		{
			_timer.Stop();
		};
	}

	private static Brush B(string key)
	{
		return DLuz.Helpers.ResourceHelper.GetBrush(key);
	}

	private UIElement BuildContent(string serial, string shortcutMod)
	{
		StackPanel stackPanel = new StackPanel
		{
			Margin = new Thickness(12.0, 10.0, 12.0, 10.0)
		};
		stackPanel.Children.Add(new System.Windows.Controls.TextBlock
		{
			Text = "DLuzMObi v2 (modo OTG)",
			FontWeight = FontWeights.Bold,
			FontSize = 12.0,
			Foreground = B("Stex.AccentLightBrush")
		});
		stackPanel.Children.Add(new System.Windows.Controls.TextBlock
		{
			Text = "⚡ OTG Activo",
			FontWeight = FontWeights.Bold,
			FontSize = 12.0,
			Foreground = B("Stex.SuccessOtgBrush"),
			Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
		});
		stackPanel.Children.Add(new System.Windows.Controls.TextBlock
		{
			Text = (string.IsNullOrWhiteSpace(serial) ? "Dispositivo: detección automática" : ("Dispositivo: " + serial)),
			FontSize = 11.0,
			Foreground = B("Stex.TextLightBrush"),
			Margin = new Thickness(0.0, 6.0, 0.0, 0.0)
		});
		stackPanel.Children.Add(new System.Windows.Controls.TextBlock
		{
			Text = "Tecla MOD: " + TeclaMod.NombreBonito(shortcutMod),
			FontSize = 11.0,
			Foreground = B("Stex.AccentMutedBrush"),
			Margin = new Thickness(0.0, 2.0, 0.0, 8.0)
		});
		stackPanel.Children.Add(new Border
		{
			Height = 1.0,
			Background = B("Stex.AccentDarkBrush"),
			Margin = new Thickness(0.0, 0.0, 0.0, 8.0)
		});
		StackPanel stackPanel2 = new StackPanel
		{
			Orientation = Orientation.Horizontal
		};
		Wpf.Ui.Controls.Button button = new Wpf.Ui.Controls.Button
		{
			Content = "⏹ Detener",
			Appearance = ControlAppearance.Danger,
			Width = 120.0,
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
			Content = "↑ Mostrar launcher",
			Width = 130.0
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
}


