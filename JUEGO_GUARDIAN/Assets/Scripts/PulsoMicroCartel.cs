using UnityEngine;

public class PulsoMicroCartel : MonoBehaviour
{
    [Header("Flotación Vertical")]
    public float amplitudFlotacion = 0.08f;
    public float velocidadFlotacion = 3f;

    [Header("Respiro de Escala")]
    public float amplitudEscala = 0.04f;
    public float velocidadEscala = 3.5f;

    private Vector3 posicionBase;
    private Vector3 escalaBase;
    private bool inicializado = false;

    void OnEnable()
    {
        if (!inicializado)
        {
            posicionBase = transform.localPosition;
            escalaBase = transform.localScale;
            inicializado = true;
        }
        else
        {
            transform.localPosition = posicionBase;
            transform.localScale = escalaBase;
        }
    }

    void Update()
    {
        float desplazamientoY = Mathf.Sin(Time.time * velocidadFlotacion) * amplitudFlotacion;
        transform.localPosition = posicionBase + new Vector3(0f, desplazamientoY, 0f);

        float factorEscala = 1f + Mathf.Sin(Time.time * velocidadEscala) * amplitudEscala;
        transform.localScale = escalaBase * factorEscala;
    }
}
