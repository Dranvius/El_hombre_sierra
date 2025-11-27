using UnityEngine;

/// <summary>
/// Instancia el personaje seleccionado al iniciar la escena.
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    [Tooltip("Prefabs de personajes disponibles. El índice debe coincidir con el del menú de selección.")]
    [SerializeField] private GameObject[] playerPrefabs;

    [Tooltip("Punto de spawn opcional; si es nulo, usa la posición del propio spawner.")]
    [SerializeField] private Transform spawnPoint;

    private void Start()
    {
        if (playerPrefabs == null || playerPrefabs.Length == 0)
        {
            Debug.LogError("[PlayerSpawner] No hay prefabs de jugador asignados.");
            return;
        }

        int idx = Mathf.Clamp(GameSession.selectedCharacterIndex, 0, playerPrefabs.Length - 1);
        GameObject prefab = playerPrefabs[idx];
        if (prefab == null)
        {
            Debug.LogError($"[PlayerSpawner] El prefab en índice {idx} es nulo.");
            return;
        }

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

        GameObject player = Instantiate(prefab, pos, rot);
        player.tag = "Player";

        // Si la cámara no tiene target, SimpleCameraFollow lo encontrará por tag.
        // Forzar asignación de enemigos si no tienen referencia.
        AssignPlayerToEnemies(player.transform);
    }

    private void AssignPlayerToEnemies(Transform player)
    {
        var enemies = Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        foreach (var e in enemies)
        {
            if (e == null) continue;
            e.ForceChase(player);
        }
    }
}
