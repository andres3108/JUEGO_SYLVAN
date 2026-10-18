# Genera las imágenes del HUD (Assets/Resources/HUD): marco de madera con lianas para la barra de
# "Salud del Bosque", su relleno verde y las cajas de los contadores de reciclaje.
# Uso: python generar_hud.py
import math, os, random
from PIL import Image, ImageDraw, ImageFilter

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "Resources", "HUD")
os.makedirs(OUT, exist_ok=True)
random.seed(3)
S = 2  # supermuestreo para bordes suaves

def guardar(im, nombre, tam):
    im.resize(tam, Image.LANCZOS).save(os.path.join(OUT, nombre))

def hoja(d, cx, cy, largo, ancho, ang, color, nervio):
    pts = []
    for i in range(21):
        t = i / 20
        w = ancho * math.sin(math.pi * t) ** 0.8
        pts.append((t * largo, w))
    pts += [(x, -y) for x, y in reversed(pts)]
    ca, sa = math.cos(ang), math.sin(ang)
    poly = [(cx + x * ca - y * sa, cy + x * sa + y * ca) for x, y in pts]
    d.polygon(poly, fill=color)
    d.line([(cx, cy), (cx + largo * 0.9 * ca, cy + largo * 0.9 * sa)], fill=nervio, width=2 * S)

# ------------------------------------------------------------------ marco de la barra (520 x 90)
W, H = 520 * S, 90 * S
im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
d = ImageDraw.Draw(im)
x0, y0, x1, y1 = 40 * S, 22 * S, 505 * S, 70 * S
d.rounded_rectangle([x0, y0, x1, y1], radius=24 * S, fill=(92, 62, 36, 255))            # madera
d.rounded_rectangle([x0 + 3 * S, y0 + 3 * S, x1 - 3 * S, y0 + 14 * S], radius=10 * S, fill=(128, 90, 55, 255))
for i in range(9):                                                                     # vetas
    yy = y0 + random.randint(8, 42) * S
    d.line([(x0 + 20 * S, yy), (x1 - 20 * S, yy + random.randint(-3, 3) * S)], fill=(76, 50, 28, 255), width=S)
d.rounded_rectangle([x0 + 9 * S, y0 + 9 * S, x1 - 9 * S, y1 - 9 * S], radius=15 * S, fill=(38, 28, 20, 255))  # canal
guardar(im, "barra_marco.png", (520, 90))
# capa superior: liana y hojas (va encima del relleno)
im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
d = ImageDraw.Draw(im)
# liana enroscada a lo largo
pts = [(x0 - 10 * S + t, (y0 + y1) / 2 + 22 * S * math.sin(t / (38 * S))) for t in range(0, x1 - x0 + 20 * S, 3)]
d.line(pts, fill=(64, 44, 22, 255), width=6 * S)
d.line(pts, fill=(104, 74, 40, 255), width=2 * S)
# hojas en el extremo izquierdo y algunas a lo largo
for (cx, cy, l, a) in ((52, 28, 34, -2.4), (40, 48, 38, 3.0), (52, 66, 32, 2.3), (30, 40, 26, -2.9)):
    hoja(d, cx * S, cy * S, l * S, 10 * S, a, (92, 168, 52, 255), (58, 118, 34, 255))
for xx in (170, 300, 430):
    hoja(d, xx * S, 20 * S, 22 * S, 7 * S, -1.2, (110, 180, 60, 255), (66, 126, 38, 255))
guardar(im, "barra_liana.png", (520, 90))
# rectángulo del canal en coordenadas finales (para colocar el relleno en Unity): x 49..496, y 31..61
print("canal", 49, 31, 496, 61)

# ------------------------------------------------------------------ relleno verde (447 x 30)
W, H = 447 * S, 30 * S
im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
d = ImageDraw.Draw(im)
for y in range(H):
    t = y / (H - 1)
    c = (int(70 + 80 * (1 - t)), int(170 + 60 * (1 - t)), int(40 + 20 * (1 - t)), 255)
    d.line([(0, y), (W, y)], fill=c)
mask = Image.new("L", (W, H), 0)
ImageDraw.Draw(mask).rounded_rectangle([0, 0, W - 1, H - 1], radius=13 * S, fill=255)
im.putalpha(mask)
hl = Image.new("RGBA", (W, H), (0, 0, 0, 0))
ImageDraw.Draw(hl).rounded_rectangle([8 * S, 4 * S, W - 8 * S, 10 * S], radius=4 * S, fill=(255, 255, 255, 90))
im = Image.alpha_composite(im, hl)
guardar(im, "barra_relleno.png", (447, 30))

# ------------------------------------------------------------------ cajas de los contadores
def caja(nombre, borde):
    n = 120 * S
    im = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    glow = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    ImageDraw.Draw(glow).rounded_rectangle([6 * S, 6 * S, n - 6 * S, n - 6 * S], radius=22 * S, outline=borde + (255,), width=10 * S)
    im = Image.alpha_composite(im, glow.filter(ImageFilter.GaussianBlur(5 * S)))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([10 * S, 10 * S, n - 10 * S, n - 10 * S], radius=18 * S, fill=(30, 36, 30, 235), outline=borde + (255,), width=4 * S)
    d.rounded_rectangle([16 * S, 16 * S, n - 16 * S, 40 * S], radius=10 * S, fill=(255, 255, 255, 22))
    guardar(im, nombre, (120, 120))

caja("caja_plastico.png", (70, 160, 255))
caja("caja_latas.png", (245, 170, 40))
caja("caja_papel.png", (120, 200, 70))

# ------------------------------------------------------------------ panel de fondo de los contadores
n_w, n_h = 330 * S, 175 * S
im = Image.new("RGBA", (n_w, n_h), (0, 0, 0, 0))
d = ImageDraw.Draw(im)
d.rounded_rectangle([0, 0, n_w - 1, n_h - 1], radius=18 * S, fill=(18, 30, 18, 170), outline=(120, 90, 50, 220), width=3 * S)
guardar(im, "panel_contadores.png", (330, 175))
print("HUD_OK")
