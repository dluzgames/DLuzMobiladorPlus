using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using DLuz.Services;

namespace DLuz.ViewModels;

public class VideoViewModel : SeccionViewModel, IDisposable
{
	[ObservableProperty]
	private string _encoderStatus = "";

	private string _encoderActivo = "";

	private int _selectedEncoderIndex;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? detectarEncodersCommand;

	protected override SeccionPerfil Seccion => SeccionPerfil.Video;

	private RelayCommand? _aplicarCompetitivoCmd;
	public IRelayCommand AplicarCompetitivoCommand => _aplicarCompetitivoCmd ??= new RelayCommand(() =>
		AplicarModoPredefinido("Competitivo USB", 120, 10, 1280, "h264", 0, false));

	private RelayCommand? _aplicarEquilibradoCmd;
	public IRelayCommand AplicarEquilibradoCommand => _aplicarEquilibradoCmd ??= new RelayCommand(() =>
		AplicarModoPredefinido("Equilibrado USB", 90, 12, 1600, "h264", 0, true));

	private RelayCommand? _aplicarQualidadeCmd;
	public IRelayCommand AplicarQualidadeCommand => _aplicarQualidadeCmd ??= new RelayCommand(() =>
		AplicarModoPredefinido("Qualidade USB", 60, 20, 1920, "h264", 0, true));

	private RelayCommand? _aplicarMaximoDesempenhoCmd;
	public IRelayCommand AplicarMaximoDesempenhoCommand => _aplicarMaximoDesempenhoCmd ??= new RelayCommand(() =>
		AplicarModoPredefinido("Latência Zero", 120, 8, 1024, "h264", 0, false));

	private void AplicarModoPredefinido(string nome, int fps, int bitrate, int maxSize, string codec, int buffer, bool audio)
	{
		base.S.Video = true;
		base.S.Fps = fps;
		base.S.Bitrate = bitrate;
		base.S.MaxSize = maxSize;
		base.S.VideoCodec = codec;
		base.S.VideoBuffer = buffer;
		base.S.Audio = audio;
		base.S.PerfilSeleccionado = nome;
		base.S.GuardarConfig();
		ToastService.Mostrar($"⚡ Modo '{nome}' aplicado com sucesso! ({fps} FPS • {bitrate} Mbps • {maxSize}p)", ToastTipo.Exito);
	}

		public string ResAncho => base.S.ResolucionAncho > 0 ? base.S.ResolucionAncho.ToString() : "1080";
	public string ResAlto => base.S.ResolucionAlto > 0 ? base.S.ResolucionAlto.ToString() : "2400";

	private string _resStatus = "Detecta a resolução nativa do aparelho.";
	public string ResStatus
	{
		get => _resStatus;
		set => SetProperty(ref _resStatus, value);
	}

	private AsyncRelayCommand? _detectarResolucionCmd;
	public IAsyncRelayCommand DetectarResolucionCommand => _detectarResolucionCmd ??= new AsyncRelayCommand(DetectarResolucionAsync);

	public async Task DetectarResolucionAsync()
	{
		if (!base.S.HayDispositivo)
		{
			ResStatus = "Conecte seu celular para detectar a resolução.";
			ToastService.Mostrar("Conecte seu celular via cabo USB para detectar a resolução.", ToastTipo.Advertencia);
			return;
		}
		ResStatus = "Detectando resolução...";
		var (flag, resolucionAncho, resolucionAlto, text) = await base.S.Adb.DetectarResolucionAsync();
		if (flag)
		{
			base.S.ResolucionAncho = resolucionAncho;
			base.S.ResolucionAlto = resolucionAlto;
			base.S.ResolucionNativaDetectada = true;
			ResStatus = $"✓ Resolução detectada: {resolucionAncho} × {resolucionAlto}";
			OnPropertyChanged(nameof(ResAncho));
			OnPropertyChanged(nameof(ResAlto));
			ToastService.Mostrar($"✓ Resolução nativa detectada: {resolucionAncho} × {resolucionAlto}", ToastTipo.Exito);
		}
		else
		{
			ResStatus = "⚠ Não foi possível detectar a resolução do aparelho.";
			ToastService.Mostrar("Não foi possível detectar a resolução do aparelho.", ToastTipo.Advertencia);
		}
	}

	public string[] Codecs { get; } = new string[3] { "h264", "h265", "av1" };

	public string[] AudioCodecs { get; } = new string[3] { "opus", "aac", "flac" };

	public string[] ResolucionesDex { get; } = new string[3] { "1280x720", "1920x1080", "2560x1440" };

	public ObservableCollection<RenderOpcion> RenderOpciones { get; } = new ObservableCollection<RenderOpcion>();

	public ObservableCollection<string> EncoderLabels { get; } = new ObservableCollection<string>();

	public bool DexHabilitado
	{
		get
		{
			if (!base.S.ModoDualExperimental)
			{
				return !base.S.ModoOtg;
			}
			return false;
		}
	}

	public bool DexBloqueadoAvisoVisible => !DexHabilitado;

	public bool MaxSizeHabilitado => !base.S.PantallaVirtualDex;

	public bool AvisoMaxSizeDexVisible => base.S.PantallaVirtualDex;

	public RenderOpcion? SelectedRenderOpcion
	{
		get
		{
			return RenderOpciones.FirstOrDefault((RenderOpcion o) => o.Valor == (base.S.RenderDriver ?? "")) ?? RenderOpciones.FirstOrDefault();
		}
		set
		{
			if (!(value == null) && !(value.Valor == (base.S.RenderDriver ?? "")))
			{
				base.S.RenderDriver = value.Valor;
				OnPropertyChanged("SelectedRenderOpcion");
			}
		}
	}

	public bool CodecHabilitado => !base.S.UseAdvancedEncoder;

	public bool AvisoCodecDesactivadoVisible => base.S.UseAdvancedEncoder;

	public bool EncoderControlesHabilitados => base.S.UseAdvancedEncoder;

	public bool AceleracionHabilitada => base.S.Video;

	public bool AvisoAceleracionSinVideoVisible => !base.S.Video;

	public bool RenderHabilitado => !base.S.AceleracionHardware;

	public bool AvisoRenderAceleracionVisible => base.S.AceleracionHardware;

	public string DescripcionCodec
	{
		get
		{
			string videoCodec = base.S.VideoCodec;
			if (!(videoCodec == "h265"))
			{
				if (videoCodec == "av1")
				{
					return "Menos común, pruébalo si tu equipo lo soporta";
				}
				return "Menor latência e maior compatibilidade";
			}
			return "Melhor qualidade; depende do aparelho";
		}
	}

	public string DescripcionRender
	{
		get
		{
			if (base.S.AceleracionHardware)
			{
				return "Controlado pela aceleração por hardware: DirectX 11 conectado diretamente ao decodificador.";
			}
			return base.S.RenderDriver switch
			{
				"direct3d12" => "Para GPUs modernas. Latência semelhante ao DirectX 11 no scrcpy.", 
				"gpu" => "Nova API do SDL3. Experimental; usa DirectX 12 internamente.", 
				"opengl" => "Útil se o DirectX falhar. Maior latência no Windows. Indisponível no modo 32 bits.", 
				"opengles2" => "Compatibilidade com hardware antigo via ANGLE. Indisponível no modo 32 bits.", 
				"direct3d" => "Máxima compatibilidade com GPUs antigas. Use se o DirectX 11 falhar.", 
				"software" => "Renderiza pela CPU. Maior consumo e latência. Use se as outras opções falharem.", 
				_ => "Recomendado. Melhor equilíbrio entre latência e compatibilidade no Windows.", 
			};
		}
	}

	public string EncoderActivo
	{
		get
		{
			if (!string.IsNullOrEmpty(_encoderActivo))
			{
				return _encoderActivo;
			}
			return CalcularEncoderActivo();
		}
		private set
		{
			_encoderActivo = value;
			OnPropertyChanged("EncoderActivo");
		}
	}

	public int SelectedEncoderIndex
	{
		get
		{
			return _selectedEncoderIndex;
		}
		set
		{
			if (_selectedEncoderIndex == value)
			{
				return;
			}
			_selectedEncoderIndex = value;
			OnPropertyChanged("SelectedEncoderIndex");
			if (value < 0)
			{
				return;
			}
			string text = ((base.S.EncodersDetectados.Count > value) ? base.S.EncodersDetectados[value] : ((value < EncoderLabels.Count) ? EncoderLabels[value] : ""));
			if (!string.IsNullOrWhiteSpace(text) && !text.StartsWith("—") && !text.StartsWith("⏳"))
			{
				if (text.Contains("["))
				{
					text = text.Split(new string[1] { "  [" }, StringSplitOptions.None)[0].Trim();
				}
				if (!string.IsNullOrWhiteSpace(text))
				{
					base.S.VideoEncoder = text;
					ActualizarEncoderActivo();
				}
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string EncoderStatus
	{
		get
		{
			return _encoderStatus;
		}
		[MemberNotNull("_encoderStatus")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_encoderStatus, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.EncoderStatus);
				_encoderStatus = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.EncoderStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand DetectarEncodersCommand => detectarEncodersCommand ?? (detectarEncodersCommand = new AsyncRelayCommand(DetectarEncodersAsync));

	private RelayCommand? _snapdragonCmd;
	public RelayCommand SelecionarSnapdragonCommand => _snapdragonCmd ??= new RelayCommand(() => AplicarEncoderManual("c2.qti.avc.encoder", "Snapdragon (Qualcomm)"));

	private RelayCommand? _mediatekCmd;
	public RelayCommand SelecionarMediaTekCommand => _mediatekCmd ??= new RelayCommand(() => AplicarEncoderManual("c2.mtk.video.encoder.avc", "MediaTek (Dimensity/Helio)"));

	private RelayCommand? _exynosCmd;
	public RelayCommand SelecionarExynosCommand => _exynosCmd ??= new RelayCommand(() => AplicarEncoderManual("c2.exynos.h264.encoder", "Exynos (Samsung)"));

	private RelayCommand? _unisocCmd;
	public RelayCommand SelecionarUnisocCommand => _unisocCmd ??= new RelayCommand(() => AplicarEncoderManual("c2.unisoc.avc.encoder", "Unisoc"));

	private void AplicarEncoderManual(string encoder, string processador)
	{
		base.S.UseAdvancedEncoder = true;
		base.S.VideoEncoder = encoder;
		base.S.GuardarConfig();

		if (!EncoderLabels.Contains(encoder))
		{
			string label = $"{encoder}  [{processador}] [hw] [H264]";
			EncoderLabels.Insert(0, label);
			SelectedEncoderIndex = 0;
		}
		else
		{
			SelectedEncoderIndex = EncoderLabels.IndexOf(encoder);
		}

		EncoderStatus = $"✓ Codificador '{encoder}' selecionado para processador {processador}!";
		ActualizarEncoderActivo();
		ToastService.Mostrar($"Encoder {processador} selecionado com sucesso!", ToastTipo.Exito);
	}

	public VideoViewModel()
	{
		(string, string)[] array = ArquitecturaHelper.ObtenerOpcionesRender(ArquitecturaHelper.ModoCompatibilidad);
		for (int i = 0; i < array.Length; i++)
		{
			var (texto, valor) = array[i];
			RenderOpciones.Add(new RenderOpcion(texto, valor));
		}
		if (base.S.EncodersDetectados.Count > 0)
		{
			foreach (string encodersDisplayLabel in base.S.EncodersDisplayLabels)
			{
				EncoderLabels.Add(encodersDisplayLabel);
			}
			int num = base.S.EncodersDetectados.IndexOf(base.S.VideoEncoder ?? "");
			_selectedEncoderIndex = ((num >= 0) ? num : 0);
			EncoderStatus = $"✓ {base.S.EncodersDetectados.Count} codificador(es) disponível(is)";
		}
		else if (!string.IsNullOrEmpty(base.S.VideoEncoder))
		{
			EncoderLabels.Add($"{base.S.VideoEncoder}  [{InferirTipo(base.S.VideoEncoder)}] [{InferirCodec(base.S.VideoEncoder)}]");
			_selectedEncoderIndex = 0;
			EncoderStatus = "Pressione Detectar para ver todos os codificadores disponíveis";
		}
		else
		{
			EncoderLabels.Add("— Presiona Detectar —");
			_selectedEncoderIndex = 0;
		}
		base.S.PropertyChanged += OnSessionChanged;
	}

	private void OnSessionChanged(object? sender, PropertyChangedEventArgs e)
	{
		string propertyName = e.PropertyName;
		if (propertyName == null)
		{
			return;
		}
		switch (propertyName.Length)
		{
		default:
			return;
		case 18:
			switch (propertyName[0])
			{
			case 'U':
				if (propertyName == "UseAdvancedEncoder")
				{
					OnPropertyChanged("CodecHabilitado");
					OnPropertyChanged("AvisoCodecDesactivadoVisible");
					OnPropertyChanged("EncoderControlesHabilitados");
					ActualizarEncoderActivo();
				}
				break;
			case 'P':
				if (propertyName == "PantallaVirtualDex")
				{
					OnPropertyChanged("MaxSizeHabilitado");
					OnPropertyChanged("AvisoMaxSizeDexVisible");
				}
				break;
			}
			return;
		case 10:
			if (propertyName == "VideoCodec")
			{
				OnPropertyChanged("DescripcionCodec");
			}
			return;
		case 12:
			if (propertyName == "RenderDriver")
			{
				OnPropertyChanged("DescripcionRender");
				OnPropertyChanged("SelectedRenderOpcion");
			}
			return;
		case 19:
			if (propertyName == "AceleracionHardware")
			{
				OnPropertyChanged("RenderHabilitado");
				OnPropertyChanged("AvisoRenderAceleracionVisible");
				OnPropertyChanged("DescripcionRender");
				base.S.GuardarConfig();
			}
			return;
		case 5:
			if (propertyName == "Video")
			{
				OnPropertyChanged("AceleracionHabilitada");
				OnPropertyChanged("AvisoAceleracionSinVideoVisible");
			}
			return;
		case 20:
			if (!(propertyName == "ModoDualExperimental"))
			{
				return;
			}
			break;
		case 7:
			if (!(propertyName == "ModoOtg"))
			{
				return;
			}
			break;
		}
		OnPropertyChanged("DexHabilitado");
		OnPropertyChanged("DexBloqueadoAvisoVisible");
	}

	private string CalcularEncoderActivo()
	{
		if (!base.S.UseAdvancedEncoder || string.IsNullOrEmpty(base.S.VideoEncoder))
		{
			return "Desativado (usando codec genérico)";
		}
		return "✓ Codificador: " + base.S.VideoEncoder + "   ·   Codec: " + InferirCodec(base.S.VideoEncoder);
	}

	private void ActualizarEncoderActivo()
	{
		EncoderActivo = CalcularEncoderActivo();
	}

	[RelayCommand]
	private async Task DetectarEncodersAsync()
	{
		EncoderStatus = "⏳ Consultando codificadores...";
		var (flag, list, list2, _) = await base.S.Scrcpy.DetectarEncodersAsync();
		if (flag && list.Count > 0)
		{
			base.S.EncodersDetectados = list;
			base.S.EncodersDisplayLabels = list2;
			EncoderLabels.Clear();
			foreach (string item in list2)
			{
				EncoderLabels.Add(item);
			}
			int num = list.IndexOf(base.S.VideoEncoder ?? "");
			SelectedEncoderIndex = ((num >= 0) ? num : 0);
			EncoderStatus = $"✓ {list.Count} codificador(es) detectado(s)";
		}
		else
		{
			EncoderStatus = "⚠ Nenhum codificador; verifique se o aparelho está conectado";
			await DialogService.AdvertenciaAsync("Codificadores não detectados", "Não foi possível detectar os codificadores do aparelho.\n\nAtive o modo de depuração em Opções extras e tente novamente para ver os detalhes do erro.");
		}
	}

	public void Dispose()
	{
		base.S.PropertyChanged -= OnSessionChanged;
		DesengancharSeccion();
	}

	private static string InferirTipo(string n)
	{
		if (string.IsNullOrEmpty(n))
		{
			return "sw";
		}
		string text = n.ToLower();
		if (!text.Contains("exynos") && !text.Contains("qcom") && !text.Contains("mtk") && !text.Contains("mediatek") && !text.Contains("mali") && !text.Contains("vendor"))
		{
			return "sw";
		}
		return "hw";
	}

	private static string InferirCodec(string n)
	{
		if (string.IsNullOrEmpty(n))
		{
			return "h264";
		}
		string text = n.ToLower();
		if (text.Contains("hevc") || text.Contains("h265") || text.Contains("h.265"))
		{
			return "h265";
		}
		if (text.Contains("av01") || text.Contains("av1"))
		{
			return "av1";
		}
		return "h264";
	}
}




