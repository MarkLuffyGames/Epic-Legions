using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndDuelUI : MonoBehaviour
{
    [SerializeField] private GameObject endDuelUI;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Button mainMenuButton;

    private void Start()
    {
        mainMenuButton.onClick.AddListener(() => {
            Destroy(DuelManager.Instance.gameObject);
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("MainMenu");
        });
        Hide();
    }
    public void Show(bool playerVictory)
    {
        endDuelUI.SetActive(true);
        resultText.text = playerVictory ? "Has Ganado" : "Has Perdido";
        resultText.color = playerVictory ? Color.blue : Color.red;
    }

    private void Hide()
    {
        endDuelUI.SetActive(false);
    }
}
