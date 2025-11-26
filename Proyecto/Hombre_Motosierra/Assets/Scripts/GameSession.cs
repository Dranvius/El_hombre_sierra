using UnityEngine;

/// <summary>
/// Clase estática para indicar que la partida debe arrancar al cargar la escena.
/// Esto evita depender de objetos DontDestroyOnLoad: simplemente guarda la intención.
/// </summary>
public static class GameSession
{
    // Si MenuPrincipal puso esto a true, el GameManager iniciará el temporizador y el score en Start()
    public static bool startOnLoad = false;

    // Duración por defecto en segundos (2 minutos)
    public static float sessionDuration = 120f;

    public static void Reset()
    {
        startOnLoad = false;
        sessionDuration = 120f;
    }
}