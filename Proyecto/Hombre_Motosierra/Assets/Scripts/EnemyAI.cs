using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    // -----------------------
    //      REFERENCIAS
    // -----------------------
    [Header("Referencias")]
    [Tooltip("Arrastra el transform del jugador aquí.")]
    public Transform player;

    private NavMeshAgent agent;
    private Animator animator;
    private AudioSource audioSource;

    // -----------------------
    //      AUDIO Y UI
    // -----------------------
    [Header("Audio y UI")]
    public AudioClip detectionSound;
    [Tooltip("Lista opcional de sonidos de detección; se elige uno aleatorio al verte.")]
    public List<AudioClip> detectionSounds;
    [Tooltip("Sonidos de terror aleatorios cuando el enemigo está cerca.")]
    public List<AudioClip> proximitySounds;
    [Tooltip("Distancia máxima para reproducir sonidos de terror.")]
    public float proximitySoundRange = 15f;
    [Tooltip("Tiempo mínimo entre sonidos de terror.")]
    public float proximitySoundCooldown = 6f;
    [Tooltip("Música especial/ambiente que se reproducirá en loop.")]
    public AudioClip backgroundMusic;
    [Range(0f, 1f)] public float backgroundMusicVolume = 0.6f;
    [Header("Audio de pasos")]
    public List<AudioClip> footstepSounds;
    public float footstepInterval = 0.6f;
    public float minFootstepSpeed = 0.1f;
    public float alertMessageDuration = 3f;
    private float alertMessageTimer = 0f;
    private float proximitySoundTimer = 0f;
    private float footstepTimer = 0f;
    private AudioSource musicSource;

    // -----------------------
    //      PATRULLAJE
    // -----------------------
    [Header("Patrullaje")]
    public List<Transform> patrolPoints;
    public float patrolSpeed = 1.5f;
    public float waypointThreshold = 0.5f;
    public float patrolWaitTime = 1.5f;

    private int currentPatrolIndex = 0;
    private bool isPatrolling = false;
    private float currentPatrolTimer = 0f;

    // -----------------------
    //      DETECCIÓN
    // -----------------------
    [Header("Detección de Jugador")]
    public float visionRange = 50f;
    public float visionAngle = 90f;
    public LayerMask obstacleMask;

    // -----------------------
    //      VELOCIDADES
    // -----------------------
    [Header("Velocidades")]
    public float idleSpeed = 0f;

    [Tooltip("Velocidad cuando persigue al jugador (Run Speed). IMPORTANTE: Debe ser MAYOR que patrolSpeed.")]
    public float chaseSpeed = 4.5f; // 🔥 Asegurado que no sea 0

    [Tooltip("Qué tan rápido alcanza su velocidad en persecución.")]
    public float chaseAcceleration = 20f;

    // -----------------------
    //  CONTROL DE ANIMACIÓN
    // -----------------------
    [Header("Control de Animación")]
    [Tooltip("Suaviza la transición entre caminar/correr (Blend Tree).")]
    public float animationSmoothTime = 0.1f;

    private bool isChasing = false;
    private bool isGameOver = false;
    private float currentAnimationSpeed = 0f;

    // ============================================================
    //      START
    // ============================================================
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (agent == null || animator == null || player == null)
        {
            Debug.LogError("Faltan referencias en EnemyAI.");
            enabled = false;
            return;
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = false;
            audioSource.spatialBlend = 1f;
        }

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.volume = backgroundMusicVolume;
        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }

        agent.acceleration = 8f;
        agent.stoppingDistance = waypointThreshold;
        agent.autoBraking = true;

        // Iniciar patrulla si hay puntos
        if (patrolPoints != null && patrolPoints.Count > 0)
        {
            isPatrolling = true;
            agent.speed = patrolSpeed;
            SetNewPatrolDestination();
        }
        else
        {
            agent.speed = idleSpeed;
            agent.isStopped = true;
        }

        // Cargar animación inicial
        currentAnimationSpeed = Mathf.InverseLerp(0f, chaseSpeed, agent.speed);
    }

    // ============================================================
    //      UPDATE
    // ============================================================
    void Update()
    {
        if (isGameOver)
        {
            animator.SetFloat("Speed", 0f);
            agent.speed = 0f;
            return;
        }

        if (proximitySoundTimer > 0f)
            proximitySoundTimer -= Time.deltaTime;

        // DETECCIÓN
        if (CanSeePlayer())
            StartChase();
        else if (isChasing)
            StopChase();

        // MOVIMIENTO
        if (isChasing)
            agent.SetDestination(player.position);
        else if (isPatrolling)
            Patrol();

        TryPlayProximitySound();
        TryPlayFootsteps();

        // TIMER ALERTA
        if (alertMessageTimer > 0)
            alertMessageTimer -= Time.deltaTime;

        // ANIMACIONES
        UpdateAnimations();
    }

    // ============================================================
    //      PATRULLAJE
    // ============================================================
    void SetNewPatrolDestination()
    {
        if (patrolPoints == null || patrolPoints.Count == 0) return;

        currentPatrolIndex %= patrolPoints.Count;
        agent.speed = patrolSpeed;
        agent.stoppingDistance = waypointThreshold;
        agent.isStopped = false;

        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
    }

    void Patrol()
    {
        bool closeEnough = !agent.pathPending &&
                           agent.remainingDistance <= agent.stoppingDistance + 0.1f;

        bool stuck = agent.velocity.sqrMagnitude < 0.01f &&
                     agent.remainingDistance <= agent.stoppingDistance + 0.5f;

        if (agent.isStopped)
        {
            if (currentPatrolTimer > 0f)
            {
                currentPatrolTimer -= Time.deltaTime;
            }

            if (currentPatrolTimer <= 0f)
            {
                agent.isStopped = false;
                SetNewPatrolDestination();
            }

            return;
        }

        if (closeEnough || stuck)
        {
            agent.ResetPath();
            agent.isStopped = true;
            currentPatrolTimer = patrolWaitTime;

            currentAnimationSpeed = 0f;
            animator.SetFloat("Speed", 0f);
        }
    }

    // ============================================================
    //      DETECCIÓN
    // ============================================================
    bool CanSeePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        float halfAngle = visionAngle * 0.5f;
        float distance = Vector3.Distance(transform.position, player.position);

        if (Vector3.Angle(transform.forward, dir) > halfAngle) return false;
        if (distance > visionRange) return false;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, dir, out hit, distance, obstacleMask))
        {
            if (!hit.collider.CompareTag("Player")) return false;
        }

        return true;
    }

    // ============================================================
    //      MANEJO DE ESTADOS
    // ============================================================
    void StartChase()
    {
        if (isChasing) return;

        alertMessageTimer = alertMessageDuration;

        if (audioSource)
        {
            AudioClip clip = GetDetectionClip();
            if (clip != null)
                audioSource.PlayOneShot(clip);
        }

        isPatrolling = false;
        isChasing = true;

        agent.ResetPath();
        agent.speed = chaseSpeed;
        agent.acceleration = chaseAcceleration;
        agent.stoppingDistance = 0.5f;
        agent.isStopped = false;

        currentAnimationSpeed = Mathf.InverseLerp(0f, chaseSpeed, agent.speed);
        animator.SetFloat("Speed", currentAnimationSpeed);
    }

    void StopChase()
    {
        if (!isChasing) return;

        isChasing = false;
        agent.ResetPath();
        agent.acceleration = 8f;

        if (patrolPoints != null && patrolPoints.Count > 0)
        {
            isPatrolling = true;
            currentPatrolTimer = 0f;
            SetNewPatrolDestination();
        }
        else
        {
            isPatrolling = false;
            agent.speed = idleSpeed;
            agent.isStopped = true;
        }

        currentAnimationSpeed = Mathf.InverseLerp(0f, chaseSpeed, agent.speed);
        animator.SetFloat("Speed", currentAnimationSpeed);
    }

    // ============================================================
    //      ANIMACIONES (BLEND TREE)
    // ============================================================
    void UpdateAnimations()
    {
        float speed = agent.velocity.magnitude;

        // Normalizar entre 0 y 1 para el Blend Tree Walk/Run
        float targetSpeed = Mathf.InverseLerp(0f, chaseSpeed, speed);

        currentAnimationSpeed = Mathf.Lerp(
            currentAnimationSpeed,
            targetSpeed,
            Time.deltaTime / animationSmoothTime
        );

        animator.SetFloat("Speed", currentAnimationSpeed);
    }

    // ============================================================
    //      COLISIÓN
    // ============================================================
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isGameOver = true;
            agent.isStopped = true;
        }
    }

    // ============================================================
    //      GUI
    // ============================================================
    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        Rect rect = new Rect(0, 0, Screen.width, Screen.height);

        if (isGameOver)
        {
            style.fontSize = 50;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = Color.red;
            GUI.Label(rect, "¡FIN DEL JUEGO!", style);
        }
        else if (alertMessageTimer > 0)
        {
            style.fontSize = 30;
            style.alignment = TextAnchor.UpperCenter;
            style.normal.textColor = Color.yellow;
            GUI.Label(new Rect(0, 20, Screen.width, 100), "¡TE VIO EL ENEMIGO!", style);
        }
        else
        {
            style.fontSize = 20;
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(10, 10, 400, 50),
                "Velocidad: " + agent.velocity.magnitude.ToString("F2"), style);
        }
    }

    // ============================================================
    //      GIZMOS
    // ============================================================
    private void OnDrawGizmos()
    {
        if (player == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        float half = visionAngle * 0.5f;
        Vector3 forward = transform.forward;
        Vector3 right = Quaternion.Euler(0, half, 0) * forward;
        Vector3 left = Quaternion.Euler(0, -half, 0) * forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, right * visionRange);
        Gizmos.DrawRay(transform.position, left * visionRange);

        if (Application.isPlaying && CanSeePlayer())
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }

    // ============================================================
    //      AUDIO DE PROXIMIDAD
    // ============================================================
    private void TryPlayProximitySound()
    {
        if (player == null || audioSource == null || proximitySounds == null || proximitySounds.Count == 0)
            return;

        if (proximitySoundTimer > 0f)
            return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > proximitySoundRange)
            return;

        AudioClip clip = proximitySounds[Random.Range(0, proximitySounds.Count)];
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
            proximitySoundTimer = proximitySoundCooldown;
        }
    }

    private AudioClip GetDetectionClip()
    {
        if (detectionSounds != null && detectionSounds.Count > 0)
            return detectionSounds[Random.Range(0, detectionSounds.Count)];

        return detectionSound;
    }

    private void TryPlayFootsteps()
    {
        if (audioSource == null || agent == null || footstepSounds == null || footstepSounds.Count == 0)
            return;

        if (footstepTimer > 0f)
            footstepTimer -= Time.deltaTime;

        float speed = agent.velocity.magnitude;
        if (!agent.isOnNavMesh || agent.isStopped || speed < minFootstepSpeed)
            return;

        if (footstepTimer > 0f)
            return;

        AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Count)];
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);

            // Ajusta la cadencia seg�n la velocidad (m�s r�pido al correr)
            float speedFactor = Mathf.Clamp(speed / chaseSpeed, 0.5f, 1.5f);
            footstepTimer = footstepInterval / speedFactor;
        }
    }

    // ============================================================
    //      RUTAS DINÁMICAS
    // ============================================================
    public void SetPatrolRoute(List<Transform> newPatrolPoints)
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        patrolPoints = newPatrolPoints ?? new List<Transform>();

        if (patrolPoints.Count == 0 || agent == null)
        {
            isPatrolling = false;
            isChasing = false;
            agent.ResetPath();
            agent.speed = idleSpeed;
            agent.isStopped = true;
            currentAnimationSpeed = 0f;
            animator.SetFloat("Speed", 0f);
            return;
        }

        isChasing = false;
        isPatrolling = true;
        currentPatrolIndex = 0;
        currentPatrolTimer = 0f;

        agent.ResetPath();
        SetNewPatrolDestination();
    }
}
