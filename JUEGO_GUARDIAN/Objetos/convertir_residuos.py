# Convierte los modelos GLB de residuos (Tripo) en FBX listos para Unity:
# centrados en el origen, a su altura final y con texturas de color y normales a 1024 en PNG.
# Salida: Assets/Guardian/Residuos/<Nombre>.fbx, <Nombre>_Color.png, <Nombre>_Normal.png
# Uso: blender --background --factory-startup --python convertir_residuos.py
import bpy, os
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "Assets", "Guardian", "Residuos")
os.makedirs(OUT, exist_ok=True)
TEX = 1024

RESIDUOS = {  # archivo -> (nombre en Unity, altura en metros antes de la escala del juego)
    "glowing_plastic_bottle.glb": ("Botella", 0.45),
    "red soda can 3d model.glb": ("Lata", 0.32),
    "crumpled paper ball 3d model.glb": ("Papel", 0.30),
}

for fname, (name, height) in RESIDUOS.items():
    src = os.path.join(HERE, fname)
    if not os.path.exists(src):
        print("FALTA", fname)
        continue
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=src)
    obj = next(o for o in bpy.data.objects if o.type == 'MESH')
    obj.name = obj.data.name = name
    bpy.ops.object.select_all(action='DESELECT')
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    # centrar y escalar a la altura deseada
    cs = [v.co for v in obj.data.vertices]
    lo = Vector((min(c.x for c in cs), min(c.y for c in cs), min(c.z for c in cs)))
    hi = Vector((max(c.x for c in cs), max(c.y for c in cs), max(c.z for c in cs)))
    center, s = (lo + hi) / 2, height / (hi.z - lo.z)
    for v in obj.data.vertices:
        v.co = (v.co - center) * s

    # texturas de color y normales (el material URP se crea en Unity)
    obj.data.materials[0].name = "M_" + name
    for img in bpy.data.images:
        low = img.name.lower()
        kind = "Color" if ("color" in low) else "Normal" if ("normal" in low) else None
        if not kind:
            continue
        if img.size[0] > TEX:
            img.scale(TEX, TEX)
        img.filepath_raw = os.path.join(OUT, f"{name}_{kind}.png")
        img.file_format = 'PNG'
        img.save()

    bpy.ops.export_scene.fbx(
        filepath=os.path.join(OUT, name + ".fbx"), use_selection=True, object_types={'MESH'},
        apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
        mesh_smooth_type='FACE', path_mode='STRIP', bake_anim=False)
    print("RESIDUO_OK", name, len(obj.data.polygons), [round(x, 3) for x in obj.dimensions])
