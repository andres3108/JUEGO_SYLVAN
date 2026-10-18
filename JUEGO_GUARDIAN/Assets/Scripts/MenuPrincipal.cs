using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menú principal de Guardianes del Bosque (escena MenuPrincipal, la primera de Build Settings).
//  - JUGAR: carga la primera zona con el guardián confirmado (Sylvan si no se eligió otro).
//  - SELECCIONAR GUARDIÁN: panel con Sylvan y Maya; se resalta el marcado, CONFIRMAR lo guarda y
//    VOLVER regresa sin cambiar la elección anterior.
//  - SALIR: cierra el juego (en el editor detiene el modo Play).
// La interfaz se construye por código con las imágenes de Assets/Resources/Menu (Fondos/Menu/generar_menu.py).
// Ratón, teclado (flechas + Enter, Esc para volver) y mando funcionan a través del EventSystem.
public class MenuPrincipal : MonoBehaviour
{
    [Tooltip("Escena que se carga al pulsar JUGAR")]
    public string escenaJuego = "Zona1";

    // guardián confirmado (se mantiene mientras el juego esté abierto); 0 = Sylvan (predeterminado), 1 = Maya
    public static int GuardianConfirmado { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar() => GuardianConfirmado = 0;

    static readonly string[] Nombres = { "Sylvan", "Maya" };
    static readonly string[] Titulos = { "El Guardián del Bosque", "La Guardiana del Bosque" };
    static readonly string[] Retratos = { "retrato_sylvan", "retrato_maya" };
    static readonly string[] Cuerpos = { "sylvan_menu", "maya_menu" };
    static readonly Color Crema = new Color32(246, 231, 196, 255);
    static readonly Color Cafe = new Color32(58, 34, 16, 255);

    static MenuPrincipal instancia;
    Font fuente;
    RectTransform raiz, aura;
    CanvasGroup grupoMenu, grupoSeleccion, fundido;
    Image personaje;
    Text nombreGuardian;
    Button btnJugar, btnSeleccionar, btnSalir, btnConfirmar;
    Image[] tarjetas = new Image[2];
    Sprite sprTarjeta, sprTarjetaSel;
    int marcado;
    bool ocupado;

    void Awake()
    {
        if (instancia && instancia != this) { Destroy(gameObject); return; }   // una sola instancia del menú
        instancia = this;
        Time.timeScale = 1f;
    }

    void Start()
    {
        fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        sprTarjeta = Spr("tarjeta");
        sprTarjetaSel = Spr("tarjeta_sel");
        Construir();
        MostrarGuardian(GuardianConfirmado);
        StartCoroutine(Fundir(1f, 0f, 0.6f));
        Seleccionar(btnJugar);
    }

    void Update()
    {
        // aura que late y gira despacio detrás del guardián
        if (aura)
        {
            aura.localRotation = Quaternion.Euler(0, 0, Time.unscaledTime * 6f);
            aura.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(Time.unscaledTime * 2f));
        }
        if (personaje)
            personaje.rectTransform.anchoredPosition = new Vector2(-330f, 80f + 6f * Mathf.Sin(Time.unscaledTime * 1.6f));

        var kb = Keyboard.current;
        if (kb == null || ocupado || grupoSeleccion == null || !grupoSeleccion.interactable) return;
        if (kb.escapeKey.wasPressedThisFrame) Volver();
        if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) Marcar(0);
        if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) Marcar(1);
    }

    // ------------------------------------------------------------------ acciones
    void Jugar()
    {
        if (ocupado) return;   // evita que la partida empiece dos veces
        ocupado = true;
        SeleccionPersonaje.Preseleccionar(GuardianConfirmado);
        StartCoroutine(CargarJuego());
    }

    IEnumerator CargarJuego()
    {
        Bloquear(true);
        yield return Fundir(0f, 1f, 0.5f);
        if (Application.CanStreamedLevelBeLoaded(escenaJuego))
            SceneManager.LoadScene(escenaJuego);
        else
        {
            Debug.LogError($"La escena '{escenaJuego}' no está en Build Settings.");
            ocupado = false;
            Bloquear(false);
            yield return Fundir(1f, 0f, 0.3f);
        }
    }

    void AbrirSeleccion()
    {
        if (ocupado) return;
        Marcar(GuardianConfirmado);
        StartCoroutine(Cambiar(grupoMenu, grupoSeleccion));
        Seleccionar(btnConfirmar);
    }

    void Confirmar()
    {
        if (ocupado) return;
        GuardianConfirmado = marcado;
        MostrarGuardian(marcado);
        StartCoroutine(Cambiar(grupoSeleccion, grupoMenu));
        Seleccionar(btnJugar);
    }

    void Volver()
    {
        if (ocupado) return;
        StartCoroutine(Cambiar(grupoSeleccion, grupoMenu));   // la elección anterior no cambia
        Seleccionar(btnSeleccionar);
    }

    void Salir()
    {
        if (ocupado) return;
        ocupado = true;
        StartCoroutine(Cerrar());
    }

    IEnumerator Cerrar()
    {
        Bloquear(true);
        yield return Fundir(0f, 1f, 0.4f);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Marcar(int i)
    {
        marcado = Mathf.Clamp(i, 0, 1);
        for (int k = 0; k < 2; k++)
        {
            tarjetas[k].sprite = k == marcado ? sprTarjetaSel : sprTarjeta;
            tarjetas[k].transform.localScale = Vector3.one * (k == marcado ? 1.06f : 1f);
        }
    }

    void MostrarGuardian(int i)
    {
        personaje.sprite = Spr(Cuerpos[i]);
        personaje.SetNativeSize();
        float alto = 700f;
        personaje.rectTransform.sizeDelta = new Vector2(alto * personaje.sprite.rect.width / personaje.sprite.rect.height, alto);
        nombreGuardian.text = $"{Nombres[i]}: {Titulos[i]}";
    }

    // ------------------------------------------------------------------ transiciones
    IEnumerator Fundir(float desde, float hasta, float dur)
    {
        fundido.blocksRaycasts = true;
        for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
        {
            fundido.alpha = Mathf.Lerp(desde, hasta, t / dur);
            yield return null;
        }
        fundido.alpha = hasta;
        fundido.blocksRaycasts = hasta > 0.01f;
    }

    IEnumerator Cambiar(CanvasGroup sale, CanvasGroup entra)
    {
        sale.interactable = false; sale.blocksRaycasts = false;
        entra.gameObject.SetActive(true);
        for (float t = 0; t < 0.25f; t += Time.unscaledDeltaTime)
        {
            float k = t / 0.25f;
            sale.alpha = 1f - k;
            entra.alpha = k;
            entra.transform.localScale = Vector3.one * Mathf.Lerp(0.95f, 1f, k);
            yield return null;
        }
        sale.alpha = 0f; entra.alpha = 1f; entra.transform.localScale = Vector3.one;
        sale.gameObject.SetActive(sale == grupoMenu);   // el menú queda de fondo; el panel se oculta
        entra.interactable = true; entra.blocksRaycasts = true;
    }

    void Bloquear(bool b)
    {
        grupoMenu.interactable = !b;
        grupoSeleccion.interactable = !b && grupoSeleccion.alpha > 0.5f;
    }

    void Seleccionar(Button b)
    {
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(b ? b.gameObject : null);
    }

    // ------------------------------------------------------------------ construcción de la interfaz
    void Construir()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var canvasGo = new GameObject("MenuUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        raiz = (RectTransform)canvasGo.transform;

        // fondo que siempre cubre la pantalla (sin deformarse)
        var fondo = Img(raiz, "Fondo", "fondo_menu", Vector2.one * 0.5f, Vector2.zero, new Vector2(1920, 1080));
        var fit = fondo.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = 16f / 9f;
        fondo.raycastTarget = false;

        // ---------------- menú principal
        grupoMenu = Grupo("Menu");
        aura = Img(grupoMenu.transform, "Aura", "aura_guardian", new Vector2(1, 0), new Vector2(-330, 430), new Vector2(820, 820)).rectTransform;
        aura.GetComponent<Image>().raycastTarget = false;
        personaje = Img(grupoMenu.transform, "Guardian", Cuerpos[0], new Vector2(1, 0), new Vector2(-330, 80), new Vector2(450, 700));
        personaje.rectTransform.pivot = new Vector2(0.5f, 0f);
        personaje.preserveAspect = true;
        personaje.raycastTarget = false;
        nombreGuardian = Texto(grupoMenu.transform, "", 30, new Vector2(1, 0), new Vector2(-330, 45), new Vector2(700, 50));

        var tit = Img(grupoMenu.transform, "Titulo", "titulo", new Vector2(0, 1), new Vector2(470, -255), new Vector2(820, 470));
        tit.raycastTarget = false;
        btnJugar = Boton(grupoMenu.transform, "Jugar", "btn_jugar", new Vector2(0, 1), new Vector2(430, -585), Jugar);
        btnSeleccionar = Boton(grupoMenu.transform, "SeleccionarGuardian", "btn_seleccionar", new Vector2(0, 1), new Vector2(430, -775), AbrirSeleccion);
        btnSalir = Boton(grupoMenu.transform, "Salir", "btn_salir", new Vector2(0, 1), new Vector2(430, -965), Salir);
        Navegacion(btnJugar, btnSalir, btnSeleccionar);
        Navegacion(btnSeleccionar, btnJugar, btnSalir);
        Navegacion(btnSalir, btnSeleccionar, btnJugar);

        // ---------------- selección de guardián
        grupoSeleccion = Grupo("SeleccionGuardian");
        var velo = new GameObject("Velo", typeof(RectTransform), typeof(Image));
        velo.transform.SetParent(grupoSeleccion.transform, false);
        Estirar((RectTransform)velo.transform);
        velo.GetComponent<Image>().color = new Color(0.02f, 0.06f, 0.02f, 0.6f);
        var panel = Img(grupoSeleccion.transform, "Panel", "panel_seleccion", Vector2.one * 0.5f, new Vector2(0, 30), new Vector2(1300, 820));
        panel.raycastTarget = true;
        var botonesTarjeta = new Button[2];
        for (int i = 0; i < 2; i++)
        {
            int idx = i;
            var t = Img(panel.transform, "Tarjeta_" + Nombres[i], "tarjeta", Vector2.one * 0.5f, new Vector2(i == 0 ? -230 : 230, -10), new Vector2(380, 494));
            tarjetas[i] = t;
            var b = t.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => Marcar(idx));
            var sel = t.gameObject.AddComponent<EventTrigger>();
            var e = new EventTrigger.Entry { eventID = EventTriggerType.Select };
            e.callback.AddListener(_ => Marcar(idx));
            sel.triggers.Add(e);
            botonesTarjeta[i] = b;
            var ret = Img(t.transform, "Retrato", Retratos[i], new Vector2(0.5f, 1), new Vector2(0, -175), new Vector2(300, 300));
            ret.preserveAspect = true;
            ret.raycastTarget = false;
            var n = Texto(t.transform, Nombres[i], 46, new Vector2(0.5f, 0), new Vector2(0, 92), new Vector2(360, 60));
            n.color = Cafe; n.GetComponent<Outline>().effectColor = new Color(1, 1, 1, 0.4f);
            var d = Texto(t.transform, Titulos[i], 22, new Vector2(0.5f, 0), new Vector2(0, 48), new Vector2(360, 40));
            d.color = new Color32(90, 60, 30, 255); d.fontStyle = FontStyle.Bold; Destroy(d.GetComponent<Outline>());
        }
        btnConfirmar = Boton(grupoSeleccion.transform, "Confirmar", "btn_confirmar", Vector2.one * 0.5f, new Vector2(230, -430), Confirmar, 0.75f);
        var btnVolver = Boton(grupoSeleccion.transform, "Volver", "btn_volver", Vector2.one * 0.5f, new Vector2(-230, -430), Volver, 0.75f);
        Navegacion(botonesTarjeta[0], btnVolver, btnVolver, null, botonesTarjeta[1]);
        Navegacion(botonesTarjeta[1], btnConfirmar, btnConfirmar, botonesTarjeta[0], null);
        Navegacion(btnVolver, botonesTarjeta[0], botonesTarjeta[0], null, btnConfirmar);
        Navegacion(btnConfirmar, botonesTarjeta[1], botonesTarjeta[1], btnVolver, null);
        grupoSeleccion.alpha = 0f; grupoSeleccion.interactable = false; grupoSeleccion.blocksRaycasts = false;
        grupoSeleccion.gameObject.SetActive(false);

        // ---------------- fundido a negro (entradas y salidas)
        var f = new GameObject("Fundido", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        f.transform.SetParent(raiz, false);
        Estirar((RectTransform)f.transform);
        f.GetComponent<Image>().color = Color.black;
        fundido = f.GetComponent<CanvasGroup>();
        fundido.alpha = 1f;
    }

    CanvasGroup Grupo(string nombre)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(raiz, false);
        Estirar((RectTransform)go.transform);
        return go.GetComponent<CanvasGroup>();
    }

    Button Boton(Transform padre, string nombre, string recurso, Vector2 ancla, Vector2 pos, UnityEngine.Events.UnityAction accion, float escala = 0.9f)
    {
        var img = Img(padre, "Boton_" + nombre, recurso, ancla, pos, new Vector2(700, 190) * escala);
        var b = img.gameObject.AddComponent<Button>();
        b.transition = Selectable.Transition.None;   // los estados los anima BotonMenu
        b.onClick.AddListener(accion);
        img.gameObject.AddComponent<BotonMenu>();
        return b;
    }

    static void Navegacion(Selectable s, Selectable arriba, Selectable abajo, Selectable izq = null, Selectable der = null)
    {
        s.navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit, selectOnUp = arriba, selectOnDown = abajo, selectOnLeft = izq, selectOnRight = der,
        };
    }

    Image Img(Transform padre, string nombre, string recurso, Vector2 ancla, Vector2 pos, Vector2 tam)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = ancla;
        rt.pivot = Vector2.one * 0.5f;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        var img = go.GetComponent<Image>();
        img.sprite = Spr(recurso);
        return img;
    }

    Text Texto(Transform padre, string s, int tam, Vector2 ancla, Vector2 pos, Vector2 caja)
    {
        var go = new GameObject("Texto", typeof(RectTransform), typeof(Text), typeof(Outline));
        go.transform.SetParent(padre, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = ancla;
        rt.anchoredPosition = pos;
        rt.sizeDelta = caja;
        var t = go.GetComponent<Text>();
        t.text = s; t.font = fuente; t.fontSize = tam; t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter; t.color = Crema; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        var o = go.GetComponent<Outline>();
        o.effectColor = Cafe; o.effectDistance = new Vector2(2, -2);
        return t;
    }

    static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static Sprite Spr(string nombre)
    {
        var tex = Resources.Load<Texture2D>("Menu/" + nombre);
        if (!tex) { Debug.LogWarning("Falta la imagen del menú: Resources/Menu/" + nombre); return null; }
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
    }
}

// Estados visuales de los botones de madera: reposo, resaltado (ratón encima o seleccionado con
// teclado/mando: crece y se ilumina) y pulsado (se encoge y oscurece).
public class BotonMenu : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
    IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    Image img;
    bool encima, seleccionado, pulsado;

    void Awake() => img = GetComponent<Image>();

    public void OnPointerEnter(PointerEventData e) { encima = true; EventSystem.current?.SetSelectedGameObject(gameObject); }
    public void OnPointerExit(PointerEventData e) { encima = false; pulsado = false; }
    public void OnPointerDown(PointerEventData e) => pulsado = true;
    public void OnPointerUp(PointerEventData e) => pulsado = false;
    public void OnSelect(BaseEventData e) => seleccionado = true;
    public void OnDeselect(BaseEventData e) => seleccionado = false;

    void Update()
    {
        bool activo = encima || seleccionado;
        float escala = pulsado ? 0.95f : activo ? 1.07f : 1f;
        Color color = pulsado ? new Color(0.78f, 0.74f, 0.68f) : activo ? Color.white : new Color(0.9f, 0.88f, 0.85f);
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * escala, Time.unscaledDeltaTime * 14f);
        img.color = Color.Lerp(img.color, color, Time.unscaledDeltaTime * 14f);
    }
}
