# DLuzMObi v2 — contexto técnico do projeto

> Documento de transferência de contexto para desenvolvimento humano ou por outra IA.
> Revisado em 11/08/2026 contra o código existente em `windows-reconstructed`.

## 1. Resumo executivo

O **DLuzMObi v2** é um aplicativo desktop para Windows que controla e espelha dispositivos Android usando **ADB** e **scrcpy**. Além do espelhamento, oferece perfis de vídeo/áudio/tela/entrada, conexão USB ou Wi‑Fi, modo OTG, tela virtual, otimizações Android e um **mapeador próprio de teclado e mouse para toques Android**.

A interface é WPF, escura, inspirada visualmente nas configurações do BlueStacks. O projeto atual foi reconstruído a partir da base LyXel e ainda conserva vários nomes internos, namespaces, caminhos e textos antigos em espanhol. O produto mostrado ao usuário é **DLuzMObi v2**, versão **2.0 beta**.

Estado verificado:

- Plataforma atual: Windows x64.
- Framework: .NET 10 para Windows/WPF.
- Compila com sucesso, mas produz aproximadamente 1.781 avisos originados principalmente da base reconstruída/descompilada.
- A versão Linux existe em outra pasta, mas não faz parte desta implementação atual.
- O mapeador está marcado como beta e precisa de testes reais com diferentes aparelhos, resoluções e jogos.

## 2. Diretórios importantes

```text
DLuzMobilador v2/
├─ windows-reconstructed/   # projeto Windows atual; trabalhar aqui
├─ linux-reconstructed/     # reconstrução Linux separada; não misturar agora
└─ backups/
   └─ DLuz-MObi-v2-before-bluestacks-ui-20260811-130653.zip
```

Dentro de `windows-reconstructed`:

```text
windows-reconstructed/
├─ LyXel.csproj                    # projeto WPF e regras de cópia dos binários
├─ LyXel.App.xaml                  # ApplicationDefinition
├─ LyXel.MainWindow.xaml           # shell/navegação principal
├─ LyXel.Views.*.xaml              # telas e overlays XAML
├─ LyXel/                          # código C# principal
│  ├─ Mapper/                      # motor de mapeamento e protocolo de controle
│  │  └─ Profile/                  # perfil normalizado e persistência do keymap
│  ├─ Services/                    # estado, dialogs, toasts, ADB integrity, mapper
│  ├─ ViewModels/                  # MVVM das páginas
│  ├─ Views/                       # code-behind de páginas e overlays
│  ├─ Helpers/                     # paths, logs, tradução, migração e links
│  ├─ App.cs                       # inicialização e tratamento global de erros
│  ├─ MainWindow.cs                # navegação, dirty state e encerramento
│  ├─ ADBManager.cs                # todas as operações ADB
│  ├─ ScrcpyManager.cs             # criação e supervisão dos processos scrcpy
│  ├─ PerfilManager.cs             # perfis INI de scrcpy
│  └─ ScrcpyConfig.cs              # modelo completo de configuração
├─ RuntimeAssets/bin/
│  ├─ adb/                         # adb.exe e DLLs oficiais
│  └─ scrcpy/
│     ├─ x86/                      # distribuição scrcpy 32 bits
│     └─ x86_64/                   # distribuição scrcpy 64 bits
├─ lib/                            # DLLs referenciadas diretamente
├─ assets/                         # logotipo e ícones
├─ themes/apptheme.xaml            # tema global
├─ Properties/AssemblyInfo.cs      # identidade e versão do assembly
├─ LyXel.perfiles.base.ini         # perfis iniciais embutidos
├─ LyXel.keymap.freefire.json      # keymap inicial embutido
├─ LyXel.adb.manifest.txt          # manifesto de integridade do ADB
└─ LyXel.adb.respaldo.zip          # cópia de reparação do ADB
```

Pastas `bin/` e `obj/` são artefatos gerados e não devem ser usadas como fonte.

## 3. Stack e dependências

- C# 12.
- WPF (`Microsoft.NET.Sdk.WindowsDesktop`).
- Target: `net10.0-windows`.
- Arquitetura: `x64`.
- `AllowUnsafeBlocks=true`.
- `Wpf.Ui` e `Wpf.Ui.Abstractions` para janela Fluent, navegação e controles.
- `CommunityToolkit.Mvvm` para `ObservableObject`, propriedades observáveis e comandos.
- `INIFileParser` para configuração e perfis INI.
- `WinRT.Runtime`.
- ADB e scrcpy distribuídos localmente em `RuntimeAssets`.

As DLLs estão em `lib/` e são referenciadas por `HintPath`; não há `PackageReference` para elas. Ao copiar o projeto para outro computador ou IA, é necessário preservar `lib/`, `RuntimeAssets/`, os arquivos embutidos e `app.ico`.

## 4. Identidade do produto

Identidade pública desejada:

- Nome: `DLuzMObi v2`.
- Versão visual: `Versão 2.0 beta`.
- Assembly: `2.0.0.0`.
- Informational version: `2.0-beta`.
- Empresa: `DLuz`.

Arquivos principais:

- `Properties/AssemblyInfo.cs` define metadados do assembly.
- `LyXel/Helpers/AppInfo.cs` expõe nome, canal e versão.
- `LyXel/ViewModels/AcercaDeViewModel.cs` gera o texto visual da versão.
- O assembly e namespace continuam chamados `LyXel` por compatibilidade interna.
- Após o build, o alvo MSBuild copia `LyXel.exe`, `.deps.json` e `.runtimeconfig.json` para aliases chamados `DLuzMObi v2.*`.

Não renomear namespaces/assembly de forma mecânica sem analisar recursos embutidos, nomes lógicos, XAML e reflexão.

## 5. Compilação e execução

Pré-requisitos:

- Windows 10 1809 ou posterior (alvo declarado `Windows10.0.17763.0`).
- SDK .NET 10 com Windows Desktop.
- Arquitetura x64.

Comandos:

```powershell
cd "C:\Users\dluzgg\Documents\ChatGPT\DLuzMobilador v2\windows-reconstructed"
dotnet build .\LyXel.csproj -c Debug -clp:ErrorsOnly
& ".\bin\Debug\net10.0-windows\DLuzMObi v2.exe"
```

Saída principal atual:

```text
bin/Debug/net10.0-windows/
├─ LyXel.exe
├─ DLuzMObi v2.exe
├─ bin/adb/...
└─ bin/scrcpy/x86 e x86_64/...
```

O alias `DLuzMObi v2.exe` depende dos demais arquivos da pasta; não é um executável standalone.

## 6. Inicialização e encerramento

Fluxo de inicialização:

1. `App.OnStartup` inicializa um Windows Job Object através de `Program.InicializarJob`.
2. O Job Object usa `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE`, ajudando a encerrar processos filhos quando o app termina.
3. Aplica tema escuro e cor azul de destaque.
4. Registra proteção de roda do mouse e handlers globais de exceção.
5. Cria os diretórios em `%LOCALAPPDATA%\LyXel`.
6. Executa migração inicial de perfis antigos.
7. Abre `MainWindow`.
8. `MainWindow` carrega configuração, aplica perfil inicial, navega para Início e começa a detecção ADB.

No fechamento, `MainWindow_Closing`:

- pergunta o que fazer com alterações não salvas;
- encerra mapeador e scrcpy;
- interrompe `track-devices`;
- restaura resolução/DPI Android quando necessário;
- desconecta Wi‑Fi;
- salva configuração;
- encerra o daemon ADB local.

O tratamento de exceções não fatais de UI registra log e tenta manter o aplicativo aberto.

## 7. Arquitetura de estado e MVVM

`SessionState.Instance` é o centro da aplicação e funciona como singleton. Ele possui e coordena:

- `ADBManager Adb`;
- `ScrcpyManager Scrcpy`;
- `PerfilManager Perfiles`;
- `PerfilMetaService PerfilesMeta`;
- `MapeadorService Mapeador` (criação tardia/lazy);
- valores atuais de vídeo, áudio, resolução, DPI, entradas, Wi‑Fi, OTG e tela virtual;
- estado de conexão do dispositivo;
- dirty state por seção e configuração global;
- carregamento, snapshots, reversão e persistência.

As páginas possuem ViewModels separados, mas compartilham o singleton. `SeccionViewModel` é a base das páginas editáveis e integra o indicador de alterações pendentes.

Ao navegar ou fechar, `MainWindow` consulta o dirty state e oferece salvar, descartar ou cancelar.

## 8. Páginas da interface

A barra de navegação contém:

1. Início — conexão atual, perfil e iniciar/parar scrcpy.
2. Vídeo e Áudio — FPS, bitrate, codecs, buffers e encoder.
3. Tela — resolução, proporção, DPI, fullscreen e ajustes da janela.
4. Conexão — USB, Wi‑Fi, IP/porta e reconexão.
5. Opções extras — comportamento de tela, renderização e atalhos.
6. Controles — modos de teclado, mouse e gamepad, incluindo UHID.
7. Mapeador Beta — sessão de jogo, captura e edição do keymap.
8. Otimização — comandos e ajustes Android.
9. Informações — produto, links sociais e apoio.

Tema e layout:

- fundo azul-marinho escuro;
- destaque azul;
- navegação lateral/horizontal customizada em `LyXel.MainWindow.xaml` e `MainWindow.cs`;
- recursos de cor em `themes/apptheme.xaml`;
- localização complementar em `PtBrLocalizer`, aplicada após cada navegação.

## 9. ADB

`ADBManager` encapsula execução de comandos e monitoramento. Capacidades principais:

- listar dispositivos e estados (`device`, `unauthorized`, `offline` etc.);
- `track-devices` persistente;
- detectar/aplicar/resetar resolução (`wm size`);
- detectar/aplicar/resetar DPI (`wm density`);
- alterar `pointer_speed`;
- executar shell;
- reiniciar servidor e reconectar;
- habilitar `tcpip`, detectar IP, conectar/desconectar Wi‑Fi;
- limpar conexões TCP antigas;
- detectar mudança USB/Wi‑Fi e emitir eventos.

`SessionState` supervisiona integridade e falhas do ADB. Existe circuit breaker para `track-devices` quando o processo morre repetidamente. Os arquivos usados pelo ADB devem estar em:

```text
<pasta do executável>/bin/adb/adb.exe
```

O erro anterior “não pode encontrar adb.exe” ocorreu quando os assets não estavam sendo copiados. O `.csproj` agora copia `RuntimeAssets/bin/**/*` para `bin/` na saída.

`IntegridadAdb` usa o manifesto e `LyXel.adb.respaldo.zip` para verificar/reparar arquivos. Não remover esses recursos sem adaptar a lógica.

## 10. scrcpy

`ScrcpyManager` monta argumentos, escolhe arquitetura e supervisiona processos. Suporta:

- espelhamento normal;
- vídeo e áudio configuráveis;
- codecs, encoder, FPS, bitrate, buffers e max-size;
- janela, fullscreen, crop, render driver e FPS;
- controle por teclado/mouse/gamepad;
- UHID, SDK e modos desativados conforme configuração;
- USB, Wi‑Fi e OTG;
- display virtual/Dex;
- modo dual experimental com processos separados de visual e entrada;
- overlay de FPS e janelas flutuantes;
- detecção de encoders disponíveis.

`ArquitecturaHelper` resolve caminhos x86/x64. A distribuição scrcpy precisa permanecer completa, incluindo `scrcpy.exe`, `scrcpy-server`, SDL/FFmpeg e DLLs auxiliares.

## 11. Sistema de mapeamento

### 11.1 Objetivo

O mapeador traduz teclado e mouse do Windows em eventos multitouch no Android sobre a janela do scrcpy. Ele não é apenas uma camada visual: abre uma conexão própria com o servidor de controle do scrcpy e injeta toques.

### 11.2 Fluxo do botão Play/Iniciar

`MapeadorViewModel.PrincipalAsync` chama `MapeadorService.IniciarSesionAsync`.

Em termos funcionais:

1. valida dispositivo e estado da sessão;
2. carrega o perfil/keymap ativo;
3. inicia o espelho scrcpy apropriado;
4. opcionalmente cria display virtual e abre o jogo nele;
5. espera localizar a janela do espelho;
6. conecta `MapeadorEngine` ao servidor de controle do scrcpy;
7. abre overlays e painel lateral;
8. prepara resolução e coordenadas;
9. somente quando a captura é ativada (por F1 ou botão “Jogar”) confina/oculta o cursor, instala/sensibiliza os hooks e transforma teclado/mouse em toques.

Isso corresponde ao comportamento desejado: abrir o editor não deve ativar o controle HID/mapeado; o modo de jogo é ativado ao iniciar/capturar.

### 11.3 Componentes

- `MapeadorService`: ciclo de vida, perfis, overlays, painel, fullscreen e display virtual.
- `MapeadorEngine`: loop de entrada de aproximadamente 8 ms, captura, liberação e coordenação dos mappers.
- `KeyboardHook`: hook global `WH_KEYBOARD_LL`.
- `MouseRawInput`: movimento bruto do mouse.
- `MouseSuppressHook`: bloqueia eventos locais quando necessário.
- `InputState`: estado thread-safe de teclas e deltas do mouse.
- `JoystickMapper`: converte WASD em joystick virtual multitouch.
- `CameraMapper`: converte movimento do mouse em arrasto de câmera/aim.
- `TouchMapper`: processa botões, hold, tap e repetição.
- `TouchInjector`: gerencia IDs de “dedos” e envia down/move/up.
- `ScrcpyControlClient`: socket/protocolo de controle do scrcpy.
- `ScrcpyServerLauncher`: inicia servidor dedicado necessário ao controle.
- `MapeadorOverlayWindow`: superfície visual/editável sobre o jogo.
- `MapeadorControlesOverlay`: bolhas das teclas sobrepostas.
- `MapeadorCursorOverlay` e `CursorMaskWindow`: cursor e máscara/confinamento.
- `MapperSidePanel`: painel lateral de edição.

### 11.4 Atalhos padrão

- F1: alterna jogar/pausar captura.
- Escape: saída/liberação definida pelo keymap.
- Alt: libera temporariamente o mouse para interação normal.
- F11: usado pelo serviço para fullscreen.
- WASD: joystick virtual.

### 11.5 Perfil padrão Free Fire

O recurso `LyXel.keymap.freefire.json` foi desenhado na base 2400 × 1080 e inclui:

- WASD no joystick;
- clique esquerdo = Disparo;
- clique direito = Mira;
- Espaço = Salto;
- F = Interagir;
- R = Recarregar;
- G = Granada;
- C = Agachar;
- Z = Deitar;
- LeftShift = Correr em modo hold.

### 11.6 Perfis normalizados

`ProfileStore` salva coordenadas normalizadas (0–1) em `mapper_profile.json`. Na carga, converte para pixels da resolução atual. Isso é essencial para reaproveitar o layout entre resoluções.

O formato `NProfile` está na versão 2 e contém:

- nome e versão;
- teclas de toggle e saída;
- opacidade do overlay;
- joystick com centro, raio e teclas;
- câmera com zona, sensibilidades X/Y, smoothing e inversões;
- botões com tecla, posição, rótulo, modo, tamanho e repetição.

`MapeadorService` permite listar, criar, ativar, renomear, excluir, importar e exportar perfis. Perfis adicionais ficam em `mapper_profiles/`.

## 12. Tela virtual

O mapeador pode usar um display virtual próprio. Configurações relevantes em `SessionState`:

- ativação do modo virtual;
- pacote do jogo (Free Fire MAX, Free Fire ou personalizado);
- resolução do display;
- DPI.

Ao iniciar, o serviço espera o ID do display virtual e lança o pacote Android nesse display. Essa área depende da versão/capacidade do Android e do scrcpy e precisa de fallback claro quando não suportada.

## 13. Perfis de scrcpy e configurações persistentes

Dados do usuário ficam em:

```text
%LOCALAPPDATA%\LyXel\
├─ config.ini
├─ perfiles.ini
├─ profiles_meta.json
├─ keymap_freefire.json
├─ mapper_profile.json
├─ mapper_profiles/
├─ profile_backups/
└─ logs/
```

Observação: o diretório ainda se chama `LyXel` por compatibilidade. Mudá-lo exige migração para não perder configurações existentes.

Responsabilidades:

- `config.ini`: preferências globais, último perfil e estados auxiliares.
- `perfiles.ini`: perfis de vídeo/áudio/tela/controle.
- `profiles_meta.json`: origem, datas e até três pontos de restauração por perfil.
- `keymap_freefire.json`: keymap antigo/fallback em pixels.
- `mapper_profile.json`: perfil normalizado ativo.
- `mapper_profiles/`: biblioteca de perfis do mapeador.
- `profile_backups/`: backups automáticos; `PerfilManager` mantém até 15.
- `logs/`: diagnóstico do aplicativo e do mapeador.

`AppMigrator` importa `config.ini` e `perfiles.ini` antigos existentes ao lado do executável.

## 14. Localização PT-BR

A intenção é que toda a interface pública esteja em português do Brasil. Porém, a base reconstruída contém identificadores e vários textos originais em espanhol. A tradução ocorre de duas formas:

1. textos já alterados diretamente em XAML/C#;
2. substituições em tempo de execução por `LyXel/Helpers/PtBrLocalizer.cs` após navegar para uma página.

Cuidados:

- não traduzir nomes de classes/métodos apenas por estética;
- ao criar nova UI, escrever o texto público diretamente em PT-BR;
- pesquisar textos espanhóis em `.cs` e `.xaml` antes de uma release;
- preservar a chave espanhola no dicionário se ela for usada para localizar o texto original;
- salvar arquivos como UTF-8. O terminal PowerShell pode exibir mojibake mesmo quando o arquivo está correto.

## 15. Links oficiais atuais

- Site: <https://dluzgames.com.br>
- YouTube: <https://www.youtube.com/@dluzgames>
- TikTok: <https://www.tiktok.com/@dluzgames>
- Instagram: <https://www.instagram.com/dluzgames/>
- Apoiar o projeto: <https://livepix.gg/dluz>

Esses links estão em `LyXel/Helpers/AppLinks.cs`. A página Informações foi simplificada para destacar apenas os canais do DLuz e o LivePix. Constantes antigas como Ko-fi, Discord, repositório scrcpy e downloads LyXel ainda existem para compatibilidade, mas não devem voltar à página pública sem decisão do proprietário.

## 16. Logs, diagnóstico e recuperação

- `AppLogger` grava logs em `%LOCALAPPDATA%\LyXel\logs`.
- `AvisoService` monta diagnósticos copiáveis.
- `Diag.CurrentPage` informa a página atual nos erros.
- `MapperDiag` oferece log específico do mapeador.
- Falhas fatais tentam restaurar `wm density` e `wm size`.
- O monitor ADB possui detecção de crash nativo e circuit breaker.
- Há reparo do ADB por manifesto/ZIP embutido.

Ao receber um relatório de erro, procurar primeiro:

- exceção e página no log;
- caminho real de `adb.exe` e `scrcpy.exe`;
- saída/exit code do processo;
- estado retornado por `adb devices -l`;
- arquitetura selecionada;
- resolução/display ID usados pelo mapeador.

## 17. Riscos técnicos conhecidos

1. **Base reconstruída:** há grande quantidade de código gerado/descompilado, métodos extensos e aproximadamente 1.781 warnings.
2. **Mistura de idiomas:** nomes internos e textos espanhóis ainda coexistem com PT-BR.
3. **Singleton grande:** `SessionState.cs` concentra muitas responsabilidades e supera 3.400 linhas.
4. **Gerenciador scrcpy grande:** `ScrcpyManager.cs` também concentra argumentos, processos, overlays e modos experimentais.
5. **Win32/PInvoke:** hooks globais, raw input, sockets e posicionamento de janelas exigem testes no Windows real.
6. **Dependência de protocolo:** `ScrcpyControlClient` precisa permanecer compatível com a versão de `scrcpy-server` distribuída.
7. **Sem suíte automatizada:** não foi encontrada uma solução/test project; a validação atual é build + teste manual.
8. **Arquivos locais essenciais:** copiar somente o EXE quebra ADB/scrcpy e DLLs.
9. **Caminho de dados legado:** `%LOCALAPPDATA%\LyXel` ainda não acompanha a nova marca.
10. **Configuração de Release:** `AssemblyInfo` declara `AssemblyConfiguration("Release")` mesmo em builds Debug; é apenas metadado fixo nesta base.

## 18. Prioridades recomendadas

### Antes de distribuir a beta

1. Fazer smoke test completo em Windows 10 e 11.
2. Testar USB autorizado, USB não autorizado, offline e Wi‑Fi.
3. Testar abrir/parar scrcpy repetidamente e encerrar com sessão ativa.
4. Testar mapeador em 16:9 e em tela vertical, com escalas DPI diferentes do Windows.
5. Validar F1, Alt, Escape, WASD, cliques e liberação de todos os toques.
6. Validar display virtual em aparelhos compatíveis e incompatíveis.
7. Revisar todos os textos espanhóis visíveis.
8. Criar processo de publicação que inclua DLLs e RuntimeAssets.

### Refatoração futura

1. Dividir `SessionState` em serviços menores.
2. Separar montagem de argumentos, supervisão de processo e layout do `ScrcpyManager`.
3. Criar interfaces para ADB/scrcpy e testes unitários dos argumentos/parsers.
4. Centralizar localização em recursos `.resx` ou dicionários XAML.
5. Criar testes de serialização/migração dos perfis normalizados.
6. Adicionar validação de compatibilidade entre cliente e servidor scrcpy.
7. Planejar migração segura de `%LOCALAPPDATA%\LyXel` para uma pasta da nova marca.

## 19. Protocolo de trabalho para outra IA

Ao continuar este projeto, a IA deve:

1. Trabalhar em `windows-reconstructed` até o proprietário pedir explicitamente a migração Linux.
2. Ler este arquivo, `LyXel.csproj`, `SessionState.cs`, `ADBManager.cs`, `ScrcpyManager.cs` e os arquivos do módulo afetado.
3. Não editar `bin/` ou `obj/`.
4. Preservar `RuntimeAssets`, recursos embutidos e as DLLs de `lib/`.
5. Fazer backup antes de mudanças grandes de interface/arquitetura.
6. Não substituir perfis/configurações do usuário em `%LOCALAPPDATA%` sem autorização.
7. Manter a interface pública em PT-BR e a marca exata `DLuzMObi v2`.
8. Manter a versão visual `Versão 2.0 beta` até nova orientação.
9. Após cada alteração, executar `dotnet build` e informar erros e warnings separadamente.
10. Para mudanças no mapeador, testar também parada, perda de foco e liberação de entradas; uma falha pode deixar tecla, toque ou cursor presos.
11. Não presumir que “HID” e o mapeador são a mesma camada: scrcpy UHID configura dispositivos de entrada; o mapeador próprio usa hooks e injeção de toque pelo canal de controle.
12. Ao mexer em caminhos/nomes internos LyXel, implementar migração e fallback em vez de renomear diretamente.

## 20. Checklist de validação manual

```text
[ ] O app abre com o título DLuzMObi v2.
[ ] Informações mostra Versão 2.0 beta.
[ ] ADB integrado é encontrado na pasta bin/adb.
[ ] Dispositivo USB aparece e muda de estado corretamente.
[ ] Dispositivo unauthorized mostra orientação adequada.
[ ] Iniciar scrcpy abre a janela com o nome DLuzMObi v2.
[ ] Parar scrcpy fecha apenas os processos da sessão.
[ ] Wi‑Fi conecta, monitora USB e desconecta corretamente.
[ ] Alterações de DPI/resolução são restauradas no encerramento.
[ ] O mapeador abre o espelho e os overlays.
[ ] O editor permite mover/criar/salvar controles.
[ ] F1 ativa e pausa a captura.
[ ] Alt libera o mouse temporariamente.
[ ] WASD move o joystick virtual.
[ ] Mouse controla câmera e cliques executam os botões.
[ ] Parar/fechar libera teclas, toques, hooks e cursor.
[ ] Perfil do mapeador persiste e escala em outra resolução.
[ ] Links oficiais e LivePix abrem corretamente.
[ ] Não há textos espanhóis visíveis no fluxo testado.
```

## 21. Prompt curto para entregar junto a este projeto

```text
Analise primeiro o arquivo projeto.md na raiz de windows-reconstructed.
Este é o DLuzMObi v2, um aplicativo WPF .NET 10 x64 para controlar Android
por ADB/scrcpy, com perfis e mapeador próprio de teclado/mouse para multitouch.
Trabalhe somente em windows-reconstructed, preserve RuntimeAssets/lib e os dados
do usuário, mantenha a interface em PT-BR e a marca DLuzMObi v2 — Versão 2.0 beta.
Antes de alterar, identifique o fluxo real no código. Depois compile e valide.
Não edite bin/obj e não migre para Linux sem pedido explícito.
```

