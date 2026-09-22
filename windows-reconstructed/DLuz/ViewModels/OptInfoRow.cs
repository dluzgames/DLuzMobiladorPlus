using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;

namespace DLuz.ViewModels;

public class OptInfoRow : ObservableObject
{
	[ObservableProperty]
	private string _titulo = "";

	[ObservableProperty]
	private string _desc = "";

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
}

