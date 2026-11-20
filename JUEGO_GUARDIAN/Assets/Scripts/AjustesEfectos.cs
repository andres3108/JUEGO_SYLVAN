using UnityEngine;

// Ajustes de los efectos de los residuos, editables desde el Inspector con barras deslizantes y en vivo
// (también mientras se juega). El archivo está en Assets/Resources/AjustesEfectos.asset.
// Para abrirlo: menú Guardianes del Bosque > Ajustes de efectos.
// Los cambios hechos en Play se quedan guardados (es un archivo del proyecto, no un objeto de la escena).
[CreateAssetMenu(fileName = "AjustesEfectos", menuName = "Guardianes del Bosque/Ajustes de efectos")]
public class AjustesEfectos : ScriptableObject
{
    [Header("Aura del residuo (antes de recogerlo)")]
    [Tooltip("Brillo del aura: 0 = invisible, 1 = 100 %")]
    [Range(0f, 1f)] public float brilloAura = 0.4f;
    [Tooltip("Tamaño del aura en metros")]
    [Range(0.5f, 4f)] public float tamañoAura = 1.9f;
    [Tooltip("Número de rayos (pétalos) del aura y del destello")]
    [Range(2, 16)] public int petalos = 6;
    [Tooltip("Velocidad de giro de los rayos (grados por segundo)")]
    [Range(0f, 120f)] public float giroRayos = 25f;

    [Header("Destello al recoger")]
    [Tooltip("Brillo del destello: 0 = sin destello, 1 = 100 %")]
    [Range(0f, 1f)] public float brilloDestello = 1f;
    [Tooltip("Tamaño del destello (1 = normal)")]
    [Range(0.3f, 2f)] public float tamañoDestello = 1f;

    [Header("Hojas al recoger")]
    [Tooltip("Cuántas hojas salen al recoger un residuo")]
    [Range(0, 40)] public int cantidadHojas = 8;
    [Tooltip("Tamaño de las hojas (1 = normal)")]
    [Range(0.3f, 2f)] public float tamañoHojas = 1f;
    [Tooltip("Brillo del halo detrás de cada hoja: 0 = sin halo, 1 = 100 %")]
    [Range(0f, 1f)] public float brilloHaloHojas = 1f;

    [Header("Chispas al recoger")]
    [Tooltip("Cuántas chispas del color del residuo salen al recogerlo")]
    [Range(0, 40)] public int cantidadChispas = 16;

    [Header("Sonido al recoger")]
    [Tooltip("Volumen de la campanita al recoger un residuo: 0 = sin sonido, 1 = 100 %")]
    [Range(0f, 1f)] public float volumenRecogida = 0.9f;

    static AjustesEfectos actual;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar() => actual = null;

    // los ajustes del proyecto (o los valores por defecto si el archivo no existe)
    public static AjustesEfectos Actual
    {
        get
        {
            if (actual == null) actual = Resources.Load<AjustesEfectos>("AjustesEfectos");
            if (actual == null) actual = CreateInstance<AjustesEfectos>();
            return actual;
        }
    }
}
