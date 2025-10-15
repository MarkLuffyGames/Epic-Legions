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

    private bool firstTurn = true;
    public bool explanationFinished = false;
    private bool firstHero = true;
    private bool firstAttack = true;
    private bool knowsHeroCards = false;
    private bool knowsEquipomentCards = false;
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

            if (oldPhase == DuelPhase.DrawingCards)
            {
                player1Manager.GetHandCardHandler().ShowHandCard();
            }

            if (firstTurn)
            {
                player1Manager.HideNextPhaseButton();
                firstTurn = false;
                yield return PhaseExplanation();
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
        yield return ShowExplanation(0);
        yield return ShowExplanation(1);
        yield return ShowExplanation(2);
        yield return ShowExplanation(3);
        yield return ShowExplanation(4);
    }

    IEnumerator PhaseExplanation()
    {
        yield return ShowExplanation(27);
        yield return ShowExplanation(25);

        explanationFinished = true;
        yield return HandExplanation();
    }

    IEnumerator HandExplanation()
    {
        yield return ShowExplanation(26);
        yield return ShowExplanation(5);
        yield return ShowExplanation(6);
    }

    private IEnumerator ShowHeroCardExplanation(Card card)
    {
        sampleCard.enabled = false;
        yield return new WaitForSeconds(0.1f);
        player1Manager.GetHandCardHandler().HideHandCards();
        yield return ShowExplanation(7);
        yield return ShowExplanation(8);
        yield return ShowExplanation(9);
        yield return ShowExplanation(10);
        yield return ShowExplanation(11);
        yield return ShowExplanation(12);
        yield return ShowExplanation(13);
        yield return ShowExplanation(14);
    }

    private IEnumerator PlayFirstTurnExplanation()
    {
        yield return ShowExplanation(15);
        yield return ShowExplanation(16);
    }

    private IEnumerator NextPhaseExplanation()
    {
        yield return ShowExplanation(17);
        player1Manager.ShowNextPhaseButton();
    }

    private IEnumerator ElementExplanation()
    {
        yield return ShowExplanation(18);
        yield return ShowExplanation(19);
        yield return YourTurnExplanation();
    }

    private IEnumerator YourTurnExplanation()
    {
        yield return ShowExplanation(20);
        yield return ShowExplanation(21);
    }
    private IEnumerator UseMovementExplanation()
    {
        firstAttack = false;
        yield return ShowExplanation(22);
        yield return ShowExplanation(23);
    }
    public void OnPlaceCard(Card card)
    {
        if(firstHero && card.cardSO is HeroCardSO)
        {
            StartCoroutine(NextPhaseExplanation());
        }
    }

    private IEnumerator ShowExplanation(int index)
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
                StartCoroutine(UseMovementExplanation());
        }
        else if(card.cardSO is EquipmentCardSO && !knowsEquipomentCards)
        {
            knowsEquipomentCards = true;
            StartCoroutine(ShowExplanation(6));
        }
        else if(card.cardSO is SpellCardSO && !knowsSpellCards)
        {
            knowsSpellCards = true;
            StartCoroutine(ShowExplanation(7));
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
