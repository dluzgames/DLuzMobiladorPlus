using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;

namespace DLuz.ViewModels;

public class OptToggleRow : ObservableObject
{
	private bool _suppress;

	[ObservableProperty]
	private string _key = "";

	[ObservableProperty]
	private string _titulo = "";

	[ObservableProperty]
	private string _desc = "";

	[ObservableProperty]
	private string _cmdLabel = "";

	[ObservableProperty]
	private bool _habilitado = true;

	public Func<OptToggleRow, Task<bool>>? OnEnable;

	public Func<OptToggleRow, Task<bool>>? OnDisable;

	public Func<OptToggleRow, Task>? OnToggle;

	private bool _isOn;

	public string? Package { get; set; }

	public bool IsOn
	{
		get
		{
			return _isOn;
		}
		set
		{
			if (_isOn != value)
			{
				_isOn = value;
				OnPropertyChanged("IsOn");
				if (!_suppress)
				{
					OnToggle?.Invoke(this);
				}
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string Key
	{
		get
		{
			return _key;
		}
		[MemberNotNull("_key")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_key, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Key);
				_key = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Key);
			}
		}
	}

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

	public void SetSilent(bool value)
	{
		_suppress = true;
		_isOn = value;
		OnPropertyChanged("IsOn");
		_suppress = false;
	}
}

