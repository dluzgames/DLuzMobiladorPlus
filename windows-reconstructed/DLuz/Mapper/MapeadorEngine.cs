using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using DLuz.Helpers;

namespace DLuz.Mapper;

public sealed class MapeadorEngine : IDisposable
{
	private readonly string _adbPath;

	private readonly InputState _estado = new InputState();

	private readonly KeyboardHook _kb = new KeyboardHook();

	private readonly MouseSuppressHook _mouseSuppress = new MouseSuppressHook();

	private readonly MouseRawInput _mouse;

	private readonly ScrcpyControlClient _control = new ScrcpyControlClient();

	private readonly ScrcpyServerLauncher _server;

	private readonly TouchInjector _inj;

	private readonly object _injLock = new object();

	private KeymapConfig _keymap;

	private TouchMapper _mapper;

	private HashSet<int> _teclasMapeadas;

	private int _toggleVk = 112;

	private int _exitVk = 27;

	public int PeriodoLoopMs { get; set; } = 1;

	private Thread? _loop;

	private volatile bool _corriendo;

	private volatile bool _capturando;

	private bool _timerFino;

	private string? _serial;

	private int? _freeKeyVk;

	private bool _confinarActual;

	private bool _libreActual;

	private bool _togglePrevAbajo;

	public Func<bool>? TogglePorMousePermitido { get; set; }

	public bool SesionActiva { get; private set; }

	public bool Capturando => _capturando;

	public int Ancho => _inj.Ancho;

	public int Alto => _inj.Alto;

	public event Action<bool>? CapturaCambiada;

	public event Action<bool>? ConfinamientoCambiado;

	public MapeadorEngine(string adbPath, KeymapConfig keymap)
	{
		_adbPath = adbPath;
		_server = new ScrcpyServerLauncher(adbPath);
		_mouse = new MouseRawInput(_estado);
		_inj = new TouchInjector(_control);
		_keymap = keymap;
		ConstruirMapper();
		_kb.Handler = HookHandler;
	}

	public void RecargarKeymap(KeymapConfig keymap)
	{
		_keymap = keymap;
		lock (_injLock)
		{
			ConstruirMapper();
		}
	}

	private void ConstruirMapper()
	{
		_mapper = new TouchMapper(_keymap, _inj);
		_teclasMapeadas = new HashSet<int>(_mapper.TeclasTecladoMapeadas().SelectMany(KeyNames.Variantes));
		_toggleVk = KeyNames.Resolver(_keymap.ToggleKey) ?? 112;
		_exitVk = KeyNames.Resolver(_keymap.ExitKey) ?? 27;
		_freeKeyVk = KeyNames.Resolver(_keymap.Camera.FreeMouseKey);
	}

	public bool ConectarServer(string? serial, int ancho, int alto, string versionServer, int displayId = -1)
	{
		_serial = serial;
		_inj.EstablecerResolucion(ancho, alto);
		bool isWifi = !string.IsNullOrWhiteSpace(serial) && serial.Contains(':');
		PeriodoLoopMs = isWifi ? 3 : 1;
		int num = _server.Iniciar(serial, versionServer, displayId);
		if (num <= 0)
		{
			return false;
		}
		if (!_control.Conectar(num))
		{
			_server.Detener(serial);
			return false;
		}
		return true;
	}

	public void ActivarEntrada()
	{
		_kb.Instalar();
		_mouseSuppress.Instalar();
		_mouse.Iniciar();
		if (!_timerFino)
		{
			try
			{
				timeBeginPeriod(1u);
				_timerFino = true;
			}
			catch
			{
			}
		}
		_corriendo = true;
		_loop = new Thread(Loop)
		{
			IsBackground = true,
			Name = "DLuzMapperLoop"
		};
		_loop.Start();
		SesionActiva = true;
	}

	public void SetCaptura(bool activo)
	{
		if (_capturando != activo)
		{
			_capturando = activo;
			_mouse.Capturar(activo);
			if (activo)
			{
				_mouse.Confinar(activo: true);
				_mouseSuppress.Suprimir(activo: true);
				_confinarActual = true;
				_libreActual = false;
			}
			else
			{
				_confinarActual = false;
				_libreActual = false;
				_mouseSuppress.Suprimir(activo: false);
				// Anti-stuck: ForceReset garante que o analógico e câmera sejam liberados ao soltar o cursor
				try
				{
					lock (_injLock)
					{
						_mapper.ForceReset();
					}
				}
				catch { }
				ReleaseAllInputs();
			}
			this.CapturaCambiada?.Invoke(activo);
			this.ConfinamientoCambiado?.Invoke(_confinarActual);
		}
	}

	public void Detener()
	{
		SetCaptura(activo: false);
		_corriendo = false;
		try
		{
			_loop?.Join(500);
		}
		catch
		{
		}
		_loop = null;
		try
		{
			_mouse.Detener();
		}
		catch
		{
		}
		try
		{
			_mouseSuppress.Desinstalar();
		}
		catch
		{
		}
		try
		{
			_kb.Desinstalar();
		}
		catch
		{
		}
		if (_timerFino)
		{
			try
			{
				timeEndPeriod(1u);
			}
			catch
			{
			}
			_timerFino = false;
		}
		ReleaseAllInputs();
		_control.Cerrar();
		_server.Detener(_serial);
		SesionActiva = false;
	}

	private void Loop()
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		long num = stopwatch.ElapsedMilliseconds;
		while (_corriendo)
		{
			bool flag;
			if (_toggleVk >= 256)
			{
				flag = _estado.Presionada(_toggleVk);
				if (flag && !_togglePrevAbajo)
				{
					if (!_capturando)
					{
						Func<bool>? togglePorMousePermitido = TogglePorMousePermitido;
						if (togglePorMousePermitido != null && !togglePorMousePermitido())
						{
							goto IL_006b;
						}
					}
					SetCaptura(!_capturando);
				}
				goto IL_006b;
			}
			goto IL_0072;
			IL_006b:
			_togglePrevAbajo = flag;
			goto IL_0072;
			IL_0072:
			if (_capturando)
			{
				bool num2 = _freeKeyVk.HasValue && _estado.Presionada(_freeKeyVk.Value);
				bool flag2 = !num2;
				if (flag2 != _confinarActual)
				{
					_mouse.Capturar(flag2);
					_mouse.Confinar(flag2);
					_mouseSuppress.Suprimir(flag2);
					_confinarActual = flag2;
					this.ConfinamientoCambiado?.Invoke(flag2);
				}
				if (num2)
				{
					_estado.TomarDeltaMouse();
					if (!_libreActual)
					{
						ReleaseToquesActivos();
					}
					_libreActual = true;
				}
				else
				{
					_libreActual = false;
					if (_control.Conectado)
					{
						try
						{
							lock (_injLock)
							{
								_mapper.Frame(_estado, stopwatch.ElapsedMilliseconds);
							}
						}
						catch (Exception ex)
						{
							AppLogger.Error("MapeadorEngine: error en frame", ex);
							ReleaseAllInputs();
						}
					}
					else
					{
						SetCaptura(activo: false);
					}
				}
			}
			num += PeriodoLoopMs;
			long num3 = num - stopwatch.ElapsedMilliseconds;
			if (num3 > 1)
			{
				Thread.Sleep((int)(num3 - 1));
			}
			while (stopwatch.ElapsedMilliseconds < num)
			{
				Thread.SpinWait(10);
			}
			if (stopwatch.ElapsedMilliseconds > num + 5)
			{
				num = stopwatch.ElapsedMilliseconds;
			}
		}
	}

	public void CursorTocar(int x, int y)
	{
		if (!SesionActiva || !_control.Conectado)
		{
			return;
		}
		lock (_injLock)
		{
			_inj.Tocar("cursor", x, y);
		}
	}

	public void CursorSoltar()
	{
		lock (_injLock)
		{
			if (_inj.EstaAbajo("cursor"))
			{
				_inj.Soltar("cursor");
			}
		}
	}

	private bool HookHandler(int vk, bool esDown)
	{
		if (esDown && KeyNames.Coincide(vk, _toggleVk))
		{
			SetCaptura(!_capturando);
			return true;
		}
		if (esDown && vk == 9 && (_estado.Presionada(18) || _estado.Presionada(164) || _estado.Presionada(165)))
		{
			SetCaptura(activo: false);
			return false;
		}
		// Tecla de atalho de efeitos DLSS 5 / ReShade
		// Tecla End (VK 35): aciona a tecla F6 (VK 117)
		if (esDown && vk == 35)
		{
			keybd_event(117, 0, 0u, UIntPtr.Zero);
			keybd_event(117, 0, 2u, UIntPtr.Zero);
			return true;
		}

		if (KeyNames.Coincide(vk, _exitVk))
		{
			if (_capturando)
			{
				if (esDown)
				{
					SetCaptura(activo: false);
				}
				return true;
			}
			return false;
		}
		int? freeKeyVk = _freeKeyVk;
		if (freeKeyVk.HasValue)
		{
			int valueOrDefault = freeKeyVk.GetValueOrDefault();
			if (KeyNames.Coincide(vk, valueOrDefault))
			{
				if (esDown)
				{
					_estado.TeclaAbajo(vk);
				}
				else
				{
					_estado.TeclaArriba(vk);
				}
				return false;
			}
		}
		if (esDown)
		{
			_estado.TeclaAbajo(vk);
		}
		else
		{
			_estado.TeclaArriba(vk);
		}
		if (_capturando)
		{
			return _teclasMapeadas.Contains(vk);
		}
		return false;
	}

	private void ReleaseAllInputs()
	{
		_estado.Reiniciar();
		ReleaseToquesActivos();
	}

	private void ReleaseToquesActivos()
	{
		lock (_injLock)
		{
			try
			{
				_mapper.ForceReset();
			}
			catch
			{
			}
			_inj.SoltarTodos();
		}
	}

	[DllImport("user32.dll")]
	private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, nuint dwExtraInfo);

	[DllImport("winmm.dll")]
	private static extern uint timeBeginPeriod(uint uPeriod);


	[DllImport("winmm.dll")]
	private static extern uint timeEndPeriod(uint uPeriod);

	public void Dispose()
	{
		try
		{
			Detener();
		}
		catch
		{
		}
		_kb.Dispose();
		_mouseSuppress.Dispose();
		_mouse.Dispose();
		_control.Dispose();
	}
}



