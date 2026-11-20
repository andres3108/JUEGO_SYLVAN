using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GestorTutorial : MonoBehaviour
{
    [Header("Asigna los 3 carteles aquí")]
    public GameObject cartelMovimiento;
    public GameObject cartelRecolectar;
    public GameObject cartelSaltar;

    [Header("Límite opcional para que no avance antes de completar")]
    [Tooltip("Posición X máxima a la que puede llegar antes de aprender a saltar")]
    public float limiteXAntesDeSaltar = 8f;

    private int pasoActual = 1;
    private bool esperandoTransicion = false;
    private Transform jugador;
    private float posXInicial;
    private float tiempoMoviendose = 0f;
    private int residuosIniciales = -1;

    void Start()
    {
        if (cartelMovimiento != null) cartelMovimiento.SetActive(true);
        if (cartelRecolectar != null) cartelRecolectar.SetActive(false);
        if (cartelSaltar != null) cartelSaltar.SetActive(false);
    }

    void Update()
    {
        if (jugador == null)
        {
            var g = FindFirstObjectByType<GuardianController>();
            if (g != null)
            {
                jugador = g.transform;
                posXInicial = jugador.position.x;
            }
            return;
        }

        // Evitar que cruce hacia el tronco si todavía no completa el tutorial
        if (pasoActual <= 3 && jugador.position.x > limiteXAntesDeSaltar)
        {
            Vector3 pos = jugador.position;
            pos.x = limiteXAntesDeSaltar;
            jugador.position = pos;
        }

        if (esperandoTransicion) return;

        var teclado = Keyboard.current;
        if (teclado == null) return;

        // PASO 1: Validar que realmente camine (al menos 0.6s o 1.5m)
        if (pasoActual == 1)
        {
            bool presionandoMover = teclado.aKey.isPressed || teclado.dKey.isPressed ||
                                    teclado.leftArrowKey.isPressed || teclado.rightArrowKey.isPressed;

            if (presionandoMover)
            {
                tiempoMoviendose += Time.deltaTime;
            }

            if (tiempoMoviendose >= 0.6f || Mathf.Abs(jugador.position.x - posXInicial) >= 1.5f)
            {
                StartCoroutine(PasarARecolectar());
            }
        }
        // PASO 2: No avanza al siguiente cartel hasta que recoja al menos 1 residuo
        else if (pasoActual == 2)
        {
            // Detectar si aumentaron los residuos recogidos (o si algún residuo desapareció)
            int residuosEnEscena = FindObjectsByType<Residuo>(FindObjectsSortMode.None).Length;
            if (residuosIniciales == -1) residuosIniciales = residuosEnEscena;

            if (residuosEnEscena < residuosIniciales)
            {
                StartCoroutine(PasarASaltar());
            }
        }
        // PASO 3: No cierra el tutorial hasta que ejecute un salto
        else if (pasoActual == 3)
        {
            if (teclado.spaceKey.wasPressedThisFrame || teclado.wKey.wasPressedThisFrame ||
                teclado.upArrowKey.wasPressedThisFrame)
            {
                StartCoroutine(FinalizarTutorial());
            }
        }
    }

    IEnumerator PasarARecolectar()
    {
        esperandoTransicion = true;
        yield return new WaitForSeconds(0.3f);
        if (cartelMovimiento != null) cartelMovimiento.SetActive(false);
        if (cartelRecolectar != null) cartelRecolectar.SetActive(true);
        residuosIniciales = FindObjectsByType<Residuo>(FindObjectsSortMode.None).Length;
        pasoActual = 2;
        esperandoTransicion = false;
    }

    IEnumerator PasarASaltar()
    {
        esperandoTransicion = true;
        yield return new WaitForSeconds(0.4f);
        if (cartelRecolectar != null) cartelRecolectar.SetActive(false);
        if (cartelSaltar != null) cartelSaltar.SetActive(true);
        pasoActual = 3;
        esperandoTransicion = false;
    }

    IEnumerator FinalizarTutorial()
    {
        esperandoTransicion = true;
        yield return new WaitForSeconds(0.6f);
        if (cartelSaltar != null) cartelSaltar.SetActive(false);
        pasoActual = 4;
        esperandoTransicion = false;
    }
}