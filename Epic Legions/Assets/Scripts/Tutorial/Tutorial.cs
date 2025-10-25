using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.ParticleSystem;
using static UnityEngine.Rendering.DebugUI.Table;

public class Tutorial : DuelManager
{
    [SerializeField] private ParticleSystem particleTutorial;
    public GameObject[] explanationTextsBoxs;

    int turnCount = 0;
    public bool explanationFinished = false;
    private bool firstHero = true;
    private bool firstAttack = true;
    private bool knowsHeroCards = false;
    private bool knowsWeaponCards = false;
    private bool knowsAttireCards = false;
    private bool knowsAccessoryCards = false;
    private bool knowsSpellCards = false;
    private bool knowsSelectTarget = false;
    private bool onClick = false;
    public bool onAction = false;
    public bool isPauseGame = false;

    public CardSelectorTutorial cardSelectorTutorial;
    public FieldPosition availablePosition;
    private List<Card> invalidTargetList = new List<Card>();
    private void Awake()
    {
        duelPhase.OnValueChanged += OnDuelPhaseChanged;
        DontDestroyOnLoad(gameObject);

        for (int i = 0; i < turns.Length; i++)
        {
            turns[i] = new List<Card>();
        }
    }

    private void Start()
    {
        isSinglePlayer = Loader.isSinglePlayer;
        if (isSinglePlayer)
        {
            AssignPlayersAndStartDuel(Loader.player1deckCardIds.ToArray(), Loader.player2deckCardIds.ToArray());
        }
    }

    public new void AssignPlayersAndStartDuel(int[] player1deckCardIds, int[] player2deckCardIds)
    {
        endDuel = false;

        // Asigna roles a los jugadores
        playerRoles[1] = 1;
        playerRoles[2] = 2;

        // Asigna los identificadores de los jugadores en la partida
        playerId[1] = 1;
        playerId[2] = 2;

        // Marca a los jugadores como no listos al inicio
        playerReady[0] = false;
        playerReady[1] = false;

        playerDecks[0] = player1deckCardIds.ToList(); 
        playerDecks[1] = player2deckCardIds.ToList();

        // Inicia el duelo después del registro
        InitializeDuel();
    }

    private new void OnDuelPhaseChanged(DuelPhase oldPhase, DuelPhase newPhase)
    {
        StartCoroutine(DuelPhaseChanged(oldPhase, newPhase));
    }

    IEnumerator DuelPhaseChanged(DuelPhase oldPhase, DuelPhase newPhase)
    {
        if (endDuel)
        {
            yield break;
        }
        UpdateDuelPhaseText();

        if (newPhase == DuelPhase.PreparingDuel)
        {
            SetDecks();
        }
        else if (newPhase == DuelPhase.Starting)
        {
            foreach(var card in player1Manager.CardList)
            {
                card.CardBorder.gameObject.SetActive(false);
            }
            yield return new WaitForSeconds(2f);
            yield return FieldExplanation();
            player1Manager.DrawStartCards();
            player2Manager.DrawStartCards();
        }
        else if (newPhase == DuelPhase.Preparation)
        {
            player1Manager.HideWaitTextGameObject();
            player1Manager.HideNextPhaseButton();
            player1Manager.GetHandCardHandler().ShowHandCard();
            player1Manager.GetHandCardHandler().ShowingCards = true;

            if (oldPhase != DuelPhase.PlayingSpellCard)
            {
                player1Manager.GetHandCardHandler().ShowHandCard();
                turnCount++;

                if (turnCount == 1)
                {
                    StartCoroutine(PhaseExplanation());
                }
                else if (turnCount == 2)
                {
                    yield return SecondTurnPlayHero();
                }
                else if (turnCount == 3)
                {
                    yield return ThirdTurnPlayHero();
                }
                else if (turnCount == 4)
                {
                    yield return SpellPresentation();
                }
                else if (turnCount == 5)
                {
                    yield return PlayKaelis();
                }
                else if (turnCount == 6)
                {
                    yield return PlayAquelir();
                }
            }
            else
            {
                if (turnCount == 4)
                {
                    yield return WeaponPresentation();
                }
            }

            
        }
        else if (newPhase == DuelPhase.Battle)
        {
            if (oldPhase != DuelPhase.PlayingSpellCard)
            {
                FlipCardsInFiled();

                AudioManager.Instance.PlayPhaseChanged();

                player1Manager.HideWaitTextGameObject();

                InitializeBattleTurns();

                StartCoroutine(StartHeroTurn());

                if (IsClient || isSinglePlayer)
                {
                    player1Manager.GetHandCardHandler().HideHandCards();
                }

            }
            else
            {
                InitializeHeroTurn();
            }

            if (firstHero)
            {
                firstHero = false;
                yield return ElementExplanation();
            }
        }
        else if (newPhase == DuelPhase.DrawingCards)
        {
            if(turnCount == 1)  yield return DrawCardExplanation();
            if (turnCount == 3) yield return ThirdTurnFinishedExplanation();

            foreach (var item in HeroCardsOnTheField)
            {
                item.turnCompleted = false;
            }

            if (player1Manager.DrawCard())
            {
                player1Manager.RechargeEnergy(energyGainedPerTurn);
            }
            if (player2Manager.DrawCard())
            {
                player2Manager.RechargeEnergy(energyGainedPerTurn);
            }
            
        }
        else if (newPhase == DuelPhase.PlayingSpellCard)
        {
            oldDuelPhase = oldPhase;
            player1Manager.GetHandCardHandler().HideHandCards();
            player1Manager.HideNextPhaseButton();
        }
    }

    public IEnumerator OnClick(Card card)
    {
        if (turnCount == 4 && sampleCard.IsEnlarged)
        {
            if (sampleCard.Cards[1] == card)
            {
                onAction = true;
                Debug.Log("Clicked on weapon card");
            }
        }

        onClick = true;
        yield return null;
        onClick = false;

        
    }

    private IEnumerator FieldExplanation()
    {
        yield return ShowText(0);
        yield return ShowText(1);
        yield return ShowText(2);
        yield return ShowText(3);
        yield return ShowText(4);
    }

    IEnumerator PhaseExplanation()
    {
        yield return ShowText(27);
        yield return ShowText(25);

        explanationFinished = true;
        StartCoroutine(HandExplanation());
    }

    IEnumerator HandExplanation()
    {
        yield return ShowText(26);
        yield return ShowText(5);

        SetSelectable(player1Manager.GetHandCardHandler().GetCardInHandList()[1]);
        yield return ShowText(6, true, false);
        StartCoroutine(ShowHeroCardExplanation());
    }

    private IEnumerator ShowHeroCardExplanation()
    {
        yield return new WaitForSeconds(0.1f);

        player1Manager.GetHandCardHandler().HideHandCards();
        yield return ShowText(7);
        yield return ShowText(8);
        yield return ShowText(9);
        yield return ShowText(10);
        yield return ShowText(11);
        yield return ShowText(12);
        yield return ShowText(13);
        yield return ShowText(14, true, false);
        StartCoroutine(PlayFirstTurnExplanation());
    }

    private IEnumerator PlayFirstTurnExplanation()
    {
        yield return ShowText(15);
        player1Manager.GetHandCardHandler().GetCardInHandList()[1].CardBorder.gameObject.SetActive(true);
        SetDraggable(player1Manager.GetHandCardHandler().GetCardInHandList()[1], 7);
        yield return ShowText(16, true, false);
    }

    private IEnumerator NextPhaseExplanation()
    {
        yield return ShowText(17);
        player1Manager.ShowNextPhaseButton();
    }

    private IEnumerator ElementExplanation()
    {
        yield return ShowText(18);
        yield return ShowText(66);
        yield return ShowText(67);
        yield return ShowText(68);
        yield return ShowElementGraph();
        yield return YourTurnExplanation();
    }

    [SerializeField] private TextMeshProUGUI elementGraphText;
    [SerializeField] private string[] elementGraphString;
    [SerializeField] private RectTransform elementGraphRectTransform;
    [SerializeField] private Vector3[] elementGraphPos;
    private IEnumerator ShowElementGraph()
    {
        isPauseGame = true;
        explanationTextsBoxs[19].SetActive(true);

        elementGraphRectTransform.localPosition = elementGraphPos[0];
        yield return SetText(elementGraphText, elementGraphString[0]);
        yield return new WaitUntil(() => onClick);

        elementGraphRectTransform.localPosition = elementGraphPos[1];
        yield return SetText(elementGraphText, elementGraphString[1]);
        yield return new WaitUntil(() => onClick);

        elementGraphRectTransform.localPosition = elementGraphPos[2];
        yield return SetText(elementGraphText, elementGraphString[2]);
        yield return new WaitUntil(() => onClick);

        elementGraphRectTransform.localPosition = elementGraphPos[3];
        yield return SetText(elementGraphText, elementGraphString[3]);
        yield return new WaitUntil(() => onClick);

        isPauseGame = false;
        explanationTextsBoxs[19].SetActive(false);

    }

    private IEnumerator SetText(TextMeshProUGUI textComponent, string fullText)
    {
        textComponent.text = "";
        foreach (char c in fullText)
        {
            textComponent.text += c;
            yield return new WaitForSeconds(0.02f);
        }
    }

    private IEnumerator YourTurnExplanation()
    {
        yield return ShowText(20);
        yield return ShowText(21);
    }
    private IEnumerator UseMovementExplanation()
    {
        firstAttack = false;
        yield return ShowText(22);
        yield return ShowText(23);
        sampleCard.Cards[0].RechargeButton.interactable = true;
    }

    private IEnumerator DrawCardExplanation()
    {
        yield return ShowText(36);
        yield return ShowText(69);
        yield return new WaitForSeconds(0.5f);  
    }
    private IEnumerator SecondTurnPlayHero()
    {
        yield return ShowText(28);

        var cardInHand = player1Manager.GetHandCardHandler().GetCardInHandList()[1];

        SetSelectable(cardInHand);
        SetDraggable(cardInHand, 2);

        yield return new WaitUntil(() => player1Manager.GetAllCardInField().Count == 2);
        player1Manager.ShowNextPhaseButton();

    }
    private IEnumerator ThirdTurnFinishedExplanation()
    {
        yield return ShowText(33);
        yield return ShowText(82);
    }

    private IEnumerator ThirdTurnPlayHero()
    {
        yield return ShowText(30);
        yield return ShowText(31);

        SetSelectable(player1Manager.GetHandCardHandler().GetCardInHandList()[2]);
        SetDraggable(player1Manager.GetHandCardHandler().GetCardInHandList()[2], 3);

        yield return new WaitUntil(() => player1Manager.GetAllCardInField().Count == 3);
        player1Manager.ShowNextPhaseButton();
    }

    private IEnumerator SpellPresentation()
    {
        SetSelectable(player1Manager.GetHandCardHandler().GetCardInHandList()[1]);
        yield return ShowText(34, true, false);
    }

    private IEnumerator SpellExplanation()
    {
        yield return ShowText(35);
        yield return ShowText(37);

        var cardInHand = player1Manager.GetHandCardHandler().GetCardInHandList()[1];
        cardInHand.CardBorder.gameObject.SetActive(true);
        SetDraggable(cardInHand, 7);


        invalidTargetList.Add(player1Manager.GetFieldPositionList()[2].Card);
        invalidTargetList.Add(player1Manager.GetFieldPositionList()[3].Card);
    }

    private IEnumerator WeaponPresentation()
    {
        SetSelectable(player1Manager.GetHandCardHandler().GetCardInHandList()[0]);
        yield return ShowText(38, false, false);
    }

    private IEnumerator WeaponExplanation()
    {
        yield return ShowText(39);
        yield return ShowText(40);
        yield return ShowText(41);
        SetDraggable(player1Manager.GetHandCardHandler().GetCardInHandList()[0], 2);
        yield return new WaitUntil(() => player1Manager.GetAllCardInField()[0].GetEquipmentCounts() == 1);
        StartCoroutine(AttirePresentation());
    }

    private IEnumerator AttirePresentation()
    {
        SetSelectable(player1Manager.GetHandCardHandler().GetCardInHandList()[2]);
        yield return ShowText(42, false, false);
    }

    private IEnumerator AttireExplanation()
    {
        yield return ShowText(43);
        yield return ShowText(44);
        yield return ShowText(45);
        SetDraggable(player1Manager.GetHandCardHandler().GetCardInHandList()[2], 2);
        yield return new WaitUntil(() => player1Manager.GetAllCardInField()[0].GetEquipmentCounts() == 2);
        StartCoroutine(AccessoryPresentation());
    }

    private IEnumerator AccessoryPresentation()
    {
        SetSelectable(player1Manager.GetHandCardHandler().GetCardInHandList()[1]);
        yield return ShowText(46, false, false);
    }

    
    private IEnumerator AccessoryExplanation()
    {
        yield return ShowText(47);
        yield return ShowText(48);
        yield return ShowText(49);
        SetDraggable(player1Manager.GetHandCardHandler().GetCardInHandList()[1], 2);
        yield return new WaitUntil(() => player1Manager.GetAllCardInField()[0].GetEquipmentCounts() == 3);
        StartCoroutine(EquippableCardExplanation());
    }

    private IEnumerator EquippableCardExplanation()
    {
        yield return ShowText(50);
        yield return ShowText(51);
        player1Manager.ShowNextPhaseButton();
    }

    private IEnumerator PlayKaelis()
    {
        yield return ShowText(55, true, false);
        yield return new WaitWhile(() => sampleCard.IsEnlarged);
        yield return ShowText(56);
        yield return ShowText(57);
        yield return ShowText(58);
        yield return ShowText(59);
        SetSelectable(player1Manager.GetHandCardHandler().GetCardInHandList()[3]);
        SetDraggable(player1Manager.GetHandCardHandler().GetCardInHandList()[3], 8);
        yield return new WaitUntil(() => player1Manager.GetAllCardInField().Count == 4);
        StartCoroutine(IncreaseAttack());
    }

    private IEnumerator IncreaseAttack()
    {
        yield return ShowText(60);
        SetSelectable(player1Manager.GetHandCardHandler().GetCardInHandList()[1]);

        invalidTargetList.Add(player1Manager.GetFieldPositionList()[8].Card);
    }

    private IEnumerator PlayAquelir()
    {
        yield return ShowText(74);
        yield return ShowText(75);
        SetSelectable(player1Manager.GetHandCardHandler().GetCardInHandList()[2]);
        SetDraggable(player1Manager.GetHandCardHandler().GetCardInHandList()[2], 7);
        yield return new WaitUntil(() => player1Manager.GetAllCardInField().Count == 4);
        player1Manager.ShowNextPhaseButton();
        invalidTargetList.Clear();
    }

    public void OnPlaceCard(Card card)
    {
        if(firstHero && card.cardSO is HeroCardSO)
        {
            StartCoroutine(NextPhaseExplanation());
        }

        cardSelectorTutorial.draggableCards.Remove(card);
    }

    bool coroutineRunning;
    private IEnumerator ShowText(int index, bool waitAction = false, bool pauseGame = true, GameObject particle = null)
    {
        yield return new WaitUntil(() => !coroutineRunning);
        coroutineRunning = true;
        yield return ShowTextCoroutine(index, waitAction, pauseGame, particle);
        coroutineRunning = false;
    }

    private IEnumerator ShowTextCoroutine(int index, bool waitAction, bool pauseGame, GameObject particle)
    {
        isPauseGame = pauseGame;
        explanationTextsBoxs[index].SetActive(true);
        yield return ToggleText(index, Vector3.one);
        onAction = false;
        yield return new WaitUntil(() => waitAction ? onAction : onClick);
        if (particle != null) particle.SetActive(false);
        isPauseGame = true;
        yield return ToggleText(index, Vector3.zero);
        explanationTextsBoxs[index].SetActive(false);
        isPauseGame = false;
        onAction = false;
    }

    private IEnumerator ToggleText(int textIndex, Vector3 targetScale)
    {
        explanationTextsBoxs[textIndex].transform.localScale = targetScale == Vector3.zero ? Vector3.one : Vector3.zero;
        Vector3 initialScale = explanationTextsBoxs[textIndex].transform.localScale;
        float elapsedTime = 0f;
        float duration = 0.3f;
        while (elapsedTime < duration)
        {
            explanationTextsBoxs[textIndex].transform.localScale = Vector3.Lerp(initialScale, targetScale, (elapsedTime / duration));
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }
        explanationTextsBoxs[textIndex].transform.localScale = targetScale;
        yield return null;
    }

    public IEnumerator OnEnlargueCard(Card card)
    {
        if (card.cardSO is HeroCardSO)
        {
            if (!knowsHeroCards)
            {
                onAction = true;
                yield return null;
                
            }
            else if (firstAttack && (player1Manager.GetAllCardInField().Count > 0 && heroInTurn.Contains(player1Manager.GetAllCardInField()[0])))
            {
                StartCoroutine(UseMovementExplanation());
            }
            else if (player1Manager.GetAllCardInField().Contains(card) && heroInTurn.Contains(card))
            {
                sampleCard.Cards[0].MovementUI1.DisableButton();
                sampleCard.Cards[1].MovementUI1.DisableButton();
                sampleCard.Cards[0].MovementUI2.DisableButton();
                sampleCard.Cards[0].RechargeButton.interactable = false;

                if (turnCount == 2)
                {
                    if (card.cardSO.CardID == 1002)
                    {
                        yield return ShowText(29);
                        sampleCard.Cards[0].RechargeButton.interactable = true;
                    }
                    else if (card.cardSO.CardID == 1051)
                    {
                        yield return ShowText(70);
                        sampleCard.Cards[0].MovementUI1.EnableButton();
                    }
                }
                else if (turnCount == 3)
                {
                    if (card.cardSO.CardID == 1002)
                    {
                        yield return ShowText(32);
                        sampleCard.Cards[0].MovementUI1.EnableButton();
                    }
                    else if (card.cardSO.CardID == 1051)
                    {
                        yield return ShowText(71);
                        sampleCard.Cards[0].MovementUI2.EnableButton();
                    }
                    else if (card.cardSO.CardID == 1025)
                    {
                        yield return ShowText(72);
                        sampleCard.Cards[0].MovementUI1.EnableButton();
                    }
                }
                else if (turnCount == 4)
                {
                    if (card.cardSO.CardID == 1002)
                    {
                        yield return ShowText(52);
                        sampleCard.Cards[0].MovementUI1.EnableButton();
                    }
                    else if (card.cardSO.CardID == 1051)
                    {
                        yield return ShowText(53, true, false, particleTutorial.gameObject);
                        sampleCard.Cards[1].MovementUI1.EnableButton();
                    }
                    else if (card.cardSO.CardID == 1025)
                    {
                        yield return ShowText(54);
                        sampleCard.Cards[0].RechargeButton.interactable = true;
                    }
                }
                else if (turnCount == 5)
                {
                    if (card.cardSO.CardID == 1052)
                    {
                        yield return ShowText(63);
                        invalidTargetList.Add(player2Manager.GetFieldPositionList()[7].Card);
                        sampleCard.Cards[0].MovementUI1.EnableButton();
                    }
                    else if (card.cardSO.CardID == 1051)
                    {
                        yield return ShowText(64);
                        sampleCard.Cards[0].RechargeButton.interactable = true;
                    }
                    else if (card.cardSO.CardID == 1025)
                    {
                        yield return ShowText(65);
                        sampleCard.Cards[0].RechargeButton.interactable = true;
                    }
                }
                else if (turnCount == 6)
                {
                    if (card.cardSO.CardID == 1052)
                    {
                        yield return ShowText(76);
                        sampleCard.Cards[0].MovementUI1.EnableButton();
                    }
                    else if (card.cardSO.CardID == 1051)
                    {
                        yield return ShowText(81);
                        yield return ShowText(77);
                        sampleCard.Cards[1].MovementUI1.EnableButton();
                    }
                    else if (card.cardSO.CardID == 1025)
                    {
                        yield return ShowText(78);
                        sampleCard.Cards[0].MovementUI1.EnableButton();
                    }
                    else if( card.cardSO.CardID == 1097)
                    {
                        yield return ShowText(79);
                        sampleCard.Cards[0].MovementUI1.EnableButton();
                    }
                }
            }
            else if (player2Manager.GetAllCardInField().Contains(card))
            {
                if (turnCount == 5 && card.cardSO.CardID == 1096)
                {
                    onAction = true;
                }
            }
        }
        else if(card.cardSO is EquipmentCardSO equipmentCard )
        {
            if (equipmentCard.EquipmentType == EquipmentType.Weapon && !knowsWeaponCards)
            {
                knowsWeaponCards = true;
                StartCoroutine(WeaponExplanation());
            }
            else if (equipmentCard.EquipmentType == EquipmentType.Armor && !knowsAttireCards)
            {
                knowsAttireCards = true;
                StartCoroutine(AttireExplanation());
            }
            else if (equipmentCard.EquipmentType == EquipmentType.Accessory && !knowsAccessoryCards)
            {
                knowsAccessoryCards = true;
                StartCoroutine(AccessoryExplanation());
            }
                
            
        }
        else if(card.cardSO is SpellCardSO)
        {
            if (!knowsSpellCards)
            {
                onAction = true;
                knowsSpellCards = true;
                StartCoroutine(SpellExplanation());
            }
            else if(turnCount == 5 && card.cardSO.CardID == 1100)
            {
                SetDraggable(player1Manager.GetHandCardHandler().GetCardInHandList()[1], 7);
                StartCoroutine(ShowText(61));
                yield return new WaitUntil(() => player1Manager.GetAllCardInField().Count == 3);
                yield return ShowText(62);

                player1Manager.ShowNextPhaseButton();
            }
        }
    }

    

    public void OnResizeCard()
    {
        if(!knowsHeroCards)
        {
            knowsHeroCards = true;
        }
        if(turnCount != 5)onAction = true;
    }

    public void SetSelectable(Card card)
    {
        card.CardBorder.gameObject.SetActive(true);
        cardSelectorTutorial.selectableCards.Add(card);
    }

    private void SetDraggable(Card card, int fieldIndex)
    {
        cardSelectorTutorial.draggableCards.Add(card);
        availablePosition = player1Manager.GetFieldPositionList()[fieldIndex];
    }

    public override List<Card> ObtainTargets(Card card, int movementToUseIndex)
    {
        if(!knowsSelectTarget && turnCount == 3 && settingAttackTarget)
        {
            knowsSelectTarget = true;
            StartCoroutine(ShowText(73));
        }
        // Lista que contendrá los objetivos posibles
        List<Card> targets = new List<Card>();

        // Si el movimiento no es un efecto positivo, buscamos enemigos en el campo del jugador contrario
        if (card.Moves[movementToUseIndex].MoveSO.MoveType != MoveType.PositiveEffect)
        {
            if (card.Moves[movementToUseIndex].MoveSO.TargetsType == TargetsType.DIRECT)
            {
                targets.Add(card);
            }
            else
            {
                var FieldPositionList = card.GetController() != null ? GetPlayerManagerForCard(card).GetFieldPositionList() : GetPlayerManagerRival(card).GetFieldPositionList();
                var rivalField = new List<FieldPosition>(FieldPositionList);
                rivalField.Remove(card.FieldPosition);

                if (card.GetController() != null ||
                    (card.cardSO is HeroCardSO hero && hero.HeroClass == HeroClass.Hunter
                    && card.Moves[movementToUseIndex].MoveSO.MoveType == MoveType.RangedAttack))
                {
                    for (int i = 0; i < rivalField.Count; i++)
                    {
                        TryAddTarget(rivalField[i].Card, card, movementToUseIndex, targets);
                    }
                }
                else if (card.Moves[movementToUseIndex].MoveSO.MoveType == MoveType.MeleeAttack
                    || card.Moves[movementToUseIndex].MoveSO.MoveType == MoveType.RangedAttack)
                {
                    for (int i = 0; i < rivalField.Count; i++)
                    {
                        if (rivalField[i].PositionIndex < 5)
                        {
                            TryAddTarget(rivalField[i].Card, card, movementToUseIndex, targets);
                        }
                        else if (rivalField[i].PositionIndex < 10)
                        {
                            if (FieldPositionList[rivalField[i].PositionIndex - 5].Card == null)
                            {
                                TryAddTarget(rivalField[i].Card, card, movementToUseIndex, targets);
                            }
                        }
                        else if (rivalField[i].PositionIndex < 15)
                        {
                            if (FieldPositionList[rivalField[i].PositionIndex - 5].Card == null &&
                                FieldPositionList[rivalField[i].PositionIndex - 10].Card == null)
                            {
                                TryAddTarget(rivalField[i].Card, card, movementToUseIndex, targets);
                            }
                        }

                        if (card.cardSO is HeroCardSO heroCard && heroCard.HeroClass == HeroClass.Assassin)
                        {
                            if (rivalField[i].PositionIndex > 9)
                            {
                                TryAddTarget(rivalField[i].Card, card, movementToUseIndex, targets);
                            }
                            else if (rivalField[i].PositionIndex > 4)
                            {
                                if (FieldPositionList[rivalField[i].PositionIndex + 5].Card == null)
                                {
                                    TryAddTarget(rivalField[i].Card, card, movementToUseIndex, targets);
                                }
                            }
                            else if (rivalField[i].PositionIndex >= 0)
                            {
                                if (FieldPositionList[rivalField[i].PositionIndex + 5].Card == null &&
                                    FieldPositionList[rivalField[i].PositionIndex + 10].Card == null)
                                {
                                    TryAddTarget(rivalField[i].Card, card, movementToUseIndex, targets);
                                }
                            }
                        }
                    }
                }
            }
        }
        else
        {
            // Si el movimiento es un efecto positivo, los objetivos serán las cartas del jugador aliado (jugador 1),
            // pero no se puede seleccionar la misma carta que está atacando
            var field = card.GetController() != null ? GetPlayerManagerRival(card).GetFieldPositionList() : GetPlayerManagerForCard(card).GetFieldPositionList();
            foreach (var position in field)
            {
                TryAddTarget(position.Card, card, movementToUseIndex, targets);
            }
        }

        foreach (var invalidTarget in invalidTargetList)
        {
            targets.Remove(invalidTarget);
        }

        // Retornamos la lista de objetivos posibles
        return targets;
    }

    

    public override void EndDuel(bool playerVictory)
    {
        StartCoroutine(EndDuelCoroutine(playerVictory));
    }

    private IEnumerator EndDuelCoroutine(bool playerVictory)
    {
        endDuel = true;
        yield return ShowText(80);
        endDuelUI.Show(playerVictory);
    }

}
