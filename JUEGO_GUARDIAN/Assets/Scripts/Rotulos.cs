using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

// Piezas de los mensajes del juego (versión 2: texto claro sobre una franja suave).
// Los rótulos son imágenes con la letra del menú, en Assets/Resources/Mensajes, generadas con
// Fondos/Menu/generar_mensajes.py. Cada texto se busca por su "slug" ("¡Bosque limpio!" -> bosque_limpio);
// si no hay imagen para un texto, se muestra con texto normal para que nunca quede vacío.
public static class Rotulos
{
    // de los píxeles del rótulo (hecho para 1440 px de alto) a unidades del lienzo (500 de alto)
    const float PxAUnidades = 500f / 1440f;

    public static readonly Color Crema = new Color32(246, 231, 196, 255);
    static readonly Color Contorno = new Color32(58, 34, 16, 240);

    public static string Slug(string texto)
    {
        var sb = new StringBuilder();
        foreach (char c in texto.ToLowerInvariant().Normalize(NormalizationForm.FormD))
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return Regex.Replace(sb.ToString(), "[^a-z0-9]+", "_").Trim('_');
    }

    static Sprite Sprite(string recurso)
    {
        var tex = Resources.Load<Texture2D>("Mensajes/" + recurso);
        return tex ? UnityEngine.Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)) : null;
    }

    // rótulo centrado a la altura y (escala 1 = tamaño de diseño); si no hay imagen, texto normal
    public static RectTransform Texto(Transform padre, string texto, float y, int tamTexto, Color colorTexto, float escala = 1f)
    {
        var sp = Sprite(Slug(texto));
        var go = new GameObject("Rotulo_" + Slug(texto), typeof(RectTransform));
        go.transform.SetParent(padre, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, y);
        if (sp)
        {
            var img = go.AddComponent<Image>();
            img.sprite = sp;
            img.raycastTarget = false;
            rt.sizeDelta = new Vector2(sp.rect.width, sp.rect.height) * PxAUnidades * escala;
        }
        else
        {
            var t = Linea(go.transform, texto, 0, tamTexto, colorTexto);
            t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
            rt.sizeDelta = new Vector2(760, tamTexto * 1.6f);
        }
        return rt;
    }

    // línea de texto normal (para números: puntos, residuos) en crema con borde marrón
    public static Text Linea(Transform padre, string texto, float y, int tam, Color color)
    {
        var go = new GameObject("Linea", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(padre, false);
        var t = go.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = texto;
        t.fontSize = tam;
        t.fontStyle = FontStyle.Bold;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.raycastTarget = false;
        var o = go.AddComponent<Outline>();
        o.effectColor = Contorno;
        o.effectDistance = new Vector2(1.5f, -1.5f);
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, y);
        rt.sizeDelta = new Vector2(760, tam * 1.6f);
        return t;
    }

    // banda verde oscura difuminada a todo lo ancho, con filetes dorados, centrada en y
    public static RectTransform Franja(Transform padre, float y, float alto)
    {
        var go = new GameObject("Franja", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, y);
        rt.sizeDelta = new Vector2(0, alto * 1.5f);   // la imagen tiene margen difuminado arriba y abajo
        var img = go.GetComponent<Image>();
        img.sprite = Sprite("franja");
        img.raycastTarget = false;
        if (img.sprite == null) img.color = new Color(0.07f, 0.16f, 0.05f, 0.6f);
        return rt;
    }

    // velo oscuro sobre todo el juego (también bloquea los clics al juego)
    public static Image Velo(Transform padre, float alfa)
    {
        var go = new GameObject("Velo", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = new Color(0.03f, 0.07f, 0.02f, alfa);
        return img;
    }

    public static Sprite Hoja(bool dorada) => Sprite(dorada ? "hoja_dorada" : "hoja_verde");
}

// Hojas verdes y doradas que caen girando por toda la pantalla (confeti de la victoria final).
public class HojasCayendo : MonoBehaviour
{
    public int cantidad = 36;
    RectTransform[] hojas;
    Vector3[] vel;   // x: deriva, y: caída, z: giro

    void Start()
    {
        var area = (RectTransform)transform;
        hojas = new RectTransform[cantidad];
        vel = new Vector3[cantidad];
        for (int i = 0; i < cantidad; i++)
        {
            var go = new GameObject("Hoja", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(area, false);
            var img = go.GetComponent<Image>();
            img.sprite = Rotulos.Hoja(Random.value < 0.35f);
            img.raycastTarget = false;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(24, 11) * Random.Range(0.8f, 1.3f);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(Random.Range(-420f, 420f), Random.Range(-260f, 320f));
            rt.localEulerAngles = new Vector3(0, 0, Random.Range(0f, 360f));
            hojas[i] = rt;
            vel[i] = new Vector3(Random.Range(-18f, 18f), Random.Range(35f, 70f), Random.Range(-160f, 160f));
        }
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        for (int i = 0; i < hojas.Length; i++)
        {
            var p = hojas[i].anchoredPosition;
            p.x += (vel[i].x + Mathf.Sin(Time.unscaledTime * 1.7f + i) * 20f) * dt;
            p.y -= vel[i].y * dt;
            if (p.y < -280f) { p.y = 290f; p.x = Random.Range(-420f, 420f); }   // vuelve a caer desde arriba
            hojas[i].anchoredPosition = p;
            hojas[i].Rotate(0, 0, vel[i].z * dt);
        }
    }
}

// Celebración del personaje al terminar el juego (Zona 3): salta y da una vuelta completa en cada salto,
// sin parar, con un aura verde detrás. Se apoya en el suelo donde esté; desactiva el control y la física.
public class Celebracion : MonoBehaviour
{
    public float altura = 1.1f;      // metros del salto
    public float tiempoSalto = 0.75f;
    public float pausa = 0.2f;       // tiempo en el suelo entre saltos

    Vector3 suelo;
    float yaw0, t;
    Animator anim;
    Transform aura;
    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int GroundedHash = Animator.StringToHash("Grounded");

    void Start()
    {
        var control = GetComponent<GuardianController>();
        if (control) control.enabled = false;
        var rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.None;
        }
        anim = GetComponentInChildren<Animator>();

        // apoyar los saltos en el suelo, aunque el último residuo se recogiera en el aire
        suelo = transform.position;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out var hit, 20f, ~0, QueryTriggerInteraction.Ignore))
            suelo.y = hit.point.y;
        yaw0 = transform.eulerAngles.y;
        CrearAura();
    }

    void CrearAura()
    {
        var tex = Resources.Load<Texture2D>("Menu/aura_guardian");
        if (tex == null) return;
        var go = new GameObject("AuraCelebracion");
        aura = go.transform;
        aura.position = suelo + new Vector3(0f, 0.9f, 0.6f);   // detrás del personaje
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width / 2.8f);
        sr.sortingOrder = -50;   // delante del fondo (-100), detrás del personaje
    }

    void Update()
    {
        t += Time.deltaTime;
        float ciclo = tiempoSalto + pausa;
        int vuelta = Mathf.FloorToInt(t / ciclo);
        float f = t - vuelta * ciclo;
        bool enAire = f < tiempoSalto;
        float u = enAire ? f / tiempoSalto : 1f;

        // salto en parábola y una vuelta completa por salto
        transform.position = suelo + Vector3.up * (enAire ? 4f * altura * u * (1f - u) : 0f);
        transform.rotation = Quaternion.Euler(0f, yaw0 + 360f * (vuelta + Mathf.SmoothStep(0f, 1f, u)), 0f);

        if (anim)
        {
            anim.SetFloat(SpeedHash, 0f);
            anim.SetBool(GroundedHash, !enAire);   // en el aire usa la pose de salto
        }
        if (aura)
        {
            float k = 1f + 0.06f * Mathf.Sin(Time.time * 3f);
            aura.localScale = new Vector3(k, k, 1f);
        }
    }
}
