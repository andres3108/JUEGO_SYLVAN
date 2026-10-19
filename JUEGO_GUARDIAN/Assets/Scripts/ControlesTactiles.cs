using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Controles táctiles para celulares (Android): botones de madera en pantalla para moverse (◀ ▶),
// saltar (▲) y pausar (II). Se pueden pulsar varios a la vez (correr y saltar).
// Solo aparecen en celulares/tabletas (o en el editor si se activa "mostrarEnEditor");
// en el PC se sigue jugando con el teclado. Lo añade el GameManager al empezar cada zona.
// GuardianController lee Horizontal y ConsumirSalto(); el botón de pausa abre el MenuPausa.
public class ControlesTactiles : MonoBehaviour
{
    public static float Horizontal { get; private set; }
    static bool saltoPendiente;
    public static bool mostrarEnEditor = false;

    public static bool EsMovil => Application.isMobilePlatform || (Application.isEditor && mostrarEnEditor);

    bool izq, der;
    GameObject ui;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar() { Horizontal = 0f; saltoPendiente = false; }

    // el salto se "consume" una vez (como wasPressedThisFrame del teclado)
    public static bool ConsumirSalto()
    {
        bool s = saltoPendiente;
        saltoPendiente = false;
        return s;
    }

    void Start()
    {
        if (!EsMovil) { enabled = false; return; }
        Construir();
    }

    void Update()
    {
        var gm = GameManager.Instance;
        // ocultos con la partida terminada, en pausa o con la ventana de selección abierta
        bool visibles = gm != null && !gm.IsGameOver && Time.timeScale > 0f;
        if (ui && ui.activeSelf != visibles) ui.SetActive(visibles);
        if (!visibles) { izq = der = false; saltoPendiente = false; }
        Horizontal = (der ? 1f : 0f) - (izq ? 1f : 0f);
    }

    void OnDestroy() { Horizontal = 0f; saltoPendiente = false; }

    // ------------------------------------------------------------------ interfaz
    void Construir()
    {
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        ui = new GameObject("ControlesTactiles", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = ui.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;   // debajo de la selección de personaje (100) y de la pausa (200)
        var scaler = ui.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800, 500);
        scaler.matchWidthOrHeight = 0.5f;

        // abajo a la izquierda: moverse; abajo a la derecha: saltar; arriba al centro: pausa
        Boton("Izquierda", "tactil_izquierda", new Vector2(0, 0), new Vector2(70, 65), 100, v => izq = v);
        Boton("Derecha", "tactil_derecha", new Vector2(0, 0), new Vector2(185, 65), 100, v => der = v);
        Boton("Saltar", "tactil_saltar", new Vector2(1, 0), new Vector2(-80, 70), 115, v => { if (v) saltoPendiente = true; });
        Boton("Pausa", "tactil_pausa", new Vector2(0.5f, 1), new Vector2(0, -32), 52, v =>
        {
            if (v && GameManager.Instance) GameManager.Instance.GetComponent<MenuPausa>()?.Pausar();
        });
    }

    void Boton(string nombre, string recurso, Vector2 ancla, Vector2 pos, float tam, System.Action<bool> alPulsar)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(ui.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = ancla;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(tam, tam);
        var img = go.GetComponent<Image>();
        var tex = Resources.Load<Texture2D>("Menu/" + recurso);
        if (tex) img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        img.preserveAspect = true;
        img.color = new Color(1f, 1f, 1f, 0.8f);   // un poco transparentes para no tapar el juego
        go.AddComponent<BotonTactil>().alPulsar = alPulsar;
    }
}

// Botón que avisa al pulsar y al soltar (también si el dedo se sale del botón).
public class BotonTactil : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public System.Action<bool> alPulsar;
    bool pulsado;

    public void OnPointerDown(PointerEventData e) { pulsado = true; alPulsar?.Invoke(true); transform.localScale = Vector3.one * 0.9f; }
    public void OnPointerUp(PointerEventData e) => Soltar();
    public void OnPointerExit(PointerEventData e) => Soltar();
    void OnDisable() => Soltar();

    void Soltar()
    {
        if (!pulsado) return;
        pulsado = false;
        alPulsar?.Invoke(false);
        transform.localScale = Vector3.one;
    }
}
