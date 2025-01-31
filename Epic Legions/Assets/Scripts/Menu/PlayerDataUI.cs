using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerDataUI : MonoBehaviour
{
    [SerializeField] private int playerIndex;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private GameObject isReady;

    [SerializeField] private List<GameObject> visualGameObject;

    private void Awake()
    {
        GameLobby.Instance.OnPlayerDataNetworkListChanged += Instance_OnPlayerDataNetworkListChanged;
        GameLobby.Instance.OnReadyChanged += PlayerReady_OnReadyChanged;
        UpdatePlayer();
    }
    private void PlayerReady_OnReadyChanged(object sender, System.EventArgs e)
    {
        UpdatePlayer();
    }

    private void Instance_OnPlayerDataNetworkListChanged(object sender, System.EventArgs e)
    {
        UpdatePlayer();
    }

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
        foreach (GameObject go in visualGameObject)
        {
            go.SetActive(true);
        }
    }

    private void Hide()
    {
        foreach (GameObject go in visualGameObject)
        {
            go.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        GameLobby.Instance.OnPlayerDataNetworkListChanged -= Instance_OnPlayerDataNetworkListChanged;
    }
}
