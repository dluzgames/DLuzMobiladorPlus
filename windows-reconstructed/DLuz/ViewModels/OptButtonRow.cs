using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;

namespace DLuz.ViewModels;

public class OptButtonRow : ObservableObject
{
	[ObservableProperty]
	private string _titulo = "";

	[ObservableProperty]
	private string _desc = "";

	[ObservableProperty]
	private string _cmdLabel = "";

	[ObservableProperty]
	private string _botonTexto = "Ejecutar";

	[ObservableProperty]
	private bool _habilitado = true;

	public string? Package { get; set; }

	public IRelayCommand EjecutarCommand { get; set; }

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string Titulo
	{
		get
		{
			return _titulo;
		}
		[MemberNotNull("_titulo")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_titulo, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Titulo);
				_titulo = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Titulo);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string Desc
	{
		get
		{
			return _desc;
		}
		[MemberNotNull("_desc")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_desc, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Desc);
				_desc = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Desc);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string CmdLabel
	{
		get
		{
			return _cmdLabel;
		}
		[MemberNotNull("_cmdLabel")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_cmdLabel, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CmdLabel);
				_cmdLabel = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CmdLabel);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string BotonTexto
	{
		get
		{
			return _botonTexto;
		}
		[MemberNotNull("_botonTexto")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_botonTexto, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.BotonTexto);
				_botonTexto = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.BotonTexto);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool Habilitado
	{
		get
		{
			return _habilitado;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_habilitado, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Habilitado);
				_habilitado = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Habilitado);
			}
		}
	}
}

