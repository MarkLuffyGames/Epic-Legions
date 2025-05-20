using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button singlePlayerButton;
    [SerializeField] private Button multiplayerButton;
    [SerializeField] private Button exitButton;

    private void Awake()
    {
        Application.targetFrameRate = 1000;

        InitializeUnityAuthentication();
        singlePlayerButton.onClick.AddListener(() =>
        {
            StartSinglePlayer();
        });

        multiplayerButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("LobbyScene");
        });

        exitButton.onClick.AddListener(() =>
        {
            Application.Quit();
        });

        if (NetworkManager.Singleton != null)
        {
            Destroy(NetworkManager.Singleton.gameObject);
        }

        if (GameLobby.Instance != null)
        {
            Destroy(GameLobby.Instance.gameObject);
        }
    }

    private async void InitializeUnityAuthentication()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            InitializationOptions initializationOptions = new InitializationOptions();
            //initializationOptions.SetProfile(Random.Range(0, 10000).ToString());

            await UnityServices.InitializeAsync(initializationOptions);

            await SignInAnonymouslyAsync();
        }
    }

    async Task SignInAnonymouslyAsync()
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Sign in anonymously succeeded!");

            // Shows how to get the playerID
            Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}");

        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
    }

    [SerializeField] private GameObject duelManagerPrefab;

    [SerializeField] private List<int> deckCardIds;
    private void StartSinglePlayer()
    {
        foreach(var card in CardDatabase.allCards)
        {
            deckCardIds.Add(32);
        }
        var duelManagerInstance = Instantiate(duelManagerPrefab);
        duelManagerInstance.GetComponent<DuelManager>().AssignPlayersAndStartDuel(deckCardIds.ToArray(), deckCardIds.ToArray());

        SceneManager.LoadScene("GameScene");
    }

}
