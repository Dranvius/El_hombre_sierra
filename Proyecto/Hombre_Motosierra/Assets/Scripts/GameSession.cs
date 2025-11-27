using UnityEngine;

/// <summary>
/// Estado estático para pasar selección y configuración entre escenas.
/// </summary>
public static class GameSession
{
    // Si MenuPrincipal puso esto a true, el GameManager iniciará el temporizador y el score en Start()
    public static bool startOnLoad = false;

    // Duración por defecto en segundos (2 minutos)
    public static float sessionDuration = 120f;

    // Índice del personaje seleccionado en el menú
    public static int selectedCharacterIndex = 0;

    public static void Reset()
    {
        startOnLoad = false;
        sessionDuration = 120f;
        selectedCharacterIndex = 0;
    }
}
