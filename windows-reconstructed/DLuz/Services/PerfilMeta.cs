using System.Collections.Generic;

namespace DLuz.Services;

public class PerfilMeta
{
	public const string TipoBase = "base";

	public const string TipoPersonalizado = "personalizado";

	public string Tipo { get; set; } = "personalizado";

	public string Origen { get; set; } = "";

	public string Creado { get; set; } = "";

	public string Modificado { get; set; } = "";

	public List<PuntoRestauracion> Puntos { get; set; } = new List<PuntoRestauracion>();
}

