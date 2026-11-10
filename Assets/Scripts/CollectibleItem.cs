using Unity.Netcode;
using UnityEngine;

public class CollectibleItem : NetworkBehaviour
{
    public enum ItemType { Comunion, Valioso, Negativo }

    [Header("Configuración del Objeto")]
    [SerializeField] private ItemType itemType;

    [HideInInspector] public int itemValue;

    private bool _isCollected = false; // Para evitar que se recolecte dos veces por error

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            switch (itemType)
            {
                case ItemType.Comunion:
                    itemValue = 10;  // Suma moderada
                    break;
                case ItemType.Valioso:
                    itemValue = 25;  // Suma alta
                    break;
                case ItemType.Negativo:
                    itemValue = -15; // ¡Resta puntos!
                    break;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verificamos si chocó contra un Jugador
        if (other.CompareTag("Player"))
        {
            NetworkObject playerNetObj = other.GetComponent<NetworkObject>();

            if (playerNetObj != null)
            {
                // CASO A: Lo toca el Host o Servidor directamente
                if (IsServer)
                {
                    ProcessCollection(playerNetObj.OwnerClientId);
                }
                // CASO B: Lo toca un Cliente (verificamos que sea NUESTRO propio jugador local)
                else if (IsClient && playerNetObj.IsOwner)
                {
                    RequestPickupServerRpc(playerNetObj.OwnerClientId);
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestPickupServerRpc(ulong clientId)
    {
        // El servidor recibe la petición del cliente y procesa la recogida de forma segura
        ProcessCollection(clientId);
    }

    private void ProcessCollection(ulong clientId)
    {
        // Asegurarnos de que solo el servidor ejecute la lógica de puntaje y destrucción
        if (!IsServer || _isCollected) return;
        _isCollected = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScoreToPlayer(clientId, itemValue);
        }

        // Despawnea el objeto en red y lo destruye para todos
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }
}