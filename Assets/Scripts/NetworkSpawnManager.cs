using UnityEngine;
using Unity.Netcode;

public class NetworkSpawnManager : NetworkBehaviour
{
    public static NetworkSpawnManager Instance;

    [Header("Referencias de Posiciones")]
    [SerializeField] private Transform hostSpawnPoint;
    [SerializeField] private Transform clientSpawnPoint;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Método que el jugador llamará al spawnear para saber dónde ubicarse
    public Vector3 GetSpawnPosition(ulong clientId)
    {
        // Si el ID es 0 (el Host), va al punto del Host; si no, al del Cliente
        if (clientId == 0 && hostSpawnPoint != null)
        {
            return hostSpawnPoint.position;
        }
        else if (clientSpawnPoint != null)
        {
            return clientSpawnPoint.position;
        }

        // Posición de respaldo por seguridad si olvidas asignar los transforms
        return new Vector3(0f, 2f, 0f);
    }
}