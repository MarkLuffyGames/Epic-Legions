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
        
    }

    void Update()
    {
        if(!NetworkManager.Singleton.IsServer) return;
        if (isPlayer)
        {
            isReady.SetActive(duelManager.playerReady.ElementAt(0).Value);
        }
        else
        {
            isReady.SetActive(duelManager.playerReady.ElementAt(1).Value);
        }
    }
}
