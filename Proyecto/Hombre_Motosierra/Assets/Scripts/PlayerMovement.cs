using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class PlayerMovement : MonoBehaviour
{
    // -------------------------
    //  REFERENCIAS
    // -------------------------
    public Transform cam;            // Cámara principal
    public Transform playerModel;    // Modelo 3D del jugador (el que rota + anima)

    // Movimiento
    public float speed = 5f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    // Componentes
    private CharacterController controller;
    private AudioSource audioSource;
    private Animator animator;

    // Estados
    private Vector3 velocity;
    private bool isGrounded;

    // Eventos
    public UnityEvent onJump;

    // Derrota
    private bool lost = false;
    private string loseMessage = "";
    public AudioClip loseSound;

    // -------------------------
    //  INICIO
    // -------------------------
    void Start()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();

        // Obtener Animator del modelo
        if (playerModel != null)
            animator = playerModel.GetComponent<Animator>();
    }

    // -------------------------
    //  UPDATE
    // -------------------------
    void Update()
    {
        // ---- GRAVEDAD ----
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        // ---- ENTRADAS DE MOVIMIENTO ----
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Dirección relativa a la cámara
        Vector3 forward = cam.forward;
        Vector3 right = cam.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 move = forward * z + right * x;

        // -------------------------
        // 🔥 BLEND TREE: mover animSpeed
        // -------------------------
        float animSpeed = move.magnitude;  // 0–1

        if (animator != null)
            animator.SetFloat("moveSpeed", animSpeed, 0.1f, Time.deltaTime);

        // -------------------------
        // 🔥 ROTAR MODELO
        // -------------------------
        if (move.magnitude > 0.1f && playerModel != null)
        {
            Quaternion targetRot = Quaternion.LookRotation(move);
            playerModel.rotation = Quaternion.Slerp(
                playerModel.rotation,
                targetRot,
                10f * Time.deltaTime
            );
        }

        // ---- MOVER PERSONAJE ----
        controller.Move(move * speed * Time.deltaTime);

        // -------------------------
        // 🔥 SALTO
        // -------------------------
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            audioSource?.Play();
            onJump?.Invoke();
        }

        // ---- APLICAR GRAVEDAD ----
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // -------------------------
    //  TRIGGERS DE DERROTA
    // -------------------------
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("pierdes") && !lost)
        {
            lost = true;
            loseMessage = "¡Has perdido!";

            if (audioSource != null && loseSound != null)
                audioSource.PlayOneShot(loseSound);
        }
    }

    // -------------------------
    //  MENSAJE DE DERROTA
    // -------------------------
    void OnGUI()
    {
        if (lost)
        {
            GUIStyle style = new GUIStyle
            {
                fontSize = 40,
                alignment = TextAnchor.MiddleCenter
            };
            style.normal.textColor = Color.red;

            GUI.Label(
                new Rect(Screen.width / 2 - 200, Screen.height / 2 - 25, 400, 50),
                loseMessage,
                style
            );
        }
    }
}
