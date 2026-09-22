using System;
using System.Collections.Concurrent;
using System.Windows.Media.Imaging;

namespace DLuz.Helpers;

public static class IconHelperWpf
{
	private static readonly ConcurrentDictionary<string, BitmapImage?> _cache = new ConcurrentDictionary<string, BitmapImage>();

	public static BitmapImage? Get(string name)
	{
		return _cache.GetOrAdd(name, (string n) => Cargar("pack://application:,,,/Assets/Icons/" + n + ".png"));
	}

	public static BitmapImage? Logo()
	{
		return _cache.GetOrAdd("__logo", (string _) => Cargar("pack://application:,,,/Assets/logo.png"));
	}

	private static BitmapImage? Cargar(string packUri)
	{
		try
		{
			BitmapImage bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.UriSource = new Uri(packUri, UriKind.Absolute);
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.EndInit();
			bitmapImage.Freeze();
			return bitmapImage;
		}
		catch
		{
			return null;
		}
	}
}

