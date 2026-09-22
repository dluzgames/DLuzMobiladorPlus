using System;
using System.Windows;
using System.Windows.Controls;
using DLuz.Helpers;
using Wpf.Ui.Controls;

namespace DLuz.Services;

public static class ToastService
{
	private static SnackbarPresenter? _presenter;

	public static void Init(SnackbarPresenter presenter)
	{
		_presenter = presenter;
	}

	private static (ControlAppearance appearance, SymbolRegular icon, string titulo) Mapear(ToastTipo tipo)
	{
		return tipo switch
		{
			ToastTipo.Exito => (appearance: ControlAppearance.Success, icon: SymbolRegular.CheckmarkCircle24, titulo: "Sucesso"), 
			ToastTipo.Advertencia => (appearance: ControlAppearance.Caution, icon: SymbolRegular.Warning24, titulo: "Aviso"), 
			ToastTipo.Peligro => (appearance: ControlAppearance.Danger, icon: SymbolRegular.Delete24, titulo: "Excluído"), 
			ToastTipo.Error => (appearance: ControlAppearance.Danger, icon: SymbolRegular.ErrorCircle24, titulo: "Erro"), 
			ToastTipo.Neutral => (appearance: ControlAppearance.Secondary, icon: SymbolRegular.Info24, titulo: "Informação"), 
			_ => (appearance: ControlAppearance.Secondary, icon: SymbolRegular.Info24, titulo: "Informação"), 
		};
	}

	public static void Mostrar(string mensaje, ToastTipo tipo = ToastTipo.Info, int duracionMs = 3000)
	{
		try
		{
			SnackbarPresenter presenter = _presenter;
			if (presenter != null)
			{
				var (appearance, icon, titulo) = Mapear(tipo);
				if (presenter.Dispatcher.CheckAccess())
				{
					Show();
				}
				else
				{
					presenter.Dispatcher.BeginInvoke(new Action(Show));
				}
				void Show()
				{
					try
					{
						Snackbar snackbar = new Snackbar(presenter);
						snackbar.Title = titulo;
						snackbar.Content = mensaje;
						snackbar.Appearance = appearance;
						snackbar.Icon = new SymbolIcon(icon);
						snackbar.Timeout = TimeSpan.FromMilliseconds(duracionMs);
						snackbar.Show();
					}
					catch (Exception ex)
					{
						AppLogger.Warn("ToastService.Show: " + ex.Message);
					}
				}
			}
		}
		catch
		{
		}
	}

	public static void MostrarConAccion(string titulo, string mensaje, ToastTipo tipo, int duracionMs, string textoAccion, Action accion)
	{
		try
		{
			SnackbarPresenter presenter = _presenter;
			if (presenter != null)
			{
				var (appearance, icon, _) = Mapear(tipo);
				if (presenter.Dispatcher.CheckAccess())
				{
					Show();
				}
				else
				{
					presenter.Dispatcher.BeginInvoke(new Action(Show));
				}
				void Show()
				{
					try
					{
						Wpf.Ui.Controls.Button boton = new Wpf.Ui.Controls.Button
						{
							Content = textoAccion,
							Appearance = ControlAppearance.Secondary,
							Margin = new Thickness(0.0, 8.0, 0.0, 0.0)
						};
						boton.Click += delegate
						{
							try
							{
								accion();
								boton.Content = "✓ Pronto";
							}
							catch
							{
								boton.Content = "Não foi possível";
							}
						};
						StackPanel content = new StackPanel
						{
							Children = 
							{
								(UIElement)new System.Windows.Controls.TextBlock
								{
									Text = mensaje,
									TextWrapping = TextWrapping.Wrap
								},
								(UIElement)boton
							}
						};
						Snackbar snackbar = new Snackbar(presenter);
						snackbar.Title = titulo;
						snackbar.Content = content;
						snackbar.Appearance = appearance;
						snackbar.Icon = new SymbolIcon(icon);
						snackbar.Timeout = TimeSpan.FromMilliseconds(duracionMs);
						snackbar.Show();
					}
					catch (Exception ex)
					{
						AppLogger.Warn("ToastService.ShowConAccion: " + ex.Message);
					}
				}
			}
		}
		catch
		{
		}
	}
}

