using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;

public enum DuelPhase { PreparingDuel, Starting, DrawingCards, Preparation, PlayingSpellCard, Battle, EndDuel, None }
public class DuelManager : NetworkBehaviour
{
    public static int NumberOfTurns = 21;

    public event EventHandler OnPlayerNotReady;
    public event EventHandler<OnPlayerReadyEventArgs> OnPlayerReady;
    public class OnPlayerReadyEventArgs : EventArgs
    {
        public ulong clientIdReady;
    }

    public event EventHandler OnChangeTurn;

    [SerializeField] private PlayerManager player1Manager;
    [SerializeField] private PlayerManager player2Manager;
    [SerializeField] private EndDuelUI endDuelUI;
    [SerializeField] private int energyGainedPerTurn = 50;

    [SerializeField] private List<int> deckCardIds;

    private Dictionary<ulong, int> playerRoles = new Dictionary<ulong, int>();
    private Dictionary<int, ulong> playerId = new Dictionary<int, ulong>();

    private Dictionary<ulong, List<int>> playerDecks = new Dictionary<ulong, List<int>>();
    private Dictionary<ulong, bool> playerReady = new Dictionary<ulong, bool>();


    public NetworkVariable<DuelPhase> duelPhase = new NetworkVariable<DuelPhase>(DuelPhase.None);

    private List<Card> HeroCardsOnTheField = new List<Card>();
    private List<Card>[] turns = new List<Card>[NumberOfTurns];
    private int heroesInTurnIndex;

    [SerializeField] private TextMeshProUGUI duelPhaseText;
    private List<Card> heroInTurn = new List<Card>();
    private int movementToUse;
    private bool settingAttackTarget;
    private Card cardSelectingTarget;

    private DuelPhase oldDuelPhase;

    public PlayerManager Player1Manager => player1Manager;
    public PlayerManager Player2Manager => player2Manager;
    public List<Card> HeroInTurn => heroInTurn;
    public int MovementToUse => movementToUse;
    public bool SettingAttackTarget => settingAttackTarget;
    public Card CardSelectingTarget => cardSelectingTarget;

    private bool isSinglePlayer;
    public bool IsSinglePlayer => isSinglePlayer;

    private void Awake()
    {
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
            SetDecks();
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

                InitializeBattleTurns();

                BeginHeroTurn();

                if (IsClient || isSinglePlayer)
                {
                    player1Manager.GetHandCardHandler().HideHandCard();
                }
            }
            else
            {
                InitializeHeroTurn();
            }
        }
        else if(newPhase == DuelPhase.DrawingCards)
        {
            player1Manager.DrawCard();
            player2Manager.DrawCard();
            player1Manager.RechargeEnergy(energyGainedPerTurn);
            player2Manager.RechargeEnergy(energyGainedPerTurn);
        }
        else if(newPhase == DuelPhase.PlayingSpellCard)
        {
            oldDuelPhase = oldPhase;
            player1Manager.GetHandCardHandler().HideHandCard();
        }
    }

    /// <summary>
    /// Registra a dos jugadores en la partida, asign�ndoles roles, identificadores y marc�ndolos como no listos.
    /// Luego, inicia el duelo.
    /// </summary>
    /// <param name="clientId1">ID del primer jugador.</param>
    /// <param name="clientId2">ID del segundo jugador.</param>
    public void AssignPlayersAndStartDuel(ulong clientId1, ulong clientId2)
    {
        isSinglePlayer = false;

        // Asigna roles a los jugadores
        playerRoles[clientId1] = 1;
        playerRoles[clientId2] = 2;

        // Asigna los identificadores de los jugadores en la partida
        playerId[1] = clientId1;
        playerId[2] = clientId2;

        // Marca a los jugadores como no listos al inicio
        playerReady[clientId1] = false;
        playerReady[clientId2] = false;

        // Inicia el duelo despu�s del registro
        InitializeDuel();
    }

    public void AssignPlayersAndStartDuel(int[] player1deckCardIds, int[] player2deckCardIds)
    {
        isSinglePlayer = true;

        // Asigna roles a los jugadores
        playerRoles[1] = 1;
        playerRoles[2] = 2;

        // Marca a los jugadores como no listos al inicio
        playerReady[0] = false;
        playerReady[1] = false;

        playerDecks[0] = CardDatabase.ShuffleArray(player1deckCardIds.ToArray()).ToList(); // Barajar el mazo y guardar en la lista de mazos
        playerDecks[1] = CardDatabase.ShuffleArray(player2deckCardIds.ToArray()).ToList(); // Barajar el mazo y guardar en la lista de mazos

        // Inicia el duelo despu�s del registro
        InitializeDuel();
    }

    private void SetDecks()
    {
        if (isSinglePlayer)
        {
            ValidateDeck(1, playerDecks[0]);
            ValidateDeck(2, playerDecks[1]);
        }
        else
        {
            ReceiveAndStoreDeckServerRpc(NetworkManager.Singleton.LocalClientId, deckCardIds.ToArray());
        }
    }


    /// <summary>
    /// Recibe y almacena el mazo de un jugador en el servidor, lo baraja y lo distribuye a los clientes.
    /// </summary>
    /// <param name="clientId">ID del jugador que env�a el mazo.</param>
    /// <param name="deckCardIds">Lista de IDs de cartas en el mazo del jugador.</param>
    [ServerRpc(RequireOwnership = false)]
    private void ReceiveAndStoreDeckServerRpc(ulong clientId, int[] deckCardIds)
    {
        // Validar si el mazo es v�lido
        if (deckCardIds == null || deckCardIds.Length == 0)
        {
            Debug.LogWarning($"Player {clientId} sent an invalid deck.");
            return;
        }

        // Guardar el mazo si a�n no ha sido registrado
        if (!playerDecks.ContainsKey(clientId))
        {
            deckCardIds = CardDatabase.ShuffleArray(deckCardIds); // Barajar el mazo

            playerDecks[clientId] = deckCardIds.ToList(); // Guardar en la lista de mazos

            DistributeDeckToClientsClientRpc(clientId, deckCardIds); // Enviar el mazo a los clientes

            AssignDeckToRole(playerRoles[clientId], deckCardIds); // Configurar el mazo en el juego
        }
    }

    private void ValidateDeck(int player, List<int> deckCardIds)
    {
        // Validar si el mazo es v�lido
        if (deckCardIds == null || deckCardIds.Count == 0)
        {
            Debug.LogWarning($"Player {player} sent an invalid deck.");
            return;
        }

        AssignDeckToRole(player1Manager, playerDecks[0]);
        AssignDeckToRole(player2Manager, playerDecks[1]);
    }


    /// <summary>
    /// Env�a el mazo de un jugador a los clientes. Si la fase de duelo no es 'Preparaci�n',
    /// espera hasta que la fase sea la correcta antes de procesar el mazo.
    /// </summary>
    /// <param name="targetClientId">ID del jugador que recibe el mazo.</param>
    /// <param name="deckCardIds">Lista de IDs de cartas en el mazo.</param>
    [ClientRpc]
    private void DistributeDeckToClientsClientRpc(ulong targetClientId, int[] deckCardIds)
    {
        // Verifica si la fase de duelo es la correcta para recibir el mazo
        if (duelPhase.Value != DuelPhase.PreparingDuel)
        {
            Debug.Log("Deck received, but phase is not 'Preparing'. Waiting for phase update...");
            StartCoroutine(WaitForPreparationPhaseAndAssignDeck(targetClientId, deckCardIds));
            return;
        }

        // Procesa el mazo inmediatamente si la fase es la correcta
        AssignDeckToPlayer(targetClientId, deckCardIds);
    }


    /// <summary>
    /// Asigna el mazo de un jugador seg�n su ID, diferenciando si es el cliente local o el oponente.
    /// </summary>
    /// <param name="targetClientId">ID del jugador al que se le asignar� el mazo.</param>
    /// <param name="deckCardIds">Lista de IDs de cartas en el mazo.</param>
    private void AssignDeckToPlayer(ulong targetClientId, int[] deckCardIds)
    {
        // Verifica si el jugador es el cliente local o el oponente y asigna el mazo en consecuencia
        if (NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            AssignDeckToRole(1, deckCardIds); // Asigna el mazo al jugador local
        }
        else
        {
            AssignDeckToRole(2, deckCardIds); // Asigna el mazo al oponente
        }
    }


    /// <summary>
    /// Espera hasta que la fase de duelo sea 'Preparaci�n' y luego asigna el mazo al jugador correspondiente.
    /// </summary>
    /// <param name="targetClientId">ID del jugador que recibir� el mazo.</param>
    /// <param name="deckCardIds">Lista de IDs de cartas en el mazo.</param>
    /// <returns>Corrutina que espera hasta que la fase de duelo sea la correcta.</returns>
    private IEnumerator WaitForPreparationPhaseAndAssignDeck(ulong targetClientId, int[] deckCardIds)
    {
        // Espera hasta que la fase de duelo sea 'Preparaci�n'
        yield return new WaitUntil(() => duelPhase.Value == DuelPhase.PreparingDuel);

        // Asigna el mazo al jugador una vez que la fase sea la correcta
        AssignDeckToPlayer(targetClientId, deckCardIds);
    }


    /// <summary>
    /// Asigna un mazo a un jugador basado en su rol, validando cada carta antes de a�adirla.
    /// </summary>
    /// <param name="playerRole">Rol del jugador (1 para el jugador 1, 2 para el jugador 2).</param>
    /// <param name="deckCardIds">Lista de IDs de cartas que componen el mazo.</param>
    private void AssignDeckToRole(int playerRole, int[] deckCardIds)
    {
        foreach (var cardId in deckCardIds)
        {
            var card = CardDatabase.GetCardById(cardId);

            if (card != null)
            {
                // Asigna las cartas al mazo del jugador correspondiente
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

    private void AssignDeckToRole(PlayerManager playerManager, List<int> deckCardIds)
    {
        foreach (var cardId in deckCardIds)
        {
            var card = CardDatabase.GetCardById(cardId);

            if (card != null)
            {
                // Asigna las cartas al mazo del jugador correspondiente
                playerManager.AddCardToPlayerDeck(card, playerDecks[0].Count);
            }
            else
            {
                Debug.LogWarning($"Player {(playerManager == player1Manager ? 1 : 2)} tried to add an invalid card ID {cardId} to their deck.");
            }
        }
    }
    /// <summary>
    /// Establece el orden de turnos en la batalla basado en la velocidad de los h�roes en el campo.
    /// </summary>
    private void InitializeBattleTurns()
    {
        // Limpia las listas de turnos previos
        foreach (var list in turns)
        {
            list.Clear();
        }

        // Verifica si hay h�roes en el campo antes de calcular los turnos
        if (HeroCardsOnTheField.Count > 0)
        {
            foreach (var card in HeroCardsOnTheField)
            {
                // Calcula el turno basado en la velocidad del h�roe
                int turn = (100 - card.CurrentSpeedPoints) / 5;
                turns[turn].Add(card);
            }
        }
    }

    /// <summary>
    /// Inicia el turno de los h�roes en la fase actual, gestionando efectos y verificando estados.
    /// </summary>
    private void BeginHeroTurn()
    {
        // Actualiza el texto de la fase del duelo
        UpdateDuelPhaseText();

        // Gestiona los efectos activos antes de iniciar el turno
        ManageEffects();

        // Si no hay h�roes en el turno actual, avanza al siguiente turno
        if (turns[heroesInTurnIndex].Count == 0)
        {
            if (IsServer || IsSinglePlayer) NextTurn();
            return;
        }

        // Reinicia la lista de h�roes que participar�n en este turno
        heroInTurn.Clear();

        foreach (var card in turns[heroesInTurnIndex])
        {
            if (card.IsStunned())
            {
                card.PassTurn(); // El h�roe pierde el turno si est� aturdido
            }
            else if (card.FieldPosition != null)
            {
                heroInTurn.Add(card); // Agrega el h�roe a la lista si tiene posici�n en el campo
            }
        }

        // Si no hay h�roes disponibles despu�s de la validaci�n, finaliza las acciones del turno
        if (heroInTurn.Count == 0)
        {
            StartCoroutine(FinishActions());
        }
        else
        {
            InitializeHeroTurn(); // Configura el turno del h�roe si hay al menos uno disponible
        }
    }


    /// <summary>
    /// Configura el turno de los h�roes que est�n listos para actuar en el campo.
    /// </summary>
    private void InitializeHeroTurn()
    {

        OnChangeTurn?.Invoke(this, EventArgs.Empty);
        // Recorre los h�roes en turno y configura su estado
        foreach (var hero in heroInTurn)
        {
            // Verifica si el h�roe est� en el campo del jugador 1
            if (player1Manager.GetFieldPositionList().Contains(hero.FieldPosition))
            {
                // Establece si el h�roe est� listo para actuar o no
                hero.SetTurn(!hero.actionIsReady);
            }
            else
            {
                // Si el h�roe no est� en el campo del jugador 1, se desactiva su turno
                hero.EndTurn();
            }
        }
    }


    /// <summary>
    /// Establece el estado de preparaci�n de un jugador en el servidor y maneja la transici�n entre fases de duelo.
    /// </summary>
    /// <param name="clientId">ID del cliente que ha indicado que est� listo.</param>
    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerReadyAndTransitionPhaseServerRpc(ulong clientId)
    {
        // Si la fase actual es Preparaci�n, se notifica al cliente que est� listo
        if (duelPhase.Value == DuelPhase.Preparation) NotifyPlayerReadyClientRpc(clientId);

        // Marca al jugador como listo
        playerReady[clientId] = true;

        // Si todos los jugadores est�n listos, reinicia el estado de los jugadores y cambia de fase
        if (AreAllTrue(playerReady))
        {
            // Reinicia el estado de todos los jugadores a "no listos"
            playerReady = playerReady.ToDictionary(kvp => kvp.Key, kvp => false);

            // Notifica a los jugadores que ya no est�n listos
            NotifyPlayerNotReadyClientRpc();

            ChangePhase();
        }
    }

    public void SetPlayerReadyAndTransitionPhase()
    {
        if(player1Manager.isReady && player2Manager.isReady)
        {
            ChangePhase();
        }
    }

    private void ChangePhase()
    {
        // Transiciones entre las fases del duelo seg�n la fase actual
        if (duelPhase.Value == DuelPhase.PreparingDuel)
        {
            duelPhase.Value = DuelPhase.Starting;
        }
        else if (duelPhase.Value == DuelPhase.Starting)
        {
            duelPhase.Value = DuelPhase.Preparation;
        }
        else if (duelPhase.Value == DuelPhase.Preparation)
        {
            duelPhase.Value = DuelPhase.Battle;
        }
        else if (duelPhase.Value == DuelPhase.DrawingCards)
        {
            duelPhase.Value = DuelPhase.Preparation;
        }
        else if (duelPhase.Value == DuelPhase.Battle)
        {
            NextTurn(); // Avanza al siguiente turno
        }
        else if (duelPhase.Value == DuelPhase.PlayingSpellCard)
        {
            duelPhase.Value = oldDuelPhase; // Regresa a la fase anterior
        }
    }

    /// <summary>
    /// Verifica si todos los valores de un diccionario de estados son verdaderos.
    /// </summary>
    /// <param name="clientStatus">Diccionario que contiene el estado de los clientes, donde la clave es el ID del cliente y el valor es su estado (true o false).</param>
    /// <returns>Devuelve true si todos los valores en el diccionario son true, de lo contrario, false.</returns>
    public bool AreAllTrue(Dictionary<ulong, bool> clientStatus)
    {
        // Verificar si todos los valores son true
        return clientStatus.Values.All(status => status);
    }

    /// <summary>
    /// Notifica al cliente que un jugador ha marcado su estado como listo.
    /// </summary>
    /// <param name="clientId">ID del cliente que ha marcado su estado como listo.</param>
    [ClientRpc]
    public void NotifyPlayerReadyClientRpc(ulong clientId)
    {
        // Llama al evento para notificar que el jugador est� listo
        OnPlayerReady?.Invoke(this, new OnPlayerReadyEventArgs
        {
            clientIdReady = clientId
        });
    }


    /// <summary>
    /// Notifica a los clientes que un jugador ha cambiado su estado a "no listo".
    /// </summary>
    [ClientRpc]
    public void NotifyPlayerNotReadyClientRpc()
    {
        // Llama al evento para notificar que el jugador ya no est� listo
        OnPlayerNotReady?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Inicia el duelo configurando la fase inicial como "Preparaci�n del Duelo".
    /// </summary>
    private void InitializeDuel()
    {
        // Establece la fase del duelo a "Preparando el Duelo"
        duelPhase.Value = DuelPhase.PreparingDuel;
    }

    /// <summary>
    /// Actualiza el texto que muestra la fase actual del duelo.
    /// </summary>
    private void UpdateDuelPhaseText()
    {
        duelPhaseText.text = $"{duelPhase.Value.ToString()}{(duelPhase.Value == DuelPhase.Battle ? $" {heroesInTurnIndex + 1}" : "")}";
    }

    /// <summary>
    /// Obtiene el estado actual de la fase del duelo.
    /// </summary>
    /// <returns>El valor de la fase actual del duelo.</returns>
    public DuelPhase GetCurrentDuelPhase()
    {
        // Retorna el valor actual de la fase del duelo
        return duelPhase.Value;
    }


    /////////// Player Actions ////////////////////////////////

    /// <summary>
    /// Coloca una carta en el campo para el jugador correspondiente en el servidor.
    /// </summary>
    /// <param name="cardIndex">�ndice de la carta que se va a colocar en el campo.</param>
    /// <param name="fieldPositionIdex">�ndice de la posici�n en el campo donde se colocar� la carta.</param>
    /// <param name="clientId">ID del cliente que est� realizando la acci�n.</param>
    [ServerRpc(RequireOwnership = false)]
    public virtual void PlaceCardOnFieldServerRpc(int cardIndex, int fieldPositionIdex, ulong clientId)
    {
        // Verifica el rol del jugador y coloca la carta en el campo del jugador correspondiente
        if (playerRoles[clientId] == 1)
        {
            PlaceCardInField(player1Manager, true, cardIndex, fieldPositionIdex);
        }
        else if (playerRoles[clientId] == 2)
        {
            PlaceCardInField(player2Manager, false, cardIndex, fieldPositionIdex);
        }

        // Notifica a los clientes para que actualicen la posici�n de la carta en el campo
        PlaceCardOnTheFieldClientRpc(cardIndex, fieldPositionIdex, clientId);
    }


    /// <summary>
    /// Coloca una carta en el campo de batalla o en el �rea de hechizos, y actualiza el estado del jugador.
    /// </summary>
    /// <param name="playerManager">El administrador del jugador que est� colocando la carta.</param>
    /// <param name="isPlayer">Indica si la carta es para el jugador o el oponente.</param>
    /// <param name="cardIndex">�ndice de la carta en la mano del jugador.</param>
    /// <param name="fieldPositionIdex">�ndice de la posici�n en el campo donde se colocar� la carta. Si es -1, la carta va al �rea de hechizos.</param>
    public void PlaceCardInField(PlayerManager playerManager, bool isPlayer, int cardIndex, int fieldPositionIdex)
    {
        // Obtiene la carta de la mano del jugador
        Card card = playerManager.GetHandCardHandler().GetCardInHandList()[cardIndex];

        // Elimina la carta de la mano del jugador
        playerManager.GetHandCardHandler().QuitCard(card);

        // Si la carta se coloca en el �rea de hechizos
        if (fieldPositionIdex == -1)
        {
            playerManager.SpellFieldPosition.SetCard(card, isPlayer);
            if (IsSinglePlayer)
            {
                duelPhase.Value = DuelPhase.PlayingSpellCard;
            }
            else
            {
                if (IsServer || IsSinglePlayer) duelPhase.Value = DuelPhase.PlayingSpellCard;
            }
            
            if (isPlayer || IsSinglePlayer) UseMovement(0, card);
        }
        else
        {
            // Si la carta se coloca en una posici�n en el campo
            playerManager.GetFieldPositionList()[fieldPositionIdex].SetCard(card, isPlayer);
            InsertCardInOrder(HeroCardsOnTheField, card);
        }

        // Marca la carta como lista para el servidor
        card.waitForServer = false;

        // Consume la energ�a correspondiente para usar la carta
        playerManager.ConsumeEnergy(card.cardSO is HeroCardSO hero ? hero.Energy : 0);
    }


    /// <summary>
    /// Inserta una carta en la lista de cartas de h�roes en orden descendente de puntos de velocidad.
    /// </summary>
    /// <param name="heroCards">La lista de cartas de h�roes donde se va a insertar la nueva carta.</param>
    /// <param name="newCard">La nueva carta que se va a insertar en la lista.</param>
    private void InsertCardInOrder(List<Card> heroCards, Card newCard)
    {
        // Encuentra la posici�n donde insertar la nueva carta, comparando con la velocidad de las cartas existentes
        int insertIndex = heroCards.FindLastIndex(card => card.CurrentSpeedPoints >= newCard.CurrentSpeedPoints);

        // Si no encontr� ninguna carta con igual o mayor velocidad, la coloca al inicio
        if (insertIndex == -1)
        {
            heroCards.Insert(0, newCard);
        }
        else
        {
            // Inserta la carta despu�s de la �ltima carta con la misma velocidad o mayor
         heroCards.Insert(insertIndex + 1, newCard);
        }
    }


    /// <summary>
    /// Coloca una carta en el campo del cliente correspondiente, seg�n el �ndice de la carta y la posici�n en el campo.
    /// </summary>
    /// <param name="cardIndex">�ndice de la carta en la mano del jugador.</param>
    /// <param name="fieldPositionIdex">�ndice de la posici�n en el campo donde se colocar� la carta.</param>
    /// <param name="clientId">ID del cliente que solicita la acci�n de colocar la carta.</param>
    [ClientRpc]
    private void PlaceCardOnTheFieldClientRpc(int cardIndex, int fieldPositionIdex, ulong clientId)
    {
        // Verifica si el servidor est� ejecutando esta funci�n, en cuyo caso no realiza ninguna acci�n.
        if (NetworkManager.Singleton.IsHost) return;

        // Si el cliente actual es el que ha realizado la acci�n, coloca la carta en el campo del jugador 1
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            PlaceCardInField(player1Manager, true, cardIndex, fieldPositionIdex);
        }
        else
        {
            // Si no, coloca la carta en el campo del jugador 2
            PlaceCardInField(player2Manager, false, cardIndex, fieldPositionIdex);
        }
    }


    /// <summary>
    /// Obtiene el ID del cliente al que pertenece el h�roe dado, bas�ndose en la posici�n del h�roe en el campo.
    /// </summary>
    /// <param name="heroCard">La carta de h�roe cuyo ID de cliente se desea obtener.</param>
    /// <returns>El ID del cliente al que pertenece el h�roe, o 0 si no se encuentra el h�roe en ning�n jugador.</returns>
    private ulong GetClientIdForHero(Card heroCard)
    {
        // Verifica si el h�roe est� en el campo del jugador 1
        if (player1Manager.GetFieldPositionList().Contains(heroCard.FieldPosition))
        {
            return playerId[1];
        }
        // Verifica si el h�roe est� en el campo del jugador 2
        else if (player2Manager.GetFieldPositionList().Contains(heroCard.FieldPosition))
        {
            return playerId[2];
        }

        // Si el h�roe no se encuentra en el campo de ning�n jugador, se muestra un error y se devuelve 0
        Debug.LogError("El h�roe no pertenece a ning�n jugador");
        return 0;
    }

    private PlayerManager GetPlayerManagerForHero(Card heroCard)
    {
        // Verifica si el h�roe est� en el campo del jugador 1
        if (player1Manager.GetFieldPositionList().Contains(heroCard.FieldPosition))
        {
            return player1Manager;
        }
        // Verifica si el h�roe est� en el campo del jugador 2
        else if (player2Manager.GetFieldPositionList().Contains(heroCard.FieldPosition))
        {
            return player2Manager;
        }

        // Si el h�roe no se encuentra en el campo de ning�n jugador, se muestra un error y se devuelve 0
        Debug.LogError("El h�roe no pertenece a ning�n jugador");
        return null;
    }

    private PlayerManager GetPlayerManagerRival(Card heroCard)
    {
        // Verifica si el h�roe est� en el campo del jugador 1
        if (player1Manager.GetFieldPositionList().Contains(heroCard.FieldPosition))
        {
            return player2Manager;
        }
        // Verifica si el h�roe est� en el campo del jugador 2
        else if (player2Manager.GetFieldPositionList().Contains(heroCard.FieldPosition))
        {
            return player1Manager;
        }

        // Si el h�roe no se encuentra en el campo de ning�n jugador, se muestra un error y se devuelve 0
        Debug.LogError("El h�roe no pertenece a ning�n jugador");
        return null;
    }

    /// <summary>
    /// Gestiona los efectos de los h�roes en el campo y actualiza su texto de estado.
    /// </summary>
    /// <remarks>
    /// Esta funci�n recorre todos los h�roes en el campo, aplica los efectos correspondientes y actualiza los textos asociados a sus estados.
    /// </remarks>
    private void ManageEffects()
    {
        // Itera sobre todos los h�roes en el campo
        foreach (var hero in HeroCardsOnTheField)
        {
            // Aplica los efectos del h�roe
            hero.ManageEffects();

            // Actualiza el texto que muestra el estado del h�roe
            hero.UpdateText();
        }
    }

    /// <summary>
    /// Activa los efectos de todos los h�roes presentes en el campo.
    /// </summary>
    /// <remarks>
    /// Esta funci�n recorre todos los h�roes en el campo y activa sus efectos correspondientes, si los tienen.
    /// </remarks>
    private void ActiveEffect()
    {
        // Itera sobre todos los h�roes en el campo y activa sus efectos
        foreach (var hero in HeroCardsOnTheField)
        {
            hero.ActivateEffect();
        }
    }

    /// <summary>
    /// Utiliza un movimiento espec�fico de un h�roe y gestiona su ejecuci�n, incluyendo la selecci�n de objetivo si es necesario.
    /// </summary>
    /// <param name="movementToUseIndex">El �ndice del movimiento a utilizar.</param>
    /// <param name="card">La carta que representa al h�roe que realiza el movimiento.</param>
    /// <remarks>
    /// Si el movimiento seleccionado necesita un objetivo, se invoca la funci�n para seleccionar uno. 
    /// Si no es necesario un objetivo, el movimiento se ejecuta inmediatamente y se marca el h�roe como listo para finalizar su turno.
    /// </remarks>
    public void UseMovement(int movementToUseIndex, Card card, int target = -1)
    {
        // Verifica si el movimiento necesita un objetivo
        if (card.Moves[movementToUseIndex].MoveSO.NeedTarget)
        {
            if(target == -1)
            {
                // Si necesita un objetivo, se selecciona uno
                SelectTarget(movementToUseIndex, card);
            }
            else
            {
                var playerManager = GetPlayerManagerForHero(card);
                ProcessAttack(target, card.cardSO is HeroCardSO, card.FieldPosition.PositionIndex, movementToUseIndex, playerManager == player1Manager ? 1u : 2u);
            }
        }
        else
        {
            // Si no necesita objetivo, se ejecuta el ataque
            if (isSinglePlayer)
            {
                var playerManager = GetPlayerManagerForHero(card);
                ProcessAttack(card.FieldPosition.PositionIndex, card.cardSO is HeroCardSO, card.FieldPosition.PositionIndex, movementToUseIndex, playerManager == player1Manager ? 1u : 2u);
            }
            else
            {
                ProcessAttackServerRpc(card.FieldPosition.PositionIndex, NetworkManager.Singleton.LocalClientId, card.cardSO is HeroCardSO, card.FieldPosition.PositionIndex, movementToUseIndex);
            }

            // Marca la acci�n como lista y finaliza el turno del h�roe
            card.actionIsReady = true;
            card.EndTurn();
        }
    }

    /// <summary>
    /// Inicia el proceso de selecci�n de un objetivo para un movimiento espec�fico de un h�roe.
    /// Si el movimiento no requiere un objetivo o no se encuentran enemigos en el campo rival, el ataque se dirige a los puntos de vida del oponente.
    /// </summary>
    /// <param name="movementToUseIndex">El �ndice del movimiento que se va a utilizar.</param>
    /// <param name="attackingCard">La carta que representa al h�roe que est� realizando el ataque.</param>
    /// <remarks>
    /// Si el movimiento requiere un objetivo, se obtienen los posibles objetivos y se destacan visualmente en el campo.
    /// Si no hay enemigos en el campo rival o el movimiento no requiere objetivo, el ataque se realiza directamente a los puntos de vida del oponente.
    /// </remarks>
    public void SelectTarget(int movementToUseIndex, Card attackingCard)
    {
        movementToUse = movementToUseIndex;
        settingAttackTarget = true;
        cardSelectingTarget = attackingCard;

        // Obtiene los posibles objetivos para el movimiento
        var targets = ObtainTargets(attackingCard, movementToUseIndex);

        // Si hay enemigos en el campo rival, los marca como seleccionables
        if (targets.Count > 0)
        {
            foreach (Card card in targets)
            {
                // Marca los objetivos de acuerdo con el jugador (verde para el jugador 1, rojo para el jugador 2)
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
            // Si no hay enemigos en el campo rival, realiza el ataque directamente a los puntos de vida del oponente
            cardSelectingTarget.actionIsReady = true;

            // Realiza el ataque directo a los puntos de vida del oponente (sin objetivo espec�fico)
            if (isSinglePlayer)
            {
                var playerManager = GetPlayerManagerForHero(attackingCard);
                ProcessAttack(-1, true, attackingCard.FieldPosition.PositionIndex, movementToUseIndex, playerManager == player1Manager ? 1u : 2u);
            }
            else
            {
                ProcessAttackServerRpc(-1, NetworkManager.Singleton.LocalClientId, true, attackingCard.FieldPosition.PositionIndex, movementToUseIndex);
            }

            // Finaliza el turno del h�roe
            cardSelectingTarget.EndTurn();

            // Desactiva los objetivos seleccionables
            DisableAttackableTargets();
        }
    }


    /// <summary>
    /// Desactiva los objetivos que pueden ser seleccionados para un ataque.
    /// Este m�todo se usa para limpiar cualquier selecci�n de objetivos despu�s de un ataque o cuando no hay m�s objetivos disponibles.
    /// </summary>
    public void DisableAttackableTargets()
    {
        // Desactiva los objetivos seleccionables para el jugador 2
        foreach (var fieldPosition in player2Manager.GetFieldPositionList())
        {
            if (fieldPosition.Card != null)
            {
                fieldPosition.Card.DesactiveSelectableTargets();
            }
        }

        // Desactiva los objetivos seleccionables para el jugador 1
        foreach (var fieldPosition in player1Manager.GetFieldPositionList())
        {
            if (fieldPosition.Card != null)
            {
                fieldPosition.Card.DesactiveSelectableTargets();
            }
        }

        // Resetea las variables de selecci�n
        settingAttackTarget = false;
        cardSelectingTarget = null;
    }


    /// <summary>
    /// Obtiene los objetivos posibles para un movimiento espec�fico de una carta.
    /// Dependiendo del tipo de movimiento (ataque cuerpo a cuerpo, ataque a distancia, efecto positivo, etc.), 
    /// se determinar�n las cartas enemigas (o aliadas en el caso de efectos positivos) que pueden ser objetivo.
    /// </summary>
    /// <param name="card">La carta que est� realizando el movimiento.</param>
    /// <param name="movementToUseIndex">El �ndice del movimiento que se va a utilizar.</param>
    /// <returns>Una lista de cartas que pueden ser objetivo del movimiento.</returns>
    public List<Card> ObtainTargets(Card card, int movementToUseIndex)
    {
        // Lista que contendr� los objetivos posibles
        List<Card> targets = new List<Card>();

        // Si el movimiento no es un efecto positivo, buscamos enemigos en el campo del jugador contrario
        if (card.Moves[movementToUseIndex].MoveSO.MoveType != MoveType.PositiveEffect)
        {
            // Itera sobre las posiciones de campo del jugador 2 (enemigo)
            for (int i = 0; i < GetPlayerManagerRival(card).GetFieldPositionList().Count; i++)
            {
                // Si hay una carta en la posici�n, la agregamos como objetivo
                if (GetPlayerManagerRival(card).GetFieldPositionList()[i].Card != null)
                {
                    targets.Add(GetPlayerManagerRival(card).GetFieldPositionList()[i].Card);
                }

                // Si ya encontramos al menos un objetivo y estamos en las �ltimas posiciones (en este caso, filas 4, 9 o 14),
                // retornamos la lista de objetivos solo si el movimiento es un ataque cuerpo a cuerpo (melee)
                if (targets.Count > 0 && (i == 4 || i == 9 || i == 14) && card.Moves[movementToUseIndex].MoveSO.MoveType == MoveType.MeleeAttack)
                {
                    return targets; // Terminamos la b�squeda si encontramos un objetivo v�lido para un ataque cuerpo a cuerpo
                }
            }
        }
        else
        {
            // Si el movimiento es un efecto positivo, los objetivos ser�n las cartas del jugador aliado (jugador 1),
            // pero no se puede seleccionar la misma carta que est� atacando
            foreach (var position in GetPlayerManagerForHero(card).GetFieldPositionList())
            {
                if (position.Card != null && position.Card != card)
                {
                    targets.Add(position.Card); // A�adimos los aliados como objetivo
                }
            }
        }

        // Retornamos la lista de objetivos posibles
        return targets;
    }


    /// <summary>
    /// Realiza un ataque directo de un h�roe, ya sea cuerpo a cuerpo o a distancia.
    /// Dependiendo del tipo de movimiento, se ejecuta la animaci�n correspondiente y se calcula el da�o.
    /// </summary>
    /// <param name="player">El jugador que est� realizando el ataque (1 o 2).</param>
    /// <param name="heroUsesTheAttack">La carta del h�roe que realiza el ataque.</param>
    /// <param name="movementToUseIndex">El �ndice del movimiento que se va a utilizar.</param>
    /// <param name="lastMove">Indica si este es el �ltimo movimiento de la fase actual.</param>
    /// <returns>Una enumeraci�n de la corrutina.</returns>
    private IEnumerator HeroDirectAttack(int player, Card heroUsesTheAttack, int movementToUseIndex, bool lastMove)
    {
        // Marca la carta atacante como lista para la acci�n y termina su turno.
        heroUsesTheAttack.actionIsReady = false;
        heroUsesTheAttack.EndTurn();

        // Iniciar la animaci�n del ataque dependiendo del tipo de movimiento (cuerpo a cuerpo o a distancia)
        if (heroUsesTheAttack.Moves[movementToUseIndex].MoveSO.MoveType == MoveType.MeleeAttack)
        {
            // Si es un ataque cuerpo a cuerpo, ejecutar la animaci�n de ataque cuerpo a cuerpo
            yield return heroUsesTheAttack.MeleeAttackAnimation(player, null, heroUsesTheAttack.Moves[movementToUseIndex]);
        }
        else
        {
            // Si es un ataque a distancia (u otro tipo), ejecutar la animaci�n correspondiente
            yield return heroUsesTheAttack.RangedMovementAnimation();
        }

        // Esperar un breve tiempo antes de aplicar el da�o
        yield return new WaitForSeconds(0.3f);

        // Verificar qu� jugador est� realizando el ataque
        if (player == 1)
        {
            // Si es el jugador 1, aplicar el da�o al jugador 2
            if (player2Manager.ReceiveDamage(heroUsesTheAttack.Moves[movementToUseIndex]))
            {
                // Si el jugador 2 recibe suficiente da�o y termina el duelo, invocar el fin del duelo
                EndDuel(true);
            }
        }
        else
        {
            // Si es el jugador 2, aplicar el da�o al jugador 1
            if (player1Manager.ReceiveDamage(heroUsesTheAttack.Moves[movementToUseIndex]))
            {
                // Si el jugador 1 recibe suficiente da�o y termina el duelo, invocar el fin del duelo
                EndDuel(false);
            }
        }

        // Mover la carta a su posicion en el campo.
        heroUsesTheAttack.MoveToLastPosition();

        // Resetear el �ndice del movimiento utilizado y otras variables de estado
        movementToUseIndex = -1;

        // Desactivar la selecci�n de objetivos y el estado de ataque
        settingAttackTarget = false;
        cardSelectingTarget = null;

        // Si este es el �ltimo movimiento de la fase, finalizar las acciones
        if (lastMove) yield return FinishActions();
    }


    private List<HeroAction> attackActions = new List<HeroAction>();
    private List<HeroAction> effectActions = new List<HeroAction>();

    [ServerRpc(RequireOwnership = false)]
    /// <summary>
    /// Realiza un ataque de un h�roe en el servidor. El m�todo maneja la prioridad de las acciones, el consumo de energ�a,
    /// y organiza las acciones de ataque seg�n su tipo.
    /// </summary>
    /// <param name="heroToAttackPositionIndex">El �ndice de la posici�n del h�roe que va a ser atacado.</param>
    /// <param name="clientId">El ID del cliente que est� ejecutando la acci�n.</param>
    /// <param name="isHero">Indica si el ataque es realizado por un h�roe (verdadero) o por otro tipo de entidad (falso).</param>
    /// <param name="heroUsesTheAttack">El �ndice de la carta del h�roe que est� realizando el ataque.</param>
    /// <param name="movementToUseIndex">El �ndice del movimiento que se va a utilizar para el ataque.</param>
    public void ProcessAttackServerRpc(int heroToAttackPositionIndex, ulong clientId, bool isHero, int heroUsesTheAttack, int movementToUseIndex)
    {
        ProcessAttack(heroToAttackPositionIndex, isHero, heroUsesTheAttack, movementToUseIndex, clientId);
    }
    public void ProcessAttack(int heroToAttackPositionIndex, bool isHero, int heroUsesTheAttack, int movementToUseIndex, ulong clientId)
    {
        // Si el ataque es realizado por un h�roe
        if (isHero)
        {
            bool hasPriority = false;

            // Verificar el jugador (1 o 2) que est� realizando la acci�n
            if (playerRoles[clientId] == 1)
            {
                // Marcar la carta como lista para la acci�n
                player1Manager.GetFieldPositionList()[heroUsesTheAttack].Card.actionIsReady = true;

                // Verificar si el movimiento es un efecto positivo (esto determina la prioridad)
                hasPriority = player1Manager.GetFieldPositionList()[heroUsesTheAttack].Card.Moves[movementToUseIndex].MoveSO.MoveType == MoveType.PositiveEffect;

                // Consumir energ�a por el movimiento
                player1Manager.ConsumeEnergy(player1Manager.GetFieldPositionList()[heroUsesTheAttack].Card.Moves[movementToUseIndex].MoveSO.EnergyCost);

                // Sincronizar el consumo de energ�a con el cliente
                if (!IsSinglePlayer) ConsumeEnergyClientRpc(clientId, player1Manager.GetFieldPositionList()[heroUsesTheAttack].Card.Moves[movementToUseIndex].MoveSO.EnergyCost);
            }
            else
            {
                // Lo mismo para el jugador 2
                player2Manager.GetFieldPositionList()[heroUsesTheAttack].Card.actionIsReady = true;
                hasPriority = player2Manager.GetFieldPositionList()[heroUsesTheAttack].Card.Moves[movementToUseIndex].MoveSO.MoveType == MoveType.PositiveEffect;
                player2Manager.ConsumeEnergy(player2Manager.GetFieldPositionList()[heroUsesTheAttack].Card.Moves[movementToUseIndex].MoveSO.EnergyCost);
                if (IsSinglePlayer) ConsumeEnergyClientRpc(clientId, player2Manager.GetFieldPositionList()[heroUsesTheAttack].Card.Moves[movementToUseIndex].MoveSO.EnergyCost);
            }

            // Si el movimiento tiene prioridad (es un efecto positivo), se a�ade a las acciones de efecto
            if (hasPriority)
            {
                effectActions.Add(new HeroAction(heroToAttackPositionIndex, clientId, heroUsesTheAttack, movementToUseIndex));
            }
            else
            {
                // Si no tiene prioridad, se a�ade a las acciones de ataque
                attackActions.Add(new HeroAction(heroToAttackPositionIndex, clientId, heroUsesTheAttack, movementToUseIndex));
            }
        }
        else
        {
            // Si no es un h�roe (por ejemplo, si es un efecto que no requiere ataque), se ejecuta el ataque de otra manera
            StartCoroutine(HeroAttackServer(heroToAttackPositionIndex, clientId, isHero, heroUsesTheAttack, movementToUseIndex, true));
        }

        // Si todos los h�roes han indicado sus acciones, se empieza la ejecuci�n de todas las acciones
        if (heroInTurn.All(hero => hero.actionIsReady))
        {
            // Comienza el procesamiento de las acciones
            StartCoroutine(StartActions());

            // Resetear el estado de todos los h�roes, indicando que est�n listos para una nueva acci�n
            foreach (var hero in HeroCardsOnTheField)
            {
                hero.actionIsReady = false;
            }
        }
    }

    /// <summary>
    /// Consume energ�a en el cliente cuando es llamado desde el servidor.
    /// </summary>
    /// <param name="clientId">El ID del cliente que est� realizando la acci�n.</param>
    /// <param name="amount">La cantidad de energ�a a consumir.</param>
    [ClientRpc]
    private void ConsumeEnergyClientRpc(ulong clientId, int amount)
    {
        // Si el cliente actual es el host (servidor), no se realiza ninguna acci�n (el host ya maneja la energ�a)
        if (IsHost) return;

        // Verificar si el cliente que ejecuta esta funci�n es el cliente local
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            // Consumir energ�a para el jugador 1 si este es el cliente local
            player1Manager.ConsumeEnergy(amount);
        }
        else
        {
            // Consumir energ�a para el jugador 2 si el cliente que ejecuta la funci�n no es el local
            player2Manager.ConsumeEnergy(amount);
        }
    }


    /// <summary>
    /// Inicia y ejecuta las acciones programadas en orden de prioridad:
    /// 1. Primero se ejecutan los efectos antes que los ataques.
    /// 2. Luego, se ejecutan los ataques normales.
    /// </summary>
    /// <returns>Una coroutine que ejecuta las acciones en orden.</returns>
    private IEnumerator StartActions()
    {
        // Ejecutar todas las acciones que tienen prioridad (efectos)
        for (int i = 0; i < effectActions.Count; i++)
        {
            // Llamar a HeroAttackServer y esperar a que termine antes de continuar con la siguiente acci�n
            yield return HeroAttackServer(
                effectActions[i].heroToAttackPositionIndex,  // �ndice de la posici�n del h�roe objetivo
                effectActions[i].clientId,                   // ID del cliente que realiza la acci�n
                true,                                        // Indica que es un h�roe el que realiza la acci�n
                effectActions[i].heroUsesTheAttack,          // �ndice del h�roe que usa el ataque
                effectActions[i].movementToUseIndex,         // �ndice del movimiento que se usar�
                i == effectActions.Count - 1 && attackActions.Count == 0 // Si es la �ltima acci�n y no hay ataques restantes, se marca como el �ltimo movimiento
            );

            // Activar los efectos despu�s de ejecutar cada acci�n de efecto
            ActiveEffect();
        }

        // Limpiar la lista de acciones de efectos una vez que todas han sido ejecutadas
        effectActions.Clear();

        // Ejecutar todas las acciones de ataque despu�s de los efectos
        for (int i = 0; i < attackActions.Count; i++)
        {
            yield return HeroAttackServer(
                attackActions[i].heroToAttackPositionIndex,  // �ndice de la posici�n del h�roe objetivo
                attackActions[i].clientId,                   // ID del cliente que realiza la acci�n
                true,                                        // Indica que es un h�roe el que realiza la acci�n
                attackActions[i].heroUsesTheAttack,          // �ndice del h�roe que usa el ataque
                attackActions[i].movementToUseIndex,         // �ndice del movimiento que se usar�
                i == attackActions.Count - 1                 // Se marca como el �ltimo movimiento si es la �ltima acci�n en la lista
            );
        }

        // Limpiar la lista de acciones de ataque despu�s de ejecutarlas
        attackActions.Clear();
    }


    /// <summary>
    /// Finaliza las acciones en curso, activando efectos y enviando cartas al cementerio seg�n la fase del duelo.
    /// </summary>
    /// <returns>Una coroutine que maneja la finalizaci�n de acciones.</returns>
    private IEnumerator FinishActions()
    {
        // Activar cualquier efecto pendiente antes de finalizar la fase de acciones.
        ActiveEffect();

        // Si la fase actual es "PlayingSpellCard" (se est� jugando una carta de hechizo),
        // las cartas se env�an al cementerio de inmediato.
        if (duelPhase.Value == DuelPhase.PlayingSpellCard)
        {
            SendCardsToGraveyard();
        }
        else
        {
            // Si no es la fase de jugar un hechizo, esperar 1 segundo antes de enviar las cartas al cementerio.
            yield return new WaitForSeconds(1);
            SendCardsToGraveyard();
        }

        // Si el c�digo se est� ejecutando en el cliente, marcar al jugador como listo y avanzar de fase.
        if (isSinglePlayer)
        {
            player1Manager.isReady = true;
            player2Manager.isReady = true;
            SetPlayerReadyAndTransitionPhase();
        }
        else
        {
            if (IsClient) SetPlayerReadyAndTransitionPhaseServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }


    /// <summary>
    /// Maneja la ejecuci�n del ataque de un h�roe en el servidor, gestionando tanto ataques directos como a objetivos espec�ficos.
    /// </summary>
    /// <param name="heroToAttackPositionIndex">�ndice de la posici�n del h�roe que ser� atacado, o -1 para un ataque directo.</param>
    /// <param name="clientId">El ID del cliente que realiza el ataque.</param>
    /// <param name="isHero">Indica si el atacante es un h�roe.</param>
    /// <param name="heroUsesTheAttack">�ndice del h�roe que est� atacando.</param>
    /// <param name="movementToUseIndex">�ndice del movimiento a usar en el ataque.</param>
    /// <param name="lastMove">Indica si este es el �ltimo movimiento de la fase.</param>
    /// <returns>Una coroutine que maneja la animaci�n y la l�gica de ataque.</returns>
    /// <summary>
    /// M�todo que maneja la ejecuci�n de un ataque desde un h�roe o hechizo hacia un objetivo en el campo de batalla.
    /// Este m�todo se encarga de determinar qu� carta est� realizando el ataque, invocar el ataque en los clientes a trav�s de RPC,
    /// y gestionar la ejecuci�n del ataque en funci�n del tipo de movimiento utilizado (con o sin efectos positivos).
    /// </summary>
    /// <param name="heroToAttackPositionIndex">�ndice de la posici�n en el campo del h�roe objetivo que ser� atacado.</param>
    /// <param name="clientId">ID del cliente que realiza el ataque.</param>
    /// <param name="isHero">Indica si la carta que realiza el ataque es un h�roe.</param>
    /// <param name="heroUsesTheAttack">�ndice del h�roe que est� realizando el ataque.</param>
    /// <param name="movementToUseIndex">�ndice del movimiento que se est� utilizando para el ataque.</param>
    /// <param name="lastMove">Indica si es el �ltimo movimiento en la secuencia de ataques.</param>
    /// <returns>Un IEnumerator que permite la ejecuci�n secuencial del ataque en el servidor.</returns>
    IEnumerator HeroAttackServer(int heroToAttackPositionIndex, ulong clientId, bool isHero, int heroUsesTheAttack, int movementToUseIndex, bool lastMove)
    {
        // Determina qu� carta est� realizando el ataque (h�roe o hechizo).
        Card attackerCard = isHero ?
            (playerRoles[clientId] == 1 ? player1Manager.GetFieldPositionList()[heroUsesTheAttack].Card : player2Manager.GetFieldPositionList()[heroUsesTheAttack].Card)
            : (playerRoles[clientId] == 1 ? player1Manager.SpellFieldPosition.Card : player2Manager.SpellFieldPosition.Card);


        // Llama a la funci�n RPC para ejecutar el ataque en los clientes.
        if (!isSinglePlayer) HeroAttackClientRpc(heroToAttackPositionIndex, clientId, movementToUseIndex, isHero, heroUsesTheAttack, lastMove);

        // Determina el jugador objetivo: si el cliente es 1, el objetivo ser� el jugador 2, y viceversa.
        int targetPlayer = (playerRoles[clientId] == 1) ? 2 : 1;
        var targetManager = (targetPlayer == 1) ? player1Manager : player2Manager;

        // Verifica si el movimiento a utilizar no es un efecto positivo. Si no lo es, realiza un ataque directo o un ataque a otro h�roe.
        if (attackerCard.Moves[movementToUseIndex].MoveSO.MoveType != MoveType.PositiveEffect)
        {
            // Si no se seleccion� un h�roe objetivo (�ndice -1), realiza un ataque directo.
            if (heroToAttackPositionIndex == -1)
            {
                yield return HeroDirectAttack(playerRoles[clientId], attackerCard, movementToUseIndex, lastMove);
            }
            else
            {
                // Si se seleccion� un h�roe objetivo, realiza un ataque a ese h�roe.
                Card card = targetManager.GetFieldPositionList()[heroToAttackPositionIndex].Card;
                yield return HeroAttack(card, playerRoles[clientId], attackerCard, movementToUseIndex, lastMove);
            }
        }
        else
        {
            // Si el movimiento es un efecto positivo, ataca al h�roe objetivo correspondiente.
            Card card = (targetPlayer == 2) ? player1Manager.GetFieldPositionList()[heroToAttackPositionIndex].Card : player2Manager.GetFieldPositionList()[heroToAttackPositionIndex].Card;
            yield return HeroAttack(card, playerRoles[clientId], attackerCard, movementToUseIndex, lastMove);
        }

        // Restablece las variables relacionadas con la selecci�n de objetivo y el estado de ataque despu�s de completar el ataque.
        settingAttackTarget = false;
        cardSelectingTarget = null;
    }

    /// <summary>
    /// Realiza un ataque de un h�roe a un objetivo, gestionando animaciones, da�o y efectos.
    /// </summary>
    /// <param name="cardToAttack">Carta objetivo del ataque.</param>
    /// <param name="player">Jugador que realiza el ataque (1 o 2).</param>
    /// <param name="attackerCard">Carta del atacante.</param>
    /// <param name="movementToUseIndex">�ndice del movimiento a usar para el ataque.</param>
    /// <param name="lastMove">Indica si este es el �ltimo movimiento de la fase.</param>
    /// <returns>Un IEnumerator para controlar el flujo del ataque de manera as�ncrona.</returns>
    private IEnumerator HeroAttack(Card cardToAttack, int player, Card attackerCard, int movementToUseIndex, bool lastMove)
    {
        // Marca la carta atacante como lista para la acci�n y termina su turno.
        attackerCard.actionIsReady = false;
        attackerCard.EndTurn();

        // Inicia la animaci�n de ataque, dependiendo del tipo de movimiento.
        if (attackerCard.Moves[movementToUseIndex].MoveSO.MoveType == MoveType.MeleeAttack)
        {
            // Animaci�n de ataque cuerpo a cuerpo.
            yield return attackerCard.MeleeAttackAnimation(player, cardToAttack, attackerCard.Moves[movementToUseIndex]);
        }
        else
        {
            // Animaci�n de ataque a distancia (o cualquier otro tipo de movimiento no cuerpo a cuerpo).
            yield return attackerCard.RangedMovementAnimation();
        }

        // Espera un breve tiempo antes de continuar.
        yield return new WaitForSeconds(0.3f);

        // Si el ataque causa da�o, se aplica a la carta objetivo.
        if (attackerCard.Moves[movementToUseIndex].MoveSO.Damage != 0)
        {
            // Animaci�n de da�o para un objetivo �nico.
            if (attackerCard.Moves[movementToUseIndex].MoveSO.TargetsType == TargetsType.SingleTarget)
            {
                cardToAttack.AnimationReceivingMovement(attackerCard.Moves[movementToUseIndex]);

                // Aplica el da�o a la carta objetivo, considerando efectos especiales como la ignorancia de defensa.
                cardToAttack.ReceiveDamage(attackerCard.Moves[movementToUseIndex].MoveSO.Damage,
                    attackerCard.Moves[movementToUseIndex].MoveSO.MoveEffect is IgnoredDefense ignored ? ignored.Amount : 0);
            }
            else
            {
                // Si el ataque tiene m�ltiples objetivos, obtiene todos los objetivos y aplica el da�o.
                var targets = GetTargetsForMovement(cardToAttack, attackerCard, movementToUseIndex);
                foreach (var card in targets)
                {
                    card.AnimationReceivingMovement(attackerCard.Moves[movementToUseIndex]);
                }

                foreach (var card in targets)
                {
                    // Aplica el da�o a todos los objetivos.
                    card.ReceiveDamage(attackerCard.Moves[movementToUseIndex].MoveSO.Damage,
                        attackerCard.Moves[movementToUseIndex].MoveSO.MoveEffect is IgnoredDefense ignored ? ignored.Amount : 0);
                }
            }
        }
        else // Si el ataque no causa da�o, es un ataque de efecto.
        {
            // Animaci�n de efecto para un solo objetivo o varios, dependiendo del tipo de movimiento.
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

            // Espera un breve momento para completar la animaci�n.
            yield return new WaitForSeconds(1);
        }

        // Restaura la posici�n de la carta atacante luego del movimiento.
        attackerCard.MoveToLastPosition();

        // Aplica efectos adicionales del ataque si no es un efecto pasivo.
        if (attackerCard.Moves[movementToUseIndex].MoveSO.TargetsType == TargetsType.SingleTarget)
        {
            if (!attackerCard.Moves[movementToUseIndex].MoveSO.AlwaysActive)
                attackerCard.Moves[movementToUseIndex].ActivateEffect(attackerCard, cardToAttack);
        }
        else
        {
            if (!attackerCard.Moves[movementToUseIndex].MoveSO.AlwaysActive)
                attackerCard.Moves[movementToUseIndex].ActivateEffect(attackerCard, GetTargetsForMovement(cardToAttack, attackerCard, movementToUseIndex));
        }

        // Resetea el �ndice del movimiento utilizado.
        movementToUseIndex = -1;

        // Si el atacante es una carta de hechizo, destruye la carta despu�s de su uso.
        if (attackerCard.cardSO is SpellCardSO)
        {
            if (player1Manager.SpellFieldPosition.Card == attackerCard)
            {
                attackerCard.FieldPosition.DestroyCard(player1Manager.GetGraveyard(), true);
            }
            else
            {
                attackerCard.FieldPosition.DestroyCard(player2Manager.GetGraveyard(), false);
            }
        }

        // Espera un breve momento antes de finalizar la acci�n.
        yield return new WaitForSeconds(1);

        // Si es el �ltimo movimiento, finaliza las acciones.
        if (lastMove) yield return FinishActions();
    }


    /// <summary>
    /// Determina los objetivos de un ataque en funci�n del tipo de movimiento y las condiciones del campo.
    /// </summary>
    /// <param name="cardToAttack">Carta objetivo del ataque.</param>
    /// <param name="attackerCard">Carta que realiza el ataque.</param>
    /// <param name="movementToUseIndex">�ndice del movimiento a usar para determinar los objetivos.</param>
    /// <returns>Una lista de cartas que representan los objetivos del ataque.</returns>
    private List<Card> GetTargetsForMovement(Card cardToAttack, Card attackerCard, int movementToUseIndex)
    {
        // Si el tipo de objetivo es una l�nea (TargetLine).
        if (attackerCard.Moves[movementToUseIndex].MoveSO.TargetsType == TargetsType.TargetLine)
        {
            // Devuelve la l�nea de cartas asociada a la carta objetivo.
            if (player1Manager.GetFieldPositionList().Contains(cardToAttack.FieldPosition))
            {
                return player1Manager.GetLineForCard(cardToAttack);
            }
            else
            {
                return player2Manager.GetLineForCard(cardToAttack);
            }
        }
        // Si el tipo de objetivo es el campo medio (Midfield).
        else if (attackerCard.Moves[movementToUseIndex].MoveSO.TargetsType == TargetsType.Midfield)
        {
            // Verifica si la carta atacante est� en el campo de jugador 1 o jugador 2 y decide los objetivos seg�n el tipo de movimiento.
            if (player1Manager.GetFieldPositionList().Contains(attackerCard.FieldPosition))
            {
                // Si el movimiento es un efecto positivo, devuelve todas las cartas del jugador 1.
                if (attackerCard.Moves[movementToUseIndex].MoveSO.MoveType == MoveType.PositiveEffect)
                {
                    return player1Manager.GetAllCardInField();
                }
                else
                {
                    return player2Manager.GetAllCardInField();
                }
            }
            else
            {
                // Si el movimiento es un efecto positivo, devuelve todas las cartas del jugador 2.
                if (attackerCard.Moves[movementToUseIndex].MoveSO.MoveType == MoveType.PositiveEffect)
                {
                    return player2Manager.GetAllCardInField();
                }
                else
                {
                    return player1Manager.GetAllCardInField();
                }
            }
        }

        // Si no hay un tipo de objetivo v�lido, devuelve null.
        return null;
    }


    /// <summary>
    /// Env�a las cartas de los jugadores al cementerio si su salud es menor o igual a cero.
    /// </summary>
    /// <remarks>
    /// La funci�n recorre las cartas en el campo de cada jugador (jugador 1 y jugador 2), verifica si su salud actual es cero o negativa,
    /// y en caso afirmativo, destruye la carta y la coloca en el cementerio correspondiente. Para el jugador 1, las cartas se env�an
    /// al cementerio de ese jugador, y para el jugador 2, se hace lo mismo.
    /// </remarks>
    private void SendCardsToGraveyard()
    {
        // Recorre las cartas del jugador 1 en el campo.
        foreach (var target in player1Manager.GetFieldPositionList())
        {
            // Si la carta est� en el campo y su salud es 0 o menor, se destruye y se manda al cementerio de jugador 1.
            if (target.Card != null && target.Card.CurrentHealtPoints <= 0)
            {
                target.DestroyCard(player1Manager.GetGraveyard(), true);
            }
        }

        // Recorre las cartas del jugador 2 en el campo.
        foreach (var target in player2Manager.GetFieldPositionList())
        {
            // Si la carta est� en el campo y su salud es 0 o menor, se destruye y se manda al cementerio de jugador 2.
            if (target.Card != null && target.Card.CurrentHealtPoints <= 0)
            {
                target.DestroyCard(player2Manager.GetGraveyard(), false);
            }
        }
    }


    /// <summary>
    /// Funci�n RPC que inicia la ejecuci�n del ataque en el cliente.
    /// </summary>
    /// <remarks>
    /// Esta funci�n es llamada a trav�s de la red para sincronizar y ejecutar un ataque en los clientes. Utiliza el sistema de RPC
    /// para invocar la ejecuci�n de un ataque en el cliente correspondiente, pas�ndole los par�metros necesarios para ejecutar
    /// el ataque de un h�roe o hechizo, seg�n corresponda. Esta funci�n inicia una corrutina para gestionar el proceso del ataque.
    /// </remarks>
    /// <param name="fieldPositionIndex">El �ndice de la posici�n de campo que est� siendo atacada.</param>
    /// <param name="attackerClientId">El ID del cliente que est� realizando el ataque.</param>
    /// <param name="movementToUseIndex">El �ndice del movimiento que se est� utilizando para el ataque.</param>
    /// <param name="isHero">Un valor booleano que indica si el atacante es un h�roe (true) o un hechizo (false).</param>
    /// <param name="heroUsesTheAttack">El �ndice del h�roe que est� usando el ataque (si es un h�roe).</param>
    /// <param name="lastMove">Un valor booleano que indica si este es el �ltimo movimiento a ejecutar.</param>
    [ClientRpc]
    private void HeroAttackClientRpc(int fieldPositionIndex, ulong attackerClientId, int movementToUseIndex, bool isHero, int heroUsesTheAttack, bool lastMove)
    {
        // Inicia la corrutina para ejecutar el ataque en el cliente correspondiente.
        StartCoroutine(HeroAttackClient(fieldPositionIndex, attackerClientId, isHero, heroUsesTheAttack, movementToUseIndex, lastMove));
    }

    /// <summary>
    /// Ejecuta el ataque en el cliente, gestionando la animaci�n y la aplicaci�n del da�o o efecto.
    /// </summary>
    /// <remarks>
    /// Esta funci�n es utilizada para gestionar la ejecuci�n del ataque en el cliente. Seg�n el atacante y el objetivo, 
    /// se determina si se debe realizar un ataque directo o un ataque a un objetivo espec�fico en el campo. Tambi�n se encarga
    /// de sincronizar las animaciones y los efectos del ataque, como el da�o o el efecto positivo, dependiendo del tipo de ataque.
    /// </remarks>
    /// <param name="fieldPositionIndex">El �ndice de la posici�n del campo que est� siendo atacada.</param>
    /// <param name="attackerClientId">El ID del cliente que est� realizando el ataque.</param>
    /// <param name="isHero">Indica si el atacante es un h�roe (true) o un hechizo (false).</param>
    /// <param name="heroUsesTheAttack">El �ndice del h�roe que est� usando el ataque (si es un h�roe).</param>
    /// <param name="movementToUseIndex">El �ndice del movimiento que se est� utilizando para el ataque.</param>
    /// <param name="lastMove">Indica si este es el �ltimo movimiento a ejecutar.</param>
    private IEnumerator HeroAttackClient(int fieldPositionIndex, ulong attackerClientId, bool isHero, int heroUsesTheAttack, int movementToUseIndex, bool lastMove)
    {
        // Si este cliente es el host, no se ejecuta la l�gica en �l.
        if (IsHost) yield break;

        // Determina qu� carta est� realizando el ataque (h�roe o hechizo).
        Card attackerCard = isHero ?
            (NetworkManager.Singleton.LocalClientId == attackerClientId ? player1Manager.GetFieldPositionList()[heroUsesTheAttack].Card : player2Manager.GetFieldPositionList()[heroUsesTheAttack].Card)
            : (NetworkManager.Singleton.LocalClientId == attackerClientId ? player1Manager.SpellFieldPosition.Card : player2Manager.SpellFieldPosition.Card);


        int playerRole = (NetworkManager.Singleton.LocalClientId == attackerClientId) ? 1 : 2;
        var targetManager = (playerRole == 1) ? player2Manager : player1Manager;

        // Si el movimiento no es un efecto positivo, realiza un ataque directo o a un objetivo espec�fico.
        if (attackerCard.Moves[movementToUseIndex].MoveSO.MoveType != MoveType.PositiveEffect)
        {
            if (fieldPositionIndex == -1)
            {
                yield return HeroDirectAttack(playerRole, attackerCard, movementToUseIndex, lastMove);
            }
            else
            {
                Card card = targetManager.GetFieldPositionList()[fieldPositionIndex].Card;
                yield return HeroAttack(card, playerRole, attackerCard, movementToUseIndex, lastMove);
            }
        }
        else // Si es un ataque de tipo efecto positivo, ataca al objetivo correspondiente.
        {
            Card card = playerRole == 1 ? player1Manager.GetFieldPositionList()[fieldPositionIndex].Card : player2Manager.GetFieldPositionList()[fieldPositionIndex].Card;
            yield return HeroAttack(card, playerRole, attackerCard, movementToUseIndex, lastMove);
        }

        // Restablece las variables de selecci�n de objetivo.
        settingAttackTarget = false;
        cardSelectingTarget = null;
    }


    /// <summary>
    /// Avanza al siguiente turno de los h�roes en el duelo. Si es el �ltimo h�roe del turno, se realiza la preparaci�n para el siguiente ciclo de cartas.
    /// </summary>
    /// <remarks>
    /// Esta funci�n gestiona el avance del turno en el juego, incrementando el �ndice del h�roe en turno y comenzando el siguiente turno para ese h�roe. 
    /// Si todos los h�roes han terminado su turno, se resetean las cartas destruidas, se regeneran las defensas y se pasa a la fase de robar cartas.
    /// </remarks>
    private void NextTurn()
    {
        // Si no hemos llegado al final del arreglo de h�roes, avanzar al siguiente h�roe en turno.
        if (heroesInTurnIndex < turns.Length - 1)
        {
            heroesInTurnIndex++; // Avanzar al siguiente h�roe en turno.
            StartHeroTurnClientRpc(heroesInTurnIndex); // Notificar a los clientes sobre el cambio de turno.
            BeginHeroTurn(); // Iniciar el turno del nuevo h�roe.
        }
        else // Si hemos llegado al �ltimo h�roe, reiniciar el ciclo de turnos.
        {
            heroesInTurnIndex = 0; // Reiniciar el �ndice de h�roe al primero.
            RemoveDestroyedCards(); // Eliminar cartas destruidas de los campos de juego.
            RegenerateDefense(); // Regenerar las defensas de los h�roes.
            duelPhase.Value = DuelPhase.DrawingCards; // Pasar a la fase de robar cartas.
        }
    }


    /// <summary>
    /// Inicia el turno de un h�roe en el cliente cuando se recibe una solicitud desde el host.
    /// </summary>
    /// <param name="heroesInTurnIndex">El �ndice del h�roe cuyo turno comienza.</param>
    /// <remarks>
    /// Esta funci�n es llamada en el cliente cuando el servidor (host) notifica que un nuevo h�roe debe comenzar su turno.
    /// La funci�n actualiza el �ndice del h�roe en turno y llama a la funci�n para iniciar dicho turno en el cliente.
    /// </remarks>
    [ClientRpc]
    private void StartHeroTurnClientRpc(int heroesInTurnIndex)
    {
        // Si el cliente es el host, no realiza ninguna acci�n.
        if (IsHost) return;

        // Actualiza el �ndice del h�roe en turno en el cliente.
        this.heroesInTurnIndex = heroesInTurnIndex;

        // Inicia el turno del h�roe en el cliente.
        BeginHeroTurn();

        // Imprime el �ndice del h�roe en turno para fines de depuraci�n.
        Debug.Log(heroesInTurnIndex);
    }


    /// <summary>
    /// Elimina las cartas que han sido destruidas del campo de batalla.
    /// </summary>
    /// <remarks>
    /// Esta funci�n elimina todas las cartas en el campo de batalla que han sido destruidas, es decir, aquellas
    /// cuyo `FieldPosition` es nulo (lo que indica que ya no est�n en el campo).
    /// Despu�s de eliminar las cartas destruidas en el lado del servidor, llama a un RPC para notificar a los clientes.
    /// </remarks>
    private void RemoveDestroyedCards()
    {
        // Elimina todas las cartas que no tienen una posici�n en el campo (es decir, cartas destruidas).
        HeroCardsOnTheField.RemoveAll(card => card.FieldPosition == null);

        // Llama al RPC para notificar a los clientes de las cartas destruidas.
        RemoveDestroyedCardsClientRpc();
    }


    /// <summary>
    /// Elimina las cartas destruidas en el cliente, sincronizando el estado del campo de batalla con el servidor.
    /// </summary>
    /// <remarks>
    /// Esta funci�n se llama en los clientes a trav�s de un RPC (Remote Procedure Call) para asegurarse de que las cartas destruidas
    /// sean eliminadas tambi�n en los clientes. El comportamiento de eliminaci�n es el mismo que en el servidor, eliminando todas las cartas
    /// que no tienen una posici�n en el campo (lo que indica que han sido destruidas).
    /// </remarks>
    [ClientRpc]
    private void RemoveDestroyedCardsClientRpc()
    {
        // Aseg�rate de que solo los clientes (no el host) ejecuten esta funci�n.
        if (IsHost) return;

        // Elimina todas las cartas en el campo de batalla que no tienen una posici�n (indicando que han sido destruidas).
        HeroCardsOnTheField.RemoveAll(card => card.FieldPosition == null);
    }

    /// <summary>
    /// Regenera la defensa de las cartas en el campo de batalla y sincroniza este cambio con los clientes.
    /// </summary>
    /// <remarks>
    /// Esta funci�n recorre todas las cartas en el campo de batalla y regenera su defensa. Luego, sincroniza el cambio de defensa
    /// con los clientes llamando a un RPC (Remote Procedure Call). Esto asegura que todos los jugadores vean la actualizaci�n
    /// de la defensa de las cartas al final de su turno o en momentos relevantes del juego.
    /// </remarks>
    private void RegenerateDefense()
    {
        // Recorre todas las cartas en el campo de batalla.
        foreach (var card in HeroCardsOnTheField)
        {
            // Regenera la defensa de cada carta en el campo de batalla.
            card.RegenerateDefense();

            // Llama al RPC para actualizar la defensa de la carta en los clientes.
            if(!isSinglePlayer) RegenerateDefenseClientRpc(card.FieldPosition.PositionIndex, GetClientIdForHero(card));
        }
    }


    /// <summary>
    /// Sincroniza la regeneraci�n de defensa de una carta en los clientes.
    /// </summary>
    /// <remarks>
    /// Este m�todo es llamado en los clientes para sincronizar la regeneraci�n de defensa de una carta en el campo de batalla.
    /// Asegura que cuando un cliente recibe la actualizaci�n, la carta en su campo de batalla tenga su defensa regenerada.
    /// </remarks>
    /// <param name="fieldPositionIndex">�ndice de la posici�n del campo donde se encuentra la carta cuyo defensa ser� regenerada.</param>
    /// <param name="ownerClientId">ID del cliente propietario de la carta cuya defensa ser� regenerada.</param>
    [ClientRpc]
    private void RegenerateDefenseClientRpc(int fieldPositionIndex, ulong ownerClientId)
    {
        // Si el cliente es el host, no se hace nada, ya que el host maneja la l�gica del servidor.
        if (IsHost) return;

        Card card = null;

        // Determina la carta a regenerar la defensa seg�n el clientId del propietario.
        if (NetworkManager.Singleton.LocalClientId == ownerClientId)
        {
            // Si el cliente es el propietario de la carta, obtiene la carta desde la posici�n del campo del jugador 1.
            card = player1Manager.GetFieldPositionList()[fieldPositionIndex].Card;
        }
        else
        {
            // Si el cliente no es el propietario de la carta, obtiene la carta desde la posici�n del campo del jugador 2.
            card = player2Manager.GetFieldPositionList()[fieldPositionIndex].Card;
        }

        // Regenera la defensa de la carta seleccionada.
        card.RegenerateDefense();
    }


    /// <summary>
    /// Finaliza el duelo y muestra la interfaz de usuario correspondiente al resultado del duelo.
    /// </summary>
    /// <param name="playerVictory">Indica si el jugador ha ganado el duelo. True si el jugador ha ganado, false si ha perdido.</param>
    private void EndDuel(bool playerVictory)
    {
        // Muestra la interfaz de usuario de finalizaci�n del duelo, indicando si el jugador ha ganado o perdido.
        endDuelUI.Show(playerVictory);
    }


    /// <summary>
    /// Obtiene el `PlayerManager` correspondiente al jugador que posee el h�roe especificado.
    /// </summary>
    /// <param name="hero">El objeto `Card` que representa el h�roe cuyo jugador se desea obtener.</param>
    /// <returns>El `PlayerManager` del jugador que posee el h�roe, o null si el h�roe no pertenece a ning�n jugador.</returns>
    public PlayerManager GetMyPlayerManager(Card hero)
    {
        // Si el h�roe est� en el campo del jugador 1, retorna el PlayerManager del jugador 1.
        if (player1Manager.GetFieldPositionList().Contains(hero.FieldPosition))
        {
            return player1Manager;
        }
        // Si el h�roe est� en el campo del jugador 2, retorna el PlayerManager del jugador 2.
        else if (player2Manager.GetFieldPositionList().Contains(hero.FieldPosition))
        {
            return player2Manager;
        }

        // Si no se encuentra el h�roe en los campos de ninguno de los jugadores, retorna null.
        return null;
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
