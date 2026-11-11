using UnityEngine;
using Unity.Netcode;

public class DeathZone : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer)
        {
            return;
        }

        PlayerMovement player =
            other.GetComponentInParent<PlayerMovement>();

        if (player == null)
        {
            return;
        }

        if (NetworkSpawnManager.Instance == null)
        {
            Debug.LogError(
                "[DEATHZONE] No se encontró NetworkSpawnManager."
            );
            return;
        }

        ulong clientId = player.OwnerClientId;

        Vector3 destino =
            NetworkSpawnManager.Instance.GetSpawnPosition(clientId);

        Quaternion rotacion =
            NetworkSpawnManager.Instance.GetSpawnRotation(clientId);

        player.RespawnOwnerAtClientRpc(
            destino,
            rotacion,
            new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { clientId }
                }
            }
        );

        Debug.Log(
            $"[DEATHZONE] Detectó al jugador {clientId}; respawn solicitado en {destino}."
        );
    }
}