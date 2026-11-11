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

        // La copia del jugador cliente en el servidor debe conservar
        // su collider activo para detectar la DeathZone.
        if (IsServer && !IsOwner)
        {
            EnableServerCollision();
        }

        // Posicionamiento inicial del dueño cuando aparece por primera vez.
        if (IsOwner)
        {
            StartCoroutine(InitialSpawnRoutine());
        }
    }

    public void EnableServerCollision()
    {
        if (!IsServer || characterController == null)
        {
            return;
        }

        characterController.enabled = true;
    }

    private IEnumerator InitialSpawnRoutine()
    {
        // Espera brevemente a que el NetworkSpawnManager esté disponible.
        int framesWaited = 0;

        while (NetworkSpawnManager.Instance == null && framesWaited < 120)
        {
            framesWaited++;
            yield return null;
        }

        if (NetworkSpawnManager.Instance == null)
        {
            Debug.LogError(
                $"[SPAWN] No se encontró NetworkSpawnManager para el jugador {OwnerClientId}."
            );
            yield break;
        }

        Vector3 destino =
            NetworkSpawnManager.Instance.GetSpawnPosition(OwnerClientId);

        Quaternion rotacion =
            NetworkSpawnManager.Instance.GetSpawnRotation(OwnerClientId);

        yield return TeleportOwnerRoutine(destino, rotacion);

        Debug.Log(
            $"[SPAWN INICIAL] Jugador {OwnerClientId} colocado en {destino}."
        );
    }

    [ClientRpc]
    public void RespawnOwnerAtClientRpc(
        Vector3 destino,
        Quaternion rotacion,
        ClientRpcParams clientRpcParams = default
    )
    {
        if (!IsOwner)
        {
            return;
        }

        StartCoroutine(TeleportOwnerRoutine(destino, rotacion));
    }

    private IEnumerator TeleportOwnerRoutine(
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
    }

    [ClientRpc]
    public void TriggerSafeTeleportClientRpc(
        Vector3 destino,
        Quaternion rotacion,
        ClientRpcParams clientRpcParams = default
    )
    {
        if (!IsOwner)
        {
            return;
        }

        StartCoroutine(TeleportOwnerRoutine(destino, rotacion));
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