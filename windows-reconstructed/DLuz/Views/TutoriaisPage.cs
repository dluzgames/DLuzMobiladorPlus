using System.Windows.Controls;
using DLuz.ViewModels;

namespace DLuz.Views;

public partial class TutoriaisPage : Page
{
	private readonly TutoriaisViewModel _vm;

	public TutoriaisPage()
	{
		InitializeComponent();
		_vm = new TutoriaisViewModel();
		DataContext = _vm;
	}
}
