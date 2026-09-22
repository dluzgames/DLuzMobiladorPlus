namespace DLuz;

public class ScrcpyConfig
{
	public bool Video { get; set; } = true;

	public int Fps { get; set; } = 90;

	public int Bitrate { get; set; } = 32;

	public int MaxSize { get; set; } = 1600;

	public int WindowWidth { get; set; }

	public int WindowHeight { get; set; }

	public string VideoCodec { get; set; } = "h264";

	public int VideoBuffer { get; set; }

	public bool AceleracionHardware { get; set; }

	public bool PrintFps { get; set; }

	public bool ForwardAllClicks { get; set; }

	public bool MostrarFlotante { get; set; } = true;

	public bool OverlayFps { get; set; } = true;

	public int OverlayEsquina { get; set; } = 2;

	public bool WmSizeActivo { get; set; }

	public string WmSizeValor { get; set; } = "";

	public bool UseAdvancedEncoder { get; set; }

	public string VideoEncoder { get; set; } = "";

	public bool Audio { get; set; } = true;

	public bool AudioDoble { get; set; }

	public int AudioBuffer { get; set; } = 50;

	public string AudioCodec { get; set; } = "opus";

	public int AudioBitrate { get; set; } = 128;

	public bool DisableScreensaver { get; set; }

	public bool KeepActive { get; set; }

	public bool TurnScreenOff { get; set; }

	public bool FreeWindowResize { get; set; }

	public string BackgroundColorHex { get; set; } = "";

	public string ShortcutMod { get; set; } = "lalt";

	public bool Fullscreen { get; set; }

	public string FullscreenCrop { get; set; } = "";

	public int ResolucionAncho { get; set; } = 1080;

	public int ResolucionAlto { get; set; } = 2400;

	public string AspectRatio { get; set; } = "16:9";

	public int CustomRatioW { get; set; } = 16;

	public int CustomRatioH { get; set; } = 9;

	public int Dpi { get; set; } = 420;

	public string TecladoModo { get; set; } = "uhid";

	public string MouseModo { get; set; } = "uhid";

	public string GamepadModo { get; set; } = "disabled";

	public int PointerSpeed { get; set; }

	public bool ModoOtg { get; set; }

	public string OtgSerial { get; set; } = "";

	public bool UsarWifi { get; set; }

	public string SerialDestino { get; set; } = "";

	public string WifiIp { get; set; } = "";

	public int WifiPuerto { get; set; } = 5555;

	public bool ModoDebug { get; set; }

	public string RenderDriver { get; set; } = "";

	public bool SoloEspejoSinControl { get; set; }

	// Usado pelo Modo DLuzStacks para iniciar a imagem do celular na horizontal.
	public bool ForcarOrientacaoHorizontal { get; set; }

	public string OrientacaoCaptura { get; set; } = "@270";

	public bool PantallaVirtualDex { get; set; }

	public string PantallaVirtualResolucion { get; set; } = "1920x1080";

	public int PantallaVirtualDpi { get; set; }

	public string PantallaVirtualLauncherPackage { get; set; } = "";
}

