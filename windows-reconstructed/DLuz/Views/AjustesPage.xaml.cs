using System.Windows;
using System.Windows.Controls;
using DLuz.Services;
using DLuz.ViewModels;
using Wpf.Ui.Controls;

namespace DLuz.Views;

public partial class AjustesPage : Page
{
	public AjustesPage()
	{
		DataContext = AjustesViewModel.Instance;
		InitializeComponent();

		Loaded += (_, _) =>
		{
			AtualizarBotoesTema(ThemeService.Instance.IsDarkTheme);
		};

		ThemeService.Instance.ThemeChanged += isDark =>
		{
			Dispatcher.Invoke(() => AtualizarBotoesTema(isDark));
		};
	}

	private void AtualizarBotoesTema(bool isDark)
	{
		if (BtnTemaEscuro != null && BtnTemaClaro != null)
		{
			BtnTemaEscuro.Appearance = isDark ? ControlAppearance.Primary : ControlAppearance.Secondary;
			BtnTemaClaro.Appearance = isDark ? ControlAppearance.Secondary : ControlAppearance.Primary;
		}
	}

	private void BtnTemaEscuro_Click(object sender, RoutedEventArgs e)
	{
		ThemeService.Instance.ApplyTheme(isDark: true, persist: true);
	}

	private void BtnTemaClaro_Click(object sender, RoutedEventArgs e)
	{
		ThemeService.Instance.ApplyTheme(isDark: false, persist: true);
	}
}
