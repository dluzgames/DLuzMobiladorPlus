using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Markup;
using DLuz.ViewModels;

namespace DLuz.Views;

public partial class VideoPage : Page, IComponentConnector
{
	private readonly VideoViewModel _vm;

	public VideoPage()
	{
		InitializeComponent();
		_vm = new VideoViewModel();
		base.DataContext = _vm;
		base.Unloaded += delegate
		{
			_vm.Dispose();
		};
	}

}

