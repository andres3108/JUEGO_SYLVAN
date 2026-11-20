# Genera los rótulos de los mensajes del juego (versión 2: texto claro sobre una franja suave) en
# Assets/Resources/Mensajes: título de cada misión, derrota, victoria de zona y victoria final.
# Cada texto es un PNG transparente con la letra del menú (Gill Sans Ultra Bold / Berlin Sans) y su
# borde marrón. El juego busca cada imagen por el texto convertido a "slug" (GameManager/Rotulos.cs):
# "¡Bosque limpio!" -> bosque_limpio.png. Si falta una imagen, el juego usa texto normal.
# También crea la franja de fondo y las hojas del confeti.
# Uso: python generar_mensajes.py
import os, re, unicodedata
from PIL import Image, ImageDraw, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
GEN = os.path.join(HERE, "generar_menu.py")
src = open(GEN, encoding="utf-8").read()
g = {"__file__": GEN, "__name__": "gen"}
exec(compile(src[:src.index("RET = os.path.join")], GEN, "exec"), g)   # solo las funciones de dibujo
S, hoja, texto, fuente = (g[k] for k in ("S", "hoja", "texto", "fuente"))
F1, F2, CREMA = g["FUENTE"], g["FUENTE2"], g["CREMA"]
OUT = os.path.join(HERE, "..", "..", "Assets", "Resources", "Mensajes")
os.makedirs(OUT, exist_ok=True)

ORO, VERDE, NARANJA = (255, 214, 92), (170, 226, 110), (246, 132, 82)


def slug(s):
    s = unicodedata.normalize("NFD", s.lower())
    s = "".join(c for c in s if unicodedata.category(c) != "Mn")
    return re.sub(r"[^a-z0-9]+", "_", s).strip("_")


def rotulo(s, f, fill, grosor, sombra=True, hojas=False):
    """Texto suelto recortado a su tamaño, con borde (y sombra) como en el menú."""
    w = int(f.getlength(s)) + 200 * S
    h = int(f.size * 1.6) + 60 * S
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    texto(d, (w // 2, h // 2), s, f, fill=fill, grosor=grosor, sombra=sombra)
    if hojas:   # dos hojitas a los lados del texto
        sep = f.getlength(s) / 2 + 26 * S
        hoja(d, w / 2 - sep, h / 2, 30 * S, 11 * S, 3.4)
        hoja(d, w / 2 + sep, h / 2, 30 * S, 11 * S, -0.25)
    im = im.crop(im.getbbox())
    im.save(os.path.join(OUT, slug(s) + ".png"))
    return im.size


# textos de las escenas (misionTitulo "Misión N: título" y tituloVictoria) y mensajes fijos
MISIONES = ["Limpia el sendero del bosque", "Limpia el bosque y la cueva de cristal",
            "Limpia las ruinas y el santuario de cristal"]
VICTORIAS = ["¡Bosque limpio!", "¡Cueva limpia!", "¡Santuario limpio!"]

for n in (1, 2, 3):
    rotulo(f"MISIÓN {n}", fuente(F1, 24 * S), VERDE, 3, sombra=False, hojas=True)
    rotulo(f"MISIÓN {n} COMPLETADA", fuente(F1, 24 * S), VERDE, 3, sombra=False, hojas=True)
for m in MISIONES:
    rotulo(m, fuente(F1, 46 * S), CREMA, 5)
for v in VICTORIAS:
    rotulo(v.upper(), fuente(F1, 72 * S), ORO, 7)
rotulo("¡INTÉNTALO DE NUEVO!", fuente(F1, 64 * S), NARANJA, 6)
rotulo("¡MISIÓN CUMPLIDA!", fuente(F1, 70 * S), ORO, 7)
rotulo("El bosque cuenta contigo, Guardián", fuente(F2, 26 * S), CREMA, 3, sombra=False)
rotulo("Eres un verdadero Guardián del Bosque", fuente(F2, 26 * S), CREMA, 3, sombra=False)

# franja: banda verde oscura difuminada arriba y abajo, con dos filetes dorados (se estira a lo ancho)
W, H, BANDA = 1280, 300, 200
fr = Image.new("RGBA", (W, H), (0, 0, 0, 0))
ImageDraw.Draw(fr).rectangle([0, (H - BANDA) // 2, W, (H + BANDA) // 2], fill=(18, 40, 14, 150))
fr = fr.filter(ImageFilter.GaussianBlur(14))
d = ImageDraw.Draw(fr)
for y in ((H - BANDA) // 2, (H + BANDA) // 2):
    d.line([(W * 0.25, y), (W * 0.75, y)], fill=ORO + (170,), width=2)
fr.save(os.path.join(OUT, "franja.png"))

# hojas del confeti (verde y dorada)
for nombre, col in (("hoja_verde", (104, 176, 56)), ("hoja_dorada", (255, 214, 92))):
    im = Image.new("RGBA", (72 * S, 32 * S), (0, 0, 0, 0))
    hoja(ImageDraw.Draw(im), 4 * S, 16 * S, 64 * S, 13 * S, 0, color=col, nervio=(60, 100, 30))
    im.crop(im.getbbox()).save(os.path.join(OUT, nombre + ".png"))

print("MENSAJES_OK", sorted(f for f in os.listdir(OUT) if f.endswith(".png")))
