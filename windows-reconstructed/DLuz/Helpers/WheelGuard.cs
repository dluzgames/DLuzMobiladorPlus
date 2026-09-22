using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Wpf.Ui.Controls;

namespace DLuz.Helpers;

public static class WheelGuard
{
	public static void Register()
	{
		EventManager.RegisterClassHandler(typeof(ScrollViewer), UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(OnScrollViewerWheel), handledEventsToo: true);
		EventManager.RegisterClassHandler(typeof(ComboBox), UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(OnComboWheel), handledEventsToo: true);
		EventManager.RegisterClassHandler(typeof(NumberBox), UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(OnNumberBoxWheel), handledEventsToo: true);
		EventManager.RegisterClassHandler(typeof(Slider), UIElement.PreviewMouseWheelEvent, new MouseWheelEventHandler(OnAlwaysForward), handledEventsToo: true);
	}

	private static void OnScrollViewerWheel(object sender, MouseWheelEventArgs e)
	{
		if (!e.Handled)
		{
			ScrollViewer scrollViewer = (ScrollViewer)sender;
			if (!(scrollViewer.ScrollableHeight <= 0.0) && !EnNumberBoxEnfocado(e.OriginalSource as DependencyObject, scrollViewer))
			{
				scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - (double)e.Delta);
				e.Handled = true;
			}
		}
	}

	private static void OnComboWheel(object sender, MouseWheelEventArgs e)
	{
		if (!(sender is ComboBox { IsDropDownOpen: not false }))
		{
			Bloquear(e);
		}
	}

	private static void OnNumberBoxWheel(object sender, MouseWheelEventArgs e)
	{
		if (!(sender is UIElement { IsKeyboardFocusWithin: not false }))
		{
			Bloquear(e);
		}
	}

	private static void OnAlwaysForward(object sender, MouseWheelEventArgs e)
	{
		Bloquear(e);
	}

	private static void Bloquear(MouseWheelEventArgs e)
	{
		if (!e.Handled)
		{
			e.Handled = true;
		}
	}

	private static bool EnNumberBoxEnfocado(DependencyObject? src, DependencyObject stop)
	{
		DependencyObject dependencyObject = src;
		while (dependencyObject != null && dependencyObject != stop)
		{
			if (dependencyObject is NumberBox { IsKeyboardFocusWithin: not false })
			{
				return true;
			}
			dependencyObject = Padre(dependencyObject);
		}
		return false;
	}

	private static DependencyObject? Padre(DependencyObject d)
	{
		if ((d is Visual || d is Visual3D) ? true : false)
		{
			return VisualTreeHelper.GetParent(d);
		}
		if (d is FrameworkContentElement { Parent: not null } frameworkContentElement)
		{
			return frameworkContentElement.Parent;
		}
		return LogicalTreeHelper.GetParent(d);
	}
}

