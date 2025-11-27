using System;
using UnityEngine;

public class CreadorEscenarios : MonoBehaviour
{
    public enum Posiciones
    {
        Arriba,
        Abajo,
        Izquierda,
        Derecha
    }

    [SerializeField] private int siguienteMapa = -1; // -1 => aleatorio
    [SerializeField] private float fallbackWidth = 50f;
    [SerializeField] private float fallbackLength = 50f;
    private BoxCollider col;
    public bool borrarEsteMapa = true;

    private float mapaWidth;
    private float mapaLength;

    private void Awake()
    {
        col = GetComponent<BoxCollider>();

        MeshRenderer renderer = null;

        if (transform.parent != null)
        {
            renderer = transform.parent.GetComponentInChildren<MeshRenderer>();
        }

        if (renderer == null)
        {
            renderer = GetComponentInParent<MeshRenderer>();
        }

        if (renderer == null)
        {
            renderer = GetComponentInChildren<MeshRenderer>();
        }

        if (renderer != null)
        {
            mapaWidth = renderer.bounds.size.x;
            mapaLength = renderer.bounds.size.z;
        }
        else
        {
            mapaWidth = fallbackWidth;
            mapaLength = fallbackLength;
            Debug.LogWarning($"[{name}] No se encontro MeshRenderer. Usando tamano {fallbackWidth}x{fallbackLength} por defecto.");
        }
    }

    public void BuclePosiciones()
    {
        foreach (Posiciones posicion in Enum.GetValues(typeof(Posiciones)))
        {
            GenerarEscenario(posicion);
        }
    }

    private void GenerarEscenario(Posiciones posicion)
    {
        var gm = GameManager.instancia;
        gm.MapaActualPosicion(transform.parent.gameObject);

        switch (posicion)
        {
            case Posiciones.Arriba:
                gm.CrearEscenario(0f, mapaLength);
                break;
            case Posiciones.Abajo:
                gm.CrearEscenario(0f, -mapaLength);
                break;
            case Posiciones.Izquierda:
                gm.CrearEscenario(-mapaWidth, 0f);
                break;
            case Posiciones.Derecha:
                gm.CrearEscenario(mapaWidth, 0f);
                break;
        }

        if (col != null)
        {
            col.enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        // Mientras el jugador esté en este mapa, no debe borrarse
        borrarEsteMapa = false;

        var gm = GameManager.instancia;
        if (gm == null)
        {
            Debug.LogWarning("[CreadorEscenarios] GameManager no encontrado.");
            return;
        }
        gm.CambiaIndiceMapa(siguienteMapa);
        gm.MapaActualPosicion(transform.parent.gameObject); // asegurar mapa actual antes de borrar
        gm.BorrarMapasAlrededor();

        BuclePosiciones();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        // Al salir el jugador, permitir que este mapa se borre en el siguiente ciclo
        borrarEsteMapa = true;
    }
}
