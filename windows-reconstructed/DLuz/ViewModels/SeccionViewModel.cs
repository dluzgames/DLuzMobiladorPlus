using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLuz.Services;

namespace DLuz.ViewModels;

public abstract class SeccionViewModel : ObservableObject
{
	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? guardarCambiosCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? revertirSeccionCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? restaurarPredeterminadoCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? cerrarAvisoCommand;

	public SessionState S => SessionState.Instance;

	protected abstract SeccionPerfil Seccion { get; }

	public string NombreSeccion => Seccion switch
	{
		SeccionPerfil.Video => "Video y Audio", 
		SeccionPerfil.Pantalla => "Pantalla", 
		SeccionPerfil.Extras => "Opciones Extras", 
		_ => "Controles", 
	};

	public bool MostrarBarraCambios
	{
		get
		{
			if (S.SeccionDirty(Seccion))
			{
				return !S.SeccionAvisoOculto(Seccion);
			}
			return false;
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand GuardarCambiosCommand => guardarCambiosCommand ?? (guardarCambiosCommand = new AsyncRelayCommand(GuardarCambiosAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand RevertirSeccionCommand => revertirSeccionCommand ?? (revertirSeccionCommand = new RelayCommand(RevertirSeccion));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand RestaurarPredeterminadoCommand => restaurarPredeterminadoCommand ?? (restaurarPredeterminadoCommand = new RelayCommand(RestaurarPredeterminado));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand CerrarAvisoCommand => cerrarAvisoCommand ?? (cerrarAvisoCommand = new RelayCommand(CerrarAviso));

	protected SeccionViewModel()
	{
		S.PropertyChanged += OnSeccionEstadoChanged;
	}

	private void OnSeccionEstadoChanged(object? sender, PropertyChangedEventArgs e)
	{
		bool flag;
		switch (e.PropertyName)
		{
		case "VideoDirty":
		case "PantallaDirty":
		case "ExtrasDirty":
		case "ControlesDirty":
		case "VideoAvisoOculto":
		case "PantallaAvisoOculto":
		case "ExtrasAvisoOculto":
		case "ControlesAvisoOculto":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			OnPropertyChanged("MostrarBarraCambios");
		}
	}

	[RelayCommand]
	private async Task GuardarCambiosAsync()
	{
		var (flag, text) = S.GuardarSecciones();
		if (flag)
		{
			ToastService.Mostrar("Cambios guardados en el perfil activo.", ToastTipo.Exito, 2500);
		}
		else
		{
			await DialogService.ErrorAsync("Erro ao salvar", "No se pudo guardar:\n" + text);
		}
	}

	[RelayCommand]
	private void RevertirSeccion()
	{
		S.RevertirSeccion(Seccion);
	}

	[RelayCommand]
	private void RestaurarPredeterminado()
	{
		S.RestaurarPredeterminadoSeccion(Seccion);
	}

	[RelayCommand]
	private void CerrarAviso()
	{
		S.CerrarAvisoSeccion(Seccion);
	}

	protected void DesengancharSeccion()
	{
		S.PropertyChanged -= OnSeccionEstadoChanged;
	}
}


