using System.Collections.ObjectModel;

namespace DLuz.ViewModels;

public class OptCard
{
	public string Titulo { get; set; } = "";

	public ObservableCollection<object> Rows { get; } = new ObservableCollection<object>();
}

