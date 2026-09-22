using System;

namespace DLuz;

public sealed class ScrcpyProcessBenchmark
{
	public string Rol { get; set; } = "";

	public string ComandoSanitizado { get; set; } = "";

	public int? ProcessId { get; set; }

	public bool InicioOk { get; set; }

	public long InicioMs { get; set; }

	public DateTimeOffset? InicioTimestamp { get; set; }

	public long? VentanaMs { get; set; }

	public bool VivoTrasLanzar { get; set; }

	public int? ExitCode { get; set; }

	public string StdoutTail { get; set; } = "";

	public string StderrTail { get; set; } = "";

	public string Error { get; set; } = "";
}

