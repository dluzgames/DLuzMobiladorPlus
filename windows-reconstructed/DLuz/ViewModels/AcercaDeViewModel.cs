using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using DLuz.Helpers;
using DLuz.Services;

namespace DLuz.ViewModels;

public class AcercaDeViewModel : ObservableObject
{
	[ObservableProperty]
	private string _scrcpyVersionNumero = "";

	[ObservableProperty]
	private bool _scrcpyVersionVisible;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand<string>? abrirUrlCommand;

	public string Version => AppInfo.VersionConPrefijo;

	public string Edicion => "Edição principal";

	public string Estado => "Beta";

	public string BuildTexto => "Versão 3.0 Pro";

	public string DescripcionDescargas => "Versiones oficiales del launcher, incluyendo la más reciente y anteriores. Usa siempre la versión más reciente y descarga únicamente desde los enlaces oficiales.";

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string ScrcpyVersionNumero
	{
		get
		{
			return _scrcpyVersionNumero;
		}
		[MemberNotNull("_scrcpyVersionNumero")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_scrcpyVersionNumero, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ScrcpyVersionNumero);
				_scrcpyVersionNumero = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ScrcpyVersionNumero);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ScrcpyVersionVisible
	{
		get
		{
			return _scrcpyVersionVisible;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_scrcpyVersionVisible, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ScrcpyVersionVisible);
				_scrcpyVersionVisible = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ScrcpyVersionVisible);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<string> AbrirUrlCommand => abrirUrlCommand ?? (abrirUrlCommand = new RelayCommand<string>(AbrirUrl));

	public AcercaDeViewModel()
	{
		CargarVersionScrcpyAsync();
	}

	private async Task CargarVersionScrcpyAsync()
	{
		string text = await ScrcpyVersionService.ObtenerAsync();
		if (!string.IsNullOrEmpty(text))
		{
			ScrcpyVersionNumero = text;
			ScrcpyVersionVisible = true;
		}
	}

	[RelayCommand]
	private void AbrirUrl(string url)
	{
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = url,
				UseShellExecute = true
			});
		}
		catch
		{
		}
	}
}

