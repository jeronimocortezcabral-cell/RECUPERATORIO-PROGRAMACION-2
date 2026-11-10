using UnityEngine;
using Unity.Netcode;

public class CollectibleItem : NetworkBehaviour
{
    public enum ItemType { Comunion, Valioso, Negativo }

    [Header("Configuración del Objeto")]
    [SerializeField] private ItemType itemType;

    [HideInInspector] public int itemValue;

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
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            NetworkObject playerNetObj = other.GetComponent<NetworkObject>();

            if (playerNetObj != null)
            {
                ulong playerId = playerNetObj.OwnerClientId;

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddScoreToPlayer(playerId, itemValue);
                }

                GetComponent<NetworkObject>().Despawn();
                Destroy(gameObject);
            }
        }
    }
}