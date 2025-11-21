using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class PlayerMovement : MonoBehaviour
{
    public Transform cam; // Cámara principal
    public float speed = 5f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private AudioSource audioSource; // AudioSource del jugador

    // Evento que se dispara al saltar
    public UnityEvent onJump;

    // Mensaje de pérdida
    private bool lost = false;
    private string loseMessage = "";

    // Audio de derrota (asignar en el inspector)
    public AudioClip loseSound;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        // Movimiento relativo a la cámara
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 forward = cam.forward;
        Vector3 right = cam.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 move = forward * z + right * x;
        controller.Move(move * speed * Time.deltaTime);

        // Salto
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            // Reproducir sonido de salto
            if (audioSource != null)
                audioSource.Play();

            onJump?.Invoke();
            Debug.Log("¡Saltaste!");
        }

        // Gravedad
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // Detecta triggers de muros invisibles
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("pierdes") && !lost)
        {
            lost = true;
            loseMessage = "¡Has perdido!";

            // Reproducir sonido de derrota
            if (audioSource != null && loseSound != null)
                audioSource.PlayOneShot(loseSound);

            Debug.Log(loseMessage);
        }
    }

    // Mostrar mensaje en pantalla
    void OnGUI()
    {
        if (lost)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 40;
            style.normal.textColor = Color.red;
            style.alignment = TextAnchor.MiddleCenter;

            GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 25, 400, 50), loseMessage, style);
        }
    }
}
