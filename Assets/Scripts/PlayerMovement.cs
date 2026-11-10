using System.Collections;
using Unity.Multiplayer.Center.NetcodeForGameObjectsExample;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

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

        // Se mantiene desactivado durante la inicialización.
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
        // Solo el dueño lee controles y mueve al jugador.
        if (!IsOwner) return;

        if (skipAction.action.WasPressedThisFrame())
        {
            RequestSkipRoundServerRpc();
        }

        Vector2 input = moveAction.action.ReadValue<Vector2>();
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        Vector3 movement = move * speed;

        if (characterController == null || !characterController.enabled)
        {
            return;
        }

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
            UnityEngine.SceneManagement.LoadSceneMode.Single
        );
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // La copia del jugador cliente en el servidor también necesita
        // un collider activo para que el servidor detecte la DeathZone.
        if (IsServer && !IsOwner && characterController != null)
        {
            characterController.enabled = true;
        }

        // Solo el dueño coloca su jugador en el spawn correspondiente.
        if (IsOwner)
        {
            StartCoroutine(SelfTeleportSafelyRoutine());
        }
    }

    private IEnumerator SelfTeleportSafelyRoutine()
    {
        // Esperamos a que la escena y la red terminen de inicializarse.
        yield return null;

        Vector3 destino = new Vector3(0f, 2f, 0f);

        string nombreSpawn = OwnerClientId == 0
            ? "SpawnPoint_Host"
            : "SpawnPoint_Client";

        GameObject puntoEncontrado = GameObject.Find(nombreSpawn);

        if (puntoEncontrado != null)
        {
            destino = puntoEncontrado.transform.position;
        }
        else
        {
            Debug.LogWarning(
                $"No se encontró '{nombreSpawn}'. Se usará la posición de respaldo."
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

        transform.SetPositionAndRotation(destino, Quaternion.identity);
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
        ClientRpcParams clientRpcParams = default
    )
    {
        // El RPC llega al cliente objetivo; solo el dueño mueve su jugador.
        if (!IsOwner) return;

        StartCoroutine(EjecutarTeletransporteDesdeMuerte(destino));
    }

    private IEnumerator EjecutarTeletransporteDesdeMuerte(Vector3 destino)
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

        transform.SetPositionAndRotation(destino, Quaternion.identity);
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