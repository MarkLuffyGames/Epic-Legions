using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;

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
    public int movementToUseIndex;
    public bool settingAttackTarget;

    private void Awake()
    {
        instance = this;
        Application.targetFrameRate = 60;
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
        else if(newPhase == DuelPhase.DrawingCards)
        {

            player1Manager.DrawCard();
            player2Manager.DrawCard();
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
            SetDeck(1, deckCardIds);
        }
        else
        {
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
            playerReady = playerReady.ToDictionary(kvp => kvp.Key, kvp => false);

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
            else if(duelPhase.Value == DuelPhase.DrawingCards)
            {
                duelPhase.Value = DuelPhase.Preparation;
            }
            else if(duelPhase.Value == DuelPhase.Battle)
            {
                NextTurn();
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
        // Encuentra la posici�n donde insertar la nueva carta
        int insertIndex = heroCards.FindLastIndex(card => card.CurrentSpeedPoints >= newCard.CurrentSpeedPoints);

        // Si no encontr� ninguna carta con igual o mayor velocidad, la coloca al inicio
        if (insertIndex == -1)
        {
            heroCards.Insert(0, newCard);
        }
        else
        {
            // Inserta despu�s de la �ltima carta con la misma velocidad o mayor
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
        if(HeroCardsOnTheField.Count == 0)
        {
            NextTurn();
            return;
        }

        heroTurn = HeroCardsOnTheField[heroTurnIndex];

        if (heroTurn.CurrentHealtPoints <= 0)
        {
            NextTurn();
            return;
        }
        SetHeroTurnClientRpc(HeroCardsOnTheField[heroTurnIndex].FieldPosition.PositionIndex, GetClientIdForHero(HeroCardsOnTheField[heroTurnIndex]));
        if(!IsHost)heroTurn.HandlingStatusEffects();
    }

    private ulong GetClientIdForHero(Card heroCard)
    {
        if (player1Manager.GetFieldPositionList().Contains(heroCard.FieldPosition))
        {
            return playerId[1];
        }
        else if (player2Manager.GetFieldPositionList().Contains(heroCard.FieldPosition))
        {
            return playerId[2];
        }
        
        Debug.LogError("El heroe no pertenece a ningun jugador");
        return 0;
    }

    [ClientRpc]
    private void SetHeroTurnClientRpc(int fieldPositionIndex, ulong ownerClientId)
    {
        Card card = null;
        if (NetworkManager.Singleton.LocalClientId == ownerClientId)
        {
            card = player1Manager.GetFieldPositionList()[fieldPositionIndex].Card;
            card.SetTurn(true);
        }
        else
        {
            card = player2Manager.GetFieldPositionList()[fieldPositionIndex].Card;
            card.SetTurn(false);
        }

        heroTurn = card;

        if(heroTurn.HandlingStatusEffects())
        {
            heroTurn.EndTurn();
            if (IsClient) SetPlayerReadyServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    public Card GetHeroTurn()
    {
        return heroTurn;
    }

    public void SelectAttackTarget(int movementToUse)
    {
        settingAttackTarget = true;

        movementToUseIndex = movementToUse;
        SetMovementToUseServerRpc(movementToUse);

        foreach (Card card in ObtainAttackableTargets())
        {
            card.ActiveAttackableTarget();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetMovementToUseServerRpc(int movementToUse)
    {
        movementToUseIndex = movementToUse;
    }

    private List<Card> ObtainAttackableTargets()
    {
        if(heroTurn.cardSO is HeroCardSO heroCardSO)
        {
            List<Card> targets = new List<Card>();
            for (int i = 0; i < player2Manager.GetFieldPositionList().Count; i++)
            {
                if (player2Manager.GetFieldPositionList()[i].Card != null)
                {
                    targets.Add(player2Manager.GetFieldPositionList()[i].Card);
                }

                if (targets.Count > 0 && (i == 4 || i == 9 || i == 14) && heroCardSO.HeroClass == HeroClass.Warrior)
                {
                    return targets;
                }

            }

            return targets;
        }

        return null;
    }

    [ServerRpc(RequireOwnership = false)]
    public void HeroAttackServerRpc(int heroToAttackPositionIndex, ulong clientId)
    {
        StartCoroutine(HeroAttackServer(heroToAttackPositionIndex, clientId));
    }
    IEnumerator HeroAttackServer(int heroToAttackPositionIndex, ulong clientId)
    {

        HeroAttackClientRpc(heroToAttackPositionIndex, clientId, movementToUseIndex);

        if (playerRoles[clientId] == 1)
        {
            Card card = player2Manager.GetFieldPositionList()[heroToAttackPositionIndex].Card;
            yield return HeroAttack(card, 1);
        }
        else if (playerRoles[clientId] == 2)
        {
            Card card = player1Manager.GetFieldPositionList()[heroToAttackPositionIndex].Card;
            yield return HeroAttack(card, 2);
        }
        settingAttackTarget = false;
    }

    private IEnumerator HeroAttack(Card cardToAttack, int player)
    {

        //Aqui va el metodo para iniciar la animacion de ataque.



        //Desactivar los objetivos atacables.
        foreach (var fieldPosition in player2Manager.GetFieldPositionList())
        {
            if (fieldPosition.Card != null)
            {
                fieldPosition.Card.DesactiveAttackableTarget();
            }
        }

        //Animacion de da�o del oponente
        cardToAttack.DamageAnimation();
        yield return new WaitForSeconds(1);

        //Aplicar afecto de ataque si es necesario.
        heroTurn.Moves[movementToUseIndex].MoveEffect.ActivateEffect(heroTurn, cardToAttack);

        //Aplicar da�o al opnente.
        if (cardToAttack.ReceiveDamage(heroTurn.Moves[movementToUseIndex].Damage))
        {
            Transform playerGraveyard = null;
            bool isPlayer = false;
            if (player == 1)
            {
                playerGraveyard = player2Manager.GetGraveyard();
                isPlayer = false;
            }
            else if(player == 2)
            {
                playerGraveyard = player1Manager.GetGraveyard();
                isPlayer = true;
            }

            cardToAttack.FieldPosition.DestroyCard(playerGraveyard, isPlayer);
        }

        movementToUseIndex = -1;
        heroTurn.EndTurn();
        if (IsClient) SetPlayerReadyServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ClientRpc]
    private void HeroAttackClientRpc(int fieldPositionIndex, ulong attackerClientId, int movementToUseIndex)
    {
        this.movementToUseIndex = movementToUseIndex;
        StartCoroutine(HeroAttackClient(fieldPositionIndex, attackerClientId));
    }
    private IEnumerator HeroAttackClient(int fieldPositionIndex, ulong attackerClientId)
    {
        if (IsHost) yield break;

        if (NetworkManager.Singleton.LocalClientId == attackerClientId)
        {
            Card card = player2Manager.GetFieldPositionList()[fieldPositionIndex].Card;
            yield return HeroAttack(card, 1);
        }
        else
        {
            Card card = player1Manager.GetFieldPositionList()[fieldPositionIndex].Card;
            yield return HeroAttack(card, 2);
        }

        settingAttackTarget = false;
    }

    private void NextTurn()
    {
        if(heroTurnIndex < HeroCardsOnTheField.Count - 1)
        {
            heroTurnIndex++;
            StartHeroTurn();
        }
        else
        {
            RemoveDestroyedCards();
            RegenerateDefense();
            duelPhase.Value = DuelPhase.DrawingCards;
        }
    }

    // M�todo para remover las cartas sin vida
    private void RemoveDestroyedCards()
    {
        HeroCardsOnTheField.RemoveAll(card => card.FieldPosition == null);
    }

    private void RegenerateDefense()
    {
        foreach(var card in HeroCardsOnTheField)
        {
            card.RegenerateDefense();
            RegenerateDefenseClientRpc(card.FieldPosition.PositionIndex, GetClientIdForHero(card));

        }
    }

    [ClientRpc]
    private void RegenerateDefenseClientRpc(int fieldPositionIndex, ulong ownerClientId)
    {
        if (IsHost) return;

        Card card = null;
        if (NetworkManager.Singleton.LocalClientId == ownerClientId)
        {
            card = player1Manager.GetFieldPositionList()[fieldPositionIndex].Card;
        }
        else
        {
            card = player2Manager.GetFieldPositionList()[fieldPositionIndex].Card;
        }

        card.RegenerateDefense();
    }
}
