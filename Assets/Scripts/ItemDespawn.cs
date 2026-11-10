using Unity.Netcode;
using UnityEngine;

public class ItemDespawn : NetworkBehaviour
{
    [Header("Tiempo de Vida")]
    [SerializeField] private float lifetime = 15f; // Ajustado a un tiempo razonable

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        Invoke(nameof(DespawnMe), lifetime);
    }

    private void DespawnMe()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }
}