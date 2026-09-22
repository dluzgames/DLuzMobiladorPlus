using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using DLuz.Helpers;
using DLuz.Services;
using DLuz.ViewModels;

namespace DLuz.Views;

public partial class PerfilesPage : Page, IComponentConnector
{
	public PerfilesPage()
	{
		InitializeComponent();
		base.DataContext = new PerfilesViewModel();
		ComboModoDesempenho.SelectionChanged += delegate
		{
			AtualizarDescricaoDesempenho();
		};
		SliderFpsDesempenho.ValueChanged += delegate
		{
			TxtFpsDesempenho.Text = $"Taxa de quadros: {(int)SliderFpsDesempenho.Value}";
		};
		BtnSalvarModoDesempenho.Click += delegate
		{
			string modo = ModoSelecionado();
			ModoDesempenhoService.Salvar(modo);
			SalvarOpcoesDesempenho();
			ToastService.Mostrar("Configurações de desempenho salvas. Serão aplicadas na próxima sessão.", ToastTipo.Exito, 3000);
		};
		base.Loaded += delegate
		{
			SelecionarModoSalvo();
			CarregarOpcoesDesempenho();
		};
	}

	private string ModoSelecionado()
	{
		return (ComboModoDesempenho.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? ModoDesempenhoService.Equilibrado;
	}

	private void SelecionarModoSalvo()
	{
		string salvo = ModoDesempenhoService.Carregar();
		foreach (object item in ComboModoDesempenho.Items)
		{
			if (item is ComboBoxItem opcao && string.Equals(opcao.Tag?.ToString(), salvo, StringComparison.OrdinalIgnoreCase))
			{
				ComboModoDesempenho.SelectedItem = opcao;
				break;
			}
		}
		AtualizarDescricaoDesempenho();
	}

	private void AtualizarDescricaoDesempenho()
	{
		switch (ModoSelecionado())
		{
		case ModoDesempenhoService.AltaPerformance:
			TxtTituloModoDesempenho.Text = "Alta performance";
			TxtDescricaoModoDesempenho.Text = "Prioriza fluidez e resposta do espelhamento, usando prioridade alta do processo.";
			break;
		case ModoDesempenhoService.MemoriaBaixa:
			TxtTituloModoDesempenho.Text = "Memória baixa";
			TxtDescricaoModoDesempenho.Text = "Reduz a prioridade de recursos do espelhamento para favorecer computadores com pouca memória disponível.";
			break;
		default:
			TxtTituloModoDesempenho.Text = "Equilibrado";
			TxtDescricaoModoDesempenho.Text = "Mantém um equilíbrio entre qualidade, fluidez e uso de memória. Recomendado para a maioria dos computadores.";
			break;
		}
	}

	private static string CaminhoOpcoesDesempenho => Path.Combine(AppPaths.LocalAppDataDir, "performance_options.ini");

	private void SalvarOpcoesDesempenho()
	{
		string cpu = (ComboCpuDesempenho.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "4";
		string memoria = (ComboMemoriaDesempenho.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "4096";
		string texto = $"cpu={cpu}\nmemoria_mb={memoria}\nfps={(int)SliderFpsDesempenho.Value}\nfps_alto={ToggleFpsAlto.IsChecked == true}\nvsync={ToggleVsync.IsChecked == true}\nmostrar_fps={ToggleMostrarFps.IsChecked == true}\n";
		File.WriteAllText(CaminhoOpcoesDesempenho, texto);
	}

	private void CarregarOpcoesDesempenho()
	{
		if (!File.Exists(CaminhoOpcoesDesempenho))
		{
			return;
		}
		try
		{
			foreach (string linha in File.ReadAllLines(CaminhoOpcoesDesempenho))
			{
				string[] partes = linha.Split('=', 2);
				if (partes.Length != 2) continue;
				switch (partes[0])
				{
				case "cpu": SelecionarPorTag(ComboCpuDesempenho, partes[1]); break;
				case "memoria_mb": SelecionarPorTag(ComboMemoriaDesempenho, partes[1]); break;
				case "fps": if (double.TryParse(partes[1], out double fps)) SliderFpsDesempenho.Value = fps; break;
				case "fps_alto": if (bool.TryParse(partes[1], out bool alto)) ToggleFpsAlto.IsChecked = alto; break;
				case "vsync": if (bool.TryParse(partes[1], out bool vsync)) ToggleVsync.IsChecked = vsync; break;
				case "mostrar_fps": if (bool.TryParse(partes[1], out bool mostrar)) ToggleMostrarFps.IsChecked = mostrar; break;
				}
			}
		}
		catch (Exception ex)
		{
			AppLogger.Warn("Não foi possível carregar as opções de desempenho: " + ex.Message);
		}
	}

	private static void SelecionarPorTag(ComboBox combo, string tag)
	{
		foreach (object item in combo.Items)
		{
			if (item is ComboBoxItem opcao && string.Equals(opcao.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase))
			{
				combo.SelectedItem = opcao;
				return;
			}
		}
	}

}

