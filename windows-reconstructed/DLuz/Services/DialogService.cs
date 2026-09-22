using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace DLuz.Services;

public static class DialogService
{
	public enum TresOpciones
	{
		Primaria,
		Secundaria,
		Cancelar
	}

	private static readonly ContentDialogService _service = new ContentDialogService();

	public static void Init(ContentPresenter host)
	{
		_service.SetDialogHost(host);
	}

	internal static Task<ContentDialogResult> MostrarAsync(ContentDialog dialog)
	{
		return _service.ShowAsync(dialog, default(CancellationToken));
	}

	public static Task InfoAsync(string titulo, string mensaje)
	{
		return SimpleAsync(titulo, mensaje);
	}

	public static Task ExitoAsync(string titulo, string mensaje)
	{
		return SimpleAsync(titulo, mensaje);
	}

	public static Task AdvertenciaAsync(string titulo, string mensaje)
	{
		return SimpleAsync(titulo, mensaje);
	}

	public static Task ErrorAsync(string titulo, string mensaje)
	{
		return SimpleAsync(titulo, mensaje);
	}

	private static async Task SimpleAsync(string titulo, string mensaje)
	{
		ContentDialog dialog = new ContentDialog
		{
			Title = titulo,
			Content = mensaje,
			CloseButtonText = "OK"
		};
		await _service.ShowAsync(dialog, default(CancellationToken));
	}

	public static async Task<bool> ConfirmarAsync(string titulo, string mensaje, string submensaje = "", string textoConfirmar = "Sí", string textoCancelar = "No")
	{
		object content = mensaje;
		if (!string.IsNullOrEmpty(submensaje))
		{
			content = new StackPanel
			{
				Children = 
				{
					(UIElement)new System.Windows.Controls.TextBlock
					{
						Text = mensaje,
						TextWrapping = TextWrapping.Wrap
					},
					(UIElement)new System.Windows.Controls.TextBlock
					{
						Text = submensaje,
						TextWrapping = TextWrapping.Wrap,
						Margin = new Thickness(0.0, 8.0, 0.0, 0.0),
						Opacity = 0.75
					}
				}
			};
		}
		ContentDialog dialog = new ContentDialog
		{
			Title = titulo,
			Content = content,
			PrimaryButtonText = textoConfirmar,
			CloseButtonText = textoCancelar,
			PrimaryButtonAppearance = ControlAppearance.Primary
		};
		return await _service.ShowAsync(dialog, default(CancellationToken)) == ContentDialogResult.Primary;
	}

	public static async Task<string?> InputAsync(string titulo, string etiqueta, string placeholder = "", int maxLength = 30)
	{
		Wpf.Ui.Controls.TextBox caja = new Wpf.Ui.Controls.TextBox
		{
			PlaceholderText = placeholder,
			MaxLength = maxLength
		};
		StackPanel stackPanel = new StackPanel();
		stackPanel.Children.Add(new System.Windows.Controls.TextBlock
		{
			Text = etiqueta,
			Margin = new Thickness(0.0, 0.0, 0.0, 8.0)
		});
		stackPanel.Children.Add(caja);
		ContentDialog dialog = new ContentDialog
		{
			Title = titulo,
			Content = stackPanel,
                PrimaryButtonText = "Criar",
			CloseButtonText = "Cancelar",
			PrimaryButtonAppearance = ControlAppearance.Primary
		};
		return (await _service.ShowAsync(dialog, default(CancellationToken)) != ContentDialogResult.Primary) ? null : caja.Text?.Trim();
	}

	public static async Task<TresOpciones> TresOpcionesAsync(string titulo, string mensaje, string primaria, string secundaria, string cancelar = "Cancelar")
	{
		ContentDialog dialog = new ContentDialog
		{
			Title = titulo,
			Content = mensaje,
			PrimaryButtonText = primaria,
			SecondaryButtonText = secundaria,
			CloseButtonText = cancelar,
			PrimaryButtonAppearance = ControlAppearance.Primary
		};
		return await _service.ShowAsync(dialog, default(CancellationToken)) switch
		{
			ContentDialogResult.Primary => TresOpciones.Primaria, 
			ContentDialogResult.Secondary => TresOpciones.Secundaria, 
			_ => TresOpciones.Cancelar, 
		};
	}

	public static async Task<(bool ok, bool noVolverMostrar)> AvanzadoAsync(string titulo, string descripcion, string[] checks, bool requireCheckbox = true, string textoConfirmar = "Continuar", string textoCancelar = "Cancelar")
	{
		StackPanel stackPanel = new StackPanel();
		stackPanel.Children.Add(new System.Windows.Controls.TextBlock
		{
			Text = descripcion,
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(0.0, 0.0, 0.0, 12.0)
		});
		List<CheckBox> obligatorios = new List<CheckBox>();
		if (requireCheckbox && checks != null && checks.Length > 0)
		{
			stackPanel.Children.Add(new System.Windows.Controls.TextBlock
			{
				Text = "Para continuar, confirma que entiendes lo siguiente:",
				TextWrapping = TextWrapping.Wrap,
				Margin = new Thickness(0.0, 0.0, 0.0, 8.0),
				Opacity = 0.85
			});
			foreach (string text in checks)
			{
				CheckBox checkBox = new CheckBox
				{
					Content = new System.Windows.Controls.TextBlock
					{
						Text = text,
						TextWrapping = TextWrapping.Wrap
					},
					Margin = new Thickness(0.0, 2.0, 0.0, 2.0)
				};
				obligatorios.Add(checkBox);
				stackPanel.Children.Add(checkBox);
			}
		}
		CheckBox chkNoVolver = new CheckBox
		{
			Content = "No volver a mostrar este mensaje",
			Margin = new Thickness(0.0, 12.0, 0.0, 0.0),
			Opacity = 0.8
		};
		stackPanel.Children.Add(chkNoVolver);
		ContentDialog dialog = new ContentDialog
		{
			Title = titulo,
			Content = stackPanel,
			PrimaryButtonText = textoConfirmar,
			CloseButtonText = textoCancelar,
			PrimaryButtonAppearance = ControlAppearance.Primary,
			IsPrimaryButtonEnabled = (!requireCheckbox || obligatorios.Count == 0)
		};
		if (requireCheckbox && obligatorios.Count > 0)
		{
			foreach (CheckBox item in obligatorios)
			{
				item.Checked += Reevaluar;
				item.Unchecked += Reevaluar;
			}
		}
		return (ok: await _service.ShowAsync(dialog, default(CancellationToken)) == ContentDialogResult.Primary, noVolverMostrar: chkNoVolver.IsChecked == true);
		void Reevaluar(object? s, RoutedEventArgs e)
		{
			dialog.IsPrimaryButtonEnabled = obligatorios.TrueForAll((CheckBox c) => c.IsChecked == true);
		}
	}
}

