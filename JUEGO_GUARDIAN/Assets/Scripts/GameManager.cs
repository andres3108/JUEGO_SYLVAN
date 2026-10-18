using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Reglas del juego (basadas en Prototipo Python/guardianes_prototipo.py):
//   - Recoger todos los residuos de la zona     -> "¡Bosque limpio!"
//   - Cada residuo suma puntos (botella, lata y papel valen distinto)
//   - Chocar con un tronco o caer del recorrido -> "¡Inténtalo de nuevo!"
//   - Al terminar, ESPACIO reinicia la partida.
// Crea su propio HUD al iniciar (imágenes en Assets/Resources/HUD, generadas con Fondos/generar_hud.py):
//   - Barra "Salud del Bosque" (marco de madera con liana) que se llena al limpiar + "Puntaje".
//   - "Contadores de reciclaje": Plástico, Latas y Papel con su icono y cantidad recogida.
//   - Título de la misión y mensajes de fin de partida.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GuardianController player;
    [Tooltip("Texto que se muestra unos segundos al empezar")]
    public string misionTitulo = "Misión 1: Limpia el sendero del bosque";
    public float misionDuracion = 3.5f;
    [Tooltip("Mensaje al recoger todos los residuos de la zona")]
    public string tituloVictoria = "¡Bosque limpio!";

    int total;
    int collected;
    int score;
    bool gameOver;
    Text saludText, scoreText, nombreText, titleText, subtitleText, missionText;
    Image barraRelleno;
    float saludMostrada;
    readonly System.Collections.Generic.Dictionary<string, (Text num, RectTransform caja, int n)> contadores = new();
    GuardianController nombreDe;

    static readonly Color TextoHud = new Color32(232, 244, 168, 255);
    static readonly Color SombraHud = new Color32(20, 34, 14, 230);
    GameObject endPanel;

    static readonly Color Negro = new Color32(30, 30, 30, 255);
    static readonly Color VerdeVictoria = new Color32(34, 139, 34, 255);
    static readonly Color Rojo = new Color32(217, 83, 79, 255);

    public bool IsGameOver => gameOver;
    // salud del bosque mostrada (0..1): sube suavemente al recoger residuos
    public float Salud => saludMostrada;

    void Awake()
    {
        Instance = this;
        if (!player) player = FindAnyObjectByType<GuardianController>();
    }

    void Start()
    {
        total = FindObjectsByType<Residuo>().Length;
        BuildHud();
        if (!GetComponent<FondoContaminacion>()) gameObject.AddComponent<FondoContaminacion>();
        UpdateCounter();
        // si hay pantalla de selección de personaje, el título se muestra al elegir
        var sel = FindAnyObjectByType<SeleccionPersonaje>();
        if (sel == null || sel.Elegido) MostrarMision();
    }

    public void MostrarMision()
    {
        if (string.IsNullOrEmpty(misionTitulo) || missionText == null) return;
        missionText.text = misionTitulo;
        CancelInvoke(nameof(HideMission));
        Invoke(nameof(HideMission), misionDuracion);
    }

    // mensaje corto en el lugar del título de la misión (p. ej. al caer en un hueco)
    public void Aviso(string texto, float segundos = 2.5f)
    {
        if (missionText == null) return;
        missionText.text = texto;
        CancelInvoke(nameof(HideMission));
        Invoke(nameof(HideMission), segundos);
    }

    void HideMission() { if (missionText) missionText.text = ""; }

    void Update()
    {
        // barra de salud: se llena suavemente hasta el valor real
        float objetivo = total > 0 ? (float)collected / total : 0f;
        saludMostrada = Mathf.MoveTowards(saludMostrada, objetivo, Time.deltaTime * 0.8f);
        if (barraRelleno) barraRelleno.fillAmount = saludMostrada;
        // pequeño rebote de la caja del contador que acaba de subir
        foreach (var c in contadores.Values)
            c.caja.localScale = Vector3.Lerp(c.caja.localScale, Vector3.one, Time.deltaTime * 10f);
        if (player != nombreDe && nombreText)
        {
            nombreDe = player;
            nombreText.text = SeleccionPersonaje.Titulo(player ? player.gameObject : null);
        }

        // en el menú final (última zona) el teclado lo manejan los botones: flechas + Enter/Espacio
        var kb = Keyboard.current;
        if (gameOver && !menuFinal && kb != null && (kb.spaceKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame
                                       || kb.enterKey.wasPressedThisFrame))
            Continuar();
    }

    public void VolverAlInicio() => SceneManager.LoadScene(0);   // el menú principal es la escena 0

    // si se ganó y hay otra zona en Build Settings, se pasa a ella; si no, se repite la zona
    public void Continuar()
    {
        int actual = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(ganado && haySiguiente ? actual + 1 : actual);
    }

    public void Collect(Residuo r)
    {
        if (gameOver) return;
        collected++;
        score += r.puntos;
        if (contadores.TryGetValue(r.tipo, out var c))
        {
            contadores[r.tipo] = (c.num, c.caja, c.n + 1);
            c.caja.localScale = Vector3.one * 1.25f;
        }
        UpdateCounter();
        if (collected >= total) Win();
    }

    public void Lose()
    {
        if (gameOver) return;
        EndGame("¡Inténtalo de nuevo!", Rojo, "Presiona ESPACIO para reiniciar");
    }

    bool ganado, haySiguiente, menuFinal;

    void Win()
    {
        ganado = true;
        haySiguiente = SceneManager.GetActiveScene().buildIndex + 1 < SceneManager.sceneCountInBuildSettings;
        EndGame(tituloVictoria, VerdeVictoria, $"Puntos: {score}");
        if (haySiguiente) BotonContinuar();
        else MenuFinal();
        if (player) player.PlayVictory();
    }

    // al completar la última zona: "ZONA 4 PRÓXIMAMENTE" (solo avisa) y "VOLVER AL INICIO" (menú principal)
    void MenuFinal()
    {
        menuFinal = true;
        // título y puntaje más arriba para que quepan los dos botones
        Center(titleText.rectTransform, 100);
        Center(subtitleText.rectTransform, 52);
        // puntaje y avisos en blanco con borde oscuro: se leen sobre cualquier fondo (también la cueva)
        subtitleText.color = Color.white;
        subtitleText.fontStyle = FontStyle.Bold;
        var borde = subtitleText.gameObject.AddComponent<Outline>();
        borde.effectColor = new Color(0.1f, 0.06f, 0.02f, 0.9f);
        borde.effectDistance = new Vector2(2, -2);
        var zona4 = Boton("BotonZona4", "btn_zona4", -40, () =>
        {
            subtitleText.text = "¡La Zona 4 llegará muy pronto!";
            CancelInvoke(nameof(MostrarPuntos));
            Invoke(nameof(MostrarPuntos), 2.5f);
        });
        var inicio = Boton("BotonVolverInicio", "btn_volver_inicio", -137, VolverAlInicio);
        // navegación con flechas arriba/abajo entre los dos botones
        zona4.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = inicio, selectOnUp = inicio };
        inicio.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = zona4, selectOnDown = zona4 };
        EventSystem.current.SetSelectedGameObject(inicio.gameObject);
    }

    void MostrarPuntos() { if (subtitleText) subtitleText.text = $"Puntos: {score}"; }

    // botón de madera "CONTINUAR ZONA N" bajo el puntaje (Assets/Resources/Menu/btn_continuar_N.png;
    // btn_continuar.png es el de la Zona 2); también se puede continuar con ESPACIO o Enter
    void BotonContinuar()
    {
        // número de la zona siguiente sacado del nombre de su escena (Zona2, Zona3...)
        string siguiente = System.IO.Path.GetFileNameWithoutExtension(
            SceneUtility.GetScenePathByBuildIndex(SceneManager.GetActiveScene().buildIndex + 1));
        string n = siguiente.Replace("Zona", "");
        string recurso = Resources.Load<Texture2D>("Menu/btn_continuar_" + n) ? "btn_continuar_" + n : "btn_continuar";
        var btn = Boton("BotonContinuar", recurso, -105, Continuar);
        EventSystem.current.SetSelectedGameObject(btn.gameObject);
    }

    // botón con imagen de Assets/Resources/Menu, con el mismo efecto al pasar el ratón que el menú principal
    Button Boton(string nombre, string recurso, float y, UnityEngine.Events.UnityAction accion)
    {
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        var canvas = endPanel.GetComponentInParent<Canvas>();
        if (!canvas.GetComponent<GraphicRaycaster>()) canvas.gameObject.AddComponent<GraphicRaycaster>();

        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(endPanel.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, y);
        rt.sizeDelta = new Vector2(350, 95);
        var img = go.GetComponent<Image>();
        var tex = Resources.Load<Texture2D>("Menu/" + recurso);
        if (tex) img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        img.preserveAspect = true;
        var btn = go.GetComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(accion);
        go.AddComponent<BotonMenu>();
        return btn;
    }

    void EndGame(string title, Color color, string subtitle)
    {
        gameOver = true;
        if (player) player.controlsEnabled = false;
        titleText.text = title;
        titleText.color = color;
        subtitleText.text = subtitle;
        HideMission();
        endPanel.SetActive(true);
    }

    void UpdateCounter()
    {
        int pct = total > 0 ? Mathf.RoundToInt(100f * collected / total) : 0;
        saludText.text = $"Salud del Bosque: {pct}%";
        scoreText.text = $"Puntaje: {score:N0}   ·   Residuos {collected}/{total}";
        foreach (var c in contadores.Values) c.num.text = c.n.ToString();
    }

    // ------------------------------------------------------------------ HUD
    void BuildHud()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800, 500);
        scaler.matchWidthOrHeight = 0.5f;

        ConstruirBarraSalud(canvasGo.transform, font);
        ConstruirContadores(canvasGo.transform, font);

        missionText = MakeText(canvasGo.transform, font, 30, TextAnchor.MiddleCenter, Negro);
        missionText.fontStyle = FontStyle.Bold;
        var msh = missionText.gameObject.AddComponent<Shadow>();
        msh.effectColor = new Color(1, 1, 1, 0.8f);
        msh.effectDistance = new Vector2(2, -2);
        var mrt = missionText.rectTransform;
        mrt.anchorMin = mrt.anchorMax = mrt.pivot = new Vector2(0.5f, 1);
        mrt.anchoredPosition = new Vector2(0, -130);
        mrt.sizeDelta = new Vector2(760, 50);

        endPanel = new GameObject("FinDePartida", typeof(RectTransform));
        endPanel.transform.SetParent(canvasGo.transform, false);
        var prt = (RectTransform)endPanel.transform;
        prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
        prt.offsetMin = prt.offsetMax = Vector2.zero;

        titleText = MakeText(endPanel.transform, font, 40, TextAnchor.MiddleCenter, Negro);
        titleText.fontStyle = FontStyle.Bold;
        var shadow = titleText.gameObject.AddComponent<Shadow>();
        shadow.effectColor = Negro;
        shadow.effectDistance = new Vector2(2, -2);
        Center(titleText.rectTransform, 30);

        subtitleText = MakeText(endPanel.transform, font, 24, TextAnchor.MiddleCenter, Negro);
        Center(subtitleText.rectTransform, -30);

        endPanel.SetActive(false);
    }

    // ------------------------------------------------------------------ barra "Salud del Bosque"
    void ConstruirBarraSalud(Transform canvas, Font font)
    {
        var raiz = Rect(canvas, "SaludDelBosque", new Vector2(0, 1), new Vector2(12, -8), new Vector2(260, 45));
        Img(raiz, "Marco", "barra_marco", Vector2.zero, new Vector2(260, 45));
        // canal interior: x 49..496, y 31..61 en la imagen de 520 x 90 (a la mitad de tamaño)
        barraRelleno = Img(raiz, "Relleno", "barra_relleno", new Vector2(24.5f, -15.5f), new Vector2(223.5f, 15f));
        barraRelleno.type = Image.Type.Filled;
        barraRelleno.fillMethod = Image.FillMethod.Horizontal;
        barraRelleno.fillOrigin = 0;
        barraRelleno.fillAmount = 0f;
        Img(raiz, "Liana", "barra_liana", Vector2.zero, new Vector2(260, 45));

        saludText = HudText(raiz, font, 18, FontStyle.Bold, new Vector2(14, -44));
        scoreText = HudText(raiz, font, 12, FontStyle.Normal, new Vector2(14, -66));
    }

    // ------------------------------------------------------------------ contadores de reciclaje
    void ConstruirContadores(Transform canvas, Font font)
    {
        var raiz = Rect(canvas, "ContadoresReciclaje", new Vector2(1, 1), new Vector2(-12, -8), new Vector2(200, 118));
        raiz.pivot = new Vector2(1, 1);
        Img(raiz, "Panel", "panel_contadores", Vector2.zero, new Vector2(200, 118));
        var titulo = HudText(raiz, font, 11, FontStyle.Bold, new Vector2(0, -5));
        titulo.text = "CONTADORES DE RECICLAJE";
        titulo.alignment = TextAnchor.UpperCenter;
        titulo.rectTransform.sizeDelta = new Vector2(200, 16);

        var tipos = new[] { ("Botella", "Plástico", "caja_plastico", "icono_botella"),
                            ("Lata", "Latas", "caja_latas", "icono_lata"),
                            ("Papel", "Papel", "caja_papel", "icono_papel") };
        for (int i = 0; i < tipos.Length; i++)
        {
            var (tipo, etiqueta, caja, icono) = tipos[i];
            float x = 12 + i * 60;
            var lab = HudText(raiz, font, 10, FontStyle.Bold, new Vector2(x, -22));
            lab.text = etiqueta;
            lab.alignment = TextAnchor.UpperCenter;
            lab.rectTransform.sizeDelta = new Vector2(56, 14);

            var c = Img(raiz, "Caja_" + tipo, caja, Vector2.zero, new Vector2(56, 56)).rectTransform;
            c.pivot = new Vector2(0.5f, 0.5f);
            c.anchoredPosition = new Vector2(x + 28, -64);
            Img(c, "Icono", icono, new Vector2(5, -9), new Vector2(26, 38));
            var num = HudText(c, font, 17, FontStyle.Bold, new Vector2(29, -15));
            num.alignment = TextAnchor.UpperLeft;
            num.rectTransform.sizeDelta = new Vector2(26, 26);
            num.resizeTextForBestFit = true; num.resizeTextMaxSize = 17; num.resizeTextMinSize = 9;
            num.horizontalOverflow = HorizontalWrapMode.Wrap;
            contadores[tipo] = (num, c, 0);
        }
        nombreText = HudText(raiz, font, 11, FontStyle.Bold, new Vector2(0, -97));
        nombreText.alignment = TextAnchor.UpperCenter;
        nombreText.rectTransform.sizeDelta = new Vector2(200, 16);
    }

    static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static Image Img(Transform parent, string name, string recurso, Vector2 pos, Vector2 size)
    {
        var rt = Rect(parent, name, new Vector2(0, 1), pos, size);
        var img = rt.gameObject.AddComponent<Image>();
        var tex = Resources.Load<Texture2D>("HUD/" + recurso);
        if (tex) img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        img.raycastTarget = false;
        img.preserveAspect = name == "Icono";
        return img;
    }

    static Text HudText(Transform parent, Font font, int size, FontStyle style, Vector2 pos)
    {
        var t = MakeText(parent, font, size, TextAnchor.UpperLeft, TextoHud);
        t.fontStyle = style;
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(260, 24);
        var o = t.gameObject.AddComponent<Outline>();
        o.effectColor = SombraHud;
        o.effectDistance = new Vector2(1.5f, -1.5f);
        t.raycastTarget = false;
        return t;
    }

    static Text MakeText(Transform parent, Font font, int size, TextAnchor anchor, Color color)
    {
        var go = new GameObject("Texto", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = anchor;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        return t;
    }

    static void Center(RectTransform rt, float y)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, y);
        rt.sizeDelta = new Vector2(700, 60);
    }
}
