using System;

namespace DLuz;

public sealed class ScrcpyDualBenchmark
{
	public DateTimeOffset Inicio { get; set; } = DateTimeOffset.Now;

	public string Modo { get; set; } = "";

	public string Serial { get; set; } = "";

	public ScrcpyProcessBenchmark Visual { get; set; } = new ScrcpyProcessBenchmark();

	public ScrcpyProcessBenchmark Entrada { get; set; } = new ScrcpyProcessBenchmark();

	public bool AmbosVivos { get; set; }

	public bool AdbPingOk { get; set; }

	public long AdbPingMs { get; set; }

	public string AdbPingStdout { get; set; } = "";

	public string AdbPingStderr { get; set; } = "";

	public string Error { get; set; } = "";

	public string LogPath { get; set; } = "";
}

