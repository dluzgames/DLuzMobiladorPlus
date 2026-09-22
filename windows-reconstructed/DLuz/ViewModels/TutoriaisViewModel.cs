using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLuz.Helpers;
using DLuz.Services;

namespace DLuz.ViewModels;

public class TutorialItem : ObservableObject
{
	public string Id { get; set; } = "";
	public string Numero { get; set; } = "01";
	public string Titulo { get; set; } = "";
	public string Descricao { get; set; } = "";
	public string Duracao { get; set; } = "03:00";
	public string Categoria { get; set; } = "Geral";
	public string VideoUrl { get; set; } = "https://www.youtube.com/@dluzgames";
	public string TagCor { get; set; } = "#38EF7D";
	public string IconeSimbolo { get; set; } = "Play24";
}

public class TutoriaisViewModel : ObservableObject
{
	private ObservableCollection<TutorialItem> _tutoriais = new();
	public ObservableCollection<TutorialItem> Tutoriais
	{
		get => _tutoriais;
		set => SetProperty(ref _tutoriais, value);
	}

	private TutorialItem? _tutorialSelecionado;
	public TutorialItem? TutorialSelecionado
	{
		get => _tutorialSelecionado;
		set => SetProperty(ref _tutorialSelecionado, value);
	}

	private string _playlistUrl = "https://www.youtube.com/@dluzgames";
	public string PlaylistUrl
	{
		get => _playlistUrl;
		set => SetProperty(ref _playlistUrl, value);
	}

	public TutoriaisViewModel()
	{
		CarregarTutoriais();
	}

	public void CarregarTutoriais()
	{
		Tutoriais.Clear();

		// Módulos Completos de Tutoriais
		Tutoriais.Add(new TutorialItem
		{
			Id = "inicio-rapido",
			Numero = "01",
			Titulo = "Início Rápido: Conectar e Jogar em 2 Minutos",
			Descricao = "Passo a passo inicial para plugar o cabo USB, clicar em Auto Configurar e jogar com 120 FPS e Zero Delay na hora.",
			Duracao = "02:30",
			Categoria = "ESSENCIAL",
			TagCor = "#00E676",
			VideoUrl = "https://www.youtube.com/@dluzgames",
			IconeSimbolo = "Flash24"
		});

		Tutoriais.Add(new TutorialItem
		{
			Id = "depuracao-usb",
			Numero = "02",
			Titulo = "Ativando a Depuração USB (Xiaomi, Samsung, Motorola, Realme)",
			Descricao = "Como desbloquear as Opções do Desenvolvedor e ativar a Depuração USB em qualquer marca de celular Android.",
			Duracao = "03:15",
			Categoria = "CONEXÃO",
			TagCor = "#2979FF",
			VideoUrl = "https://www.youtube.com/@dluzgames",
			IconeSimbolo = "PlugConnected24"
		});

		Tutoriais.Add(new TutorialItem
		{
			Id = "dluzstacks-mapeador",
			Numero = "03",
			Titulo = "Modo DLuzStacks: Mapeamento de Teclas no Free Fire / FPS",
			Descricao = "Aprenda a usar o mapeador estilo emulador no PC, posicionar os botões na tela, suspender o mouse (F1/Alt) e atirar.",
			Duracao = "04:45",
			Categoria = "MAPEAÇÃO",
			TagCor = "#FF9100",
			VideoUrl = "https://www.youtube.com/@dluzgames",
			IconeSimbolo = "Desktop24"
		});

		Tutoriais.Add(new TutorialItem
		{
			Id = "sensibilidade-capa",
			Numero = "04",
			Titulo = "Ajustando Sensibilidade X, Y (Puxada de Capa) & 1000Hz",
			Descricao = "Como calibrar a mira lisa, puxada de capa facilitada no Free Fire e ativar o Polling Rate de 1000Hz (1ms de resposta).",
			Duracao = "03:50",
			Categoria = "SENSIBILIDADE",
			TagCor = "#E040FB",
			VideoUrl = "https://www.youtube.com/@dluzgames",
			IconeSimbolo = "Cursor24"
		});

		Tutoriais.Add(new TutorialItem
		{
			Id = "modo-otg",
			Numero = "05",
			Titulo = "Modo Sem Vídeo (OTG) - Controle Total com Zero Delay",
			Descricao = "Como jogar olhando diretamente para a tela do smartphone e controlando pelo teclado e mouse físicos do PC com 0ms puro.",
			Duracao = "02:50",
			Categoria = "MODO OTG",
			TagCor = "#00E5FF",
			VideoUrl = "https://www.youtube.com/@dluzgames",
			IconeSimbolo = "Keyboard24"
		});

		Tutoriais.Add(new TutorialItem
		{
			Id = "wifi-sem-fio",
			Numero = "06",
			Titulo = "Conexão Sem Fio Wi-Fi 5GHz (Como Parear e Jogar)",
			Descricao = "Aprenda a parear seu celular pela rede Wi-Fi 5GHz para jogar sem precisar de cabos com alta fluidez.",
			Duracao = "03:20",
			Categoria = "WI-FI",
			TagCor = "#76FF03",
			VideoUrl = "https://www.youtube.com/@dluzgames",
			IconeSimbolo = "Wifi124"
		});

		Tutoriais.Add(new TutorialItem
		{
			Id = "audio-microfone",
			Numero = "07",
			Titulo = "Guia de Áudio & Microfone (Discord vs Call do Jogo)",
			Descricao = "Qual a melhor configuração para falar com a guilda/squad no Discord com voz limpa ou usar o fone direto no celular.",
			Duracao = "03:05",
			Categoria = "ÁUDIO & VOZ",
			TagCor = "#FF5252",
			VideoUrl = "https://www.youtube.com/@dluzgames",
			IconeSimbolo = "Speaker224"
		});

		if (Tutoriais.Count > 0)
		{
			TutorialSelecionado = Tutoriais[0];
		}
	}

	[RelayCommand]
	private void AbrirTutorial(TutorialItem? item)
	{
		if (item == null || string.IsNullOrWhiteSpace(item.VideoUrl)) return;

		try
		{
			ToastService.Mostrar($"🎬 Abrindo aula: {item.Titulo}...", ToastTipo.Info, 2500);
			Process.Start(new ProcessStartInfo
			{
				FileName = item.VideoUrl,
				UseShellExecute = true
			});
		}
		catch (Exception ex)
		{
			ToastService.Mostrar("Não foi possível abrir o navegador: " + ex.Message, ToastTipo.Error);
		}
	}

	[RelayCommand]
	private void AbrirPlaylist()
	{
		try
		{
			ToastService.Mostrar("🎬 Abrindo canal e playlist oficial no YouTube...", ToastTipo.Info, 2500);
			Process.Start(new ProcessStartInfo
			{
				FileName = PlaylistUrl,
				UseShellExecute = true
			});
		}
		catch { }
	}

	[RelayCommand]
	private void AbrirSuporte()
	{
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = "https://wa.me/5581999999999", // Link de suporte ou site
				UseShellExecute = true
			});
		}
		catch { }
	}
}
