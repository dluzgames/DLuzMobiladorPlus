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
}

