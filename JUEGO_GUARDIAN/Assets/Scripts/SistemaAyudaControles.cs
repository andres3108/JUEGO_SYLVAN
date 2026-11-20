using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SistemaAyudaControles : MonoBehaviour
{
    [Header("1. Tarjeta General (HUD) y Botones")]
    public GameObject tarjetaGeneral;
    public Button botonAbrirAyuda;
    public Button botonCerrarAyuda;
    public bool mostrarTarjetaAlInicio = true;

    [Header("2. Micro-indicadores en el Escenario")]
    public GameObject microMover;
    public GameObject microTocar;
    public GameObject microSaltar;

    [Header("3. Distancia y Límite de Intentos")]
    public float distanciaActivarCartel = 2.2f;
    public int maxIntentosConGuia = 2;

    // Se mantiene entre reintentos de la escena y se reinicia al cerrar y volver a abrir el juego
    private static int intentosSesion = 0;

    private Transform jugadorActivo;
    private int residuosIniciales = -1;
    private bool movimientoCompletado = false;
    private bool recoleccionCompletada = false;
    private bool saltoCompletado = false;
    private float tiempoInicio;

    void Start()
    {
        tiempoInicio = Time.time;
        intentosSesion++;

        if (botonAbrirAyuda != null)
            botonAbrirAyuda.onClick.AddListener(AlternarTarjetaGeneral);

        if (botonCerrarAyuda != null)
            botonCerrarAyuda.onClick.AddListener(CerrarTarjetaGeneral);

        if (microMover != null) microMover.SetActive(false);
        if (microTocar != null) microTocar.SetActive(false);
        if (microSaltar != null) microSaltar.SetActive(false);

        bool mostrarGuiaEnEsteIntento = mostrarTarjetaAlInicio && (intentosSesion <= maxIntentosConGuia);

        if (tarjetaGeneral != null)
        {
            tarjetaGeneral.SetActive(mostrarGuiaEnEsteIntento);
        }

        // A partir del tercer intento, omite los micro-carteles automáticos y deja disponible el botón '?'
        if (intentosSesion > maxIntentosConGuia)
        {
            movimientoCompletado = true;
            recoleccionCompletada = true;
            saltoCompletado = true;
        }
        else if (!mostrarTarjetaAlInicio && microMover != null)
        {
            microMover.SetActive(true);
        }

        residuosIniciales = FindObjectsByType<Residuo>().Length;
        BuscarJugadorActivo();
    }

    void BuscarJugadorActivo()
    {
        GameObject g1 = GameObject.Find("Guardian_del_Bosque");
        if (g1 != null && g1.activeInHierarchy)
        {
            jugadorActivo = g1.transform;
            return;
        }

        GameObject g2 = GameObject.Find("Guardiana_del_Bosque");
        if (g2 != null && g2.activeInHierarchy)
        {
            jugadorActivo = g2.transform;
        }
    }

    void Update()
    {
        if (jugadorActivo == null || !jugadorActivo.gameObject.activeInHierarchy)
            BuscarJugadorActivo();

        var teclado = Keyboard.current;
        if (teclado == null) return;

        if (Time.time - tiempoInicio < 0.4f) return;

        if (teclado.hKey.wasPressedThisFrame)
            AlternarTarjetaGeneral();

        bool presionoMover = teclado.aKey.wasPressedThisFrame || teclado.dKey.wasPressedThisFrame ||
                             teclado.leftArrowKey.wasPressedThisFrame || teclado.rightArrowKey.wasPressedThisFrame;

        if (presionoMover && tarjetaGeneral != null && tarjetaGeneral.activeSelf)
        {
            CerrarTarjetaGeneral();
            return;
        }

        bool tarjetaCerrada = (tarjetaGeneral == null || !tarjetaGeneral.activeSelf);

        // PASO 1: MOVERSE (único activo al cerrar la tarjeta general)
        if (!movimientoCompletado && microMover != null)
        {
            if (tarjetaCerrada && !microMover.activeSelf)
            {
                microMover.SetActive(true);
            }
            else if (microMover.activeSelf && presionoMover)
            {
                movimientoCompletado = true;
                microMover.SetActive(false);
            }
            return; // No evalúa el siguiente paso hasta que aprenda a moverse
        }

        // PASO 2: RECOLECTAR (solo después de haberse movido)
        if (!recoleccionCompletada && microTocar != null && jugadorActivo != null)
        {
            int actuales = FindObjectsByType<Residuo>().Length;
            if (actuales < residuosIniciales)
            {
                recoleccionCompletada = true;
                microTocar.SetActive(false);
            }
            else if (tarjetaCerrada)
            {
                float distX = Mathf.Abs(jugadorActivo.position.x - microTocar.transform.position.x);
                microTocar.SetActive(distX <= distanciaActivarCartel);
            }
            return; // No evalúa salto hasta recoger el primer residuo
        }

        // PASO 3: SALTAR (solo después de haber recolectado)
        if (!saltoCompletado && microSaltar != null && jugadorActivo != null)
        {
            bool presionoSaltar = teclado.spaceKey.wasPressedThisFrame || teclado.wKey.wasPressedThisFrame ||
                                  teclado.upArrowKey.wasPressedThisFrame;

            if (microSaltar.activeSelf && presionoSaltar)
            {
                saltoCompletado = true;
                microSaltar.SetActive(false);
            }
            else if (tarjetaCerrada)
            {
                float distX = Mathf.Abs(jugadorActivo.position.x - microSaltar.transform.position.x);
                microSaltar.SetActive(distX <= distanciaActivarCartel);
            }
        }
    }

    public void AlternarTarjetaGeneral()
    {
        if (tarjetaGeneral == null) return;

        bool nuevoEstado = !tarjetaGeneral.activeSelf;
        tarjetaGeneral.SetActive(nuevoEstado);

        if (nuevoEstado)
        {
            if (microMover != null) microMover.SetActive(false);
            if (microTocar != null) microTocar.SetActive(false);
            if (microSaltar != null) microSaltar.SetActive(false);
        }
        else if (!movimientoCompletado && microMover != null)
        {
            microMover.SetActive(true);
        }
    }

    public void CerrarTarjetaGeneral()
    {
        if (tarjetaGeneral != null)
            tarjetaGeneral.SetActive(false);

        if (!movimientoCompletado && microMover != null)
            microMover.SetActive(true);
    }
}