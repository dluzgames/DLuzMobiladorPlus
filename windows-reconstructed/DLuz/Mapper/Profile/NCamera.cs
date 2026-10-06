namespace DLuz.Mapper.Profile;

public sealed class NCamera
{
	public double Cx { get; set; } = 0.78;

	public double Cy { get; set; } = 0.5;

	public double Sensitivity { get; set; } = 1.5;

	public double SensitivityX { get; set; }

	public double SensitivityY { get; set; }

	public double SensitivityRatioY { get; set; } = 1.0;

	public string? FreeMouseKey { get; set; } = "Alt";

	public double Smoothing { get; set; } = 0.1;

	public bool InvertX { get; set; }

	public bool InvertY { get; set; }

	public double ZoneSize { get; set; } = 0.18;
}
