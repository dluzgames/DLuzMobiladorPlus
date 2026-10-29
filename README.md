# DLuzMobiladorPlus Pro v3.5.1 🎮⚡

> **O melhor software mobilador para Windows com transmissão USB 120 FPS Zero Delay, motor de mouse 1000Hz (1ms) Raw Input, Scrcpy v5.0.1 com decodificação Direct3D 11 GPU (H.265) e pareamento 100% Mobile (Anti-Ban).**

[![Release](https://img.shields.io/github/v/release/dluzgames/DLuzMobiladorPlus?color=39FF14&label=Vers%C3%A3o%20Oficial&style=for-the-badge)](https://github.com/dluzgames/DLuzMobiladorPlus/releases/tag/v3.5.1)
[![License](https://img.shields.io/badge/Licen%C3%A7a-PRO%20Vital%C3%ADcia-00E5FF?style=for-the-badge)](https://loja.dluz.com.br/produto/dluzmobilador-pro)
[![Store](https://img.shields.io/badge/Loja%20Oficial-loja.dluz.com.br-FFD700?style=for-the-badge)](https://loja.dluz.com.br)
[![Status](https://img.shields.io/badge/Status-100%25%20Anti--Ban-success?style=for-the-badge)](https://dluzgames.com.br)

---

## 📥 Downloads Oficiais (v3.5.1)

| Pacote | Arquivo | Descrição |
| :--- | :--- | :--- |
| 🚀 **Instalador Oficial** | [`DLuzMobiladorPlus_Instalador.exe`](https://github.com/dluzgames/DLuzMobiladorPlus/releases/download/v3.5.1/DLuzMobiladorPlus_Instalador.exe) | Instalação automática com atalhos e drivers USB |
| 📦 **Pacote Portátil (.zip)** | [`DLuzMobiladorPlus_v3.5.1_Portatil.zip`](https://github.com/dluzgames/DLuzMobiladorPlus/releases/download/v3.5.1/DLuzMobiladorPlus_v3.5.1_Portatil.zip) | Pronto para extrair e jogar direto sem instalação |

---

## ⚡ Principais Novidades da Versão 3.5.1

- 🚀 **Motor Scrcpy v5.0.1 Oficial Integrado:**
  - Correção na decodificação de hardware **H.265 (HEVC)** no Windows via `D3D11VA`.
  - Suporte ao formato `DRM_FORMAT_RG88` para plano croma NV12.
  - Correção no renderizador OpenGL/DirectX para eliminar travamentos com múltiplos GPUs.
  - Zero Buffer (`display_buffer=0` / `audio_buffer=15ms`) para latência imperceptível (< 12ms totais).
- 🖱️ **Sensor de Mouse 1000Hz (1ms) Raw Input:**
  - Leitura bruta de mouse Windows Raw Input (`WM_INPUT`) sem aceleração de ponteiro.
  - Curva de sensibilidade *Linear Spline* com ajuste independente dos eixos **X (Horizontal)** e **Y (Vertical - Puxada de Capa)**.
  - Recenter 360° dinâmico com clamping circular (`R = 0.38`), evitando perda de cursor nas bordas.
- 🎨 **Novo Motor de Temas Studio (Claro / Escuro):**
  - Alternância instantânea entre modo Dark Slate Studio (`#0B0F19`) e Light Studio moderno.
  - Paleta com alto contraste e ergonomia visual.
- ⌨️ **UHID ABNT2 com Scancodes Nativos:**
  - Mapeamento direto de teclado sem necessidade de root ou apps extras no Android.
  - Suporte completo ao layout ABNT2 (Ç, acentos, teclas numéricas).
- 🛡️ **100% Anti-Ban (Pareia em Salas Mobile):**
  - O jogo roda nativamente dentro do Android; o PC atua apenas como central de transmissão e periféricos.
  - Não modifica APKs nem injeta código em processos de jogos (Free Fire, Blood Strike, COD Mobile, PUBG, Roblox, Standoff 2).

---

## 🏛️ Arquitetura do Sistema

```
DLuzMobiladorPlus (Windows C# / WPF + Direct3D 11)
  ├── Scrcpy Engine v5.0.1
  │     ├── Decoder: D3D11VA Hardware (H.265 / HEVC)
  │     ├── Audio Stream: WASAPI Low-Latency (15ms)
  │     └── Display Buffer: 0ms Real-Time Mirror
  ├── Peripherals & Input Engine
  │     ├── Windows Raw Input (1000Hz / 1ms)
  │     ├── Linear Spline Interpolation (X / Y Independent)
  │     └── UHID ABNT2 Scancode Direct Passthrough
  ├── Cloud & Licensing Hub
  │     ├── Coolify License Server (ECDSA & HWID Lock)
  │     ├── Auto-Update Engine (v3.5.1 API)
  │     └── Loja DLuz Pro Integration (Mercado Pago + Evolution WhatsApp)
  └── UI / Theme Subsystem
        ├── Dynamic Theme Manager (Dark / Light Studio)
        └── Game Profile Presets (Free Fire, Blood Strike, COD, Roblox)
```

---

## 🔑 Ativação & Licença

- Adquira sua Chave Oficial Vitalícia na **[Loja Oficial DLuz Games](https://loja.dluz.com.br/produto/dluzmobilador-pro)**.
- O serial PRO e os links são liberados instantaneamente na tela após o Pix ou Cartão, e enviados por **WhatsApp** e **E-mail**.

---

## 🛠️ Requisitos de Sistema

- **Sistema Operacional:** Windows 10 ou Windows 11 (64-bit).
- **Processador:** Dual-Core 2.0 GHz ou superior.
- **Memória RAM:** 4 GB de RAM.
- **Placa de Vídeo:** Compatível com DirectX 11 (Direct3D 11) para aceleração por hardware.
- **Dispositivo Móvel:** Smartphone Android 8.0+ com **Depuração USB** habilitada e cabo USB de qualidade.

---

## 💬 Suporte Oficial

- 🌐 **Site Oficial:** [dluzgames.com.br](https://dluzgames.com.br)
- 🛒 **Loja Oficial:** [loja.dluz.com.br](https://loja.dluz.com.br)
- 💬 **Discord VIP:** [discord.gg/r2tUSVnkn2](https://discord.gg/r2tUSVnkn2)
- 📱 **WhatsApp Suporte:** +55 (63) 98119-9852

---

*© 2026 DLuz Games. Todos os direitos reservados.*
