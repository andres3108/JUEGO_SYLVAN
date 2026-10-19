using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Ventana flotante al iniciar el juego para elegir personaje (cara + nombre de cada uno).
// El elegido se activa y pasa a ser el jugador; el otro se desactiva. Se puede elegir con el ratón,
// con las flechas ← → + Enter, o con las teclas 1 / 2. Tras reiniciar la partida se recuerda la elección.
public class SeleccionPersonaje : MonoBehaviour
{
    [System.Serializable]
    public class Opcion
    {
        public string nombre;
        public GameObject personaje;
        public Sprite retrato;
    }

    public Opcion[] opciones;

    // nombres de los personajes según su objeto en la escena
    public static string Nombre(GameObject personaje) =>
        personaje && personaje.name.Contains("Guardiana") ? "Maya" : "Sylvan";

    public static string Titulo(GameObject personaje) =>
        personaje && personaje.name.Contains("Guardiana") ? "Maya: La Guardiana del Bosque" : "Sylvan: El Guardián del Bosque";
    public string titulo = "Elige tu Guardián del Bosque";

    static int eleccionAnterior = -1;   // se mantiene al reiniciar la escena

    // al empezar una partida nueva (cada vez que se pulsa Play) se vuelve a mostrar la ventana
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ReiniciarEleccion() => eleccionAnterior = -1;

    // El menú principal fija el guardián antes de cargar la zona (0 = Sylvan, 1 = Maya): así la zona
    // empieza directamente con él y no muestra esta ventana. Sin menú (zona abierta directamente en el
    // editor) se sigue mostrando la ventana de selección.
    public static void Preseleccionar(int indice) => eleccionAnterior = Mathf.Max(0, indice);

    GameObject panel;
    Image[] marcos;
    int actual;
    public bool Elegido { get; private set; }

    static readonly Color Fondo = new Color(0.08f, 0.18f, 0.1f, 0.88f);
    static readonly Color Tarjeta = new Color32(236, 244, 222, 255);
    static readonly Color Resalte = new Color32(246, 196, 64, 255);
    static readonly Color VerdeOscuro = new Color32(36, 84, 44, 255);

    void Start()
    {
        foreach (var o in opciones)
            if (o.personaje) o.personaje.SetActive(false);

        if (eleccionAnterior >= 0 && eleccionAnterior < opciones.Length)
        {
            Elegir(eleccionAnterior);
            return;
        }
        Time.timeScale = 0f;
        Construir();
    }

    void Update()
    {
        if (panel == null) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) Marcar((actual + opciones.Length - 1) % opciones.Length);
        if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) Marcar((actual + 1) % opciones.Length);
        if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) Elegir(actual);
        for (int i = 0; i < opciones.Length && i < 9; i++)
            if (kb[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame) Elegir(i);
    }

    public void Elegir(int i)
    {
        eleccionAnterior = i;
        Elegido = true;
        for (int k = 0; k < opciones.Length; k++)
            if (opciones[k].personaje) opciones[k].personaje.SetActive(k == i);

        var jugador = opciones[i].personaje ? opciones[i].personaje.GetComponent<GuardianController>() : null;
        if (GameManager.Instance) GameManager.Instance.player = jugador;
        var cam = Camera.main ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cam && jugador)
        {
            cam.target = jugador.transform;
            cam.Snap();
        }

        if (panel) Destroy(panel);
        Time.timeScale = 1f;
        if (GameManager.Instance) GameManager.Instance.MostrarMision();
    }

    // ------------------------------------------------------------------ interfaz
    void Construir()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        panel = new GameObject("SeleccionPersonaje", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = panel.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = panel.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800, 500);
        scaler.matchWidthOrHeight = 0.5f;

        // velo oscuro sobre el juego
        var velo = Rect(panel.transform, "Velo", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        velo.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.45f);

        // ventana flotante
        float cardW = 200f, gap = 30f;
        float winW = opciones.Length * cardW + (opciones.Length - 1) * gap + 60f;
        var win = Rect(panel.transform, "Ventana", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(winW, 360f));
        win.gameObject.AddComponent<Image>().color = Fondo;
        var outline = win.gameObject.AddComponent<Outline>();
        outline.effectColor = Resalte; outline.effectDistance = new Vector2(3, -3);

        var t = Texto(win, titulo, font, 28, FontStyle.Bold, Color.white);
        t.rectTransform.anchoredPosition = new Vector2(0, 145);
        var sub = Texto(win, ControlesTactiles.EsMovil ? "Toca un personaje para elegirlo" : "Haz clic en un personaje  ·  ← → y Enter  ·  1 / 2", font, 14, FontStyle.Normal, new Color(1, 1, 1, 0.75f));
        sub.rectTransform.anchoredPosition = new Vector2(0, -158);

        marcos = new Image[opciones.Length];
        for (int i = 0; i < opciones.Length; i++)
        {
            int idx = i;
            float x = -winW * 0.5f + 30f + cardW * 0.5f + i * (cardW + gap);
            var card = Rect(win, "Opcion_" + opciones[i].nombre, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, -5f), new Vector2(cardW, 250f));
            marcos[i] = card.gameObject.AddComponent<Image>();
            var btn = card.gameObject.AddComponent<Button>();
            btn.targetGraphic = marcos[i];
            btn.onClick.AddListener(() => Elegir(idx));
            var hover = card.gameObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => Marcar(idx));
            hover.triggers.Add(enter);

            var cara = Rect(card, "Cara", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -95f), new Vector2(cardW - 20f, cardW - 20f));
            var img = cara.gameObject.AddComponent<Image>();
            img.sprite = opciones[i].retrato;
            img.preserveAspect = true;
            img.raycastTarget = false;

            var n = Texto(card, opciones[i].personaje ? Nombre(opciones[i].personaje) : opciones[i].nombre, font, 20, FontStyle.Bold, VerdeOscuro);
            n.rectTransform.anchoredPosition = new Vector2(0, -100f);
            n.raycastTarget = false;
        }
        Marcar(0);
    }

    void Marcar(int i)
    {
        actual = i;
        for (int k = 0; k < marcos.Length; k++)
        {
            marcos[k].color = k == i ? Resalte : Tarjeta;
            marcos[k].transform.localScale = Vector3.one * (k == i ? 1.06f : 1f);
        }
    }

    static RectTransform Rect(Transform parent, string name, Vector2 amin, Vector2 amax, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = amin; rt.anchorMax = amax;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        if (amin == Vector2.zero && amax == Vector2.one) { rt.offsetMin = rt.offsetMax = Vector2.zero; }
        return rt;
    }

    static Text Texto(Transform parent, string s, Font font, int size, FontStyle style, Color color)
    {
        var rt = Rect(parent, "Texto", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 40));
        var t = rt.gameObject.AddComponent<Text>();
        t.text = s; t.font = font; t.fontSize = size; t.fontStyle = style; t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        return t;
    }
}
