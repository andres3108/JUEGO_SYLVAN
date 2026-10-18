
import os
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "..", "Assets", "Guardian", "Fondo", "Zona3")
OVERLAP = 220
partes = [Image.open(os.path.join(HERE, f"zona3_{i}.jpg")).convert("RGB") for i in range(2, 8)]
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
mitad = total // 2
pano.crop((0, 0, mitad, H)).save(os.path.join(OUT, "Fondo_Zona3_A.jpg"), quality=92)
pano.crop((mitad, 0, total, H)).save(os.path.join(OUT, "Fondo_Zona3_B.jpg"), quality=92)
pano.resize((total // 8, H // 8)).save(os.path.join(HERE, "panorama_preview.jpg"), quality=85)
print("PANORAMA_OK", pano.size, "mitades de", mitad, "px")
