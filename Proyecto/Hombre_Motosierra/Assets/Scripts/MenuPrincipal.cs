using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    [Header("Nombre de la escena del juego")]
    [SerializeField] private string nombreEscenaJuego = "Inicio_juego";

    // Llamado desde el botón "Iniciar"
    public void IniciarJuego()
    {
        if (string.IsNullOrEmpty(nombreEscenaJuego))
        {
            Debug.LogError("⚠ No has asignado el nombre de la escena del juego en el MenuPrincipal.");
            return;
        }

        // Asegurarnos de que el tiempo esté en 1 (por si venimos de una pausa)
        Time.timeScale = 1f;

        // Marcar que queremos que la sesión comience al cargar la escena
        GameSession.startOnLoad = true;
        GameSession.sessionDuration = 120f; // 2 minutos (puedes cambiar si quieres variable)

        Debug.Log($"Cargando escena: {nombreEscenaJuego} (iniciando sesión con {GameSession.sessionDuration}s)");
        SceneManager.LoadScene(nombreEscenaJuego);
    }

    // Llamado desde el botón "Salir"
    public void Salir()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();

#if UNITY_EDITOR
        // Para que funcione también dentro del editor de Unity
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}