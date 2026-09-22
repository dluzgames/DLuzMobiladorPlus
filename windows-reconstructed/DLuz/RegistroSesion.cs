using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using DLuz.Helpers;

namespace DLuz;

public sealed class RegistroSesion
{
	private const int MaxArchivos = 60;

	private readonly object _lock = new object();

	private readonly List<int> _fps = new List<int>();

	private readonly List<double> _cpu = new List<double>();

	private readonly Stopwatch _crono = new Stopwatch();

	private StreamWriter? _writer;

	private Process? _objetivo;

	private TimeSpan _cpuPrevio;

	private double _tPrevio;

	private long _skipTotal;

	private long _inactivas;

	private long _memPico;

	private string _ruta = "";

	public string Ruta
	{
		get
		{
			lock (_lock)
			{
				return _ruta;
			}
		}
	}

	public bool Activo
	{
		get
		{
			lock (_lock)
			{
				return _writer != null;
			}
		}
	}

	private static CultureInfo Inv => CultureInfo.InvariantCulture;

	public void Iniciar(string contexto, Process? objetivo)
	{
		lock (_lock)
		{
			CerrarInterno();
			try
			{
				string text = Path.Combine(AppPaths.LogsDir, "diag");
				Directory.CreateDirectory(text);
				Purgar(text);
				_ruta = Path.Combine(text, $"diag_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
				_writer = new StreamWriter(_ruta, append: false, Encoding.UTF8);
				_writer.WriteLine($"# {"DLuz"} {AppInfo.VersionConPrefijo} | {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
				_writer.WriteLine("# " + contexto);
				_writer.WriteLine("# " + DescribirMotor());
				_writer.WriteLine("# " + DescribirEquipo());
				_writer.WriteLine("t,fps,skip,cpu,mem");
				_writer.Flush();
				_fps.Clear();
				_cpu.Clear();
				_skipTotal = 0L;
				_memPico = 0L;
				_objetivo = objetivo;
				_cpuPrevio = LeerCpu(objetivo);
				_tPrevio = 0.0;
				_crono.Restart();
			}
			catch
			{
				_writer = null;
			}
		}
	}

	public void Registrar(int fps, int skip)
	{
		lock (_lock)
		{
			if (_writer == null)
			{
				return;
			}
			double totalSeconds = _crono.Elapsed.TotalSeconds;
			if (fps > 0)
			{
				_fps.Add(fps);
			}
			else
			{
				_inactivas++;
			}
			_skipTotal += skip;
			double num = -1.0;
			double num2 = -1.0;
			try
			{
				Process objetivo = _objetivo;
				if (objetivo != null && !objetivo.HasExited)
				{
					TimeSpan totalProcessorTime = objetivo.TotalProcessorTime;
					double num3 = totalSeconds - _tPrevio;
					if (num3 > 0.05)
					{
						num = (totalProcessorTime - _cpuPrevio).TotalSeconds / num3 * 100.0;
						if (num < 0.0)
						{
							num = 0.0;
						}
						_cpu.Add(num);
					}
					_cpuPrevio = totalProcessorTime;
					_tPrevio = totalSeconds;
					objetivo.Refresh();
					long workingSet = objetivo.WorkingSet64;
					num2 = (double)workingSet / 1048576.0;
					if (workingSet > _memPico)
					{
						_memPico = workingSet;
					}
				}
			}
			catch
			{
			}
			try
			{
				_writer.WriteLine(string.Join(",", totalSeconds.ToString("F1", Inv), fps.ToString(Inv), skip.ToString(Inv), (num < 0.0) ? "" : num.ToString("F1", Inv), (num2 < 0.0) ? "" : num2.ToString("F0", Inv)));
				_writer.Flush();
			}
			catch
			{
			}
		}
	}

	public void Cerrar()
	{
		lock (_lock)
		{
			CerrarInterno();
		}
	}

	private void CerrarInterno()
	{
		if (_writer == null)
		{
			return;
		}
		try
		{
			if (_fps.Count > 0)
			{
				List<int> list = _fps.OrderBy((int v) => v).ToList();
				double media = _fps.Average();
				double num = Math.Sqrt(_fps.Average((int v) => Math.Pow((double)v - media, 2.0)));
				StreamWriter? writer = _writer;
				string[] obj = new string[11]
				{
					"# resumen",
					$"n={_fps.Count}",
					$"quietas={_inactivas}",
					"dur=" + _crono.Elapsed.TotalSeconds.ToString("F0", Inv),
					"avg=" + media.ToString("F1", Inv),
					$"min={list[0]}",
					$"p1={Percentil(list, 0.01)}",
					$"p5={Percentil(list, 0.05)}",
					null,
					null,
					null
				};
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(4, 1);
				defaultInterpolatedStringHandler.AppendLiteral("max=");
				defaultInterpolatedStringHandler.AppendFormatted(list[list.Count - 1]);
				obj[8] = defaultInterpolatedStringHandler.ToStringAndClear();
				obj[9] = "sd=" + num.ToString("F2", Inv);
				obj[10] = $"skip={_skipTotal}";
				writer.WriteLine(string.Join(",", obj));
			}
			if (_cpu.Count > 0)
			{
				List<double> list2 = _cpu.OrderBy((double v) => v).ToList();
				StreamWriter? writer2 = _writer;
				string[] obj2 = new string[6]
				{
					"# carga",
					$"n={_cpu.Count}",
					"cpu_avg=" + _cpu.Average().ToString("F1", Inv),
					"cpu_p95=" + list2[Math.Min(list2.Count - 1, (int)Math.Ceiling(0.95 * (double)list2.Count) - 1)].ToString("F1", Inv),
					null,
					null
				};
				obj2[4] = "cpu_max=" + list2[list2.Count - 1].ToString("F1", Inv);
				obj2[5] = "mem_pico=" + (_memPico / 1048576).ToString(Inv);
				writer2.WriteLine(string.Join(",", obj2));
			}
			_writer.Flush();
			_writer.Dispose();
		}
		catch
		{
		}
		_writer = null;
		_objetivo = null;
		_fps.Clear();
		_cpu.Clear();
		_inactivas = 0L;
		_crono.Reset();
	}

	private static TimeSpan LeerCpu(Process? proceso)
	{
		try
		{
			if (proceso != null && !proceso.HasExited)
			{
				return proceso.TotalProcessorTime;
			}
		}
		catch
		{
		}
		return TimeSpan.Zero;
	}

	private static int Percentil(List<int> ordenados, double p)
	{
		int num = (int)Math.Ceiling(p * (double)ordenados.Count) - 1;
		if (num < 0)
		{
			num = 0;
		}
		if (num >= ordenados.Count)
		{
			num = ordenados.Count - 1;
		}
		return ordenados[num];
	}

	private static string DescribirMotor()
	{
		try
		{
			FileInfo fileInfo = new FileInfo(ArquitecturaHelper.RutaScrcpy);
			if (!fileInfo.Exists)
			{
				return "motor=?";
			}
			using SHA256 sHA = SHA256.Create();
			using FileStream inputStream = fileInfo.OpenRead();
			string value = Convert.ToHexString(sHA.ComputeHash(inputStream)).Substring(0, 12);
			return $"motor={value} {fileInfo.Length}B {fileInfo.LastWriteTime:yyyyMMdd_HHmm}";
		}
		catch
		{
			return "motor=?";
		}
	}

	private static string DescribirEquipo()
	{
		string value = (ArquitecturaHelper.ModoCompatibilidad ? "x86" : "x86_64");
		return $"os={Environment.OSVersion.Version} cores={Environment.ProcessorCount} bin={value}";
	}

	private static void Purgar(string carpeta)
	{
		try
		{
			foreach (FileInfo item in (from f in new DirectoryInfo(carpeta).GetFiles("diag_*.csv")
				orderby f.CreationTimeUtc descending
				select f).Skip(59).ToList())
			{
				try
				{
					item.Delete();
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
	}
}

