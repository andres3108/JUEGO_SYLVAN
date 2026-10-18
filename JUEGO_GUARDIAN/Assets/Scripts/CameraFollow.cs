using UnityEngine;

// La cámara sigue al personaje de lado (vista lateral).
// Si "limitar" está activo, no muestra nada fuera del nivel: al empezar, el borde izquierdo de la
// pantalla coincide con el inicio del recorrido (minX) y al final con su término (maxX).
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 1.2f, -10f);
    public float smooth = 6f;

    [Header("Altura")]
    [Tooltip("Si está desactivado, la cámara se queda a 'alturaFija' y solo sigue al personaje en horizontal")]
    public bool seguirY = true;
    public float alturaFija = 2f;

    [Header("Límites del nivel (x)")]
    public bool limitar = false;
    public float minX = 0f;
    public float maxX = 60f;

    Camera cam;

    void Awake() => cam = GetComponent<Camera>();

    void LateUpdate()
    {
        if (!target) return;
        transform.position = Vector3.Lerp(transform.position, Goal(), 1f - Mathf.Exp(-smooth * Time.deltaTime));
    }

    // coloca la cámara directamente en su posición (sin suavizado), p. ej. al preparar el nivel
    public void Snap()
    {
        if (target) transform.position = Goal();
    }

    Vector3 Goal()
    {
        if (!cam) cam = GetComponent<Camera>();
        Vector3 goal = target.position + offset;
        if (!seguirY) goal.y = alturaFija;
        if (limitar && cam != null && cam.orthographic)
        {
            float half = cam.orthographicSize * cam.aspect;
            goal.x = Mathf.Clamp(goal.x, minX + half, Mathf.Max(minX + half, maxX - half));
        }
        return goal;
    }
}
