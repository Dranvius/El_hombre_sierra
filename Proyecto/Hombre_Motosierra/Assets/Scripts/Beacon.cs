using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Beacon : MonoBehaviour
{
    [Header("Beacon settings")]
    public int pointsOnActivate = 500;
    public float stunDuration = 3f;
    public AudioClip activateSfx;
    public ParticleSystem activateEffect;

    private bool activated = false;

    void Start()
    {
        var col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    // El jugador debe permanecer dentro del trigger y presionar E para activar
    private void OnTriggerStay(Collider other)
    {
        if (activated) return;
        if (!other.CompareTag("Player")) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            Activate(other.gameObject);
            
        }
    }

    public void Activate(GameObject activator)
    {
        if (activated) return;
        activated = true;

        // Reproducir SFX/partículas
        if (activateSfx != null)
            AudioSource.PlayClipAtPoint(activateSfx, transform.position);

        if (activateEffect != null)
            activateEffect.Play();

        // Notificar al GameManager
        if (GameManager.instancia != null)
            GameManager.instancia.OnBeaconActivated(pointsOnActivate, stunDuration);

        // Desactivar visual y colisión para evitar reactivaciones
        var rends = GetComponentsInChildren<Renderer>();
        foreach (var r in rends) r.enabled = false;

        var col = GetComponent<Collider>();
        if (col) col.enabled = false;

        // Destruir objeto tras unos segundos (opcional)
        Destroy(gameObject, 5f);
    }
}