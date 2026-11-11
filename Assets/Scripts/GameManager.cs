using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    [Header("Configuración del Tiempo")]
    public float maxTime = 60f;
    public NetworkVariable<float> timeRemaining =
        new NetworkVariable<float>(60f);

    private bool isGameOver = false;

    [Header("Puntajes Competitivos Sincronizados")]
    public NetworkVariable<int> hostScore =
        new NetworkVariable<int>(0);

    public NetworkVariable<int> clientScore =
        new NetworkVariable<int>(0);

    [Header("Spawn de Jugadores")]
    public Transform[] playerSpawnPoints;

    [Header("Referencias de la UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI pointsText;

    public static string WinnerText = "";
    public static int HostFinalScore = 0;
    public static int ClientFinalScore = 0;

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Time.timeScale = 1f;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            isGameOver = false;
            timeRemaining.Value = maxTime;
            hostScore.Value = 0;
            clientScore.Value = 0;
        }
    }

    private void Update()
    {
        if (IsServer && !isGameOver)
        {
            if (timeRemaining.Value > 0f)
            {
                timeRemaining.Value -= Time.deltaTime;
            }
            else
            {
                timeRemaining.Value = 0f;
                isGameOver = true;

                string winnerMessage;

                if (hostScore.Value > clientScore.Value)
                {
                    winnerMessage = "¡GANÓ EL JUGADOR 1 (HOST)!";
                }
                else if (clientScore.Value > hostScore.Value)
                {
                    winnerMessage = "¡GANÓ EL JUGADOR 2 (CLIENTE)!";
                }
                else
                {
                    winnerMessage = "¡HUBO UN EMPATE!";
                }

                WinnerText = winnerMessage;
                HostFinalScore = hostScore.Value;
                ClientFinalScore = clientScore.Value;

                UpdateGameOverClientRpc(
                    winnerMessage,
                    hostScore.Value,
                    clientScore.Value
                );

                NetworkManager.Singleton.SceneManager.LoadScene(
                    "GameOver",
                    LoadSceneMode.Single
                );
            }
        }

        UpdateTimerUI();
        UpdatePointsUI();
    }

    private void UpdateTimerUI()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(timeRemaining.Value / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining.Value % 60f);

        timerText.text = string.Format(
            "Tiempo: {0:00}:{1:00}",
            minutes,
            seconds
        );
    }

    private void UpdatePointsUI()
    {
        if (pointsText == null) return;

        pointsText.text =
            $"J1 (Host): {hostScore.Value} | J2 (Cliente): {clientScore.Value}";
    }

    public void AddScoreToPlayer(ulong clientId, int points)
    {
        if (!IsServer) return;

        if (clientId == 0)
        {
            hostScore.Value += points;
        }
        else
        {
            clientScore.Value += points;
        }
    }

    [ClientRpc]
    private void UpdateGameOverClientRpc(
        string winner,
        int hostPts,
        int clientPts
    )
    {
        WinnerText = winner;
        HostFinalScore = hostPts;
        ClientFinalScore = clientPts;
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        base.OnNetworkDespawn();
    }
}