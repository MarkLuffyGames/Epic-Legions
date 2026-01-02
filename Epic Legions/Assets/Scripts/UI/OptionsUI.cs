using IngameDebugConsole;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class OptionsUI : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown player1Control;
    [SerializeField] private TMP_Dropdown player2Control;
    [SerializeField] private Toggle player1AIDebugToggle;
    [SerializeField] private Toggle player1AIDebugDeepToggle;
    [SerializeField] private Toggle player2AIDebugToggle;
    [SerializeField] private Toggle player2AIDebugDeepToggle;
    [SerializeField] private Toggle ingameDebugConsole;


    private void Awake()
    {
        player1Control.value = PlayerPrefs.GetInt("Player1Control", 0);
        player2Control.value = PlayerPrefs.GetInt("Player2Control", 1);

        player1AIDebugToggle.isOn = PlayerPrefs.GetInt("Player1AIDebug", 0) == 1;
        player1AIDebugDeepToggle.isOn = PlayerPrefs.GetInt("Player1AIDebugDeep", 0) == 1;
        player2AIDebugToggle.isOn = PlayerPrefs.GetInt("Player2AIDebug", 1) == 1;
        player2AIDebugDeepToggle.isOn = PlayerPrefs.GetInt("Player2AIDebugDeep", 0) == 1;

        ingameDebugConsole.isOn = PlayerPrefs.GetInt("IngameDebugConsole", 0) == 1;

        SetPlayer1Control();
        SetPlayer2Control();

        SetIngameDebugConsole();
    }
    
    public void SetPlayer1Control()
    {
        int value = player1Control.value;
        PlayerPrefs.SetInt("Player1Control", value);
        if(value == 0)
        {
            player1AIDebugToggle.interactable = false;
            player1AIDebugDeepToggle.interactable = false;

            PlayerPrefs.SetInt("isPlayer", 1);
        }
        else
        {
            /*if(value == 1)
            {
                PlayerPrefs.SetString("AIDifficulty", AIDifficulty.Easy.ToString());
            }
            else if(value == 2)
            {
                PlayerPrefs.SetString("AIDifficulty", AIDifficulty.Normal.ToString());
            }
            else if(value == 3)
            {
                PlayerPrefs.SetString("AIDifficulty", AIDifficulty.Hard.ToString());
            }
            else if(value == 4)
            {
                PlayerPrefs.SetString("AIDifficulty", AIDifficulty.Nightmare.ToString());
            }*/

            player1AIDebugToggle.interactable = true;
            player1AIDebugDeepToggle.interactable = true;
            PlayerPrefs.SetInt("isPlayer", 0);
        }
    }

    public void SetPlayer2Control()
    {
        int value = player2Control.value;
        PlayerPrefs.SetInt("Player2Control", value);
        /*if (value == 0)
        {
            PlayerPrefs.SetString("AIDifficulty2", AIDifficulty.Easy.ToString());
        }
        else if (value == 1)
        {
            PlayerPrefs.SetString("AIDifficulty2", AIDifficulty.Normal.ToString());
        }
        else if (value == 2)
        {
            PlayerPrefs.SetString("AIDifficulty2", AIDifficulty.Hard.ToString());
        }
        else if (value == 3)
        {
            PlayerPrefs.SetString("AIDifficulty2", AIDifficulty.Nightmare.ToString());
        }*/
    }

    public void SetPlayer1AIDebug()
    {
        PlayerPrefs.SetInt("Player1AIDebug", player1AIDebugToggle.isOn ? 1 : 0);
    }
    public void SetPlayer1AIDebugDeep()
    {
        PlayerPrefs.SetInt("Player1AIDebugDeep", player1AIDebugDeepToggle.isOn ? 1 : 0);
    }
    public void SetPlayer2AIDebug()
    {
        PlayerPrefs.SetInt("Player2AIDebug", player2AIDebugToggle.isOn ? 1 : 0);
    }
    public void SetPlayer2AIDebugDeep()
    {
        PlayerPrefs.SetInt("Player2AIDebugDeep", player2AIDebugDeepToggle.isOn ? 1 : 0);
    }
    public void SetIngameDebugConsole()
    {
        PlayerPrefs.SetInt("IngameDebugConsole", ingameDebugConsole.isOn ? 1 : 0);
        if (ingameDebugConsole.isOn)
        {
            DebugLogManager.Instance.gameObject.SetActive(true);
        }
        else
        {
            DebugLogManager.Instance.gameObject.SetActive(false);
        }
    }

    public void BackToMainMenu()
    {
        Loader.LoadScene("MainMenu", true);
    }
}
