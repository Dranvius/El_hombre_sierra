using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreadorEscenarios : MonoBehaviour
{
    public enum Posiciones
    {
        Arriba,
        Abajo,
        Izquierda,
        Derecha
    };

    [SerializeField] private int siguienteMapa;
    private BoxCollider col;
    public bool borrarEsteMapa = true;

    private float mapaWidth;   // tamaño en X
    private float mapaLength;  // tamaño en Z

    private void Awake()
    {
        col = GetComponent<BoxCollider>();

        // --------------------------------------------
        // Buscar el MeshRenderer correctamente
        // --------------------------------------------
        MeshRenderer renderer = null;

        // 1️⃣ Intentar obtenerlo del padre
        if (transform.parent != null)
            renderer = transform.parent.GetComponentInChildren<MeshRenderer>();

        // 2️⃣ Intentar buscar hacia arriba por si está más arriba en jerarquía
        if (renderer == null)
            renderer = GetComponentInParent<MeshRenderer>();

        // 3️⃣ Intentar buscar en hijos del mismo objeto
        if (renderer == null)
            renderer = GetComponentInChildren<MeshRenderer>();

        // 4️⃣ Asignar tamaño si lo encontramos
        if (renderer != null)
        {
            mapaWidth = renderer.bounds.size.x;
            mapaLength = renderer.bounds.size.z;
        }
        else
        {
            // Valor por defecto para evitar crasheos
            mapaWidth = 50;
            mapaLength = 50;

            Debug.LogWarning(
                $"[{name}] No encontré ningún MeshRenderer. Usando tamaño 50x50 por defecto."
            );
        }
    }

    // --------------------------------------------
    // Generar escenarios alrededor
    // --------------------------------------------
    public void BuclePosiciones()
    {
        string[] posiciones = System.Enum.GetNames(typeof(Posiciones));

        foreach (var posicion in posiciones)
            GenerarEscenario(posicion);
    }

    // --------------------------------------------
    // Crear mapa según dirección
    // --------------------------------------------
    private void GenerarEscenario(string posicion)
    {
        GameManager.instancia.MapaActualPosicion(this.transform.parent.gameObject);

        switch (posicion)
        {
            case "Arriba":
                GameManager.instancia.CrearEscenario(0, mapaLength);
                break;

            case "Abajo":
                GameManager.instancia.CrearEscenario(0, -mapaLength);
                break;

            case "Izquierda":
                GameManager.instancia.CrearEscenario(-mapaWidth, 0);
                break;

            case "Derecha":
                GameManager.instancia.CrearEscenario(mapaWidth, 0);
                break;
        }

        // Desactivar collider para evitar doble activación
        col.enabled = false;
    }

    // --------------------------------------------
    // Cuando el jugador entra al trigger
    // --------------------------------------------
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            borrarEsteMapa = false;

            GameManager.instancia.CambiaIndiceMapa(siguienteMapa);
            GameManager.instancia.BorrarMapasAlrededor();

            BuclePosiciones();
        }
    }
}
