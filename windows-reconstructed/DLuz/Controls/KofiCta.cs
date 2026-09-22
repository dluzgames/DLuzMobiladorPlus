using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;

namespace DLuz.Controls;

public partial class KofiCta : UserControl, IComponentConnector
{
	public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(KofiCta), new PropertyMetadata("", OnTextChanged));

	public string Text
	{
		get
		{
			return (string)GetValue(TextProperty);
		}
		set
		{
			SetValue(TextProperty, value);
		}
	}

	public KofiCta()
	{
		InitializeComponent();
	}

	private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		((KofiCta)d).Reconstruir((string)e.NewValue);
	}

	private void Reconstruir(string texto)
	{
		Host.Inlines.Clear();
		if (texto == null)
		{
			texto = "";
		}
		int num = texto.IndexOf("Ko-fi", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			texto += " Ko-fi";
			num = texto.IndexOf("Ko-fi", StringComparison.OrdinalIgnoreCase);
		}
		if (num > 0)
		{
			Host.Inlines.Add(new Run(texto.Substring(0, num)));
		}
		Hyperlink hyperlink = new Hyperlink(new Run(texto.Substring(num, "Ko-fi".Length)))
		{
			Foreground = (Brush)Application.Current.Resources["Stex.KofiAccentBrush"],
			TextDecorations = null
		};
		hyperlink.Click += delegate
		{
			try
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = "https://ko-fi.com/s/f0483f1cf2",
					UseShellExecute = true
				});
			}
			catch
			{
			}
		};
		Host.Inlines.Add(hyperlink);
		int num2 = num + "Ko-fi".Length;
		if (num2 < texto.Length)
		{
			Host.Inlines.Add(new Run(texto.Substring(num2)));
		}
	}
}

