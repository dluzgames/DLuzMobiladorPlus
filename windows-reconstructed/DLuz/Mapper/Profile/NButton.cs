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
}

