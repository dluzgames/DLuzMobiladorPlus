using System.Windows.Media;

namespace DLuz.ViewModels;

public class PerfilValor
{
	public string Nombre { get; set; } = "";

	public string Valor { get; set; } = "";

	public Brush Color { get; set; } = Brushes.White;

	public bool EsCheck { get; set; }
}

