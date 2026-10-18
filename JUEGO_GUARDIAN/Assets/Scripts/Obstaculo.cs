using UnityEngine;

// Tronco u obstáculo estático. Como en el prototipo de Python, tocarlo es derrota.
// Desactiva "Derrota Al Tocar" si el grupo decide que chocar solo detiene al personaje.
[RequireComponent(typeof(Collider))]
public class Obstaculo : MonoBehaviour
{
    public bool derrotaAlTocar = true;

    void OnCollisionEnter(Collision collision)
    {
        if (!derrotaAlTocar) return;
        if (collision.collider.GetComponentInParent<GuardianController>() == null) return;
        if (GameManager.Instance) GameManager.Instance.Lose();
    }
}
