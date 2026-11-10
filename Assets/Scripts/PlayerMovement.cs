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
    [SerializeField] private InputActionProperty skipAction; // Atajo para presionar la X y saltear ronda

    [Header("Configuración de Movimiento")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1.5f;

    private CharacterController characterController;
    private Vector3 velocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        // Blindaje: Nos aseguramos de que nazca apagado para evitar que las físicas 
        // lo detecten en (0,0,0) al recargar la escena o presionar "Rejugar".
        if (characterController != null)
        {
            characterController.enabled = false;
        }
    }

    void Start()
    {
        if (!IsOwner) return;

        moveAction.action.Enable();
        jumpAction.action.Enable();
        skipAction.action.Enable();
    }

    void Update()
    {
        if (!IsOwner) return;

        // 1. Detectar si se presiona la X para ir al GameOver de inmediato
        if (skipAction.action.WasPressedThisFrame())
        {
            RequestSkipRoundServerRpc();
        }

        // 2. Movimiento horizontal
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        Vector3 movement = move * speed;

        if (characterController != null && characterController.enabled)
        {
            // 3. Comprobar suelo y salto
            if (characterController.isGrounded)
            {
                if (velocity.y < 0)
                {
                    velocity.y = -2f;
                }

                if (jumpAction.action.WasPressedThisFrame())
                {
                    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }

            // 4. Gravedad
            velocity.y += gravity * Time.deltaTime;
            movement.y = velocity.y;

            // 5. Mover controlador
            characterController.Move(movement * Time.deltaTime);
        }
    }

    [ServerRpc]
    private void RequestSkipRoundServerRpc()
    {
        Debug.Log("⚡ [SERVER] Salteando ronda con la tecla X hacia GameOver...");
        NetworkManager.Singleton.SceneManager.LoadScene("GameOver", LoadSceneMode.Single);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Solo el dueño del objeto ejecuta su propia rutina de auto-posicionamiento seguro
        if (IsOwner)
        {
            StartCoroutine(SelfTeleportSafelyRoutine());
        }
    }

    private IEnumerator SelfTeleportSafelyRoutine()
    {
        yield return null; // Esperamos un frame para que la red y la escena se estabilicen

        Vector3 destino = new Vector3(0f, 2f, 0f); // Posición por defecto de respaldo

        // Buscamos el punto de spawn automáticamente por nombre en la escena actual
        string nombreSpawn = (OwnerClientId == 0) ? "SpawnPoint_Host" : "SpawnPoint_Client";
        GameObject puntoEncontrado = GameObject.Find(nombreSpawn);

        if (puntoEncontrado != null)
        {
            destino = puntoEncontrado.transform.position;
        }
        else
        {
            Debug.LogWarning($"⚠️ No se encontró el objeto '{nombreSpawn}' en la escena. Usando posición por defecto.");
        }

        ClientNetworkTransform clientTransform = GetComponent<ClientNetworkTransform>();

        // 1. Apagamos los componentes que interfieren con las físicas iniciales
        if (clientTransform != null) clientTransform.enabled = false;
        if (characterController != null) characterController.enabled = false;

        // 2. Colocamos al jugador en el punto exacto y reseteamos la inercia de caída
        transform.position = destino;
        transform.rotation = Quaternion.identity;
        velocity = Vector3.zero; // Vital para que no arrastre velocidad hacia el abismo

        // 3. Esperamos un frame para que Unity procese el cambio en el mundo
        yield return null;

        // 4. Reactivamos todo y encendemos el CharacterController con seguridad
        if (characterController != null) characterController.enabled = true;
        if (clientTransform != null) clientTransform.enabled = true;

        Debug.Log($"[SPAWN SEGURO] Jugador {OwnerClientId} colocado en {destino} con CharacterController activo.");
    }

    [ClientRpc]
    public void TriggerSafeTeleportClientRpc(Vector3 destino, ClientRpcParams clientRpcParams = default)
    {
        if (IsOwner)
        {
            StartCoroutine(EjecutarTeletransporteDesdeMuerte(destino));
        }
    }

    private System.Collections.IEnumerator EjecutarTeletransporteDesdeMuerte(Vector3 destino)
    {
        ClientNetworkTransform clientTransform = GetComponent<ClientNetworkTransform>();

        // 1. Apagamos componentes que interfieren
        if (clientTransform != null) clientTransform.enabled = false;
        if (characterController != null) characterController.enabled = false;

        // 2. Teletransportamos y matamos la inercia de caída
        transform.position = destino;
        transform.rotation = Quaternion.identity;
        velocity = Vector3.zero;

        // 3. Esperamos un frame
        yield return null;

        // 4. Volvemos a encender todo con seguridad
        if (characterController != null) characterController.enabled = true;
        if (clientTransform != null) clientTransform.enabled = true;

        Debug.Log("✅ [RESPAWN POR DEATHZONE] Jugador recolocado con éxito en tierra firme.");
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            moveAction.action.Disable();
            jumpAction.action.Disable();
            skipAction.action.Disable();
        }
    }
}