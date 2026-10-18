# Modela el alambre de púas (rollo en espiral con nudos de púas cada pocos centímetros, como la imagen
# de referencia Downloads/Alambrede puaz.jpg) en estilo low-poly y lo exporta a FBX:
#   Objetos/alambre_1/alambre_1.fbx + alambre_1_color.png   (copia para usar en otras zonas)
#   Assets/Guardian/Naturaleza/alambre_1/...                  (lo usa el juego, como tronco_1 y las rocas)
# El rollo está girado ~35° para que, visto de lado en el juego, las vueltas se vean como óvalos.
# Uso: blender --background --python crear_alambre.py
import math, os, random, shutil
import bpy, bmesh
from mathutils import Vector, Matrix

HERE = os.path.dirname(os.path.abspath(__file__))
NOMBRE = "alambre_1"
OUT = os.path.join(HERE, NOMBRE)
JUEGO = os.path.join(HERE, "..", "Assets", "Guardian", "Naturaleza", NOMBRE)
random.seed(7)

LARGO, RADIO, VUELTAS = 1.9, 0.42, 8       # rollo (m)
GROSOR = 0.018                              # radio del alambre
LADOS = 5                                   # lados del tubo (low-poly)
SEP_PUAS = 0.15                             # distancia entre nudos de púas a lo largo del alambre

bpy.ops.wm.read_factory_settings(use_empty=True)
bm = bmesh.new()


def tubo(puntos, r=GROSOR, lados=LADOS):
    """Tubo low-poly que sigue una lista de puntos."""
    anillos = []
    for i, p in enumerate(puntos):
        t = (puntos[min(i + 1, len(puntos) - 1)] - puntos[max(i - 1, 0)]).normalized()
        a = t.cross(Vector((0, 0, 1)))
        if a.length < 1e-3:
            a = t.cross(Vector((0, 1, 0)))
        a.normalize()
        b = t.cross(a).normalized()
        anillos.append([bm.verts.new(p + (a * math.cos(2 * math.pi * k / lados) + b * math.sin(2 * math.pi * k / lados)) * r)
                        for k in range(lados)])
    for r0, r1 in zip(anillos, anillos[1:]):
        for k in range(lados):
            bm.faces.new((r0[k], r0[(k + 1) % lados], r1[(k + 1) % lados], r1[k]))
    return anillos


def pua(centro, direccion, largo=0.06, base=0.009):
    """Púa: pirámide de 3 lados."""
    d = direccion.normalized()
    a = d.cross(Vector((0.3, 0.2, 1))).normalized()
    b = d.cross(a).normalized()
    base_v = [bm.verts.new(centro + (a * math.cos(2 * math.pi * k / 3) + b * math.sin(2 * math.pi * k / 3)) * base)
              for k in range(3)]
    punta = bm.verts.new(centro + d * largo)
    for k in range(3):
        bm.faces.new((base_v[k], base_v[(k + 1) % 3], punta))
    bm.faces.new(tuple(reversed(base_v)))


def nudo(p, t):
    """Nudo del alambre (anillo más grueso) con 4 púas hacia fuera."""
    t = t.normalized()
    tubo([p - t * 0.022, p + t * 0.022], r=GROSOR * 1.9, lados=6)
    a = t.cross(Vector((0, 0, 1)))
    if a.length < 1e-3:
        a = t.cross(Vector((0, 1, 0)))
    a.normalize()
    b = t.cross(a).normalized()
    giro = random.uniform(0, math.pi)
    for k in range(4):
        ang = giro + k * math.pi / 2
        d = a * math.cos(ang) + b * math.sin(ang) + t * random.uniform(-0.35, 0.35)
        pua(p, d, largo=random.uniform(0.05, 0.075))


def con_puas(puntos):
    tubo(puntos)
    acum = SEP_PUAS * random.uniform(0.3, 1.0)
    for p0, p1 in zip(puntos, puntos[1:]):
        seg = (p1 - p0).length
        while acum < seg:
            nudo(p0.lerp(p1, acum / seg), p1 - p0)
            acum += SEP_PUAS * random.uniform(0.8, 1.2)
        acum -= seg


# 1. espiral principal (eje X), un poco irregular como en la referencia
pasos = VUELTAS * 28
espiral = []
for i in range(pasos + 1):
    u = i / pasos
    ang = 2 * math.pi * VUELTAS * u
    r = RADIO * (1 + 0.08 * math.sin(ang * 0.5 + 1.3) + random.uniform(-0.02, 0.02))
    x = -LARGO / 2 + LARGO * u + 0.05 * math.sin(ang)
    espiral.append(Vector((x, r * math.cos(ang), RADIO + r * math.sin(ang))))
# cola suelta al principio (como en la imagen)
cola = [espiral[0] + Vector((-0.04 * k, 0.02 * k, -0.035 * k * (1 + 0.15 * k))) for k in range(6, 0, -1)]
con_puas(cola + espiral)

# 2. dos alambres rectos a lo largo del rollo que lo sujetan
for ang in (math.radians(200), math.radians(330)):
    pts = [Vector((-LARGO / 2 + LARGO * k / 10, RADIO * 0.95 * math.cos(ang), RADIO + RADIO * 0.95 * math.sin(ang)))
           for k in range(11)]
    con_puas(pts)

bm.normal_update()
me = bpy.data.meshes.new(NOMBRE)
bm.to_mesh(me)
bm.free()
me.uv_layers.new(name="UVMap")          # color uniforme: todas las caras usan la misma textura gris
ob = bpy.data.objects.new(NOMBRE, me)
bpy.context.scene.collection.objects.link(ob)

# girado para que en la vista lateral del juego se vean las vueltas como óvalos; base en z = 0
me.transform(Matrix.Rotation(math.radians(35), 4, 'Z'))
zmin = min(v.co.z for v in me.vertices)
me.transform(Matrix.Translation((0, 0, -zmin)))
for p in me.polygons:
    p.use_smooth = False

mat = bpy.data.materials.new("M_" + NOMBRE)
me.materials.append(mat)

# textura de color: gris metálico azulado como la referencia
os.makedirs(OUT, exist_ok=True)
img = bpy.data.images.new(NOMBRE + "_color", 16, 16)
col = (0.47, 0.49, 0.55, 1.0)
img.pixels = [c for _ in range(16 * 16) for c in col]
img.filepath_raw = os.path.join(OUT, NOMBRE + "_color.png")
img.file_format = 'PNG'
img.save()

bpy.ops.object.select_all(action='DESELECT')
ob.select_set(True)
bpy.context.view_layer.objects.active = ob
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, NOMBRE + ".fbx"), use_selection=True,
                         object_types={'MESH'}, apply_scale_options='FBX_SCALE_ALL',
                         axis_forward='-Z', axis_up='Y', mesh_smooth_type='FACE',
                         path_mode='STRIP', bake_anim=False)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, NOMBRE + ".blend"))

os.makedirs(JUEGO, exist_ok=True)
for f in (NOMBRE + ".fbx", NOMBRE + "_color.png"):
    shutil.copy(os.path.join(OUT, f), os.path.join(JUEGO, f))
d = ob.dimensions
print("ALAMBRE_OK", len(me.polygons), "caras", [round(x, 2) for x in d])
