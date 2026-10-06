namespace DLuz.Mapper.Profile;

public sealed class NButton
{
	public string Key { get; set; } = "";

	public double X { get; set; }

	public double Y { get; set; }

	public string Label { get; set; } = "";

	public string? Mode { get; set; }

	public double Size { get; set; }

	public int RepeatMs { get; set; }

	public double X2 { get; set; }

	public double Y2 { get; set; }

	public int DurationMs { get; set; }

	public int AngleDeg { get; set; }

	public string? Script { get; set; }
}
