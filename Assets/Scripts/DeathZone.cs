using UnityEngine;
using Unity.Netcode;

public class DeathZone : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // La muerte y el respawn se procesan solo en el servidor.
        if (!IsServer) return;

        // También busca en los padres por si el collider está en un objeto hijo.
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();

        if (player == null) return;

        ulong clientId = player.OwnerClientId;

        string nombreSpawn = clientId == 0
            ? "SpawnPoint_Host"
            : "SpawnPoint_Client";

        GameObject puntoEncontrado = GameObject.Find(nombreSpawn);

        if (puntoEncontrado == null)
        {
            Debug.LogWarning(
                $"[DEATHZONE] No se encontró '{nombreSpawn}'. Se usará la posición de respaldo."
            );
        }

        Vector3 destino = puntoEncontrado != null
            ? puntoEncontrado.transform.position
            : new Vector3(0f, 2f, 0f);

        player.TriggerSafeTeleportClientRpc(
            destino,
            new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { clientId }
                }
            }
        );

        Debug.Log(
            $"[DEATHZONE] El servidor detectó la caída del jugador {clientId}. " +
            $"Enviando respawn a '{nombreSpawn}' en {destino}."
        );
    }
}