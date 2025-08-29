using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerDuelUI : MonoBehaviour
{
    [SerializeField] private DuelManager duelManager;
    [SerializeField] private GameObject isReady;
    [SerializeField] private bool isPlayer;

    [SerializeField] private TextMeshProUGUI playerHealtText;
    [SerializeField] private TextMeshProUGUI playerEnergyText;
    [SerializeField] private Image healtBar;
    [SerializeField] private Image energytBar;

    private int playerHealt;
    private int playerEnergy;

    void Start()
    {
        duelManager.OnPlayerReady += DuelManager_OnPlayerReady;
        duelManager.OnPlayerNotReady += DuelManager_OnPlayerNotReady;

        isReady.SetActive(false);
    }

    private void DuelManager_OnPlayerNotReady(object sender, System.EventArgs e)
    {
        isReady.SetActive(false);
    }

    private void DuelManager_OnPlayerReady(object sender, DuelManager.OnPlayerReadyEventArgs e)
    {
        if (duelManager.IsSinglePlayer)
        {
            isReady.SetActive(isPlayer ? duelManager.Player1Manager.isReady : duelManager.Player2Manager.isReady);
        }
        else
        {
            if (e.clientIdReady == NetworkManager.Singleton.LocalClientId && isPlayer)
            {
                isReady.SetActive(true);
            }
            else if (e.clientIdReady != NetworkManager.Singleton.LocalClientId && !isPlayer)
            {
                isReady.SetActive(true);
            }
        }
    }

    public void UpdateUI(int playerHealt, int playerEnergy)
    {
        StartCoroutine(ChangeHealtSmoothly(playerHealt));
        StartCoroutine(ChangeEnergySmoothly(playerEnergy));
    }

    IEnumerator ChangeHealtSmoothly(int targetHealth)
    {
        float duration = 1f;
        float elapsed = 0f;
        int start = playerHealt;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            playerHealt = Mathf.RoundToInt(Mathf.Lerp(start, targetHealth, t));
            playerHealtText.text = $"{playerHealt}";
            healtBar.fillAmount = playerHealt / 100f;
            yield return null;
        }

        this.playerHealt = targetHealth;
    }

    IEnumerator ChangeEnergySmoothly(int targetEnergy)
    {
        float duration = 1f;
        float elapsed = 0f;
        int start = playerEnergy;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            playerEnergy = Mathf.RoundToInt(Mathf.Lerp(start, targetEnergy, t));
            playerEnergyText.text = $"{playerEnergy}";
            energytBar.fillAmount = playerEnergy / 100f;
            yield return null;
        }

        this.playerEnergy = targetEnergy;
    }

}
