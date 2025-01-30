using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerDataUI : MonoBehaviour
{
    [SerializeField] private int playerIndex;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private GameObject isReady;

    private void UpdatePlayer()
    {
        if (GameLobby.Instance.IsPlayerIndexConnected(playerIndex))
        {
            Show();

            PlayerData playerData = GameLobby.Instance.GetPlayerDataFromPlayerIndex(playerIndex);

            isReady.SetActive(GameLobby.Instance.IsPlayerReady(playerData.clientId));

            playerNameText.text = playerData.playerName.ToString();
        }
        else
        {
            Hide();
        }
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

}
