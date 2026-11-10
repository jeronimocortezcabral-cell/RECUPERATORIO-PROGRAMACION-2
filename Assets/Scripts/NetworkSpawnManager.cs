using UnityEngine;
using Unity.Netcode;

public class NetworkSpawnManager : NetworkBehaviour
{
    public static NetworkSpawnManager Instance { get; private set; }

    [Header("Referencias de Posiciones")]
    [SerializeField] private Transform hostSpawnPoint;
    [SerializeField] private Transform clientSpawnPoint;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning(
                "[NetworkSpawnManager] Hay más de una instancia. Se eliminará esta."
            );

            Destroy(gameObject);
        }
    }

    public Vector3 GetSpawnPosition(ulong clientId)
    {
        Transform spawnPoint = clientId == 0
            ? hostSpawnPoint
            : clientSpawnPoint;

        if (spawnPoint != null)
        {
            return spawnPoint.position;
        }

        Debug.LogError(
            clientId == 0
                ? "[NetworkSpawnManager] Falta asignar Host Spawn Point en el Inspector."
                : "[NetworkSpawnManager] Falta asignar Client Spawn Point en el Inspector."
        );

        return new Vector3(0f, 2f, 0f);
    }

    public Quaternion GetSpawnRotation(ulong clientId)
    {
        Transform spawnPoint = clientId == 0
            ? hostSpawnPoint
            : clientSpawnPoint;

        return spawnPoint != null
            ? spawnPoint.rotation
            : Quaternion.identity;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}