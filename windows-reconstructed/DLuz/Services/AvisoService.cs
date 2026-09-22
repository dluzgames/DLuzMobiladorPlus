using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DLuz.Helpers;
using Wpf.Ui.Controls;

namespace DLuz.Services;

public static class AvisoService
{
	private const int DuracionInfoMs = 9000;

	private const int DuracionInfoDiagnosticoMs = 15000;

	private const int LineasDeLog = 20;

	private static readonly SemaphoreSlim _unoALaVez = new SemaphoreSlim(1, 1);

	public static void Info(string mensaje, ToastTipo tipo = ToastTipo.Advertencia)
	{
		ToastService.Mostrar(mensaje, tipo, 9000);
	}

	public static void InfoConDiagnostico(string titulo, string mensaje, string detalleTecnico, ToastTipo tipo = ToastTipo.Advertencia)
	{
		AppLogger.Warn("[AVISO-DEGRADADO] " + titulo + ": " + mensaje);
		ToastService.MostrarConAccion(titulo, mensaje, tipo, 15000, "Copiar diagnóstico", delegate
		{
			Clipboard.SetText(ConstruirDiagnostico(titulo, detalleTecnico));
		});
	}

	public static void Urgente(string titulo, string mensajeUsuario, string detalleTecnico)
	{
		SessionState.Marshal(delegate
		{
			UrgenteAsync(titulo, mensajeUsuario, detalleTecnico);
		});
	}

	public static async Task UrgenteAsync(string titulo, string mensajeUsuario, string detalleTecnico)
	{
		AppLogger.Error("[AVISO-URGENTE] " + titulo);
		try
		{
			await _unoALaVez.WaitAsync();
			try
			{
				await MostrarDialogoAsync(titulo, mensajeUsuario, detalleTecnico);
			}
			finally
			{
				_unoALaVez.Release();
			}
		}
		catch (Exception ex)
		{
			AppLogger.Error("AvisoService: no se pudo mostrar el diálogo urgente", ex);
			Info(titulo + ". " + mensajeUsuario, ToastTipo.Error);
		}
	}

	private static async Task MostrarDialogoAsync(string titulo, string mensajeUsuario, string detalleTecnico)
	{
		StackPanel stackPanel = new StackPanel
		{
			MaxWidth = 480.0
		};
		stackPanel.Children.Add(new System.Windows.Controls.TextBlock
		{
			Text = mensajeUsuario,
			TextWrapping = TextWrapping.Wrap
		});
		System.Windows.Controls.TextBox content = new System.Windows.Controls.TextBox
		{
			Text = detalleTecnico,
			IsReadOnly = true,
			TextWrapping = TextWrapping.Wrap,
			MaxHeight = 160.0,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			FontFamily = new FontFamily("Cascadia Mono, Consolas"),
			FontSize = 12.0,
			BorderThickness = new Thickness(0.0),
			Background = Brushes.Transparent,
			Margin = new Thickness(0.0, 6.0, 0.0, 0.0)
		};
		stackPanel.Children.Add(new Expander
		{
			Header = "Detalle técnico",
			Content = content,
			IsExpanded = false,
			Margin = new Thickness(0.0, 12.0, 0.0, 0.0)
		});
		Wpf.Ui.Controls.Button btnCopiar = new Wpf.Ui.Controls.Button
		{
			Content = "Copiar diagnóstico",
			Icon = new SymbolIcon(SymbolRegular.Copy24),
			Appearance = ControlAppearance.Secondary,
			Margin = new Thickness(0.0, 12.0, 0.0, 0.0)
		};
		btnCopiar.Click += delegate
		{
			try
			{
				Clipboard.SetText(ConstruirDiagnostico(titulo, detalleTecnico));
				btnCopiar.Content = "✓ Diagnóstico copiado";
			}
			catch
			{
				btnCopiar.Content = "No se pudo copiar";
			}
		};
		stackPanel.Children.Add(btnCopiar);
		await DialogService.MostrarAsync(new ContentDialog
		{
			Title = "⚠ " + titulo,
			Content = stackPanel,
			CloseButtonText = "Entendido"
		});
	}

	public static string ConstruirDiagnostico(string problema, string detalleTecnico)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("── Diagnóstico DLuzMObi v2 ──");
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(13, 3, stringBuilder2);
		handler.AppendLiteral("Versión: ");
		handler.AppendFormatted("DLuzMObi v2");
		handler.AppendLiteral(" ");
		handler.AppendFormatted(AppInfo.VersionConPrefijo);
		handler.AppendLiteral(" (");
		handler.AppendFormatted("Estable");
		handler.AppendLiteral(")");
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(7, 1, stringBuilder2);
		handler.AppendLiteral("Fecha: ");
		handler.AppendFormatted(DateTime.Now, "yyyy-MM-dd HH:mm:ss");
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(10, 1, stringBuilder2);
		handler.AppendLiteral("Problema: ");
		handler.AppendFormatted(problema);
		stringBuilder5.AppendLine(ref handler);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("Detalle técnico:");
		stringBuilder.AppendLine(detalleTecnico.Trim());
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("Últimas líneas del log:");
		stringBuilder.AppendLine(LeerColaLog(20));
		stringBuilder.Append("── Fin del diagnóstico ──");
		return stringBuilder.ToString();
	}

	private static string LeerColaLog(int lineas)
	{
		try
		{
			if (!File.Exists(AppLogger.LogPath))
			{
				return "(sin log)";
			}
			string[] source = File.ReadAllLines(AppLogger.LogPath);
			return string.Join(Environment.NewLine, source.TakeLast(lineas));
		}
		catch (Exception ex)
		{
			return "(no se pudo leer el log: " + ex.Message + ")";
		}
	}
}

