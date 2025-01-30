using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private LobbyCreateUI lobbyCreateUI;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button quickJoinButton;
    [SerializeField] private Button joinCodeButton;
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private TMP_InputField playerNameInputField;
    [SerializeField] private GameObject connectingUI;
    [SerializeField] private GameObject lobbyMessageUI;
    [SerializeField] private TextMeshProUGUI lobbyMessageUIText;
    [SerializeField] private Button lobbyMessageUICloseButton;
    [SerializeField] private Transform lobbyContainer;
    [SerializeField] private Transform lobbyTemplate;


    private void Start()
    {
        mainMenuButton.onClick.AddListener(() => { SceneManager.LoadScene("MainMenu"); });
        createLobbyButton.onClick.AddListener(() => { lobbyCreateUI.Show(); });
        quickJoinButton.onClick.AddListener(() => { 
            GameLobby.Instance.QuickJoin();
        });
        joinCodeButton.onClick.AddListener(() => { 
            GameLobby.Instance.JoinWithCode(joinCodeInputField.text);
        });
        lobbyMessageUICloseButton.onClick.AddListener(() => {HideLobbyMessageUI(); });

        playerNameInputField.text = GameLobby.Instance.GetPlayerName();
        playerNameInputField.onValueChanged.AddListener((string newText) => { GameLobby.Instance.SetPlayerName(newText); });

        GameLobby.Instance.OnTryingToJoinGame += GameMultiplayer_OnTryingToJoinGame;
        GameLobby.Instance.OnFailedToJoinGame += GameMultiplayer_OnFailedToJoinGame;
        GameLobby.Instance.OnCreateLobbyStarted += GameLobby_OnCreateLobbyStarted;
        GameLobby.Instance.OnCreateLobbyFailed += GameLobby_OnCreateLobbyFailed;
        GameLobby.Instance.OnJoinStarted += GameLobby_OnJoinStarted;
        GameLobby.Instance.OnJoinFailed += GameLobby_OnJoinFailed;
        GameLobby.Instance.OnQuickJoinFailed += GameLobby_OnQuickJoinFailed;
        GameLobby.Instance.OnLobbyListChanged += GameLobby_OnLobbyListChanged;

        UpdateLobbyList(new List<Lobby>());

        HideLobbyMessageUI();
        HideConnectingUI();
        lobbyTemplate.gameObject.SetActive(false);
    }

    private void GameLobby_OnLobbyListChanged(object sender, GameLobby.OnLobbyListChangedEventArgs e)
    {
        UpdateLobbyList(e.lobbyList);
    }

    private void GameMultiplayer_OnTryingToJoinGame(object sender, EventArgs e)
    {
        ShowConnectingUI();
    }

    private void GameLobby_OnQuickJoinFailed(object sender, System.EventArgs e)
    {
        ShowMessage("Could not find a Lobby to Quick Join!");
    }

    private void GameLobby_OnJoinFailed(object sender, System.EventArgs e)
    {
        ShowMessage("Failed to join Lobby!");
    }

    private void GameLobby_OnJoinStarted(object sender, System.EventArgs e)
    {
        ShowMessage("Joining Lobby...");
    }

    private void GameLobby_OnCreateLobbyFailed(object sender, System.EventArgs e)
    {
        ShowMessage("Failed to create Lobby!");
    }

    private void GameLobby_OnCreateLobbyStarted(object sender, System.EventArgs e)
    {
        ShowMessage("Creating Lobby...");
    }

    private void GameMultiplayer_OnFailedToJoinGame(object sender, System.EventArgs e)
    {
        if (NetworkManager.Singleton.DisconnectReason == "")
        {
            ShowMessage("Failed to connect");
        }
        else
        {
            ShowMessage(NetworkManager.Singleton.DisconnectReason);
        }
    }

    private void ShowConnectingUI()
    {
        connectingUI.SetActive(true);
    }

    private void HideConnectingUI()
    {
        connectingUI.SetActive(false);
    }

    private void ShowMessage(string message)
    {
        HideConnectingUI();
        ShowLobbyMessageUI();
        lobbyMessageUIText.text = message;
    }

    private void ShowLobbyMessageUI()
    {
        lobbyMessageUI.SetActive(true);
    }

    private void HideLobbyMessageUI()
    {
        lobbyMessageUI.SetActive(false);
    }

    private void UpdateLobbyList(List<Lobby> lobbyList)
    {
        foreach (Transform child in lobbyContainer)
        {
            if (child == lobbyTemplate) continue;
            Destroy(child.gameObject);
        }

        foreach (Lobby lobby in lobbyList)
        {
            Transform lobbyTransform = Instantiate(lobbyTemplate, lobbyContainer);
            lobbyTransform.gameObject.SetActive(true);
            lobbyTransform.GetComponent<LobbyListSingleUI>().SetLobby(lobby);
        }
    }

    private void OnDestroy()
    {
        GameLobby.Instance.OnTryingToJoinGame -= GameMultiplayer_OnTryingToJoinGame;
        GameLobby.Instance.OnFailedToJoinGame -= GameMultiplayer_OnFailedToJoinGame;
        GameLobby.Instance.OnCreateLobbyStarted -= GameLobby_OnCreateLobbyStarted;
        GameLobby.Instance.OnCreateLobbyFailed -= GameLobby_OnCreateLobbyFailed;
        GameLobby.Instance.OnJoinStarted -= GameLobby_OnJoinStarted;
        GameLobby.Instance.OnJoinFailed -= GameLobby_OnJoinFailed;
        GameLobby.Instance.OnQuickJoinFailed -= GameLobby_OnQuickJoinFailed;
        GameLobby.Instance.OnLobbyListChanged -= GameLobby_OnLobbyListChanged;
    }
}
