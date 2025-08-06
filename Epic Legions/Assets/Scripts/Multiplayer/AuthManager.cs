using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class AuthManager : MonoBehaviour
{
    private string apiUrl = "https://localhost:7255/api/auth";

    private void Start()
    {
        // Optionally, you can call the Login method here for testing purposes
        StartCoroutine(Login("test@hemera.com", "password123"));
    }
    public IEnumerator Login(string username, string password)
    {
       LoginRequest loginRequest = new LoginRequest
        {
            Email = username,
            Password = password
        };
        string jsonData = JsonUtility.ToJson(loginRequest);
        UnityWebRequest request = new UnityWebRequest(apiUrl + "/login", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Login failed: " + request.error);
        }
        else
        {
            LoginResponse response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
            Debug.Log("Login successful! Token: " + response.token);
            PlayerPrefs.SetString("authToken", response.token);
            PlayerPrefs.SetString("refreshToken", response.refreshToken);
        }
    }

    public IEnumerator RefreshToken()
    {
        string refreshToken = PlayerPrefs.GetString("refreshToken");
        string jsonData = JsonUtility.ToJson(new { refreshToken });

        UnityWebRequest request = new UnityWebRequest(apiUrl + "/refresh", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if(request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Token refresh failed: " + request.error);
        }
        else
        {
            LoginResponse response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
            Debug.Log("Token refreshed successfully! New Token: " + response.token);
            PlayerPrefs.SetString("authToken", response.token);
            PlayerPrefs.SetString("refreshToken", response.refreshToken);
        }
    }
}

[System.Serializable]
public class LoginRequest
{
    public string Email;
    public string Password;
}

[System.Serializable]
public class LoginResponse
{
    public string token;
    public string refreshToken;
}