using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DLuz.Helpers;
using DLuz.Services;
using DLuz.ViewModels;
using DLuz.Views;
using Wpf.Ui.Controls;

namespace DLuz;

public partial class MainWindow : FluentWindow, IComponentConnector
{
	private sealed record RailEntry(System.Windows.Controls.Button Boton, System.Windows.Controls.TextBlock Texto, SymbolIcon Icono);

	private readonly SessionState _s = SessionState.Instance;

	private readonly Dictionary<Type, RailEntry> _rail = new Dictionary<Type, RailEntry>();

	private Type? _tipoActivo;

	private bool _navConfirmada;

	private bool _forzarCierre;

	public MainWindow()
	{
		InitializeComponent();
		ToastService.Init(RootSnackbar);
		DialogService.Init(RootDialogHost);
		RootNav.Navigated += OnNavViewNavigated;
		RootNav.Navigating += OnNavViewNavigating;
		ConstruirRail();
		RailScroll.PreviewMouseWheel += delegate(object _, MouseWheelEventArgs e)
		{
			RailScroll.ScrollToHorizontalOffset(RailScroll.HorizontalOffset - (double)e.Delta);
			e.Handled = true;
		};
		RailScroll.ScrollChanged += delegate
		{
			ActualizarChevrons();
		};
		base.SizeChanged += delegate
		{
			PosicionarIndicador(animar: false);
			ActualizarChevrons();
		};
		RailContent.SizeChanged += delegate
		{
			PosicionarIndicador(animar: false);
			ActualizarChevrons();
		};
		_s.CargarConfig();
		_s.AplicarPerfilInicialSiPrimerArranque();
		base.Loaded += async delegate
		{
			try
			{
				RootNav.Navigate(typeof(MapeadorPage));
				if (App.ResultadoMigracionPerfiles == ResultadoMigracion.Migrado)
				{
					ToastService.Mostrar("Perfiles anteriores importados correctamente.", ToastTipo.Exito);
				}
				else if (App.ResultadoMigracionPerfiles == ResultadoMigracion.Fallo)
				{
					ToastService.Mostrar("No se pudieron importar los perfiles anteriores.", ToastTipo.Error);
				}
				if (_s.MostrarAvisoPrimerArranque)
				{
					_s.MostrarAvisoPrimerArranque = false;
					await DialogService.InfoAsync("Perfil seleccionado", "Se ha seleccionado el perfil " + _s.PerfilInicialNombre + " como configuração inicial recomendada.\n\nPuedes cambiarlo en qualquer momento desde a seção de Perfis.");
				}
				await _s.IniciarDeteccionAsync();
			}
			catch (Exception ex)
			{
				AppLogger.Error("MainWindow: erro ao carregar página inicial", ex);
			}
		};
		base.Closing += MainWindow_Closing;
	}

	private void ConstruirRail()
	{
		(string? Header, Type? PageType, string? Title, SymbolRegular Symbol)[] items = new (string?, Type?, string?, SymbolRegular)[]
		{
			("MODOS DE USO", null, null, SymbolRegular.Empty),
			(null, typeof(MapeadorPage), "Modo DLuzStacks", SymbolRegular.Desktop24),
			(null, typeof(InicioPage), "Espelhar / GG Mouse 🔒", SymbolRegular.Home24),
			(null, typeof(ModoOtgPage), "Modo Sem Vídeo (OTG) 🔒", SymbolRegular.Keyboard24),

			("CONFIGURAÇÕES", null, null, SymbolRegular.Empty),
			(null, typeof(AjustesPage), "Ajustes & Mira Pro 🔒", SymbolRegular.Wrench24),
			(null, typeof(OptimizacionPage), "Otimização & FPS 🔒", SymbolRegular.Flash24),
			(null, typeof(VideoPage), "Vídeo e Áudio", SymbolRegular.Video24),
			(null, typeof(PantallaPage), "Tela do celular", SymbolRegular.Desktop24),
			(null, typeof(ConexionPage), "Conexão USB / Wi-Fi", SymbolRegular.Wifi124),
			(null, typeof(ControlesPage), "Controles", SymbolRegular.Games24),
			(null, typeof(ExtrasPage), "Opções extras", SymbolRegular.Options24),

			("AJUDA & SUPORTE", null, null, SymbolRegular.Empty),
			(null, typeof(TutoriaisPage), "🎓 Tutoriais em Vídeo", SymbolRegular.VideoClip24),
			(null, typeof(AcercaDePage), "Informações & Suporte", SymbolRegular.Info24)
		};

		Style style = (Style)base.Resources["RailButton"];
		Brush foreground = (Brush)FindResource("Stex.TextSecondaryBrush");
		Brush accentBrush = (Brush)FindResource("Stex.AccentLightBrush");

		RailItems.Children.Clear();
		_rail.Clear();

		for (int i = 0; i < items.Length; i++)
		{
			var tuple = items[i];
			if (tuple.Header != null)
			{
				var headerContainer = new StackPanel
				{
					Margin = new Thickness(6, i == 0 ? 0 : 14, 6, 6)
				};

				if (i > 0)
				{
					headerContainer.Children.Add(new Border
					{
						Height = 1,
						Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
						Margin = new Thickness(4, 0, 4, 10)
					});
				}

				var headerText = new System.Windows.Controls.TextBlock
				{
					Text = tuple.Header,
					FontSize = 10.5,
					FontWeight = FontWeights.Bold,
					Foreground = accentBrush,
					Margin = new Thickness(6, 0, 0, 0),
					Opacity = 0.95
				};
				headerContainer.Children.Add(headerText);
				RailItems.Children.Add(headerContainer);
				continue;
			}

			if (tuple.PageType == null || tuple.Title == null) continue;

			SymbolIcon symbolIcon = new SymbolIcon
			{
				Symbol = tuple.Symbol,
				FontSize = 16.0,
				Margin = new Thickness(0.0, 0.0, 8.0, 0.0),
				Foreground = foreground
			};
			System.Windows.Controls.TextBlock textBlock = new System.Windows.Controls.TextBlock
			{
				Text = tuple.Title,
				FontSize = 13.0,
				VerticalAlignment = VerticalAlignment.Center,
				Foreground = foreground
			};
			StackPanel stackPanel = new StackPanel
			{
				Orientation = Orientation.Horizontal
			};
			stackPanel.Children.Add(symbolIcon);
			stackPanel.Children.Add(textBlock);
			Type tipo = tuple.PageType;
			System.Windows.Controls.Button button = new System.Windows.Controls.Button
			{
				Content = stackPanel,
				Style = style,
				Tag = tipo
			};
			button.Click += delegate
			{
				if (tipo == typeof(InicioPage))
				{
					if (!LicenseService.Instance.VerificarOuBloquearPro("Espelhar / GG Mouse", this)) return;
				}
				else if (tipo == typeof(ModoOtgPage))
				{
					if (!LicenseService.Instance.VerificarOuBloquearPro("Modo Sem Vídeo (OTG)", this)) return;
				}
				else if (tipo == typeof(AjustesPage))
				{
					if (!LicenseService.Instance.VerificarOuBloquearPro("Ajustes & Mira Pro", this)) return;
				}
				else if (tipo == typeof(OptimizacionPage))
				{
					if (!LicenseService.Instance.VerificarOuBloquearPro("Otimização & FPS", this)) return;
				}
				RootNav.Navigate(tipo);
			};
			RailItems.Children.Add(button);
			_rail[tipo] = new RailEntry(button, textBlock, symbolIcon);
		}
	}

	public void NavegarPara(Type pageType)
	{
		RootNav?.Navigate(pageType);
	}

	private void OnNavViewNavigated(NavigationView sender, NavigatedEventArgs args)
	{
		Page page = args.Page as Page;
		Diag.CurrentPage = page?.Title ?? args.Page?.GetType().Name ?? "?";
		ShellDirtyBar.DataContext = ((page?.DataContext is SeccionViewModel seccionViewModel) ? seccionViewModel : null);
		Type type = args.Page?.GetType();
		if (type != null && _rail.ContainsKey(type))
		{
			_tipoActivo = type;
			MarcarActivo(type);
		}
		else
		{
			_tipoActivo = null;
			MarcarActivo(null);
			NavIndicator.Visibility = Visibility.Collapsed;
		}
		base.Dispatcher.BeginInvoke((Action)delegate
		{
			if (page != null)
			{
				PtBrLocalizer.Aplicar(page);
			}
			PosicionarIndicador(animar: true);
			if (_tipoActivo != null && _rail.TryGetValue(_tipoActivo, out RailEntry value))
			{
				value.Boton.BringIntoView();
			}
		}, DispatcherPriority.Loaded);
	}

	private void ActualizarChevrons()
	{
		bool flag = RailScroll.ScrollableWidth > 1.0;
		RailLeft.Visibility = ((!flag || !(RailScroll.HorizontalOffset > 1.0)) ? Visibility.Collapsed : Visibility.Visible);
		RailRight.Visibility = ((!flag || !(RailScroll.HorizontalOffset < RailScroll.ScrollableWidth - 1.0)) ? Visibility.Collapsed : Visibility.Visible);
	}

	private void AbrirModalAtivacao_Click(object sender, RoutedEventArgs e)
	{
		var win = new DLuz.Views.LicencaWindow
		{
			Owner = this
		};
		win.ShowDialog();
	}

	private void RailLeft_Click(object sender, RoutedEventArgs e)
	{
		RailScroll.ScrollToHorizontalOffset(Math.Max(0.0, RailScroll.HorizontalOffset - 220.0));
	}

	private void RailRight_Click(object sender, RoutedEventArgs e)
	{
		RailScroll.ScrollToHorizontalOffset(Math.Min(RailScroll.ScrollableWidth, RailScroll.HorizontalOffset + 220.0));
	}

	private void MarcarActivo(Type? tipo)
	{
		Brush brush = (Brush)FindResource("Stex.AccentLightBrush");
		Brush brush2 = (Brush)FindResource("Stex.TextSecondaryBrush");
		foreach (KeyValuePair<Type, RailEntry> item in _rail)
		{
			item.Deconstruct(out var key, out var value);
			Type type = key;
			RailEntry railEntry = value;
			bool flag = type == tipo;
			railEntry.Texto.Foreground = (flag ? brush : brush2);
			railEntry.Icono.Foreground = (flag ? brush : brush2);
			railEntry.Texto.FontWeight = (flag ? FontWeights.SemiBold : FontWeights.Normal);
			railEntry.Boton.Background = flag ? (Brush)FindResource("Stex.BgTabActiveBrush") : Brushes.Transparent;
		}
	}

	private void PosicionarIndicador(bool animar)
	{
		if (_tipoActivo == null || !_rail.TryGetValue(_tipoActivo, out RailEntry value))
		{
			return;
		}
		System.Windows.Controls.Button boton = value.Boton;
		if (!boton.IsLoaded || RailContent.ActualHeight <= 0.0)
		{
			return;
		}
		try
		{
			double num = boton.TransformToVisual(RailContent).Transform(new Point(0.0, 0.0)).Y + 5.0;
			double num2 = Math.Max(0.0, boton.ActualHeight - 10.0);
			if (!(num2 <= 0.0))
			{
				if (NavIndicator.Visibility != Visibility.Visible || !animar)
				{
					NavIndicator.BeginAnimation(Canvas.TopProperty, null);
					NavIndicator.BeginAnimation(FrameworkElement.HeightProperty, null);
					NavIndicator.Visibility = Visibility.Visible;
					Canvas.SetTop(NavIndicator, num);
					NavIndicator.Height = num2;
				}
				else
				{
					Duration duration = new Duration(TimeSpan.FromMilliseconds(190.0));
					QuadraticEase easingFunction = new QuadraticEase
					{
						EasingMode = EasingMode.EaseOut
					};
					NavIndicator.BeginAnimation(Canvas.TopProperty, new DoubleAnimation(num, duration)
					{
						EasingFunction = easingFunction
					});
					NavIndicator.BeginAnimation(FrameworkElement.HeightProperty, new DoubleAnimation(num2, duration)
					{
						EasingFunction = easingFunction
					});
				}
			}
		}
		catch
		{
		}
	}

	private async void OnNavViewNavigating(NavigationView sender, NavigatingCancelEventArgs args)
	{
		if (_navConfirmada)
		{
			return;
		}
		SeccionPerfil? seccionPerfil = SeccionDeTitulo(Diag.CurrentPage);
		if (!seccionPerfil.HasValue)
		{
			return;
		}
		SeccionPerfil s = seccionPerfil.GetValueOrDefault();
		if (!_s.SeccionDirty(s))
		{
			return;
		}
		Type destino = (args.Page as Type) ?? args.Page?.GetType();
		if (destino == TipoDeSeccion(s))
		{
			return;
		}
		args.Cancel = true;
		switch (await DialogService.TresOpcionesAsync("Alterações não salvas em " + NombreDeSeccion(s), "Há alterações pendentes nesta seção. O que deseja fazer antes de mudar de seção?", "Salvar e continuar", "Descartar e continuar", "Permanecer aqui"))
		{
		case DialogService.TresOpciones.Cancelar:
			return;
		case DialogService.TresOpciones.Primaria:
			_s.GuardarSecciones();
			break;
		default:
			_s.RevertirSeccion(s);
			break;
		}
		if (!(destino != null))
		{
			return;
		}
		_navConfirmada = true;
		try
		{
			RootNav.Navigate(destino);
		}
		finally
		{
			_navConfirmada = false;
		}
	}

	private static SeccionPerfil? SeccionDeTitulo(string titulo)
	{
		return titulo switch
		{
			"Vídeo e Áudio" => SeccionPerfil.Video, 
			"Tela do celular" => SeccionPerfil.Pantalla, 
			"Opções extras" => SeccionPerfil.Extras, 
			"Controles" => SeccionPerfil.Controles, 
			_ => null, 
		};
	}

	private static Type TipoDeSeccion(SeccionPerfil s)
	{
		return s switch
		{
			SeccionPerfil.Video => typeof(VideoPage), 
			SeccionPerfil.Pantalla => typeof(PantallaPage), 
			SeccionPerfil.Extras => typeof(ExtrasPage), 
			_ => typeof(ControlesPage), 
		};
	}

	private static string NombreDeSeccion(SeccionPerfil s)
	{
		return s switch
		{
			SeccionPerfil.Video => "Vídeo e Áudio", 
			SeccionPerfil.Pantalla => "Tela do celular", 
			SeccionPerfil.Extras => "Opções extras", 
			_ => "Controles", 
		};
	}

	private async void MainWindow_Closing(object? sender, CancelEventArgs e)
	{
		if (!_forzarCierre && _s.HayCambiosPerfil)
		{
			e.Cancel = true;
			switch (await DialogService.TresOpcionesAsync("Alterações não salvas", "Há alterações que ainda não foram salvas. O que deseja fazer antes de sair?", "Salvar e sair", "Sair sem salvar"))
			{
			case DialogService.TresOpciones.Cancelar:
				return;
			case DialogService.TresOpciones.Primaria:
				_s.GuardarSecciones();
				break;
			default:
				if (_s.HayCambiosConfig)
				{
					_s.RevertirCambiosConfig();
				}
				break;
			}
			_forzarCierre = true;
			await base.Dispatcher.BeginInvoke(new Action(base.Close));
			return;
		}
		try
		{
			if (_s.MapeadorActivo)
			{
				_s.Mapeador.DetenerSesion();
			}
			_s.Scrcpy.Detener();
			_s.Adb.DetenerTrackDevices();
			if (_s.Adb.HayDispositivoConectado())
			{
				if (_s.WmSizeActivo || _s.ResAdbActiva)
				{
					_s.Adb.ResetearResolucion();
				}
				if (_s.DpiPendienteReset != 0)
				{
					_s.Adb.ResetearDPI();
				}
			}
			if (_s.WifiConectado)
			{
				_s.Adb.DesconectarTodo();
			}
			_s.GuardarConfig();
			_s.Adb.CerrarDaemonLocal();
		}
		catch
		{
		}
	}

}








