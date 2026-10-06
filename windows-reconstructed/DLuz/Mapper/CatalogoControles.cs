using System.Collections.Generic;

namespace DLuz.Mapper;

public static class CatalogoControles
{
	public static readonly IReadOnlyList<TipoControl> Todos = new TipoControl[13]
	{
		new TipoControl("tap", "tap", "Tap spot", Disponible: true),
		new TipoControl("repeat", "repeat", "Repeated tap", Disponible: true),
		new TipoControl("hold", "hold", "Hold", Disponible: true),
		new TipoControl("dpad", "", "D-pad", Disponible: true),
		new TipoControl("aim", "", "Aim and pan", Disponible: true),
		new TipoControl("freelook", "", "Free look", Disponible: true),
		new TipoControl("swipe", "swipe", "Swipe", Disponible: true),
		new TipoControl("zoom", "zoom", "Zoom", Disponible: true),
		new TipoControl("scroll", "scroll", "Scroll", Disponible: true),
		new TipoControl("script", "script", "Script", Disponible: true),
		new TipoControl("mobadpad", "moba", "MOBA D-Pad", Disponible: true),
		new TipoControl("mobaskill", "skill", "MOBA Skill pad", Disponible: true),
		new TipoControl("rotate", "rotate", "Rotate", Disponible: true)
	};

	public static bool EsDosPuntos(string? modo)
	{
		switch (modo)
		{
		case "swipe":
		case "zoom":
		case "scroll":
		case "rotate":
		case "skill":
		case "moba":
			return true;
		default:
			return false;
		}
	}

	public static bool EsRadial(string? modo)
	{
		if (modo == "skill" || modo == "moba")
		{
			return true;
		}
		return false;
	}
}
