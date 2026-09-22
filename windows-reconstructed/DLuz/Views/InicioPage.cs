using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Markup;
using DLuz.ViewModels;

namespace DLuz.Views;

public partial class InicioPage : Page, IComponentConnector
{
	private readonly InicioViewModel _vm;

	public InicioPage()
	{
		InitializeComponent();
		_vm = new InicioViewModel();
		base.DataContext = _vm;
		base.Unloaded += delegate
		{
			_vm.Dispose();
		};
	}

	private void ComboPerfil_DropDownOpened(object sender, EventArgs e)
	{
		_vm.RefrescarPerfiles();
	}

}

