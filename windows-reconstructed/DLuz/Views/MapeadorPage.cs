using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Markup;
using DLuz.Helpers;
using DLuz.ViewModels;

namespace DLuz.Views;

public partial class MapeadorPage : Page, IComponentConnector
{
	private readonly MapeadorViewModel _vm;

	public MapeadorPage()
	{
		try
		{
			InitializeComponent();
		}
		catch (Exception ex)
		{
			AppLogger.Error("Falha ao criar a tela Modo DLuzStacks", ex);
			throw;
		}
		_vm = new MapeadorViewModel();
		base.DataContext = _vm;
		base.Unloaded += delegate
		{
			_vm.Dispose();
		};
	}

}

