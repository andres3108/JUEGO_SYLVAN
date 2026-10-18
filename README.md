# Guardianes del Bosque

Videojuego 2.5D de plataformas hecho en **Unity 6.6 (6000.6.0f1)** con URP para Diseño Interactivo.
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
| `JUEGO_GUARDIAN/Fondos` | Imágenes de fondo de cada zona y scripts que generan el menú y el HUD |

## Cómo abrirlo

1. En Unity Hub, usa **Añadir › Añadir proyecto desde el disco** y elige la carpeta `JUEGO_GUARDIAN`.
2. Ábrelo con Unity **6000.6.0f1**. La primera vez tarda unos minutos porque Unity regenera la carpeta `Library`.
3. Abre la escena `Assets/Scenes/MenuPrincipal` y pulsa Play.

## Controles

- Moverse: **A / D** o **← / →**
- Saltar: **Espacio**, **W** o **↑**
- Menús: ratón, o flechas + **Enter**

## Zonas

1. **Zona 1**: sendero del bosque (70 m).
2. **Zona 2**: del bosque a la cueva de cristal (70 m).
3. **Zona 3**: ruinas, santuario y lago de cristal (164 m). Tiene huecos de agua, rocas que caen y alambre de púas.
