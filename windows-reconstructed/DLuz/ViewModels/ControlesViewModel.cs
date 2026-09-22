using System;
using DLuz.Services;

namespace DLuz.ViewModels;

public class ControlesViewModel : SeccionViewModel, IDisposable
{
	protected override SeccionPerfil Seccion => SeccionPerfil.Controles;

	public string[] ModosEntrada { get; } = new string[2] { "uhid", "sdk" };

	public string InputModeSel
	{
		get
		{
			return base.S.InputMode;
		}
		set
		{
			if (!(base.S.InputMode == value))
			{
				base.S.InputMode = value;
				if (base.S.TecladoModo != "disabled")
				{
					base.S.TecladoModo = value;
				}
				if (base.S.MouseModo != "disabled")
				{
					base.S.MouseModo = value;
				}
				OnPropertyChanged("InputModeSel");
			}
		}
	}

	public bool TecladoActivo
	{
		get
		{
			return base.S.TecladoModo != "disabled";
		}
		set
		{
			base.S.TecladoModo = (value ? base.S.InputMode : "disabled");
			OnPropertyChanged("TecladoActivo");
		}
	}

	public bool MouseActivo
	{
		get
		{
			return base.S.MouseModo != "disabled";
		}
		set
		{
			base.S.MouseModo = (value ? base.S.InputMode : "disabled");
			OnPropertyChanged("MouseActivo");
		}
	}

	public bool ForwardAllClicks
	{
		get
		{
			return base.S.ForwardAllClicks;
		}
		set
		{
			base.S.ForwardAllClicks = value;
			OnPropertyChanged("ForwardAllClicks");
		}
	}

	public bool GamepadUhid
	{
		get
		{
			return base.S.GamepadModo == "uhid";
		}
		set
		{
			if (value)
			{
				base.S.GamepadPrevTeclado = base.S.TecladoModo;
				base.S.GamepadPrevMouse = base.S.MouseModo;
				base.S.TecladoModo = "disabled";
				base.S.MouseModo = "disabled";
				base.S.GamepadModo = "uhid";
			}
			else
			{
				base.S.TecladoModo = base.S.GamepadPrevTeclado;
				base.S.MouseModo = base.S.GamepadPrevMouse;
				base.S.GamepadModo = "disabled";
			}
			OnPropertyChanged("GamepadUhid");
			OnPropertyChanged("TecladoActivo");
			OnPropertyChanged("MouseActivo");
			OnPropertyChanged("TecladoHabilitado");
			OnPropertyChanged("MouseHabilitado");
			OnPropertyChanged("GamepadInfoVisible");
		}
	}

	public bool TecladoHabilitado => base.S.GamepadModo == "disabled";

	public bool MouseHabilitado => base.S.GamepadModo == "disabled";

	public bool GamepadInfoVisible => base.S.GamepadModo != "disabled";

	public void Dispose()
	{
		DesengancharSeccion();
	}
}

