using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Burst.Intrinsics;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;

public enum DuelPhase { PreparingDuel, Starting, DrawingCards, Preparation, Battle, None }
public class DuelManager : NetworkBehaviour
{
    public static DuelManager instance;

    [SerializeField] private PlayerManager player1Manager;
    [SerializeField] private PlayerManager player2Manager;

    public List<int> deckCardIds;

    private Dictionary<ulong, int> playerRoles = new Dictionary<ulong, int>();
    private int connectedPlayerCount = 0;

    private Dictionary<ulong, List<int>> playerDecks = new Dictionary<ulong, List<int>>();
    private Dictionary<ulong, bool> playerReady = new Dictionary<ulong, bool>();


    private NetworkVariable<DuelPhase> duelPhase = new NetworkVariable<DuelPhase>(DuelPhase.None);

    public TextMeshProUGUI duelPhaseText;

    private void Awake()
    {
        DuelManager.instance = this;
    }

    private void Start()
    {
        duelPhase.OnValueChanged += OnDuelPhaseChanged;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
    }

    private void NetworkManager_OnClientConnectedCallback(ulong ClientId)
    {
        if (IsServer)
        {
            RegisterPlayer(ClientId);
        }
    }

    private void OnDuelPhaseChanged(DuelPhase oldPhase, DuelPhase newPhase)
    {
        UpdateDuelPhaseText();
        if (IsServer)
        {
            playerReady.Clear();
        }

        player1Manager.isReady = false;
        player2Manager.isReady = false;

        if (newPhase == DuelPhase.PreparingDuel)
        {
            SendDeckToServerRpc(NetworkManager.Singleton.LocalClientId, deckCardIds.ToArray());
        }

        if (newPhase == DuelPhase.Starting)
        {
            player1Manager.DrawStartCards();
            player2Manager.DrawStartCards();
        }
    }

    public void RegisterPlayer(ulong clientId)
    {
        if (!playerRoles.ContainsKey(clientId))
        {
            connectedPlayerCount++;
            playerRoles[clientId] = connectedPlayerCount;
        }

        if (connectedPlayerCount == 2)
        {
            StartDuel();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendDeckToServerRpc(ulong clientId, int[] deckCardIds)
    {
        // Validar el deck (opcional)
        if (deckCardIds == null || deckCardIds.Length == 0)
        {
            Debug.LogWarning($"Player {clientId} sent an invalid deck.");
            return;
        }

        // Guardar el deck del jugador
        if (!playerDecks.ContainsKey(clientId))
        {
            deckCardIds = CardDatabase.ShuffleArray(deckCardIds);

            playerDecks[clientId] = deckCardIds.ToList();
            Debug.Log($"Deck received from Player {clientId}: {string.Join(", ", deckCardIds)}");

            SendDeckToClientsClientRpc(clientId, deckCardIds);

            SetDeck(playerRoles[clientId], deckCardIds);
        }
    }

    [ClientRpc]
    private void SendDeckToClientsClientRpc(ulong targetClientId, int[] deckCardIds)
    {
        if (NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            Debug.Log($"Deck del jugador validado por el servidor: {string.Join(", ", deckCardIds)}");
            SetDeck(1, deckCardIds);
        }
        else
        {
            Debug.Log($"Deck del rival recibido del servidor: {string.Join(", ", deckCardIds)}");
            SetDeck(2, deckCardIds);
        }
    }

    private void SetDeck(int playerRole, int[] deckCardIds)
    {
        foreach (var cardId in deckCardIds)
        {
            var card = CardDatabase.GetCardById(cardId);
            if (card != null)
            {
                if (playerRole == 1)
                    player1Manager.AddCardToPlayerDeck(card, deckCardIds.Length);
                else if (playerRole == 2)
                    player2Manager.AddCardToPlayerDeck(card, deckCardIds.Length);
            }
            else
            {
                Debug.LogWarning($"Player {playerRole} tried to add an invalid card ID {cardId} to their deck.");
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerReadyServerRpc(ulong clientId, bool isReady)
    {
        playerReady[clientId] = isReady;

        if (AreAllTrue(playerReady))
        {
            if (duelPhase.Value == DuelPhase.PreparingDuel)
            {
                duelPhase.Value = DuelPhase.Starting;
            }
            else if (duelPhase.Value == DuelPhase.Starting)
            {
                duelPhase.Value = DuelPhase.Preparation;
            }
        }
    }
    public bool AreAllTrue(Dictionary<ulong, bool> clientStatus)
    {
        // Verificar si todos los valores son true
        return clientStatus.Values.All(status => status);
    }

    private void StartDuel()
    {
        duelPhase.Value = DuelPhase.PreparingDuel;
    }

    private void UpdateDuelPhaseText()
    {
        duelPhaseText.text = duelPhase.Value.ToString();
    }

    /////////// Player Actions ////////////////////////////////

    /*[ServerRpc]
    public void PlaceCardOnTheFieldServerRpc(Card card, FieldPosition fieldPosition)
    {

    }*/
}
