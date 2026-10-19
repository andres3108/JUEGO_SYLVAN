using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menú de pausa de las zonas: al pulsar Escape (o el botón "atrás" / el botón II en el celular) mientras se juega, el juego se detiene y aparece un
// tablero de madera con CONTINUAR, VOLVER AL INICIO y SALIR (imágenes de Assets/Resources/Menu,
// generadas con Fondos/Menu/generar_menu.py). Escape otra vez también continúa.
// No se abre con la ventana de selección de personaje abierta ni con la partida terminada.
// Lo añade el GameManager al empezar cada zona.
public class MenuPausa : MonoBehaviour
{
    public static bool Pausado { get; private set; }

    GameObject ui;
    bool controlesAntes;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar() => Pausado = false;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
        if (Pausado) { Continuar(); return; }

        Pausar();   // no se abre con la partida terminada ni con la selección abierta
    }

    void OnDestroy()
    {
        if (!Pausado) return;
        Pausado = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    // también la llama el botón de pausa de los controles táctiles
    public void Pausar()
    {
        var gm = GameManager.Instance;
        if (Pausado || gm == null || gm.IsGameOver || Time.timeScale == 0f) return;
        Pausado = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        var jugador = GameManager.Instance ? GameManager.Instance.player : null;
        if (jugador) { controlesAntes = jugador.controlsEnabled; jugador.controlsEnabled = false; }
        Construir();
    }

    public void Continuar()
    {
        if (ui) Destroy(ui);
        Pausado = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        var jugador = GameManager.Instance ? GameManager.Instance.player : null;
        if (jugador) jugador.controlsEnabled = controlesAntes;
    }

    public void VolverAlInicio()
    {
        Pausado = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(0);   // el menú principal es la escena 0
    }

    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ------------------------------------------------------------------ interfaz
    void Construir()
    {
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        ui = new GameObject("MenuPausa", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = ui.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;   // por encima del HUD y de los efectos
        var scaler = ui.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800, 500);
        scaler.matchWidthOrHeight = 0.5f;

        // velo oscuro sobre el juego (también bloquea los clics al juego)
        var velo = Rect(ui.transform, "Velo", Vector2.zero, new Vector2(800, 500));
        velo.anchorMin = Vector2.zero; velo.anchorMax = Vector2.one; velo.offsetMin = velo.offsetMax = Vector2.zero;
        velo.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        // tablero 760 x 820 a la mitad; botones en el mismo sitio que en el diseño
        var panel = Rect(ui.transform, "Panel", Vector2.zero, new Vector2(380, 410));
        Imagen(panel.gameObject, "panel_pausa").raycastTarget = false;
        var continuar = Boton(panel, "BotonContinuar", "btn_continuar_juego", 72, Continuar);
        var inicio = Boton(panel, "BotonVolverInicio", "btn_volver_inicio", -23, VolverAlInicio);
        var salir = Boton(panel, "BotonSalir", "btn_salir", -118, Salir);

        // flechas arriba/abajo entre los botones (en círculo) y Enter/Espacio para elegir
        Enlazar(continuar, salir, inicio);
        Enlazar(inicio, continuar, salir);
        Enlazar(salir, inicio, continuar);
        EventSystem.current.SetSelectedGameObject(continuar.gameObject);
    }

    static void Enlazar(Button b, Button arriba, Button abajo) =>
        b.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = arriba, selectOnDown = abajo };

    static Button Boton(Transform padre, string nombre, string recurso, float y, UnityEngine.Events.UnityAction accion)
    {
        var rt = Rect(padre, nombre, new Vector2(0, y), new Vector2(280, 76));
        var img = Imagen(rt.gameObject, recurso);
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.transition = Selectable.Transition.None;   // los estados los anima BotonMenu
        b.onClick.AddListener(accion);
        rt.gameObject.AddComponent<BotonMenu>();
        return b;
    }

    static Image Imagen(GameObject go, string recurso)
    {
        var img = go.AddComponent<Image>();
        var tex = Resources.Load<Texture2D>("Menu/" + recurso);
        if (tex) img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        img.preserveAspect = true;
        return img;
    }

    static RectTransform Rect(Transform padre, string nombre, Vector2 pos, Vector2 tam)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        return rt;
    }
}
