using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Spawner : NetworkBehaviour
{
    [Header("Configuración de Prefabs")]
    [SerializeField] private GameObject prefabComun;      
    [SerializeField] private GameObject prefabEspecial;   
    [SerializeField] private GameObject prefabTrampa;      

    [Header("Configuración del Spawner")]
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private Vector3 spawnRange = new Vector3(10f, 0f, 10f);

    private int contadorComunes = 0;
    private const int frecuenciaEspecial = 10;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnItem();
        }
    }

    private void SpawnItem()
    {
        GameObject selectedPrefab = null;

        if (contadorComunes >= frecuenciaEspecial)
        {
            selectedPrefab = prefabEspecial;
            contadorComunes = 0;
            Debug.Log("✨ [SPAWNER] ¡Llegamos a 10 comunes! Aparece el item ESPECIAL.");
        }
        else
        {
            bool esTrampa = (Random.value < 0.3f); 

            if (esTrampa && prefabTrampa != null)
            {
                selectedPrefab = prefabTrampa;
            }
            else if (prefabComun != null)
            {
                selectedPrefab = prefabComun;
                contadorComunes++;
            }
        }

        if (selectedPrefab == null)
        {
            Debug.LogError("¡Falta asignar alguno de los prefabs en el Inspector del Spawner!");
            return;
        }

        Vector3 randomPos = new Vector3(
            Random.Range(-spawnRange.x, spawnRange.x),
            spawnRange.y,
            Random.Range(-spawnRange.z, spawnRange.z)
        ) + transform.position;

        GameObject spawnedItem = Instantiate(selectedPrefab, randomPos, Quaternion.Euler(0, Random.Range(0, 360), 0));

        NetworkObject netObj = spawnedItem.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }
        else
        {
            Debug.LogError($"¡El prefab '{selectedPrefab.name}' no tiene un componente NetworkObject!");
            Destroy(spawnedItem);
        }
    }
}