using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
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
    [SerializeField] private Button collectionButton;
    [SerializeField] private Button tutorialButton;
    [SerializeField] private TextMeshProUGUI versionText;
    [SerializeField] private Animator imageAnimator;
    [SerializeField] private Animator cameraAnimator;
    private void Awake()
    {
        versionText.text = $"v{Application.version}";
        Application.targetFrameRate = 1000;

        InitializeUnityAuthentication();
        singlePlayerButton.onClick.AddListener(() =>
        {
            StartAnimation();
            Invoke(nameof(StartSinglePlayer), 0.6f);
        });

        multiplayerButton.onClick.AddListener(() =>
        {
            StartAnimation();
            Invoke(nameof(StartCasualMultiplayer), 0.6f);
            
        });

        collectionButton.onClick.AddListener(() =>
        {
            StartAnimation();
            Invoke(nameof(Collection), 0.6f);
        });

        tutorialButton.onClick.AddListener(() =>
        {
            StartAnimation();
            Invoke(nameof(StartTutorial), 0.6f);
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
        Loader.LoadScene("GameScene", true);
    }
    private void Collection()
    {
        Loader.LoadScene("CollectionScene", true);
    }

    private void StartCasualMultiplayer()
    {
        Loader.LoadScene("LobbyScene", true);
    }

    private void StartTutorial()
    {
        Loader.LoadScene("TutorialScene", true);
    }

    private void StartAnimation()
    {
        imageAnimator.SetTrigger("Activate");
        cameraAnimator.SetTrigger("Activate");
    }
}
