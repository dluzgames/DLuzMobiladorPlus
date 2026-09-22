using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Markup;
using DLuz.ViewModels;

namespace DLuz.Views;

public partial class ConexionPage : Page, IComponentConnector
{
	private readonly ConexionViewModel _vm;

	public ConexionPage()
	{
		InitializeComponent();
		_vm = new ConexionViewModel();
		base.DataContext = _vm;
		base.Unloaded += delegate
		{
			_vm.Dispose();
		};
	}

}

