using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DLuz.Helpers;

namespace DLuz.Services;

public class PerfilMetaService
{
	public const int MaxPuntos = 3;

	private readonly string _ruta;

	private Dictionary<string, PerfilMeta> _meta = new Dictionary<string, PerfilMeta>(StringComparer.Ordinal);

	private static readonly JsonSerializerOptions _json = new JsonSerializerOptions
	{
		WriteIndented = true
	};

	public PerfilMetaService(string ruta)
	{
		_ruta = ruta;
		Cargar();
	}

	private void Cargar()
	{
		try
		{
			if (!File.Exists(_ruta))
			{
				_meta = new Dictionary<string, PerfilMeta>(StringComparer.Ordinal);
				return;
			}
			Dictionary<string, PerfilMeta> dictionary = JsonSerializer.Deserialize<Dictionary<string, PerfilMeta>>(File.ReadAllText(_ruta), _json);
			_meta = ((dictionary != null) ? new Dictionary<string, PerfilMeta>(dictionary, StringComparer.Ordinal) : new Dictionary<string, PerfilMeta>(StringComparer.Ordinal));
		}
		catch (Exception ex)
		{
			AppLogger.Error("PerfilMetaService.Cargar: fallo leyendo profiles_meta.json", ex);
			_meta = new Dictionary<string, PerfilMeta>(StringComparer.Ordinal);
		}
	}

	private void Guardar()
	{
		try
		{
			string directoryName = Path.GetDirectoryName(_ruta);
			if (!string.IsNullOrEmpty(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			File.WriteAllText(_ruta, JsonSerializer.Serialize(_meta, _json));
		}
		catch (Exception ex)
		{
			AppLogger.Error("PerfilMetaService.Guardar: fallo escribiendo profiles_meta.json", ex);
		}
	}

	private static string Ahora()
	{
		return DateTime.Now.ToString("yyyy-MM-dd HH:mm");
	}

	public void Sincronizar(IEnumerable<string> nombresExistentes)
	{
		HashSet<string> existentes = new HashSet<string>(nombresExistentes, StringComparer.Ordinal);
		bool flag = false;
		foreach (string item in existentes)
		{
			if (!_meta.ContainsKey(item))
			{
				_meta[item] = new PerfilMeta
				{
					Tipo = (PerfilesBase.EsNombreBase(item) ? "base" : "personalizado"),
					Creado = Ahora(),
					Modificado = Ahora()
				};
				flag = true;
			}
		}
		foreach (string item2 in _meta.Keys.Where((string k) => !existentes.Contains(k)).ToList())
		{
			_meta.Remove(item2);
			flag = true;
		}
		if (flag)
		{
			Guardar();
		}
	}

	public PerfilMeta Obtener(string nombre)
	{
		if (_meta.TryGetValue(nombre, out PerfilMeta value))
		{
			return value;
		}
		PerfilMeta perfilMeta = new PerfilMeta
		{
			Tipo = (PerfilesBase.EsNombreBase(nombre) ? "base" : "personalizado"),
			Creado = Ahora(),
			Modificado = Ahora()
		};
		_meta[nombre] = perfilMeta;
		Guardar();
		return perfilMeta;
	}

	public bool EsBase(string nombre)
	{
		if (!_meta.TryGetValue(nombre, out PerfilMeta value))
		{
			return PerfilesBase.EsNombreBase(nombre);
		}
		return value.Tipo == "base";
	}

	public void RegistrarCreacion(string nombre, string origen)
	{
		_meta[nombre] = new PerfilMeta
		{
			Tipo = "personalizado",
			Origen = (origen ?? ""),
			Creado = Ahora(),
			Modificado = Ahora()
		};
		Guardar();
	}

	public void RegistrarGuardado(string nombre, ScrcpyConfig estadoAnterior)
	{
		PerfilMeta perfilMeta = Obtener(nombre);
		perfilMeta.Modificado = Ahora();
		if (estadoAnterior != null)
		{
			perfilMeta.Puntos.Insert(0, new PuntoRestauracion
			{
				Fecha = Ahora(),
				Datos = estadoAnterior
			});
			if (perfilMeta.Puntos.Count > 3)
			{
				perfilMeta.Puntos.RemoveRange(3, perfilMeta.Puntos.Count - 3);
			}
		}
		Guardar();
	}

	public int CantidadPuntos(string nombre)
	{
		if (!_meta.TryGetValue(nombre, out PerfilMeta value))
		{
			return 0;
		}
		return value.Puntos.Count;
	}

	public IReadOnlyList<PuntoRestauracion> Puntos(string nombre)
	{
		if (!_meta.TryGetValue(nombre, out PerfilMeta value))
		{
			return Array.Empty<PuntoRestauracion>();
		}
		return value.Puntos;
	}

	public ScrcpyConfig? PuntoEn(string nombre, int indice)
	{
		if (!_meta.TryGetValue(nombre, out PerfilMeta value) || indice < 0 || indice >= value.Puntos.Count)
		{
			return null;
		}
		return value.Puntos[indice].Datos;
	}

	public void Renombrar(string viejo, string nuevo)
	{
		if (!string.Equals(viejo, nuevo, StringComparison.Ordinal) && _meta.TryGetValue(viejo, out PerfilMeta value))
		{
			_meta.Remove(viejo);
			_meta[nuevo] = value;
			Guardar();
		}
	}

	public void Eliminar(string nombre)
	{
		if (_meta.Remove(nombre))
		{
			Guardar();
		}
	}
}

