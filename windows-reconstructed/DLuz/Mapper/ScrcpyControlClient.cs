using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using DLuz.Helpers;

namespace DLuz.Mapper;

public sealed class ScrcpyControlClient : IDisposable
{
	private const byte MsgTouch = 2;

	public const byte ACTION_DOWN = 0;

	public const byte ACTION_UP = 1;

	public const byte ACTION_MOVE = 2;

	private readonly object _gate = new object();

	private TcpClient? _control;

	private NetworkStream? _controlStream;

	private Thread? _drenajeWorker;

	private Thread? _sendWorker;

	private BlockingCollection<byte[]>? _sendQueue;

	private volatile bool _drenajeActivo;

	private volatile bool _sendActivo;

	public bool Conectado => _controlStream != null;

	public bool Conectar(int puerto, int timeoutMs = 12000)
	{
		DateTime dateTime = DateTime.UtcNow.AddMilliseconds(timeoutMs);
		while (DateTime.UtcNow < dateTime)
		{
			TcpClient tcpClient = null;
			try
			{
				tcpClient = new TcpClient();
				tcpClient.Connect("127.0.0.1", puerto);
				tcpClient.NoDelay = true;
				NetworkStream stream = tcpClient.GetStream();
				tcpClient.ReceiveTimeout = 3000;
				if (stream.ReadByte() < 0)
				{
					tcpClient.Close();
					Thread.Sleep(150);
					continue;
				}
				tcpClient.ReceiveTimeout = 0;
				lock (_gate)
				{
					_control = tcpClient;
					_controlStream = stream;
				}
				IniciarDrenaje(stream);
				IniciarEnvio();
				return true;
			}
			catch
			{
				try
				{
					tcpClient?.Close();
				}
				catch
				{
				}
				Thread.Sleep(150);
			}
		}
		AppLogger.Error("ScrcpyControlClient: canal de control no disponible");
		return false;
	}

	private void IniciarDrenaje(NetworkStream s)
	{
		_drenajeActivo = true;
		_drenajeWorker = new Thread((ThreadStart)delegate
		{
			byte[] array = new byte[4096];
			try
			{
				while (_drenajeActivo && s.Read(array, 0, array.Length) > 0)
				{
				}
			}
			catch
			{
			}
		})
		{
			IsBackground = true,
			Name = "DLuzMapperDrain"
		};
		_drenajeWorker.Start();
	}

	private void IniciarEnvio()
	{
		_sendActivo = true;
		_sendQueue = new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>(), 1024);
		_sendWorker = new Thread(EnviarLoop)
		{
			IsBackground = true,
			Name = "DLuzMapperSender"
		};
		_sendWorker.Start();
	}

	private void EnviarLoop()
	{
		BlockingCollection<byte[]> sendQueue = _sendQueue;
		if (sendQueue == null)
		{
			return;
		}
		while (_sendActivo && !sendQueue.IsCompleted)
		{
			byte[] item;
			try
			{
				if (!sendQueue.TryTake(out item, 100) || item == null)
				{
					continue;
				}
				goto IL_0023;
			}
			catch
			{
				break;
			}
			IL_0023:
			try
			{
				NetworkStream controlStream;
				lock (_gate)
				{
					controlStream = _controlStream;
				}
				if (controlStream == null)
				{
					break;
				}
				controlStream.Write(item, 0, item.Length);
			}
			catch (Exception ex)
			{
				AppLogger.Error("ScrcpyControlClient: envío fallido", ex);
				CerrarSocketControl();
				break;
			}
		}
	}

	public bool EnviarTouch(byte action, long pointerId, int x, int y, int anchoRef, int altoRef, float pressure)
	{
		if (_controlStream == null || _sendQueue == null || _sendQueue.IsAddingCompleted)
		{
			return false;
		}
		byte[] array = new byte[32];
		array[0] = 2;
		array[1] = action;
		BinaryPrimitives.WriteInt64BigEndian(array.AsSpan(2, 8), pointerId);
		BinaryPrimitives.WriteInt32BigEndian(array.AsSpan(10, 4), x);
		BinaryPrimitives.WriteInt32BigEndian(array.AsSpan(14, 4), y);
		BinaryPrimitives.WriteUInt16BigEndian(array.AsSpan(18, 2), (ushort)Math.Clamp(anchoRef, 1, 65535));
		BinaryPrimitives.WriteUInt16BigEndian(array.AsSpan(20, 2), (ushort)Math.Clamp(altoRef, 1, 65535));
		BinaryPrimitives.WriteUInt16BigEndian(array.AsSpan(22, 2), FloatToU16Fp(pressure));
		BinaryPrimitives.WriteUInt32BigEndian(array.AsSpan(24, 4), 0u);
		BinaryPrimitives.WriteUInt32BigEndian(array.AsSpan(28, 4), 0u);
		try
		{
			return _sendQueue.TryAdd(array);
		}
		catch (Exception ex)
		{
			AppLogger.Error("ScrcpyControlClient: cola de envío no disponible", ex);
			return false;
		}
	}

	private static ushort FloatToU16Fp(float value)
	{
		if (value <= 0f)
		{
			return 0;
		}
		if (value >= 1f)
		{
			return ushort.MaxValue;
		}
		return (ushort)(value * 65536f);
	}

	public void Cerrar()
	{
		_drenajeActivo = false;
		_sendActivo = false;
		try
		{
			_sendQueue?.CompleteAdding();
		}
		catch
		{
		}
		if (_sendWorker != null && Thread.CurrentThread != _sendWorker)
		{
			try
			{
				_sendWorker.Join(500);
			}
			catch
			{
			}
		}
		_sendWorker = null;
		try
		{
			_sendQueue?.Dispose();
		}
		catch
		{
		}
		_sendQueue = null;
		CerrarSocketControl();
		_drenajeWorker = null;
	}

	private void CerrarSocketControl()
	{
		lock (_gate)
		{
			try
			{
				_controlStream?.Dispose();
			}
			catch
			{
			}
			try
			{
				_control?.Close();
			}
			catch
			{
			}
			_controlStream = null;
			_control = null;
		}
	}

	public void Dispose()
	{
		Cerrar();
	}
}
