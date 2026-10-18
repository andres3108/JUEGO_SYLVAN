using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Control del Guardián del Bosque (sección 6 del documento de diseño):
//   A / D  o  ← / →   : correr a izquierda / derecha
//   Espacio, W o ↑     : saltar (con 0,2 s de tolerancia)
// Se pierde solo al caer por debajo del recorrido.
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class GuardianController : MonoBehaviour
{
    [Header("Movimiento")]
    public float runSpeed = 5f;
    public float jumpForce = 7f;
    [Tooltip("Tiempo extra para poder saltar justo después de dejar el suelo (pág. accesibilidad: 0,2 s)")]
    public float jumpTolerance = 0.2f;
    [Tooltip("Gravedad extra al caer, para que el salto no se sienta flotante")]
    public float fallMultiplier = 2f;

    [Header("Orientación del modelo")]
    [Tooltip("Si el personaje corre de espaldas, cambia este valor a 180")]
    public float modelYawOffset = 0f;

    [Header("Derrota")]
    public float fallLimitY = -5f;

    [HideInInspector] public bool controlsEnabled = true;

    Rigidbody rb;
    CapsuleCollider capsule;
    Animator animator;
    float lastGroundedTime = -10f;
    float lastJumpPressedTime = -10f;
    bool grounded;
    float facing = 1f;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int GroundedHash = Animator.StringToHash("Grounded");
    static readonly int VictoryHash = Animator.StringToHash("Victory");

    public void PlayVictory()
    {
        if (animator) animator.SetTrigger(VictoryHash);
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        animator = GetComponentInChildren<Animator>();

        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        if (animator) animator.applyRootMotion = false;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        float input = 0f;
        if (controlsEnabled)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input += 1f;

            if (kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
                lastJumpPressedTime = Time.time;
        }

        // velocidad horizontal
        Vector3 v = rb.linearVelocity;
        v.x = input * runSpeed;

        // salto con tolerancia (antes y después de tocar el suelo)
        bool canJump = Time.time - lastGroundedTime <= jumpTolerance;
        bool wantsJump = Time.time - lastJumpPressedTime <= jumpTolerance;
        if (canJump && wantsJump)
        {
            v.y = jumpForce;
            lastGroundedTime = -10f;
            lastJumpPressedTime = -10f;
            grounded = false;
        }
        rb.linearVelocity = v;

        // mirar hacia donde corre
        if (Mathf.Abs(input) > 0.01f) facing = Mathf.Sign(input);
        transform.rotation = Quaternion.Euler(0f, 90f * facing + modelYawOffset, 0f);

        if (animator)
        {
            animator.SetFloat(SpeedHash, Mathf.Abs(input));
            animator.SetBool(GroundedHash, grounded);
        }

        if (transform.position.y < fallLimitY)
        {
            if (GameManager.Instance) GameManager.Instance.Lose();
            else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    void FixedUpdate()
    {
        grounded = CheckGround();
        if (grounded) lastGroundedTime = Time.time;

        if (rb.linearVelocity.y < 0f)
            rb.AddForce(Physics.gravity * (fallMultiplier - 1f), ForceMode.Acceleration);
    }

    bool CheckGround()
    {
        if (rb.linearVelocity.y > 0.1f) return false;
        Vector3 feet = transform.position + Vector3.up * (capsule.radius * 0.9f);
        foreach (var col in Physics.OverlapSphere(feet - Vector3.up * 0.08f, capsule.radius * 0.85f, ~0, QueryTriggerInteraction.Ignore))
            if (col != capsule && !col.transform.IsChildOf(transform)) return true;
        return false;
    }
}
