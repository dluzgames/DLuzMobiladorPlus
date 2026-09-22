using System.Windows.Controls;
using DLuz.ViewModels;

namespace DLuz.Views;

public partial class AjustesPage : Page
{
	public AjustesPage()
	{
		DataContext = AjustesViewModel.Instance;
		InitializeComponent();
	}
}
