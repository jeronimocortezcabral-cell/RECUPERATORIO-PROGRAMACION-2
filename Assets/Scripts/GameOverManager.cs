using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.UI;

public class GameOverManager : MonoBehaviour
{
    [Header("Referencias UI")]
    public TextMeshProUGUI finalScoreText;
    public Button playAgainButton;
    public Button menuButton;

    [Header("Configuración")]
    [SerializeField] private string gameSceneName = "GameScene"; // Nombre exacto de tu escena de juego

    private void Start()
    {
        if (finalScoreText != null)
        {
            finalScoreText.text = $"{GameManager.WinnerText}\n\n" +
                                  $"Jugador 1 (Host): {GameManager.HostFinalScore} pts\n" +
                                  $"Jugador 2 (Cliente): {GameManager.ClientFinalScore} pts";
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Auto-enlace de seguridad para los botones
        if (playAgainButton != null)
        {
            playAgainButton.onClick.RemoveAllListeners();
            playAgainButton.onClick.AddListener(OnPlayAgainClicked);
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(VolverAlMenu);
        }
    }

    public void OnPlayAgainClicked()
    {
        Debug.Log("🔄 [GAME OVER] Botón de Rejugar pulsado.");

        // Verificamos si este jugador es el Host (Servidor)
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            Debug.Log($"🔄 [SERVIDOR] Reiniciando partida. Cargando escena: {gameSceneName}");
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.Log("⚠️ Solo el Host puede reiniciar la partida. (O puedes programar que mande al menú)");
            // Opcional: si es cliente, puedes mandarlo al menú o mostrarle un aviso
            // VolverAlMenu();
        }
    }

    public void VolverAlMenu()
    {
        Debug.Log("🔌 [GAME OVER] Apagando Netcode y regresando al menú principal...");

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene("MainMenu");
    }
}