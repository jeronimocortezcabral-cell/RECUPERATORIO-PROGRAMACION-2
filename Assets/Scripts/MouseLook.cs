using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : NetworkBehaviour
{
    [Header("Controles (Input System)")]
    [SerializeField] private InputActionProperty lookAction;

    [Header("Configuración de Vista")]
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private Transform playerBody;

    private float xRotation = 0f;

    void Start()
    {
        if (!IsOwner)
        {
            if (GetComponent<Camera>() != null) GetComponent<Camera>().enabled = false;
            if (GetComponent<AudioListener>() != null) GetComponent<AudioListener>().enabled = false;
            return;
        }

        if (playerBody == null)
        {
            Debug.LogError("🚨 ¡FALTA ASIGNAR! Arrastrá el objeto padre 'Player' a la casilla PlayerBody de la cámara.", this);
        }

        Cursor.lockState = CursorLockMode.Locked;
        lookAction.action.Enable();
    }

    void Update()
    {
        if (!IsOwner) return;

        Vector2 lookInput = lookAction.action.ReadValue<Vector2>();

        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            lookAction.action.Disable();
        }
    }
}