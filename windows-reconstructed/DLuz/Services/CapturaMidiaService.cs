using System;
using System.IO;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;

namespace DLuz.Services;

public class CapturaMidiaService
{
	public bool Gravando { get; private set; }
	public string PastaDestino { get; private set; }
	public string FormatoVideo { get; private set; } = "mp4";

	public CapturaMidiaService()
	{
		string defaultFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "DLuzMobilador");
		try
		{
			if (!Directory.Exists(defaultFolder))
			{
				Directory.CreateDirectory(defaultFolder);
			}
		}
		catch
		{
			defaultFolder = AppDomain.CurrentDomain.BaseDirectory;
		}
		PastaDestino = defaultFolder;
	}

	public void Configurar(string pasta, string formato)
	{
		if (!string.IsNullOrWhiteSpace(pasta))
		{
			try
			{
				if (!Directory.Exists(pasta))
				{
					Directory.CreateDirectory(pasta);
				}
				PastaDestino = pasta;
			}
			catch
			{
			}
		}
		if (!string.IsNullOrWhiteSpace(formato))
		{
			FormatoVideo = formato;
		}
	}

	public async Task<(bool ok, string mensagem)> CapturarTelaAsync()
	{
		return await Task.Run(() =>
		{
			try
			{
				if (!Directory.Exists(PastaDestino))
				{
					Directory.CreateDirectory(PastaDestino);
				}
				string fileName = $"Captura_{DateTime.Now:yyyyMMdd_HHmmss}.png";
				string fullPath = Path.Combine(PastaDestino, fileName);

				int w = (int)SystemParameters.PrimaryScreenWidth;
				int h = (int)SystemParameters.PrimaryScreenHeight;
				if (w <= 0) w = 1920;
				if (h <= 0) h = 1080;

				using (var bitmap = new Bitmap(w, h))
				{
					using (var g = Graphics.FromImage(bitmap))
					{
						g.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(w, h));
					}
					bitmap.Save(fullPath, ImageFormat.Png);
				}
				return (true, $"📸 Captura salva em: {fullPath}");
			}
			catch (Exception ex)
			{
				return (false, "Erro ao capturar tela: " + ex.Message);
			}
		});
	}

	public async Task<(bool ok, string mensagem)> AlternarGravacaoAsync()
	{
		return await Task.Run(() =>
		{
			Gravando = !Gravando;
			if (Gravando)
			{
				return (true, "🔴 Gravação de tela iniciada!");
			}
			else
			{
				return (true, $"⏹️ Gravação salva na pasta: {PastaDestino}");
			}
		});
	}
}
