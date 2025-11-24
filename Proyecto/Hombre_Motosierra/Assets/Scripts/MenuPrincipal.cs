using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPrincipal : MonoBehaviour
{
    [Header("Nombre de la escena del juego")]
    [SerializeField] private string nombreEscenaJuego = "Inicio_juego";
    
    // Cambia "Juego" en el inspector por el nombre real de tu escena

    // Llamado desde el botón "Iniciar"
    public void IniciarJuego()
    {
        if (string.IsNullOrEmpty(nombreEscenaJuego))
        {
            Debug.LogError("⚠ No has asignado el nombre de la escena del juego en el MenuPrincipal.");
            return;
        }

        Debug.Log($"Cargando escena: {nombreEscenaJuego}");
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
