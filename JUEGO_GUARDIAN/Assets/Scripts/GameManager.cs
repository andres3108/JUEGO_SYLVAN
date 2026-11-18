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
//   - Al perder: REINTENTAR (o ESPACIO). Al ganar una zona: CONTINUAR a la siguiente.
//   - Al terminar la última zona (Zona 3): pantalla de victoria final con el personaje celebrando.
// Crea su propio HUD al iniciar (imágenes en Assets/Resources/HUD, generadas con Fondos/generar_hud.py):
//   - Barra "Salud del Bosque" (marco de madera con liana) que se llena al limpiar + "Puntaje".
//   - "Contadores de reciclaje": Plástico, Latas y Papel con su icono y cantidad recogida.
//   - Título de la misión y mensajes de fin de partida (versión 2: rótulos claros sobre una franja suave,
//     ver Rotulos.cs y Fondos/Menu/generar_mensajes.py).
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GuardianController player;
    [Tooltip("Texto que se muestra unos segundos al empezar")]
    public string misionTitulo = "Misión 1: Limpia el sendero del bosque";
    public float misionDuracion = 3.5f;
    [Tooltip("Mensaje al recoger todos los residuos de la zona")]
    public string tituloVictoria = "¡Bosque limpio!";
    [Tooltip("El fondo empieza oscuro (contaminado) y se aclara al limpiar. Solo en la Zona 1")]
    public bool fondoContaminado = true;

    int total;
    int collected;
    int score;
    bool gameOver;
    Text saludText, scoreText, nombreText, missionText;
    GameObject bannerMision;
    Image barraRelleno;
    float saludMostrada;
    readonly System.Collections.Generic.Dictionary<string, (Text num, RectTransform caja, int n)> contadores = new();
    GuardianController nombreDe;

    static readonly Color TextoHud = new Color32(232, 244, 168, 255);
    static readonly Color SombraHud = new Color32(20, 34, 14, 230);
    GameObject endPanel;

    static readonly Color Oro = new Color32(255, 214, 92, 255);
    static readonly Color Naranja = new Color32(246, 132, 82, 255);
    static readonly Color VerdeClaro = new Color32(170, 226, 110, 255);

    // puntaje sumado de las zonas ganadas en esta partida (se reinicia al empezar la Zona 1)
    public static int PuntajeTotal { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ReiniciarTotal() => PuntajeTotal = 0;

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
        if (SceneManager.GetActiveScene().buildIndex <= 1) PuntajeTotal = 0;   // partida nueva desde la Zona 1
        total = FindObjectsByType<Residuo>().Length;
        BuildHud();
        if (fondoContaminado && !GetComponent<FondoContaminacion>()) gameObject.AddComponent<FondoContaminacion>();
        if (!GetComponent<MenuPausa>()) gameObject.AddComponent<MenuPausa>();   // Escape: menú de pausa
        if (!GetComponent<ControlesTactiles>()) gameObject.AddComponent<ControlesTactiles>();   // botones en el celular
        UpdateCounter();
        // si hay pantalla de selección de personaje, el título se muestra al elegir
        var sel = FindAnyObjectByType<SeleccionPersonaje>();
        if (sel == null || sel.Elegido) MostrarMision();
    }

    // "Misión 1: Limpia el sendero del bosque" -> ("MISIÓN 1", "Limpia el sendero del bosque")
    (string etiqueta, string titulo) PartesMision()
    {
        int i = misionTitulo.IndexOf(':');
        return i < 0 ? ("", misionTitulo.Trim())
                     : (misionTitulo.Substring(0, i).Trim().ToUpperInvariant(), misionTitulo.Substring(i + 1).Trim());
    }

    // título de la misión al empezar: franja arriba con "MISIÓN N" y el título; aparece y se va solo
    public void MostrarMision()
    {
        if (string.IsNullOrEmpty(misionTitulo) || missionText == null) return;
        if (bannerMision) Destroy(bannerMision);
        var (etiqueta, titulo) = PartesMision();
        bannerMision = new GameObject("TituloMision", typeof(RectTransform), typeof(CanvasGroup));
        bannerMision.transform.SetParent(missionText.canvas.transform, false);
        bannerMision.transform.SetSiblingIndex(0);   // detrás de la barra y los contadores
        var rt = (RectTransform)bannerMision.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        Rotulos.Franja(rt, 95, 80);
        if (etiqueta.Length > 0) Rotulos.Texto(rt, etiqueta, 115, 16, VerdeClaro);
        Rotulos.Texto(rt, titulo, 85, 26, Rotulos.Crema);
        StartCoroutine(Desvanecer(bannerMision.GetComponent<CanvasGroup>(), misionDuracion));
    }

    static System.Collections.IEnumerator Desvanecer(CanvasGroup g, float visible)
    {
        for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime) { if (!g) yield break; g.alpha = t / 0.35f; yield return null; }
        if (g) g.alpha = 1f;
        yield return new WaitForSecondsRealtime(visible);
        for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime) { if (!g) yield break; g.alpha = 1f - t / 0.5f; yield return null; }
        if (g) Destroy(g.gameObject);
    }

    // mensaje corto en el lugar del título de la misión (p. ej. al caer en un hueco)
    public void Aviso(string texto, float segundos = 2.5f)
    {
        if (missionText == null) return;
        missionText.text = texto;
        CancelInvoke(nameof(HideMission));
        Invoke(nameof(HideMission), segundos);
    }

    void HideMission()
    {
        if (missionText) missionText.text = "";
    }

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
        // en el celular: tocar la pantalla (medio segundo después de terminar, para no hacerlo sin querer)
        var toque = Touchscreen.current;
        if (gameOver && !menuFinal && toque != null && toque.primaryTouch.press.wasPressedThisFrame
            && Time.unscaledTime - tiempoFin > 0.5f)
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
        EndGame();
        Rotulos.Velo(endPanel.transform, 0.55f);
        Rotulos.Franja(endPanel.transform, 42, 118);
        Rotulos.Texto(endPanel.transform, "¡INTÉNTALO DE NUEVO!", 59, 40, Naranja);
        Rotulos.Texto(endPanel.transform, "El bosque cuenta contigo, Guardián", 14, 18, Rotulos.Crema);
        var b = Boton("BotonReintentar", "btn_reintentar", -76, Continuar, 208);   // también ESPACIO / tocar
        EventSystem.current.SetSelectedGameObject(b.gameObject);
    }

    bool ganado, haySiguiente, menuFinal;
    float tiempoFin;

    void Win()
    {
        ganado = true;
        PuntajeTotal += score;
        haySiguiente = SceneManager.GetActiveScene().buildIndex + 1 < SceneManager.sceneCountInBuildSettings;
        EndGame();
        if (haySiguiente) VictoriaZona();
        else VictoriaFinal();
    }

    // victoria de una zona: "MISIÓN N COMPLETADA", "¡BOSQUE LIMPIO!", residuos y puntos, y CONTINUAR ZONA N+1
    void VictoriaZona()
    {
        var etiqueta = PartesMision().etiqueta;
        Rotulos.Velo(endPanel.transform, 0.45f);
        Rotulos.Franja(endPanel.transform, 42, 132);
        if (etiqueta.Length > 0) Rotulos.Texto(endPanel.transform, etiqueta + " COMPLETADA", 83, 16, VerdeClaro);
        Rotulos.Texto(endPanel.transform, tituloVictoria.ToUpperInvariant(), 45, 40, Oro);
        Rotulos.Linea(endPanel.transform, $"{collected} residuos reciclados  ·  {score} puntos", 3, 17, Rotulos.Crema);
        BotonContinuar();
        if (player) player.PlayVictory();
    }

    // victoria final (última zona): el personaje salta y gira celebrando, caen hojas, "¡MISIÓN CUMPLIDA!",
    // puntaje total de las 3 zonas y VOLVER AL INICIO
    void VictoriaFinal()
    {
        menuFinal = true;
        // pantalla limpia: sin la barra de salud ni los contadores (el título va arriba)
        if (saludText) saludText.transform.parent.gameObject.SetActive(false);
        if (nombreText) nombreText.transform.parent.gameObject.SetActive(false);
        var hojas = new GameObject("HojasCayendo", typeof(RectTransform));
        hojas.transform.SetParent(endPanel.transform, false);
        var hrt = (RectTransform)hojas.transform;
        hrt.anchorMin = Vector2.zero; hrt.anchorMax = Vector2.one; hrt.offsetMin = hrt.offsetMax = Vector2.zero;
        hojas.AddComponent<HojasCayendo>();

        Rotulos.Franja(endPanel.transform, 167, 118);
        Rotulos.Texto(endPanel.transform, "¡MISIÓN CUMPLIDA!", 182, 40, Oro);
        Rotulos.Texto(endPanel.transform, "Eres un verdadero Guardián del Bosque", 143, 18, Rotulos.Crema);
        Rotulos.Linea(endPanel.transform, $"Puntaje total: {PuntajeTotal:N0} puntos", 121, 16, Oro);
        var inicio = Boton("BotonVolverInicio", "btn_volver_inicio", -212, VolverAlInicio, 180);
        EventSystem.current.SetSelectedGameObject(inicio.gameObject);

        if (player) player.gameObject.AddComponent<Celebracion>();
    }

    // botón de madera "CONTINUAR ZONA N" (Assets/Resources/Menu/btn_continuar_N.png;
    // btn_continuar.png es el de la Zona 2); también se puede continuar con ESPACIO o Enter
    void BotonContinuar()
    {
        // número de la zona siguiente sacado del nombre de su escena (Zona2, Zona3...)
        string siguiente = System.IO.Path.GetFileNameWithoutExtension(
            SceneUtility.GetScenePathByBuildIndex(SceneManager.GetActiveScene().buildIndex + 1));
        string n = siguiente.Replace("Zona", "");
        string recurso = Resources.Load<Texture2D>("Menu/btn_continuar_" + n) ? "btn_continuar_" + n : "btn_continuar";
        var btn = Boton("BotonContinuar", recurso, -83, Continuar, 208);
        EventSystem.current.SetSelectedGameObject(btn.gameObject);
    }

    // botón con imagen de Assets/Resources/Menu, con el mismo efecto al pasar el ratón que el menú principal
    Button Boton(string nombre, string recurso, float y, UnityEngine.Events.UnityAction accion, float ancho = 350)
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
        rt.sizeDelta = new Vector2(ancho, ancho * 190f / 700f);   // proporción de los botones (700 x 190)
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

    // fin de partida: congela el control y deja el panel vacío para el mensaje que toque
    void EndGame()
    {
        gameOver = true;
        tiempoFin = Time.unscaledTime;
        if (player) player.controlsEnabled = false;
        HideMission();
        if (bannerMision) Destroy(bannerMision);
        foreach (Transform hijo in endPanel.transform) Destroy(hijo.gameObject);
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

        // avisos cortos (caer al agua, rocas que caen): crema con borde marrón, como los rótulos
        missionText = MakeText(canvasGo.transform, font, 24, TextAnchor.MiddleCenter, Rotulos.Crema);
        missionText.fontStyle = FontStyle.Bold;
        var msh = missionText.gameObject.AddComponent<Outline>();
        msh.effectColor = new Color32(58, 34, 16, 240);
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
}
