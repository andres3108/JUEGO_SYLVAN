# Ciclos de caminar (Walk) y correr (Run) por fases de la zancada, compartidos por el Guardián y la Guardiana.
# Se ejecuta con exec() después de las animaciones base de crear_guardian.py (usa new_action, pose,
# with_arms y set_linear_cycle) y reemplaza las acciones Run y Walk.
#
# Cada pierna recorre la misma curva (p = 0..1, la otra pierna va medio ciclo desfasada):
#   p = 0     contacto (talón delante, pierna casi estirada)
#   p ~ 0.12  amortigua (la rodilla cede un poco)
#   p = 0.25  pasa bajo el cuerpo
#   p = 0.5   despegue (pierna detrás, punta del pie empuja)
#   p ~ 0.75  balanceo (rodilla muy doblada, el pie pasa alto)
# Ángulos en grados: muslo + = hacia delante; rodilla + = doblada; pie + = punta hacia abajo.
import math as _m

def _curve(keys, p):
    p %= 1.0
    for (p0, v0), (p1, v1) in zip(keys, keys[1:]):
        if p0 <= p <= p1:
            t = (p - p0) / (p1 - p0)
            t = t * t * (3 - 2 * t)          # transición suave entre fases
            return v0 + (v1 - v0) * t
    return keys[-1][1]

CICLOS = {
    "Walk": dict(
        frames=32, lean=4, head=-2, forearm=15, arm_swing=0.8, arm_out=8, bob=0.014, base=-0.006,
        thigh=[(0, 22), (0.12, 16), (0.25, 4), (0.5, -18), (0.62, -12), (0.75, 8), (0.88, 22), (1, 22)],
        shin=[(0, 3), (0.12, 12), (0.25, 6), (0.5, 14), (0.62, 40), (0.75, 55), (0.88, 18), (1, 3)],
        foot=[(0, -7), (0.12, 2), (0.25, 0), (0.5, 12), (0.62, 14), (0.75, 3), (0.88, -7), (1, -7)],
    ),
    "Run": dict(
        frames=20, lean=10, head=-6, forearm=75, arm_swing=1.0, arm_out=12, bob=0.03, base=-0.01,
        thigh=[(0, 30), (0.12, 18), (0.25, 4), (0.5, -26), (0.62, -16), (0.75, 18), (0.88, 36), (1, 30)],
        shin=[(0, 10), (0.12, 30), (0.25, 22), (0.5, 26), (0.62, 75), (0.75, 95), (0.88, 45), (1, 10)],
        foot=[(0, -6), (0.12, 3), (0.25, 0), (0.5, 18), (0.62, 20), (0.75, 7), (0.88, -7), (1, -6)],
    ),
}

for _name, _c in CICLOS.items():
    _old = bpy.data.actions.get(_name)
    if _old:
        bpy.data.actions.remove(_old)
    _act = new_action(_name)
    _n = _c["frames"]
    for _f in range(0, _n + 1, 2):
        _pL = _f / _n
        _pR = _pL + 0.5
        _tL, _tR = _curve(_c["thigh"], _pL), _curve(_c["thigh"], _pR)
        # la cadera baja al amortiguar (p ~ 0.12 y 0.62) y sube en el impulso
        _bob = -_c["bob"] * max(0.0, _m.cos(4 * _m.pi * (_pL - 0.12)))
        pose(1 + _f,
             {"Spine": _c["lean"], "Head": _c["head"],
              "Thigh.L": _tL, "Thigh.R": _tR,
              "Shin.L": _curve(_c["shin"], _pL), "Shin.R": _curve(_c["shin"], _pR),
              "Foot.L": _curve(_c["foot"], _pL), "Foot.R": _curve(_c["foot"], _pR),
              # brazos al contrario que la pierna del mismo lado
              "UpperArm.L": -_c["arm_swing"] * _tL, "UpperArm.R": -_c["arm_swing"] * _tR,
              "Forearm.L": _c["forearm"], "Forearm.R": _c["forearm"]},
             with_arms({"UpperArm.L": _c["arm_out"], "UpperArm.R": _c["arm_out"]}),
             hips_up=_c["base"] + _bob)
    set_linear_cycle(_act, True)
    _act.use_frame_range = True
    _act.frame_start, _act.frame_end = 1, _n + 1
