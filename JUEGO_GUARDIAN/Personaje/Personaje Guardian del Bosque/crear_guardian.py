# Guardián del Bosque - personaje 3D low-poly (Guardianes del Bosque, sección 7)
# Uso: blender --background --python crear_guardian.py
# Genera: Guardian_del_Bosque.blend, Guardian_del_Bosque.fbx y renders de vista previa.
import bpy, bmesh, math, os
from mathutils import Vector

OUT = os.path.dirname(os.path.abspath(__file__))

# ---------------------------------------------------------------- escena limpia
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'

# ---------------------------------------------------------------- materiales
def srgb(h):
    h = h.lstrip('#')
    c = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    return tuple(((x + 0.055) / 1.055) ** 2.4 if x > 0.04045 else x / 12.92 for x in c) + (1.0,)

def mat(name, hexcol, rough=0.8):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = srgb(hexcol)
    bsdf.inputs["Roughness"].default_value = rough
    m.diffuse_color = srgb(hexcol)
    return m

M = {
    "piel":      mat("M_Piel", "#E8B48E"),
    "pelo":      mat("M_Pelo", "#3A2A1E"),
    "gorra":     mat("M_Gorra", "#7F8C5E"),
    "gorra_costura": mat("M_Gorra_Costura", "#6E7A50"),
    "metal":     mat("M_Metal", "#B59F74", 0.45),
    "emblema":   mat("M_Emblema", "#3E7B45"),
    "camiseta":  mat("M_Camiseta", "#9CC46C"),
    "short":     mat("M_Short", "#3F6640"),
    "cuero":     mat("M_Cuero", "#6B4A2F"),
    "mochila":   mat("M_Mochila", "#5A4030"),
    "bota":      mat("M_Bota", "#7A5236"),
    "suela":     mat("M_Suela", "#3B2A1E"),
    "ojos":      mat("M_Ojos", "#3A2316", 0.35),
    "boca":      mat("M_Boca", "#8E4A3F"),
    "esclera":   mat("M_Esclera", "#F3EEE8", 0.3),
    "iris":      mat("M_Iris", "#6A3E22", 0.25),
    "iris_claro": mat("M_Iris_Claro", "#A0663A", 0.25),
    "pupila":    mat("M_Pupila", "#120A06", 0.2),
    "pestana":   mat("M_Pestana", "#24160F"),
    "labio":     mat("M_Labio", "#CF8D7C", 0.55),
    "fosa":      mat("M_Fosa", "#7A4236"),
    "brillo":    mat("M_Brillo", "#FFFFFF", 0.2),
    "mejilla":   mat("M_Mejilla", "#ECB49E"),
}

# ---------------------------------------------------------------- utilidades de modelado
parts = []

def finish(o, material, bone, smooth=False):
    o.data.materials.append(M[material])
    o["bone"] = bone
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    for p in o.data.polygons:
        p.use_smooth = smooth
    parts.append(o)
    return o

def sphere(loc, scale, material, bone, seg=10, rings=7, rot=(0, 0, 0), cut=None, smooth=False, plane=None, fill=False):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=seg, ring_count=rings, radius=1, location=loc, rotation=rot)
    o = bpy.context.object
    if cut:  # cut = (eje, umbral, conservar_mayor) en coordenadas locales antes de escalar
        axis, thr, keep_greater = cut
        bm = bmesh.new(); bm.from_mesh(o.data)
        kill = [v for v in bm.verts if (v.co[axis] < thr) == keep_greater]
        bmesh.ops.delete(bm, geom=kill, context='VERTS')
        if fill:  # cerrar el hueco del corte
            bmesh.ops.holes_fill(bm, edges=bm.edges[:], sides=0)
        bm.to_mesh(o.data); bm.free()
    if plane:  # plane = (normal, d): conserva lo que cumple normal·co > d (corte limpio)
        no, d = Vector(plane[0]).normalized(), plane[1]
        bm = bmesh.new(); bm.from_mesh(o.data)
        bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:],
                               plane_co=no * d, plane_no=no, clear_inner=True)
        bm.to_mesh(o.data); bm.free()
    o.scale = scale
    return finish(o, material, bone, smooth)

def make_obj(bm, name="part"):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new(name, me)
    scene.collection.objects.link(o)
    bpy.ops.object.select_all(action='DESELECT')
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    return o

def bevel_mesh(o, width, segs=3):
    bm = bmesh.new(); bm.from_mesh(o.data)
    bmesh.ops.bevel(bm, geom=bm.edges[:], offset=width, offset_type='OFFSET', segments=segs,
                    profile=0.5, affect='EDGES', clamp_overlap=True)
    bm.to_mesh(o.data); bm.free()

def box(loc, scale, material, bone, rot=(0, 0, 0), bevel=None, segs=2):
    bpy.ops.mesh.primitive_cube_add(size=2, location=loc, rotation=rot)
    o = bpy.context.object
    o.scale = scale
    o = finish(o, material, bone, True)
    if bevel is None:
        bevel = min(min(scale) * 0.45, 0.012)
    if bevel > 0:
        bevel_mesh(o, bevel, segs)
    return o

def cyl(loc, scale, material, bone, verts=8, rot=(0, 0, 0), top=1.0, bevel=0.0, segs=2):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=1, depth=2, location=loc, rotation=rot)
    o = bpy.context.object
    if top != 1.0:
        for v in o.data.vertices:
            if v.co.z > 0:
                v.co.x *= top; v.co.y *= top
    o.scale = scale
    o = finish(o, material, bone, True)
    if bevel > 0:
        bevel_mesh(o, bevel, segs)
    return o

def _frame(t, ref):
    t = t.normalized()
    rv = Vector(ref)
    a = rv - rv.dot(t) * t
    if a.length < 1e-5:
        rv = Vector((0, 1, 0)) if abs(t.y) < 0.9 else Vector((1, 0, 0))
        a = rv - rv.dot(t) * t
    a.normalize()
    return a, t.cross(a)

def _skin(bm, rings, closed, cap):
    n = len(rings[0])
    pairs = list(zip(rings, rings[1:])) + ([(rings[-1], rings[0])] if closed else [])
    for r0, r1 in pairs:
        for i in range(n):
            j = (i + 1) % n
            bm.faces.new((r0[i], r0[j], r1[j], r1[i]))
    if cap and not closed:
        for ring in (rings[0], rings[-1]):
            c = bm.verts.new(sum((v.co for v in ring), Vector()) / n)
            for i in range(n):
                bm.faces.new((ring[i], ring[(i + 1) % n], c))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])

def lathe(p1, p2, profile, material, bone, verts=16, sx=1.0, sy=1.0, ref=(1, 0, 0), wobble=None, cap=True):
    """Superficie de revolución entre p1 y p2. profile = [(u, radio)], u en 0..1 a lo largo del eje."""
    p1, p2 = Vector(p1), Vector(p2)
    d = p2 - p1
    a, b = _frame(d, ref)
    bm = bmesh.new(); rings = []
    for u, r in profile:
        c = p1 + d * u
        ring = []
        for i in range(verts):
            ang = 2 * math.pi * i / verts
            rr = r
            if wobble and wobble[2] <= u <= wobble[3]:
                rr += wobble[0] * math.sin(wobble[1] * ang)
            ring.append(bm.verts.new(c + a * (rr * sx * math.cos(ang)) + b * (rr * sy * math.sin(ang))))
        rings.append(ring)
    _skin(bm, rings, False, cap)
    return finish(make_obj(bm), material, bone, True)

def tube(points, w, h, material, bone, verts=8, ref=(1, 0, 0), closed=False, cap=True):
    """Tubo de sección elíptica (w = ancho en la dirección ref, h = grosor) a lo largo de una polilínea."""
    P = [Vector(p) for p in points]; n = len(P)
    bm = bmesh.new(); rings = []
    for k, c in enumerate(P):
        if closed:
            t = P[(k + 1) % n] - P[k - 1]
        else:
            t = P[min(k + 1, n - 1)] - P[max(k - 1, 0)]
        a, b = _frame(t, ref(c) if callable(ref) else ref)
        rings.append([bm.verts.new(c + a * (w * math.cos(2 * math.pi * i / verts))
                                   + b * (h * math.sin(2 * math.pi * i / verts))) for i in range(verts)])
    _skin(bm, rings, closed, cap)
    return finish(make_obj(bm), material, bone, True)

def ring_pts(center, axis, r, n=24, sx=1.0, sy=1.0, ref=(1, 0, 0)):
    a, b = _frame(Vector(axis), ref)
    c = Vector(center)
    return [c + a * (r * sx * math.cos(2 * math.pi * i / n)) + b * (r * sy * math.sin(2 * math.pi * i / n)) for i in range(n)]

def hem(center, axis, r, minor, material, bone, sx=1.0, sy=1.0, n=20):
    """Dobladillo / costura: anillo fino alrededor de un eje."""
    return tube(ring_pts(center, axis, r, n, sx, sy), minor, minor, material, bone, verts=6, ref=Vector(axis), closed=True)

def bulge(o, axis, amount, half):
    """Abomba la cara del lado positivo de 'axis' (0=x,1=y,2=z) para que no quede plana."""
    others = [i for i in range(3) if i != axis]
    for v in o.data.vertices:
        if v.co[axis] > 0:
            f = 1.0
            for i in others:
                f *= max(0.0, 1 - (v.co[i] / half[i]) ** 2)
            v.co[axis] += amount * f

# ---------------------------------------------------------------- modelo
# Frente del personaje = -Y (en Unity queda mirando a +Z). Altura total ~1.6 m.
TORSO = [(0.712, 0.175), (0.73, 0.176), (0.75, 0.168), (0.85, 0.172), (0.95, 0.187),
         (1.01, 0.19), (1.045, 0.172), (1.065, 0.125), (1.074, 0.065)]
TORSO_SY = 0.72

def torso_r(z):
    for (z0, r0), (z1, r1) in zip(TORSO, TORSO[1:]):
        if z0 <= z <= z1:
            return r0 + (r1 - r0) * (z - z0) / (z1 - z0)
    return TORSO[0][1] if z < TORSO[0][0] else TORSO[-1][1]

def torso_front_y(x, z):
    r = torso_r(z)
    return -r * TORSO_SY * math.sqrt(max(0.0, 1 - (x / r) ** 2))

for s in (1, -1):
    side = "L" if s > 0 else "R"
    x = 0.1 * s
    # ---- botas: caña redondeada, puntera biselada, suela, cuello acolchado y cordones
    lathe((x, -0.005, 0.02), (x, -0.005, 0.165), [(0, 0.066), (0.4, 0.07), (0.85, 0.072), (1, 0.07)],
          "bota", f"Foot.{side}", verts=16, sy=1.12)
    box((x, -0.072, 0.047), (0.064, 0.062, 0.04), "bota", f"Foot.{side}", bevel=0.024, segs=3)
    box((x, -0.035, 0.013), (0.074, 0.113, 0.013), "suela", f"Foot.{side}", bevel=0.007)
    hem((x, -0.005, 0.16), (0, 0, 1), 0.073, 0.011, "bota", f"Shin.{side}", sy=1.12)
    for zz in (0.095, 0.118, 0.141):
        box((x, -0.086, zz), (0.026, 0.004, 0.0045), "suela", f"Foot.{side}", rot=(math.radians(8), 0, 0), bevel=0.002)
    # ---- piernas (pantorrilla suave)
    lathe((x, 0, 0.14), (x, 0, 0.44), [(0, 0.04), (0.25, 0.045), (0.55, 0.05), (0.85, 0.048), (1, 0.046)],
          "piel", f"Shin.{side}", verts=14)
    # ---- short: pernera con pliegues discretos, dobladillo y bolsillo con solapa
    lathe((x * 0.95, 0, 0.675), (x, 0, 0.405), [(0, 0.09), (0.5, 0.092), (0.92, 0.089), (1, 0.087)],
          "short", f"Thigh.{side}", verts=18, wobble=(0.0025, 5, 0.6, 1.0))
    hem((x, 0, 0.41), (0, 0, 1), 0.087, 0.006, "short", f"Thigh.{side}")
    box((x + 0.093 * s, 0, 0.5), (0.011, 0.044, 0.048), "short", f"Thigh.{side}", bevel=0.006)
    box((x + 0.099 * s, 0, 0.543), (0.008, 0.047, 0.013), "short", f"Thigh.{side}", bevel=0.004)
    sphere((x + 0.106 * s, 0, 0.535), (0.005, 0.006, 0.006), "metal", f"Thigh.{side}", seg=8, rings=6)
    # ---- brazos: hombro, manga con dobladillo, transiciones suaves en codo y muñeca
    sphere((0.2 * s, 0, 1.0), (0.074, 0.072, 0.074), "camiseta", "Spine", seg=16, rings=10)
    sleeve_a, sleeve_b = Vector((0.205 * s, 0, 1.03)), Vector((0.255 * s, 0, 0.875))
    lathe(sleeve_a, sleeve_b, [(-0.05, 0.04), (0, 0.062), (0.3, 0.072), (0.75, 0.07), (1, 0.067)],
          "camiseta", f"UpperArm.{side}", verts=16, wobble=(0.002, 3, 0.5, 1.0))
    hem(sleeve_b + (sleeve_a - sleeve_b).normalized() * 0.004, sleeve_a - sleeve_b, 0.067, 0.006,
        "camiseta", f"UpperArm.{side}")
    lathe((0.24 * s, 0, 0.93), (0.265 * s, 0, 0.8), [(0, 0.043), (0.6, 0.042), (1, 0.041)],
          "piel", f"UpperArm.{side}", verts=14)
    sphere((0.265 * s, 0, 0.8), (0.041, 0.041, 0.041), "piel", f"Forearm.{side}", seg=14, rings=10)
    lathe((0.265 * s, 0, 0.8), (0.284 * s, 0, 0.625), [(0, 0.041), (0.3, 0.043), (0.75, 0.037), (1, 0.033)],
          "piel", f"Forearm.{side}", verts=14)
    sphere((0.284 * s, -0.002, 0.625), (0.033, 0.032, 0.03), "piel", f"Hand.{side}", seg=12, rings=8)
    # puño: palma redondeada, nudillos y pulgar
    sphere((0.287 * s, -0.008, 0.585), (0.049, 0.046, 0.055), "piel", f"Hand.{side}", seg=16, rings=12)
    sphere((0.285 * s, -0.03, 0.558), (0.042, 0.02, 0.022), "piel", f"Hand.{side}", seg=14, rings=8)
    sphere((0.254 * s, -0.032, 0.594), (0.015, 0.016, 0.026), "piel", f"Hand.{side}", seg=10, rings=8,
           rot=(0, math.radians(-20 * s), 0))
    # ---- tirante de la mochila: sigue la curva del pecho y pasa sobre el hombro
    sx_ = 0.107 * s
    pts = [(sx_, 0.135, 0.98), (sx_, 0.11, 1.035), (sx_, 0.06, 1.075), (sx_, 0.0, 1.088), (sx_, -0.06, 1.072)]
    pts += [(sx_ * (1 - 0.03 * k), torso_front_y(sx_, z) - 0.008, z) for k, z in
            enumerate((1.035, 1.0, 0.96, 0.92, 0.88, 0.84, 0.80, 0.765))]
    tube(pts, 0.024, 0.0055, "cuero", "Spine", verts=10, ref=(1, 0, 0))
    by = torso_front_y(sx_, 0.92) - 0.016
    box((sx_ * 0.97, by, 0.92), (0.03, 0.005, 0.013), "metal", "Spine", bevel=0.003)
    # ---- bolsillos del cinturón con solapa y broche
    for (px, py, sz) in ((0.1, -0.136, (0.038, 0.024, 0.042)), (0.172, -0.045, (0.024, 0.034, 0.038))):
        box((px * s, py, 0.722), sz, "cuero", "Hips", bevel=0.011, segs=3)
        fy = py - 0.002 if px < 0.15 else py
        fx = px * s + (0.002 * s if px > 0.15 else 0)
        box((fx, fy, 0.757), (sz[0] + 0.003, sz[1] + 0.003, 0.009), "cuero", "Hips", bevel=0.005)
        if px < 0.15:
            sphere((px * s, py - 0.025, 0.748), (0.006, 0.004, 0.006), "metal", "Hips", seg=8, rings=6)

# ---- torso (camiseta) con ligero abolsado sobre el cinturón y cuello redondo
z0, z1 = TORSO[0][0], TORSO[-1][0]
lathe((0, 0, z0), (0, 0, z1), [((z - z0) / (z1 - z0), r) for z, r in TORSO], "camiseta", "Spine",
      verts=20, sy=TORSO_SY, wobble=(0.0035, 6, 0.0, 0.18))
hem((0, 0, 1.064), (0, 0, 1), 0.072, 0.0075, "camiseta", "Spine", sy=0.88)
# ---- short (cadera) y cinturón con hebilla
lathe((0, 0, 0.575), (0, 0, 0.722), [(0, 0.172), (0.5, 0.175), (0.8, 0.172), (1, 0.158)], "short", "Hips", verts=20, sy=0.73)
lathe((0, 0, 0.703), (0, 0, 0.745), [(0, 0.178), (0.2, 0.184), (0.8, 0.184), (1, 0.178)], "cuero", "Hips",
      verts=24, sy=0.735)
box((0, -0.138, 0.724), (0.027, 0.006, 0.021), "suela", "Hips", bevel=0.004)
box((0, -0.144, 0.724), (0.018, 0.003, 0.013), "metal", "Hips", bevel=0.002)
# ---- mochila: cuerpo abombado con bordes redondeados, solapa, correas con hebillas, bolsillo y asa
pack = box((0, 0.178, 0.87), (0.13, 0.066, 0.15), "mochila", "Spine", bevel=0.038, segs=3)
bulge(pack, 1, 0.02, (0.13, 0.066, 0.15))
flap = box((0, 0.206, 0.968), (0.136, 0.05, 0.058), "cuero", "Spine", bevel=0.028, segs=3)
bulge(flap, 1, 0.012, (0.136, 0.05, 0.058))
for sx2 in (-0.06, 0.06):
    box((sx2, 0.262, 0.885), (0.012, 0.0045, 0.075), "cuero", "Spine", bevel=0.003)
    box((sx2, 0.266, 0.87), (0.017, 0.0035, 0.012), "metal", "Spine", bevel=0.002)
pocket = box((0, 0.252, 0.785), (0.09, 0.02, 0.05), "cuero", "Spine", bevel=0.016, segs=3)
bulge(pocket, 1, 0.006, (0.09, 0.02, 0.05))
tube([(-0.04, 0.19, 1.018), (-0.03, 0.19, 1.045), (0, 0.19, 1.056), (0.03, 0.19, 1.045), (0.04, 0.19, 1.018)],
     0.007, 0.007, "cuero", "Spine", verts=8, ref=(0, 1, 0))
# ---- cuello
lathe((0, 0, 0.99), (0, 0, 1.14), [(0, 0.058), (0.5, 0.054), (1, 0.056)], "piel", "Neck", verts=14)
# ---- cabeza: cara redonda con mejillas llenas y barbilla pequeña (estilo animación 3D)
HEAD_C, HEAD_R = Vector((0, 0, 1.27)), Vector((0.168, 0.157, 0.19))
JAW_TAPER = 0.06
CHEEK = (0.09, 1.212, 0.034, 0.009)       # x, z, radio de influencia, cuánto sobresale

def taper(zl):  # zl = altura relativa (-1..1) dentro de la cabeza
    return 1.0 - JAW_TAPER * max(0.0, -zl)

def cheek_bump(x, z):
    cx, cz, rad, amp = CHEEK
    return amp * math.exp(-(((abs(x) - cx) ** 2 + (z - cz) ** 2) / (2 * rad ** 2)))

def head_y(x, z):
    """Y de la superficie frontal de la cabeza en (x, z), incluidas las mejillas."""
    zl = (z - HEAD_C.z) / HEAD_R.z
    t = taper(zl)
    k = 1 - (x / (HEAD_R.x * t)) ** 2 - zl ** 2
    return -HEAD_R.y * t * math.sqrt(max(0.0, k)) - cheek_bump(x, z)

head = sphere(tuple(HEAD_C), tuple(HEAD_R), "piel", "Head", seg=32, rings=24, smooth=True)
for v in head.data.vertices:
    t = taper(v.co.z / HEAD_R.z)
    v.co.x *= t; v.co.y *= t
    if v.co.y < 0:
        v.co.y -= cheek_bump(v.co.x, HEAD_C.z + v.co.z) * min(1.0, -v.co.y / 0.06)

def conform(o, surface_y, lift):
    """Pega una pieza fina a una superficie: y = superficie(x, z) - lift + grosor local."""
    lx, ly, lz = o.location
    for v in o.data.vertices:
        wx, wz = lx + v.co.x, lz + v.co.z
        v.co.y = surface_y(wx, wz) - lift + v.co.y - ly + (ly - surface_y(lx, lz))
    return o

def on_head(x, z, size, material, seg=16, rings=10, lift=0.0, rot=(0, 0, 0)):
    o = sphere((x, head_y(x, z), z), size, material, "Head", seg=seg, rings=rings, rot=rot, smooth=True)
    return conform(o, head_y, lift)

EYE_X, EYE_Z = 0.062, 1.27
EYE_R = Vector((0.0305, 0.0215, 0.034))

for s in (1, -1):
    # orejas
    sphere((0.166 * s, 0.015, 1.258), (0.022, 0.03, 0.04), "piel", "Head", seg=12, rings=10, smooth=True)
    sphere((0.172 * s, 0.012, 1.258), (0.01, 0.018, 0.026), "mejilla", "Head", seg=10, rings=8, smooth=True)

    # ---- ojos grandes: esclerótica, iris con reflejo inferior, pupila, brillos
    ec = Vector((EYE_X * s, head_y(EYE_X * s, EYE_Z) + 0.0085, EYE_Z))
    sphere(tuple(ec), tuple(EYE_R), "esclera", "Head", seg=20, rings=14, smooth=True)

    def eye_y(x, z, ec=ec):
        k = 1 - ((x - ec.x) / EYE_R.x) ** 2 - ((z - ec.z) / EYE_R.z) ** 2
        return ec.y - EYE_R.y * math.sqrt(max(0.0, k))

    ix, iz = ec.x - 0.002 * s, ec.z - 0.002
    for size, mat_name, dx, dz, lift in (
            ((0.0185, 0.0015, 0.0195), "iris", 0, 0, 0.0005),
            ((0.012, 0.0012, 0.0065), "iris_claro", 0, -0.0095, 0.0009),
            ((0.0085, 0.0012, 0.009), "pupila", 0, 0.0005, 0.0013),
            ((0.0048, 0.0012, 0.0052), "brillo", 0.0055, 0.0065, 0.0018),
            ((0.0022, 0.0012, 0.0022), "brillo", -0.0055, -0.0065, 0.0018)):
        px, pz = ix + dx, iz + dz
        conform(sphere((px, eye_y(px, pz), pz), size, mat_name, "Head", seg=16, rings=8, smooth=True), eye_y, lift)

    # párpado superior (piel) con borde arqueado que sigue la forma del ojo, y línea de pestañas
    LID, ARCH, LS = 0.6, 0.42, 1.06
    def lid_z(xu):
        return LID - ARCH * xu ** 2
    bpy.ops.mesh.primitive_uv_sphere_add(segments=28, ring_count=22, radius=1, location=tuple(ec))
    lid = bpy.context.object
    bm = bmesh.new(); bm.from_mesh(lid.data)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < lid_z(v.co.x)], context='VERTS')
    bm.to_mesh(lid.data); bm.free()
    lid.scale = tuple(EYE_R * LS)
    finish(lid, "piel", "Head", True)
    lash = []
    for i in range(15):
        xu = -0.96 + 0.96 * 2 * i / 14
        zu = lid_z(xu)
        yu = -math.sqrt(max(0.0, 1 - xu ** 2 - zu ** 2))
        lash.append((ec.x + EYE_R.x * LS * xu, ec.y + EYE_R.y * LS * yu - 0.0008, ec.z + EYE_R.z * LS * zu - 0.0006))
    lash_o = tube(lash, 0.0026, 0.0022, "pestana", "Head", verts=6, ref=(0, 0, 1))
    for v in lash_o.data.vertices:      # más gruesa hacia el extremo exterior
        t = max(0.0, (v.co.x - ec.x) * s) / EYE_R.x
        v.co.z += 0.0012 * t

    # ---- cejas gruesas y arqueadas
    bx, bz = 0.066 * s, 1.326
    brow = sphere((bx, head_y(bx, bz), bz), (0.037, 0.0075, 0.0115), "pelo", "Head", seg=16, rings=8,
                  rot=(0, math.radians(-5 * s), 0), smooth=True)
    for v in brow.data.vertices:
        v.co.z -= 4.5 * v.co.x ** 2
        tip = max(0.0, v.co.x * s) / 0.037          # el extremo exterior se afina
        v.co.z *= 1 - 0.35 * tip
    conform(brow, head_y, 0.004)

    # patillas
    # rubor suave en las mejillas
    on_head(0.1 * s, 1.214, (0.02, 0.004, 0.011), "mejilla", lift=0.0006)

# ---- nariz redondeada con puente, aletas y fosas nasales
NOSE_Z = 1.232
tip_y = head_y(0, NOSE_Z) - 0.009
sphere((0, tip_y, NOSE_Z), (0.018, 0.017, 0.017), "piel", "Head", seg=20, rings=14, smooth=True)
for s in (1, -1):
    sphere((0.0155 * s, head_y(0.0155 * s, 1.225) - 0.001, 1.225), (0.0095, 0.009, 0.009), "piel", "Head",
           seg=14, rings=10, smooth=True)
    sphere((0.0075 * s, tip_y + 0.006, NOSE_Z - 0.0155), (0.0045, 0.0035, 0.0025), "fosa", "Head",
           seg=10, rings=6, smooth=True)

# ---- sonrisa: línea de labios curvada hacia arriba y labio inferior
smile = []
for i in range(11):
    x = -0.032 + 0.0064 * i
    z = 1.184 + 6.0 * x ** 2
    smile.append((x, head_y(x, z) - 0.0004, z))
tube(smile, 0.0019, 0.0014, "boca", "Head", verts=6, ref=(0, 0, 1))
for s in (1, -1):                                       # comisuras
    x = 0.0335 * s
    z = 1.184 + 6.0 * x ** 2 + 0.0015
    sphere((x, head_y(x, z) - 0.0008, z), (0.0035, 0.002, 0.0035), "boca", "Head", seg=8, rings=6, smooth=True)
on_head(0, 1.176, (0.021, 0.0035, 0.0058), "labio", lift=0.0008)
on_head(0, 1.158, (0.012, 0.003, 0.006), "mejilla", lift=0.0)        # sombra suave bajo el labio

# pelo (atrás y lados, por debajo de la gorra)
sphere((0, 0.02, 1.29), (0.172, 0.163, 0.18), "pelo", "Head", seg=24, rings=16,
       plane=((0, 1, 0.6), 0.02), smooth=True)

# ---- flequillo en mechones bajo la gorra
for i in range(7):
    x = -0.114 + 0.038 * i
    z = 1.377 + 0.004 * math.cos(i * 1.7)
    ang = math.radians(-28 + 8 * (i % 2))
    strand = sphere((x, head_y(x, z), z), (0.024, 0.0045, 0.034), "pelo", "Head", seg=12, rings=10, smooth=True)
    for v in strand.data.vertices:
        zl = -v.co.z / 0.034                      # punta hacia abajo
        v.co.x *= 1 - 0.85 * max(0.0, zl) ** 1.5
        x0, z0 = v.co.x, v.co.z
        v.co.x = x0 * math.cos(ang) + z0 * math.sin(ang)
        v.co.z = -x0 * math.sin(ang) + z0 * math.cos(ang)
    conform(strand, head_y, 0.004)

# ---- gorra de béisbol: copa ajustada con costuras entre paneles, visera larga y curva, emblema de hojas
CAP_Z = 1.41
CAP_R = Vector((0.18, 0.178, 0.152))
CAP_RINGS = 14
CAP_RIM_EL = -math.pi / CAP_RINGS     # el borde coincide con el primer anillo de la malla bajo el ecuador
CAP_CUT = math.sin(CAP_RIM_EL) - 0.01
CAP_BOTTOM = CAP_Z + CAP_R.z * math.sin(CAP_RIM_EL)
CAP_BOTTOM_K = math.cos(CAP_RIM_EL)

def cap_y(x, z):
    k = 1 - ((z - CAP_Z) / CAP_R.z) ** 2 - (x / CAP_R.x) ** 2
    return -CAP_R.y * math.sqrt(max(0.0, k))

sphere((0, 0, CAP_Z), tuple(CAP_R), "gorra", "Head", seg=24, rings=CAP_RINGS, cut=(2, CAP_CUT, True), smooth=True, fill=True)
hem((0, 0, CAP_BOTTOM + 0.002), (0, 0, 1), 1.0, 0.0042, "gorra_costura", "Head",
    sx=CAP_R.x * CAP_BOTTOM_K, sy=CAP_R.y * CAP_BOTTOM_K, n=32)
for k in range(6):                                                        # costuras de los paneles
    th = math.radians(60 * k)
    pts = []
    for e in range(0, 13):
        el = CAP_RIM_EL * (1 - e / 12) + math.radians(90) * (e / 12)
        pts.append((CAP_R.x * math.cos(el) * math.cos(th), CAP_R.y * math.cos(el) * math.sin(th),
                    CAP_Z + CAP_R.z * math.sin(el)))
    tube(pts, 0.0018, 0.0018, "gorra_costura", "Head", verts=5, ref=lambda c: Vector(c) - Vector((0, 0, CAP_Z)))
brim = cyl((0, -0.2, CAP_BOTTOM + 0.009), (0.155, 0.14, 0.0065), "gorra", "Head", verts=32,
           rot=(math.radians(9), 0, 0))
for v in brim.data.vertices:          # visera curvada hacia abajo en los lados
    v.co.z -= 1.4 * v.co.x ** 2
bevel_mesh(brim, 0.004, 2)
sphere((0, 0, CAP_Z + CAP_R.z), (0.018, 0.018, 0.01), "gorra", "Head", seg=10, rings=6, smooth=True)   # botón

def conform_to_cap(o, lift):
    lx, ly, lz = o.location
    for v in o.data.vertices:
        wx, wz = lx + v.co.x, lz + v.co.z
        v.co.y = cap_y(wx, wz) - lift + (v.co.y) - ly + (ly - cap_y(lx, lz))

EMB_C = Vector((0.0, 0.0, CAP_Z + 0.068))
for ang in (0, 120, 240):                                                 # emblema: tres hojas en triángulo
    a = math.radians(ang)
    d = Vector((math.sin(a), 0, math.cos(a)))
    c = EMB_C + d * 0.043
    leaf = sphere((c.x, cap_y(c.x, c.z), c.z), (0.019, 0.003, 0.036), "emblema", "Head", seg=14, rings=10, smooth=True)
    for v in leaf.data.vertices:
        zl = v.co.z / 0.036
        v.co.x *= 1 - 0.8 * max(0.0, zl) ** 1.6 - 0.25 * max(0.0, -zl) ** 2   # punta afilada, base redondeada
        x0, z0 = v.co.x, v.co.z
        v.co.x = x0 * math.cos(a) + z0 * math.sin(a)
        v.co.z = -x0 * math.sin(a) + z0 * math.cos(a)
    conform_to_cap(leaf, 0.002)
    vein = tube([EMB_C + d * (0.043 + t) for t in (-0.028, -0.01, 0.01, 0.026)], 0.0016, 0.0016,
                "gorra", "Head", verts=5)
    for v in vein.data.vertices:
        v.co.y = cap_y(v.co.x, v.co.z) - 0.0045

# ---------------------------------------------------------------- unir en una sola malla con grupos
for o in parts:
    vg = o.vertex_groups.new(name=o["bone"])
    vg.add(list(range(len(o.data.vertices))), 1.0, 'REPLACE')
bpy.ops.object.select_all(action='DESELECT')
for o in parts:
    o.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
body = bpy.context.object
body.name = body.data.name = "Guardian_Mesh"
bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.mesh.remove_doubles(threshold=0.0001)
bpy.ops.mesh.normals_make_consistent(inside=False)
bpy.ops.object.mode_set(mode='OBJECT')
del body["bone"]

# cabeza más grande (proporción tipo chibi de la referencia)
HEAD_SCALE, PIVOT = 1.2, Vector((0, 0, 1.08))
gi = body.vertex_groups["Head"].index
for v in body.data.vertices:
    if any(g.group == gi for g in v.groups):
        v.co = PIVOT + (v.co - PIVOT) * HEAD_SCALE

# sombreado: suave en general, aristas marcadas solo donde hay un ángulo fuerte, normales ponderadas
for p in body.data.polygons:
    p.use_smooth = True
body.data.set_sharp_from_angle(angle=math.radians(40))
wn = body.modifiers.new("WeightedNormal", 'WEIGHTED_NORMAL')
wn.keep_sharp = True
wn.weight = 50
bpy.context.view_layer.objects.active = body
bpy.ops.object.modifier_apply(modifier=wn.name)

# ---------------------------------------------------------------- esqueleto
bones = {
    "Root":  ((0, 0, 0), (0, 0, 0.2), None),
    "Hips":  ((0, 0, 0.64), (0, 0, 0.8), "Root"),
    "Spine": ((0, 0, 0.8), (0, 0, 1.04), "Hips"),
    "Neck":  ((0, 0, 1.04), (0, 0, 1.13), "Spine"),
    "Head":  ((0, 0, 1.13), (0, 0, 1.58), "Neck"),
}
for s, side in ((1, "L"), (-1, "R")):
    bones.update({
        f"Thigh.{side}":    ((0.1 * s, 0, 0.64), (0.1 * s, 0, 0.43), "Hips"),
        f"Shin.{side}":     ((0.1 * s, 0, 0.43), (0.1 * s, 0, 0.12), f"Thigh.{side}"),
        f"Foot.{side}":     ((0.1 * s, 0, 0.12), (0.1 * s, -0.11, 0.03), f"Shin.{side}"),
        f"UpperArm.{side}": ((0.22 * s, 0, 1.0), (0.265 * s, 0, 0.8), "Spine"),
        f"Forearm.{side}":  ((0.265 * s, 0, 0.8), (0.285 * s, 0, 0.62), f"UpperArm.{side}"),
        f"Hand.{side}":     ((0.285 * s, 0, 0.62), (0.29 * s, -0.01, 0.54), f"Forearm.{side}"),
    })
arm_data = bpy.data.armatures.new("Guardian_Armature")
rig = bpy.data.objects.new("Guardian_del_Bosque", arm_data)
scene.collection.objects.link(rig)
bpy.context.view_layer.objects.active = rig
bpy.ops.object.mode_set(mode='EDIT')
for name, (h, t, parent) in bones.items():
    eb = arm_data.edit_bones.new(name)
    eb.head, eb.tail = h, t
    eb.align_roll(Vector((0, -1, 0)) if abs(Vector(t).z - Vector(h).z) > 0.05 else Vector((0, 0, 1)))
for name, (_, _, parent) in bones.items():
    if parent:
        eb = arm_data.edit_bones[name]
        eb.parent = arm_data.edit_bones[parent]
        eb.use_connect = False
bpy.ops.object.mode_set(mode='OBJECT')
body.parent = rig
mod = body.modifiers.new("Armature", 'ARMATURE')
mod.object = rig

# ---------------------------------------------------------------- ejes de rotación detectados automáticamente
for pb in rig.pose.bones:
    pb.rotation_mode = 'XYZ'

def tail_after(pb, axis, ang):
    pb.rotation_euler = (0, 0, 0)
    e = [0, 0, 0]; e[axis] = ang
    pb.rotation_euler = e
    bpy.context.view_layer.update()
    t = (rig.matrix_world @ pb.tail).copy()
    pb.rotation_euler = (0, 0, 0)
    return t

def find_axis(pb, world_dir):
    bpy.context.view_layer.update()
    base = (rig.matrix_world @ pb.tail).copy()
    best = None
    for axis in (0, 2):
        d = (tail_after(pb, axis, 0.3) - base).dot(Vector(world_dir))
        if best is None or abs(d) > abs(best[2]):
            best = (axis, 1 if d > 0 else -1, d)
    return best[:2]

FWD, SIDE = {}, {}
for pb in rig.pose.bones:
    if pb.name == "Root":
        continue
    side = 1 if pb.name.endswith(".L") else -1
    if pb.name.startswith("Shin"):
        FWD[pb.name] = find_axis(pb, (0, 1, 0))        # positivo = rodilla flexionada (pie hacia atrás)
    else:
        FWD[pb.name] = find_axis(pb, (0, -1, 0))       # positivo = hacia delante
    if pb.name.startswith(("UpperArm", "Thigh")):
        SIDE[pb.name] = find_axis(pb, (side, 0, 0))    # positivo = abrir hacia fuera
    if pb.name in ("Spine", "Head", "Hips"):
        SIDE[pb.name] = find_axis(pb, (1, 0, 0))       # inclinación lateral

def pose(frame, fwd=None, side=None, hips_up=0.0):
    for pb in rig.pose.bones:
        pb.rotation_euler = (0, 0, 0)
        pb.location = (0, 0, 0)
    for name, deg in (fwd or {}).items():
        axis, sign = FWD[name]
        e = list(rig.pose.bones[name].rotation_euler); e[axis] += math.radians(deg) * sign
        rig.pose.bones[name].rotation_euler = e
    for name, deg in (side or {}).items():
        axis, sign = SIDE[name]
        e = list(rig.pose.bones[name].rotation_euler); e[axis] += math.radians(deg) * sign
        rig.pose.bones[name].rotation_euler = e
    rig.pose.bones["Hips"].location = (0, hips_up, 0)   # eje Y local de Hips = arriba
    for pb in rig.pose.bones:
        pb.keyframe_insert("rotation_euler", frame=frame)
        pb.keyframe_insert("location", frame=frame)

ARMS_DOWN = {"UpperArm.L": 8, "UpperArm.R": 8}   # brazos ligeramente separados del cuerpo (como en la referencia)
def with_arms(d=None):
    r = dict(ARMS_DOWN); r.update(d or {}); return r

def new_action(name):
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    rig.animation_data_create()
    rig.animation_data.action = act
    return act

def set_linear_cycle(act, cyclic):
    fcs = []
    if hasattr(act, "fcurves"):
        fcs = list(act.fcurves)
    if not fcs and hasattr(act, "layers"):
        for layer in act.layers:
            for strip in layer.strips:
                for bag in strip.channelbags:
                    fcs += list(bag.fcurves)
    for fc in fcs:
        if cyclic:
            fc.modifiers.new('CYCLES')

actions = []

# Idle ---------------------------------------------------------- 48 f, respiración
a = new_action("Idle")
for f, k in ((1, 0), (25, 1), (49, 0)):
    pose(f, {"Spine": -2 + 3 * k, "Head": 1 - 2 * k, "UpperArm.L": 2 * k, "UpperArm.R": -2 * k,
             "Forearm.L": 6, "Forearm.R": 6},
         with_arms({"UpperArm.L": 8 + 2 * k, "UpperArm.R": 8 + 2 * k}), hips_up=-0.008 * k)
actions.append((a, 1, 49))

# Run ----------------------------------------------------------- 20 f, ciclo
a = new_action("Run")
def run_pose(f, ph, stride=38, lean=12, bob=0.035):
    s = math.sin(ph)
    c = math.cos(ph)
    pose(f, {"Spine": lean, "Head": -lean * 0.6,
             "Thigh.L": stride * s, "Thigh.R": -stride * s,
             "Shin.L": 15 + 50 * max(0, -c) * (1 if s < 0.3 else 0.6), "Shin.R": 15 + 50 * max(0, c) * (1 if -s < 0.3 else 0.6),
             "Foot.L": -10 * s, "Foot.R": 10 * s,
             "UpperArm.L": -stride * 1.1 * s, "UpperArm.R": stride * 1.1 * s,
             "Forearm.L": 75, "Forearm.R": 75},
         with_arms({"UpperArm.L": 12, "UpperArm.R": 12}), hips_up=-bob * abs(c) + bob * 0.4)
for i in range(5):
    run_pose(1 + i * 5, i * math.pi / 2)
actions.append((a, 1, 21))

# Slow (lodo) --------------------------------------------------- 32 f, ciclo
a = new_action("Slow")
for i in range(5):
    ph = i * math.pi / 2
    s, c = math.sin(ph), math.cos(ph)
    pose(1 + i * 8, {"Spine": 20, "Head": -12,
                     "Thigh.L": 22 * s, "Thigh.R": -22 * s,
                     "Shin.L": 10 + 35 * max(0, -c), "Shin.R": 10 + 35 * max(0, c),
                     "UpperArm.L": -15 * s, "UpperArm.R": 15 * s,
                     "Forearm.L": 35, "Forearm.R": 35},
         with_arms({"UpperArm.L": 22, "UpperArm.R": 22}), hips_up=-0.04 - 0.012 * abs(c))
actions.append((a, 1, 33))

# Jump ---------------------------------------------------------- 24 f (salto en el sitio; la altura la da la física)
a = new_action("Jump")
crouch = {"Spine": 18, "Thigh.L": 45, "Thigh.R": 45, "Shin.L": 80, "Shin.R": 80, "Foot.L": -30, "Foot.R": -30,
          "UpperArm.L": -30, "UpperArm.R": -30, "Forearm.L": 30, "Forearm.R": 30}
pose(1, with_arms() and {"Forearm.L": 6, "Forearm.R": 6}, with_arms())
pose(4, crouch, with_arms(), hips_up=-0.12)
pose(7, {"Spine": -5, "Head": 5, "Thigh.L": -5, "Thigh.R": -5, "Foot.L": 25, "Foot.R": 25,
         "UpperArm.L": 150, "UpperArm.R": 150, "Forearm.L": 20, "Forearm.R": 20}, with_arms({"UpperArm.L": 15, "UpperArm.R": 15}))
pose(13, {"Spine": 8, "Thigh.L": 70, "Thigh.R": 40, "Shin.L": 95, "Shin.R": 70,
          "UpperArm.L": 110, "UpperArm.R": 120, "Forearm.L": 40, "Forearm.R": 40}, with_arms({"UpperArm.L": 25, "UpperArm.R": 25}))
pose(19, {"Spine": 2, "Thigh.L": 25, "Thigh.R": 15, "Shin.L": 30, "Shin.R": 25, "Foot.L": 10, "Foot.R": 10,
          "UpperArm.L": 40, "UpperArm.R": 40, "Forearm.L": 25, "Forearm.R": 25}, with_arms({"UpperArm.L": 30, "UpperArm.R": 30}))
pose(21, crouch, with_arms(), hips_up=-0.1)
pose(25, {"Forearm.L": 6, "Forearm.R": 6}, with_arms())
actions.append((a, 1, 25))

# Stumble ------------------------------------------------------- 18 f, reacción al golpe
a = new_action("Stumble")
pose(1, {"Forearm.L": 6, "Forearm.R": 6}, with_arms())
pose(4, {"Spine": -18, "Head": -15, "Thigh.L": -15, "Shin.L": 20, "Thigh.R": 20, "Shin.R": 35,
         "UpperArm.L": 60, "UpperArm.R": 30, "Forearm.L": 50, "Forearm.R": 60},
     with_arms({"UpperArm.L": 55, "UpperArm.R": 70, "Spine": 8}), hips_up=-0.03)
pose(9, {"Spine": 22, "Head": 10, "Thigh.L": 30, "Shin.L": 45, "Thigh.R": -10, "Shin.R": 20,
         "UpperArm.L": -20, "UpperArm.R": 40, "Forearm.L": 40, "Forearm.R": 40},
     with_arms({"UpperArm.L": 40, "UpperArm.R": 35, "Spine": -5}), hips_up=-0.06)
pose(14, {"Spine": 5, "Thigh.L": 8, "Shin.L": 12, "Forearm.L": 15, "Forearm.R": 15}, with_arms(), hips_up=-0.015)
pose(19, {"Forearm.L": 6, "Forearm.R": 6}, with_arms())
actions.append((a, 1, 19))

# Victory ------------------------------------------------------- 40 f, celebra con saltitos y brazos arriba
a = new_action("Victory")
pose(1, {"Forearm.L": 6, "Forearm.R": 6}, with_arms())
for f, up in ((7, 0), (13, 1), (19, 0), (25, 1), (31, 0)):
    pose(f, {"Spine": -6, "Head": -10, "Thigh.L": 10 * up, "Thigh.R": 10 * up, "Shin.L": 20 * (1 - up) + 5, "Shin.R": 20 * (1 - up) + 5,
             "UpperArm.L": 20, "UpperArm.R": 20, "Forearm.L": 15 + 10 * up, "Forearm.R": 15 + 10 * up},
         with_arms({"UpperArm.L": 150 + 12 * up, "UpperArm.R": 150 + 12 * up}), hips_up=0.05 * up - 0.04 * (1 - up))
pose(41, {"Forearm.L": 6, "Forearm.R": 6}, with_arms())
actions.append((a, 1, 41))

for act, f0, f1 in actions:
    set_linear_cycle(act, act.name in ("Idle", "Run", "Slow"))
    act.use_frame_range = True
    act.frame_start, act.frame_end = f0, f1

idle = bpy.data.actions["Idle"]
rig.animation_data.action = idle
scene.frame_start, scene.frame_end = 1, 49
scene.render.fps = 24

# ---------------------------------------------------------------- guardar .blend
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "Guardian_del_Bosque.blend"))

# ---------------------------------------------------------------- exportar FBX para Unity
bpy.ops.object.select_all(action='DESELECT')
rig.select_set(True); body.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(
    filepath=os.path.join(OUT, "Guardian_del_Bosque.fbx"),
    use_selection=True, object_types={'ARMATURE', 'MESH'},
    apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
    add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=True,
    bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=True,
    bake_anim_simplify_factor=0.0, mesh_smooth_type='FACE', path_mode='AUTO')

print("GUARDIAN_OK", len(body.data.polygons), "tris~", sum(len(p.vertices) - 2 for p in body.data.polygons))
