using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Markup;
using DLuz.ViewModels;

namespace DLuz.Views;

public partial class AcercaDePage : Page, IComponentConnector
{
	public AcercaDePage()
	{
		InitializeComponent();
		base.DataContext = new AcercaDeViewModel();
	}

}

