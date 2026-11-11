using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class NetworkSpawnManager : NetworkBehaviour
{
    public static NetworkSpawnManager Instance { get; private set; }

    [Header("Referencias de Posiciones")]
    [SerializeField] private Transform hostSpawnPoint;
    [SerializeField] private Transform clientSpawnPoint;

    [Header("Escena de partida")]
    [SerializeField] private string gameSceneName = "Juego";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning(
                "[NetworkSpawnManager] Hay otra instancia. Se eliminará esta."
            );

            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted +=
                OnLoadEventCompleted;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -=
                OnLoadEventCompleted;
        }

        base.OnNetworkDespawn();
    }

    private void OnLoadEventCompleted(
        string sceneName,
        LoadSceneMode loadSceneMode,
        List<ulong> clientsCompleted,
        List<ulong> clientsTimedOut
    )
    {
        if (!IsServer || sceneName != gameSceneName)
        {
            return;
        }

        Debug.Log(
            $"[NetworkSpawnManager] Terminó de cargar '{sceneName}'. Recolocando jugadores."
        );

        StartCoroutine(RespawnPlayersAfterSceneLoad());
    }

    private IEnumerator RespawnPlayersAfterSceneLoad()
    {
        yield return null;

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError(
                "[NetworkSpawnManager] No se encontró NetworkManager al recolocar jugadores."
            );
            yield break;
        }

        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            NetworkObject playerObject = client.PlayerObject;

            if (playerObject == null)
            {
                Debug.LogWarning(
                    $"[NetworkSpawnManager] El cliente {client.ClientId} todavía no tiene PlayerObject."
                );
                continue;
            }

            PlayerMovement player =
                playerObject.GetComponent<PlayerMovement>();

            if (player == null)
            {
                Debug.LogError(
                    $"[NetworkSpawnManager] El PlayerObject del cliente {client.ClientId} no tiene PlayerMovement."
                );
                continue;
            }

            Vector3 destino = GetSpawnPosition(client.ClientId);
            Quaternion rotacion = GetSpawnRotation(client.ClientId);

            player.EnableServerCollision();

            player.RespawnOwnerAtClientRpc(
                destino,
                rotacion,
                new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] { client.ClientId }
                    }
                }
            );

            Debug.Log(
                $"[NetworkSpawnManager] Respawn solicitado para {client.ClientId} en {destino}."
            );
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
                ? "[NetworkSpawnManager] Falta asignar Host Spawn Point."
                : "[NetworkSpawnManager] Falta asignar Client Spawn Point."
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