
import math, os, random
from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
JUEGO = os.path.join(HERE, "..", "..")
OUT = os.path.join(JUEGO, "Assets", "Resources", "Menu")
os.makedirs(OUT, exist_ok=True)
FUENTE = r"C:\Windows\Fonts\GILLUBCD.TTF"
FUENTE2 = r"C:\Windows\Fonts\BRLNSDB.TTF"
CREMA = (246, 231, 196)
CONTORNO = (58, 34, 16)
random.seed(5)
S = 2   # supermuestreo


def fuente(path, tam):
    return ImageFont.truetype(path, tam)


def guardar(im, nombre, tam=None):
    if tam:
        im = im.resize(tam, Image.LANCZOS)
    im.save(os.path.join(OUT, nombre))


# ------------------------------------------------------------------ piezas
def madera(w, h, radio, base=(120, 74, 38)):
    """Tablón de madera con vetas, borde oscuro y brillo superior."""
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([0, 0, w - 1, h - 1], radius=radio, fill=(60, 34, 16, 255))
    m = int(min(w, h) * 0.06)
    capa = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    dc = ImageDraw.Draw(capa)
    for y in range(h):
        t = y / h
        c = tuple(int(base[i] * (1.12 - 0.3 * t)) for i in range(3)) + (255,)
        dc.line([(0, y), (w, y)], fill=c)
    mask = Image.new("L", (w, h), 0)
    ImageDraw.Draw(mask).rounded_rectangle([m, m, w - 1 - m, h - 1 - m], radius=max(4, radio - m), fill=255)
    im.paste(capa, (0, 0), mask)
    # vetas
    for i in range(int(h / (14 * S))):
        y = m + random.randint(0, h - 2 * m)
        amp = random.uniform(2, 6) * S
        pts = [(x, y + amp * math.sin(x / (random.uniform(60, 140) * S))) for x in range(m + 10, w - m - 10, 8)]
        d.line(pts, fill=(84, 50, 24, 150), width=max(1, S))
    # separación de tablones
    if h > 220 * S:
        for k in (1, 2):
            yy = m + (h - 2 * m) * k // 3
            d.line([(m + 6, yy), (w - m - 6, yy)], fill=(70, 40, 18, 255), width=3 * S)
    # brillo superior
    d.rounded_rectangle([m + 8 * S, m + 4 * S, w - m - 8 * S, m + 12 * S], radius=6 * S, fill=(255, 220, 170, 50))
    return im


def hoja(d, cx, cy, largo, ancho, ang, color=(104, 176, 56), nervio=(64, 120, 34)):
    pts = []
    for i in range(21):
        t = i / 20
        pts.append((t * largo, ancho * math.sin(math.pi * t) ** 0.8))
    pts += [(x, -y) for x, y in reversed(pts)]
    ca, sa = math.cos(ang), math.sin(ang)
    d.polygon([(cx + x * ca - y * sa, cy + x * sa + y * ca) for x, y in pts], fill=color, outline=(48, 92, 26))
    d.line([(cx, cy), (cx + largo * 0.9 * ca, cy + largo * 0.9 * sa)], fill=nervio, width=2 * S)


def liana(d, pts, ancho=6):
    d.line(pts, fill=(52, 96, 30), width=ancho * S)
    d.line(pts, fill=(96, 150, 50), width=max(1, ancho * S // 3))


def texto(d, xy, s, f, fill=CREMA, borde=CONTORNO, grosor=6, anchor="mm", sombra=True):
    if sombra:
        d.text((xy[0] + 3 * S, xy[1] + 4 * S), s, font=f, fill=(30, 16, 6, 160), anchor=anchor,
               stroke_width=grosor * S, stroke_fill=(30, 16, 6, 160))
    d.text(xy, s, font=f, fill=fill, anchor=anchor, stroke_width=grosor * S, stroke_fill=borde)


def medallon(im, cx, cy, r, icono):
    d = ImageDraw.Draw(im)
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(62, 36, 16), outline=(40, 22, 8), width=3 * S)
    d.ellipse([cx - r + 8 * S, cy - r + 8 * S, cx + r - 8 * S, cy + r - 8 * S], fill=(140, 88, 46))
    d.ellipse([cx - r + 16 * S, cy - r + 16 * S, cx + r - 16 * S, cy + r - 16 * S], fill=(104, 62, 30))
    ri = r - 26 * S
    if icono == "play":
        d.polygon([(cx - ri * 0.45, cy - ri * 0.7), (cx - ri * 0.45, cy + ri * 0.7), (cx + ri * 0.75, cy)], fill=CREMA,
                  outline=CONTORNO)
    elif icono == "puerta":
        d.rectangle([cx - ri * 0.55, cy - ri * 0.75, cx + ri * 0.15, cy + ri * 0.75], fill=CREMA, outline=CONTORNO, width=2 * S)
        d.ellipse([cx - ri * 0.08, cy - 3 * S, cx + 0.02 * ri + 4 * S, cy + 5 * S], fill=CONTORNO)
        d.line([(cx + ri * 0.3, cy), (cx + ri * 0.8, cy)], fill=CREMA, width=6 * S)
        d.polygon([(cx + ri * 0.8, cy - ri * 0.25), (cx + ri * 0.8, cy + ri * 0.25), (cx + ri, cy)], fill=CREMA)
    elif icono == "check":
        d.line([(cx - ri * 0.6, cy), (cx - ri * 0.15, cy + ri * 0.5), (cx + ri * 0.65, cy - ri * 0.55)], fill=CREMA, width=12 * S,
               joint="curve")
    elif icono == "atras":
        d.line([(cx + ri * 0.6, cy), (cx - ri * 0.3, cy)], fill=CREMA, width=10 * S)
        d.polygon([(cx - ri * 0.75, cy), (cx - ri * 0.2, cy - ri * 0.5), (cx - ri * 0.2, cy + ri * 0.5)], fill=CREMA)
    elif icono == "candado":
        d.arc([cx - ri * 0.38, cy - ri * 0.85, cx + ri * 0.38, cy - ri * 0.05], 180, 360, fill=CREMA, width=8 * S)
        d.line([(cx - ri * 0.38, cy - ri * 0.45), (cx - ri * 0.38, cy - ri * 0.1)], fill=CREMA, width=8 * S)
        d.line([(cx + ri * 0.38, cy - ri * 0.45), (cx + ri * 0.38, cy - ri * 0.1)], fill=CREMA, width=8 * S)
        d.rounded_rectangle([cx - ri * 0.6, cy - ri * 0.15, cx + ri * 0.6, cy + ri * 0.7], radius=6 * S, fill=CREMA,
                            outline=CONTORNO, width=2 * S)
        d.ellipse([cx - 7 * S, cy + ri * 0.12, cx + 7 * S, cy + ri * 0.12 + 14 * S], fill=CONTORNO)
        d.rectangle([cx - 3 * S, cy + ri * 0.12 + 10 * S, cx + 3 * S, cy + ri * 0.5], fill=CONTORNO)
    elif icono == "casa":
        d.polygon([(cx, cy - ri * 0.8), (cx - ri * 0.8, cy - ri * 0.05), (cx + ri * 0.8, cy - ri * 0.05)], fill=CREMA,
                  outline=CONTORNO)
        d.rectangle([cx - ri * 0.55, cy - ri * 0.1, cx + ri * 0.55, cy + ri * 0.7], fill=CREMA, outline=CONTORNO, width=2 * S)
        d.rectangle([cx - ri * 0.17, cy + ri * 0.2, cx + ri * 0.17, cy + ri * 0.7], fill=CONTORNO)
    elif icono.endswith(".png"):
        ret = Image.open(icono).convert("RGBA").resize((2 * ri, 2 * ri), Image.LANCZOS)
        m = Image.new("L", (2 * ri, 2 * ri), 0)
        ImageDraw.Draw(m).ellipse([0, 0, 2 * ri - 1, 2 * ri - 1], fill=255)
        fondo = Image.new("RGBA", (2 * ri, 2 * ri), (230, 222, 190, 255))
        fondo.alpha_composite(ret)
        im.paste(fondo, (cx - ri, cy - ri), m)


# ------------------------------------------------------------------ fondo
def fondo():
    src = Image.open(os.path.join(JUEGO, "Fondos", "fondo 1.jpg")).convert("RGB")
    W, H = src.size
    w = int(H * 16 / 9)
    im = src.crop((0, 0, w, H)).resize((1920, 1080), Image.LANCZOS)
    # degradado oscuro suave a la izquierda para que el título y los botones se lean mejor
    sombra = Image.new("L", (1920, 1080), 0)
    ds = ImageDraw.Draw(sombra)
    for x in range(900):
        ds.line([(x, 0), (x, 1080)], fill=int(90 * (1 - x / 900) ** 1.5))
    im = Image.composite(Image.new("RGB", im.size, (10, 24, 8)), im, sombra)
    im.save(os.path.join(OUT, "fondo_menu.jpg"), quality=92)


# ------------------------------------------------------------------ título
def titulo():
    W, H = 820 * S, 470 * S
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    # poste y cartel colgante
    im.alpha_composite(madera(110 * S, 70 * S, 10 * S, (110, 66, 32)), ((W - 110 * S) // 2, 0))
    tabla = madera(760 * S, 330 * S, 34 * S)
    im.alpha_composite(tabla, (30 * S, 50 * S))
    d = ImageDraw.Draw(im)
    texto(d, (W // 2, 150 * S), "GUARDIANES", fuente(FUENTE, 104 * S), fill=(132, 196, 64), borde=(28, 60, 16), grosor=7)
    texto(d, (W // 2, 270 * S), "DEL BOSQUE", fuente(FUENTE, 100 * S), grosor=7)
    # cinta inferior
    cinta = madera(600 * S, 66 * S, 18 * S, (92, 54, 26))
    im.alpha_composite(cinta, ((W - 600 * S) // 2, 372 * S))
    d = ImageDraw.Draw(im)
    texto(d, (W // 2, 405 * S), "Pequeñas acciones, grandes cambios", fuente(FUENTE2, 34 * S), grosor=3)
    # hojas en las esquinas
    for (x, y, l, a) in ((50, 70, 80, -2.5), (40, 110, 70, 3.0), (760, 70, 80, -0.6), (770, 110, 70, 0.2),
                         (60, 340, 70, 2.6), (760, 340, 70, 0.5), (300, 40, 55, -1.9), (520, 40, 55, -1.2)):
        hoja(d, x * S, y * S, l * S, 22 * S, a)
    guardar(im, "titulo.png", (W // S, H // S))


# ------------------------------------------------------------------ botones
def boton(nombre, etiqueta, icono, tam_txt=78):
    W, H = 700 * S, 190 * S
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    im.alpha_composite(madera(620 * S, 160 * S, 30 * S), (40 * S, 15 * S))
    d = ImageDraw.Draw(im)
    lineas = etiqueta.split("\n")
    f = fuente(FUENTE, (tam_txt if len(lineas) == 1 else 56) * S)
    cx = 40 * S + (620 * S - 150 * S) // 2 + 10 * S
    if len(lineas) == 1:
        texto(d, (cx, H // 2), etiqueta, f, grosor=6)
    else:
        texto(d, (cx, H // 2 - 32 * S), lineas[0], f, grosor=5)
        texto(d, (cx, H // 2 + 32 * S), lineas[1], f, grosor=5)
    medallon(im, 40 * S + 620 * S - 92 * S, H // 2, 70 * S, icono)
    d = ImageDraw.Draw(im)
    # lianas y hojas
    liana(d, [(470 * S, 10 * S), (500 * S, 40 * S), (530 * S, 22 * S), (570 * S, 34 * S), (600 * S, 12 * S)])
    liana(d, [(40 * S, 150 * S), (70 * S, 176 * S), (110 * S, 168 * S)], 5)
    for (x, y, l, a) in ((36, 40, 60, 3.4), (30, 80, 52, 2.8), (36, 150, 55, 2.3), (660, 40, 60, -0.3), (665, 150, 55, 0.5),
                         (520, 18, 40, -1.4), (95, 170, 40, 1.4)):
        hoja(d, x * S, y * S, l * S, 18 * S, a)
    guardar(im, nombre, (W // S, H // S))


# ------------------------------------------------------------------ selección de guardián
def panel():
    W, H = 1300 * S, 820 * S
    im = madera(W, H, 50 * S, (104, 64, 32))
    d = ImageDraw.Draw(im)
    texto(d, (W // 2, 90 * S), "SELECCIONA TU GUARDIÁN", fuente(FUENTE, 72 * S), grosor=6)
    for (x, y, l, a) in ((40, 50, 90, -2.4), (30, 110, 70, 3.1), (1260, 50, 90, -0.7), (1270, 110, 70, 0.1),
                         (50, 770, 80, 2.4), (1250, 770, 80, 0.7)):
        hoja(d, x * S, y * S, l * S, 26 * S, a)
    guardar(im, "panel_seleccion.png", (W // S, H // S))


def tarjeta(nombre, borde, brillo):
    W, H = 400 * S, 520 * S
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    if brillo:
        g = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        ImageDraw.Draw(g).rounded_rectangle([10 * S, 10 * S, W - 10 * S, H - 10 * S], radius=40 * S, outline=borde + (255,),
                                            width=22 * S)
        im.alpha_composite(g.filter(ImageFilter.GaussianBlur(12 * S)))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([20 * S, 20 * S, W - 20 * S, H - 20 * S], radius=34 * S, fill=(244, 234, 206, 255), outline=borde + (255,),
                        width=10 * S)
    d.rounded_rectangle([44 * S, 44 * S, W - 44 * S, 380 * S], radius=24 * S, fill=(214, 228, 190, 255))
    guardar(im, nombre, (W // S, H // S))


def aura():
    n = 900
    im = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    g = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    dg = ImageDraw.Draw(g)
    for r in range(n // 2, 0, -4):
        a = int(150 * (1 - r / (n / 2)) ** 1.6)
        dg.ellipse([n / 2 - r, n / 2 - r, n / 2 + r, n / 2 + r], fill=(170, 255, 140, a))
    im.alpha_composite(g)
    d = ImageDraw.Draw(im)
    for k, (rx, ry) in enumerate(((380, 180), (330, 150))):
        d.ellipse([n / 2 - rx, n / 2 - ry + 120, n / 2 + rx, n / 2 + ry + 120], outline=(200, 255, 170, 170 - 50 * k), width=6)
    for i in range(70):   # chispas
        x, y = random.uniform(80, n - 80), random.uniform(80, n - 80)
        r = random.uniform(2, 6)
        d.ellipse([x - r, y - r, x + r, y + r], fill=(230, 255, 190, random.randint(120, 230)))
    im.save(os.path.join(OUT, "aura_guardian.png"))


def personajes():
    for n in ("sylvan", "maya"):
        im = Image.open(os.path.join(HERE, f"{n}_cuerpo.png")).convert("RGBA")
        im.crop(im.getbbox()).save(os.path.join(OUT, f"{n}_menu.png"))


RET = os.path.join(JUEGO, "Assets", "Guardian", "Retratos")
fondo()
titulo()
boton("btn_jugar.png", "JUGAR", "play", 92)
boton("btn_seleccionar.png", "SELECCIONAR\nGUARDIÁN", os.path.join(RET, "Retrato_Guardian.png"))
boton("btn_salir.png", "SALIR", "puerta", 92)
boton("btn_confirmar.png", "CONFIRMAR", "check", 70)
boton("btn_volver.png", "VOLVER", "atras", 80)
boton("btn_continuar.png", "CONTINUAR\nZONA 2", "play")   # fin de la Zona 1
boton("btn_continuar_3.png", "CONTINUAR\nZONA 3", "play")   # fin de la Zona 2
boton("btn_zona4.png", "ZONA 4\nPRÓXIMAMENTE", "candado")   # fin de la Zona 3
boton("btn_volver_inicio.png", "VOLVER AL\nINICIO", "casa")
panel()
tarjeta("tarjeta.png", (122, 84, 44), False)
tarjeta("tarjeta_sel.png", (246, 196, 64), True)
aura()
personajes()
for n in ("Retrato_Guardian.png", "Retrato_Guardiana.png"):
    Image.open(os.path.join(RET, n)).save(os.path.join(OUT, n.replace("Retrato_Guardiana", "retrato_maya").replace("Retrato_Guardian", "retrato_sylvan")))
print("MENU_OK", sorted(os.listdir(OUT)))
