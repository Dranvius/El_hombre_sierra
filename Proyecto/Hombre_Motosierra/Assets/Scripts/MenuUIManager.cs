using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Control central del menú: música de fondo, SFX de botones y transición animada al iniciar.
/// Coloca este script en el Canvas del menú (o en un GameObject hijo del Canvas).
/// </summary>
public class MenuUIManager : MonoBehaviour
{
    public static MenuUIManager Instance { get; private set; }

    [Header("Audio")]
    public AudioClip backgroundMusic;
    public AudioClip clickSfx;
    public AudioClip hoverSfx;
    [Tooltip("Volumen 0..1")]
    public float musicVolume = 0.6f;
    public float sfxVolume = 1f;

    [Header("Transición al iniciar")]
    [Tooltip("Animator con una transición que use el trigger 'Start' (opcional).")]
    public Animator menuAnimator;
    [Tooltip("Tiempo en segundos que esperamos antes de cargar la escena (si no hay Animator).")]
    public float startDelay = 0.6f;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Opcional: si quieres mantener la música al cambiar de escena, descomenta la línea
        // DontDestroyOnLoad(gameObject);

        // Crear AudioSources si no existen
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.volume = musicVolume;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.volume = sfxVolume;
    }

    private void Start()
    {
        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }
    }

    public void PlayClick() => PlaySfx(clickSfx);
    public void PlayHover() => PlaySfx(hoverSfx);

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    /// <summary>
    /// Llama a este método para iniciar la transición (animación + SFX) y cargar la escena.
    /// </summary>
    public void StartGameWithAnimation(string sceneName)
    {
        StartCoroutine(DoStartTransition(sceneName));
    }

    private IEnumerator DoStartTransition(string sceneName)
    {
        // reproducir sonido de click
        PlayClick();

        // si hay animator, activar trigger y esperar la duración del clip (si la hay)
        if (menuAnimator != null)
        {
            menuAnimator.SetTrigger("Start");

            // si el animator tiene un estado "Start" con una animación, esperar su length
            // Sino esperar el startDelay por defecto
            float wait = startDelay;

            // intentar calcular duración del clip en el estado actual
            var info = menuAnimator.GetCurrentAnimatorStateInfo(0);
            // No podemos leer la duración del siguiente estado a menos que lo conozcamos.
            // Por seguridad, esperamos startDelay.
            yield return new WaitForSeconds(wait);
        }
        else
        {
            yield return new WaitForSeconds(startDelay);
        }

        // Asegurar que Time.timeScale = 1
        Time.timeScale = 1f;

        // Cargar escena
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Para detener la música (llámalo si sales del menú y no quieres mantener la música).
    /// </summary>
    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Stop();
    }
}