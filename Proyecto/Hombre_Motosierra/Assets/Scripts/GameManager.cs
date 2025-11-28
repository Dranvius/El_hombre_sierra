using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class GameManager : MonoBehaviour
{
    // ! Gestion de la creacion y destruccion de los mapas
    [SerializeField] private GameObject[] arrayMapas;
    [SerializeField] private GameObject mapaFinalPrefab;
    [SerializeField] private int mapasAntesDeFinal = 5;
    [SerializeField] private List<GameObject> listaMapasAlrededor;
    [SerializeField] private GameObject mapaActual;
    [SerializeField] private Vector3 posicionMapaActual;
    [SerializeField] private EnemyTeleportManager enemyTeleporter;
    [Header("Respawn")]
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private Transform enemySpawnPoint;
    [SerializeField] private EnemyAI enemyRef;
    [SerializeField] private float respawnDelay = 1f;


    private int mapaQueTocaPoner;
    private int mapasGenerados;
    private bool finalGenerado;
    private Transform playerTransformCache;

    public static GameManager instancia;

    // ---------- NUEVAS PROPIEDADES PARA UI: Score, Timer, Vidas, Faros ----------
    [Header("UI - Score/Timer/Vidas/Faros (opcional)")]
    public Text livesLabel;
    public Text livesText;
    public Text scoreLabel;
    public Text scoreText;
    public Text timerText;
    public Text beaconsLabel;
    public Text beaconsText;

    public TextMeshProUGUI livesLabelTMP;
    public TextMeshProUGUI livesTextTMP;
    public TextMeshProUGUI scoreLabelTMP;
    public TextMeshProUGUI scoreTextTMP;
    public TextMeshProUGUI timerTextTMP;
    public TextMeshProUGUI beaconsLabelTMP;
    public TextMeshProUGUI beaconsTextTMP;

    [Header("Duración de la sesión (segundos)")]
    public float defaultSessionDuration = 120f; // valor por defecto en inspector

    [Header("Vidas")]
    [Tooltip("Vidas iniciales del jugador")]
    public int startingLives = 3;
    private int lives = 3;

    // ---------- Score progression settings ----------
    [Header("Score progression")]
    [Tooltip("Puntos por segundo iniciales")]
    public float initialPointsPerSecond = 50f;
    [Tooltip("Cada cuánto (s) aumenta la velocidad de ganancia")]
    public float rateIncreaseInterval = 30f;
    [Tooltip("Multiplicador aplicado a pointsPerSecond cada interval")]
    public float rateMultiplier = 1.1f;

    // ---------- Beacon settings ----------
    [Header("Beacons")]
    [Tooltip("Prefab del faro (Beacon) a spawnear en cada mapa")]
    public GameObject beaconPrefab;
    [Tooltip("Cuántos faros spawnear por mapa instanciado")]
    public int beaconsPerMap = 1;

    private float remainingTime = 0f;
    private bool sessionActive = false;
    private bool sessionOver = false;

    private int score = 0;
    private float scoreAccumulator = 0f; // acumula puntos en float antes de convertir a int

    // runtime control of rate
    private float pointsPerSecond;
    private float elapsedTime = 0f;
    private float lastIncreaseTime = 0f;

    // Beacon counters
    private int beaconsTotal = 0;
    private int beaconsActivated = 0;

    // CICLO DE VIDA DEL GAME MANAGER
    private void Awake()
    {
        instancia = this;
    }

    private void Start()
    {
        // Initialize map info
        EnsureMapaActual();
        mapaQueTocaPoner = -1;
        mapasGenerados = 0;
        finalGenerado = false;

        if (listaMapasAlrededor == null)
            listaMapasAlrededor = new List<GameObject>();

        if (mapaActual != null && !listaMapasAlrededor.Contains(mapaActual))
            listaMapasAlrededor.Add(mapaActual);
        posicionMapaActual = mapaActual != null ? mapaActual.transform.position : posicionMapaActual;

        // inicializar vidas
        lives = Mathf.Max(0, startingLives);

        // Asegurar UI: si no hay referencias, intentar encontrar o crear elementos UI
        EnsureUI();

        // Iniciar sesion siempre al cargar para sumar score con el tiempo
        StartGameSession(GameSession.sessionDuration > 0 ? GameSession.sessionDuration : defaultSessionDuration);
        GameSession.Reset();
    }

    private void Update()
    {
        if (!sessionActive || sessionOver) return;

        // Tiempo
        remainingTime -= Time.deltaTime;
        elapsedTime += Time.deltaTime;

        // Incrementar puntosPerSecond periódicamente
        if (elapsedTime - lastIncreaseTime >= rateIncreaseInterval)
        {
            pointsPerSecond *= rateMultiplier;
            lastIncreaseTime = elapsedTime;
        }

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            EndGame(false); // false = tiempo agotado
        }

        // Actualizar score: acumulamos puntos según pointsPerSecond
        scoreAccumulator += Time.deltaTime * pointsPerSecond;
        if (scoreAccumulator >= 1f)
        {
            int add = Mathf.FloorToInt(scoreAccumulator);
            score += add;
            scoreAccumulator -= add;
        }

        UpdateUI();
    }

    // ---------- METHODS para SESSION ----------
    public void StartGameSession(float duration)
    {
        remainingTime = duration > 0f ? duration : defaultSessionDuration;
        score = 0;
        scoreAccumulator = 0f;
        sessionActive = true;
        sessionOver = false;

        // inicializar rate
        pointsPerSecond = Mathf.Max(1f, initialPointsPerSecond);
        elapsedTime = 0f;
        lastIncreaseTime = 0f;

        // reset beacons counters
        beaconsTotal = 0;
        beaconsActivated = 0;

        // Asegurar que el tiempo del juego está en 1 (por si venimos del menú con pause)
        Time.timeScale = 1f;

        // reset vidas si corresponde
        lives = Mathf.Max(0, startingLives);

        UpdateUI();
        Debug.Log($"[GameManager] Sesión iniciada: {remainingTime} s - initial PPS: {pointsPerSecond} - Vidas: {lives}");
    }

    /// <summary>
    /// Pierde una vida. Devuelve true si la partida termina (vidas <= 0).
    /// </summary>
    public bool LoseLife()
    {
        if (sessionOver) return true;

        lives = Mathf.Max(0, lives - 1);
        UpdateUI();

        if (lives <= 0)
        {
            Debug.Log("[GameManager] No quedan vidas. Fin de la partida.");
            EndGame(true);
            return true;
        }
        else
        {
            Debug.Log($"[GameManager] Vida perdida. Vidas restantes: {lives}");
            StartCoroutine(RespawnAfterDelay());
            return false;
        }
    }

    /// <summary>
    /// Añade puntos al score de forma segura desde otros scripts.
    /// </summary>
    public void AddScore(int points)
    {
        if (points <= 0) return;
        score += points;
        UpdateUI();
    }

    /// <summary>
    /// Llamado por un Beacon cuando se activa.
    /// </summary>
    public void OnBeaconActivated(int points, float stunDuration)
    {
        beaconsActivated++;
        AddScore(points);

        // Aturdir a todos los enemigos en escena
        var enemies = Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        foreach (var e in enemies)
        {
            e.Stun(stunDuration);
        }

        UpdateUI();

        // si completamos todos los beacons, recompensa
        if (beaconsActivated >= beaconsTotal && beaconsTotal > 0)
        {
            Debug.Log("[GameManager] Todos los faros activados! Recompensa aplicada.");
            remainingTime += 20f; // recompensa: +20s
            UpdateUI();
        }
    }

    /// <summary>
    /// Termina la sesión. Si lostByEnemy==true indica que perdió por enemigo.
    /// </summary>
        public void EndGame(bool lostByEnemy)
    {
        if (sessionOver) return;

        sessionActive = false;
        sessionOver = true;

        // Pausar el juego
        Time.timeScale = 0f;

        // Mostrar resultado final
        string reason = lostByEnemy ? "Has sido atrapado" : "Se acabo el tiempo";
        Debug.Log($"[GameManager] FIN DE LA PARTIDA: {reason}. Score final: {score:N0}");

        // Actualizar textos finales (si estan asignados)
        UpdateUI();

        // Aqu� podr�as mostrar pantalla de derrota; dejamos el juego pausado
    }

    // Overload para llamadas externas donde no sabemos la razón
    public void EndGame()
    {
        EndGame(true);
    }

    private void UpdateUI()
    {
        // Lives
        if (livesLabel != null) livesLabel.text = "Vidas";
        if (livesLabelTMP != null) livesLabelTMP.text = "Vidas";

        string livesStr = lives.ToString();
        if (livesText != null) livesText.text = livesStr;
        if (livesTextTMP != null) livesTextTMP.text = livesStr;

        // Beacons (Faros)
        if (beaconsLabel != null) beaconsLabel.text = "Faros";
        if (beaconsLabelTMP != null) beaconsLabelTMP.text = "Faros";

        string beaconsStr = $"{beaconsActivated}/{beaconsTotal}";
        if (beaconsText != null) beaconsText.text = beaconsStr;
        if (beaconsTextTMP != null) beaconsTextTMP.text = beaconsStr;

        // Score label y número
        if (scoreLabel != null) scoreLabel.text = "Puntuación";
        if (scoreLabelTMP != null) scoreLabelTMP.text = "Puntuación";

        string scoreStr = score.ToString("N0"); // separador de miles
        if (scoreText != null) scoreText.text = scoreStr;
        if (scoreTextTMP != null) scoreTextTMP.text = scoreStr;

        // Timer: mostrar mm:ss
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        string timerStr = string.Format("{0:00}:{1:00}", minutes, seconds);

        if (timerText != null) timerText.text = timerStr;
        if (timerTextTMP != null) timerTextTMP.text = timerStr;
    }

    // Intenta encontrar elementos UI o los crea si no existen
    private void EnsureUI()
    {
        // Si ya hay referencias explícitas asignadas, nada que hacer
        if ((scoreText != null || scoreTextTMP != null) && (timerText != null || timerTextTMP != null) && (livesText != null || livesTextTMP != null) && (beaconsText != null || beaconsTextTMP != null))
            return;

        // Intentar encontrar por nombre en la escena (LivesLabel, LivesText, ScoreLabel, ScoreText, TimerText, BeaconsLabel, BeaconsText, TMP variantes)
        if (livesLabel == null)
        {
            var go = GameObject.Find("LivesLabel");
            if (go != null) livesLabel = go.GetComponent<Text>();
        }
        if (livesText == null)
        {
            var go = GameObject.Find("LivesText");
            if (go != null) livesText = go.GetComponent<Text>();
        }
        if (scoreLabel == null)
        {
            var go = GameObject.Find("ScoreLabel");
            if (go != null) scoreLabel = go.GetComponent<Text>();
        }
        if (scoreText == null)
        {
            var go = GameObject.Find("ScoreText");
            if (go != null) scoreText = go.GetComponent<Text>();
        }
        if (timerText == null)
        {
            var go = GameObject.Find("TimerText");
            if (go != null) timerText = go.GetComponent<Text>();
        }
        if (beaconsLabel == null)
        {
            var go = GameObject.Find("BeaconsLabel");
            if (go != null) beaconsLabel = go.GetComponent<Text>();
        }
        if (beaconsText == null)
        {
            var go = GameObject.Find("BeaconsText");
            if (go != null) beaconsText = go.GetComponent<Text>();
        }

        if (livesLabelTMP == null)
        {
            var go = GameObject.Find("LivesLabelTMP");
            if (go != null) livesLabelTMP = go.GetComponent<TextMeshProUGUI>();
        }
        if (livesTextTMP == null)
        {
            var go = GameObject.Find("LivesTextTMP");
            if (go != null) livesTextTMP = go.GetComponent<TextMeshProUGUI>();
        }
        if (scoreLabelTMP == null)
        {
            var go = GameObject.Find("ScoreLabelTMP");
            if (go != null) scoreLabelTMP = go.GetComponent<TextMeshProUGUI>();
        }
        if (scoreTextTMP == null)
        {
            var go = GameObject.Find("ScoreTextTMP");
            if (go != null) scoreTextTMP = go.GetComponent<TextMeshProUGUI>();
        }
        if (timerTextTMP == null)
        {
            var go = GameObject.Find("TimerTextTMP");
            if (go != null) timerTextTMP = go.GetComponent<TextMeshProUGUI>();
        }
        if (beaconsLabelTMP == null)
        {
            var go = GameObject.Find("BeaconsLabelTMP");
            if (go != null) beaconsLabelTMP = go.GetComponent<TextMeshProUGUI>();
        }
        if (beaconsTextTMP == null)
        {
            var go = GameObject.Find("BeaconsTextTMP");
            if (go != null) beaconsTextTMP = go.GetComponent<TextMeshProUGUI>();
        }

        // Si ya encontró todos, ajustar tamaño y salir
        if ((scoreText != null || scoreTextTMP != null) && (timerText != null || timerTextTMP != null) && (livesText != null || livesTextTMP != null) && (beaconsText != null || beaconsTextTMP != null))
        {
            if (livesLabel != null) livesLabel.fontSize = 22;
            if (livesText != null) livesText.fontSize = 28;
            if (scoreLabel != null) scoreLabel.fontSize = 28;
            if (scoreText != null) scoreText.fontSize = 72;
            if (timerText != null) timerText.fontSize = 56;
            if (beaconsLabel != null) beaconsLabel.fontSize = 22;
            if (beaconsText != null) beaconsText.fontSize = 20;

            if (livesLabelTMP != null) livesLabelTMP.fontSize = 22;
            if (livesTextTMP != null) livesTextTMP.fontSize = 28;
            if (scoreLabelTMP != null) scoreLabelTMP.fontSize = 28;
            if (scoreTextTMP != null) scoreTextTMP.fontSize = 72;
            if (timerTextTMP != null) timerTextTMP.fontSize = 56;
            if (beaconsLabelTMP != null) beaconsLabelTMP.fontSize = 22;
            if (beaconsTextTMP != null) beaconsTextTMP.fontSize = 20;

            return;
        }

        // Si no hay ninguno, crear un Canvas con los textos (UI Text)
        CreateDefaultUI();
    }

    private void CreateDefaultUI()
    {
        // Revisar si ya existe un Canvas en escena
        Canvas existingCanvas = Object.FindFirstObjectByType<Canvas>();
        GameObject canvasGO;
        Canvas canvas;
        if (existingCanvas != null && existingCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            canvas = existingCanvas;
            canvasGO = existingCanvas.gameObject;
        }
        else
        {
            canvasGO = new GameObject("GameUI_Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Fuente incorporada recomendada en versiones recientes de Unity
        Font arial = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (arial == null)
        {
            // Fallback a la fuente del GUI.skin si no se encuentra LegacyRuntime
            arial = GUI.skin != null ? GUI.skin.font : null;
            if (arial == null)
                Debug.LogWarning("[GameManager] No se encontró LegacyRuntime.ttf ni GUI.skin.font. Los textos podrían no mostrarse correctamente.");
        }

        // Lives Label (arriba derecha)
        if (livesLabel == null && livesLabelTMP == null)
        {
            GameObject labelGO = new GameObject("LivesLabel");
            labelGO.transform.SetParent(canvasGO.transform);
            var txt = labelGO.AddComponent<Text>();
            txt.font = arial;
            txt.fontSize = 22;
            txt.alignment = TextAnchor.UpperRight;
            txt.color = Color.white;
            txt.raycastTarget = false;
            RectTransform rlabel = txt.GetComponent<RectTransform>();
            rlabel.anchorMin = new Vector2(1, 1);
            rlabel.anchorMax = new Vector2(1, 1);
            rlabel.pivot = new Vector2(1, 1);
            rlabel.anchoredPosition = new Vector2(-10, -10);
            rlabel.sizeDelta = new Vector2(300, 30);

            livesLabel = txt;
        }

        // Lives Text
        if (livesText == null && livesTextTMP == null)
        {
            GameObject livesGO = new GameObject("LivesText");
            livesGO.transform.SetParent(canvasGO.transform);
            var txt = livesGO.AddComponent<Text>();
            txt.font = arial;
            txt.fontSize = 28;
            txt.alignment = TextAnchor.UpperRight;
            txt.color = Color.yellow;
            txt.raycastTarget = false;
            RectTransform rt = txt.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-10, -40);
            rt.sizeDelta = new Vector2(200, 40);

            livesText = txt;
        }

        // Beacons Label (Faros)
        if (beaconsLabel == null && beaconsLabelTMP == null)
        {
            GameObject labelGO = new GameObject("BeaconsLabel");
            labelGO.transform.SetParent(canvasGO.transform);
            var txt = labelGO.AddComponent<Text>();
            txt.font = arial;
            txt.fontSize = 22;
            txt.alignment = TextAnchor.UpperRight;
            txt.color = Color.white;
            txt.raycastTarget = false;
            RectTransform rlabel = txt.GetComponent<RectTransform>();
            rlabel.anchorMin = new Vector2(1, 1);
            rlabel.anchorMax = new Vector2(1, 1);
            rlabel.pivot = new Vector2(1, 1);
            rlabel.anchoredPosition = new Vector2(-10, -80);
            rlabel.sizeDelta = new Vector2(300, 30);

            beaconsLabel = txt;
        }

        // Beacons Text
        if (beaconsText == null && beaconsTextTMP == null)
        {
            GameObject beaconsGO = new GameObject("BeaconsText");
            beaconsGO.transform.SetParent(canvasGO.transform);
            var txt = beaconsGO.AddComponent<Text>();
            txt.font = arial;
            txt.fontSize = 20;
            txt.alignment = TextAnchor.UpperRight;
            txt.color = Color.cyan;
            txt.raycastTarget = false;
            RectTransform rt = txt.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-10, -110);
            rt.sizeDelta = new Vector2(200, 30);

            beaconsText = txt;
        }

        // Score Label (arriba derecha, debajo de faros)
        if (scoreLabel == null && scoreLabelTMP == null)
        {
            GameObject labelGO = new GameObject("ScoreLabel");
            labelGO.transform.SetParent(canvasGO.transform);
            var txt = labelGO.AddComponent<Text>();
            txt.font = arial;
            txt.fontSize = 22;
            txt.alignment = TextAnchor.UpperRight;
            txt.color = Color.white;
            txt.raycastTarget = false;
            RectTransform rlabel = txt.GetComponent<RectTransform>();
            rlabel.anchorMin = new Vector2(1, 1);
            rlabel.anchorMax = new Vector2(1, 1);
            rlabel.pivot = new Vector2(1, 1);
            rlabel.anchoredPosition = new Vector2(-10, -140);
            rlabel.sizeDelta = new Vector2(400, 30);

            scoreLabel = txt;
        }

        // Score Text
        if (scoreText == null && scoreTextTMP == null)
        {
            GameObject scoreGO = new GameObject("ScoreText");
            scoreGO.transform.SetParent(canvasGO.transform);
            var txt = scoreGO.AddComponent<Text>();
            txt.font = arial;
            txt.fontSize = 72;                      // grande
            txt.alignment = TextAnchor.UpperRight;
            txt.color = Color.white;
            txt.raycastTarget = false;
            RectTransform rt = txt.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-10, -180);
            rt.sizeDelta = new Vector2(700, 100);

            scoreText = txt;
        }

        // Timer Text
        if (timerText == null && timerTextTMP == null)
        {
            GameObject timerGO = new GameObject("TimerText");
            timerGO.transform.SetParent(canvasGO.transform);
            var txt = timerGO.AddComponent<Text>();
            txt.font = arial;
            txt.fontSize = 56;                      // grande
            txt.alignment = TextAnchor.UpperRight;
            txt.color = Color.white;
            txt.raycastTarget = false;
            RectTransform rt = txt.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-10, -260);
            rt.sizeDelta = new Vector2(600, 80);

            timerText = txt;
        }

        // Actualizar inicialmente
        UpdateUI();
    }

    // ! Guardar la posicion del mapa actual
    public void MapaActualPosicion(GameObject mapa)
    {
        mapaActual = mapa;
        posicionMapaActual = mapaActual.transform.position;
    }

    // ! Cambiar el indice del mapa a generar
    public void CambiaIndiceMapa(int mapa)
    {
        mapaQueTocaPoner = mapa;
    }

    // ! Crear un nuevo escenario en la posicion indicada
    public void CrearEscenario(float posX, float posZ)
    {
        if (!EnsureMapaActual())
        {
            Debug.LogWarning("[GameManager] mapaActual es nulo, no se puede crear escenario. Asigna el mapa actual en escena.");
            return;
        }

        posicionMapaActual = mapaActual.transform.position;

        GameObject prefab = SeleccionarPrefab();
        if (prefab == null)
        {
            Debug.LogWarning("[GameManager] No hay prefab disponible para instanciar.");
            return;
        }

        GameObject esteMapa = Instantiate(
            prefab,
            new Vector3(posicionMapaActual.x + posX, posicionMapaActual.y, posicionMapaActual.z + posZ),
            Quaternion.identity
        );

        // Deshabilitar agentes para evitar error de NavMesh al instanciar
        var agents = esteMapa.GetComponentsInChildren<NavMeshAgent>(true);
        foreach (var ag in agents)
        {
            if (ag != null)
                ag.enabled = false;
        }

        // Rebuild NavMesh en el mapa nuevo si tiene superficies
        var surfaces = esteMapa.GetComponentsInChildren<NavMeshSurface>();
        foreach (var surface in surfaces)
        {
            surface.RemoveData();
            surface.BuildNavMesh();
        }

        // Reactivar agentes dentro del mapa recién creado
        var enemiesInMap = esteMapa.GetComponentsInChildren<EnemyAI>(true);
        foreach (var enemy in enemiesInMap)
        {
            if (enemy != null)
                enemy.ReactivateOnNavMesh();
        }

        listaMapasAlrededor.Add(esteMapa);
        mapasGenerados++;

        if (enemyTeleporter != null)
        {
            enemyTeleporter.HandleMapaCreado(esteMapa);
        }

        // Asegurar que todos los enemigos sigan al jugador después de crear un mapa
        Transform playerTf = GetPlayerTransform();
        if (playerTf != null)
        {
            var enemies = Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
            foreach (var e in enemies)
            {
                if (e != null)
                    e.ForceChase(playerTf);
            }
        }
    }

    // Intenta generar beacons dentro del bounds del mapa dado
    private void TrySpawnBeaconsInMap(GameObject map)
    {
        if (beaconPrefab == null) return;

        // Buscar un MeshRenderer en el mapa para obtener bounds
        MeshRenderer renderer = map.GetComponentInChildren<MeshRenderer>();
        if (renderer == null)
        {
            // si no hay renderer, no spawn
            return;
        }

        Bounds b = renderer.bounds;

        for (int i = 0; i < beaconsPerMap; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(b.min.x + 1f, b.max.x - 1f),
                b.min.y + 1f,
                Random.Range(b.min.z + 1f, b.max.z - 1f)
            );

            GameObject beacon = Instantiate(beaconPrefab, pos, Quaternion.identity, map.transform);
            beaconsTotal++;
        }

        UpdateUI();
    }

    public void BorrarMapasAlrededor()
    {
        if (listaMapasAlrededor == null)
            listaMapasAlrededor = new List<GameObject>();

        // Asegura que tenemos un mapa actual; si no, no borres nada
        if (mapaActual == null)
        {
            // intentar recuperar algún mapa en escena
            var ce = Object.FindFirstObjectByType<CreadorEscenarios>();
            if (ce != null && ce.transform.parent != null)
                mapaActual = ce.transform.parent.gameObject;
            if (mapaActual == null)
                return;
        }

        if (!listaMapasAlrededor.Contains(mapaActual))
            listaMapasAlrededor.Add(mapaActual);

        foreach (var mapa in listaMapasAlrededor)
        {
            if (mapa == null) continue;

            CreadorEscenarios creadorEscenarios = mapa.GetComponentInChildren<CreadorEscenarios>();
            if (creadorEscenarios == null) continue;

            // Nunca destruyas el mapa actual
            if (mapa == mapaActual)
            {
                creadorEscenarios.borrarEsteMapa = true;
                posicionMapaActual = mapaActual.transform.position;
                continue;
            }

            if (creadorEscenarios.borrarEsteMapa == true)
            {
                Destroy(mapa);
            }
            else
            {
                creadorEscenarios.borrarEsteMapa = true;
                mapaActual = creadorEscenarios.transform.parent.gameObject;
                posicionMapaActual = mapaActual.transform.position;
            }
        }

        listaMapasAlrededor.Clear();
        if (mapaActual != null)
            listaMapasAlrededor.Add(mapaActual);
    }

    private GameObject SeleccionarPrefab()
    {
        if (!finalGenerado && mapasGenerados >= mapasAntesDeFinal && mapaFinalPrefab != null)
        {
            finalGenerado = true;
            return mapaFinalPrefab;
        }

        if (mapaQueTocaPoner >= 0 && mapaQueTocaPoner < arrayMapas.Length)
        {
            GameObject elegido = arrayMapas[mapaQueTocaPoner];
            mapaQueTocaPoner = -1; // volver a aleatorio después de usar el índice forzado
            return elegido;
        }

        if (arrayMapas == null || arrayMapas.Length == 0)
        {
            return null;
        }

        int indiceAleatorio = Random.Range(0, arrayMapas.Length);
        return arrayMapas[indiceAleatorio];
    }

    private bool EnsureMapaActual()
    {
        if (mapaActual != null)
            return true;

        CreadorEscenarios ce = Object.FindFirstObjectByType<CreadorEscenarios>();
        if (ce != null && ce.transform.parent != null)
        {
            mapaActual = ce.transform.parent.gameObject;
            posicionMapaActual = mapaActual.transform.position;
            return true;
        }

        return false;
    }

    private Transform GetPlayerTransform()
    {
        if (playerTransformCache != null)
            return playerTransformCache;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerTransformCache = playerObj.transform;

        return playerTransformCache;
    }

    private void RespawnEntities()
    {
        // Respawn del jugador
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && playerSpawnPoint != null)
        {
            var controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
                player.transform.position = playerSpawnPoint.position;
                controller.enabled = true;
            }
            else
            {
                player.transform.position = playerSpawnPoint.position;
            }
        }

        // Respawn/Reset del enemigo
        EnemyAI enemy = enemyRef != null ? enemyRef : Object.FindFirstObjectByType<EnemyAI>();
        if (enemy != null && enemySpawnPoint != null)
        {
            enemy.ResetAI(enemySpawnPoint.position);
        }
    }

    private IEnumerator RespawnAfterDelay()
    {
        Time.timeScale = 1f;
        yield return new WaitForSeconds(respawnDelay);
        RespawnEntities();
    }

    private IEnumerator ReloadSceneAfterDelay(float seconds)
    {
        Time.timeScale = 1f;
        yield return new WaitForSeconds(seconds);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}

