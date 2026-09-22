using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DLuz.Mapper.Profile;

public sealed class NProfile
{
	public int Version { get; set; } = 2;

	public string Name { get; set; } = "Free Fire";

	public string ToggleKey { get; set; } = "F1";

	public string ExitKey { get; set; } = "Escape";

	public double OverlayOpacity { get; set; } = 0.85;

	public NJoystick Joystick { get; set; } = new NJoystick();

	public NCamera Camera { get; set; } = new NCamera();

	[JsonConverter(typeof(NButtonListConverter))]
	public List<NButton> Buttons { get; set; } = new List<NButton>();
}

