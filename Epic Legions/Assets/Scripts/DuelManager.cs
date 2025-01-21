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
            SetCard(player1Manager, true, cardIndex, fieldPositionIdex);
        }
        else if (playerRoles[clientId] == 2)
        {
            SetCard(player2Manager, false, cardIndex, fieldPositionIdex);
        }

        PlaceCardOnTheFieldClientRpc(cardIndex, fieldPositionIdex, clientId);
    }

    private void SetCard(PlayerManager playerManager, bool isPlayer, int cardIndex, int fieldPositionIdex)
    {
        Card card = playerManager.GetHandCardHandler().GetCardInHandList()[cardIndex];
        playerManager.GetHandCardHandler().QuitCard(card);
        if(fieldPositionIdex == -1)
        {
            playerManager.SpellFieldPosition.SetCard(card, isPlayer);
            UseSpellCard(card);
        }
        else
        {
            playerManager.GetFieldPositionList()[fieldPositionIdex].SetCard(card, isPlayer);
            if (IsServer) InsertCardInOrder(HeroCardsOnTheField, card);
        }
        
        card.waitForServer = false;
        
    }

    private void InsertCardInOrder(List<Card> heroCards, Card newCard)
    {
        // Encuentra la posición donde insertar la nueva carta
        int insertIndex = heroCards.FindLastIndex(card => card.CurrentSpeedPoints >= newCard.CurrentSpeedPoints);

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
            SetCard(player1Manager, true, cardIndex, fieldPositionIdex);
        }
        else
        {
            SetCard(player2Manager, false, cardIndex, fieldPositionIdex);
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

        if (heroTurn.FieldPosition == null)
        {
            NextTurn();
            return;
        }
        SetHeroTurnClientRpc(HeroCardsOnTheField[heroTurnIndex].FieldPosition.PositionIndex, GetClientIdForHero(HeroCardsOnTheField[heroTurnIndex]));
        if (!IsHost)
        {
            heroTurn.HandlingStatusEffects();
            ManageEffects();
        }
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

        ManageEffects();
        if (heroTurn.HandlingStatusEffects())
        {
            heroTurn.EndTurn();
            if (IsClient) SetPlayerReadyServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    private void ManageEffects()
    {
        heroTurn.ManageEffects();

        foreach (var fieldPosition in player1Manager.GetFieldPositionList())
        {
            fieldPosition.statModifier.RemoveAll(stat => stat.durability <= 0);
        }

        foreach (var fieldPosition in player2Manager.GetFieldPositionList())
        {
            fieldPosition.statModifier.RemoveAll(stat => stat.durability <= 0);
        }

        heroTurn.UpdateText();
    }

    public Card GetHeroTurn()
    {
        return heroTurn;
    }

    public void UseMovement(int movementToUse)
    {
        movementToUseIndex = movementToUse;
        SetMovementToUseServerRpc(movementToUse);

        if (heroTurn.Moves[movementToUse].MoveSO.NeedTarget)
        {
            SelectTarget(movementToUse);
        }
        else
        {
            HeroAttackServerRpc(heroTurn.FieldPosition.PositionIndex, NetworkManager.Singleton.LocalClientId);
        }
    }

    private void UseSpellCard(Card spellCard)
    {
        if (spellCard.Moves[0].MoveSO.NeedTarget)
        {
            SelectTarget(0);
        }
        else
        {
            HeroAttackServerRpc(heroTurn.FieldPosition.PositionIndex, NetworkManager.Singleton.LocalClientId);
        }
    }


    public void SelectTarget(int movementToUse)
    {
        settingAttackTarget = true;

        foreach (Card card in ObtainTargets())
        {
            if (player1Manager.GetFieldPositionList().Contains(card.FieldPosition))
            {
                card.ActiveAttackableTarget(Color.green);
            }
            else
            {
                card.ActiveAttackableTarget(Color.red);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetMovementToUseServerRpc(int movementToUse)
    {
        movementToUseIndex = movementToUse;
    }

    private List<Card> ObtainTargets() //Ajustar para obtener los objetivos para cada tipo de ataque.
    {
        List<Card> targets = new List<Card>();

        if(heroTurn.Moves[movementToUseIndex].MoveSO.MoveType != MoveType.PositiveEffect)
        {
            for (int i = 0; i < player2Manager.GetFieldPositionList().Count; i++)
            {
                if (player2Manager.GetFieldPositionList()[i].Card != null)
                {
                    targets.Add(player2Manager.GetFieldPositionList()[i].Card);
                }

                if (targets.Count > 0 && (i == 4 || i == 9 || i == 14) && heroTurn.Moves[movementToUseIndex].MoveSO.MoveType == MoveType.MeleeAttack)
                {
                    return targets;
                }
            }
        }
        else
        {
            foreach (var position in player1Manager.GetFieldPositionList())
            {
                if (position.Card != null && position.Card != heroTurn)
                {
                    targets.Add(position.Card);
                }
            }
        }
        

        return targets;
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
            if (heroTurn.Moves[movementToUseIndex].MoveSO.MoveType != MoveType.PositiveEffect)
            {
                Card card = player2Manager.GetFieldPositionList()[heroToAttackPositionIndex].Card;
                yield return HeroAttack(card, 1);
            }
            else
            {
                Card card = player1Manager.GetFieldPositionList()[heroToAttackPositionIndex].Card;
                yield return HeroAttack(card, 1);
            }
            
        }
        else if (playerRoles[clientId] == 2)
        {
            if (heroTurn.Moves[movementToUseIndex].MoveSO.MoveType != MoveType.PositiveEffect)
            {
                Card card = player1Manager.GetFieldPositionList()[heroToAttackPositionIndex].Card;
                yield return HeroAttack(card, 2);
            }
            else
            {
                Card card = player2Manager.GetFieldPositionList()[heroToAttackPositionIndex].Card;
                yield return HeroAttack(card, 2);
            }
        }
        settingAttackTarget = false;
    }

    private IEnumerator HeroAttack(Card cardToAttack, int player)
    {
        //Desactivar los objetivos atacables.
        foreach (var fieldPosition in player2Manager.GetFieldPositionList())
        {
            if (fieldPosition.Card != null)
            {
                fieldPosition.Card.DesactiveAttackableTarget();
            }
        }
        foreach (var fieldPosition in player1Manager.GetFieldPositionList())
        {
            if (fieldPosition.Card != null)
            {
                fieldPosition.Card.DesactiveAttackableTarget();
            }
        }

        //Aqui va el metodo para iniciar la animacion de ataque.
        if (heroTurn.Moves[movementToUseIndex].MoveSO.MoveType == MoveType.MeleeAttack)
        {
            yield return heroTurn.MeleeAttackAnimation(player, cardToAttack, heroTurn.Moves[movementToUseIndex]);
        }
        else //if(heroTurn.Moves[movementToUseIndex].MoveSO.MoveType == MoveType.PositiveEffect)
        {
            heroTurn.RangedMovementAnimation();
        }

        yield return new WaitForSeconds(0.3f);

        heroTurn.MoveToLastPosition();

        //Aplicar afecto de ataque si es necesario.
        if (heroTurn.Moves[movementToUseIndex].MoveSO.TargetsType == TargetsType.SingleTarget)
        {
            heroTurn.Moves[movementToUseIndex].ActivateEffect(heroTurn, cardToAttack);
        }
        else
        {
            heroTurn.Moves[movementToUseIndex].ActivateEffect(heroTurn, GetTargetsForMovement(cardToAttack));
        }


        if (heroTurn.Moves[movementToUseIndex].MoveSO.Damage != 0) //Ataque de daño.
        {

            //Animacion de daño del oponente
            if (heroTurn.Moves[movementToUseIndex].MoveSO.TargetsType == TargetsType.SingleTarget)
            {
                cardToAttack.AnimationReceivingMovement(heroTurn.Moves[movementToUseIndex]);

                //Aplicar daño al opnente.
                if (cardToAttack.ReceiveDamage(heroTurn.Moves[movementToUseIndex].MoveSO.Damage))
                {
                    SendCardToGraveyard();
                }
            }
            else
            {
                var targets = GetTargetsForMovement(cardToAttack);
                foreach (var card in targets)
                {
                    Debug.Log(card.FieldPosition);
                    card.AnimationReceivingMovement(heroTurn.Moves[movementToUseIndex]);
                }

                foreach (var card in targets) 
                {
                    //Aplicar daño al opnente.
                    if (card.ReceiveDamage(heroTurn.Moves[movementToUseIndex].MoveSO.Damage))
                    {
                        SendCardToGraveyard();
                    }
                }
            }
            
        }
        else //Ataque de efecto
        {
            //Animacion de efecto
            if (heroTurn.Moves[movementToUseIndex].MoveSO.TargetsType == TargetsType.SingleTarget)
            {
                cardToAttack.AnimationReceivingMovement(heroTurn.Moves[movementToUseIndex]);
            }
            else
            {
                foreach (var card in GetTargetsForMovement(cardToAttack))
                {
                    card.AnimationReceivingMovement(heroTurn.Moves[movementToUseIndex]);
                }
            }
        }

        movementToUseIndex = -1;
        heroTurn.EndTurn();
        if (IsClient) SetPlayerReadyServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    private List<Card> GetTargetsForMovement(Card cardToAttack)
    {
        if(heroTurn.Moves[movementToUseIndex].MoveSO.TargetsType == TargetsType.TargetLine)
        {

            if (player1Manager.GetFieldPositionList().Contains(cardToAttack.FieldPosition))
            {
                return player1Manager.GetLineForCard(cardToAttack);
            }
            else
            {
                return player2Manager.GetLineForCard(cardToAttack);
            }
        }
        else if(heroTurn.Moves[movementToUseIndex].MoveSO.TargetsType == TargetsType.Midfield)
        {
            if (player1Manager.GetFieldPositionList().Contains(cardToAttack.FieldPosition))
            {
                return player1Manager.GetAllCardInField(cardToAttack);
            }
            else
            {
                return player2Manager.GetAllCardInField(cardToAttack);
            }
        }

        return null;
    }

    private void SendCardToGraveyard()
    {
        foreach(var target in player1Manager.GetFieldPositionList())
        {
            if(target.Card != null && target.Card.CurrentHealtPoints <= 0)
            {
                target.DestroyCard(player1Manager.GetGraveyard(), true);
            }
        }
        foreach (var target in player2Manager.GetFieldPositionList())
        {
            if (target.Card != null && target.Card.CurrentHealtPoints <= 0)
            {
                target.DestroyCard(player2Manager.GetGraveyard(), false);
            }
        }
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

            if (heroTurn.Moves[movementToUseIndex].MoveSO.MoveType != MoveType.PositiveEffect)
            {
                Card card = player2Manager.GetFieldPositionList()[fieldPositionIndex].Card;
                yield return HeroAttack(card, 1);
            }
            else
            {
                Card card = player1Manager.GetFieldPositionList()[fieldPositionIndex].Card;
                yield return HeroAttack(card, 1);
            }
        }
        else
        {
            if (heroTurn.Moves[movementToUseIndex].MoveSO.MoveType != MoveType.PositiveEffect)
            {
                Card card = player1Manager.GetFieldPositionList()[fieldPositionIndex].Card;
                yield return HeroAttack(card, 2);
            }
            else
            {
                Card card = player2Manager.GetFieldPositionList()[fieldPositionIndex].Card;
                yield return HeroAttack(card, 2);
            }
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

    // Método para remover las cartas sin vida
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
