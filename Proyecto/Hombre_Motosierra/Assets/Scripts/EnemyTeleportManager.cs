using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyTeleportManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private EnemyAI enemy;
    [SerializeField] private string waypointTag = "Waypoint";
    [SerializeField] private string spawnPointTag = "EnemySpawn";
    [SerializeField] private float warpYOffset = 0.2f;

    /// <summary>
    /// Llamar cada vez que se crea un mapa nuevo.
    /// Teletransporta al enemigo y asigna nuevos puntos de patrulla encontrados en el mapa.
    /// </summary>
    public void HandleMapaCreado(GameObject nuevoMapa)
    {
        if (enemy == null || nuevoMapa == null)
            return;

        List<Transform> waypoints = ObtenerWaypoints(nuevoMapa);
        Vector3 destino = CalcularDestino(nuevoMapa, waypoints);

        NavMeshAgent navAgent = enemy.GetComponent<NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.Warp(destino + Vector3.up * warpYOffset);
        }
        else
        {
            enemy.transform.position = destino;
        }

        if (waypoints.Count > 0)
        {
            enemy.SetPatrolRoute(waypoints);
        }
    }

    private Vector3 CalcularDestino(GameObject mapa, List<Transform> waypoints)
    {
        Transform spawn = BuscarPorTag(mapa.transform, spawnPointTag);
        if (spawn != null)
            return spawn.position;

        if (waypoints.Count > 0)
            return waypoints[Random.Range(0, waypoints.Count)].position;

        return mapa.transform.position;
    }

    private List<Transform> ObtenerWaypoints(GameObject mapa)
    {
        List<Transform> puntos = new List<Transform>();
        foreach (Transform t in mapa.GetComponentsInChildren<Transform>())
        {
            if (t.CompareTag(waypointTag))
            {
                puntos.Add(t);
            }
        }

        return puntos;
    }

    private Transform BuscarPorTag(Transform root, string tag)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>())
        {
            if (t.CompareTag(tag))
                return t;
        }
        return null;
    }
}
