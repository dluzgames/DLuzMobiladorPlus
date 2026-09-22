using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using DLuz.Services;
using DLuz.ViewModels;

namespace DLuz.Views;

public partial class OptimizacionPage : Page, IComponentConnector
{
	private OptimizacionViewModel? _vm;

	private bool _guardChecked;

	public OptimizacionPage()
	{
		InitializeComponent();
		base.Loaded += OnLoaded;
		base.Unloaded += delegate
		{
			_vm?.Dispose();
		};
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (_guardChecked)
		{
			return;
		}
		_guardChecked = true;
		SessionState s = SessionState.Instance;
		if (!s.OptimizacionAceptada)
		{
			var (flag, flag2) = await DialogService.AvanzadoAsync("Optimización ADB", "Esta sección utiliza comandos ADB para solicitar cambios al sistema Android.\n\nAunque los comandos incluidos están basados en funciones documentadas de Android/AOSP, su efecto puede variar según la marca, modelo, versión de Android, modo térmico, batería, permisos del fabricante y restricciones del sistema.\n\nAlgunas opciones son avanzadas y pueden modificar comportamiento de apps, procesos en segundo plano o paquetes instalados para el usuario actual. Usa estas funciones solo si entiendes que pueden no funcionar igual en todos los dispositivos.", new string[1] { "Acepto usar funciones avanzadas bajo mi responsabilidad y entiendo que el resultado depende del dispositivo." }, requireCheckbox: true, "Entrar a optimización");
			if (!flag)
			{
				if (Application.Current.MainWindow is MainWindow mainWindow)
				{
					mainWindow.RootNav.Navigate(typeof(InicioPage));
				}
				return;
			}
			s.OptimizacionAceptada = true;
			if (flag2)
			{
				s.GuardarAceptacionOptimizacion();
			}
		}
		_vm = new OptimizacionViewModel();
		base.DataContext = _vm;
	}

}

