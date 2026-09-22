using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Threading;
using DLuz.Mapper;

namespace DLuz.Views;

public partial class MapeadorCursorOverlay : Window, IComponentConnector
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

	private readonly MapeadorEngine _engine;

	private readonly Func<nint> _hwndProvider;

	private readonly DispatcherTimer _track;

	private nint _hwnd;

	private int _fallos;

	private RECT _ultima;

	private bool _arrastrando;

	private const int GWL_EXSTYLE = -20;

	private const int WS_EX_NOACTIVATE = 134217728;

	private const int WS_EX_TRANSPARENT = 32;

	private static readonly nint HWND_TOPMOST = new IntPtr(-1);

	private static readonly nint HWND_NOTOPMOST = new IntPtr(-2);

	private const uint SWP_NOACTIVATE = 16u;

	private const uint SWP_NOMOVE = 2u;

	private const uint SWP_NOSIZE = 1u;

	public MapeadorCursorOverlay(MapeadorEngine engine, Func<nint> hwndProvider)
	{
		InitializeComponent();
		_engine = engine;
		_hwndProvider = hwndProvider;
		_track = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(200.0)
		};
		_track.Tick += delegate
		{
			Anclar();
		};
		base.SourceInitialized += delegate
		{
			_hwnd = new WindowInteropHelper(this).Handle;
			HacerNoActivable(_hwnd);
		};
		base.Loaded += delegate
		{
			Anclar();
			_track.Start();
		};
		base.Closed += delegate
		{
			_track.Stop();
			SoltarToque();
		};
		base.IsVisibleChanged += delegate(object _, DependencyPropertyChangedEventArgs e)
		{
			object newValue = e.NewValue;
			if (newValue is bool && !(bool)newValue)
			{
				SoltarToque();
			}
		};
		Raiz.MouseLeftButtonDown += AlPresionar;
		Raiz.MouseMove += AlMover;
		Raiz.MouseLeftButtonUp += AlSoltar;
		Raiz.LostMouseCapture += delegate
		{
			SoltarToque();
		};
	}

	public void OcultarYSoltar()
	{
		SoltarToque();
		if (base.IsVisible)
		{
			Hide();
		}
	}

	public void EstablecerClickThrough(bool clickThrough)
	{
		if (_hwnd != IntPtr.Zero)
		{
			int windowLong = GetWindowLong(_hwnd, -20);
			int num = (clickThrough ? (windowLong | 0x20) : (windowLong & -33));
			if (num != windowLong)
			{
				SetWindowLong(_hwnd, -20, num);
			}
		}
	}

	private void AlPresionar(object sender, MouseButtonEventArgs e)
	{
		(int, int)? tuple = MapearADispositivo(e.GetPosition(Raiz));
		if (tuple.HasValue)
		{
			_arrastrando = true;
			Raiz.CaptureMouse();
			_engine.CursorTocar(tuple.Value.Item1, tuple.Value.Item2);
			e.Handled = true;
		}
	}

	private void AlMover(object sender, MouseEventArgs e)
	{
		if (_arrastrando)
		{
			(int, int)? tuple = MapearADispositivo(e.GetPosition(Raiz));
			if (tuple.HasValue)
			{
				_engine.CursorTocar(tuple.Value.Item1, tuple.Value.Item2);
			}
		}
	}

	private void AlSoltar(object sender, MouseButtonEventArgs e)
	{
		if (_arrastrando)
		{
			SoltarToque();
			e.Handled = true;
		}
	}

	private void SoltarToque()
	{
		if (_arrastrando)
		{
			_arrastrando = false;
			if (Raiz.IsMouseCaptured)
			{
				Raiz.ReleaseMouseCapture();
			}
		}
		_engine.CursorSoltar();
	}

	private (int x, int y)? MapearADispositivo(Point p)
	{
		double actualWidth = Raiz.ActualWidth;
		double actualHeight = Raiz.ActualHeight;
		if (actualWidth <= 0.0 || actualHeight <= 0.0 || _engine.Ancho <= 0 || _engine.Alto <= 0)
		{
			return null;
		}
		double num = Math.Min(actualWidth / (double)_engine.Ancho, actualHeight / (double)_engine.Alto);
		if (num <= 0.0)
		{
			return null;
		}
		double num2 = (actualWidth - (double)_engine.Ancho * num) / 2.0;
		double num3 = (actualHeight - (double)_engine.Alto * num) / 2.0;
		int value = (int)Math.Round((p.X - num2) / num);
		return new ValueTuple<int, int>(item2: Math.Clamp((int)Math.Round((p.Y - num3) / num), 0, _engine.Alto - 1), item1: Math.Clamp(value, 0, _engine.Ancho - 1));
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

	private static void HacerNoActivable(nint hwnd)
	{
		if (hwnd != IntPtr.Zero)
		{
			int windowLong = GetWindowLong(hwnd, -20);
			SetWindowLong(hwnd, -20, windowLong | 0x8000000);
		}
	}

	public void CederFrente(bool ceder)
	{
		if (ceder)
		{
			SoltarToque();
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

