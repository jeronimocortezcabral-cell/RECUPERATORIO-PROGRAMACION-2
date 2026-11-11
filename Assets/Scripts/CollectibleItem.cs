using Unity.Netcode;
using UnityEngine;

public class CollectibleItem : NetworkBehaviour
{
    public enum ItemType { Comunion, Valioso, Negativo }

    [Header("Configuración del Objeto")]
    [SerializeField] private ItemType itemType;

    [HideInInspector] public int itemValue;

    private bool _isCollected = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            switch (itemType)
            {
                case ItemType.Comunion:
                    itemValue = 10;  
                    break;
                case ItemType.Valioso:
                    itemValue = 25; 
                    break;
                case ItemType.Negativo:
                    itemValue = -15; 
                    break;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            NetworkObject playerNetObj = other.GetComponent<NetworkObject>();

            if (playerNetObj != null)
            {
                if (IsServer)
                {
                    ProcessCollection(playerNetObj.OwnerClientId);
                }
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
        ProcessCollection(clientId);
    }

    private void ProcessCollection(ulong clientId)
    {
        if (!IsServer || _isCollected) return;
        _isCollected = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScoreToPlayer(clientId, itemValue);
        }

        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }
}