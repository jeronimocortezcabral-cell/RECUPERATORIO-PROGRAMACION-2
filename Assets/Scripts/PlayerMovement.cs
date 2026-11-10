using System.Collections;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Controles (Input System)")]
    [SerializeField] private InputActionProperty moveAction;
    [SerializeField] private InputActionProperty jumpAction;
    [SerializeField] private InputActionProperty skipAction;

    [Header("Configuración de Movimiento")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1.5f;

    private CharacterController characterController;
    private Vector3 velocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        // Se activa después de que el jugador se ubique en su spawn.
        if (characterController != null)
        {
            characterController.enabled = false;
        }
    }

    private void Start()
    {
        if (!IsOwner) return;

        moveAction.action.Enable();
        jumpAction.action.Enable();
        skipAction.action.Enable();
    }

    private void Update()
    {
        // Solo el dueño lee controles y mueve a este jugador.
        if (!IsOwner) return;

        if (skipAction.action.WasPressedThisFrame())
        {
            RequestSkipRoundServerRpc();
        }

        if (characterController == null || !characterController.enabled)
        {
            return;
        }

        Vector2 input = moveAction.action.ReadValue<Vector2>();

        Vector3 move =
            transform.right * input.x +
            transform.forward * input.y;

        Vector3 movement = move * speed;

        if (characterController.isGrounded)
        {
            if (velocity.y < 0f)
            {
                velocity.y = -2f;
            }

            if (jumpAction.action.WasPressedThisFrame())
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        velocity.y += gravity * Time.deltaTime;
        movement.y = velocity.y;

        characterController.Move(movement * Time.deltaTime);
    }

    [ServerRpc]
    private void RequestSkipRoundServerRpc()
    {
        Debug.Log("[SERVER] Salteando ronda hacia GameOver...");

        NetworkManager.Singleton.SceneManager.LoadScene(
            "GameOver",
            LoadSceneMode.Single
        );
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // La copia del jugador cliente en el servidor necesita
        // el CharacterController activo para detectar la DeathZone.
        if (IsServer && !IsOwner && characterController != null)
        {
            characterController.enabled = true;
        }

        if (IsOwner)
        {
            StartCoroutine(SelfTeleportSafelyRoutine());
        }
    }

    private IEnumerator SelfTeleportSafelyRoutine()
    {
        // Espera a que la escena termine de inicializarse.
        yield return null;

        Vector3 destino = new Vector3(0f, 2f, 0f);
        Quaternion rotacion = Quaternion.identity;

        if (NetworkSpawnManager.Instance != null)
        {
            destino = NetworkSpawnManager.Instance.GetSpawnPosition(
                OwnerClientId
            );

            rotacion = NetworkSpawnManager.Instance.GetSpawnRotation(
                OwnerClientId
            );
        }
        else
        {
            Debug.LogError(
                $"[SPAWN SEGURO] No se encontró NetworkSpawnManager para el jugador {OwnerClientId}. Se usará la posición de respaldo."
            );
        }

        ClientNetworkTransform clientTransform =
            GetComponent<ClientNetworkTransform>();

        if (clientTransform != null)
        {
            clientTransform.enabled = false;
        }

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        transform.SetPositionAndRotation(destino, rotacion);
        velocity = Vector3.zero;

        yield return null;

        if (characterController != null)
        {
            characterController.enabled = true;
        }

        if (clientTransform != null)
        {
            clientTransform.enabled = true;
        }

        Debug.Log(
            $"[SPAWN SEGURO] Jugador {OwnerClientId} colocado en {destino}."
        );
    }

    [ClientRpc]
    public void TriggerSafeTeleportClientRpc(
        Vector3 destino,
        Quaternion rotacion,
        ClientRpcParams clientRpcParams = default
    )
    {
        if (!IsOwner) return;

        StartCoroutine(
            EjecutarTeletransporteDesdeMuerte(destino, rotacion)
        );
    }

    private IEnumerator EjecutarTeletransporteDesdeMuerte(
        Vector3 destino,
        Quaternion rotacion
    )
    {
        ClientNetworkTransform clientTransform =
            GetComponent<ClientNetworkTransform>();

        if (clientTransform != null)
        {
            clientTransform.enabled = false;
        }

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        transform.SetPositionAndRotation(destino, rotacion);
        velocity = Vector3.zero;

        yield return null;

        if (characterController != null)
        {
            characterController.enabled = true;
        }

        if (clientTransform != null)
        {
            clientTransform.enabled = true;
        }

        Debug.Log(
            $"[RESPAWN] Jugador {OwnerClientId} recolocado en {destino}."
        );
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            moveAction.action.Disable();
            jumpAction.action.Disable();
            skipAction.action.Disable();
        }

        base.OnNetworkDespawn();
    }
}