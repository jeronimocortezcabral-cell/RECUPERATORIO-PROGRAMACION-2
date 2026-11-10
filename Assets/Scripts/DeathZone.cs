using UnityEngine;
using Unity.Netcode;

public class DeathZone : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // El servidor procesa la caída y solicita el respawn.
        if (!IsServer) return;

        // Busca PlayerMovement también en los padres por si el collider está en un hijo.
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();

        if (player == null) return;

        Vector3 destino = new Vector3(0f, 2f, 0f);
        Quaternion rotacion = Quaternion.identity;

        if (NetworkSpawnManager.Instance != null)
        {
            destino = NetworkSpawnManager.Instance.GetSpawnPosition(
                player.OwnerClientId
            );

            rotacion = NetworkSpawnManager.Instance.GetSpawnRotation(
                player.OwnerClientId
            );
        }
        else
        {
            Debug.LogError(
                "[DEATHZONE] No se encontró NetworkSpawnManager. Se usará la posición de respaldo."
            );
        }

        player.TriggerSafeTeleportClientRpc(
            destino,
            rotacion,
            new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { player.OwnerClientId }
                }
            }
        );

        Debug.Log(
            $"[DEATHZONE] Jugador {player.OwnerClientId}: respawn solicitado en {destino}."
        );
    }
}