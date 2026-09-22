using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using DLuz.Services;
using Wpf.Ui.Controls;

namespace DLuz.Views;

public partial class LicencaWindow : FluentWindow
{
	public LicencaWindow()
	{
		InitializeComponent();
	}

	private void BtnCopiarHwid_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Clipboard.SetText(TxtHwid.Text);
			ToastService.Mostrar("HWID copiado para a área de transferência!", ToastTipo.Exito);
		}
		catch { }
	}

	private async void BtnAtivar_Click(object sender, RoutedEventArgs e)
	{
		string chave = TxtChave.Text?.Trim() ?? "";
		if (string.IsNullOrEmpty(chave))
		{
			TxtMsgResultado.Text = "Por favor, digite a sua chave de ativação.";
			TxtMsgResultado.Foreground = new SolidColorBrush(Color.FromRgb(255, 166, 87));
			TxtMsgResultado.Visibility = Visibility.Visible;
			return;
		}

		TxtMsgResultado.Text = "Verificando chave no servidor...";
		TxtMsgResultado.Foreground = new SolidColorBrush(Color.FromRgb(88, 166, 255));
		TxtMsgResultado.Visibility = Visibility.Visible;

		var (ok, msg) = await LicenseService.Instance.AtivarChaveAsync(chave);

		if (ok)
		{
			TxtMsgResultado.Text = "✓ " + msg;
			TxtMsgResultado.Foreground = new SolidColorBrush(Color.FromRgb(56, 239, 125));
			ToastService.Mostrar("Licença Pro ativada com sucesso!", ToastTipo.Exito);
		}
		else
		{
			TxtMsgResultado.Text = "❌ " + msg;
			TxtMsgResultado.Foreground = new SolidColorBrush(Color.FromRgb(255, 42, 58));
		}
	}

	private void BtnComprar_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			// Link da sua página de vendas na Kiwify / Hotmart / WhatsApp
			Process.Start(new ProcessStartInfo
			{
				FileName = "https://loja.dluz.com.br/produto/dluzmobilador-pro/",
				UseShellExecute = true
			});
		}
		catch { }
	}

	private void BtnMembroYoutube_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = "https://www.youtube.com/@dluzgames/join",
				UseShellExecute = true
			});
		}
		catch { }
	}

	private void TxtChave_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
	{
		if (TxtMsgResultado.Visibility == Visibility.Visible)
		{
			TxtMsgResultado.Visibility = Visibility.Collapsed;
		}
	}
}

