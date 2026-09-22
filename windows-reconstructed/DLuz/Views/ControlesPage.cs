using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Markup;
using DLuz.ViewModels;

namespace DLuz.Views;

public partial class ControlesPage : Page, IComponentConnector
{
	private readonly ControlesViewModel _vm;

	public ControlesPage()
	{
		InitializeComponent();
		_vm = new ControlesViewModel();
		base.DataContext = _vm;
		base.Unloaded += delegate
		{
			_vm.Dispose();
		};
	}

}

