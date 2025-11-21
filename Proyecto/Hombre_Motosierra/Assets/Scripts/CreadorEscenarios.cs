using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class CreadorEscenarios : MonoBehaviour
{


    // ! Determinacion de cada uno de las dirreciones para generar escenarios
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



    private void Awake()
    {
        col = GetComponent<BoxCollider>();
    }

    // ! Bucle para generar escenarios en las 4 direcciones

    public void BuclePosiciones()
    {
        string[] posiciones = System.Enum.GetNames(typeof(Posiciones));

        foreach (var posicion in posiciones)
        {
            GenerarEscenario(posicion);
        }
    }


    // ! Generacion de escenarios

    private void GenerarEscenario(string posicion){

        GameManager.instancia.MapaActualPosicion(this.transform.parent.gameObject);

        switch (posicion)
        {
            case "Arriba":
                GameManager.instancia.CrearEscenario(0, 50);
                break;
            case "Abajo":
                GameManager.instancia.CrearEscenario(0, -50);
                break;
            case "Izquierda":
                GameManager.instancia.CrearEscenario(-50, 0);
                break;
            case "Derecha":
                GameManager.instancia.CrearEscenario(50, 0);
                break;
        }

        col.enabled = false;
    }


    // ! Deteccion de colision para generar nuevos mapas

    private void OnTriggerEnter(Collider other){

        //Referecncia  jugador
        if (other.gameObject.CompareTag("Player"))
        {
            borrarEsteMapa = false;
            GameManager.instancia.CambiaIndiceMapa(siguienteMapa);
            GameManager.instancia.BorrarMapasAlrededor();
            BuclePosiciones();
        }
    }



}
