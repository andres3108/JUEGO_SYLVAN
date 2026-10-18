using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Rocas que caen del techo en un tramo del recorrido (Zona 3: la cueva, de desdeX a hastaX).
// Mientras el jugador está en el tramo, cada cierto tiempo aparece una sombra en el suelo (aviso) y,
// tras "aviso" segundos, cae una roca justo ahí: casi siempre un poco por delante del jugador y a veces
// encima de él. Si la roca golpea al jugador mientras cae, se pierde (como al tocar un obstáculo);
// al llegar al suelo deja de ser peligrosa y desaparece.
// Las rocas son copias de las plantillas (hijos inactivos con los modelos roca_1..3) que coloca
// GuardianZona3 al crear la escena.
public class RocasQueCaen : MonoBehaviour
{
    public float desdeX = 56f, hastaX = 135f;
    public Vector2 intervalo = new(1.3f, 2.3f);   // segundos entre rocas (al azar)
    public float aviso = 0.8f;                    // segundos que se ve la sombra antes de que caiga la roca
    public float alturaSalida = 6.5f;             // por encima de la pantalla (cámara a y = 2, tamaño 4)
    public float alturaSuelo = 0f;
    public int maxRocas = 3;
    public GameObject[] plantillas;
    public string mensaje = "¡Cuidado! Caen rocas en la cueva";

    float siguiente;
    bool avisado;
    readonly List<GameObject> activas = new();
    static Sprite sombra;

    // sin recarga de dominio al pulsar Play: el sprite estático se vuelve a crear en cada partida
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar() => sombra = null;

    void Start() => siguiente = Time.time + 1.5f;

    void Update()
    {
        var gm = GameManager.Instance;
        if (Time.timeScale == 0f) { siguiente = Time.time + 1f; return; }   // ventana de selección abierta
        if (gm == null || gm.IsGameOver || gm.player == null || plantillas == null || plantillas.Length == 0) return;
        var jugador = gm.player.transform;
        float px = jugador.position.x;
        if (px < desdeX || px > hastaX)
        {
            siguiente = Time.time + 0.6f;   // al entrar al tramo, la primera roca tarda un poco
            return;
        }
        if (!avisado) { avisado = true; gm.Aviso(mensaje); }

        activas.RemoveAll(r => r == null);
        if (Time.time < siguiente || activas.Count >= maxRocas) return;
        siguiente = Time.time + Random.Range(intervalo.x, intervalo.y);

        // hacia dónde va el jugador: la roca cae por delante; a veces cae justo encima
        var rb = gm.player.GetComponent<Rigidbody>();
        float dir = rb && Mathf.Abs(rb.linearVelocity.x) > 0.1f ? Mathf.Sign(rb.linearVelocity.x) : (Random.value < 0.5f ? -1f : 1f);
        float x = px + (Random.value < 0.2f ? Random.Range(-0.4f, 0.4f) : dir * Random.Range(2f, 5.5f));
        StartCoroutine(Caer(Mathf.Clamp(x, desdeX, hastaX)));
    }

    IEnumerator Caer(float x)
    {
        // 1. sombra en el suelo que crece y parpadea: avisa dónde va a caer
        var s = new GameObject("SombraRoca");
        s.transform.position = new Vector3(x, alturaSuelo + 0.03f, 0.4f);
        var sr = s.AddComponent<SpriteRenderer>();
        sr.sprite = Sombra();
        sr.sortingOrder = -5;
        for (float t = 0f; t < aviso; t += Time.deltaTime)
        {
            float k = t / aviso;
            s.transform.localScale = new Vector3(Mathf.Lerp(0.4f, 1.1f, k), Mathf.Lerp(0.4f, 1.1f, k), 1f);
            sr.color = new Color(0f, 0f, 0f, 0.35f + 0.3f * Mathf.Abs(Mathf.Sin(t * 14f)));
            yield return null;
        }

        // 2. la roca cae
        var plantilla = plantillas[Random.Range(0, plantillas.Length)];
        var roca = Instantiate(plantilla, new Vector3(x, alturaSalida, 0f), Random.rotation);
        roca.name = "RocaCayendo";
        roca.SetActive(true);
        var body = roca.AddComponent<Rigidbody>();
        body.mass = 5f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.constraints = RigidbodyConstraints.FreezePositionZ;
        body.linearVelocity = Vector3.down * 2f;
        body.angularVelocity = Random.insideUnitSphere * 4f;
        roca.AddComponent<RocaQueCae>().sombra = s;
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

// Una roca que cae: golpea al jugador (derrota) hasta tocar el suelo; luego se queda quieta y desaparece.
public class RocaQueCae : MonoBehaviour
{
    public GameObject sombra;
    bool peligrosa = true;

    void OnCollisionEnter(Collision c)
    {
        if (!peligrosa) return;
        peligrosa = false;
        if (sombra) Destroy(sombra);
        if (c.collider.GetComponentInParent<GuardianController>() != null)
        {
            if (GameManager.Instance) GameManager.Instance.Lose();
            return;
        }
        // en el suelo: quieta, sin colisión (no estorba al pasar) y se encoge hasta desaparecer
        var rb = GetComponent<Rigidbody>();
        if (rb) { rb.linearVelocity = Vector3.zero; rb.isKinematic = true; }
        foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;
        StartCoroutine(Desaparecer());
    }

    IEnumerator Desaparecer()
    {
        yield return new WaitForSeconds(1f);
        var escala = transform.localScale;
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            transform.localScale = escala * (1f - t / 0.4f);
            yield return null;
        }
        Destroy(gameObject);
    }
}
