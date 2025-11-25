using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    // ! Gestion de la creacion y destruccion de los mapas

    [SerializeField] private GameObject[] arrayMapas;
    [SerializeField] private GameObject mapaFinalPrefab;
    [SerializeField] private int mapasAntesDeFinal = 5;
    [SerializeField] private List<GameObject> listaMapasAlrededor;
    [SerializeField] private GameObject mapaActual;
    [SerializeField] private Vector3 posicionMapaActual;
    [SerializeField] private EnemyTeleportManager enemyTeleporter;


    private int mapaQueTocaPoner;
    private int mapasGenerados;
    private bool finalGenerado;

    public static GameManager instancia;



    // CICLO DE VIDA DEL GAME MANAGER
    private void Awake()
    {
        instancia = this;
    }


    // ! En el start se guarda la posicion del mapa actual
        // - Define la ejecucion en cada paso que se inicia el juego

    private void Start()
    {
        mapaQueTocaPoner = -1;
        mapasGenerados = 0;
        finalGenerado = false;

        MapaActualPosicion(mapaActual);

        listaMapasAlrededor.Add(mapaActual);
    }


    // ! Guardar la posicion del mapa actual
        // - Recibe el mapa actual como parametro
    public void MapaActualPosicion(GameObject mapa)
    {

        mapaActual = mapa;
        posicionMapaActual = mapaActual.transform.position;
    }

    // ! Cambiar el indice del mapa a generar
        // - Recibe el indice del mapa como parametro
    public void CambiaIndiceMapa(int mapa)
    {
        mapaQueTocaPoner = mapa;
    }

    // ! Crear un nuevo escenario en la posicion indicada
        // - Recibe las posiciones en X y Z como parametros
    public void CrearEscenario(float posX, float posZ)
    {
        GameObject prefab = SeleccionarPrefab();
        if (prefab == null)
        {
            Debug.LogWarning("[GameManager] No hay prefab disponible para instanciar.");
            return;
        }

        GameObject esteMapa = Instantiate(prefab, new Vector3(posicionMapaActual.x + posX, posicionMapaActual.y, posicionMapaActual.z + posZ), Quaternion.identity);

        listaMapasAlrededor.Add(esteMapa);
        mapasGenerados++;

        if (enemyTeleporter != null)
        {
            enemyTeleporter.HandleMapaCreado(esteMapa);
        }
    }


    // ! Crear un nuevo escenario en la posicion indicada
        // - Recibe las posiciones en X y Z como parametros

    public void BorrarMapasAlrededor()
    {
        foreach (var mapa in listaMapasAlrededor)
        {
            CreadorEscenarios creadorEscenarios = mapa.GetComponentInChildren<CreadorEscenarios>();

            if (creadorEscenarios.borrarEsteMapa == true)
            {
                Destroy(mapa);
            }
            else
            {
                
                creadorEscenarios.borrarEsteMapa = true;
                mapaActual = creadorEscenarios.transform.parent.gameObject;
            }
        }
        
        listaMapasAlrededor.Clear();
        listaMapasAlrededor.Add(mapaActual);
        
    }

    private GameObject SeleccionarPrefab()
    {
        if (!finalGenerado && mapasGenerados >= mapasAntesDeFinal && mapaFinalPrefab != null)
        {
            finalGenerado = true;
            return mapaFinalPrefab;
        }

        if (mapaQueTocaPoner >= 0 && mapaQueTocaPoner < arrayMapas.Length)
        {
            GameObject elegido = arrayMapas[mapaQueTocaPoner];
            mapaQueTocaPoner = -1; // volver a aleatorio después de usar el índice forzado
            return elegido;
        }

        if (arrayMapas == null || arrayMapas.Length == 0)
        {
            return null;
        }

        int indiceAleatorio = Random.Range(0, arrayMapas.Length);
        return arrayMapas[indiceAleatorio];
    }

    
}
