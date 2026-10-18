
import os
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "..", "Assets", "Guardian", "Fondo", "Zona2", "Fondo_Zona2.jpg")
OVERLAP = 220
partes = [Image.open(os.path.join(HERE, n)).convert("RGB") for n in
          ("zona2_1_bosque.png", "zona2_2_transicion.png", "zona2_3_cueva.png")]
W, H = partes[0].size
total = W * len(partes) - OVERLAP * (len(partes) - 1)
pano = Image.new("RGB", (total, H))
x = 0
for i, p in enumerate(partes):
    if i == 0:
        pano.paste(p, (0, 0))
    else:
        mask = Image.new("L", (W, H), 255)
        grad = Image.linear_gradient("L").rotate(90).resize((OVERLAP, H))   # 0 -> 255 de izquierda a derecha
        mask.paste(grad, (0, 0))
        pano.paste(p, (x, 0), mask)
    x += W - OVERLAP
pano.save(OUT, quality=92)
print("PANORAMA_OK", pano.size, OUT)
