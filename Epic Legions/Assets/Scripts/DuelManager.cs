using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;
using static UnityEngine.Rendering.GPUSort;

public enum DuelPhase { PreparingDuel, Starting, DrawingCards, Preparation, Battle, None }
public class DuelManager : NetworkBehaviour
{
    public static DuelManager instance;

    [SerializeField] private PlayerManager player1Manager;
    [SerializeField] private PlayerManager player2Manager;

    public List<int> deckCardIds;

    private Dictionary<ulong, int> playerRoles = new Dictionary<ulong, int>();
    private Dictionary<int, ulong> playerId = new Dictionary<int, ulong>();
    private int connectedPlayerCount = 0;

    private Dictionary<ulong, List<int>> playerDecks = new Dictionary<ulong, List<int>>();
    private Dictionary<ulong, bool> playerReady = new Dictionary<ulong, bool>();


    private NetworkVariable<DuelPhase> duelPhase = new NetworkVariable<DuelPhase>(DuelPhase.None);

    private List<Card> HeroCardsOnTheField = new List<Card>();
    private int heroTurnIndex;

    public TextMeshProUGUI duelPhaseText;
    public Card heroTurn;

    private void Awake()
    {
        instance = this;
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

        player1Manager.isReady = false;
        player2Manager.isReady = false;

        if (newPhase == DuelPhase.PreparingDuel)
        {
            SendDeckToServerRpc(NetworkManager.Singleton.LocalClientId, deckCardIds.ToArray());
        }
        else if (newPhase == DuelPhase.Starting)
        {
            player1Manager.DrawStartCards();
            player2Manager.DrawStartCards();
        }
        else if (newPhase == DuelPhase.Preparation)
        {
            player1Manager.ShowNextPhaseButton();
        }
        else if(newPhase == DuelPhase.Battle)
        {
            player1Manager.HideWaitTextGameObject();

            if (IsServer)
            {
                heroTurnIndex = 0;

                StartHeroTurn();
            }

            if(IsClient)
            {
                player1Manager.GetHandCardHandler().HideHandCard();
            }
        }


    }

    public void RegisterPlayer(ulong clientId)
    {
        if (!playerRoles.ContainsKey(clientId))
        {
            connectedPlayerCount++;
            playerRoles[clientId] = connectedPlayerCount;
            playerId[connectedPlayerCount] = clientId;
            playerReady[clientId] = false;
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
        if (duelPhase.Value != DuelPhase.PreparingDuel)
        {
            Debug.Log("Deck received, but phase is not 'Preparing'. Waiting for phase update...");
            StartCoroutine(WaitForPhaseAndProcessDeck(targetClientId, deckCardIds));
            return;
        }

        ProcessDeck(targetClientId, deckCardIds);
    }

    private void ProcessDeck(ulong targetClientId, int[] deckCardIds)
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

    private IEnumerator WaitForPhaseAndProcessDeck(ulong targetClientId, int[] deckCardIds)
    {
        yield return new WaitUntil(() => duelPhase.Value == DuelPhase.PreparingDuel);
        ProcessDeck(targetClientId, deckCardIds);
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
    public void SetPlayerReadyServerRpc(ulong clientId)
    {
        playerReady[clientId] = true;

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
            else if(duelPhase.Value == DuelPhase.Preparation)
            {
                duelPhase.Value = DuelPhase.Battle;
            }

            playerReady = playerReady.ToDictionary(kvp => kvp.Key, kvp => false);
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

    public DuelPhase GetDuelPhase()
    {
        return duelPhase.Value;
    }

    /////////// Player Actions ////////////////////////////////

    [ServerRpc(RequireOwnership = false)]
    public void PlaceCardOnTheFieldServerRpc(int cardIndex, int fieldPositionIdex, ulong clientId)
    {

        if (playerRoles[clientId] == 1)
        {
            Card card = player1Manager.GetHandCardHandler().GetCardInHandList()[cardIndex];
            player1Manager.GetHandCardHandler().QuitCard(card);
            player1Manager.GetFieldPositionList()[fieldPositionIdex].SetCard(card, true);
            card.waitForServer = false;
            InsertCardInOrder(HeroCardsOnTheField, card);
        }
        else if (playerRoles[clientId] == 2)
        {
            Card card = player2Manager.GetHandCardHandler().GetCardInHandList()[cardIndex];
            player2Manager.GetHandCardHandler().QuitCard(card);
            player2Manager.GetFieldPositionList()[fieldPositionIdex].SetCard(card, false);
            card.waitForServer = false;
            InsertCardInOrder(HeroCardsOnTheField, card);
        }

        PlaceCardOnTheFieldClientRpc(cardIndex, fieldPositionIdex, clientId);
    }

    private void InsertCardInOrder(List<Card> heroCards, Card newCard)
    {
        // Encuentra la posición donde insertar la nueva carta
        int insertIndex = heroCards.FindLastIndex(card => card.SpeedPoint >= newCard.SpeedPoint);

        // Si no encontró ninguna carta con igual o mayor velocidad, la coloca al inicio
        if (insertIndex == -1)
        {
            heroCards.Insert(0, newCard);
        }
        else
        {
            // Inserta después de la última carta con la misma velocidad o mayor
            heroCards.Insert(insertIndex + 1, newCard);
        }
    }

    [ClientRpc]
    private void PlaceCardOnTheFieldClientRpc(int cardIndex, int fieldPositionIdex, ulong clientId)
    {
        if(NetworkManager.Singleton.IsHost)return;

        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            Card card = player1Manager.GetHandCardHandler().GetCardInHandList()[cardIndex];
            player1Manager.GetHandCardHandler().QuitCard(card);
            player1Manager.GetFieldPositionList()[fieldPositionIdex].SetCard(card, true);
            card.waitForServer = false;
        }
        else
        {
            Card card = player2Manager.GetHandCardHandler().GetCardInHandList()[cardIndex];
            player2Manager.GetHandCardHandler().QuitCard(card);
            player2Manager.GetFieldPositionList()[fieldPositionIdex].SetCard(card, false);
            card.waitForServer = false;
        }
    }

    private void StartHeroTurn()
    {
        SetHeroTurnClientRpc(HeroCardsOnTheField[heroTurnIndex].FieldPosition.PositionIndex, GetClientIdForCurrentTurnHero());
        heroTurn = HeroCardsOnTheField[heroTurnIndex];
    }

    private ulong GetClientIdForCurrentTurnHero()
    {
        if (player1Manager.GetFieldPositionList().Contains(HeroCardsOnTheField[heroTurnIndex].FieldPosition))
        {
            return playerId[1];
        }
        else if (player2Manager.GetFieldPositionList().Contains(HeroCardsOnTheField[heroTurnIndex].FieldPosition))
        {
            return playerId[2];
        }
        
        Debug.LogError("El heroe no pertenece a ningun jugador");
        return 0;
    }

    [ClientRpc]
    private void SetHeroTurnClientRpc(int fieldPositionIndex, ulong ownerClientId)
    {
        if (NetworkManager.Singleton.LocalClientId == ownerClientId)
        {
            Card card = player1Manager.GetFieldPositionList()[fieldPositionIndex].Card;
            card.SetTurn(true);
            heroTurn = card;
        }
        else
        {
            Card card = player2Manager.GetFieldPositionList()[fieldPositionIndex].Card;
            card.SetTurn(false);
            heroTurn = card;
        }
    }

    [ServerRpc]
    public void HeroAttackServerRpc(int heroToAttackPositionIndex, ulong clientId)
    {

    }
}
