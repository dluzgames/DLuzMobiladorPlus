using System.Collections.Generic;

namespace DLuz.Mapper;

public sealed class JoystickConfig
{
	public int CenterX { get; set; } = 200;

	public int CenterY { get; set; } = 800;

	public int Radius { get; set; } = 120;

	public Dictionary<string, int[]> Keys { get; set; } = new Dictionary<string, int[]>();
}

