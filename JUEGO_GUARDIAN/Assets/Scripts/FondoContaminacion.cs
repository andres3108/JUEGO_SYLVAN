using UnityEngine;

// El bosque empieza oscuro y apagado (contaminado) y se va iluminando y volviendo más verde a medida que
// sube la "Salud del Bosque" (residuos recogidos). Tiñe las copias del fondo ("Fondo") usando copias de
// su material en tiempo de juego, así los materiales del proyecto no se modifican.
public class FondoContaminacion : MonoBehaviour
{
    [Tooltip("Color del fondo con 0 % de salud (bosque contaminado)")]
    public Color contaminado = new Color(0.32f, 0.30f, 0.30f);
    [Tooltip("Color a mitad de camino")]
    public Color intermedio = new Color(0.68f, 0.70f, 0.60f);
    [Tooltip("Color con 100 % de salud (bosque limpio, algo más verde)")]
    public Color limpio = new Color(0.94f, 1.00f, 0.88f);

    Material[] mats;
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    void Start()
    {
        var fondo = GameObject.Find("Fondo");
        if (!fondo) { enabled = false; return; }
        var rs = fondo.GetComponentsInChildren<Renderer>();
        mats = new Material[rs.Length];
        for (int i = 0; i < rs.Length; i++) mats[i] = rs[i].material;   // copia en tiempo de juego
        Aplicar(0f);
    }

    void Update()
    {
        if (mats != null && GameManager.Instance) Aplicar(GameManager.Instance.Salud);
    }

    void Aplicar(float salud)
    {
        Color c = salud < 0.5f ? Color.Lerp(contaminado, intermedio, salud * 2f)
                               : Color.Lerp(intermedio, limpio, (salud - 0.5f) * 2f);
        foreach (var m in mats)
        {
            if (!m) continue;
            if (m.HasProperty(BaseColor)) m.SetColor(BaseColor, c);
            else m.color = c;
        }
    }
}
