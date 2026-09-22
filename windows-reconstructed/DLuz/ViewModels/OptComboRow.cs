using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;

namespace DLuz.ViewModels;

public class OptComboRow : ObservableObject
{
	[ObservableProperty]
	private string _titulo = "";

	[ObservableProperty]
	private string _desc = "";

	[ObservableProperty]
	private string _cmdLabel = "";

	[ObservableProperty]
	private bool _habilitado = true;

	[ObservableProperty]
	private int _selectedIndex;

	public string[] Opciones { get; set; } = Array.Empty<string>();

	public IRelayCommand AplicarCommand { get; set; }

	public IRelayCommand ResetearCommand { get; set; }

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

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int SelectedIndex
	{
		get
		{
			return _selectedIndex;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_selectedIndex, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedIndex);
				_selectedIndex = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedIndex);
			}
		}
	}
}

