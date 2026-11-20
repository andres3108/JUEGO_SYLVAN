using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocasQueCaen : MonoBehaviour
{
    public float desdeX = 56f, hastaX = 135f;
    public Vector2 intervalo = new(1.3f, 2.3f);
    public float aviso = 0.8f;
    public float alturaSalida = 6.5f;
    public float alturaSuelo = 0f;
    public float velocidadCaida = 8.5f;
    public int maxRocas = 3;
    public GameObject[] plantillas;
    public string mensaje = "¡Cuidado! Caen rocas en la cueva";

    float siguiente;
    bool avisado;
    bool estabaEnZona;
    readonly List<GameObject> activas = new();
    static Sprite sombra;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar() => sombra = null;

    void Start() => siguiente = Time.time + 1.2f;

    void Update()
    {
        var gm = GameManager.Instance;
        if (Time.timeScale == 0f) { siguiente = Time.time + 1f; return; }
        if (gm == null || gm.IsGameOver || gm.player == null || plantillas == null || plantillas.Length == 0) return;

        float px = gm.player.transform.position.x;
        bool enZona = px >= desdeX && px <= hastaX;

        if (!enZona)
        {
            estabaEnZona = false;
            siguiente = Time.time + 0.4f;
            return;
        }

        // Detección limpia al entrar a la zona
        if (!estabaEnZona)
        {
            estabaEnZona = true;
            siguiente = Time.time + 0.5f;
            if (!avisado)
            {
                avisado = true;
                gm.Aviso(mensaje);
            }
            return;
        }

        activas.RemoveAll(r => r == null);
        if (Time.time < siguiente || activas.Count >= maxRocas) return;
        siguiente = Time.time + Random.Range(intervalo.x, intervalo.y);

        var rb = gm.player.GetComponent<Rigidbody>();
        float dir = rb && Mathf.Abs(rb.linearVelocity.x) > 0.1f ? Mathf.Sign(rb.linearVelocity.x) : (Random.value < 0.5f ? -1f : 1f);
        float x = px + (Random.value < 0.2f ? Random.Range(-0.4f, 0.4f) : dir * Random.Range(2f, 5.5f));
        StartCoroutine(Caer(Mathf.Clamp(x, desdeX, hastaX)));
    }

    IEnumerator Caer(float x)
    {
        var s = new GameObject("SombraRoca");
        s.transform.position = new Vector3(x, alturaSuelo + 0.03f, 0.4f);
        var sr = s.AddComponent<SpriteRenderer>();
        sr.sprite = Sombra();
        sr.sortingOrder = -5;
        for (float t = 0f; t < aviso; t += Time.deltaTime)
        {
            float k = t / aviso;
            s.transform.localScale = new Vector3(Mathf.Lerp(0.4f, 1.15f, k), Mathf.Lerp(0.4f, 1.15f, k), 1f);
            sr.color = new Color(0f, 0f, 0f, 0.35f + 0.3f * Mathf.Abs(Mathf.Sin(t * 14f)));
            yield return null;
        }

        var plantilla = plantillas[Random.Range(0, plantillas.Length)];
        var roca = Instantiate(plantilla, new Vector3(x, alturaSalida, 0f), Quaternion.identity);
        roca.name = "RocaCayendo";
        roca.SetActive(true);

        var body = roca.AddComponent<Rigidbody>();
        body.mass = 15f;
        body.useGravity = true;
        body.linearDamping = 0f;
        body.angularDamping = 10f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        // Congela rotación en todos los ejes y movimiento en Z para caer recto y sin maromas
        body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        body.linearVelocity = new Vector3(0f, -velocidadCaida, 0f);
        body.angularVelocity = Vector3.zero;

        var cae = roca.AddComponent<RocaQueCae>();
        cae.sombra = s;
        activas.Add(roca);
    }

    static Sprite Sombra()
    {
        if (sombra != null) return sombra;
        const int w = 64, h = 24;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float dx = (x + 0.5f - w / 2f) / (w / 2f), dy = (y + 0.5f - h / 2f) / (h / 2f);
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d) * 1.6f));
        }
        tex.Apply();
        sombra = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 64f);
        return sombra;
    }
}

public class RocaQueCae : MonoBehaviour
{
    public GameObject sombra;
    bool peligrosa = true;

    void OnCollisionEnter(Collision c)
    {
        if (!peligrosa) return;

        var guardian = c.collider.GetComponentInParent<GuardianController>();
        if (guardian != null)
        {
            peligrosa = false;
            if (sombra) Destroy(sombra);
            DesactivarFisicaYColision();
            if (GameManager.Instance != null && !GameManager.Instance.IsGameOver)
            {
                GameManager.Instance.Lose();
            }
            return;
        }

        // Al tocar suelo u otro objeto: deja de ser peligrosa, se queda firme sin maromas y desaparece
        peligrosa = false;
        if (sombra) Destroy(sombra);
        DesactivarFisicaYColision();
        StartCoroutine(Desaparecer());
    }

    void DesactivarFisicaYColision()
    {
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        foreach (var col in GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }
    }

    IEnumerator Desaparecer()
    {
        yield return new WaitForSeconds(0.8f);
        var escala = transform.localScale;
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            transform.localScale = escala * (1f - t / 0.3f);
            yield return null;
        }
        Destroy(gameObject);
    }
}
