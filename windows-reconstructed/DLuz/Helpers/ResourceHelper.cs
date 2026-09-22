using System;
using System.Windows;
using System.Windows.Media;

namespace DLuz.Helpers;

public static class ResourceHelper
{
	public static Brush GetBrush(string key, Brush? fallback = null)
	{
		try
		{
			if (Application.Current != null)
			{
				if (Application.Current.TryFindResource(key) is Brush b)
				{
					return b;
				}
				if (Application.Current.Resources.Contains(key) && Application.Current.Resources[key] is Brush b2)
				{
					return b2;
				}
			}
		}
		catch
		{
		}
		return fallback ?? Brushes.Transparent;
	}
}

