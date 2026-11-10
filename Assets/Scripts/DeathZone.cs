using UnityEngine;
using Unity.Netcode;

public class DeathZone : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Solo el servidor procesa la colisión para evitar llamadas duplicadas
        if (!IsServer) return;

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player != null)
        {
            ulong clientId = player.OwnerClientId;

            // Buscamos su punto de spawn correspondiente
            string nombreSpawn = (clientId == 0) ? "SpawnPoint_Host" : "SpawnPoint_Client";
            GameObject puntoEncontrado = GameObject.Find(nombreSpawn);
            Vector3 destino = puntoEncontrado != null ? puntoEncontrado.transform.position : new Vector3(0f, 2f, 0f);

            // Ordenamos al cliente específico que ejecute el teletransporte seguro
            player.TriggerSafeTeleportClientRpc(destino, new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
            });

            Debug.Log($"🛡️ [DEATHZONE] Jugador {clientId} cayó al vacío. Rescatándolo hacia {nombreSpawn}...");
        }
    }
}