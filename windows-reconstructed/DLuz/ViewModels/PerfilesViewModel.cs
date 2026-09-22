using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using DLuz.Helpers;
using DLuz.Services;
using Microsoft.Win32;

namespace DLuz.ViewModels;

public class PerfilesViewModel : ObservableObject
{
	private readonly SessionState _s = SessionState.Instance;

	[ObservableProperty]
	private bool _detalleVisible;

	[ObservableProperty]
	private bool _listaVacia;

	[ObservableProperty]
	private string _nombreEditable = "";

	[ObservableProperty]
	private string _nombreError = "";

	[ObservableProperty]
	private bool _esBase;

	[ObservableProperty]
	private bool _esPersonalizado;

	[ObservableProperty]
	private string _tipoEtiqueta = "";

	[ObservableProperty]
	private string _origenEtiqueta = "";

	[ObservableProperty]
	private bool _origenVisible;

	[ObservableProperty]
	private bool _puedeRestaurarOrigen;

	[ObservableProperty]
	private bool _hayPuntos;

	[ObservableProperty]
	private string _modificadoEtiqueta = "";

	private bool _sincronizandoSeleccion;

	private string? _creacionPendienteNombre;

	private bool _creacionEsDuplicado;

	private string? _selectedPerfil;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? nuevoPerfilCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? crearDesdeCeroCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? duplicarCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? restaurarEsteBaseCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? restaurarBaseCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? revertirCambiosCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand<int>? restaurarPuntoCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? restaurarDesdeOrigenCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? importarCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? exportarListaCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? guardarCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? eliminarCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? exportarCommand;

	public ObservableCollection<string> Perfiles { get; } = new ObservableCollection<string>();

	public ObservableCollection<PerfilValor> Valores { get; } = new ObservableCollection<PerfilValor>();

	public ObservableCollection<PuntoRestauracionItem> PuntosRestauracion { get; } = new ObservableCollection<PuntoRestauracionItem>();

	public bool HayCambios => _s.HayCambiosPerfil;

	public string? SelectedPerfil
	{
		get
		{
			return _selectedPerfil;
		}
		set
		{
			if (_selectedPerfil == value)
			{
				return;
			}
			string selectedPerfil = _selectedPerfil;
			_selectedPerfil = value;
			OnPropertyChanged("SelectedPerfil");
			if (string.IsNullOrEmpty(value))
			{
				DetalleVisible = false;
				return;
			}
			MostrarDetalle(value);
			if (!_sincronizandoSeleccion)
			{
				AplicarSeleccionAsync(value, selectedPerfil);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool DetalleVisible
	{
		get
		{
			return _detalleVisible;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_detalleVisible, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.DetalleVisible);
				_detalleVisible = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.DetalleVisible);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ListaVacia
	{
		get
		{
			return _listaVacia;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_listaVacia, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ListaVacia);
				_listaVacia = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ListaVacia);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string NombreEditable
	{
		get
		{
			return _nombreEditable;
		}
		[MemberNotNull("_nombreEditable")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_nombreEditable, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.NombreEditable);
				_nombreEditable = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.NombreEditable);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string NombreError
	{
		get
		{
			return _nombreError;
		}
		[MemberNotNull("_nombreError")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_nombreError, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.NombreError);
				_nombreError = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.NombreError);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool EsBase
	{
		get
		{
			return _esBase;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_esBase, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.EsBase);
				_esBase = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.EsBase);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool EsPersonalizado
	{
		get
		{
			return _esPersonalizado;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_esPersonalizado, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.EsPersonalizado);
				_esPersonalizado = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.EsPersonalizado);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string TipoEtiqueta
	{
		get
		{
			return _tipoEtiqueta;
		}
		[MemberNotNull("_tipoEtiqueta")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_tipoEtiqueta, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.TipoEtiqueta);
				_tipoEtiqueta = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.TipoEtiqueta);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string OrigenEtiqueta
	{
		get
		{
			return _origenEtiqueta;
		}
		[MemberNotNull("_origenEtiqueta")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_origenEtiqueta, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.OrigenEtiqueta);
				_origenEtiqueta = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.OrigenEtiqueta);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool OrigenVisible
	{
		get
		{
			return _origenVisible;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_origenVisible, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.OrigenVisible);
				_origenVisible = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.OrigenVisible);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool PuedeRestaurarOrigen
	{
		get
		{
			return _puedeRestaurarOrigen;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_puedeRestaurarOrigen, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PuedeRestaurarOrigen);
				_puedeRestaurarOrigen = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PuedeRestaurarOrigen);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool HayPuntos
	{
		get
		{
			return _hayPuntos;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_hayPuntos, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HayPuntos);
				_hayPuntos = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HayPuntos);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string ModificadoEtiqueta
	{
		get
		{
			return _modificadoEtiqueta;
		}
		[MemberNotNull("_modificadoEtiqueta")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_modificadoEtiqueta, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ModificadoEtiqueta);
				_modificadoEtiqueta = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ModificadoEtiqueta);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand NuevoPerfilCommand => nuevoPerfilCommand ?? (nuevoPerfilCommand = new AsyncRelayCommand(NuevoPerfilAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand CrearDesdeCeroCommand => crearDesdeCeroCommand ?? (crearDesdeCeroCommand = new AsyncRelayCommand(CrearDesdeCeroAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand DuplicarCommand => duplicarCommand ?? (duplicarCommand = new AsyncRelayCommand(DuplicarAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand RestaurarEsteBaseCommand => restaurarEsteBaseCommand ?? (restaurarEsteBaseCommand = new AsyncRelayCommand(RestaurarEsteBaseAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand RestaurarBaseCommand => restaurarBaseCommand ?? (restaurarBaseCommand = new AsyncRelayCommand(RestaurarBaseAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand RevertirCambiosCommand => revertirCambiosCommand ?? (revertirCambiosCommand = new AsyncRelayCommand(RevertirCambiosAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<int> RestaurarPuntoCommand => restaurarPuntoCommand ?? (restaurarPuntoCommand = new AsyncRelayCommand<int>(RestaurarPuntoAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand RestaurarDesdeOrigenCommand => restaurarDesdeOrigenCommand ?? (restaurarDesdeOrigenCommand = new AsyncRelayCommand(RestaurarDesdeOrigenAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ImportarCommand => importarCommand ?? (importarCommand = new AsyncRelayCommand(ImportarAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ExportarListaCommand => exportarListaCommand ?? (exportarListaCommand = new AsyncRelayCommand(ExportarListaAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand GuardarCommand => guardarCommand ?? (guardarCommand = new RelayCommand(Guardar));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand EliminarCommand => eliminarCommand ?? (eliminarCommand = new AsyncRelayCommand(EliminarAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ExportarCommand => exportarCommand ?? (exportarCommand = new AsyncRelayCommand(ExportarAsync));

	public PerfilesViewModel()
	{
		RefrescarLista();
		_s.PropertyChanged += OnSessionChanged;
		if (!string.IsNullOrEmpty(_s.PerfilSeleccionado) && Perfiles.Contains(_s.PerfilSeleccionado))
		{
			SelectedPerfil = _s.PerfilSeleccionado;
		}
	}

	private void OnSessionChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == "HayCambiosPerfil")
		{
			OnPropertyChanged("HayCambios");
		}
	}

	private async Task AplicarSeleccionAsync(string nombre, string? anterior)
	{
		bool fueCreacion = _creacionPendienteNombre == nombre;
		bool duplicado = _creacionEsDuplicado;
		bool descartarConfig = false;
		if (fueCreacion)
		{
			_creacionPendienteNombre = null;
		}
		if (nombre == _s.PerfilSeleccionado)
		{
			return;
		}
		if (_s.HayCambiosSinGuardar)
		{
			switch (await DialogService.TresOpcionesAsync("Alterações não salvas", "Há alterações que ainda não foram salvas. O que deseja fazer antes de aplicar o perfil '" + nombre + "'?", "Salvar e continuar", "Descartar e continuar", "Permanecer aqui"))
			{
			case DialogService.TresOpciones.Cancelar:
				_sincronizandoSeleccion = true;
				SelectedPerfil = anterior;
				_sincronizandoSeleccion = false;
				if (fueCreacion)
				{
					ToastService.Mostrar(duplicado ? ("Perfil duplicado: " + nombre) : ("Perfil creado: " + nombre), ToastTipo.Exito);
				}
				return;
			case DialogService.TresOpciones.Primaria:
				_s.GuardarSecciones();
				break;
			default:
				descartarConfig = _s.HayCambiosConfig;
				break;
			}
		}
		ScrcpyConfig scrcpyConfig = _s.Perfiles.ObtenerPerfil(nombre);
		if (scrcpyConfig != null)
		{
			if (descartarConfig)
			{
				_s.RevertirCambiosConfig();
			}
			_s.CargarPerfilEnApp(scrcpyConfig);
			_s.PerfilSeleccionado = nombre;
			_s.GuardarConfig();
			FillValores(scrcpyConfig);
			if (fueCreacion)
			{
				ToastService.Mostrar(duplicado ? ("Perfil duplicado: " + nombre) : ("Perfil creado y aplicado: " + nombre), ToastTipo.Exito);
			}
			else
			{
				ToastService.Mostrar("Perfil aplicado: " + nombre, ToastTipo.Exito);
			}
		}
	}

	private void RefrescarLista()
	{
		Perfiles.Clear();
		foreach (string item in _s.Perfiles.ListarPerfiles())
		{
			Perfiles.Add(item);
		}
		ListaVacia = Perfiles.Count == 0;
		if (ListaVacia)
		{
			DetalleVisible = false;
		}
	}

	private void MostrarDetalle(string nombre)
	{
		ScrcpyConfig scrcpyConfig = _s.Perfiles.ObtenerPerfil(nombre);
		if (scrcpyConfig == null)
		{
			DetalleVisible = false;
			return;
		}
		NombreEditable = nombre;
		NombreError = "";
		FillValores(scrcpyConfig);
		ActualizarMeta(nombre);
		DetalleVisible = true;
	}

	private void ActualizarMeta(string nombre)
	{
		PerfilMeta perfilMeta = _s.PerfilesMeta.Obtener(nombre);
		EsBase = _s.PerfilesMeta.EsBase(nombre);
		EsPersonalizado = !EsBase;
		TipoEtiqueta = (EsBase ? "Oficial" : "Personalizado");
		string text = perfilMeta.Origen ?? "";
		OrigenVisible = EsPersonalizado && !string.IsNullOrWhiteSpace(text);
		OrigenEtiqueta = (OrigenVisible ? ("Origen: " + text) : "");
		PuedeRestaurarOrigen = EsPersonalizado && PerfilesBase.EsNombreBase(text);
		ModificadoEtiqueta = (string.IsNullOrWhiteSpace(perfilMeta.Modificado) ? "" : ("Última modificación: " + perfilMeta.Modificado));
		PuntosRestauracion.Clear();
		IReadOnlyList<PuntoRestauracion> readOnlyList = _s.PerfilesMeta.Puntos(nombre);
		for (int i = 0; i < readOnlyList.Count; i++)
		{
			PuntosRestauracion.Add(new PuntoRestauracionItem
			{
				Indice = i,
				Etiqueta = "Restaurar guardado del " + readOnlyList[i].Fecha
			});
		}
		HayPuntos = PuntosRestauracion.Count > 0;
	}

	private void FillValores(ScrcpyConfig cfg)
	{
		Valores.Clear();
		Brush brush = Rec("Stex.AccentLightBrush");
		Brush brush2 = Rec("Stex.SuccessBrush");
		Brush brush3 = Rec("Stex.ErrorBrush");
		Add("Video", cfg.Video ? "Activo" : "Inactivo", cfg.Video ? brush2 : brush3, cfg.Video);
		Add("Audio", cfg.Audio ? "Activo" : "Inactivo", cfg.Audio ? brush2 : brush3, cfg.Audio);
		Add("FPS", cfg.Fps.ToString(), brush, check: true);
		Add("Bitrate", $"{cfg.Bitrate} Mb", brush, check: true);
		Add("Codec", cfg.VideoCodec ?? "h264", brush, check: true);
		Add("Fullscreen", cfg.Fullscreen ? "Sí" : "No", cfg.Fullscreen ? brush2 : brush3, cfg.Fullscreen);
		Add("Max Size", (cfg.MaxSize > 0) ? cfg.MaxSize.ToString() : "Auto", brush, check: true);
		Add("MOD", TeclaMod.NombreBonito(cfg.ShortcutMod), brush, check: true);
		Add("Conexão", cfg.UsarWifi ? "Wi-Fi" : "USB", brush, check: true);
		Add("Keep Active", cfg.KeepActive ? "Sí" : "No", cfg.KeepActive ? brush2 : brush3, cfg.KeepActive);
		Add("Screen Off", cfg.TurnScreenOff ? "Sí" : "No", cfg.TurnScreenOff ? brush2 : brush3, cfg.TurnScreenOff);
		string text = (((cfg.TecladoModo ?? "uhid") != "disabled") ? (cfg.TecladoModo ?? "uhid") : (((cfg.MouseModo ?? "uhid") != "disabled") ? (cfg.MouseModo ?? "uhid") : "uhid"));
		Add("Input Mode", text.ToUpper(), brush, check: true);
		void Add(string n, string v, Brush c, bool check)
		{
			Valores.Add(new PerfilValor
			{
				Nombre = n,
				Valor = v,
				Color = c,
				EsCheck = check
			});
		}
	}

	[RelayCommand]
	private async Task NuevoPerfilAsync()
	{
		string text = await DialogService.InputAsync("Nuevo perfil desde estado actual", "Nombre del nuevo perfil:", "Ej: Perfil Gaming, Alta Calidad...");
		if (text != null)
		{
			string text2 = ValidarNombre(text, null);
			if (!string.IsNullOrEmpty(text2))
			{
				await DialogService.AdvertenciaAsync("Nombre inválido", text2);
			}
			else if (_s.Perfiles.AgregarPerfil(text, _s.ObtenerConfigActual()).exito)
			{
				_s.PerfilesMeta.RegistrarCreacion(text, "Perfil actual");
				RefrescarLista();
				_creacionPendienteNombre = text;
				_creacionEsDuplicado = false;
				SelectedPerfil = text;
			}
			else
			{
				ToastService.Mostrar("Não foi possível concluir a ação.", ToastTipo.Error);
			}
		}
	}

	[RelayCommand]
	private async Task CrearDesdeCeroAsync()
	{
		string text = await DialogService.InputAsync("Crear perfil desde cero", "Nombre del nuevo perfil:", "Ej: Mi perfil");
		if (text != null)
		{
			string text2 = ValidarNombre(text, null);
			if (!string.IsNullOrEmpty(text2))
			{
				await DialogService.AdvertenciaAsync("Nombre inválido", text2);
			}
			else if (_s.Perfiles.AgregarPerfil(text, new ScrcpyConfig()).exito)
			{
				_s.PerfilesMeta.RegistrarCreacion(text, "Desde cero");
				RefrescarLista();
				_creacionPendienteNombre = text;
				_creacionEsDuplicado = false;
				SelectedPerfil = text;
			}
			else
			{
				ToastService.Mostrar("Não foi possível concluir a ação.", ToastTipo.Error);
			}
		}
	}

	[RelayCommand]
	private async Task DuplicarAsync()
	{
		if (string.IsNullOrEmpty(SelectedPerfil))
		{
			return;
		}
		string original = SelectedPerfil;
		string text = await DialogService.InputAsync("Duplicar «" + original + "»", "Nombre de la copia:", original + " copia");
		if (text == null)
		{
			return;
		}
		string text2 = ValidarNombre(text, null);
		if (!string.IsNullOrEmpty(text2))
		{
			await DialogService.AdvertenciaAsync("Nombre inválido", text2);
			return;
		}
		ScrcpyConfig scrcpyConfig = _s.Perfiles.ObtenerPerfil(original);
		if (scrcpyConfig != null)
		{
			if (_s.Perfiles.AgregarPerfil(text, scrcpyConfig).exito)
			{
				string origen = (EsBase ? original : (_s.PerfilesMeta.Obtener(original).Origen ?? ""));
				_s.PerfilesMeta.RegistrarCreacion(text, origen);
				RefrescarLista();
				_creacionPendienteNombre = text;
				_creacionEsDuplicado = true;
				SelectedPerfil = text;
			}
			else
			{
				ToastService.Mostrar("Não foi possível concluir a ação.", ToastTipo.Error);
			}
		}
	}

	[RelayCommand]
	private async Task RestaurarEsteBaseAsync()
	{
		if (string.IsNullOrEmpty(SelectedPerfil) || !EsBase)
		{
			return;
		}
		string nombre = SelectedPerfil;
		if (!(await DialogService.ConfirmarAsync("Restaurar este perfil base", "Esto restaurará este perfil a sus valores originales.", "Los cambios guardados en este perfil base se reemplazarán. Se creará una copia de seguridad antes de continuar.", "Restaurar", "Cancelar")))
		{
			return;
		}
		if (!HacerBackupSeguro(out string _, out string error))
		{
			await DialogService.ErrorAsync("Erro", "No se pudo respaldar perfiles.ini:\n" + error);
			return;
		}
		string text = PerfilesBase.LeerSeccionEmbebida(nombre);
		if (string.IsNullOrWhiteSpace(text))
		{
			await DialogService.ErrorAsync("Erro", "No se encontró el perfil base «" + nombre + "» embebido.");
			return;
		}
		var (flag, _, _, mensaje) = _s.Perfiles.RestaurarPerfilesBase(text);
		if (!flag)
		{
			await DialogService.ErrorAsync("Error al restaurar", mensaje);
			return;
		}
		RefrescarTrasRestauracion(nombre, _s.PerfilSeleccionado == nombre);
		ToastService.Mostrar("Perfil restaurado: " + nombre, ToastTipo.Advertencia);
	}

	[RelayCommand]
	private async Task RestaurarBaseAsync()
	{
		if (!(await DialogService.ConfirmarAsync("Restaurar perfiles oficiales", "Esto restaurará los 7 perfiles oficiales a sus valores originales.", "Tus perfiles personalizados no se modificarán. Se creará una copia de seguridad antes de continuar.", "Restaurar", "Cancelar")))
		{
			return;
		}
		if (!HacerBackupSeguro(out string _, out string error))
		{
			await DialogService.ErrorAsync("Erro", "No se pudo respaldar perfiles.ini:\n" + error);
			return;
		}
		string text = PerfilesBase.LeerEmbebido();
		if (string.IsNullOrWhiteSpace(text))
		{
			await DialogService.ErrorAsync("Erro", "No se encontraron los perfiles base embebidos.");
			return;
		}
		var (flag, _, _, mensaje) = _s.Perfiles.RestaurarPerfilesBase(text);
		if (!flag)
		{
			await DialogService.ErrorAsync("Error al restaurar", mensaje);
			return;
		}
		RefrescarTrasRestauracion(SelectedPerfil, PerfilesBase.EsNombreBase(_s.PerfilSeleccionado ?? ""));
		ToastService.Mostrar("Perfiles oficiales restaurados", ToastTipo.Advertencia);
	}

	[RelayCommand]
	private async Task RevertirCambiosAsync()
	{
		if (string.IsNullOrEmpty(SelectedPerfil))
		{
			return;
		}
		if (!_s.HayCambiosSinGuardar)
		{
			ToastService.Mostrar("Não há alterações pendentes.");
			return;
		}
		string nombre = SelectedPerfil;
		if (await DialogService.ConfirmarAsync("Desfazer alterações não salvas", "As alterações não salvas de «" + nombre + "» serão descartadas.", "O perfil retornará ao último estado salvo.", "Desfazer", "Cancelar"))
		{
			ScrcpyConfig scrcpyConfig = _s.Perfiles.ObtenerPerfil(nombre);
			if (scrcpyConfig != null)
			{
				_s.CargarPerfilEnApp(scrcpyConfig);
				FillValores(scrcpyConfig);
				ActualizarMeta(nombre);
				ToastService.Mostrar("Cambios revertidos", ToastTipo.Advertencia);
			}
		}
	}

	[RelayCommand]
	private async Task RestaurarPuntoAsync(int indice)
	{
		if (string.IsNullOrEmpty(SelectedPerfil))
		{
			return;
		}
		string nombre = SelectedPerfil;
		IReadOnlyList<PuntoRestauracion> readOnlyList = _s.PerfilesMeta.Puntos(nombre);
		if (indice >= 0 && indice < readOnlyList.Count)
		{
			string fecha = readOnlyList[indice].Fecha;
			ScrcpyConfig cfg = _s.PerfilesMeta.PuntoEn(nombre, indice);
			if (cfg != null && await DialogService.ConfirmarAsync("Restaurar guardado anterior", $"Esto volverá «{nombre}» al guardado del {fecha}.", "Los valores actuales del perfil se reemplazarán.", "Restaurar", "Cancelar"))
			{
				_s.Perfiles.GuardarConfigEnPerfil(nombre, cfg);
				_s.CargarPerfilEnApp(cfg);
				RefrescarTrasRestauracion(nombre);
				ToastService.Mostrar("Guardado anterior restaurado: " + fecha, ToastTipo.Advertencia);
			}
		}
	}

	[RelayCommand]
	private async Task RestaurarDesdeOrigenAsync()
	{
		if (!string.IsNullOrEmpty(SelectedPerfil) && PuedeRestaurarOrigen)
		{
			string nombre = SelectedPerfil;
			string text = _s.PerfilesMeta.Obtener(nombre).Origen ?? "";
			ScrcpyConfig cfg = _s.Perfiles.ObtenerPerfil(text);
			if (cfg == null)
			{
				ToastService.Mostrar("Não foi possível concluir a ação.", ToastTipo.Error);
			}
			else if (await DialogService.ConfirmarAsync("Restaurar desde origen", "Esto reemplazará este perfil personalizado con los valores de su perfil base de origen «" + text + "».", "Tus cambios actuales se reemplazarán.", "Restaurar", "Cancelar"))
			{
				_s.Perfiles.GuardarConfigEnPerfil(nombre, cfg);
				_s.CargarPerfilEnApp(cfg);
				RefrescarTrasRestauracion(nombre);
				ToastService.Mostrar("Perfil restaurado: " + nombre, ToastTipo.Advertencia);
			}
		}
	}

	[RelayCommand]
	private async Task ImportarAsync()
	{
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Title = "Importar Perfil",
			Filter = "Archivos INI (*.ini)|*.ini"
		};
		if (openFileDialog.ShowDialog() == true)
		{
			var (flag, text, text2) = _s.Perfiles.ImportarDesdeArchivo(openFileDialog.FileName);
			if (flag)
			{
				_s.PerfilesMeta.Sincronizar(_s.Perfiles.ListarPerfiles());
				RefrescarLista();
				SelectedPerfil = text;
				ToastService.Mostrar("Perfil importado: " + text);
			}
			else
			{
				await DialogService.ErrorAsync("Error al importar", "Error al importar:\n" + text2);
			}
		}
	}

	[RelayCommand]
	private async Task ExportarListaAsync()
	{
		if (string.IsNullOrEmpty(SelectedPerfil))
		{
			await DialogService.AdvertenciaAsync("Sin selección", "Selecciona un perfil para exportar.");
		}
		else
		{
			await ExportarPerfil(SelectedPerfil);
		}
	}

	[RelayCommand]
	private void Guardar()
	{
		if (string.IsNullOrEmpty(SelectedPerfil))
		{
			return;
		}
		string text = SelectedPerfil;
		string text2 = (NombreEditable ?? "").Trim();
		string text3 = ValidarNombre(text2, text);
		if (!string.IsNullOrEmpty(text3))
		{
			NombreError = text3;
			return;
		}
		NombreError = "";
		if (!_s.HayCambiosPerfil && _s.HayCambiosConfig && text2 == text)
		{
			var (flag, mensaje) = _s.GuardarSecciones();
			if (flag)
			{
				ToastService.Mostrar("Configuración guardada.", ToastTipo.Exito);
			}
			else
			{
				ToastService.Mostrar(mensaje, ToastTipo.Error);
			}
			return;
		}
		if (text2 != text)
		{
			var (flag2, nombreError) = _s.Perfiles.RenombrarPerfil(text, text2);
			if (!flag2)
			{
				NombreError = nombreError;
				return;
			}
			_s.PerfilesMeta.Renombrar(text, text2);
			text = text2;
		}
		ScrcpyConfig scrcpyConfig = _s.Perfiles.ObtenerPerfil(text);
		if (_s.Perfiles.GuardarConfigEnPerfil(text, _s.ObtenerConfigActual()).exito)
		{
			if (scrcpyConfig != null)
			{
				_s.PerfilesMeta.RegistrarGuardado(text, scrcpyConfig);
			}
			RefrescarLista();
			_selectedPerfil = text;
			OnPropertyChanged("SelectedPerfil");
			_s.PerfilSeleccionado = text;
			ScrcpyConfig scrcpyConfig2 = _s.Perfiles.ObtenerPerfil(text);
			if (scrcpyConfig2 != null)
			{
				FillValores(scrcpyConfig2);
			}
			ActualizarMeta(text);
			if (_s.HayCambiosConfig)
			{
				_s.GuardarConfig();
			}
			_s.LimpiarTodasSecciones();
			_s.TomarSnapshot();
			ToastService.Mostrar("Perfil guardado: " + text, ToastTipo.Exito);
		}
		else
		{
			ToastService.Mostrar("Não foi possível concluir a ação.", ToastTipo.Error);
		}
	}

	[RelayCommand]
	private async Task EliminarAsync()
	{
		if (string.IsNullOrEmpty(SelectedPerfil))
		{
			return;
		}
		string nombre = SelectedPerfil;
		if (!((!_s.PerfilesMeta.EsBase(nombre)) ? (await DialogService.ConfirmarAsync("Eliminar perfil", "¿Eliminar el perfil «" + nombre + "»?", "Esta acción es permanente y no se puede deshacer.", "Eliminar", "Cancelar")) : (await DialogService.ConfirmarAsync("Eliminar perfil oficial", "¿Eliminar el perfil oficial «" + nombre + "»?", "Estás eliminando un perfil oficial. Podrás recuperarlo usando «Restaurar perfiles oficiales».", "Eliminar", "Cancelar"))))
		{
			return;
		}
		HacerBackupSeguro(out string _, out string _);
		if (!_s.Perfiles.EliminarPerfil(nombre).exito)
		{
			ToastService.Mostrar("Não foi possível concluir a ação.", ToastTipo.Error);
			return;
		}
		_s.PerfilesMeta.Eliminar(nombre);
		if (_s.PerfilSeleccionado == nombre)
		{
			_s.PerfilSeleccionado = "";
			_s.GuardarConfig();
		}
		SelectedPerfil = null;
		RefrescarLista();
		ToastService.Mostrar("Perfil eliminado: " + nombre, ToastTipo.Peligro);
	}

	[RelayCommand]
	private async Task ExportarAsync()
	{
		if (!string.IsNullOrEmpty(SelectedPerfil))
		{
			await ExportarPerfil(SelectedPerfil);
		}
	}

	private async Task ExportarPerfil(string nombre)
	{
		SaveFileDialog saveFileDialog = new SaveFileDialog
		{
			Title = "Exportar Perfil",
			Filter = "Archivos INI (*.ini)|*.ini",
			FileName = "perfil_" + nombre + ".ini"
		};
		if (saveFileDialog.ShowDialog() == true)
		{
			var (flag, text) = _s.Perfiles.ExportarPerfil(nombre, saveFileDialog.FileName);
			if (flag)
			{
				ToastService.Mostrar("Perfil exportado correctamente");
			}
			else
			{
				await DialogService.ErrorAsync("Error al exportar", "Error al exportar:\n" + text);
			}
		}
	}

	private bool HacerBackupSeguro(out string ruta, out string error)
	{
		string marcaTiempo = DateTime.Now.ToString("yyyyMMdd_HHmmss");
		var (result, text, text2) = _s.Perfiles.HacerBackup(AppPaths.BackupsDir, marcaTiempo);
		_s.Perfiles.HacerBackup();
		ruta = text;
		error = text2;
		return result;
	}

	private void RefrescarTrasRestauracion(string? seleccion, bool recargarActivo = false)
	{
		RefrescarLista();
		if (recargarActivo && !string.IsNullOrEmpty(_s.PerfilSeleccionado))
		{
			ScrcpyConfig scrcpyConfig = _s.Perfiles.ObtenerPerfil(_s.PerfilSeleccionado);
			if (scrcpyConfig != null)
			{
				_s.CargarPerfilEnApp(scrcpyConfig);
			}
		}
		if (!string.IsNullOrEmpty(seleccion) && Perfiles.Contains(seleccion))
		{
			ScrcpyConfig scrcpyConfig2 = _s.Perfiles.ObtenerPerfil(seleccion);
			if (scrcpyConfig2 != null)
			{
				FillValores(scrcpyConfig2);
			}
			ActualizarMeta(seleccion);
		}
	}

	private string ValidarNombre(string nombre, string? nombreActual)
	{
		if (string.IsNullOrWhiteSpace(nombre))
		{
			return "El nombre no puede estar vacío";
		}
		if (nombre.Length > 30)
		{
			return "Máximo 30 caracteres";
		}
		if (Regex.IsMatch(nombre, "[:\\*\\?\"<>\\|/\\\\]"))
		{
			return "Caracteres no permitidos: / \\ : * ? \" < > |";
		}
		if (nombre != nombreActual && _s.Perfiles.ListarPerfiles().Contains(nombre))
		{
			return "Ya existe un perfil con el nombre '" + nombre + "'";
		}
		return "";
	}

	private static Brush Rec(string clave)
	{
		return DLuz.Helpers.ResourceHelper.GetBrush(clave);
	}
}



