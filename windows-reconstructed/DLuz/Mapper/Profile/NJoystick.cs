using System.Collections.Generic;

namespace DLuz.Mapper.Profile;

public sealed class NJoystick
{
	public double Cx { get; set; } = 0.16;

	public double Cy { get; set; } = 0.76;

	public double Radius { get; set; } = 0.044;

	public Dictionary<string, int[]> Keys { get; set; } = new Dictionary<string, int[]>();
}

