using UnityEngine;

public class SimpleCameraFollow : MonoBehaviour
{
    [Tooltip("Drag your player here (Capsule). If empty, the script will try to find an object tagged 'Player'.")]
    public Transform target;
    public float mouseSensitivity = 100f;
    public float distance = 3f;
    public float height = 1.5f;
    public bool lockCursor = true;
    public bool debugMode = false;

    float rotationY = 0f;

    void Start()
    {
        // Si no asignaste target en el inspector, intenta encontrar al jugador por tag
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                target = playerObj.transform;
        }

        if (lockCursor)
            Cursor.lockState = CursorLockMode.Locked;
        else
            Cursor.lockState = CursorLockMode.None;

        if (target == null && debugMode)
            Debug.LogWarning("[SimpleCameraFollow] Target is null. Assign a target in the inspector or tag your player as 'Player'.");
    }

    void LateUpdate()
    {
        if (target == null) return; // evita errores si no hay target

        // Lee el movimiento del ratón (asegúrate de tener Input antiguo activo si no detecta)
        float mouseX = Input.GetAxis("Mouse X");
        rotationY += mouseX * mouseSensitivity * Time.deltaTime;
        Quaternion rotation = Quaternion.Euler(0, rotationY, 0);

        Vector3 desiredPos = target.position - rotation * Vector3.forward * distance + Vector3.up * height;
        transform.position = desiredPos;
        transform.LookAt(target.position + Vector3.up * (height * 0.5f));

        if (debugMode)
        {
            Debug.DrawLine(transform.position, target.position, Color.green);
            Debug.Log($"Cam pos: {transform.position} | Target pos: {target.position} | mouseX: {mouseX}");
        }
    }
}
