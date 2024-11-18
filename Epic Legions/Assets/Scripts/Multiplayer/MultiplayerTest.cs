using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerTest : MonoBehaviour
{

    public GameObject multiplayerUI;
    public Button startHost;
    public Button startClient;
    public Button startServer;
    

    public void Start()
    {
        startHost.onClick.AddListener(() => StartHost());

        startClient.onClick.AddListener(() => StartClient());

        startServer.onClick.AddListener(() => StartServer());
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
