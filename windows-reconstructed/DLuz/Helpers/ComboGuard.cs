using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace DLuz.Helpers;

public static class ComboGuard
{
	public static readonly DependencyProperty ProtegerAperturaProperty = DependencyProperty.RegisterAttached("ProtegerApertura", typeof(bool), typeof(ComboGuard), new PropertyMetadata(false, OnProtegerAperturaChanged));

	private static readonly DependencyProperty ArmadoProperty = DependencyProperty.RegisterAttached("Armado", typeof(bool), typeof(ComboGuard), new PropertyMetadata(false));

	public static void SetProtegerApertura(DependencyObject o, bool v)
	{
		o.SetValue(ProtegerAperturaProperty, v);
	}

	public static bool GetProtegerApertura(DependencyObject o)
	{
		return (bool)o.GetValue(ProtegerAperturaProperty);
	}

	private static void OnProtegerAperturaChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
	{
		if (o is ComboBox comboBox)
		{
			if ((bool)e.NewValue)
			{
				comboBox.DropDownOpened += OnOpened;
				comboBox.DropDownClosed += OnClosed;
				comboBox.AddHandler(UIElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(OnPreviewUp), handledEventsToo: true);
			}
			else
			{
				comboBox.DropDownOpened -= OnOpened;
				comboBox.DropDownClosed -= OnClosed;
				comboBox.RemoveHandler(UIElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(OnPreviewUp));
			}
		}
	}

	private static void OnOpened(object? sender, EventArgs e)
	{
		if (sender is ComboBox comboBox)
		{
			comboBox.SetValue(ArmadoProperty, Mouse.LeftButton == MouseButtonState.Pressed);
		}
	}

	private static void OnClosed(object? sender, EventArgs e)
	{
		if (sender is ComboBox comboBox)
		{
			comboBox.SetValue(ArmadoProperty, false);
		}
	}

	private static void OnPreviewUp(object sender, MouseButtonEventArgs e)
	{
		if (sender is ComboBox comboBox && (bool)comboBox.GetValue(ArmadoProperty))
		{
			comboBox.SetValue(ArmadoProperty, false);
			if (e.OriginalSource is DependencyObject o && DentroDeItem(o))
			{
				e.Handled = true;
			}
		}
	}

	private static bool DentroDeItem(DependencyObject o)
	{
		DependencyObject dependencyObject = o;
		while (dependencyObject != null)
		{
			if (dependencyObject is ComboBoxItem)
			{
				return true;
			}
			bool flag = ((dependencyObject is Visual || dependencyObject is Visual3D) ? true : false);
			dependencyObject = (flag ? VisualTreeHelper.GetParent(dependencyObject) : (dependencyObject as FrameworkElement)?.Parent);
		}
		return false;
	}
}

