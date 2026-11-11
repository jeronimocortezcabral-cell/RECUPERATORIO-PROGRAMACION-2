using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Referencias de UI")]
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private GameObject menuPrincipalPanel;
    [SerializeField] private GameObject pantallaClientePanel;

    [Header("Configuración de Escenas")]
    [SerializeField] private string nombreEscenaJuego = "Juego";

    void Start()
    {
        if (ipInputField != null)
        {
            ipInputField.text = "127.0.0.1";
        }

        if (menuPrincipalPanel != null) menuPrincipalPanel.SetActive(true);
        if (pantallaClientePanel != null) pantallaClientePanel.SetActive(false);
    }

    public void BotonSalir()
    {
        Application.Quit();
    }

    public void IniciarHost()
    {
        if (NetworkManager.Singleton.StartHost())
        {
            Debug.Log("🌐 Host iniciado correctamente. Cambiando de escena...");
            NetworkManager.Singleton.SceneManager.LoadScene(nombreEscenaJuego, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("🚨 No se pudo iniciar el Host.");
        }

        ApagarTodoElMenu();
    }

    public void AbrirPantallaCliente()
    {
        if (menuPrincipalPanel != null) menuPrincipalPanel.SetActive(false);
        if (pantallaClientePanel != null) pantallaClientePanel.SetActive(true);
    }

    public void VolverAlMenuPrincipal()
    {
        if (menuPrincipalPanel != null) menuPrincipalPanel.SetActive(true);
        if (pantallaClientePanel != null) pantallaClientePanel.SetActive(false);
    }

    public void ConectarComoCliente()
    {
        string ipIngresada = ipInputField.text.Trim();

        if (string.IsNullOrEmpty(ipIngresada))
        {
            Debug.LogError("🚨 ¡Escribí una IP antes de conectar!");
            return;
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.ConnectionData.Address = ipIngresada;
        }

        NetworkManager.Singleton.StartClient();
        ApagarTodoElMenu();
    }

    private void ApagarTodoElMenu()
    {
        if (menuPrincipalPanel != null) menuPrincipalPanel.SetActive(false);
        if (pantallaClientePanel != null) pantallaClientePanel.SetActive(false);
    }
}