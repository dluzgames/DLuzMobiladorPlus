namespace DLuz.Mapper;

public sealed class CameraConfig
{
	public int ZoneX { get; set; } = 800;

	public int ZoneY { get; set; } = 400;

	public double Sensitivity { get; set; } = 1.5;

	public double SensitivityX { get; set; }

	public double SensitivityY { get; set; }

	public double SensitivityRatioY { get; set; } = 1.0;

	public string? FreeMouseKey { get; set; } = "Alt";

	public double Smoothing { get; set; } = 0.0;

	public bool InvertX { get; set; }

	public bool InvertY { get; set; }

	public double ZoneSize { get; set; } = 0.18;

	public double Acceleration { get; set; } = 0.15;

	public double ExponentY { get; set; } = 1.25;
}

