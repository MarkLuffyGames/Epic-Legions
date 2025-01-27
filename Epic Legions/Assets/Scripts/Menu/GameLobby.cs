using Unity.Netcode;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLobby : MonoBehaviour
{

    private Lobby joinedLobby;
    public async void CreateLobby(string roomName, bool isPrivate)
    {
        try
        {
            joinedLobby = await LobbyService.Instance.CreateLobbyAsync(roomName, 2, new CreateLobbyOptions { IsPrivate = isPrivate });
            NetworkManager.Singleton.StartHost();
            SceneManager.LoadScene("GameScene");
        }
        catch(LobbyServiceException ex) 
        {
            Debug.LogException(ex);
        }
    }

    public async void QuickJoin()
    {
        try
        {
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            NetworkManager.Singleton.StartClient();
            SceneManager.LoadScene("GameScene");
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogException(ex);
        }
        
    }
}
