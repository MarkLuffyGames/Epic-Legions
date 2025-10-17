using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Tutorial : DuelManager
{
    public GameObject[] explanationTextsBoxs;

    public bool isPauseGame = false;

    int turnCount = 0;
    public bool explanationFinished = false;
    private bool firstHero = true;
    private bool firstAttack = true;
    private bool knowsHeroCards = false;
    private bool knowsWeaponCards = false;
    private bool knowsAttireCards = false;
    private bool knowsAccessoryCards = false;
    private bool knowsSpellCards = false;
    private bool onClick = false;
    public bool canCardHeld = false;


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
        UpdateDuelPhaseText();

        if (newPhase == DuelPhase.PreparingDuel)
        {
            SetDecks();
        }
        else if (newPhase == DuelPhase.Starting)
        {
            yield return new WaitForSeconds(2f);
            yield return FieldExplanation();
            sampleCard.enabled = false;
            player1Manager.DrawStartCards();
            player2Manager.DrawStartCards();
        }
        else if (newPhase == DuelPhase.Preparation)
        {
            player1Manager.HideWaitTextGameObject();
            player1Manager.ShowNextPhaseButton();
            player1Manager.GetHandCardHandler().ShowHandCard();
            player1Manager.GetHandCardHandler().ShowingCards = true;

            if (oldPhase != DuelPhase.PlayingSpellCard)
            {
                player1Manager.GetHandCardHandler().ShowHandCard();
                turnCount++;

                if (turnCount == 1)
                {
                    player1Manager.HideNextPhaseButton();
                    yield return PhaseExplanation();
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

                if (turnCount == 2)
                {
                    yield return SecondTurnActions();
                }
                else if (turnCount == 3)
                {
                    yield return ThirdTurnActions();
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

    public IEnumerator OnClick()
    {
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
        yield return HandExplanation();
    }

    IEnumerator HandExplanation()
    {
        yield return ShowText(26);
        yield return ShowText(5);
        yield return ShowText(6);
    }

    private IEnumerator ShowHeroCardExplanation(Card card)
    {
        sampleCard.enabled = false;
        yield return new WaitForSeconds(0.1f);
        player1Manager.GetHandCardHandler().HideHandCards();
        yield return ShowText(7);
        yield return ShowText(8);
        yield return ShowText(9);
        yield return ShowText(10);
        yield return ShowText(11);
        yield return ShowText(12);
        yield return ShowText(13);
        yield return ShowText(14);
    }

    private IEnumerator PlayFirstTurnExplanation()
    {
        yield return ShowText(15);
        yield return ShowText(16);
    }

    private IEnumerator NextPhaseExplanation()
    {
        yield return ShowText(17);
        player1Manager.ShowNextPhaseButton();
    }

    private IEnumerator ElementExplanation()
    {
        yield return ShowText(18);
        yield return ShowText(19);
        yield return YourTurnExplanation();
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
    }

    private IEnumerator DrawCardExplanation()
    {
        yield return ShowText(36);
        yield return new WaitForSeconds(0.5f);  
    }
    private IEnumerator SecondTurnPlayHero()
    {
        yield return ShowText(28);
    }
    private IEnumerator SecondTurnActions()
    {
        yield return ShowText(29);
    }
    private IEnumerator ThirdTurnFinishedExplanation()
    {
        yield return ShowText(33);
    }

    private IEnumerator ThirdTurnPlayHero()
    {
        yield return ShowText(30);
        yield return ShowText(31);
    }
    private IEnumerator ThirdTurnActions()
    {
        yield return ShowText(32);
    }

    private IEnumerator SpellPresentation()
    {
        yield return ShowText(34);
    }

    private IEnumerator SpellExplanation()
    {
        yield return ShowText(35);
        yield return ShowText(37);
    }

    private IEnumerator WeaponPresentation()
    {
        yield return ShowText(38);
    }

    private IEnumerator WeaponExplanation()
    {
        yield return ShowText(39);
        yield return ShowText(40);
        yield return ShowText(41);
        yield return new WaitUntil(() => player1Manager.GetAllCardInField()[0].GetEquipmentCounts() == 1);
        StartCoroutine(AttirePresentation());
    }

    private IEnumerator AttirePresentation()
    {
        yield return ShowText(42);
    }

    private IEnumerator AttireExplanation()
    {
        yield return ShowText(43);
        yield return ShowText(44);
        yield return ShowText(45);
        yield return new WaitUntil(() => player1Manager.GetAllCardInField()[0].GetEquipmentCounts() == 2);
        StartCoroutine(AccessoryPresentation());
    }

    private IEnumerator AccessoryPresentation()
    {
        yield return ShowText(46);
    }

    private IEnumerator AccessoryExplanation()
    {
        yield return ShowText(47);
        yield return ShowText(48);
        yield return ShowText(49);
        yield return new WaitUntil(() => player1Manager.GetAllCardInField()[0].GetEquipmentCounts() == 3);
        StartCoroutine(EquippableCardExplanation());
    }

    private IEnumerator EquippableCardExplanation()
    {
        yield return ShowText(50);
        yield return ShowText(51);
    }

    public void OnPlaceCard(Card card)
    {
        if(firstHero && card.cardSO is HeroCardSO)
        {
            StartCoroutine(NextPhaseExplanation());
        }
    }

    private IEnumerator ShowText(int index)
    {
        isPauseGame = true;
        sampleCard.enabled = false;
        explanationTextsBoxs[index].SetActive(true);
        yield return ToggleText(index, Vector3.one);
        yield return new WaitWhile(() => !onClick);
        yield return ToggleText(index, Vector3.zero);
        explanationTextsBoxs[index].SetActive(false);
        onClick = false;
        sampleCard.enabled = true;
        isPauseGame = false;
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

    public void OnEnlargueCard(Card card)
    {
        if (card.cardSO is HeroCardSO)
        {
            if(!knowsHeroCards)
                StartCoroutine(ShowHeroCardExplanation(card));
            else if(firstAttack && (player1Manager.GetAllCardInField().Count > 0 && heroInTurn.Contains(player1Manager.GetAllCardInField()[0])))
            {

                StartCoroutine(UseMovementExplanation());
            }
            else if(turnCount == 4 && player1Manager.GetAllCardInField().Contains(card) && heroInTurn.Contains(card))
            {
                if(card.cardSO.CardID == 1002)
                {
                    StartCoroutine(ShowText(52));
                }
                else if (card.cardSO.CardID == 1051)
                {
                    StartCoroutine(ShowText(53));
                }
                else if (card.cardSO.CardID == 1025)
                {
                    StartCoroutine(ShowText(54));
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
        else if(card.cardSO is SpellCardSO && !knowsSpellCards)
        {
            knowsSpellCards = true;
            StartCoroutine(SpellExplanation());
        }
    }

    public void OnResizeCard()
    {
        if(!knowsHeroCards)
        {
            knowsHeroCards = true;
            StartCoroutine(PlayFirstTurnExplanation());
        }
    }

}
