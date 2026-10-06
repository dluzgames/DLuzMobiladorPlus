namespace DLuz.Mapper;

public sealed class ButtonConfig
{
	public string Key { get; set; } = "";

	public int X { get; set; }

	public int Y { get; set; }

	public string Label { get; set; } = "";

	public string? Mode { get; set; }

	public double Size { get; set; }

	public int RepeatMs { get; set; }

	public int X2 { get; set; }

	public int Y2 { get; set; }

	public int DurationMs { get; set; }

	public int AngleDeg { get; set; }

	public string? Script { get; set; }
}
