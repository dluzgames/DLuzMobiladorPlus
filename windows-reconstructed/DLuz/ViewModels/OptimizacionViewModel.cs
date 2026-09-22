using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using DLuz.Services;
using DLuz.Helpers;

namespace DLuz.ViewModels;

public class OptimizacionViewModel : ObservableObject, IDisposable
{
	private readonly SessionState _s = SessionState.Instance;

	private readonly List<OptToggleRow> _toggles = new List<OptToggleRow>();

	private readonly List<OptComboRow> _combos = new List<OptComboRow>();

	private readonly List<OptButtonRow> _buttons = new List<OptButtonRow>();

	private readonly List<OptPackageRow> _packageRows = new List<OptPackageRow>();

	private readonly HashSet<string> _paquetesInstalados = new HashSet<string>(StringComparer.Ordinal);

	private bool _revirtiendoTodo;

	private bool _dispositivoListo;

	private bool _sdkLeido;

	private int _sdk;

	[ObservableProperty]
	private bool _advertenciaVisible;

	[ObservableProperty]
	private bool _noDispositivoVisible;

	private readonly OptInfoRow _deviceInfo = new OptInfoRow
	{
		Titulo = "Aparelho",
		Desc = "Detectando aparelho..."
	};

	private readonly OptInfoRow _compatInfo = new OptInfoRow
	{
		Titulo = "Compatibilidade",
		Desc = ""
	};

	private readonly OptInfoRow _ffInfo = new OptInfoRow
	{
		Titulo = "Jogos detectados",
		Desc = "Validando pacotes..."
	};

	private readonly OptInfoRow _historialInfo = new OptInfoRow
	{
		Titulo = "Histórico local",
		Desc = "Nenhum pacote registrado."
	};

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? revertirTodoCommand;

	public ObservableCollection<OptCard> Cards { get; } = new ObservableCollection<OptCard>();

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool AdvertenciaVisible
	{
		get
		{
			return _advertenciaVisible;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_advertenciaVisible, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AdvertenciaVisible);
				_advertenciaVisible = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AdvertenciaVisible);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool NoDispositivoVisible
	{
		get
		{
			return _noDispositivoVisible;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_noDispositivoVisible, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.NoDispositivoVisible);
				_noDispositivoVisible = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.NoDispositivoVisible);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand RevertirTodoCommand => revertirTodoCommand ?? (revertirTodoCommand = new AsyncRelayCommand(RevertirTodoAsync));

	private string _encoderBenchmarkStatus = "";
	public string EncoderBenchmarkStatus
	{
		get => _encoderBenchmarkStatus;
		set => SetProperty(ref _encoderBenchmarkStatus, value);
	}

	private AsyncRelayCommand? latenciaUltraCommand;
	public IAsyncRelayCommand LatenciaUltraCommand => latenciaUltraCommand ??= new AsyncRelayCommand(AplicarLatenciaUltraAsync);

	private AsyncRelayCommand? testarEncodersCommand;
	public IAsyncRelayCommand TestarEncodersCommand => testarEncodersCommand ??= new AsyncRelayCommand(TestarEncodersAsync);

	private async Task AplicarLatenciaUltraAsync()
	{
		try
		{
			// 1. Configura parâmetros Scrcpy/USB para latência mínima
			_s.VideoCodec = "h264";
			_s.VideoBuffer = 0;
			_s.AudioBuffer = 0;
			_s.Fps = 120;
			_s.Bitrate = 10;
			_s.MaxSize = 1280;
			_s.GuardarConfig();

			// 2. Aplica Zero Delay no Android (escalas de animação = 0)
			await EjecutarOpt("settings put global window_animation_scale 0");
			await EjecutarOpt("settings put global transition_animation_scale 0");
			await EjecutarOpt("settings put global animator_duration_scale 0");

			// 3. Força taxa de quadros máxima para 120Hz
			await EjecutarOpt("settings put system peak_refresh_rate 120");
			await EjecutarOpt("settings put system min_refresh_rate 120");

			ToastService.Mostrar("⚡ Modo Latência Ultra USB ativado! H.264, Buffer 0, 120 FPS e Zero Delay configurados.", ToastTipo.Exito, 4500);
		}
		catch (Exception ex)
		{
			AppLogger.Error("Falha ao aplicar Latência Ultra USB", ex);
			ToastService.Mostrar("Não foi possível aplicar o modo Latência Ultra USB.", ToastTipo.Error);
		}
	}

	private async Task TestarEncodersAsync()
	{
		if (!_s.HayDispositivo)
		{
			ToastService.Mostrar("Conecte um aparelho para testar os encoders.", ToastTipo.Advertencia);
			return;
		}

		EncoderBenchmarkStatus = "Identificando encoders de hardware no aparelho...";
		var (ok, encoders, best) = await _s.Adb.DescobrirMelhoresEncodersAsync();
		if (ok && !string.IsNullOrEmpty(best))
		{
			EncoderBenchmarkStatus = $"✓ Encoder recomendado: {best}\n(Hardware de ultra baixa latência detectado)";
			_s.UseAdvancedEncoder = true;
			_s.VideoEncoder = best;
			_s.GuardarConfig();
			ToastService.Mostrar($"Encoder de hardware '{best}' selecionado automaticamente!", ToastTipo.Exito, 4000);
		}
		else
		{
			EncoderBenchmarkStatus = "Encoders padrão H.264 ativos (compatibilidade máxima).";
			ToastService.Mostrar("Encoders padrão validados com sucesso.", ToastTipo.Info);
		}
	}

	public OptimizacionViewModel()
	{
		ConstruirCards();
		LimpiarEstadosRetirados();
		_s.Adb.OnDispositivoConectado += OnConectado;
		_s.Adb.OnDispositivoDesconectado += OnDesconectado;
		DetectarDispositivoAsync();
	}

	private async Task DetectarDispositivoAsync()
	{
		bool flag = _s.HayDispositivo;
		if (!flag)
		{
			flag = await Task.Run(() => _s.Adb.HayDispositivoConectado());
		}
		bool flag2 = flag;
		NoDispositivoVisible = !flag2;
		if (!flag2)
		{
			_dispositivoListo = false;
			_sdkLeido = false;
			_paquetesInstalados.Clear();
			ActualizarInfoDispositivo("", "", "", "", "");
			ActualizarHabilitados();
			return;
		}
		string release = await GetProp("ro.build.version.release");
		string sdkText = await GetProp("ro.build.version.sdk");
		string manufacturer = await GetProp("ro.product.manufacturer");
		string model = await GetProp("ro.product.model");
		string brand = await GetProp("ro.product.brand");
		_sdkLeido = int.TryParse(sdkText, out _sdk);
		_dispositivoListo = _sdkLeido;
		ActualizarInfoDispositivo(release, sdkText, manufacturer, model, brand);
		await DetectarPaqueteAsync("com.dts.freefireth");
		await DetectarPaqueteAsync("com.dts.freefiremax");
		_ffInfo.Desc = "Free Fire: " + EstadoPaquete("com.dts.freefireth") + " · Free Fire MAX: " + EstadoPaquete("com.dts.freefiremax");
		ActualizarHabilitados();
	}

	private async Task<string> GetProp(string prop)
	{
		var (flag, text, _) = await _s.Adb.EjecutarShellAsync("getprop " + prop);
		return flag ? text.Trim() : "";
	}

	private async Task DetectarPaqueteAsync(string packageName)
	{
		var (flag, text, _) = await _s.Adb.EjecutarShellAsync("pm list packages " + packageName);
		if (flag && text.Contains(packageName, StringComparison.Ordinal))
		{
			_paquetesInstalados.Add(packageName);
		}
		else
		{
			_paquetesInstalados.Remove(packageName);
		}
	}

	private void ActualizarInfoDispositivo(string release, string sdk, string manufacturer, string model, string brand)
	{
		string value = ((!_sdkLeido) ? "Ações avançadas bloqueadas até a leitura do SDK." : ((_sdk >= 34) ? "Android 14 ou superior detectado." : "Modo legado. Algumas funções podem não estar disponíveis."));
		_deviceInfo.Desc = (_sdkLeido ? $"{manufacturer} {model} · Android {release} · SDK {sdk} · {value}" : "Conecte um aparelho e aceite a depuração USB para detectar a versão, o fabricante e o modelo.");
		_compatInfo.Desc = "Compatível principalmente com Android 14, 15, 16 e 17. O resultado depende do fabricante, bateria, temperatura, permissões e suporte real.";
		_ffInfo.Desc = "Free Fire: " + EstadoPaquete("com.dts.freefireth") + " · Free Fire MAX: " + EstadoPaquete("com.dts.freefiremax");
	}

	private string EstadoPaquete(string packageName)
	{
		if (!_paquetesInstalados.Contains(packageName))
		{
			return "não instalado";
		}
		return "instalado";
	}

	private void ActualizarHabilitados()
	{
		foreach (OptToggleRow toggle in _toggles)
		{
			toggle.Habilitado = _dispositivoListo && (toggle.Package == null || _paquetesInstalados.Contains(toggle.Package));
		}
		foreach (OptButtonRow button in _buttons)
		{
			button.Habilitado = _dispositivoListo && (button.Package == null || _paquetesInstalados.Contains(button.Package));
		}
		foreach (OptComboRow combo in _combos)
		{
			combo.Habilitado = _dispositivoListo;
		}
		foreach (OptPackageRow packageRow in _packageRows)
		{
			packageRow.Habilitado = _dispositivoListo;
		}
		ActualizarAvisos();
	}

	private async Task<bool> EjecutarOpt(string cmd, string? mensajeExito = null)
	{
		if (!_dispositivoListo)
		{
			if (!_revirtiendoTodo)
			{
				ToastService.Mostrar("Conecte o celular e aceite a depuração USB antes de aplicar ações ADB.", ToastTipo.Advertencia);
			}
			return false;
		}
		var (flag, text, text2) = await _s.Adb.EjecutarShellAsync(cmd);
		if (flag)
		{
			if (!_revirtiendoTodo && !string.IsNullOrWhiteSpace(mensajeExito))
			{
				ToastService.Mostrar(mensajeExito, ToastTipo.Exito, 2800);
			}
			return true;
		}
		if (!_revirtiendoTodo)
		{
			ToastService.Mostrar(MensajeError(text + text2), ToastTipo.Error);
		}
		return false;
	}

	private static string MensajeError(string output)
	{
		string text = output.ToLowerInvariant();
		if (text.Contains("unknown") || text.Contains("not found"))
		{
			return "O aparelho não reconhece este comando.";
		}
		if (text.Contains("permission") || text.Contains("securityexception"))
		{
			return "O Android ou o fabricante não permite esta alteração por ADB.";
		}
		if (text.Contains("no devices") || text.Contains("offline"))
		{
			return "O ADB não possui um aparelho disponível.";
		}
		return "O Android não aplicou esta alteração. Isso pode depender do fabricante ou das permissões.";
	}

	private OptToggleRow Toggle(string key, string titulo, string desc, string cmdOn, string cmdOff, Func<Task<bool>> on, Func<Task<bool>> off, string? packageName = null)
	{
		OptToggleRow optToggleRow = new OptToggleRow
		{
			Key = key,
			Titulo = titulo,
			Desc = desc,
			CmdLabel = cmdOn,
			Package = packageName,
			OnEnable = (OptToggleRow _) => on(),
			OnDisable = (OptToggleRow _) => off()
		};
		optToggleRow.OnToggle = async delegate(OptToggleRow r)
		{
			if (!_revirtiendoTodo)
			{
				r.Habilitado = false;
				bool wanted = r.IsOn;
				if (!((!wanted) ? (await r.OnDisable(r)) : (await r.OnEnable(r))))
				{
					r.SetSilent(!wanted);
				}
				else
				{
					_s.OptimizacionEstado[r.Key] = wanted;
					_s.GuardarEstadoOptimizacion();
					ActualizarAvisos();
				}
				ActualizarHabilitados();
			}
		};
		optToggleRow.SetSilent(_s.OptimizacionEstado.TryGetValue(key, out var value) && value);
		_toggles.Add(optToggleRow);
		return optToggleRow;
	}

	private OptToggleRow ToggleSimple(string key, string titulo, string desc, string cmdOn, string cmdOff, string? packageName = null)
	{
		return Toggle(key, titulo, desc, cmdOn, cmdOff, () => EjecutarOpt(cmdOn, "Alteração solicitada ao Android."), () => EjecutarOpt(cmdOff, "Restauração solicitada ao Android."), packageName);
	}

	private OptButtonRow Boton(string titulo, string desc, string botonTexto, string cmdLabel, Func<Task<bool>> accion, string? packageName = null)
	{
		OptButtonRow optButtonRow = new OptButtonRow
		{
			Titulo = titulo,
			Desc = desc,
			BotonTexto = botonTexto,
			CmdLabel = cmdLabel,
			Package = packageName
		};
		optButtonRow.EjecutarCommand = new AsyncRelayCommand((Func<Task>)async delegate
		{
			await accion();
		});
		_buttons.Add(optButtonRow);
		return optButtonRow;
	}

	private OptComboRow ComboRefresco()
	{
		OptComboRow row = new OptComboRow
		{
			Titulo = "Preferência de taxa de atualização",
			Desc = "Solicita ao Android uma taxa de atualização preferencial. O fabricante pode ignorá-la.",
			CmdLabel = "settings put system peak_refresh_rate {valor}",
			Opciones = new string[4] { "60", "90", "120", "144" },
			SelectedIndex = 0
		};
		row.AplicarCommand = new AsyncRelayCommand((Func<Task>)async delegate
		{
			string value = row.Opciones[Math.Clamp(row.SelectedIndex, 0, row.Opciones.Length - 1)];
			if (await AplicarRefresco(value))
			{
				_s.OptimizacionEstado["refresh_rate"] = true;
				_s.GuardarEstadoOptimizacion();
				ActualizarAvisos();
			}
		});
		row.ResetearCommand = new AsyncRelayCommand((Func<Task>)async delegate
		{
			if (await RestaurarRefresco())
			{
				_s.OptimizacionEstado["refresh_rate"] = false;
				_s.GuardarEstadoOptimizacion();
				ActualizarAvisos();
			}
		});
		_combos.Add(row);
		return row;
	}

	private OptPackageRow PackageRow(string titulo, string desc, string cmdLabel, string aplicar, string restaurar, Func<string, Task<bool>> on, Func<string, Task<bool>> off)
	{
		OptPackageRow row = new OptPackageRow
		{
			Titulo = titulo,
			Desc = desc,
			CmdLabel = cmdLabel,
			AplicarTexto = aplicar,
			RestaurarTexto = restaurar
		};
		row.ListarCommand = new AsyncRelayCommand(MostrarPaquetesAsync);
		row.AplicarCommand = new AsyncRelayCommand((Func<Task>)async delegate
		{
			string text = row.PackageText.Trim();
			if (!ValidPackage(text))
			{
				ToastService.Mostrar("Digite um pacote Android válido.", ToastTipo.Advertencia);
			}
			else
			{
				await on(text);
			}
		});
		row.RestaurarCommand = new AsyncRelayCommand((Func<Task>)async delegate
		{
			string text = row.PackageText.Trim();
			if (!ValidPackage(text))
			{
				ToastService.Mostrar("Digite um pacote Android válido.", ToastTipo.Advertencia);
			}
			else
			{
				await off(text);
			}
		});
		_packageRows.Add(row);
		return row;
	}

	private void ConstruirCards()
	{
		OptCard optCard = new OptCard
		{
			Titulo = "Estado do aparelho"
		};
		optCard.Rows.Add(_deviceInfo);
		optCard.Rows.Add(_compatInfo);
		optCard.Rows.Add(_ffInfo);
		Cards.Add(optCard);
		OptCard optCard2 = new OptCard
		{
			Titulo = "Limpeza rápida"
		};
		optCard2.Rows.Add(Boton("Limpar cache dos aplicativos", "Solicita ao Android a limpeza de arquivos de cache. Não apaga dados pessoais.", "Limpar", "pm trim-caches 999G", () => EjecutarOpt("pm trim-caches 999G", "Solicitação de limpeza enviada ao Android.")));
		optCard2.Rows.Add(Boton("Liberar processos em segundo plano", "Solicita ao Android que encerre processos em cache ou seguros para finalizar. Não fecha todos os aplicativos visíveis.", "Liberar", "am kill-all", () => EjecutarOpt("am kill-all", "Solicitação enviada ao Android.")));
		Cards.Add(optCard2);
		OptCard optCard3 = new OptCard
		{
			Titulo = "Preparar jogo"
		};
		AgregarJuego(optCard3, "Free Fire", "com.dts.freefireth", "ff");
		AgregarJuego(optCard3, "Free Fire MAX", "com.dts.freefiremax", "ffmax");
		Cards.Add(optCard3);
		OptCard optCard4 = new OptCard
		{
			Titulo = "Pantalla"
		};
		optCard4.Rows.Add(Toggle("animaciones", "Reduzir animações", "Reduz as animações de janelas e transições. Não aumenta o FPS do jogo.", "settings put global window_animation_scale 0", "settings put global window_animation_scale {previo}", AplicarAnimaciones, RestaurarAnimaciones));
		optCard4.Rows.Add(ComboRefresco());
		Cards.Add(optCard4);
		OptCard optCard5 = new OptCard
		{
			Titulo = "Aplicativos e armazenamento"
		};
		optCard5.Rows.Add(_historialInfo);
		optCard5.Rows.Add(PackageRow("Limitar aplicativo em segundo plano", "Restringe um aplicativo secundário escolhido manualmente. Não aplique ao jogo ativo.", "cmd appops set <pacote> RUN_ANY_IN_BACKGROUND deny", "Limitar", "Permitir", LimitarAppAsync, RestaurarAppAsync));
		optCard5.Rows.Add(PackageRow("Desinstalar aplicativo para este usuário", "Desinstala o pacote para o usuário atual quando permitido pelo Android. Confira o pacote antes.", "pm uninstall -k --user 0 <pacote>", "Desinstalar", "Restaurar", DesinstalarPaqueteAsync, RestaurarPaqueteAsync));
		Cards.Add(optCard5);
		OptCard optCard6 = new OptCard
		{
			Titulo = "Experimental"
		};
		optCard6.Rows.Add(ToggleSimple("fixed_perf", "Modo de desempenho fixo experimental", "Ativa um modo usado principalmente para testes. Não equivale ao desempenho máximo sustentado.", "cmd power set-fixed-performance-mode-enabled true", "cmd power set-fixed-performance-mode-enabled false"));
		optCard6.Rows.Add(ToggleSimple("cached_freezer", "Congelar aplicativos em cache", "Solicita ao sistema que congele processos em cache. O fabricante pode ignorar.", "settings put global cached_apps_freezer enabled", "settings put global cached_apps_freezer disabled"));
		Cards.Add(optCard6);
		ActualizarHistorial();
	}

	private void AgregarJuego(OptCard card, string nombre, string packageName, string prefix)
	{
		card.Rows.Add(ToggleSimple(prefix + "_game_mode", nombre + ": modo de desempenho do jogo", "Solicita ao sistema o modo de desempenho para este jogo quando houver suporte.", "cmd game mode performance " + packageName, "cmd game mode standard " + packageName, packageName));
		card.Rows.Add(Toggle(prefix + "_standby", nombre + ": manter como aplicativo ativo", "Coloca o jogo no grupo ativo. Isso não o bloqueia na memória.", "am set-standby-bucket " + packageName + " active", "am set-standby-bucket " + packageName + " working_set", async delegate
		{
			bool ok = await EjecutarOpt("am set-standby-bucket " + packageName + " active", "Solicitud enviada a Android.");
			if (ok)
			{
				await _s.Adb.EjecutarShellAsync("am get-standby-bucket " + packageName);
			}
			return ok;
		}, async () => await EjecutarOpt("am set-standby-bucket " + packageName + " working_set", "Restauración solicitada a Android."), packageName));
		card.Rows.Add(Boton(nombre + ": otimizar compilação", "Solicita ao Android que recompile o jogo. Não garante mais FPS e pode demorar.", "Otimizar", "pm compile -m speed -f " + packageName, () => EjecutarOpt("pm compile -m speed -f " + packageName, "Compilação solicitada ao Android."), packageName));
	}

	private async Task<bool> AplicarAnimaciones()
	{
		await GuardarValorPrevio("anim_window", "settings get global window_animation_scale");
		await GuardarValorPrevio("anim_transition", "settings get global transition_animation_scale");
		await GuardarValorPrevio("anim_duration", "settings get global animator_duration_scale");
		bool a = await EjecutarOpt("settings put global window_animation_scale 0");
		bool b = await EjecutarOpt("settings put global transition_animation_scale 0");
		bool c = await EjecutarOpt("settings put global animator_duration_scale 0");
		if (!(a && b && c))
		{
			await RestaurarAnimaciones();
		}
		if (a && b && c)
		{
			ToastService.Mostrar("Animaciones reducidas.", ToastTipo.Exito, 2500);
		}
		return a && b && c;
	}

	private async Task<bool> RestaurarAnimaciones()
	{
		bool a = await RestaurarSetting("global", "window_animation_scale", "anim_window", "1");
		bool b = await RestaurarSetting("global", "transition_animation_scale", "anim_transition", "1");
		bool flag = await RestaurarSetting("global", "animator_duration_scale", "anim_duration", "1");
		if (a && b && flag && !_revirtiendoTodo)
		{
			ToastService.Mostrar("Animaciones restauradas.", ToastTipo.Exito, 2500);
		}
		return a && b && flag;
	}

	private async Task<bool> AplicarRefresco(string value)
	{
		await GuardarValorPrevio("refresh_peak", "settings get system peak_refresh_rate");
		await GuardarValorPrevio("refresh_min", "settings get system min_refresh_rate");
		bool a = await EjecutarOpt("settings put system peak_refresh_rate " + value);
		bool b = await EjecutarOpt("settings put system min_refresh_rate " + value);
		if (!(a && b))
		{
			await RestaurarRefresco();
		}
		if (a && b)
		{
			ToastService.Mostrar("Preferencia de refresco enviada a Android.", ToastTipo.Exito, 2500);
		}
		return a && b;
	}

	private async Task<bool> RestaurarRefresco()
	{
		bool a = await RestaurarSetting("system", "peak_refresh_rate", "refresh_peak", "");
		bool flag = await RestaurarSetting("system", "min_refresh_rate", "refresh_min", "");
		if (a && flag)
		{
			ToastService.Mostrar("Preferencia de refresco restaurada.", ToastTipo.Exito, 2500);
		}
		return a && flag;
	}

	private async Task GuardarValorPrevio(string key, string cmd)
	{
		string prevKey = "prev_" + key;
		if (!_s.OptimizacionDatos.ContainsKey(prevKey))
		{
			var (flag, text, _) = await _s.Adb.EjecutarShellAsync(cmd);
			if (flag)
			{
				_s.OptimizacionDatos[prevKey] = text.Trim();
			}
		}
	}

	private async Task<bool> RestaurarSetting(string scope, string name, string key, string fallback)
	{
		string value;
		string text = (_s.OptimizacionDatos.TryGetValue("prev_" + key, out value) ? value : fallback);
		string cmd = ((string.IsNullOrWhiteSpace(text) || string.Equals(text, "null", StringComparison.OrdinalIgnoreCase)) ? ("settings delete " + scope + " " + name) : $"settings put {scope} {name} {text}");
		return await EjecutarOpt(cmd);
	}

	private async Task<bool> LimitarAppAsync(string packageName)
	{
		if (EsJuego(packageName))
		{
			ToastService.Mostrar("No apliques esta restricción al juego activo.", ToastTipo.Advertencia);
			return false;
		}
		if (!(await DialogService.ConfirmarAsync("Limitar app", "Aplicar restricción en segundo plano a:\n" + packageName, "", "Limitar", "Cancelar")))
		{
			return false;
		}
		bool num = await EjecutarOpt("cmd appops set " + packageName + " RUN_ANY_IN_BACKGROUND deny", "Restricción solicitada a Android.");
		if (num)
		{
			AgregarDatoLista("appops_restringidos", packageName);
		}
		return num;
	}

	private async Task<bool> RestaurarAppAsync(string packageName)
	{
		bool num = await EjecutarOpt("cmd appops set " + packageName + " RUN_ANY_IN_BACKGROUND allow", "Permiso en segundo plano restaurado.");
		if (num)
		{
			QuitarDatoLista("appops_restringidos", packageName);
		}
		return num;
	}

	private async Task<bool> DesinstalarPaqueteAsync(string packageName)
	{
		if (!(await DialogService.ConfirmarAsync("Desinstalar para este usuario", "No desinstales apps del sistema si no sabes para qué sirven.\n\nPaquete:\n" + packageName, "", "Desinstalar", "Cancelar")))
		{
			return false;
		}
		bool num = await EjecutarOpt("pm uninstall -k --user 0 " + packageName, "Paquete desinstalado para este usuario.");
		if (num)
		{
			AgregarDatoLista("paquetes_desinstalados", packageName);
		}
		return num;
	}

	private async Task<bool> RestaurarPaqueteAsync(string packageName)
	{
		bool num = await EjecutarOpt("pm install-existing " + packageName, "Restauración solicitada a Android.");
		if (num)
		{
			QuitarDatoLista("paquetes_desinstalados", packageName);
		}
		return num;
	}

	private async Task MostrarPaquetesAsync()
	{
		if (!_dispositivoListo)
		{
			ToastService.Mostrar("Conecta tu teléfono antes de listar paquetes.", ToastTipo.Advertencia);
			return;
		}
		var (flag, text, text2) = await _s.Adb.EjecutarShellAsync("pm list packages -3");
		if (!flag)
		{
			ToastService.Mostrar(MensajeError(text + text2), ToastTipo.Error);
			return;
		}
		string text3 = string.Join("\n", (from x in text.Split('\n')
			select x.Trim().Replace("package:", "") into x
			where x.Length > 0
			select x).Take(80));
		if (string.IsNullOrWhiteSpace(text3))
		{
			text3 = "No se encontraron paquetes de usuario.";
		}
		await DialogService.InfoAsync("Apps instaladas", text3);
	}

	private static bool ValidPackage(string packageName)
	{
		return Regex.IsMatch(packageName, "^[a-zA-Z][a-zA-Z0-9_]*(\\.[a-zA-Z0-9_]+)+$");
	}

	private static bool EsJuego(string packageName)
	{
		if (packageName == "com.dts.freefireth" || packageName == "com.dts.freefiremax")
		{
			return true;
		}
		return false;
	}

	private void AgregarDatoLista(string key, string value)
	{
		HashSet<string> hashSet = LeerDatoLista(key);
		hashSet.Add(value);
		_s.OptimizacionDatos[key] = string.Join("|", hashSet.OrderBy((string x) => x));
		_s.GuardarEstadoOptimizacion();
		ActualizarHistorial();
	}

	private void QuitarDatoLista(string key, string value)
	{
		HashSet<string> hashSet = LeerDatoLista(key);
		hashSet.Remove(value);
		_s.OptimizacionDatos[key] = string.Join("|", hashSet.OrderBy((string x) => x));
		_s.GuardarEstadoOptimizacion();
		ActualizarHistorial();
	}

	private HashSet<string> LeerDatoLista(string key)
	{
		if (!_s.OptimizacionDatos.TryGetValue(key, out string value) || string.IsNullOrWhiteSpace(value))
		{
			return new HashSet<string>(StringComparer.Ordinal);
		}
		return value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet<string>(StringComparer.Ordinal);
	}

	private void ActualizarHistorial()
	{
		HashSet<string> hashSet = LeerDatoLista("appops_restringidos");
		HashSet<string> hashSet2 = LeerDatoLista("paquetes_desinstalados");
		string value = ((hashSet.Count == 0) ? "nenhum" : string.Join(", ", hashSet.Take(4)));
		string value2 = ((hashSet2.Count == 0) ? "nenhum" : string.Join(", ", hashSet2.Take(4)));
		_historialInfo.Desc = $"Aplicativos limitados: {value}. Pacotes desinstalados: {value2}.";
	}

	[RelayCommand]
	private async Task RevertirTodoAsync()
	{
		if (!_dispositivoListo)
		{
			ToastService.Mostrar("Conecta tu teléfono antes de revertir.", ToastTipo.Advertencia);
			return;
		}
		List<OptToggleRow> list = _toggles.Where((OptToggleRow t) => t.IsOn).ToList();
		bool value;
		bool refresh = _s.OptimizacionEstado.TryGetValue("refresh_rate", out value) && value;
		if (list.Count == 0 && !refresh)
		{
			ToastService.Mostrar("No hay optimizaciones activas para revertir.");
			return;
		}
		_revirtiendoTodo = true;
		List<string> errores = new List<string>();
		try
		{
			foreach (OptToggleRow row in list)
			{
				bool flag = row.OnDisable != null;
				if (flag)
				{
					flag = await row.OnDisable(row);
				}
				bool flag2 = flag;
				row.SetSilent(!flag2);
				_s.OptimizacionEstado[row.Key] = !flag2;
				if (!flag2)
				{
					errores.Add(row.Titulo);
				}
			}
			if (refresh)
			{
				bool flag3 = await RestaurarRefresco();
				_s.OptimizacionEstado["refresh_rate"] = !flag3;
				if (!flag3)
				{
					errores.Add("Preferencia de tasa de refresco");
				}
			}
			_s.GuardarEstadoOptimizacion();
		}
		finally
		{
			_revirtiendoTodo = false;
			ActualizarAvisos();
		}
		if (errores.Count == 0)
		{
			ToastService.Mostrar("Optimización revertida.", ToastTipo.Exito);
			return;
		}
		await DialogService.AdvertenciaAsync("Revertir optimización", "No se pudieron revertir:\n" + string.Join("\n", errores.Select((string x) => "- " + x)));
	}

	private void LimpiarEstadosRetirados()
	{
		string[] obj = new string[18]
		{
			"ff_bg", "ffmax_bg", "ff_mem", "ffmax_mem", "game_driver", "blur", "vsync", "opengl", "samsung_hd", "samsung_amp",
			"samsung_gos", "samsung_cpu", "miui_opt", "miui_analytics", "pixel_battery", "pixel_smartspace", "pixel_hotword", "pixel_freeze"
		};
		bool flag = false;
		string[] array = obj;
		foreach (string key in array)
		{
			flag |= _s.OptimizacionEstado.Remove(key);
		}
		if (flag)
		{
			_s.GuardarEstadoOptimizacion();
		}
	}

	private void ActualizarAvisos()
	{
		bool value;
		bool flag = _toggles.Any((OptToggleRow t) => t.IsOn) || (_s.OptimizacionEstado.TryGetValue("refresh_rate", out value) && value);
		AdvertenciaVisible = flag && !_dispositivoListo;
		NoDispositivoVisible = !_dispositivoListo;
	}

	private void OnConectado(string serial)
	{
		SessionState.Marshal(delegate
		{
			DetectarDispositivoAsync();
		});
	}

	private void OnDesconectado()
	{
		SessionState.Marshal(delegate
		{
			_dispositivoListo = false;
			_sdkLeido = false;
			_paquetesInstalados.Clear();
			ActualizarInfoDispositivo("", "", "", "", "");
			ActualizarHabilitados();
		});
	}

	public void Dispose()
	{
		_s.Adb.OnDispositivoConectado -= OnConectado;
		_s.Adb.OnDispositivoDesconectado -= OnDesconectado;
	}
}





