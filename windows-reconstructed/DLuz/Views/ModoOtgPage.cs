using System.Windows.Controls;
using DLuz.ViewModels;

namespace DLuz.Views;

public partial class ModoOtgPage : Page
{
	private readonly ConexionViewModel _vm;

	public ModoOtgPage()
	{
		InitializeComponent();
		_vm = new ConexionViewModel();
		DataContext = _vm;
		Unloaded += delegate
		{
			_vm.Dispose();
		};
	}
}
