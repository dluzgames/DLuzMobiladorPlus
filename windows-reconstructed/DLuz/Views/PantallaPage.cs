using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Markup;
using DLuz.ViewModels;

namespace DLuz.Views;

public partial class PantallaPage : Page, IComponentConnector
{
	private readonly PantallaViewModel _vm;

	public PantallaPage()
	{
		InitializeComponent();
		_vm = new PantallaViewModel();
		base.DataContext = _vm;
		base.Unloaded += delegate
		{
			_vm.Dispose();
		};
	}

}

