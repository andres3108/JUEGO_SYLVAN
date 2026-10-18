using UnityEngine;

// Residuo coleccionable: gira, flota y brilla con un aura de su color. Se recoge automáticamente
// al tocarlo: suma puntos y lanza el efecto de hojitas, el texto "+puntos" y el sonido (EfectosRecogida).
[RequireComponent(typeof(Collider))]
public class Residuo : MonoBehaviour
{
    [Tooltip("Botella, Lata o Papel")]
    public string tipo = "Botella";
    [Tooltip("Puntos que suma al recogerlo")]
    public int puntos = 10;
    public float spinSpeed = 90f;
    public float bobHeight = 0.08f;
    [Tooltip("Aura de color alrededor del residuo")]
    public bool aura = true;

    Vector3 startPos;
    EfectosRecogida.Aura auraFx;
    bool recogido;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        startPos = transform.position;
    }

    void Start()
    {
        if (aura) auraFx = EfectosRecogida.CrearAura(transform, tipo);
    }

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * 3f) * bobHeight;
    }

    void OnTriggerEnter(Collider other)
    {
        if (recogido || other.GetComponentInParent<GuardianController>() == null) return;
        recogido = true;
        if (GameManager.Instance) GameManager.Instance.Collect(this);
        EfectosRecogida.Recoger(transform.position, tipo, puntos);
        if (auraFx) Destroy(auraFx.gameObject);
        Destroy(gameObject);
    }
}
