using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Microsoft.Win32;
using DLuz.Services;
using DLuz.ViewModels;

namespace DLuz.Views;

public partial class ExtrasPage : Page, IComponentConnector
{
	private readonly ExtrasViewModel _vm;

	public ExtrasPage()
	{
		InitializeComponent();
		_vm = new ExtrasViewModel();
		base.DataContext = _vm;
		TxtPastaCapturas.Text = SessionState.Instance.CapturaMidia.PastaDestino;
		base.PreviewKeyDown += OnPreviewKeyDown;
		base.Unloaded += delegate
		{
			_vm.Dispose();
		};
	}

	private void EscolherPastaCapturas_Click(object sender, System.Windows.RoutedEventArgs e)
	{
		OpenFolderDialog dialog = new OpenFolderDialog { Title = "Escolha onde salvar capturas e gravações", InitialDirectory = SessionState.Instance.CapturaMidia.PastaDestino };
		if (dialog.ShowDialog() == true)
		{
			SessionState.Instance.CapturaMidia.Configurar(dialog.FolderName, "mp4");
			TxtPastaCapturas.Text = SessionState.Instance.CapturaMidia.PastaDestino;
		}
	}

	private void FormatoGravacao_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (TxtPastaCapturas != null)
		{
			SessionState.Instance.CapturaMidia.Configurar(SessionState.Instance.CapturaMidia.PastaDestino, "mp4");
		}
	}

	private void OnPreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (_vm.CapturandoMod)
		{
			Key key = ((e.Key == Key.System) ? e.SystemKey : e.Key);
			_vm.CapturarModAsync(key);
			e.Handled = true;
		}
	}

}

