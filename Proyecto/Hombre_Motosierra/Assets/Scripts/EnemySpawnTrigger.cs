using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemySpawnTrigger : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private EnemyAI enemyPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private bool spawnOnce = true;
    [SerializeField] private string playerTag = "Player";

    private EnemyAI spawnedEnemy;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        if (spawnOnce && spawnedEnemy != null)
            return;

        SpawnAndChase(other.transform);
    }

    private void SpawnAndChase(Transform player)
    {
        if (enemyPrefab == null || player == null)
        {
            Debug.LogWarning("[EnemySpawnTrigger] Falta asignar enemyPrefab o player.");
            return;
        }

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        spawnedEnemy = Instantiate(enemyPrefab, pos, rot);
        spawnedEnemy.ForceChase(player);
    }
}
