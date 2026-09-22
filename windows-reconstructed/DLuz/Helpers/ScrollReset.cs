using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace DLuz.Helpers;

public static class ScrollReset
{
	public static readonly DependencyProperty ResetOnShowProperty = DependencyProperty.RegisterAttached("ResetOnShow", typeof(bool), typeof(ScrollReset), new PropertyMetadata(false, OnResetOnShowChanged));

	public static bool GetResetOnShow(DependencyObject d)
	{
		return (bool)d.GetValue(ResetOnShowProperty);
	}

	public static void SetResetOnShow(DependencyObject d, bool value)
	{
		d.SetValue(ResetOnShowProperty, value);
	}

	private static void OnResetOnShowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		ScrollViewer sv = d as ScrollViewer;
		if (sv == null)
		{
			return;
		}
		object newValue = e.NewValue;
		if (!(newValue is bool) || !(bool)newValue)
		{
			return;
		}
		sv.Loaded += delegate
		{
			AlInicio(sv);
		};
		sv.IsVisibleChanged += delegate(object _, DependencyPropertyChangedEventArgs args)
		{
			object newValue2 = args.NewValue;
			if (newValue2 is bool && (bool)newValue2)
			{
				AlInicio(sv);
			}
		};
	}

	private static void AlInicio(ScrollViewer sv)
	{
		sv.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(Top));
		DispatcherTimer dispatcherTimer = new DispatcherTimer(DispatcherPriority.Background);
		dispatcherTimer.Interval = TimeSpan.FromMilliseconds(120.0);
		dispatcherTimer.Tick += delegate(object? s, EventArgs _)
		{
			((DispatcherTimer)s).Stop();
			Top();
		};
		dispatcherTimer.Start();
		void Top()
		{
			ScrollViewer obj = ((sv.ScrollableHeight > 0.0) ? sv : (AncestroScrollable(sv) ?? sv));
			obj.ScrollToTop();
			obj.ScrollToHome();
		}
	}

	private static ScrollViewer? AncestroScrollable(DependencyObject d)
	{
		for (DependencyObject parent = VisualTreeHelper.GetParent(d); parent != null; parent = VisualTreeHelper.GetParent(parent))
		{
			if (parent is ScrollViewer { ScrollableHeight: >0.0 } scrollViewer)
			{
				return scrollViewer;
			}
		}
		return null;
	}
}

