using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerTest : MonoBehaviour
{

    [SerializeField] private GameObject duelManagerPrefab;
    public GameObject multiplayerUI;
    public Button startHost;
    public Button startClient;
    public Button startServer;

    public string serverIP = "127.0.0.1"; // Dirección IP del servidor (o "localhost" para pruebas locales)
    public ushort serverPort = 7777;      // Puerto del servidor



    public void Start()
    {
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
           serverIP,
           serverPort);

        startHost.onClick.AddListener(() => StartHost());

        startClient.onClick.AddListener(() => StartClient());

        startServer.onClick.AddListener(() => StartServer());

        NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
    }

    private void Singleton_OnClientConnectedCallback(ulong obj)
    {
        if (NetworkManager.Singleton.IsServer && NetworkManager.Singleton.ConnectedClientsList.Count == 2)
        {
            var duelManagerInstance = Instantiate(duelManagerPrefab);
            duelManagerInstance.GetComponent<NetworkObject>().Spawn();
            duelManagerInstance.GetComponent<DuelManager>().AssignPlayersAndStartDuel(NetworkManager.Singleton.ConnectedClientsList[0].ClientId, NetworkManager.Singleton.ConnectedClientsList[1].ClientId);
        }
    }

    private void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        HideMenu();
    }

    private void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        HideMenu();
    }

    private void StartServer()
    {
        NetworkManager.Singleton.StartServer();
        HideMenu();
    }
    private void HideMenu()
    {
        multiplayerUI.SetActive(false);
    }

}
