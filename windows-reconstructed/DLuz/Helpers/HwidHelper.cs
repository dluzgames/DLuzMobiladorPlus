using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace DLuz.Helpers;

public static class HwidHelper
{
	private static string? _cachedHwid;

	public static string ObterHwid()
	{
		if (!string.IsNullOrEmpty(_cachedHwid))
		{
			return _cachedHwid;
		}

		try
		{
			StringBuilder sb = new StringBuilder();

			// 1. MachineGuid do Registro (Único por instalação do Windows)
			sb.Append(ObterRegistro(@"SOFTWARE\Microsoft\Cryptography", "MachineGuid"));
			sb.Append(":");

			// 2. Processador Identifier
			sb.Append(ObterRegistro(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString"));
			sb.Append(":");
			sb.Append(ObterRegistro(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0", "Identifier"));
			sb.Append(":");

			// 3. Informações de BIOS / Placa Mãe
			sb.Append(ObterRegistro(@"HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardProduct"));
			sb.Append(":");
			sb.Append(ObterRegistro(@"HARDWARE\DESCRIPTION\System\BIOS", "SystemManufacturer"));
			sb.Append(":");
			sb.Append(ObterRegistro(@"HARDWARE\DESCRIPTION\System\BIOS", "SystemProductName"));
			sb.Append(":");

			// 4. Fallback com variáveis de ambiente do sistema
			sb.Append(Environment.MachineName);
			sb.Append(":");
			sb.Append(Environment.ProcessorCount);

			using SHA256 sha256 = SHA256.Create();
			byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
			string hashHex = BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();

			// Formato amigável e único: DLUZ-XXXX-XXXX-XXXX-XXXX
			_cachedHwid = $"DLUZ-{hashHex.Substring(0, 4)}-{hashHex.Substring(4, 4)}-{hashHex.Substring(8, 4)}-{hashHex.Substring(12, 4)}";
			return _cachedHwid;
		}
		catch
		{
			string fallback = $"{Environment.MachineName}-{Environment.UserName}-{Environment.ProcessorCount}";
			using SHA256 sha256 = SHA256.Create();
			byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(fallback));
			string hashHex = BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();
			_cachedHwid = $"DLUZ-{hashHex.Substring(0, 4)}-{hashHex.Substring(4, 4)}-{hashHex.Substring(8, 4)}-{hashHex.Substring(12, 4)}";
			return _cachedHwid;
		}
	}

	private static string ObterRegistro(string subKey, string valor)
	{
		try
		{
			using RegistryKey? key = Registry.LocalMachine.OpenSubKey(subKey);
			if (key != null)
			{
				object? val = key.GetValue(valor);
				if (val != null)
				{
					return val.ToString()?.Trim() ?? "";
				}
			}
		}
		catch { }
		return "";
	}
}
