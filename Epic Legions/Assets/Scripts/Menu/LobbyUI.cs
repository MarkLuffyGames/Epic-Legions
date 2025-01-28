using TMPro;
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

    private void Start()
    {
        mainMenuButton.onClick.AddListener(() => { SceneManager.LoadScene("MainMenu"); });
        createLobbyButton.onClick.AddListener(() => { lobbyCreateUI.Show(); });
        quickJoinButton.onClick.AddListener(() => { GameLobby.instance.QuickJoin(); });
        joinCodeButton.onClick.AddListener(() => { GameLobby.instance.JoinWithCode(joinCodeInputField.text); });

        playerNameInputField.text = GameLobby.instance.GetPlayerName();
        playerNameInputField.onValueChanged.AddListener((string newText) => { GameLobby.instance.SetPlayerName(newText); });
    }
}
