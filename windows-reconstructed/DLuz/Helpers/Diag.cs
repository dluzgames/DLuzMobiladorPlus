namespace DLuz.Helpers;

public static class Diag
{
	public static volatile string CurrentPage = "(arranque)";

	public static volatile string LastAction = "-";

	public static string AppVersion => AppInfo.Version;

	public static string Contexto()
	{
		return $"versión={AppVersion} | canal={"Estable"} | página={CurrentPage} | acción={LastAction}";
	}
}

