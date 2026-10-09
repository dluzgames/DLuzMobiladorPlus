using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using DLuz.Helpers;
using DLuz.Services;
using Wpf.Ui.Appearance;

namespace DLuz;

public partial class App : Application
{
	public static ResultadoMigracion ResultadoMigracionPerfiles { get; private set; }

	[System.Runtime.InteropServices.DllImport("winmm.dll", EntryPoint = "timeBeginPeriod", SetLastError = true)]
	private static extern uint TimeBeginPeriod(uint uMilliseconds);

	[System.Runtime.InteropServices.DllImport("winmm.dll", EntryPoint = "timeEndPeriod", SetLastError = true)]
	private static extern uint TimeEndPeriod(uint uMilliseconds);

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		// Ativa resolução de timer de 1ms do Windows Kernel para eliminar input lag
		try { TimeBeginPeriod(1); } catch { }

		// ShutdownMode OnMainWindowClose by default

		string desktopLog = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DLuz", "logs", "lifecycle.log");
		AppDomain.CurrentDomain.ProcessExit += (_, __) =>
		{
			try { TimeEndPeriod(1); } catch { }
			string msg = $"[{DateTime.Now}] PROCESS_EXIT:\n" + new System.Diagnostics.StackTrace(true).ToString() + "\n";
			try { File.AppendAllText(desktopLog, msg); } catch {}
			AppLogger.Info(msg);
		};

		AppDomain.CurrentDomain.UnhandledException += (s, args) =>
		{
			string msg = $"[{DateTime.Now}] UNHANDLED_EXCEPTION:\n" + args.ExceptionObject?.ToString() + "\n";
			try { File.AppendAllText(desktopLog, msg); } catch {}
			AppLogger.Error(msg);
		};

		Program.InicializarJob();
		ThemeService.Instance.Initialize();
		WheelGuard.Register();

		AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs args)
		{
			if (args.ExceptionObject is Exception ex2)
			{
				AppLogger.Error("App: excepción FATAL no controlada [" + Diag.Contexto() + "]", ex2);
			}
			IntentarResetDispositivo();
		};
		base.DispatcherUnhandledException += OnDispatcherUnhandledException;
		TaskScheduler.UnobservedTaskException += delegate(object? s, UnobservedTaskExceptionEventArgs args)
		{
			AppLogger.Error("App: excepción de Task no observada [" + Diag.Contexto() + "]", args.Exception);
			args.SetObserved();
		};

		try
		{
			AppPaths.EnsureDirectoriesExist();
			ResultadoMigracionPerfiles = AppMigrator.EjecutarMigracionInicial();
		}
		catch (Exception ex)
		{
			AppLogger.Error("App: error crítico durante inicialización del launcher", ex);
			MessageBox.Show("Não foi possível inicializar a pasta de configuração do DLuzMObi v2.\n\nDetalhes: " + ex.Message + "\n\nO aplicativo tentará continuar, mas talvez as alterações não sejam salvas.", "DLuzMObi v2: erro de inicialização", MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}

		AppLogger.Info("Inicialização OnStartup concluída. Carregando MainWindow via StartupUri...");
	}

	private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
	{
		AppLogger.Error("App: excepción en hilo UI [" + Diag.Contexto() + "]", args.Exception);
		try
		{
			ToastService.Mostrar("Ocorreu um erro inesperado. Ele foi registrado no log e o aplicativo continua funcionando.", ToastTipo.Error, 5000);
		}
		catch
		{
		}
		args.Handled = true;
	}

	private static void IntentarResetDispositivo()
	{
		try
		{
			string rutaAdb = ArquitecturaHelper.RutaAdb;
			if (File.Exists(rutaAdb))
			{
				string[] array = new string[2] { "shell wm density reset", "shell wm size reset" };
				foreach (string argCmd in array)
				{
					Process process = new Process();
					process.StartInfo = FabricaProcesos.AdbSilencioso(rutaAdb, argCmd);
					process.Start();
					process.WaitForExit(3000);
				}
			}
		}
		catch
		{
		}
	}
}







