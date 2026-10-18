# Exporta los objetos de naturaleza (tronco y rocas) desde Modelos_naturaleza.blend, cada uno a su carpeta:
#   Objetos/<nombre>/<nombre>.fbx  +  <nombre>_color.png
# Cada objeto queda centrado en el origen con la base en el suelo (z = 0) y la textura reducida a 2048.
# Uso: blender --background Modelos_naturaleza/Modelos_naturaleza.blend --python exportar_naturaleza.py
import bpy, os
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
NOMBRES = ["tronco_1", "roca_1", "roca_2", "roca_3"]
TEX = 2048

for nombre in NOMBRES:
    o = bpy.data.objects.get(nombre)
    if o is None:
        print("FALTA", nombre); continue
    out = os.path.join(HERE, nombre)
    os.makedirs(out, exist_ok=True)

    # copia de la malla centrada y apoyada en el suelo
    me = o.data.copy()
    me.transform(o.matrix_world)
    cs = [v.co for v in me.vertices]
    lo = Vector((min(c.x for c in cs), min(c.y for c in cs), min(c.z for c in cs)))
    hi = Vector((max(c.x for c in cs), max(c.y for c in cs), max(c.z for c in cs)))
    off = Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z))
    for v in me.vertices:
        v.co -= off
    o.name = nombre + "_orig"
    tmp = bpy.data.objects.new(nombre, me)
    bpy.context.scene.collection.objects.link(tmp)

    # textura de color del material
    mat = me.materials[0] if me.materials else None
    if mat and mat.use_nodes:
        for n in mat.node_tree.nodes:
            if n.type == 'TEX_IMAGE' and n.image and any(l.to_socket.name == "Base Color" for l in n.outputs[0].links):
                img = n.image.copy()
                if img.size[0] > TEX:
                    img.scale(TEX, TEX)
                img.filepath_raw = os.path.join(out, nombre + "_color.png")
                img.file_format = 'PNG'
                img.save()
                break
        mat.name = "M_" + nombre

    bpy.ops.object.select_all(action='DESELECT')
    tmp.select_set(True)
    bpy.context.view_layer.objects.active = tmp
    bpy.ops.export_scene.fbx(filepath=os.path.join(out, nombre + ".fbx"), use_selection=True,
                             object_types={'MESH'}, apply_scale_options='FBX_SCALE_ALL',
                             axis_forward='-Z', axis_up='Y', mesh_smooth_type='FACE',
                             path_mode='STRIP', bake_anim=False)
    print("EXPORT_OK", nombre, len(me.polygons), [round(x, 3) for x in (hi - lo)])
    bpy.data.objects.remove(tmp)
    o.name = nombre
