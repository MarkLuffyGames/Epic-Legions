using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button playButton;

    private void Awake()
    {
        InitializeUnityAuthentication();
        playButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("LobbyScene");
        });
        NetworkManager.OnInstantiated += NetworkManager_OnInstantiated;
    }

    private void NetworkManager_OnInstantiated(NetworkManager obj)
    {
        if(NetworkManager.Singleton != null)
        {
            Destroy(obj.gameObject);
        }
    }

    private async void InitializeUnityAuthentication()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            InitializationOptions initializationOptions = new InitializationOptions();
            initializationOptions.SetProfile(Random.Range(0, 10000).ToString());

            await UnityServices.InitializeAsync(initializationOptions);

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }
}
