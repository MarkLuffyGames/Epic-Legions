using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLobby : NetworkBehaviour
{
    public static GameLobby instance { get; private set; }

    [SerializeField] private GameObject duelManagerPrefab;

    private Lobby joinedLobby;

    private Dictionary<ulong, bool> playerReady = new Dictionary<ulong, bool>();
    private Dictionary<int, ulong> playerID = new Dictionary<int, ulong>();
    private string playerName;

    private void Awake()
    {
        if(instance == null) instance = this;

        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;

        GetPlayerNameMultiplayer();

        StartCoroutine(HandleHeartbeat());
    }

    private async void GetPlayerNameMultiplayer()
    {
        playerName = PlayerPrefs.GetString("PlayerName", await AuthenticationService.Instance.GetPlayerNameAsync());
    }

    public string GetPlayerName()
    {
        return playerName;
    }

    public void SetPlayerName(string playerName)
    {
        this.playerName = playerName;

        PlayerPrefs.SetString("PlayerName", playerName);
    }

    private IEnumerator HandleHeartbeat()
    {
        while (IsLobbyHost())
        {
            yield return new WaitForSeconds(15);
            LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
        }
    }

    private bool IsLobbyHost()
    {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Detecta cuando se carga una escena específica
        if (scene.name == "GameScene") 
        {
            Debug.Log("GameScene cargada. Instanciando Duel Manager...");

            // Instanciar el Duel Manager en el servidor
            if (NetworkManager.Singleton.IsServer)
            {
                var duelManagerInstance = Instantiate(duelManagerPrefab);
                duelManagerInstance.GetComponent<NetworkObject>().Spawn();
                duelManagerInstance.GetComponent<DuelManager>().RegisterPlayer(playerID[1], playerID[2]);
            }
        }
    }

    public async void CreateLobby(string roomName, bool isPrivate)
    {
        try
        {
            joinedLobby = await LobbyService.Instance.CreateLobbyAsync(roomName, 2, new CreateLobbyOptions { IsPrivate = isPrivate });
            NetworkManager.Singleton.StartHost();
        }
        catch(LobbyServiceException ex) 
        {
            Debug.LogException(ex);
        }
    }

    public async void QuickJoin()
    {
        try
        {
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            NetworkManager.Singleton.StartClient();
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogException(ex);
        }
        
    }

    public async void JoinWithCode(string lobbyCode)
    {
        try
        {
            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
            NetworkManager.Singleton.StartClient();
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogException(ex);
        }
        
    }

    private void Singleton_OnClientConnectedCallback(ulong clientID)
    {
        if(clientID == NetworkManager.Singleton.LocalClientId) SceneManager.LoadScene("DuelPreparationLobby");
    }

    private void Singleton_OnClientDisconnectCallback(ulong obj)
    {
        Debug.Log(NetworkManager.Singleton.DisconnectReason);
    }

    public Lobby GetLobby()
    {
        return joinedLobby;
    }

    public bool AreAllTrue(Dictionary<ulong, bool> clientStatus)
    {
        // Verificar si todos los valores son true
        return clientStatus.Values.All(status => status);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerReadyServerRpc(ulong clientID)
    {
        playerReady[clientID] = true;
        if (clientID == NetworkManager.Singleton.LocalClientId)
        {
            playerID[1] = clientID;
        }
        else 
        {
            playerID[2] = clientID;
        }

        if (playerReady.Count == 2 && AreAllTrue(playerReady))
        {
            StartDuelClientRpc();
        }
    }

    [ClientRpc]
    public void StartDuelClientRpc()
    {
        SceneManager.LoadScene("GameScene");
    }
}
