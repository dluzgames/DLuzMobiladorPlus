using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using DLuz.Helpers;
using DLuz.Services;

namespace DLuz.ViewModels;

public class ExtrasViewModel : SeccionViewModel, IDisposable
{
	[ObservableProperty]
	private bool _capturandoMod;

	[ObservableProperty]
	private string _modStatus = "";

	[ObservableProperty]
	private string _cursorStatus = "";

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? iniciarCapturaModCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? aplicarCursorCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? resetCursorCommand;

	protected override SeccionPerfil Seccion => SeccionPerfil.Extras;

	public string ModNombre => TeclaMod.NombreBonito(base.S.ShortcutMod);

	public string ModBotonTexto
	{
		get
		{
			if (!CapturandoMod)
			{
				return "Cambiar tecla";
			}
			return "Pulsa una tecla…";
		}
	}

    public string[] FpsModos { get; } = new string[4] { "Não mostrar FPS", "Na janela flutuante", "Sobreposição no scrcpy", "Janela flutuante + sobreposição" };

	public int FpsModoSel
	{
		get
		{
			bool overlayFps = base.S.OverlayFps;
			bool printFps = base.S.PrintFps;
			if (overlayFps)
			{
				if (printFps)
				{
					return 3;
				}
				return 2;
			}
			if (printFps)
			{
				return 1;
			}
			return 0;
		}
		set
		{
			bool flag = ((value == 1 || value == 3) ? true : false);
			bool flag2 = flag;
			flag = (uint)(value - 2) <= 1u;
			bool flag3 = flag;
			if (base.S.PrintFps != flag2)
			{
				base.S.PrintFps = flag2;
			}
			if (base.S.OverlayFps != flag3)
			{
				base.S.OverlayFps = flag3;
			}
			OnPropertyChanged("FpsModoSel");
			OnPropertyChanged("OverlayPosVisible");
			OnPropertyChanged("FpsModoNota");
			OnPropertyChanged("FlotanteDesactivadaNotaVisible");
		}
	}

	public bool OverlayPosVisible => base.S.OverlayFps;

	public bool FlotanteDesactivadaNotaVisible
	{
		get
		{
			if (base.S.PrintFps && base.S.OverlayFps)
			{
				return !base.S.MostrarFlotante;
			}
			return false;
		}
	}

	public string FpsModoNota
	{
		get
		{
			int fpsModoSel = FpsModoSel;
			bool flag;
			switch (fpsModoSel)
			{
			case 0:
            return "Os FPS não serão exibidos durante a sessão.";
			case 1:
			case 3:
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (flag && !base.S.MostrarFlotante)
			{
            return "Para ver os FPS na janela flutuante, ative também a janela flutuante de controle.";
			}
			return fpsModoSel switch
			{
            2 => "A sobreposição aparece sobre a janela do scrcpy ao iniciar a transmissão; a captura de FPS é ativada automaticamente.",
            3 => "Os FPS são exibidos na janela flutuante e na sobreposição sobre o scrcpy.",
            _ => "Os FPS serão exibidos na janela flutuante de controle durante a sessão.",
			};
		}
	}

	public string[] EsquinasOverlay { get; } = new string[4] { "Superior izquierda", "Superior derecha", "Inferior izquierda", "Inferior derecha" };

	public int OverlayEsquinaSel
	{
		get
		{
			if (base.S.OverlayEsquina < 0 || base.S.OverlayEsquina >= 4)
			{
				return 2;
			}
			return base.S.OverlayEsquina;
		}
		set
		{
			if (value != base.S.OverlayEsquina)
			{
				base.S.OverlayEsquina = value;
			}
			OnPropertyChanged("OverlayEsquinaSel");
		}
	}

	public bool FreeResizeHabilitado => base.S.Video;

	public string FreeResizeDesc
	{
		get
		{
			if (!base.S.Video)
			{
				return "Disponible solo cuando el video está activado";
			}
        return "Permite redimensionar a janela para qualquer proporção. Pode exibir bordas ou preenchimento quando o formato não coincidir com a tela do celular.";
		}
	}

	public Brush ColorPreviewBrush
	{
		get
		{
			string text = base.S.BackgroundColorHex ?? "";
			if (Regex.IsMatch(text, "^#?[0-9A-Fa-f]{6}$"))
			{
				try
				{
					return new SolidColorBrush((Color)ColorConverter.ConvertFromString(text.StartsWith("#") ? text : ("#" + text)));
				}
				catch
				{
				}
			}
			return DLuz.Helpers.ResourceHelper.GetBrush("Stex.BgCardBrush");
		}
	}

	public string ColorStatus
	{
		get
		{
			string text = (base.S.BackgroundColorHex ?? "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				return "Sin color personalizado. Se usará el predeterminado de scrcpy.";
			}
			if (!Regex.IsMatch(text, "^#?[0-9A-Fa-f]{6}$"))
			{
            return "Formato inválido. Use uma cor como #222222.";
			}
        return "Cor válida. Será aplicada ao iniciar a transmissão.";
		}
	}

	public string CursorValorTexto
	{
		get
		{
			if (base.S.PointerSpeed != 0)
			{
				return base.S.PointerSpeed.ToString("+0;-0");
			}
			return "0 (default)";
		}
	}

	public bool UltimaVelocidadVisible => base.S.UltimaVelocidadCursor != int.MinValue;

	public string UltimaVelocidadTexto => "Última velocidad aplicada: " + ((base.S.UltimaVelocidadCursor == 0) ? "0 (default)" : base.S.UltimaVelocidadCursor.ToString("+0;-0"));

	public string ComandoPreview
	{
		get
		{
			string text = (ArquitecturaHelper.ModoCompatibilidad ? "x86" : "x86_64");
			ScrcpyConfig scrcpyConfig = base.S.ObtenerConfigActual();
			if (!base.S.ModoDualExperimental || scrcpyConfig.ModoOtg)
			{
				return "scrcpy.exe (" + text + ") " + ScrcpyManager.ConstruirArgumentos(scrcpyConfig);
			}
			List<string> list = new List<string>();
			if (ScrcpyManager.UsaInstanciaVisualExperimental(scrcpyConfig))
			{
				list.Add("scrcpy.exe (" + text + ") visual " + ScrcpyManager.ConstruirArgumentosVisualesExperimental(scrcpyConfig, ""));
			}
			if (ScrcpyManager.UsaInstanciaEntradaExperimental(scrcpyConfig))
			{
				list.Add("scrcpy.exe (" + text + ") entrada " + ScrcpyManager.ConstruirArgumentosEntradaExperimental(scrcpyConfig, ""));
			}
			return string.Join("\n", list);
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool CapturandoMod
	{
		get
		{
			return _capturandoMod;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_capturandoMod, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CapturandoMod);
				_capturandoMod = value;
				OnCapturandoModChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CapturandoMod);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string ModStatus
	{
		get
		{
			return _modStatus;
		}
		[MemberNotNull("_modStatus")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_modStatus, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ModStatus);
				_modStatus = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ModStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string CursorStatus
	{
		get
		{
			return _cursorStatus;
		}
		[MemberNotNull("_cursorStatus")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_cursorStatus, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CursorStatus);
				_cursorStatus = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CursorStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand IniciarCapturaModCommand => iniciarCapturaModCommand ?? (iniciarCapturaModCommand = new RelayCommand(IniciarCapturaMod));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand AplicarCursorCommand => aplicarCursorCommand ?? (aplicarCursorCommand = new AsyncRelayCommand(AplicarCursorAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ResetCursorCommand => resetCursorCommand ?? (resetCursorCommand = new AsyncRelayCommand(ResetCursorAsync));

	public ExtrasViewModel()
	{
		base.S.PropertyChanged += OnSessionChanged;
	}

	[RelayCommand]
	private void IniciarCapturaMod()
	{
		ModStatus = "";
		CapturandoMod = !CapturandoMod;
	}

	public async Task CapturarModAsync(Key key)
	{
		if (!CapturandoMod)
		{
			return;
		}
		if (key == Key.Escape)
		{
			CapturandoMod = false;
			ModStatus = "";
			return;
		}
		(string, string)? tuple = TeclaMod.DesdeKey(key);
		if (!tuple.HasValue)
		{
			(string Token, string Nombre)? oem = TeclaMod.DesdeKeyOem(key);
			if (!oem.HasValue)
			{
            ModStatus = "Essa tecla não pode ser usada como MOD. Tente outra.";
				return;
			}
			ModStatus = "Comprobando tecla…";
			if (!(await ValidarTokenConMotorAsync(oem.Value.Token)))
			{
            ModStatus = "O mecanismo não reconhece \"" + oem.Value.Nombre + "\". Tente outra tecla.";
				return;
			}
			tuple = oem;
		}
		base.S.ShortcutMod = tuple.Value.Item1;
		CapturandoMod = false;
		ModStatus = "✓ Tecla MOD definida: " + tuple.Value.Item2;
		OnPropertyChanged("ModNombre");
	}

	private static async Task<bool> ValidarTokenConMotorAsync(string token)
	{
		_ = 2;
		try
		{
			string rutaScrcpy = ArquitecturaHelper.RutaScrcpy;
			if (string.IsNullOrEmpty(rutaScrcpy) || !File.Exists(rutaScrcpy))
			{
				return false;
			}
			ProcessStartInfo processStartInfo = new ProcessStartInfo
			{
				FileName = rutaScrcpy,
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};
			processStartInfo.ArgumentList.Add("--shortcut-mod=" + token);
			processStartInfo.ArgumentList.Add("--version");
			using Process p = Process.Start(processStartInfo);
			if (p == null)
			{
				return false;
			}
			string err = await p.StandardError.ReadToEndAsync();
			string std = await p.StandardOutput.ReadToEndAsync();
			await p.WaitForExitAsync(new CancellationTokenSource(3000).Token);
			return !(std + err).Contains("ERROR", StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	private void OnSessionChanged(object? sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
		case "MostrarFlotante":
			OnPropertyChanged("FpsModoNota");
			OnPropertyChanged("FlotanteDesactivadaNotaVisible");
			break;
		case "PrintFps":
		case "OverlayFps":
			OnPropertyChanged("FpsModoSel");
			OnPropertyChanged("OverlayPosVisible");
			OnPropertyChanged("FpsModoNota");
			OnPropertyChanged("FlotanteDesactivadaNotaVisible");
			break;
		case "OverlayEsquina":
			OnPropertyChanged("OverlayEsquinaSel");
			break;
		case "Video":
			OnPropertyChanged("FreeResizeHabilitado");
			OnPropertyChanged("FreeResizeDesc");
			break;
		case "BackgroundColorHex":
			OnPropertyChanged("ColorPreviewBrush");
			OnPropertyChanged("ColorStatus");
			break;
		case "PointerSpeed":
			OnPropertyChanged("CursorValorTexto");
			break;
		case "ShortcutMod":
			OnPropertyChanged("ModNombre");
			break;
		case "ModoDebug":
			base.S.GuardarConfig();
			break;
		}
		OnPropertyChanged("ComandoPreview");
	}

	[RelayCommand]
	private async Task AplicarCursorAsync()
	{
		bool item = (await base.S.Adb.AplicarPointerSpeedAsync(base.S.PointerSpeed)).Item1;
		CursorStatus = (item ? "✓ Aplicado" : "⚠ Sem aparelho conectado");
		if (item)
		{
			base.S.UltimaVelocidadCursor = base.S.PointerSpeed;
			base.S.GuardarConfig();
			OnPropertyChanged("UltimaVelocidadVisible");
			OnPropertyChanged("UltimaVelocidadTexto");
		}
		await Task.Delay(2500);
		CursorStatus = "";
	}

	[RelayCommand]
	private async Task ResetCursorAsync()
	{
		base.S.PointerSpeed = 0;
		bool item = (await base.S.Adb.AplicarPointerSpeedAsync(0)).Item1;
		CursorStatus = (item ? "✓ Restablecido a 0" : "Guardado (se aplicará al conectar)");
		await Task.Delay(2500);
		CursorStatus = "";
	}

	public void Dispose()
	{
		base.S.PropertyChanged -= OnSessionChanged;
		DesengancharSeccion();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnCapturandoModChanged(bool value)
	{
		OnPropertyChanged("ModBotonTexto");
	}
}



