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
    [SerializeField] private string gameSceneName = "Juego";

    private void Start()
    {
        if (finalScoreText != null)
        {
            finalScoreText.text =
                $"{GameManager.WinnerText}\n\n" +
                $"Jugador 1 (Host): {GameManager.HostFinalScore} pts\n" +
                $"Jugador 2 (Cliente): {GameManager.ClientFinalScore} pts";
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

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
        Debug.Log($"[GAME OVER] Rejugar. Escena configurada: '{gameSceneName}'.");

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[GAME OVER] No se encontró NetworkManager.");
            return;
        }

        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("[GAME OVER] Solo el host puede reiniciar la partida.");
            return;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(
            gameSceneName,
            LoadSceneMode.Single
        );
    }

    public void VolverAlMenu()
    {
        Debug.Log("[GAME OVER] Apagando Netcode y regresando al menú.");

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene("MainMenu");
    }
}