using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using DLuz.Helpers;

namespace DLuz.Mapper;

public static class MapperDiag
{
	private static readonly ConcurrentQueue<string> Cola = new ConcurrentQueue<string>();

	private static int _drenajeIniciado;

	public static bool Enabled { get; set; }

	public static void Log(string mensaje)
	{
		if (Enabled)
		{
			Cola.Enqueue($"[Mapper {DateTime.Now:HH:mm:ss.fff}] {mensaje}");
			if (Interlocked.CompareExchange(ref _drenajeIniciado, 1, 0) == 0)
			{
				Thread thread = new Thread(Drenar);
				thread.IsBackground = true;
				thread.Name = "DLuzMapperDiag";
				thread.Start();
			}
		}
	}

	private static void Drenar()
	{
		StringBuilder stringBuilder = new StringBuilder();
		while (true)
		{
			Thread.Sleep(200);
			if (Cola.IsEmpty)
			{
				continue;
			}
			stringBuilder.Clear();
			bool flag = true;
			string result;
			while (Cola.TryDequeue(out result))
			{
				if (!flag)
				{
					stringBuilder.Append('\n');
				}
				stringBuilder.Append(result);
				flag = false;
			}
			if (stringBuilder.Length > 0)
			{
				AppLogger.Info(stringBuilder.ToString());
			}
		}
	}
}
