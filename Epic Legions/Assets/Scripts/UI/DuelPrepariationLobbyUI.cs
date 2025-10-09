using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DuelPrepariationLobbyUI : MonoBehaviour
{
    [SerializeField] private Button readyButton;
    [SerializeField] private Button returnButton;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI lobbyCodeText;
    [SerializeField] private TextMeshProUGUI countdown;

    [SerializeField] private GameObject lobbyMessageUI;
    [SerializeField] private TextMeshProUGUI lobbyMessageUIText;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        readyButton.onClick.AddListener(() => { 
            GameLobby.Instance.SetPlayerReadyServerRpc(NetworkManager.Singleton.LocalClientId);
        });
        returnButton.onClick.AddListener(() => {
            if (NetworkManager.Singleton.IsServer) 
            {
                GameLobby.Instance.DeleteLobby();
            }
            else
            {
                GameLobby.Instance.LeaveLobby();
            }
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("MainMenu");
        });
        closeButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("MainMenu");
        });

        lobbyMessageUI.SetActive(false);
        countdown.enabled = false;

        NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;
        GameLobby.Instance.OnReadyToStart += Instance_OnReadyToStart;
    }

    private void Instance_OnReadyToStart(object sender, EventArgs e)
    {
        readyButton.onClick.RemoveAllListeners();
        countdown.enabled = true;
        StartCoroutine(CountDown());
    }

    IEnumerator CountDown()
    {
        int count = 3;
        for (int i = 0; i < count; i++)
        {
            countdown.text = (count - i).ToString();
            yield return new WaitForSeconds(1);
        }
        Loader.LoadScene("GameScene", false);
    }

    private void Singleton_OnClientDisconnectCallback(ulong clientID)
    {
        if(clientID == NetworkManager.Singleton.LocalClientId)
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
    }

    private void ShowMessage(string message)
    {
        lobbyMessageUI.SetActive(true);
        lobbyMessageUIText.text = message;
    }

    private void Start()
    {
        Lobby lobby = GameLobby.Instance.GetLobby();

        lobbyNameText.text = lobby.Name;
        lobbyCodeText.text = $"Code: {lobby.LobbyCode}";   
    }

    private void OnDestroy()
    {
        if(NetworkManager.Singleton != null)NetworkManager.Singleton.OnClientDisconnectCallback -= Singleton_OnClientDisconnectCallback;
    }

}
