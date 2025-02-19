using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PlayerDuelUI : MonoBehaviour
{
    [SerializeField] private DuelManager duelManager;
    [SerializeField] private GameObject isReady;
    [SerializeField] private bool isPlayer;
    void Start()
    {
        duelManager.OnPlayerReady += DuelManager_OnPlayerReady;
    }

    private void DuelManager_OnPlayerReady(object sender, DuelManager.OnPlayerReadyEventArgs e)
    {
        if(e.clientIdReady == NetworkManager.Singleton.LocalClientId && isPlayer)
        {
            isReady.SetActive(true);
        }
        else if(e.clientIdReady != NetworkManager.Singleton.LocalClientId && !isPlayer)
        {
            isReady.SetActive(true);
        }


    }
}
