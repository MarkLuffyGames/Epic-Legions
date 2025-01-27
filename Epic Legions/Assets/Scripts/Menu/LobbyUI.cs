using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private GameLobby gameLobby;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button quickJoinButton;

    private void Awake()
    {
        mainMenuButton.onClick.AddListener(() => { SceneManager.LoadScene("MainMenu"); });
        createLobbyButton.onClick.AddListener(() => { gameLobby.CreateLobby("LobbyName", false); });
        quickJoinButton.onClick.AddListener(() => { gameLobby.QuickJoin(); });
    }
}
