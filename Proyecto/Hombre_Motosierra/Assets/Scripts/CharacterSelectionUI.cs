using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// UI simple para seleccionar personaje y cargar la escena de juego.
/// Asigna este script a un objeto en el menú y conecta botones a SelectCharacter(index) y StartGame().
/// </summary>
public class CharacterSelectionUI : MonoBehaviour
{
    [Tooltip("Nombre de la escena del juego a cargar.")]
    [SerializeField] private string gameSceneName = "Inicio_juego";

    [Tooltip("Número de personajes disponibles.")]
    [SerializeField] private int characterCount = 1;

    public void SelectCharacter(int index)
    {
        if (index < 0) index = 0;
        if (index >= characterCount) index = characterCount - 1;
        GameSession.selectedCharacterIndex = index;
        Debug.Log($"[CharacterSelectionUI] Seleccionado personaje índice: {index}");
    }

    public void StartGame()
    {
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogError("[CharacterSelectionUI] No se ha configurado la escena de juego.");
            return;
        }

        GameSession.startOnLoad = true;
        SceneManager.LoadScene(gameSceneName);
    }
}
