using UnityEngine;

// Huecos en el suelo (Zona 3: las cascadas del muro de piedra): si el jugador cae por debajo de
// "alturaCaida" no pierde la partida, sino que reaparece en "punto" (el inicio de la zona) y sigue
// jugando con lo que ya había recogido. Sin este componente en la escena, caer sigue siendo perder
// (GuardianController.fallLimitY).
public class Reaparicion : MonoBehaviour
{
    public Vector3 punto = new(1.2f, 0.05f, 0f);
    public float alturaCaida = -3f;
    public string aviso = "¡Cuidado con el agua! Vuelves al inicio";

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.IsGameOver || gm.player == null) return;
        var jugador = gm.player;
        if (jugador.transform.position.y >= alturaCaida) return;

        var rb = jugador.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.position = punto;
        }
        jugador.transform.SetPositionAndRotation(punto, Quaternion.Euler(0f, 90f, 0f));
        var cam = Camera.main ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cam) cam.Snap();
        gm.Aviso(aviso);
    }
}
