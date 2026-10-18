# Convierte "Guardiana.glb" (Tripo, una sola malla en pose T) en un personaje jugable:
#  1) suelda, reduce polígonos y hornea su textura sobre UV nuevas,
#  2) le pone un esqueleto provisional en su pose T y baja los brazos a una pose de pie,
#  3) le crea el esqueleto definitivo (mismos nombres de huesos que el Guardián) y las mismas animaciones,
#  4) exporta Guardiana_del_Bosque.fbx + textura y genera su retrato para la pantalla de selección.
# Uso: blender --background --factory-startup --python preparar_guardiana.py
import bpy, bmesh, math, os
from mathutils import Vector, Matrix

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "Guardiana.glb")
ANIM_SRC = os.path.join(HERE, "Personaje Guardian del Bosque", "crear_guardian.py")
UNITY_DIR = os.path.join(HERE, "..", "Assets", "Guardian", "Guardiana")
os.makedirs(UNITY_DIR, exist_ok=True)
S = 1.6 / 0.98          # misma escala que el Guardián (modelo de Tripo de ~0.98 m -> 1.6 m)
TRIS, TEX = 26000, 2048

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
bpy.ops.import_scene.gltf(filepath=SRC)
hi = next(o for o in bpy.data.objects if o.type == 'MESH')
bpy.ops.object.select_all(action='DESELECT')
hi.select_set(True); bpy.context.view_layer.objects.active = hi
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

def P(x, y, z):   # coordenadas medidas en el modelo original -> escala final
    return Vector((x, y, z)) * S

# ---------------------------------------------------------------- 1) reducir y hornear textura
lo = hi.copy(); lo.data = hi.data.copy(); scene.collection.objects.link(lo)
lo.name = lo.data.name = "Guardiana_Mesh"
bpy.ops.object.select_all(action='DESELECT')
lo.select_set(True); bpy.context.view_layer.objects.active = lo
bm = bmesh.new(); bm.from_mesh(lo.data)
bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0002)
bm.to_mesh(lo.data); bm.free()
m = lo.modifiers.new("Decimate", 'DECIMATE')
m.ratio = min(1.0, TRIS / sum(len(p.vertices) - 2 for p in lo.data.polygons))
m.use_collapse_triangulate = True
bpy.ops.object.modifier_apply(modifier=m.name)
while len(lo.data.uv_layers):
    lo.data.uv_layers.remove(lo.data.uv_layers[0])
lo.data.uv_layers.new(name="UVMap")
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.003)
bpy.ops.object.mode_set(mode='OBJECT')
img = bpy.data.images.new("Guardiana_tex", TEX, TEX)
mat = bpy.data.materials.new("M_Guardiana")
mat.use_nodes = True
tex = mat.node_tree.nodes.new('ShaderNodeTexImage'); tex.image = img
bsdf = mat.node_tree.nodes.get("Principled BSDF")
bsdf.inputs["Roughness"].default_value = 0.75
mat.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
mat.node_tree.nodes.active = tex
lo.data.materials.clear(); lo.data.materials.append(mat)
scene.render.engine = 'CYCLES'; scene.cycles.device = 'CPU'; scene.cycles.samples = 4
bpy.ops.object.select_all(action='DESELECT')
hi.select_set(True); lo.select_set(True); bpy.context.view_layer.objects.active = lo
bpy.ops.object.bake(type='DIFFUSE', pass_filter={'COLOR'}, use_selected_to_active=True,
                    cage_extrusion=0.006, max_ray_distance=0.03, margin=8, use_clear=True)
tex_path = os.path.join(UNITY_DIR, "Guardiana_tex.png")
img.filepath_raw = tex_path; img.file_format = 'PNG'; img.save(); img.filepath = tex_path
bpy.data.objects.remove(hi, do_unlink=True)
# quitar fragmentos sueltos pequeños que deja la reducción (p. ej. un trozo de suela)
bm = bmesh.new(); bm.from_mesh(lo.data); bm.verts.ensure_lookup_table()
seen, kill = set(), []
for v in bm.verts:
    if v.index in seen: continue
    stack, comp = [v], []
    seen.add(v.index)
    while stack:
        x = stack.pop(); comp.append(x)
        for e in x.link_edges:
            o = e.other_vert(x)
            if o.index not in seen:
                seen.add(o.index); stack.append(o)
    if len(comp) < 300:
        kill += comp
bmesh.ops.delete(bm, geom=kill, context='VERTS')
bm.to_mesh(lo.data); bm.free()
print("FRAGMENTOS_QUITADOS", len(kill))
for im in list(bpy.data.images):
    if im != img:
        bpy.data.images.remove(im)
scene.render.engine = 'BLENDER_EEVEE'
body = lo
for v in body.data.vertices:
    v.co = v.co * S
for p in body.data.polygons:
    p.use_smooth = True
print("BAKE_OK", len(body.data.polygons))

# ---------------------------------------------------------------- utilidades de esqueleto y pesos
RADIUS = {"Hips": 0.16, "Spine": 0.22, "Neck": 0.07, "Head": 0.24,
          "Thigh": 0.10, "Shin": 0.06, "Foot": 0.09, "UpperArm": 0.06, "Forearm": 0.05, "Hand": 0.05}
TAU = 0.015
TAU_LEG = 0.035
ARM_X, ARM_ZMAX = 0.20, 0.95   # zona de brazos (|x| > 0.20 m por debajo de 0.95 m): sin huesos de piernas

def make_rig(name, bones):
    ad = bpy.data.armatures.new(name + "_Armature")
    rig = bpy.data.objects.new(name, ad)
    scene.collection.objects.link(rig)
    bpy.ops.object.select_all(action='DESELECT')
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode='EDIT')
    for n, (h, t, parent) in bones.items():
        eb = ad.edit_bones.new(n)
        eb.head, eb.tail = h, t
        eb.align_roll(Vector((0, -1, 0)) if abs(t.z - h.z) > 0.05 else Vector((0, 0, 1)))
    for n, (_, _, parent) in bones.items():
        if parent:
            ad.edit_bones[n].parent = ad.edit_bones[parent]
    bpy.ops.object.mode_set(mode='OBJECT')
    return rig

def seg_dist(p, a, b):
    ab = b - a
    t = max(0.0, min(1.0, (p - a).dot(ab) / ab.length_squared))
    return (p - (a + ab * t)).length

def weight(obj, bones, radius=None, zones=False):
    radius = radius or RADIUS
    obj.vertex_groups.clear()
    names = [n for n in bones if n != "Root"]
    groups = {n: obj.vertex_groups.new(name=n) for n in names}
    shoulder_z = bones["UpperArm.L"][0].z
    for v in obj.data.vertices:
        cand = []
        for n in names:
            if n.endswith(".L") and v.co.x < -0.02: continue
            if n.endswith(".R") and v.co.x > 0.02: continue
            if n.split(".")[0] in ("UpperArm", "Forearm", "Hand") and v.co.z > shoulder_z + 0.03                     and seg_dist(v.co, bones[n][0], bones[n][1]) > 0.08:
                continue   # el pelo y la cabeza no siguen a los brazos (salvo la propia piel del brazo)
            cand.append((seg_dist(v.co, bones[n][0], bones[n][1]) - radius[n.split(".")[0]], n))
        if zones:
            # el hueso más cercano decide la zona: brazo (sin huesos de piernas/cadera) o cuerpo (sin antebrazo/mano)
            nearest = min(cand)[1].split(".")[0]
            arm_zone = nearest in ("UpperArm", "Forearm", "Hand")
            banned = ("Hips", "Thigh", "Shin", "Foot") if arm_zone else ("Forearm", "Hand")
            cand = [(sc, n) for sc, n in cand if n.split(".")[0] not in banned]
        scores = cand
        best = min(sc for sc, _ in scores)
        # transición más suave en caderas y rodillas para que no se pellizquen al doblarse
        ws = sorted(((math.exp(-(sc - best) / (TAU_LEG if n.split(".")[0] in ("Hips", "Thigh", "Shin") else TAU)), n)
                     for sc, n in scores), reverse=True)[:4]
        tot = sum(w for w, _ in ws)
        # piernas: rodilla y tobillo con bandas de mezcla cortas, para que la espinilla y el muslo no se
        # doblen como un plátano. El peso de la cadera (Hips) se conserva; el resto se reparte por altura.
        if zones and ws[0][1].split(".")[0] in ("Thigh", "Shin", "Foot"):
            side = ws[0][1].split(".")[1]
            w_hips = sum(w for w, n in ws if n == "Hips") / tot
            knee = bones[f"Shin.{side}"][0]
            k = min(1.0, max(0.0, (knee.z + 0.035 - v.co.z) / 0.07))       # 0 = muslo, 1 = espinilla
            k = k * k * (3 - 2 * k)
            # bota rígida: solo la puntera gira con el pie (se dobla por la línea de los dedos)
            f = 0.0
            if v.co.z < bones[f"Shin.{side}"][1].z + 0.12:
                f = min(1.0, max(0.0, (ball_point(bones, side).y + 0.015 - v.co.y) / 0.05))
                f = f * f * (3 - 2 * f)
            rest = 1.0 - w_hips
            ws = [(w, n) for w, n in ((w_hips, "Hips"), (rest * (1 - k), f"Thigh.{side}"),
                                     (rest * k * (1 - f), f"Shin.{side}"), (rest * k * f, f"Foot.{side}")) if w > 0.01]
            tot = sum(w for w, _ in ws)
        for w, n in ws:
            if w / tot > 0.01:
                groups[n].add([v.index], w / tot, 'REPLACE')

def ball_point(bones, side):
    """Línea de los dedos: 55 % del camino del tobillo a la punta, a ras de suela."""
    ankle, toe = bones[f"Shin.{side}"][1], bones[f"Foot.{side}"][1]
    b = ankle + (toe - ankle) * 0.55
    return Vector((b.x, b.y, toe.z + 0.03))

def bind(obj, rig):
    obj.parent = rig
    obj.modifiers.clear()
    mod = obj.modifiers.new("Armature", 'ARMATURE')
    mod.object = rig

def skeleton(j):
    """j: diccionario de articulaciones (coordenadas del modelo original)."""
    b = {
        "Root":  (P(0, 0, 0), P(0, 0, 0.12), None),
        "Hips":  (j["hips"], j["spine0"], "Root"),
        "Spine": (j["spine0"], j["neck0"], "Hips"),
        "Neck":  (j["neck0"], j["head0"], "Spine"),
        "Head":  (j["head0"], j["head1"], "Neck"),
    }
    for s in "LR":
        b.update({
            f"Thigh.{s}":    (j[f"hip{s}"], j[f"knee{s}"], "Hips"),
            f"Shin.{s}":     (j[f"knee{s}"], j[f"ankle{s}"], f"Thigh.{s}"),
            f"Foot.{s}":     (j[f"ankle{s}"], j[f"toe{s}"], f"Shin.{s}"),
            f"UpperArm.{s}": (j[f"sh{s}"], j[f"elbow{s}"], "Spine"),
            f"Forearm.{s}":  (j[f"elbow{s}"], j[f"wrist{s}"], f"UpperArm.{s}"),
            f"Hand.{s}":     (j[f"wrist{s}"], j[f"hand{s}"], f"Forearm.{s}"),
        })
    return b

# articulaciones (coordenadas del modelo original), medidas en vistas frontal y lateral
BASE = {"hips": P(0, -0.03, 0.42), "spine0": P(0, -0.03, 0.49), "neck0": P(0, -0.03, 0.665),
        "head0": P(0, -0.03, 0.71), "head1": P(0, -0.05, 0.98)}
LEGS = {
    "hipL": P(0.05, -0.03, 0.42), "kneeL": P(0.05, -0.03, 0.26), "ankleL": P(0.05, -0.03, 0.10), "toeL": P(0.05, -0.12, 0.02),
    "hipR": P(-0.05, -0.03, 0.42), "kneeR": P(-0.05, -0.03, 0.26), "ankleR": P(-0.05, -0.03, 0.10), "toeR": P(-0.05, -0.12, 0.02),
}
# pose T (L = +x, R = -x); eje del brazo medido en la malla: z = 0.652, y = 0.005
POSED = dict(BASE, **LEGS, **{
    "shL": P(0.13, 0.005, 0.652), "elbowL": P(0.23, 0.005, 0.652), "wristL": P(0.33, 0.005, 0.648), "handL": P(0.36, 0.005, 0.645),
    "shR": P(-0.13, 0.005, 0.652), "elbowR": P(-0.23, 0.005, 0.652), "wristR": P(-0.33, 0.005, 0.648), "handR": P(-0.36, 0.005, 0.645),
})
# pose de pie (solo cuentan las direcciones: los brazos bajan casi verticales, algo separados del cuerpo)
STAND = dict(BASE, **LEGS, **{
    "shL": P(0.13, 0.005, 0.652), "elbowL": P(0.15, 0.0, 0.552), "wristL": P(0.165, -0.005, 0.452), "handL": P(0.17, -0.01, 0.422),
    "shR": P(-0.13, 0.005, 0.652), "elbowR": P(-0.15, 0.0, 0.552), "wristR": P(-0.165, -0.005, 0.452), "handR": P(-0.17, -0.01, 0.422),
})

# ---------------------------------------------------------------- 2) enderezar: esqueleto en pose de correr -> pose de pie
posed_bones = skeleton(POSED)
tmp = make_rig("Tmp", posed_bones)
# para enderezar, cada extremidad debe girar entera: radios de brazos y piernas más grandes
# pesos definitivos calculados en pose T (brazos separados del cuerpo): se conservan al bajar los brazos
weight(body, posed_bones, zones=True)
bind(body, tmp)
stand_bones = skeleton(STAND)
order = ["Thigh.L", "Shin.L", "Foot.L", "Thigh.R", "Shin.R", "Foot.R",
         "UpperArm.L", "Forearm.L", "Hand.L", "UpperArm.R", "Forearm.R", "Hand.R"]
bpy.context.view_layer.objects.active = tmp
bpy.ops.object.mode_set(mode='POSE')
for n in order:
    bpy.context.view_layer.update()
    pb = tmp.pose.bones[n]
    head = pb.head.copy()
    cur = (pb.tail - pb.head).normalized()
    tgt = (stand_bones[n][1] - stand_bones[n][0]).normalized()
    q = cur.rotation_difference(tgt)
    pb.matrix = Matrix.Translation(head) @ q.to_matrix().to_4x4() @ Matrix.Translation(-head) @ pb.matrix
bpy.ops.object.mode_set(mode='OBJECT')
bpy.context.view_layer.update()
posed_final = {n: (pb.head.copy(), pb.tail.copy(), posed_bones[n][2]) for n, pb in ((b.name, b) for b in tmp.pose.bones)}
# fijar la deformación en la malla y quitar el esqueleto provisional
bpy.ops.object.select_all(action='DESELECT')
body.select_set(True); bpy.context.view_layer.objects.active = body
bpy.ops.object.modifier_apply(modifier="Armature")
mw = body.matrix_world.copy(); body.parent = None; body.matrix_world = mw
bpy.data.objects.remove(tmp, do_unlink=True)
# apoyar los pies en el suelo
zmin = min(v.co.z for v in body.data.vertices)
for v in body.data.vertices:
    v.co.z -= zmin
print("POSE_OK altura", round(max(v.co.z for v in body.data.vertices), 3))

# ---------------------------------------------------------------- 3) esqueleto definitivo, pesos y animaciones
final_bones = {n: (h - Vector((0, 0, zmin)), t - Vector((0, 0, zmin)), p) for n, (h, t, p) in posed_final.items()}
final_bones["Root"] = (Vector((0, 0, 0)), Vector((0, 0, 0.2)), None)
# el hueso del pie arranca en la línea de los dedos (allí se dobla la bota)
for side in "LR":
    h, t, par = final_bones[f"Foot.{side}"]
    ball = ball_point(final_bones, side)
    final_bones[f"Foot.{side}"] = (ball, t + (t - ball).normalized() * 0.03, par)
rig = make_rig("Guardian_del_Bosque", final_bones)
arm_data = rig.data

# Tripo fusionó el brazo con la cadera donde se tocaban en la pose de correr: cortar esas uniones para
# que al mover el brazo no arrastre una lámina de malla: se quitan las caras que unen antebrazo/mano con
# tronco/cadera/piernas (quedan ocultas donde el brazo tocaba el cuerpo)
gname = {g.index: g.name.split(".")[0] for g in body.vertex_groups}
def zone(v):
    best = max(v.groups, key=lambda g: g.weight, default=None)
    if best is None: return None
    n = gname[best.group]
    return "arm" if n in ("Forearm", "Hand") else "body" if n in ("Hips", "Thigh", "Shin", "Foot") else None
zones_v = [zone(v) for v in body.data.vertices]
bm = bmesh.new(); bm.from_mesh(body.data); bm.faces.ensure_lookup_table()
bridges = [f for f in bm.faces if {"arm", "body"} <= {zones_v[v.index] for v in f.verts}]
bmesh.ops.delete(bm, geom=bridges, context='FACES_ONLY')
bm.to_mesh(body.data); bm.free()
print("CARAS_PUENTE_QUITADAS", len(bridges))
bind(body, rig)

src = open(ANIM_SRC, encoding="utf8").read()
a = src.index("# ---------------------------------------------------------------- ejes de rotación")
b = src.index("# ---------------------------------------------------------------- guardar .blend")
exec(src[a:b], globals())
# caminar y correr por fases de la zancada (archivo compartido con el Guardián)
CICLOS_SRC = os.path.join(os.path.dirname(ANIM_SRC), "ciclos_piernas.py")
exec(open(CICLOS_SRC, encoding="utf8").read(), globals())
# su malla tiene el brazo pegado al costado: en Saltar, Victory y Tropezar los brazos suben menos
def fcurves_of(act):
    if hasattr(act, "fcurves") and len(act.fcurves):
        return list(act.fcurves)
    out = []
    for layer in getattr(act, "layers", []):
        for strip in layer.strips:
            for bag in strip.channelbags:
                out += list(bag.fcurves)
    return out

for act_name, k_arm in (("Jump", 0.7), ("Victory", 0.6), ("Stumble", 0.7)):
    act = bpy.data.actions.get(act_name)
    for fc in fcurves_of(act) if act else []:
        if fc.data_path.endswith("rotation_euler") and ('"UpperArm.' in fc.data_path):
            for kp in fc.keyframe_points:
                kp.co.y *= k_arm; kp.handle_left.y *= k_arm; kp.handle_right.y *= k_arm
            fc.update()

rig.animation_data.action = bpy.data.actions["Idle"]
rig.show_in_front = True

bpy.ops.wm.save_as_mainfile(filepath=os.path.join(HERE, "Guardiana_del_Bosque.blend"))
bpy.ops.object.select_all(action='DESELECT')
rig.select_set(True); body.select_set(True); bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(
    filepath=os.path.join(UNITY_DIR, "Guardiana_del_Bosque.fbx"), use_selection=True,
    object_types={'ARMATURE', 'MESH'}, apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
    add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
    bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0, mesh_smooth_type='FACE',
    path_mode='STRIP')
print("GUARDIANA_OK", len(body.data.polygons), "altura", round(max(v.co.z for v in body.data.vertices), 3))
