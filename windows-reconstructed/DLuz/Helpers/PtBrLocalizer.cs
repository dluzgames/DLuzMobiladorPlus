using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DLuz.Helpers;

public static class PtBrLocalizer
{
	private static readonly Dictionary<string, string> Textos = new(StringComparer.Ordinal)
	{
		["Video y Audio"] = "Vídeo e Áudio",
		["Pantalla"] = "Tela",
		["Conexión"] = "Conexão",
		["Opciones Extras"] = "Opções extras",
		["Optimización"] = "Otimização",
		["Información"] = "Informações",
		["Perfiles"] = "Perfis",
		["Perfiles Guardados"] = "Perfis salvos",
		["Configuración de Video"] = "Configuração de vídeo",
		["Configuración de Audio"] = "Configuração de áudio",
		["Habilitar Video"] = "Ativar vídeo",
		["Habilitar Audio"] = "Ativar áudio",
		["Codificador de video"] = "Codificador de vídeo",
		["Codificador de audio"] = "Codificador de áudio",
		["Codificador avanzado"] = "Codificador avançado",
		["Modo de Renderizado"] = "Modo de renderização",
		["Aceleración por hardware"] = "Aceleração por hardware",
		["Pantalla virtual (modo DeX)"] = "Tela virtual (modo DeX)",
		["Depende del teléfono"] = "Depende do aparelho",
		["Estado de Conexión ADB"] = "Estado da conexão ADB",
		["Reiniciar ADB"] = "Reiniciar ADB",
		["Limpiar WiFi Huérfanas"] = "Limpar conexões Wi-Fi órfãs",
		["Modo OTG (Teclado y Mouse)"] = "Modo OTG (teclado e mouse)",
		["Dispositivo USB:"] = "Aparelho USB:",
		["Detectar Dispositivos"] = "Detectar aparelhos",
		["Conexión WiFi (Recomendada)"] = "Conexão Wi-Fi (recomendada)",
		["Puerto:"] = "Porta:",
		["IP del dispositivo:"] = "IP do aparelho:",
		["Detectar IP"] = "Detectar IP",
		["Cerrar Puerto"] = "Fechar porta",
		["Crear, cargar, guardar, importar y exportar perfiles."] = "Crie, carregue, salve, importe e exporte perfis.",
		["Nuevo desde estado actual"] = "Novo a partir do estado atual",
		["Crear desde cero"] = "Criar do zero",
		["Restaurar perfiles oficiales"] = "Restaurar perfis oficiais",
		["No hay perfiles disponibles."] = "Não há perfis disponíveis.",
		["Crear perfil desde cero"] = "Criar perfil do zero",
		["Importar perfil"] = "Importar perfil",
		["Nombre"] = "Nome",
		["Valores del Perfil"] = "Valores do perfil",
		["Puntos de restauración"] = "Pontos de restauração",
		["Sin puntos de restauración todavía."] = "Ainda não há pontos de restauração.",
		["Guardar Cambios"] = "Salvar alterações",
		["Revertir cambios"] = "Reverter alterações",
		["Restaurar perfil oficial"] = "Restaurar perfil oficial",
		["Restaurar desde origen"] = "Restaurar da origem",
		["Duplicar"] = "Duplicar",
		["Eliminar"] = "Excluir",
		["Aplicar"] = "Aplicar",
		["Restaurar"] = "Restaurar",
		["Revertir todo"] = "Reverter tudo",
		["Ver apps"] = "Ver aplicativos",
		["Resolución"] = "Resolução",
		["Resolución Personalizada (Opción 3 · Experimental)"] = "Resolução personalizada (opção 3 · experimental)",
		["Control de DPI (Opción 4 · Avanzado)"] = "Controle de DPI (opção 4 · avançado)",
		["Nuevo DPI personalizado:"] = "Novo DPI personalizado:",
		["Aplicar Resolución"] = "Aplicar resolução",
		["Resetear"] = "Redefinir",
		["Revertir"] = "Reverter",
		["Aplicar DPI"] = "Aplicar DPI",
		["Resetear DPI"] = "Redefinir DPI",
		["Controles"] = "Controles",
		["Sensibilidad X"] = "Sensibilidade X",
		["Sensibilidad Y"] = "Sensibilidade Y",
		["Nombre del perfil"] = "Nome do perfil",
		["Cada perfil guarda joystick, teclas, posiciones y tamaños, y se adapta solo a cualquier resolución."] = "Cada perfil salva joystick, teclas, posições e tamanhos e se adapta automaticamente a qualquer resolução."
		,["Códec, FPS, bitrate, codificador y renderizado."] = "Codec, FPS, bitrate, codificador e renderização."
		,["¿Necesitas una configuración personalizada? Disponible en Ko-fi"] = "Precisa de uma configuração personalizada? Disponível no Ko-fi"
		,["Limitador de resolución (0 = sin límite)"] = "Limitador de resolução (0 = sem limite)"
		,["⚠ Desactivado (pantalla virtual activa)"] = "⚠ Desativado (tela virtual ativa)"
		,["⚠ Desactivado (usando codificador avanzado)"] = "⚠ Desativado (usando codificador avançado)"
		,["Transmite el audio del teléfono al PC junto con la pantalla."] = "Transmite o áudio do celular ao PC junto com a tela."
		,["Resolución, aspecto, recorte, DPI y pantalla completa."] = "Resolução, proporção, recorte, DPI e tela cheia."
		,["Iniciar en pantalla completa"] = "Iniciar em tela cheia"
		,["Tamaño de ventana (0 = automático)"] = "Tamanho da janela (0 = automático)"
		,["Resolución Nativa del Dispositivo"] = "Resolução nativa do aparelho"
		,["Ancho:"] = "Largura:"
		,["Recorte de Imagen (Opción 1 · Recomendado)"] = "Recorte de imagem (opção 1 · recomendado)"
		,["Estado ADB, modo OTG y conexión WiFi."] = "Estado do ADB, modo OTG e conexão Wi-Fi."
		,["¿Necesitas ayuda para conectar tu dispositivo? Revisa las opciones en Ko-fi"] = "Precisa de ajuda para conectar seu aparelho? Confira as opções no Ko-fi"
		,["Control total del teléfono via teclado/mouse físico. No incluye transmisión de video/audio."] = "Controle total do celular por teclado e mouse físicos. Não inclui transmissão de vídeo ou áudio."
		,["① Conecta el cable USB    ② Activa WiFi    ③ Habilita el puerto    ④ Conecta"] = "① Conecte o cabo USB    ② Ative o Wi-Fi    ③ Habilite a porta    ④ Conecte"
		,["Úsalo solo en redes privadas, no en redes públicas."] = "Use somente em redes privadas, não em redes públicas."
		,["Comportamiento del dispositivo, atajos, cursor y diagnóstico."] = "Comportamento do aparelho, atalhos, cursor e diagnóstico."
		,["Comportamiento del Dispositivo"] = "Comportamento do aparelho"
		,["Depuración"] = "Depuração"
		,["Mostrar FPS"] = "Mostrar FPS"
		,["Posición del overlay"] = "Posição da sobreposição"
		,["Tecla de Atajos (MOD)"] = "Tecla de atalhos (MOD)"
		,["Modos de teclado, mouse y gamepad."] = "Modos de teclado, mouse e gamepad."
		,["Modo de Entrada"] = "Modo de entrada"
		,["Teclado y Mouse"] = "Teclado e mouse"
		,["Activar Teclado"] = "Ativar teclado"
		,["Activar Mouse"] = "Ativar mouse"
		,["Pasar todos los clics"] = "Repassar todos os cliques"
		,["⚠ Teclado y Mouse desactivados por Gamepad UHID"] = "⚠ Teclado e mouse desativados pelo gamepad UHID"
		,["Acciones ADB ligeras, reversibles y dependientes del dispositivo para Android moderno."] = "Ações ADB leves, reversíveis e dependentes do aparelho para Android moderno."
		,["Tienes optimizaciones activas. Conecta tu teléfono para revertirlas."] = "Há otimizações ativas. Conecte o celular para revertê-las."
		,["No se detecta ningún dispositivo. Verifica la conexión USB y que la depuración USB esté activa."] = "Nenhum aparelho foi detectado. Verifique a conexão e a depuração USB."
		,["¿Necesitas un perfil personalizado? Disponible en Ko-fi"] = "Precisa de um perfil personalizado? Disponível no Ko-fi"
		,["Crea uno nuevo o restaura los perfiles oficiales."] = "Crie um novo ou restaure os perfis oficiais."
		,["Acerca de LyXel"] = "Sobre o DLuzMObi v2"
		,["Descargas oficiales, recursos y soporte."] = "Downloads oficiais, recursos e suporte."
		,["DLuz"] = "DLuzMObi v2"
		,["Descargas oficiales"] = "Downloads oficiais"
		,["Ver todas las versiones"] = "Ver todas as versões"
		,["Recursos y soporte"] = "Recursos e suporte"
		,["Página oficial"] = "Página oficial"
		,["Sitio y descargas de LyXel"] = "Site e downloads do DLuzMObi v2"
		,["Comunidad y soporte"] = "Comunidade e suporte"
		,["Únete al Discord para ayuda"] = "Entre no Discord para obter ajuda"
		,["Apoyar el proyecto"] = "Apoiar o projeto"
		,["Configuraciones y extras en Ko-fi"] = "Configurações e extras no Ko-fi"
		,["Edición principal"] = "Edição principal"
		,["Estable"] = "Estável"
		,["Versiones oficiales del launcher, incluyendo la más reciente y anteriores. Usa siempre la versión más reciente y descarga únicamente desde los enlaces oficiales."] = "Versões oficiais do aplicativo, incluindo a mais recente e as anteriores. Use sempre a versão mais recente e baixe somente pelos links oficiais."
		,["Sitio y descargas de DLuz MObi v2"] = "Site e downloads do DLuzMObi v2"
		,["Dale una estrella al creador en"] = "Dê uma estrela ao criador no"
		,["Novedades y demos"] = "Novidades e demonstrações"
		,["Guías y tutoriales"] = "Guias e tutoriais"
	};

	public static void Aplicar(DependencyObject raiz)
	{
		try
		{
			if (raiz == null) return;
			TraduzirPropriedade(raiz, "Title");
			TraduzirPropriedade(raiz, "Message");
			TraduzirPropriedade(raiz, "PlaceholderText");
			if (raiz is TextBlock texto && texto.Text != null) texto.Text = Traduzir(texto.Text);
			if (raiz is ContentControl conteudo && conteudo.Content is string valor) conteudo.Content = Traduzir(valor);
			if (raiz is FrameworkElement elemento && elemento.ToolTip is string dica) elemento.ToolTip = Traduzir(dica);
			if (raiz is Visual || raiz is System.Windows.Media.Media3D.Visual3D)
			{
				int total = VisualTreeHelper.GetChildrenCount(raiz);
				for (int i = 0; i < total; i++)
				{
					DependencyObject filho = VisualTreeHelper.GetChild(raiz, i);
					if (filho != null) Aplicar(filho);
				}
			}
		}
		catch
		{
		}
	}

	private static void TraduzirPropriedade(object alvo, string nome)
	{
		PropertyInfo? prop = alvo.GetType().GetProperty(nome, BindingFlags.Instance | BindingFlags.Public);
		if (prop?.CanRead == true && prop.CanWrite && prop.PropertyType == typeof(string) && prop.GetValue(alvo) is string valor)
			prop.SetValue(alvo, Traduzir(valor));
	}

	private static string Traduzir(string valor)
	{
		if (Textos.TryGetValue(valor, out string? traduzido)) return traduzido;
		(string espanhol, string portugues)[] termos =
		{
			("Configuración", "Configuração"), ("configuración", "configuração"),
			("Resolución", "Resolução"), ("resolución", "resolução"),
			("Dispositivo", "Aparelho"), ("dispositivo", "aparelho"), ("teléfono", "celular"),
			("Pantalla", "Tela"), ("pantalla", "tela"), ("Conexión", "Conexão"), ("conexión", "conexão"),
			("Activar", "Ativar"), ("activar", "ativar"), ("Desactivar", "Desativar"), ("desactivar", "desativar"),
			("Activado", "Ativado"), ("activado", "ativado"), ("Desactivado", "Desativado"), ("desactivado", "desativado"),
			("Guardar", "Salvar"), ("guardar", "salvar"), ("Eliminar", "Excluir"), ("eliminar", "excluir"),
			("Restaurar", "Restaurar"), ("Revertir", "Reverter"), ("revertir", "reverter"),
			("Aplicar", "Aplicar"), ("Detectar", "Detectar"), ("Cerrar", "Fechar"), ("cerrar", "fechar"),
			("Ventana", "Janela"), ("ventana", "janela"), ("Teclado y Mouse", "Teclado e mouse"),
			("Perfil activo", "Perfil ativo"), ("perfil activo", "perfil ativo"), ("Perfiles", "Perfis"), ("perfiles", "perfis"),
			("Opciones", "Opções"), ("opciones", "opções"), ("Avanzado", "Avançado"), ("avanzado", "avançado"),
			("Recomendado", "Recomendado"), ("recomendado", "recomendado"), ("Nuevo", "Novo"), ("nuevo", "novo"),
			("Sin ", "Sem "), ("sin ", "sem "), ("Ningún", "Nenhum"), ("ningún", "nenhum"),
			("Cambios", "Alterações"), ("cambios", "alterações"), ("Error", "Erro"), ("error", "erro"),
			("Disponible", "Disponível"), ("disponible", "disponível"), ("Ayuda", "Ajuda"), ("ayuda", "ajuda")
		};
		foreach ((string espanhol, string portugues) in termos) valor = valor.Replace(espanhol, portugues, StringComparison.Ordinal);
		return valor;
	}
}


