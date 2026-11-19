

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroduccionController : MonoBehaviour
{
    [Header("Pantallas de introduccion")]
    public GameObject pantalla1;
    public GameObject pantalla2;
    public GameObject pantalla3;

    [Header("Indicadores")]
    public Image indicador1;
    public Image indicador2;
    public Image indicador3;

    [Header("Sprites de indicadores")]
    public Sprite indicadorGris;
    public Sprite indicadorAmarillo;

    [Header("Configuracion de transicion")]
    public float duracionTransicion = 0.6f;

    [Header("Animacion del titulo")]
    public CanvasGroup tituloSylvan;
    public float duracionTitulo = 0.8f;
    public float escalaInicialTitulo = 0.8f;

    private RectTransform rectTitulo;
    private Vector3 escalaFinalTitulo;

    private int pantallaActual = 0;
    private bool enTransicion = false;

    private CanvasGroup grupo1;
    private CanvasGroup grupo2;
    private CanvasGroup grupo3;

    void Start()
    {
        grupo1 = pantalla1.GetComponent<CanvasGroup>();
        grupo2 = pantalla2.GetComponent<CanvasGroup>();
        grupo3 = pantalla3.GetComponent<CanvasGroup>();

        MostrarPantalla(0);

        grupo1.alpha = 0;
        StartCoroutine(Aparecer(grupo1));

        rectTitulo = tituloSylvan.GetComponent<RectTransform>();
        escalaFinalTitulo = rectTitulo.localScale;

        StartCoroutine(AparecerTitulo());
    }

    public void Continuar()
    {
        if (enTransicion)
            return;

        if (pantallaActual < 2)
        {
            StartCoroutine(CambiarPantalla());
        }
        else
        {
            StartCoroutine(IrAlMenu());
        }
    }

    IEnumerator CambiarPantalla()
    {
        enTransicion = true;

        CanvasGroup grupoActual = ObtenerGrupo(pantallaActual);

        yield return StartCoroutine(Desaparecer(grupoActual));

        pantallaActual++;

        MostrarPantalla(pantallaActual);

        CanvasGroup siguienteGrupo = ObtenerGrupo(pantallaActual);
        siguienteGrupo.alpha = 0;

        yield return StartCoroutine(Aparecer(siguienteGrupo));

        enTransicion = false;
    }

    IEnumerator IrAlMenu()
    {
        enTransicion = true;

        CanvasGroup grupoActual = ObtenerGrupo(pantallaActual);

        yield return StartCoroutine(Desaparecer(grupoActual));

        SceneManager.LoadScene("MenuPrincipal");
    }

    void MostrarPantalla(int indice)
    {
        pantalla1.SetActive(indice == 0);
        pantalla2.SetActive(indice == 1);
        pantalla3.SetActive(indice == 2);

        indicador1.sprite = indice == 0 ? indicadorAmarillo : indicadorGris;
        indicador2.sprite = indice == 1 ? indicadorAmarillo : indicadorGris;
        indicador3.sprite = indice == 2 ? indicadorAmarillo : indicadorGris;
    }

    CanvasGroup ObtenerGrupo(int indice)
    {
        if (indice == 0) return grupo1;
        if (indice == 1) return grupo2;

        return grupo3;
    }

    IEnumerator Aparecer(CanvasGroup grupo)
    {
        grupo.interactable = false;
        grupo.blocksRaycasts = false;

        float tiempo = 0;

        while (tiempo < duracionTransicion)
        {
            tiempo += Time.deltaTime;
            grupo.alpha = Mathf.Clamp01(tiempo / duracionTransicion);

            yield return null;
        }

        grupo.alpha = 1;
        grupo.interactable = true;
        grupo.blocksRaycasts = true;
    }

    IEnumerator Desaparecer(CanvasGroup grupo)
    {
        grupo.interactable = false;
        grupo.blocksRaycasts = false;

        float tiempo = 0;

        while (tiempo < duracionTransicion)
        {
            tiempo += Time.deltaTime;
            grupo.alpha = 1 - Mathf.Clamp01(tiempo / duracionTransicion);

            yield return null;
        }

        grupo.alpha = 0;
    }

    IEnumerator AparecerTitulo()
    {
        float tiempo = 0;

        rectTitulo.localScale = escalaFinalTitulo * escalaInicialTitulo;
        tituloSylvan.alpha = 0;

        while (tiempo < duracionTitulo)
        {
            tiempo += Time.deltaTime;

            float progreso = Mathf.Clamp01(tiempo / duracionTitulo);

            tituloSylvan.alpha = progreso;

            rectTitulo.localScale = Vector3.Lerp(
                escalaFinalTitulo * escalaInicialTitulo,
                escalaFinalTitulo,
                progreso
            );

            yield return null;
        }

        tituloSylvan.alpha = 1;
        rectTitulo.localScale = escalaFinalTitulo;
    }


}
