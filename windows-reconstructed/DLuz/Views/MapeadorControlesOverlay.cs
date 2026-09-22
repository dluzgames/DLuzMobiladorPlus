using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using DLuz.Mapper;
using DLuz;

namespace DLuz.Views;

public partial class MapeadorControlesOverlay : Window, IComponentConnector
{
	private struct RECT
	{
		public int Left;

		public int Top;

		public int Right;

		public int Bottom;
	}

	private struct POINT
	{
		public int X;

		public int Y;
	}

	private readonly KeymapConfig _keymap;

	private readonly int _devW;

	private readonly int _devH;

	private readonly Func<nint> _hwndProvider;

	private readonly ScrcpyManager _scrcpy;

	private readonly DispatcherTimer _track;

	private nint _hwnd;

	private int _fallos;

	private RECT _ultima;

	private bool _mostrarFps;

	private string _fpsTexto = "FPS: ...";

	private const int GWL_EXSTYLE = -20;

	private const int WS_EX_TRANSPARENT = 32;

	private const int WS_EX_LAYERED = 524288;

	private const int WS_EX_NOACTIVATE = 134217728;

	private static readonly nint HWND_TOPMOST = new IntPtr(-1);

	private static readonly nint HWND_NOTOPMOST = new IntPtr(-2);

	private const uint SWP_NOACTIVATE = 16u;

	private const uint SWP_NOMOVE = 2u;

	private const uint SWP_NOSIZE = 1u;

	public MapeadorControlesOverlay(KeymapConfig keymap, int devW, int devH, Func<nint> hwndProvider, double opacidad, ScrcpyManager scrcpy, bool mostrarFps)
	{
		InitializeComponent();
		_keymap = keymap;
		_devW = ((devW > 0) ? devW : 1080);
		_devH = ((devH > 0) ? devH : 2400);
		_hwndProvider = hwndProvider;
		_scrcpy = scrcpy;
		_mostrarFps = mostrarFps;
		base.Opacity = Math.Clamp(opacidad, 0.2, 1.0);
		base.SourceInitialized += delegate
		{
			_hwnd = new WindowInteropHelper(this).Handle;
			HacerClickThrough(_hwnd);
		};
		base.Loaded += delegate
		{
			_scrcpy.OnFpsUpdate += FpsAtualizado;
			Anclar();
			Dibujar();
			_track.Start();
		};
		Lienzo.SizeChanged += delegate
		{
			Dibujar();
		};
		base.Closed += delegate
		{
			_scrcpy.OnFpsUpdate -= FpsAtualizado;
			_track.Stop();
		};
		_track = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(300.0)
		};
		_track.Tick += delegate
		{
			Anclar();
		};
	}

	public void EstablecerFpsVisible(bool visible)
	{
		_mostrarFps = visible;
		Dibujar();
	}

	private void FpsAtualizado(string fps)
	{
		_fpsTexto = "FPS: " + fps;
		Dispatcher.BeginInvoke(new Action(() =>
		{
			if (_mostrarFps)
			{
				Dibujar();
			}
		}));
	}

	public void EstablecerOpacidad(double v)
	{
		base.Opacity = Math.Clamp(v, 0.2, 1.0);
	}

	private void Dibujar()
	{
		Lienzo.Children.Clear();
		double actualWidth = Lienzo.ActualWidth;
		double actualHeight = Lienzo.ActualHeight;
		if (actualWidth <= 0.0 || actualHeight <= 0.0)
		{
			return;
		}
		double num = Math.Min(actualWidth / (double)_devW, actualHeight / (double)_devH);
		double num2 = (actualWidth - (double)_devW * num) / 2.0;
		double num3 = (actualHeight - (double)_devH * num) / 2.0;
		Chip(TeclasJoystick(), num2 + (double)_keymap.Joystick.CenterX * num, num3 + (double)_keymap.Joystick.CenterY * num, grande: true, 36.0);
		Chip(Bonito(string.IsNullOrWhiteSpace(_keymap.ToggleKey) ? "F1" : _keymap.ToggleKey), num2 + (double)_keymap.Camera.ZoneX * num, num3 + (double)_keymap.Camera.ZoneY * num, grande: true, 36.0);
		foreach (ButtonConfig button in _keymap.Buttons)
		{
			if (!string.IsNullOrWhiteSpace(button.Key))
			{
				Chip(Bonito(button.Key), num2 + (double)button.X * num, num3 + (double)button.Y * num, grande: false, (button.Size > 0.0) ? button.Size : 54.0);
			}
		}
		if (_mostrarFps)
		{
			Border fps = new Border
			{
				Background = new SolidColorBrush(Color.FromArgb(210, 10, 18, 39)),
				BorderBrush = new SolidColorBrush(Color.FromRgb(58, 160, 255)),
				BorderThickness = new Thickness(1),
				CornerRadius = new CornerRadius(4),
				Padding = new Thickness(7, 3, 7, 3),
				Child = new TextBlock { Text = _fpsTexto, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White }
			};
			// FPS no canto inferior esquerdo, sem cobrir os controles principais.
			Canvas.SetLeft(fps, 8);
			Canvas.SetTop(fps, Math.Max(8, actualHeight - 30));
			Lienzo.Children.Add(fps);
		}
	}

	private string TeclasJoystick()
	{
		string[] array = new string[4] { "arriba", "abajo", "izquierda", "derecha" };
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, int[]> key in _keymap.Joystick.Keys)
		{
			int[] value = key.Value;
			if (value != null && value.Length >= 2)
			{
				string value2 = ((value[1] < 0) ? "arriba" : ((value[1] > 0) ? "abajo" : ((value[0] < 0) ? "izquierda" : "derecha")));
				list.Add($"{Array.IndexOf(array, value2)}{key.Key}");
			}
		}
		list.Sort(StringComparer.Ordinal);
		if (list.Count <= 0)
		{
			return "Joystick";
		}
		return string.Concat(list.ConvertAll((string t) => t.Substring(1)));
	}

	private void Chip(string texto, double cx, double cy, bool grande, double tam)
	{
		// Todos os atalhos ficam compactos e consistentes na tela, como no BlueStacks.
		double num = Math.Clamp(tam, 28.0, 38.0);
		Border element = new Border
		{
			Width = num,
			Height = num,
			Background = (Brush)FindResource(grande ? "Stex.OverlayMarkerCameraBrush" : "Stex.OverlayMarkerBrush"),
			BorderBrush = (Brush)FindResource("Stex.OverlayMarkerBorderBrush"),
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(num / 2.0),
			Child = new TextBlock
			{
				Text = texto,
				FontSize = 11.0,
				FontWeight = FontWeights.SemiBold,
				TextWrapping = TextWrapping.Wrap,
				TextAlignment = TextAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Foreground = (Brush)FindResource("Stex.OverlayMarkerTextBrush")
			}
		};
		Canvas.SetLeft(element, cx - num / 2.0);
		Canvas.SetTop(element, cy - num / 2.0);
		Lienzo.Children.Add(element);
	}

	private static string Bonito(string key)
	{
		switch (key)
		{
		case "mouse_left":
			return "Clic izq";
		case "mouse_right":
			return "Clic der";
		case "mouse_middle":
			return "Clic medio";
		case "mouse_x1":
			return "Lateral 1";
		case "mouse_x2":
			return "Lateral 2";
		case "Space":
			return "Espacio";
		case "Shift":
		case "LeftShift":
			return "Shift izq";
		default:
			return key;
		}
	}

	private void Anclar()
	{
		nint num = _hwndProvider();
		if (num == IntPtr.Zero || !IsWindow(num))
		{
			if (++_fallos >= 16)
			{
				Close();
			}
			return;
		}
		_fallos = 0;
		if (!GetClientRect(num, out var lpRect))
		{
			return;
		}
		POINT lpPoint = new POINT
		{
			X = 0,
			Y = 0
		};
		if (!ClientToScreen(num, ref lpPoint))
		{
			return;
		}
		int num2 = lpRect.Right - lpRect.Left;
		int num3 = lpRect.Bottom - lpRect.Top;
		if (num2 > 0 && num3 > 0 && _hwnd != IntPtr.Zero)
		{
			RECT ultima = new RECT
			{
				Left = lpPoint.X,
				Top = lpPoint.Y,
				Right = lpPoint.X + num2,
				Bottom = lpPoint.Y + num3
			};
			if (ultima.Left != _ultima.Left || ultima.Top != _ultima.Top || ultima.Right != _ultima.Right || ultima.Bottom != _ultima.Bottom)
			{
				_ultima = ultima;
				SetWindowPos(_hwnd, HWND_TOPMOST, lpPoint.X, lpPoint.Y, num2, num3, 16u);
			}
		}
	}

	private static void HacerClickThrough(nint hwnd)
	{
		if (hwnd != IntPtr.Zero)
		{
			int windowLong = GetWindowLong(hwnd, -20);
			SetWindowLong(hwnd, -20, windowLong | 0x20 | 0x80000 | 0x8000000);
		}
	}

	public void CederFrente(bool ceder)
	{
		if (ceder)
		{
			_track.Stop();
			base.Topmost = false;
			if (_hwnd != IntPtr.Zero)
			{
				SetWindowPos(_hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, 19u);
			}
		}
		else
		{
			base.Topmost = true;
			_ultima = default(RECT);
			Anclar();
			_track.Start();
		}
	}

	[DllImport("user32.dll")]
	private static extern bool GetClientRect(nint hWnd, out RECT lpRect);

	[DllImport("user32.dll")]
	private static extern bool ClientToScreen(nint hWnd, ref POINT lpPoint);

	[DllImport("user32.dll")]
	private static extern bool IsWindow(nint hWnd);

	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(nint hWnd, nint after, int X, int Y, int cx, int cy, uint flags);

	[DllImport("user32.dll")]
	private static extern int GetWindowLong(nint hWnd, int nIndex);

	[DllImport("user32.dll")]
	private static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);
}

