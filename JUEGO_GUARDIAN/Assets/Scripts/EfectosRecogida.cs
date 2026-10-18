using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Efectos visuales y de sonido de los residuos, generados por código (no necesitan archivos):
//  - Aura: halo suave que late + rayos que giran detrás del objeto, del color de cada residuo.
//  - Recogida: explosión de hojitas verdes y chispas del color del residuo, texto "+puntos" que sube
//    y se desvanece, y un sonido de campanita (más agudo o grave según el tipo).
public static class EfectosRecogida
{
    public static Color ColorDe(string tipo) => tipo switch
    {
        "Botella" => new Color(0.30f, 0.78f, 1.00f),   // azul agua
        "Lata"    => new Color(1.00f, 0.72f, 0.20f),   // dorado
        "Papel"   => new Color(0.75f, 1.00f, 0.55f),   // verde claro
        _         => new Color(0.60f, 1.00f, 0.60f),
    };

    static float TonoDe(string tipo) => tipo switch { "Lata" => 1.12f, "Papel" => 0.9f, _ => 1f };

    // ------------------------------------------------------------------ texturas generadas
    static Sprite halo, rayos, hoja, chispa;

    // El proyecto tiene desactivada la recarga de dominio al entrar en Play: las variables estáticas
    // sobreviven entre partidas, pero las texturas creadas en la partida anterior ya están destruidas.
    // Se limpian al empezar cada partida para volver a generarlas.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar()
    {
        halo = rayos = hoja = chispa = null;
        canvas = null; fuente = null; campanita = null;
    }

    static Sprite Halo => halo != null ? halo : halo = Crear(128, (x, y) =>
    {
        float d = Mathf.Sqrt(x * x + y * y);
        return Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f);
    });

    static Sprite Rayos => rayos != null ? rayos : rayos = Crear(128, (x, y) =>
    {
        float d = Mathf.Sqrt(x * x + y * y), a = Mathf.Atan2(y, x);
        float rayo = Mathf.Pow(Mathf.Abs(Mathf.Cos(a * 4f)), 18f) + 0.6f * Mathf.Pow(Mathf.Abs(Mathf.Cos(a * 4f + 0.39f)), 30f);
        return rayo * Mathf.Clamp01(1f - d) * Mathf.Clamp01(d * 4f);
    });

    static Sprite Hoja => hoja != null ? hoja : hoja = Crear(64, (x, y) =>
    {
        // hoja en punta: ancho máximo en el centro, se afina hacia las puntas, con nervio central
        float w = 0.45f * (1f - y * y);
        if (Mathf.Abs(x) > w) return 0f;
        float nervio = Mathf.Abs(x) < 0.04f ? 0.75f : 1f;
        return nervio;
    }, colorear: true);

    static Sprite Chispa => chispa != null ? chispa : chispa = Crear(32, (x, y) =>
    {
        float d = Mathf.Sqrt(x * x + y * y);
        return Mathf.Clamp01(1f - d * 1.2f) + 0.5f * Mathf.Clamp01(1f - Mathf.Abs(x) * 8f) * Mathf.Clamp01(1f - Mathf.Abs(y))
                                         + 0.5f * Mathf.Clamp01(1f - Mathf.Abs(y) * 8f) * Mathf.Clamp01(1f - Mathf.Abs(x));
    });

    static Sprite Crear(int n, System.Func<float, float, float> f, bool colorear = false)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color[n * n];
        for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                float x = (i + 0.5f) / n * 2f - 1f, y = (j + 0.5f) / n * 2f - 1f;
                float v = Mathf.Clamp01(f(x, y));
                px[j * n + i] = colorear ? new Color(v, v, v, v > 0f ? 1f : 0f) : new Color(1, 1, 1, v);
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);   // 1 unidad de ancho
    }

    static SpriteRenderer NuevoSprite(string nombre, Sprite s, Color c, Transform padre = null)
    {
        var go = new GameObject(nombre);
        if (padre) go.transform.SetParent(padre, false);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = s;
        r.color = c;
        return r;
    }

    // ------------------------------------------------------------------ aura
    public static Aura CrearAura(Transform objetivo, string tipo)
    {
        var raiz = new GameObject("Aura_" + objetivo.name);
        var aura = raiz.AddComponent<Aura>();
        aura.objetivo = objetivo;
        aura.color = ColorDe(tipo);
        aura.halo = NuevoSprite("Halo", Halo, aura.color, raiz.transform);
        aura.rayos = NuevoSprite("Rayos", Rayos, aura.color, raiz.transform);
        aura.halo.sortingOrder = -10;
        aura.rayos.sortingOrder = -11;
        return aura;
    }

    public class Aura : MonoBehaviour
    {
        public Transform objetivo;
        public Color color;
        public SpriteRenderer halo, rayos;
        public float tamaño = 1.9f;
        float fase;

        void Start() => fase = Random.value * 10f;

        void LateUpdate()
        {
            if (!objetivo) { Destroy(gameObject); return; }
            // detrás del objeto (la cámara mira hacia +z) y sin girar con él
            transform.position = objetivo.position + new Vector3(0f, 0f, 0.4f);
            float t = Time.time * 2.2f + fase;
            float pulso = 0.5f + 0.5f * Mathf.Sin(t);
            halo.transform.localScale = Vector3.one * tamaño * (0.9f + 0.15f * pulso);
            halo.color = new Color(color.r, color.g, color.b, 0.75f + 0.25f * pulso);
            rayos.transform.localScale = Vector3.one * tamaño * 1.35f;
            rayos.transform.localRotation = Quaternion.Euler(0f, 0f, Time.time * 25f + fase * 30f);
            rayos.color = new Color(color.r, color.g, color.b, 0.5f + 0.3f * (1f - pulso));
        }
    }

    // ------------------------------------------------------------------ recogida
    public static void Recoger(Vector3 pos, string tipo, int puntos)
    {
        var go = new GameObject("Efecto_Recogida");
        go.transform.position = pos;
        go.AddComponent<Explosion>().Iniciar(ColorDe(tipo));
        TextoPuntos(pos, "+" + puntos, ColorDe(tipo));
        Sonar(TonoDe(tipo));
    }

    class Particula
    {
        public SpriteRenderer r, brillo; public Vector3 vel; public float giro, vida, total, tam, fase; public Color c;
    }

    // Explosión bien visible: destello blanco + estallido de rayos del color del residuo, hojitas claras con
    // un halo luminoso detrás (para que se distingan sobre el césped) que suben, se balancean y caen, y chispas.
    class Explosion : MonoBehaviour
    {
        const float Z = -1.5f;                 // por delante del personaje
        readonly List<Particula> ps = new();
        SpriteRenderer nucleo, onda, estallido;
        float t;
        Color color;

        public void Iniciar(Color c)
        {
            color = c;
            onda = NuevoSprite("Onda", Halo, c, transform);
            estallido = NuevoSprite("Estallido", Rayos, Color.Lerp(c, Color.white, 0.3f), transform);
            nucleo = NuevoSprite("Nucleo", Halo, Color.white, transform);
            int o = 20;
            foreach (var r in new[] { onda, estallido, nucleo })
            {
                r.sortingOrder = o++;
                r.transform.localPosition = new Vector3(0, 0, Z);
            }
            for (int i = 0; i < 22; i++)   // hojitas
            {
                var verde = Color.Lerp(new Color(0.55f, 0.95f, 0.3f), new Color(0.95f, 1f, 0.45f), Random.value);
                Nueva(Hoja, verde, Random.Range(3f, 5.5f), Random.Range(0.42f, 0.68f), Random.Range(1.6f, 2.2f), true);
            }
            for (int i = 0; i < 16; i++)   // chispas del color del residuo
                Nueva(Chispa, Color.Lerp(c, Color.white, 0.45f), Random.Range(2f, 4f), Random.Range(0.28f, 0.45f), Random.Range(0.8f, 1.3f), false);
        }

        void Nueva(Sprite s, Color c, float rapidez, float tam, float vida, bool hoja)
        {
            var r = NuevoSprite("Particula", s, c, transform);
            r.sortingOrder = 31;
            float a = Random.Range(20f, 160f) * Mathf.Deg2Rad;     // hacia arriba y a los lados
            var start = new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.15f, 0.25f), Z);
            r.transform.localPosition = start;
            r.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            r.transform.localScale = Vector3.one * tam;
            SpriteRenderer brillo = null;
            if (hoja)   // halo luminoso detrás de cada hoja
            {
                brillo = NuevoSprite("Brillo", Halo, new Color(color.r, color.g, color.b, 0.7f), transform);
                brillo.sortingOrder = 30;
                brillo.transform.localPosition = start + new Vector3(0, 0, 0.05f);
                brillo.transform.localScale = Vector3.one * tam * 2.2f;
            }
            ps.Add(new Particula
            {
                r = r, brillo = brillo, c = c, tam = tam, vida = vida, total = vida, fase = Random.value * 6f,
                vel = new Vector3(Mathf.Cos(a), Mathf.Sin(a) * 1.3f, 0f) * rapidez,
                giro = hoja ? Random.Range(-360f, 360f) : 0f,
            });
        }

        void Update()
        {
            float dt = Time.deltaTime;
            t += dt;
            // destello: núcleo blanco breve, onda de color y estallido de rayos que se expanden
            float k1 = Mathf.Clamp01(t / 0.25f), k2 = Mathf.Clamp01(t / 0.6f);
            nucleo.transform.localScale = Vector3.one * Mathf.Lerp(1.2f, 2.2f, k1);
            nucleo.color = new Color(1, 1, 1, 1f - k1);
            onda.transform.localScale = Vector3.one * Mathf.Lerp(1f, 5f, k2);
            onda.color = new Color(color.r, color.g, color.b, 0.9f * (1f - k2));
            estallido.transform.localScale = Vector3.one * Mathf.Lerp(1.5f, 4.5f, k2);
            estallido.transform.localRotation = Quaternion.Euler(0, 0, t * 90f);
            estallido.color = new Color(estallido.color.r, estallido.color.g, estallido.color.b, 1f - k2);

            foreach (var p in ps)
            {
                if (p.vida <= 0f) continue;
                p.vida -= dt;
                p.vel += Vector3.down * 3.2f * dt;                       // caen despacio, como hojas
                p.vel *= 1f - 1.6f * dt;                                 // resistencia del aire
                var sway = p.brillo ? new Vector3(Mathf.Sin(t * 5f + p.fase) * 0.8f, 0, 0) : Vector3.zero;   // balanceo
                p.r.transform.localPosition += (p.vel + sway) * dt;
                p.r.transform.localRotation *= Quaternion.Euler(0, 0, p.giro * dt);
                float a = Mathf.Clamp01(p.vida / p.total * 2f);
                float pop = Mathf.Clamp01((p.total - p.vida) / 0.12f);    // aparecen creciendo
                p.r.transform.localScale = Vector3.one * p.tam * pop;
                p.r.color = new Color(p.c.r, p.c.g, p.c.b, a);
                if (p.brillo)
                {
                    p.brillo.transform.localPosition = p.r.transform.localPosition + new Vector3(0, 0, 0.05f);
                    p.brillo.color = new Color(color.r, color.g, color.b, 0.65f * a);
                }
                if (p.vida <= 0f) { p.r.enabled = false; if (p.brillo) p.brillo.enabled = false; }
            }
            if (t > 2.4f) Destroy(gameObject);
        }
    }

    // ------------------------------------------------------------------ texto "+puntos"
    static Canvas canvas;

    static void TextoPuntos(Vector3 pos, string s, Color c)
    {
        var cam = Camera.main;
        if (!cam) return;
        if (!canvas)
        {
            var go = new GameObject("Efectos_UI", typeof(Canvas), typeof(CanvasScaler));
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(800, 500);
            sc.matchWidthOrHeight = 0.5f;
        }
        var t = new GameObject("Puntos", typeof(RectTransform), typeof(Text));
        t.transform.SetParent(canvas.transform, false);
        var txt = t.GetComponent<Text>();
        txt.text = s;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 46;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.color = Color.Lerp(c, Color.white, 0.35f);
        var o = t.AddComponent<Outline>();
        o.effectColor = new Color(0.1f, 0.25f, 0.08f, 0.9f);
        o.effectDistance = new Vector2(2, -2);
        t.AddComponent<TextoFlotante>().Iniciar(pos + Vector3.up * 0.5f, cam);
    }

    class TextoFlotante : MonoBehaviour
    {
        Vector3 mundo; Camera cam; Text txt; float t; Color c0;

        public void Iniciar(Vector3 pos, Camera camara)
        {
            mundo = pos; cam = camara; txt = GetComponent<Text>(); c0 = txt.color;
            Update();
        }

        void Update()
        {
            t += Time.deltaTime;
            var rt = (RectTransform)transform;
            Vector3 sp = cam.WorldToScreenPoint(mundo + Vector3.up * t * 1.2f);
            var canvasRt = (RectTransform)rt.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, sp, null, out var local);
            rt.anchoredPosition = local;
            float pop = t < 0.15f ? Mathf.Lerp(0.5f, 1.25f, t / 0.15f) : Mathf.Lerp(1.25f, 1f, (t - 0.15f) / 0.2f);
            rt.localScale = Vector3.one * pop;
            txt.color = new Color(c0.r, c0.g, c0.b, Mathf.Clamp01((1.4f - t) / 0.4f));
            if (t > 1.4f) Destroy(gameObject);
        }
    }

    // ------------------------------------------------------------------ sonido de campanita
    static AudioClip campanita;
    static AudioSource fuente;

    static void Sonar(float tono)
    {
        if (campanita == null)
        {
            const int rate = 44100;
            float dur = 0.45f;
            int n = (int)(rate * dur);
            var data = new float[n];
            float[] notas = { 1046.5f, 1318.5f, 1568f };              // Do6 - Mi6 - Sol6 (arpegio)
            for (int i = 0; i < n; i++)
            {
                float time = (float)i / rate, v = 0f;
                for (int k = 0; k < notas.Length; k++)
                {
                    float t0 = k * 0.06f;
                    if (time < t0) continue;
                    float tt = time - t0, env = Mathf.Exp(-tt * 9f) * Mathf.Clamp01(tt * 400f);
                    v += env * (Mathf.Sin(2 * Mathf.PI * notas[k] * tt) + 0.3f * Mathf.Sin(2 * Mathf.PI * notas[k] * 2f * tt));
                }
                data[i] = v * 0.22f;
            }
            campanita = AudioClip.Create("Campanita", n, 1, rate, false);
            campanita.SetData(data, 0);
        }
        if (!fuente)
        {
            var go = new GameObject("Sonidos_Recogida");
            fuente = go.AddComponent<AudioSource>();
            fuente.playOnAwake = false;
            fuente.spatialBlend = 0f;
        }
        fuente.pitch = tono * Random.Range(0.97f, 1.03f);
        fuente.PlayOneShot(campanita, 0.9f);
    }
}
