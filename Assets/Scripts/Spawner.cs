using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Spawner : NetworkBehaviour
{
    [Header("Configuración de Prefabs")]
    [SerializeField] private GameObject prefabComun;       // El item común de siempre
    [SerializeField] private GameObject prefabEspecial;    // El que aparece cada 10 comunes
    [SerializeField] private GameObject prefabTrampa;      // El item trampa (aparece con normalidad)

    [Header("Configuración del Spawner")]
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private Vector3 spawnRange = new Vector3(10f, 0f, 10f);

    private int contadorComunes = 0;
    private const int frecuenciaEspecial = 10; // 1 especial cada 10 comunes

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

        // 1. Evaluamos si ya se cumplieron los 10 items comunes para forzar el especial
        if (contadorComunes >= frecuenciaEspecial)
        {
            selectedPrefab = prefabEspecial;
            contadorComunes = 0; // Reiniciamos el contador de comunes
            Debug.Log("✨ [SPAWNER] ¡Llegamos a 10 comunes! Aparece el item ESPECIAL.");
        }
        else
        {
            // 2. Si no toca especial, decidimos al azar entre que sea un item común o un item trampa
            // (Usamos 50/50 o la probabilidad que prefieras para la trampa)
            bool esTrampa = (Random.value < 0.3f); // Por ejemplo, 30% de probabilidad de trampa, 70% de común

            if (esTrampa && prefabTrampa != null)
            {
                selectedPrefab = prefabTrampa;
                // Nota: La trampa NO afecta al contador de comunes, sigue su curso normal
            }
            else if (prefabComun != null)
            {
                selectedPrefab = prefabComun;
                contadorComunes++; // Sumamos al contador porque es un item común
            }
        }

        if (selectedPrefab == null)
        {
            Debug.LogError("¡Falta asignar alguno de los prefabs en el Inspector del Spawner!");
            return;
        }

        // Posición aleatoria
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