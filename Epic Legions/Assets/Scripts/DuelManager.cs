using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public enum DuelPhase { PreparingDuel, Starting, DrawingCards, Preparation, PlayingSpellCard, Battle, EndDuel, None }
public class DuelManager : NetworkBehaviour
{
    public static DuelManager Instance;

    public static int NumberOfTurns = 21;

    public event EventHandler OnPlayerNotReady;
    public event EventHandler<OnPlayerReadyEventArgs> OnPlayerReady;
    public class OnPlayerReadyEventArgs : EventArgs
    {
        public ulong clientIdReady;
    }

    [SerializeField] private PlayerManager player1Manager;
    [SerializeField] private PlayerManager player2Manager;
    [SerializeField] private EndDuelUI endDuelUI;

    public List<int> deckCardIds;

    private Dictionary<ulong, int> playerRoles = new Dictionary<ulong, int>();
    private Dictionary<int, ulong> playerId = new Dictionary<int, ulong>();

    private Dictionary<ulong, List<int>> playerDecks = new Dictionary<ulong, List<int>>();
    public Dictionary<ulong, bool> playerReady = new Dictionary<ulong, bool>(); //Poner privado despues de la prueba.


    private NetworkVariable<DuelPhase> duelPhase = new NetworkVariable<DuelPhase>(DuelPhase.None);

    private List<Card> HeroCardsOnTheField = new List<Card>();
    private List<Card>[] turns = new List<Card>[NumberOfTurns];
    private int heroesInTurnIndex;

    public TextMeshProUGUI duelPhaseText;
    public List<Card> heroInTurn;
    public int movementToUse;
    public bool settingAttackTarget;
    public Card cardSelectingTarget;

    private DuelPhase oldDuelPhase;

    private void Awake()
    {
        Instance = this;
        Application.targetFrameRate = 60;

        duelPhase.OnValueChanged += OnDuelPhaseChanged;
        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < turns.Length; i++)
        {
            turns[i] = new List<Card>();
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
            player1Manager.HideWaitTextGameObject();
            player1Manager.ShowNextPhaseButton();
            if (oldPhase == DuelPhase.DrawingCards)
            {
                player1Manager.GetHandCardHandler().ShowHandCard();
            }
        }
        else if(newPhase == DuelPhase.Battle)
        {
            if (oldPhase != DuelPhase.PlayingSpellCard)
            {
                player1Manager.HideWaitTextGameObject();

                SetBattleTurns();

                StartHeroTurn();

                if (IsClient)
                {
                    player1Manager.GetHandCardHandler().HideHandCard();
                }
            }
            else
            {
                SetHeroTurn();
            }
        }
        else if(newPhase == DuelPhase.DrawingCards)
        {
            player1Manager.DrawCard();
            player2Manager.DrawCard();
        }
        else if(newPhase == DuelPhase.PlayingSpellCard)
        {
            oldDuelPhase = oldPhase;
            player1Manager.GetHandCardHandler().HideHandCard();
        }


    }

    public void RegisterPlayer(ulong clientId1, ulong clientId2)
    {
        playerRoles[clientId1] = 1;
        playerRoles[clientId2] = 2;

        playerId[1] = clientId1;
        playerId[2] = clientId2;

        playerReady[clientId1] = false;
        playerReady[clientId2] = false;

        StartDuel();
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

    private void SetBattleTurns()
    {
        foreach (var list in turns)
        {
            list.Clear();
        }

        if (HeroCardsOnTheField.Count > 0)
        {
            foreach (var card in HeroCardsOnTheField)
            {
                int turn = (100 - card.CurrentSpeedPoints) / 5;
                turns[turn].Add(card);
            }
        }
    }
    private void StartHeroTurn()
    {
        UpdateDuelPhaseText();
        ManageEffects();

        if (turns[heroesInTurnIndex].Count == 0)
        {
            if(IsServer)NextTurn();
            return;
        }
        heroInTurn.Clear();
        foreach (var card in turns[heroesInTurnIndex])
        {
            if (card.IsStunned())
            {
                card.PassTurn();
            }
            else if (card.FieldPosition != null) heroInTurn.Add(card);
        }

        if (heroInTurn.Count == 0)
        {
            StartCoroutine(FinishActions());
        }
        else
        {
            SetHeroTurn();
        }
    }

    private void SetHeroTurn()
    {
        foreach (var hero in heroInTurn)
        {
            if (player1Manager.GetFieldPositionList().Contains(hero.FieldPosition))
            {
                hero.SetTurn(!hero.actionIsReady);
            }
            else
            {
                hero.SetTurn(false);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerReadyServerRpc(ulong clientId)
    {

        if (duelPhase.Value == DuelPhase.Preparation) SetPlayerReadyClientRpc(clientId);
        playerReady[clientId] = true;

        if (AreAllTrue(playerReady))
        {
            playerReady = playerReady.ToDictionary(kvp => kvp.Key, kvp => false);
            SetPlayerNotReadyClientRpc();

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
            else if(duelPhase.Value == DuelPhase.PlayingSpellCard)
            {
                duelPhase.Value = oldDuelPhase;
            }
        }
    }
    public bool AreAllTrue(Dictionary<ulong, bool> clientStatus)
    {
        // Verificar si todos los valores son true
        return clientStatus.Values.All(status => status);
    }

    [ClientRpc]
    public void SetPlayerReadyClientRpc(ulong clientId)
    {
        OnPlayerReady?.Invoke(this, new OnPlayerReadyEventArgs
        {
            clientIdReady = clientId
        });
    }
    [ClientRpc]
    public void SetPlayerNotReadyClientRpc()
    {
        OnPlayerNotReady?.Invoke(this, EventArgs.Empty);
    }

    private void StartDuel()
    {
        duelPhase.Value = DuelPhase.PreparingDuel;
    }

    private void UpdateDuelPhaseText()
    {
        duelPhaseText.text = $"{duelPhase.Value.ToString()}{(duelPhase.Value == DuelPhase.Battle ? $" {heroesInTurnIndex + 1}" : "")}";
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

    /// <summary>
    /// Establece la carta en el campo
    /// </summary>
    /// <param name="playerManager"></param>
    /// <param name="isPlayer"></param>
    /// <param name="cardIndex"></param>
    /// <param name="fieldPositionIdex"></param>
    private void SetCard(PlayerManager playerManager, bool isPlayer, int cardIndex, int fieldPositionIdex)
    {
        Card card = playerManager.GetHandCardHandler().GetCardInHandList()[cardIndex];
        playerManager.GetHandCardHandler().QuitCard(card);
        if(fieldPositionIdex == -1)
        {
            playerManager.SpellFieldPosition.SetCard(card, isPlayer);
            if (IsServer) duelPhase.Value = DuelPhase.PlayingSpellCard;
            if(isPlayer)UseMovement(0, card);       
        }
        else
        {
            playerManager.GetFieldPositionList()[fieldPositionIdex].SetCard(card, isPlayer);
            InsertCardInOrder(HeroCardsOnTheField, card);
        }
        
        card.waitForServer = false;
        
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
            SetCard(player1Manager, true, cardIndex, fieldPositionIdex);
        }
        else
        {
            SetCard(player2Manager, false, cardIndex, fieldPositionIdex);
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

    

    private void ManageEffects()
    {
        foreach (var hero in HeroCardsOnTheField)
        {
            hero.ManageEffects();
            hero.UpdateText();
        }
    }

    private void ActiveEffect()
    {
        foreach (var hero in HeroCardsOnTheField)
        {
            hero.ActivateEffect();
        }
    }


    public void UseMovement(int movementToUseIndex, Card card)
    {
        if (card.Moves[movementToUseIndex].MoveSO.NeedTarget)
        {
            SelectTarget(movementToUseIndex, card);
        }
        else
        {
            HeroAttackServerRpc(card.FieldPosition.PositionIndex, NetworkManager.Singleton.LocalClientId, card.cardSO is HeroCardSO, card.FieldPosition.PositionIndex, movementToUseIndex);
            card.actionIsReady = true;
            card.EndTurn();
        }
    }

    public void SelectTarget(int movementToUseIndex, Card attackingCard)
    {
        movementToUse = movementToUseIndex;
        settingAttackTarget = true;
        cardSelectingTarget = attackingCard;

        var targets = ObtainTargets(attackingCard, movementToUseIndex);

        if (targets.Count > 0)
        {
            foreach (Card card in targets)
            {
                if (player1Manager.GetFieldPositionList().Contains(card.FieldPosition))
                {
                    card.ActiveSelectableTargets(Color.green);
                }
                else
                {
                    card.ActiveSelectableTargets(Color.red);
                }
            }
        }
        else
        {
            cardSelectingTarget.actionIsReady = true;

            HeroAttackServerRpc(-1, NetworkManager.Singleton.LocalClientId, true, attackingCard.FieldPosition.PositionIndex, movementToUseIndex);
            
            cardSelectingTarget.EndTurn();
            DisableAttackableTargets();
        }
    }

    public void DisableAttackableTargets()
    {
        //Desactivar los objetivos atacables.
        foreach (var fieldPosition in player2Manager.GetFieldPositionList())
        {
            if (fieldPosition.Card != null)
            {
                fieldPosition.Card.DesactiveSelectableTargets();
            }
        }
        foreach (var fieldPosition in player1Manager.GetFieldPositionList())
        {
            if (fieldPosition.Card != null)
            {
                fieldPosition.Card.DesactiveSelectableTargets();
            }
        }

        settingAttackTarget = false;
        cardSelectingTarget = null;
    }

    public List<Card> ObtainTargets(Card card, int movementToUseIndex) //Ajustar para obtener los objetivos para cada tipo de ataque.
    {
        List<Card> targets = new List<Card>();

        if(card.Moves[movementToUseIndex].MoveSO.MoveType != MoveType.PositiveEffect)
        {
            for (int i = 0; i < player2Manager.GetFieldPositionList().Count; i++)
            {
                if (player2Manager.GetFieldPositionList()[i].Card != null)
                {
                    targets.Add(player2Manager.GetFieldPositionList()[i].Card);
                }

                if (targets.Count > 0 && (i == 4 || i == 9 || i == 14) && card.Moves[movementToUseIndex].MoveSO.MoveType == MoveType.MeleeAttack)
                {
                    return targets;
                }
            }
        }
        else
        {
            foreach (var position in player1Manager.GetFieldPositionList())
            {
                if (position.Card != null && position.Card != card)
                {
                    targets.Add(position.Card);
                }
            }
        }
        

        return targets;
    }

    private IEnumerator HeroDirectAttack(int player, Card heroUsesTheAttack, int movementToUseIndex, bool lastMove)
    {
        
        //Iniciar la animacion de ataque.
        if (heroUsesTheAttack.Moves[movementToUseIndex].MoveSO.MoveType == MoveType.MeleeAttack)
        {
            yield return heroUsesTheAttack.MeleeAttackAnimation(player, null, heroUsesTheAttack.Moves[movementToUseIndex]);
        }
        else //if(attackerCard.Moves[movementToUseIndex].MoveSO.MoveType == MoveType.PositiveEffect)
        {
            yield return heroUsesTheAttack.RangedMovementAnimation();
        }

        yield return new WaitForSeconds(0.3f);

        if(player == 1)
        {
            if (player2Manager.ReceiveDamage(heroUsesTheAttack.Moves[movementToUseIndex]))
            {
                EndDuel(true);
            }
        }
        else
        {
            if (player1Manager.ReceiveDamage(heroUsesTheAttack.Moves[movementToUseIndex]))
            {
                EndDuel(false);
            }
        }

        heroUsesTheAttack.MoveToLastPosition();

        movementToUseIndex = -1;

        settingAttackTarget = false;
        cardSelectingTarget = null;

        if (lastMove) yield return FinishActions();
    }

    List<HeroAction> attackActions = new List<HeroAction>();
    List<HeroAction> effectActions = new List<HeroAction>();
    [ServerRpc(RequireOwnership = false)]
    public void HeroAttackServerRpc(int heroToAttackPositionIndex, ulong clientId, bool isHero, int heroUsesTheAttack,int movementToUseIndex)
    {
        if (isHero)
        {
            bool hasPriority = false;
            if (playerRoles[clientId] == 1)
            {
                player1Manager.GetFieldPositionList()[heroUsesTheAttack].Card.actionIsReady = true;
                hasPriority = player1Manager.GetFieldPositionList()[heroUsesTheAttack].Card.Moves[movementToUseIndex].MoveSO.MoveType == MoveType.PositiveEffect;
            }
            else
            {
                player2Manager.GetFieldPositionList()[heroUsesTheAttack].Card.actionIsReady = true;
                hasPriority = player2Manager.GetFieldPositionList()[heroUsesTheAttack].Card.Moves[movementToUseIndex].MoveSO.MoveType == MoveType.PositiveEffect;
            }
            if (hasPriority)
            {
                effectActions.Add(new HeroAction(heroToAttackPositionIndex, clientId, heroUsesTheAttack, movementToUseIndex));
            }
            else
            {
                attackActions.Add(new HeroAction(heroToAttackPositionIndex, clientId, heroUsesTheAttack, movementToUseIndex));
            }
        }
        else
        {
            StartCoroutine(HeroAttackServer(heroToAttackPositionIndex, clientId, isHero, heroUsesTheAttack, movementToUseIndex, true));
        }

        if (heroInTurn.All(hero => hero.actionIsReady)) 
        { 
            StartCoroutine(StartActions());

            foreach (var hero in HeroCardsOnTheField)
            {
                hero.actionIsReady = false;
            }
        }
        
    }

    private IEnumerator StartActions()
    {
        for (int i = 0; i < effectActions.Count; i++)
        {
            yield return HeroAttackServer(effectActions[i].heroToAttackPositionIndex, effectActions[i].clientId, true, effectActions[i].heroUsesTheAttack, effectActions[i].movementToUseIndex,
                i == effectActions.Count - 1 && attackActions.Count == 0);
            ActiveEffect();
        }
        effectActions.Clear();

        for (int i = 0; i < attackActions.Count; i++)
        {
            yield return HeroAttackServer(attackActions[i].heroToAttackPositionIndex, attackActions[i].clientId, true, attackActions[i].heroUsesTheAttack, attackActions[i].movementToUseIndex,
                i == attackActions.Count - 1);
        }
        attackActions.Clear();
    }

    private IEnumerator FinishActions()
    {

        ActiveEffect();

        if (duelPhase.Value == DuelPhase.PlayingSpellCard)
        {
            SendCardsToGraveyard();
        }
        else
        {
            yield return new WaitForSeconds(1);

            SendCardsToGraveyard();
        }

        if (IsClient) SetPlayerReadyServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    IEnumerator HeroAttackServer(int heroToAttackPositionIndex, ulong clientId, bool isHero, int heroUsesTheAttack, int movementToUseIndex, bool lastMove)
    {
        Card attackerCard = isHero ? 
            (playerRoles[clientId] == 1 ? player1Manager.GetFieldPositionList()[heroUsesTheAttack].Card : player2Manager.GetFieldPositionList()[heroUsesTheAttack].Card)
            : (playerRoles[clientId] == 1 ? player1Manager.SpellFieldPosition.Card : player2Manager.SpellFieldPosition.Card);

        HeroAttackClientRpc(heroToAttackPositionIndex, clientId, movementToUseIndex, isHero, heroUsesTheAttack, lastMove);

        if (playerRoles[clientId] == 1)
        {
            if (attackerCard.Moves[movementToUseIndex].MoveSO.MoveType != MoveType.PositiveEffect)
            {
                if (heroToAttackPositionIndex == -1)
                {
                    yield return HeroDirectAttack(1, attackerCard, movementToUseIndex, lastMove);
                }
                else
                {
                    Card card = player2Manager.GetFieldPositionList()[heroToAttackPositionIndex].Card;
                    yield return HeroAttack(card, 1, attackerCard, movementToUseIndex, lastMove);
                }
            }
            else
            {
                Card card = player1Manager.GetFieldPositionList()[heroToAttackPositionIndex].Card;
                yield return HeroAttack(card, 1, attackerCard, movementToUseIndex, lastMove);
            }
            
        }
        else if (playerRoles[clientId] == 2)
        {
            if (attackerCard.Moves[movementToUseIndex].MoveSO.MoveType != MoveType.PositiveEffect)
            {
                if (heroToAttackPositionIndex == -1)
                {
                    yield return HeroDirectAttack(2, attackerCard, movementToUseIndex, lastMove);
                }
                else
                {
                    Card card = player1Manager.GetFieldPositionList()[heroToAttackPositionIndex].Card;
                    yield return HeroAttack(card, 2, attackerCard, movementToUseIndex, lastMove);
                }
            }
            else
            {
                Card card = player2Manager.GetFieldPositionList()[heroToAttackPositionIndex].Card;
                yield return HeroAttack(card, 2, attackerCard, movementToUseIndex, lastMove);
            }
        }
        settingAttackTarget = false;
        cardSelectingTarget = null;
    }

    private IEnumerator HeroAttack(Card cardToAttack, int player, Card attackerCard, int movementToUseIndex, bool lastMove)
    {
        attackerCard.actionIsReady = false;
        attackerCard.EndTurn();

        //Iniciar la animacion de ataque.
        if (attackerCard.Moves[movementToUseIndex].MoveSO.MoveType == MoveType.MeleeAttack)
        {
            yield return attackerCard.MeleeAttackAnimation(player, cardToAttack, attackerCard.Moves[movementToUseIndex]);
        }
        else //if(attackerCard.Moves[movementToUseIndex].MoveSO.MoveType == MoveType.PositiveEffect)
        {
            yield return attackerCard.RangedMovementAnimation();
        }

        yield return new WaitForSeconds(0.3f);

        if (attackerCard.Moves[movementToUseIndex].MoveSO.Damage != 0) //Ataque de da�o.
        {
            //Animacion de da�o del oponente
            if (attackerCard.Moves[movementToUseIndex].MoveSO.TargetsType == TargetsType.SingleTarget)
            {
                cardToAttack.AnimationReceivingMovement(attackerCard.Moves[movementToUseIndex]);

                //Aplicar da�o al opnente.
                cardToAttack.ReceiveDamage(attackerCard.Moves[movementToUseIndex].MoveSO.Damage,
                    attackerCard.Moves[movementToUseIndex].MoveSO.MoveEffect is IgnoredDefense ignored ? ignored.Amount : 0);
            }
            else
            {
                var targets = GetTargetsForMovement(cardToAttack, attackerCard, movementToUseIndex);
                foreach (var card in targets)
                {
                    card.AnimationReceivingMovement(attackerCard.Moves[movementToUseIndex]);
                }

                foreach (var card in targets) 
                {
                    //Aplicar da�o al opnente.
                    card.ReceiveDamage(attackerCard.Moves[movementToUseIndex].MoveSO.Damage,
                        attackerCard.Moves[movementToUseIndex].MoveSO.MoveEffect is IgnoredDefense ignored ? ignored.Amount : 0);
                }
            }
            
        }
        else //Ataque de efecto
        {
            //Animacion de efecto
            if (attackerCard.Moves[movementToUseIndex].MoveSO.TargetsType == TargetsType.SingleTarget)
            {
                cardToAttack.AnimationReceivingMovement(attackerCard.Moves[movementToUseIndex]);
            }
            else
            {
                foreach (var card in GetTargetsForMovement(cardToAttack, attackerCard, movementToUseIndex))
                {
                    card.AnimationReceivingMovement(attackerCard.Moves[movementToUseIndex]);
                }
            }

            yield return new WaitForSeconds(1);
        }


        attackerCard.MoveToLastPosition();

        //Aplicar afecto de ataque si es necesario.
        if (attackerCard.Moves[movementToUseIndex].MoveSO.TargetsType == TargetsType.SingleTarget)
        {
            if(!attackerCard.Moves[movementToUseIndex].MoveSO.IsPassiveEffect) 
                attackerCard.Moves[movementToUseIndex].ActivateEffect(attackerCard, cardToAttack);
        }
        else
        {
            if (!attackerCard.Moves[movementToUseIndex].MoveSO.IsPassiveEffect) 
                attackerCard.Moves[movementToUseIndex].ActivateEffect(attackerCard, GetTargetsForMovement(cardToAttack, attackerCard, movementToUseIndex));
        }


        movementToUseIndex = -1;
        if (attackerCard.cardSO is SpellCardSO)
        {
            if(player1Manager.SpellFieldPosition.Card == attackerCard)
            {
                attackerCard.FieldPosition.DestroyCard(player1Manager.GetGraveyard(), true);
            }
            else
            {
                attackerCard.FieldPosition.DestroyCard(player2Manager.GetGraveyard(), false);
            }
            
        }
        yield return new WaitForSeconds(1); 

        if (lastMove) yield return FinishActions();
    }

    private List<Card> GetTargetsForMovement(Card cardToAttack, Card attackerCard, int movementToUseIndex)
    {
        if(attackerCard.Moves[movementToUseIndex].MoveSO.TargetsType == TargetsType.TargetLine)
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
        else if(attackerCard.Moves[movementToUseIndex].MoveSO.TargetsType == TargetsType.Midfield)
        {
            if (player1Manager.GetFieldPositionList().Contains(attackerCard.FieldPosition))
            {
                if (attackerCard.Moves[movementToUseIndex].MoveSO.MoveType == MoveType.PositiveEffect)
                {
                    return player1Manager.GetAllCardInField(cardToAttack);
                }
                else
                {
                    return player2Manager.GetAllCardInField(cardToAttack);
                }
            }
            else
            {
                if (attackerCard.Moves[movementToUseIndex].MoveSO.MoveType == MoveType.PositiveEffect)
                {
                    return player2Manager.GetAllCardInField(cardToAttack);
                }
                else
                {
                    return player1Manager.GetAllCardInField(cardToAttack);
                }
            }
        }

        return null;
    }

    private void SendCardsToGraveyard()
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
    private void HeroAttackClientRpc(int fieldPositionIndex, ulong attackerClientId, int movementToUseIndex, bool isHero, int heroUsesTheAttack, bool lastMove)
    {
        StartCoroutine(HeroAttackClient(fieldPositionIndex, attackerClientId, isHero, heroUsesTheAttack, movementToUseIndex, lastMove));
    }
    private IEnumerator HeroAttackClient(int fieldPositionIndex, ulong attackerClientId, bool isHero, int heroUsesTheAttack, int movementToUseIndex, bool lastMove)
    {
        if (IsHost) yield break;
        Card attackerCard = isHero ? (NetworkManager.Singleton.LocalClientId == attackerClientId ? player1Manager.GetFieldPositionList()[heroUsesTheAttack].Card : player2Manager.GetFieldPositionList()[heroUsesTheAttack].Card)
            : (NetworkManager.Singleton.LocalClientId == attackerClientId ? player1Manager.SpellFieldPosition.Card : player2Manager.SpellFieldPosition.Card);

        if (NetworkManager.Singleton.LocalClientId == attackerClientId)
        {

            if (attackerCard.Moves[movementToUseIndex].MoveSO.MoveType != MoveType.PositiveEffect)
            {
                if (fieldPositionIndex == -1)
                {
                    yield return HeroDirectAttack(1, attackerCard, movementToUseIndex, lastMove);
                }
                else
                {
                    Card card = player2Manager.GetFieldPositionList()[fieldPositionIndex].Card;
                    yield return HeroAttack(card, 1, attackerCard, movementToUseIndex, lastMove);
                }
            }
            else
            {
                Card card = player1Manager.GetFieldPositionList()[fieldPositionIndex].Card;
                yield return HeroAttack(card, 1, attackerCard, movementToUseIndex, lastMove);
            }
        }
        else
        {
            if (attackerCard.Moves[movementToUseIndex].MoveSO.MoveType != MoveType.PositiveEffect)
            {
                if (fieldPositionIndex == -1)
                {
                    yield return HeroDirectAttack(2, attackerCard, movementToUseIndex, lastMove);
                }
                else
                {
                    Card card = player1Manager.GetFieldPositionList()[fieldPositionIndex].Card;
                    yield return HeroAttack(card, 2, attackerCard, movementToUseIndex, lastMove);
                }
                
            }
            else
            {
                Card card = player2Manager.GetFieldPositionList()[fieldPositionIndex].Card;
                yield return HeroAttack(card, 2, attackerCard, movementToUseIndex, lastMove);
            }
        }

        settingAttackTarget = false;
        cardSelectingTarget = null;
    }

    private void NextTurn()
    {
        if(heroesInTurnIndex < turns.Length - 1)
        {
            heroesInTurnIndex++;
            StartHeroTurnClientRpc(heroesInTurnIndex);
            StartHeroTurn();
        }
        else
        {
            heroesInTurnIndex = 0;
            RemoveDestroyedCards();
            RegenerateDefense();
            duelPhase.Value = DuelPhase.DrawingCards;
        }
    }

    [ClientRpc]
    private void StartHeroTurnClientRpc(int heroesInTurnIndex)
    {
        if (IsHost) return;
        this.heroesInTurnIndex = heroesInTurnIndex;
        StartHeroTurn();
        Debug.Log(heroesInTurnIndex);
    }

    // M�todo para remover las cartas sin vida
    private void RemoveDestroyedCards()
    {
        HeroCardsOnTheField.RemoveAll(card => card.FieldPosition == null);
        RemoveDestroyedCardsClientRpc();
    }

    [ClientRpc]
    private void RemoveDestroyedCardsClientRpc()
    {
        if(IsHost)return;
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

    private void EndDuel(bool playerVictory)
    {
        endDuelUI.Show(playerVictory);
    }
}

[Serializable]
public class HeroAction
{
    public int heroToAttackPositionIndex;
    public ulong clientId;
    public int heroUsesTheAttack;
    public int movementToUseIndex;
    public HeroAction(int heroToAttackPositionIndex, ulong clientId, int heroUsesTheAttack, int movementToUseIndex)
    {
        this.heroToAttackPositionIndex = heroToAttackPositionIndex;
        this.clientId = clientId;
        this.heroUsesTheAttack = heroUsesTheAttack;
        this.movementToUseIndex = movementToUseIndex;
    }
}
