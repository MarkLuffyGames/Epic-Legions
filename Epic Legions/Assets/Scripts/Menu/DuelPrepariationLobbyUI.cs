using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class DuelPrepariationLobbyUI : MonoBehaviour
{
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI lobbyCodeText;

    private void Awake()
    {
        readyButton.onClick.AddListener(() => { GameLobby.instance.SetPlayerReadyServerRpc(NetworkManager.Singleton.LocalClientId); });
    }

    private void Start()
    {
        Lobby lobby = GameLobby.instance.GetLobby();

        lobbyNameText.text = lobby.Name;
        lobbyCodeText.text = $"Code: {lobby.LobbyCode}";   
    }
}
