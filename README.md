# Guardianes del Bosque

Videojuego 2.5D de plataformas hecho en **Unity 6.6 (6000.6.0f1)**.
Eres un guardián del bosque, **Sylvan** o **Maya**, y recorres el sendero recogiendo residuos (botellas, latas y papel) mientras saltas obstáculos.

## Contenido

| Carpeta | Qué hay |
|---|---|
| `JUEGO_GUARDIAN/` | Proyecto de Unity |
| `JUEGO_GUARDIAN/Assets/Scenes` | `MenuPrincipal`, `Zona1`, `Zona2` y `Zona3` |
| `JUEGO_GUARDIAN/Assets/Scripts` | Lógica del juego: jugador, cámara, HUD, residuos, obstáculos, rocas que caen, menú |
| `JUEGO_GUARDIAN/Assets/Editor` | Herramientas del menú **Guardianes del Bosque** que construyen cada zona |
| `JUEGO_GUARDIAN/Objetos` | Modelos fuente: tronco, rocas, alambre de púas y residuos (FBX, GLB y Blender) |
| `JUEGO_GUARDIAN/Personaje` | Personajes en Blender (Sylvan y Maya) y sus scripts |
| `JUEGO_GUARDIAN/Fondos` | Imágenes de fondo de cada zona y scripts que generan el menú, el HUD y los botones |
| `Setup/` | Script de Inno Setup e icono para crear el instalador de Windows |

## Cómo abrirlo

1. En Unity Hub, usa **Añadir › Añadir proyecto desde el disco** y elige la carpeta `JUEGO_GUARDIAN`.
2. Ábrelo con Unity **6000.6.0f1**. La primera vez tarda unos minutos porque Unity regenera la carpeta `Library`.
3. Abre la escena `Assets/Scenes/MenuPrincipal` y pulsa Play.

## Controles

**PC**
- Moverse: **A / D** o **← / →**
- Saltar: **Espacio**, **W** o **↑**
- Pausa: **Escape** (menú con Continuar, Volver al inicio y Salir)
- Menús: ratón, o flechas + **Enter**

**Celular (Android)**
- Botones en pantalla: **◀ ▶** para moverse, **▲** para saltar y **II** para pausar
- El botón "atrás" del celular también abre la pausa

## Zonas

1. **Zona 1**: sendero del bosque (70 m).
2. **Zona 2**: del bosque a la cueva de cristal (70 m).
3. **Zona 3**: ruinas, santuario y lago de cristal (164 m). Tiene huecos de agua, rocas que caen y alambre de púas.

## Compilar

**Windows**
1. En Unity: **Guardianes del Bosque › Compilar para Windows**. Se genera `Compilacion/Windows/GuardianesDelBosque.exe`.
2. Con [Inno Setup 6](https://jrsoftware.org/isinfo.php), compila `Setup/GuardianesDelBosque.iss`. Se genera el instalador en `Instalador/`.

**Android (APK)**
1. Instala en Unity Hub el módulo **Android Build Support** (con OpenJDK y Android SDK & NDK).
2. El proyecto debe estar en una ruta **sin tildes ni ñ**, porque las herramientas de Android no funcionan con ellas. Por ejemplo, `C:\JUEGO_SYLVAN_ANDROID\JUEGO_GUARDIAN`.
3. En Unity: **Guardianes del Bosque › Compilar APK para Android**. Se genera `Android/GuardianesDelBosque.apk` (ARM64, horizontal, Android 8.0 o superior).

Las carpetas `Compilacion/`, `Instalador/` y `Android/` no se suben al repositorio.
