using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLobby : NetworkBehaviour
{

    public event EventHandler OnTryingToJoinGame;
    public event EventHandler OnFailedToJoinGame;

    public event EventHandler OnCreateLobbyStarted;
    public event EventHandler OnCreateLobbyFailed;
    public event EventHandler OnJoinStarted;
    public event EventHandler OnQuickJoinFailed;
    public event EventHandler OnJoinFailed;
    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
    public class OnLobbyListChangedEventArgs : EventArgs
    {
        public List<Lobby> lobbyList;
    }
    public static GameLobby Instance { get; private set; }

    [SerializeField] private GameObject duelManagerPrefab;

    private Lobby joinedLobby;

    private Dictionary<ulong, bool> playerReady = new Dictionary<ulong, bool>();
    private Dictionary<int, ulong> playerID = new Dictionary<int, ulong>();
    private string playerName;

    private void Awake()
    {
        if (Instance == null) 
        { 
            Instance = this; 
        }
        else
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;

        GetPlayerNameMultiplayer();

        StartCoroutine(HandleHeartbeat());
        StartCoroutine(HandlePeriodicListLobbies());
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

    private IEnumerator HandlePeriodicListLobbies()
    {
        while (true)
        {
            yield return new WaitForSeconds(3);
            if (joinedLobby == null &&
            UnityServices.State == ServicesInitializationState.Initialized &&
            AuthenticationService.Instance.IsSignedIn &&
            SceneManager.GetActiveScene().name == "LobbyScene")
            {
                ListLobbies();
            }
        }
    }

    private IEnumerator HandleHeartbeat()
    {
        while (true)
        {
            yield return new WaitForSeconds(15);
            if (IsLobbyHost())
            {
                LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }
        }
    }

    private bool IsLobbyHost()
    {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    private async void ListLobbies()
    {
        try
        {
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions
            {
                Filters = new List<QueryFilter> {
                  new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
             }
            };
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(queryLobbiesOptions);

            OnLobbyListChanged?.Invoke(this, new OnLobbyListChangedEventArgs
            {
                lobbyList = queryResponse.Results
            });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
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

                DeleteLobby();
            }
        }
    }

    public async void CreateLobby(string roomName, bool isPrivate)
    {
        OnCreateLobbyStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            joinedLobby = await LobbyService.Instance.CreateLobbyAsync(roomName, 2, new CreateLobbyOptions { IsPrivate = isPrivate });
            NetworkManager.Singleton.StartHost();
        }
        catch(LobbyServiceException ex) 
        {
            OnCreateLobbyFailed?.Invoke(this, EventArgs.Empty);
            Debug.LogException(ex);
        }
    }

    public async void QuickJoin()
    {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            OnTryingToJoinGame?.Invoke(this, EventArgs.Empty);
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            NetworkManager.Singleton.StartClient();
        }
        catch (LobbyServiceException ex)
        {
            OnQuickJoinFailed?.Invoke(this, EventArgs.Empty);
            Debug.LogException(ex);
        }
        
    }

    public async void JoinWithId(string lobbyId)
    {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
            NetworkManager.Singleton.StartClient();
        }
        catch (LobbyServiceException e)
        {
            OnJoinFailed?.Invoke(this, EventArgs.Empty);
            Debug.Log(e);
        }
    }

    public async void JoinWithCode(string lobbyCode)
    {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
            NetworkManager.Singleton.StartClient();
        }
        catch (LobbyServiceException ex)
        {
            OnJoinFailed?.Invoke(this, EventArgs.Empty);
            Debug.LogException(ex);
        }
        
    }

    private void Singleton_OnClientConnectedCallback(ulong clientID)
    {
        if(clientID == NetworkManager.Singleton.LocalClientId) SceneManager.LoadScene("DuelPreparationLobby");
    }

    private void Singleton_OnClientDisconnectCallback(ulong obj)
    {
        OnFailedToJoinGame?.Invoke(this, EventArgs.Empty);
        Debug.Log(NetworkManager.Singleton.DisconnectReason);
    }

    public Lobby GetLobby()
    {
        return joinedLobby;
    }

    public async void DeleteLobby()
    {
        if (joinedLobby != null)
        {
            try
            {
                foreach (var player in joinedLobby.Players)
                {
                    if(player.Id != joinedLobby.HostId) KickPlayer(player.Id);
                }
                await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);

                joinedLobby = null;
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    public async void LeaveLobby()
    {
        if (joinedLobby != null)
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);

                joinedLobby = null;
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    public async void KickPlayer(string playerId)
    {
        if (IsLobbyHost())
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
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
