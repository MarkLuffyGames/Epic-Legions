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
    /// Registra a dos jugadores en la partida, asignándoles roles, identificadores y marcándolos como no listos.
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

        // Inicia el duelo después del registro
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

        // Inicia el duelo después del registro
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
    /// <param name="clientId">ID del jugador que envía el mazo.</param>
    /// <param name="deckCardIds">Lista de IDs de cartas en el mazo del jugador.</param>
    [ServerRpc(RequireOwnership = false)]
    private void ReceiveAndStoreDeckServerRpc(ulong clientId, int[] deckCardIds)
    {
        // Validar si el mazo es válido
        if (deckCardIds == null || deckCardIds.Length == 0)
        {
            Debug.LogWarning($"Player {clientId} sent an invalid deck.");
            return;
        }

        // Guardar el mazo si aún no ha sido registrado
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
        // Validar si el mazo es válido
        if (deckCardIds == null || deckCardIds.Count == 0)
        {
            Debug.LogWarning($"Player {player} sent an invalid deck.");
            return;
        }

        AssignDeckToRole(player1Manager, playerDecks[0]);
        AssignDeckToRole(player2Manager, playerDecks[1]);
    }


    /// <summary>
    /// Envía el mazo de un jugador a los clientes. Si la fase de duelo no es 'Preparación',
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
    /// Asigna el mazo de un jugador según su ID, diferenciando si es el cliente local o el oponente.
    /// </summary>
    /// <param name="targetClientId">ID del jugador al que se le asignará el mazo.</param>
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
    /// Espera hasta que la fase de duelo sea 'Preparación' y luego asigna el mazo al jugador correspondiente.
    /// </summary>
    /// <param name="targetClientId">ID del jugador que recibirá el mazo.</param>
    /// <param name="deckCardIds">Lista de IDs de cartas en el mazo.</param>
    /// <returns>Corrutina que espera hasta que la fase de duelo sea la correcta.</returns>
    private IEnumerator WaitForPreparationPhaseAndAssignDeck(ulong targetClientId, int[] deckCardIds)
    {
        // Espera hasta que la fase de duelo sea 'Preparación'
        yield return new WaitUntil(() => duelPhase.Value == DuelPhase.PreparingDuel);

        // Asigna el mazo al jugador una vez que la fase sea la correcta
        AssignDeckToPlayer(targetClientId, deckCardIds);
    }


    /// <summary>
    /// Asigna un mazo a un jugador basado en su rol, validando cada carta antes de añadirla.
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
    /// Establece el orden de turnos en la batalla basado en la velocidad de los héroes en el campo.
    /// </summary>
    private void InitializeBattleTurns()
    {
        // Limpia las listas de turnos previos
        foreach (var list in turns)
        {
            list.Clear();
        }

        // Verifica si hay héroes en el campo antes de calcular los turnos
        if (HeroCardsOnTheField.Count > 0)
        {
            foreach (var card in HeroCardsOnTheField)
            {
                // Calcula el turno basado en la velocidad del héroe
                int turn = (100 - card.CurrentSpeedPoints) / 5;
                turns[turn].Add(card);
            }
        }
    }

    /// <summary>
    /// Inicia el turno de los héroes en la fase actual, gestionando efectos y verificando estados.
    /// </summary>
    private void BeginHeroTurn()
    {
        // Actualiza el texto de la fase del duelo
        UpdateDuelPhaseText();

        // Gestiona los efectos activos antes de iniciar el turno
        ManageEffects();

        // Si no hay héroes en el turno actual, avanza al siguiente turno
        if (turns[heroesInTurnIndex].Count == 0)
        {
            if (IsServer || IsSinglePlayer) NextTurn();
            return;
        }

        // Reinicia la lista de héroes que participarán en este turno
        heroInTurn.Clear();

        foreach (var card in turns[heroesInTurnIndex])
        {
            if (card.IsStunned())
            {
                card.PassTurn(); // El héroe pierde el turno si está aturdido
            }
            else if (card.FieldPosition != null)
            {
                heroInTurn.Add(card); // Agrega el héroe a la lista si tiene posición en el campo
            }
        }

        // Si no hay héroes disponibles después de la validación, finaliza las acciones del turno
        if (heroInTurn.Count == 0)
        {
            StartCoroutine(FinishActions());
        }
        else
        {
            InitializeHeroTurn(); // Configura el turno del héroe si hay al menos uno disponible
        }
    }


    /// <summary>
    /// Configura el turno de los héroes que están listos para actuar en el campo.
    /// </summary>
    private void InitializeHeroTurn()
    {

        OnChangeTurn?.Invoke(this, EventArgs.Empty);
        // Recorre los héroes en turno y configura su estado
        foreach (var hero in heroInTurn)
        {
            // Verifica si el héroe está en el campo del jugador 1
            if (player1Manager.GetFieldPositionList().Contains(hero.FieldPosition))
            {
                // Establece si el héroe está listo para actuar o no
                hero.SetTurn(!hero.actionIsReady);
            }
            else
            {
                // Si el héroe no está en el campo del jugador 1, se desactiva su turno
                hero.EndTurn();
            }
        }
    }


    /// <summary>
    /// Establece el estado de preparación de un jugador en el servidor y maneja la transición entre fases de duelo.
    /// </summary>
    /// <param name="clientId">ID del cliente que ha indicado que está listo.</param>
    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerReadyAndTransitionPhaseServerRpc(ulong clientId)
    {
        // Si la fase actual es Preparación, se notifica al cliente que está listo
        if (duelPhase.Value == DuelPhase.Preparation) NotifyPlayerReadyClientRpc(clientId);

        // Marca al jugador como listo
        playerReady[clientId] = true;

        // Si todos los jugadores están listos, reinicia el estado de los jugadores y cambia de fase
        if (AreAllTrue(playerReady))
        {
            // Reinicia el estado de todos los jugadores a "no listos"
            playerReady = playerReady.ToDictionary(kvp => kvp.Key, kvp => false);

            // Notifica a los jugadores que ya no están listos
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
        // Transiciones entre las fases del duelo según la fase actual
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
        // Llama al evento para notificar que el jugador está listo
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
        // Llama al evento para notificar que el jugador ya no está listo
        OnPlayerNotReady?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Inicia el duelo configurando la fase inicial como "Preparación del Duelo".
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
    /// <param name="cardIndex">Índice de la carta que se va a colocar en el campo.</param>
    /// <param name="fieldPositionIdex">Índice de la posición en el campo donde se colocará la carta.</param>
    /// <param name="clientId">ID del cliente que está realizando la acción.</param>
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

        // Notifica a los clientes para que actualicen la posición de la carta en el campo
        PlaceCardOnTheFieldClientRpc(cardIndex, fieldPositionIdex, clientId);
    }


    /// <summary>
    /// Coloca una carta en el campo de batalla o en el área de hechizos, y actualiza el estado del jugador.
    /// </summary>
    /// <param name="playerManager">El administrador del jugador que está colocando la carta.</param>
    /// <param name="isPlayer">Indica si la carta es para el jugador o el oponente.</param>
    /// <param name="cardIndex">Índice de la carta en la mano del jugador.</param>
    /// <param name="fieldPositionIdex">Índice de la posición en el campo donde se colocará la carta. Si es -1, la carta va al área de hechizos.</param>
    public void PlaceCardInField(PlayerManager playerManager, bool isPlayer, int cardIndex, int fieldPositionIdex)
    {
        // Obtiene la carta de la mano del jugador
        Card card = playerManager.GetHandCardHandler().GetCardInHandList()[cardIndex];

        // Elimina la carta de la mano del jugador
        playerManager.GetHandCardHandler().QuitCard(card);

        // Si la carta se coloca en el área de hechizos
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
            // Si la carta se coloca en una posición en el campo
            playerManager.GetFieldPositionList()[fieldPositionIdex].SetCard(card, isPlayer);
            InsertCardInOrder(HeroCardsOnTheField, card);
        }

        // Marca la carta como lista para el servidor
        card.waitForServer = false;

        // Consume la energía correspondiente para usar la carta
        playerManager.ConsumeEnergy(card.cardSO is HeroCardSO hero ? hero.Energy : 0);
    }


    /// <summary>
    /// Inserta una carta en la lista de cartas de héroes en orden descendente de puntos de velocidad.
    /// </summary>
    /// <param name="heroCards">La lista de cartas de héroes donde se va a insertar la nueva carta.</param>
    /// <param name="newCard">La nueva carta que se va a insertar en la lista.</param>
    private void InsertCardInOrder(List<Card> heroCards, Card newCard)
    {
        // Encuentra la posición donde insertar la nueva carta, comparando con la velocidad de las cartas existentes
        int insertIndex = heroCards.FindLastIndex(card => card.CurrentSpeedPoints >= newCard.CurrentSpeedPoints);

        // Si no encontró ninguna carta con igual o mayor velocidad, la coloca al inicio
        if (insertIndex == -1)
        {
            heroCards.Insert(0, newCard);
        }
        else
        {
            // Inserta la carta después de la última carta con la misma velocidad o mayor
         heroCards.Insert(insertIndex + 1, newCard);
        }
    }


    /// <summary>
    /// Coloca una carta en el campo del cliente correspondiente, según el índice de la carta y la posición en el campo.
    /// </summary>
    /// <param name="cardIndex">Índice de la carta en la mano del jugador.</param>
    /// <param name="fieldPositionIdex">Índice de la posición en el campo donde se colocará la carta.</param>
    /// <param name="clientId">ID del cliente que solicita la acción de colocar la carta.</param>
    [ClientRpc]
    private void PlaceCardOnTheFieldClientRpc(int cardIndex, int fieldPositionIdex, ulong clientId)
    {
        // Verifica si el servidor está ejecutando esta función, en cuyo caso no realiza ninguna acción.
        if (NetworkManager.Singleton.IsHost) return;

        // Si el cliente actual es el que ha realizado la acción, coloca la carta en el campo del jugador 1
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
    /// Obtiene el ID del cliente al que pertenece el héroe dado, basándose en la posición del héroe en el campo.
    /// </summary>
    /// <param name="heroCard">La carta de héroe cuyo ID de cliente se desea obtener.</param>
    /// <returns>El ID del cliente al que pertenece el héroe, o 0 si no se encuentra el héroe en ningún jugador.</returns>
    private ulong GetClientIdForHero(Card heroCard)
    {
        // Verifica si el héroe está en el campo del jugador 1
        if (player1Manager.GetFieldPositionList().Contains(heroCard.FieldPosition))
        {
            return playerId[1];
        }
        // Verifica si el héroe está en el campo del jugador 2
        else if (player2Manager.GetFieldPositionList().Contains(heroCard.FieldPosition))
        {
            return playerId[2];
        }

        // Si el héroe no se encuentra en el campo de ningún jugador, se muestra un error y se devuelve 0
        Debug.LogError("El héroe no pertenece a ningún jugador");
        return 0;
    }

    private PlayerManager GetPlayerManagerForHero(Card heroCard)
    {
        // Verifica si el héroe está en el campo del jugador 1
        if (player1Manager.GetFieldPositionList().Contains(heroCard.FieldPosition))
        {
            return player1Manager;
        }
        // Verifica si el héroe está en el campo del jugador 2
        else if (player2Manager.GetFieldPositionList().Contains(heroCard.FieldPosition))
        {
            return player2Manager;
        }

        // Si el héroe no se encuentra en el campo de ningún jugador, se muestra un error y se devuelve 0
        Debug.LogError("El héroe no pertenece a ningún jugador");
        return null;
    }

    private PlayerManager GetPlayerManagerRival(Card heroCard)
    {
        // Verifica si el héroe está en el campo del jugador 1
        if (player1Manager.GetFieldPositionList().Contains(heroCard.FieldPosition))
        {
            return player2Manager;
        }
        // Verifica si el héroe está en el campo del jugador 2
        else if (player2Manager.GetFieldPositionList().Contains(heroCard.FieldPosition))
        {
            return player1Manager;
        }

        // Si el héroe no se encuentra en el campo de ningún jugador, se muestra un error y se devuelve 0
        Debug.LogError("El héroe no pertenece a ningún jugador");
        return null;
    }

    /// <summary>
    /// Gestiona los efectos de los héroes en el campo y actualiza su texto de estado.
    /// </summary>
    /// <remarks>
    /// Esta función recorre todos los héroes en el campo, aplica los efectos correspondientes y actualiza los textos asociados a sus estados.
    /// </remarks>
    private void ManageEffects()
    {
        // Itera sobre todos los héroes en el campo
        foreach (var hero in HeroCardsOnTheField)
        {
            // Aplica los efectos del héroe
            hero.ManageEffects();

            // Actualiza el texto que muestra el estado del héroe
            hero.UpdateText();
        }
    }

    /// <summary>
    /// Activa los efectos de todos los héroes presentes en el campo.
    /// </summary>
    /// <remarks>
    /// Esta función recorre todos los héroes en el campo y activa sus efectos correspondientes, si los tienen.
    /// </remarks>
    private void ActiveEffect()
    {
        // Itera sobre todos los héroes en el campo y activa sus efectos
        foreach (var hero in HeroCardsOnTheField)
        {
            hero.ActivateEffect();
        }
    }

    /// <summary>
    /// Utiliza un movimiento específico de un héroe y gestiona su ejecución, incluyendo la selección de objetivo si es necesario.
    /// </summary>
    /// <param name="movementToUseIndex">El índice del movimiento a utilizar.</param>
    /// <param name="card">La carta que representa al héroe que realiza el movimiento.</param>
    /// <remarks>
    /// Si el movimiento seleccionado necesita un objetivo, se invoca la función para seleccionar uno. 
    /// Si no es necesario un objetivo, el movimiento se ejecuta inmediatamente y se marca el héroe como listo para finalizar su turno.
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

            // Marca la acción como lista y finaliza el turno del héroe
            card.actionIsReady = true;
            card.EndTurn();
        }
    }

    /// <summary>
    /// Inicia el proceso de selección de un objetivo para un movimiento específico de un héroe.
    /// Si el movimiento no requiere un objetivo o no se encuentran enemigos en el campo rival, el ataque se dirige a los puntos de vida del oponente.
    /// </summary>
    /// <param name="movementToUseIndex">El índice del movimiento que se va a utilizar.</param>
    /// <param name="attackingCard">La carta que representa al héroe que está realizando el ataque.</param>
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

            // Realiza el ataque directo a los puntos de vida del oponente (sin objetivo específico)
            if (isSinglePlayer)
            {
                var playerManager = GetPlayerManagerForHero(attackingCard);
                ProcessAttack(-1, true, attackingCard.FieldPosition.PositionIndex, movementToUseIndex, playerManager == player1Manager ? 1u : 2u);
            }
            else
            {
                ProcessAttackServerRpc(-1, NetworkManager.Singleton.LocalClientId, true, attackingCard.FieldPosition.PositionIndex, movementToUseIndex);
            }

            // Finaliza el turno del héroe
            cardSelectingTarget.EndTurn();

            // Desactiva los objetivos seleccionables
            DisableAttackableTargets();
        }
    }


    /// <summary>
    /// Desactiva los objetivos que pueden ser seleccionados para un ataque.
    /// Este método se usa para limpiar cualquier selección de objetivos después de un ataque o cuando no hay más objetivos disponibles.
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

        // Resetea las variables de selección
        settingAttackTarget = false;
        cardSelectingTarget = null;
    }


    /// <summary>
    /// Obtiene los objetivos posibles para un movimiento específico de una carta.
    /// Dependiendo del tipo de movimiento (ataque cuerpo a cuerpo, ataque a distancia, efecto positivo, etc.), 
    /// se determinarán las cartas enemigas (o aliadas en el caso de efectos positivos) que pueden ser objetivo.
    /// </summary>
    /// <param name="card">La carta que está realizando el movimiento.</param>
    /// <param name="movementToUseIndex">El índice del movimiento que se va a utilizar.</param>
    /// <returns>Una lista de cartas que pueden ser objetivo del movimiento.</returns>
    public List<Card> ObtainTargets(Card card, int movementToUseIndex)
    {
        // Lista que contendrá los objetivos posibles
        List<Card> targets = new List<Card>();

        // Si el movimiento no es un efecto positivo, buscamos enemigos en el campo del jugador contrario
        if (card.Moves[movementToUseIndex].MoveSO.MoveType != MoveType.PositiveEffect)
        {
            // Itera sobre las posiciones de campo del jugador 2 (enemigo)
            for (int i = 0; i < GetPlayerManagerRival(card).GetFieldPositionList().Count; i++)
            {
                // Si hay una carta en la posición, la agregamos como objetivo
                if (GetPlayerManagerRival(card).GetFieldPositionList()[i].Card != null)
                {
                    targets.Add(GetPlayerManagerRival(card).GetFieldPositionList()[i].Card);
                }

                // Si ya encontramos al menos un objetivo y estamos en las últimas posiciones (en este caso, filas 4, 9 o 14),
                // retornamos la lista de objetivos solo si el movimiento es un ataque cuerpo a cuerpo (melee)
                if (targets.Count > 0 && (i == 4 || i == 9 || i == 14) && card.Moves[movementToUseIndex].MoveSO.MoveType == MoveType.MeleeAttack)
                {
                    return targets; // Terminamos la búsqueda si encontramos un objetivo válido para un ataque cuerpo a cuerpo
                }
            }
        }
        else
        {
            // Si el movimiento es un efecto positivo, los objetivos serán las cartas del jugador aliado (jugador 1),
            // pero no se puede seleccionar la misma carta que está atacando
            foreach (var position in GetPlayerManagerForHero(card).GetFieldPositionList())
            {
                if (position.Card != null && position.Card != card)
                {
                    targets.Add(position.Card); // Añadimos los aliados como objetivo
                }
            }
        }

        // Retornamos la lista de objetivos posibles
        return targets;
    }


    /// <summary>
    /// Realiza un ataque directo de un héroe, ya sea cuerpo a cuerpo o a distancia.
    /// Dependiendo del tipo de movimiento, se ejecuta la animación correspondiente y se calcula el daño.
    /// </summary>
    /// <param name="player">El jugador que está realizando el ataque (1 o 2).</param>
    /// <param name="heroUsesTheAttack">La carta del héroe que realiza el ataque.</param>
    /// <param name="movementToUseIndex">El índice del movimiento que se va a utilizar.</param>
    /// <param name="lastMove">Indica si este es el último movimiento de la fase actual.</param>
    /// <returns>Una enumeración de la corrutina.</returns>
    private IEnumerator HeroDirectAttack(int player, Card heroUsesTheAttack, int movementToUseIndex, bool lastMove)
    {
        // Marca la carta atacante como lista para la acción y termina su turno.
        heroUsesTheAttack.actionIsReady = false;
        heroUsesTheAttack.EndTurn();

        // Iniciar la animación del ataque dependiendo del tipo de movimiento (cuerpo a cuerpo o a distancia)
        if (heroUsesTheAttack.Moves[movementToUseIndex].MoveSO.MoveType == MoveType.MeleeAttack)
        {
            // Si es un ataque cuerpo a cuerpo, ejecutar la animación de ataque cuerpo a cuerpo
            yield return heroUsesTheAttack.MeleeAttackAnimation(player, null, heroUsesTheAttack.Moves[movementToUseIndex]);
        }
        else
        {
            // Si es un ataque a distancia (u otro tipo), ejecutar la animación correspondiente
            yield return heroUsesTheAttack.RangedMovementAnimation();
        }

        // Esperar un breve tiempo antes de aplicar el daño
        yield return new WaitForSeconds(0.3f);

        // Verificar qué jugador está realizando el ataque
        if (player == 1)
        {
            // Si es el jugador 1, aplicar el daño al jugador 2
            if (player2Manager.ReceiveDamage(heroUsesTheAttack.Moves[movementToUseIndex]))
            {
                // Si el jugador 2 recibe suficiente daño y termina el duelo, invocar el fin del duelo
                EndDuel(true);
            }
        }
        else
        {
            // Si es el jugador 2, aplicar el daño al jugador 1
            if (player1Manager.ReceiveDamage(heroUsesTheAttack.Moves[movementToUseIndex]))
            {
                // Si el jugador 1 recibe suficiente daño y termina el duelo, invocar el fin del duelo
                EndDuel(false);
            }
        }

        // Mover la carta a su posicion en el campo.
        heroUsesTheAttack.MoveToLastPosition();

        // Resetear el índice del movimiento utilizado y otras variables de estado
        movementToUseIndex = -1;

        // Desactivar la selección de objetivos y el estado de ataque
        settingAttackTarget = false;
        cardSelectingTarget = null;

        // Si este es el último movimiento de la fase, finalizar las acciones
        if (lastMove) yield return FinishActions();
    }


    private List<HeroAction> attackActions = new List<HeroAction>();
    private List<HeroAction> effectActions = new List<HeroAction>();

    [ServerRpc(RequireOwnership = false)]
    /// <summary>
    /// Realiza un ataque de un héroe en el servidor. El método maneja la prioridad de las acciones, el consumo de energía,
    /// y organiza las acciones de ataque según su tipo.
    /// </summary>
    /// <param name="heroToAttackPositionIndex">El índice de la posición del héroe que va a ser atacado.</param>
    /// <param name="clientId">El ID del cliente que está ejecutando la acción.</param>
    /// <param name="isHero">Indica si el ataque es realizado por un héroe (verdadero) o por otro tipo de entidad (falso).</param>
    /// <param name="heroUsesTheAttack">El índice de la carta del héroe que está realizando el ataque.</param>
    /// <param name="movementToUseIndex">El índice del movimiento que se va a utilizar para el ataque.</param>
    public void ProcessAttackServerRpc(int heroToAttackPositionIndex, ulong clientId, bool isHero, int heroUsesTheAttack, int movementToUseIndex)
    {
        ProcessAttack(heroToAttackPositionIndex, isHero, heroUsesTheAttack, movementToUseIndex, clientId);
    }
    public void ProcessAttack(int heroToAttackPositionIndex, bool isHero, int heroUsesTheAttack, int movementToUseIndex, ulong clientId)
    {
        // Si el ataque es realizado por un héroe
        if (isHero)
        {
            bool hasPriority = false;

            // Verificar el jugador (1 o 2) que está realizando la acción
            if (playerRoles[clientId] == 1)
            {
                // Marcar la carta como lista para la acción
                player1Manager.GetFieldPositionList()[heroUsesTheAttack].Card.actionIsReady = true;

                // Verificar si el movimiento es un efecto positivo (esto determina la prioridad)
                hasPriority = player1Manager.GetFieldPositionList()[heroUsesTheAttack].Card.Moves[movementToUseIndex].MoveSO.MoveType == MoveType.PositiveEffect;

                // Consumir energía por el movimiento
                player1Manager.ConsumeEnergy(player1Manager.GetFieldPositionList()[heroUsesTheAttack].Card.Moves[movementToUseIndex].MoveSO.EnergyCost);

                // Sincronizar el consumo de energía con el cliente
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

            // Si el movimiento tiene prioridad (es un efecto positivo), se añade a las acciones de efecto
            if (hasPriority)
            {
                effectActions.Add(new HeroAction(heroToAttackPositionIndex, clientId, heroUsesTheAttack, movementToUseIndex));
            }
            else
            {
                // Si no tiene prioridad, se añade a las acciones de ataque
                attackActions.Add(new HeroAction(heroToAttackPositionIndex, clientId, heroUsesTheAttack, movementToUseIndex));
            }
        }
        else
        {
            // Si no es un héroe (por ejemplo, si es un efecto que no requiere ataque), se ejecuta el ataque de otra manera
            StartCoroutine(HeroAttackServer(heroToAttackPositionIndex, clientId, isHero, heroUsesTheAttack, movementToUseIndex, true));
        }

        // Si todos los héroes han indicado sus acciones, se empieza la ejecución de todas las acciones
        if (heroInTurn.All(hero => hero.actionIsReady))
        {
            // Comienza el procesamiento de las acciones
            StartCoroutine(StartActions());

            // Resetear el estado de todos los héroes, indicando que están listos para una nueva acción
            foreach (var hero in HeroCardsOnTheField)
            {
                hero.actionIsReady = false;
            }
        }
    }

    /// <summary>
    /// Consume energía en el cliente cuando es llamado desde el servidor.
    /// </summary>
    /// <param name="clientId">El ID del cliente que está realizando la acción.</param>
    /// <param name="amount">La cantidad de energía a consumir.</param>
    [ClientRpc]
    private void ConsumeEnergyClientRpc(ulong clientId, int amount)
    {
        // Si el cliente actual es el host (servidor), no se realiza ninguna acción (el host ya maneja la energía)
        if (IsHost) return;

        // Verificar si el cliente que ejecuta esta función es el cliente local
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            // Consumir energía para el jugador 1 si este es el cliente local
            player1Manager.ConsumeEnergy(amount);
        }
        else
        {
            // Consumir energía para el jugador 2 si el cliente que ejecuta la función no es el local
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
            // Llamar a HeroAttackServer y esperar a que termine antes de continuar con la siguiente acción
            yield return HeroAttackServer(
                effectActions[i].heroToAttackPositionIndex,  // Índice de la posición del héroe objetivo
                effectActions[i].clientId,                   // ID del cliente que realiza la acción
                true,                                        // Indica que es un héroe el que realiza la acción
                effectActions[i].heroUsesTheAttack,          // Índice del héroe que usa el ataque
                effectActions[i].movementToUseIndex,         // Índice del movimiento que se usará
                i == effectActions.Count - 1 && attackActions.Count == 0 // Si es la última acción y no hay ataques restantes, se marca como el último movimiento
            );

            // Activar los efectos después de ejecutar cada acción de efecto
            ActiveEffect();
        }

        // Limpiar la lista de acciones de efectos una vez que todas han sido ejecutadas
        effectActions.Clear();

        // Ejecutar todas las acciones de ataque después de los efectos
        for (int i = 0; i < attackActions.Count; i++)
        {
            yield return HeroAttackServer(
                attackActions[i].heroToAttackPositionIndex,  // Índice de la posición del héroe objetivo
                attackActions[i].clientId,                   // ID del cliente que realiza la acción
                true,                                        // Indica que es un héroe el que realiza la acción
                attackActions[i].heroUsesTheAttack,          // Índice del héroe que usa el ataque
                attackActions[i].movementToUseIndex,         // Índice del movimiento que se usará
                i == attackActions.Count - 1                 // Se marca como el último movimiento si es la última acción en la lista
            );
        }

        // Limpiar la lista de acciones de ataque después de ejecutarlas
        attackActions.Clear();
    }


    /// <summary>
    /// Finaliza las acciones en curso, activando efectos y enviando cartas al cementerio según la fase del duelo.
    /// </summary>
    /// <returns>Una coroutine que maneja la finalización de acciones.</returns>
    private IEnumerator FinishActions()
    {
        // Activar cualquier efecto pendiente antes de finalizar la fase de acciones.
        ActiveEffect();

        // Si la fase actual es "PlayingSpellCard" (se está jugando una carta de hechizo),
        // las cartas se envían al cementerio de inmediato.
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

        // Si el código se está ejecutando en el cliente, marcar al jugador como listo y avanzar de fase.
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
    /// Maneja la ejecución del ataque de un héroe en el servidor, gestionando tanto ataques directos como a objetivos específicos.
    /// </summary>
    /// <param name="heroToAttackPositionIndex">Índice de la posición del héroe que será atacado, o -1 para un ataque directo.</param>
    /// <param name="clientId">El ID del cliente que realiza el ataque.</param>
    /// <param name="isHero">Indica si el atacante es un héroe.</param>
    /// <param name="heroUsesTheAttack">Índice del héroe que está atacando.</param>
    /// <param name="movementToUseIndex">Índice del movimiento a usar en el ataque.</param>
    /// <param name="lastMove">Indica si este es el último movimiento de la fase.</param>
    /// <returns>Una coroutine que maneja la animación y la lógica de ataque.</returns>
    /// <summary>
    /// Método que maneja la ejecución de un ataque desde un héroe o hechizo hacia un objetivo en el campo de batalla.
    /// Este método se encarga de determinar qué carta está realizando el ataque, invocar el ataque en los clientes a través de RPC,
    /// y gestionar la ejecución del ataque en función del tipo de movimiento utilizado (con o sin efectos positivos).
    /// </summary>
    /// <param name="heroToAttackPositionIndex">Índice de la posición en el campo del héroe objetivo que será atacado.</param>
    /// <param name="clientId">ID del cliente que realiza el ataque.</param>
    /// <param name="isHero">Indica si la carta que realiza el ataque es un héroe.</param>
    /// <param name="heroUsesTheAttack">Índice del héroe que está realizando el ataque.</param>
    /// <param name="movementToUseIndex">Índice del movimiento que se está utilizando para el ataque.</param>
    /// <param name="lastMove">Indica si es el último movimiento en la secuencia de ataques.</param>
    /// <returns>Un IEnumerator que permite la ejecución secuencial del ataque en el servidor.</returns>
    IEnumerator HeroAttackServer(int heroToAttackPositionIndex, ulong clientId, bool isHero, int heroUsesTheAttack, int movementToUseIndex, bool lastMove)
    {
        // Determina qué carta está realizando el ataque (héroe o hechizo).
        Card attackerCard = isHero ?
            (playerRoles[clientId] == 1 ? player1Manager.GetFieldPositionList()[heroUsesTheAttack].Card : player2Manager.GetFieldPositionList()[heroUsesTheAttack].Card)
            : (playerRoles[clientId] == 1 ? player1Manager.SpellFieldPosition.Card : player2Manager.SpellFieldPosition.Card);


        // Llama a la función RPC para ejecutar el ataque en los clientes.
        if (!isSinglePlayer) HeroAttackClientRpc(heroToAttackPositionIndex, clientId, movementToUseIndex, isHero, heroUsesTheAttack, lastMove);

        // Determina el jugador objetivo: si el cliente es 1, el objetivo será el jugador 2, y viceversa.
        int targetPlayer = (playerRoles[clientId] == 1) ? 2 : 1;
        var targetManager = (targetPlayer == 1) ? player1Manager : player2Manager;

        // Verifica si el movimiento a utilizar no es un efecto positivo. Si no lo es, realiza un ataque directo o un ataque a otro héroe.
        if (attackerCard.Moves[movementToUseIndex].MoveSO.MoveType != MoveType.PositiveEffect)
        {
            // Si no se seleccionó un héroe objetivo (índice -1), realiza un ataque directo.
            if (heroToAttackPositionIndex == -1)
            {
                yield return HeroDirectAttack(playerRoles[clientId], attackerCard, movementToUseIndex, lastMove);
            }
            else
            {
                // Si se seleccionó un héroe objetivo, realiza un ataque a ese héroe.
                Card card = targetManager.GetFieldPositionList()[heroToAttackPositionIndex].Card;
                yield return HeroAttack(card, playerRoles[clientId], attackerCard, movementToUseIndex, lastMove);
            }
        }
        else
        {
            // Si el movimiento es un efecto positivo, ataca al héroe objetivo correspondiente.
            Card card = (targetPlayer == 2) ? player1Manager.GetFieldPositionList()[heroToAttackPositionIndex].Card : player2Manager.GetFieldPositionList()[heroToAttackPositionIndex].Card;
            yield return HeroAttack(card, playerRoles[clientId], attackerCard, movementToUseIndex, lastMove);
        }

        // Restablece las variables relacionadas con la selección de objetivo y el estado de ataque después de completar el ataque.
        settingAttackTarget = false;
        cardSelectingTarget = null;
    }

    /// <summary>
    /// Realiza un ataque de un héroe a un objetivo, gestionando animaciones, daño y efectos.
    /// </summary>
    /// <param name="cardToAttack">Carta objetivo del ataque.</param>
    /// <param name="player">Jugador que realiza el ataque (1 o 2).</param>
    /// <param name="attackerCard">Carta del atacante.</param>
    /// <param name="movementToUseIndex">Índice del movimiento a usar para el ataque.</param>
    /// <param name="lastMove">Indica si este es el último movimiento de la fase.</param>
    /// <returns>Un IEnumerator para controlar el flujo del ataque de manera asíncrona.</returns>
    private IEnumerator HeroAttack(Card cardToAttack, int player, Card attackerCard, int movementToUseIndex, bool lastMove)
    {
        // Marca la carta atacante como lista para la acción y termina su turno.
        attackerCard.actionIsReady = false;
        attackerCard.EndTurn();

        // Inicia la animación de ataque, dependiendo del tipo de movimiento.
        if (attackerCard.Moves[movementToUseIndex].MoveSO.MoveType == MoveType.MeleeAttack)
        {
            // Animación de ataque cuerpo a cuerpo.
            yield return attackerCard.MeleeAttackAnimation(player, cardToAttack, attackerCard.Moves[movementToUseIndex]);
        }
        else
        {
            // Animación de ataque a distancia (o cualquier otro tipo de movimiento no cuerpo a cuerpo).
            yield return attackerCard.RangedMovementAnimation();
        }

        // Espera un breve tiempo antes de continuar.
        yield return new WaitForSeconds(0.3f);

        // Si el ataque causa daño, se aplica a la carta objetivo.
        if (attackerCard.Moves[movementToUseIndex].MoveSO.Damage != 0)
        {
            // Animación de daño para un objetivo único.
            if (attackerCard.Moves[movementToUseIndex].MoveSO.TargetsType == TargetsType.SingleTarget)
            {
                cardToAttack.AnimationReceivingMovement(attackerCard.Moves[movementToUseIndex]);

                // Aplica el daño a la carta objetivo, considerando efectos especiales como la ignorancia de defensa.
                cardToAttack.ReceiveDamage(attackerCard.Moves[movementToUseIndex].MoveSO.Damage,
                    attackerCard.Moves[movementToUseIndex].MoveSO.MoveEffect is IgnoredDefense ignored ? ignored.Amount : 0);
            }
            else
            {
                // Si el ataque tiene múltiples objetivos, obtiene todos los objetivos y aplica el daño.
                var targets = GetTargetsForMovement(cardToAttack, attackerCard, movementToUseIndex);
                foreach (var card in targets)
                {
                    card.AnimationReceivingMovement(attackerCard.Moves[movementToUseIndex]);
                }

                foreach (var card in targets)
                {
                    // Aplica el daño a todos los objetivos.
                    card.ReceiveDamage(attackerCard.Moves[movementToUseIndex].MoveSO.Damage,
                        attackerCard.Moves[movementToUseIndex].MoveSO.MoveEffect is IgnoredDefense ignored ? ignored.Amount : 0);
                }
            }
        }
        else // Si el ataque no causa daño, es un ataque de efecto.
        {
            // Animación de efecto para un solo objetivo o varios, dependiendo del tipo de movimiento.
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

            // Espera un breve momento para completar la animación.
            yield return new WaitForSeconds(1);
        }

        // Restaura la posición de la carta atacante luego del movimiento.
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

        // Resetea el índice del movimiento utilizado.
        movementToUseIndex = -1;

        // Si el atacante es una carta de hechizo, destruye la carta después de su uso.
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

        // Espera un breve momento antes de finalizar la acción.
        yield return new WaitForSeconds(1);

        // Si es el último movimiento, finaliza las acciones.
        if (lastMove) yield return FinishActions();
    }


    /// <summary>
    /// Determina los objetivos de un ataque en función del tipo de movimiento y las condiciones del campo.
    /// </summary>
    /// <param name="cardToAttack">Carta objetivo del ataque.</param>
    /// <param name="attackerCard">Carta que realiza el ataque.</param>
    /// <param name="movementToUseIndex">Índice del movimiento a usar para determinar los objetivos.</param>
    /// <returns>Una lista de cartas que representan los objetivos del ataque.</returns>
    private List<Card> GetTargetsForMovement(Card cardToAttack, Card attackerCard, int movementToUseIndex)
    {
        // Si el tipo de objetivo es una línea (TargetLine).
        if (attackerCard.Moves[movementToUseIndex].MoveSO.TargetsType == TargetsType.TargetLine)
        {
            // Devuelve la línea de cartas asociada a la carta objetivo.
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
            // Verifica si la carta atacante está en el campo de jugador 1 o jugador 2 y decide los objetivos según el tipo de movimiento.
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

        // Si no hay un tipo de objetivo válido, devuelve null.
        return null;
    }


    /// <summary>
    /// Envía las cartas de los jugadores al cementerio si su salud es menor o igual a cero.
    /// </summary>
    /// <remarks>
    /// La función recorre las cartas en el campo de cada jugador (jugador 1 y jugador 2), verifica si su salud actual es cero o negativa,
    /// y en caso afirmativo, destruye la carta y la coloca en el cementerio correspondiente. Para el jugador 1, las cartas se envían
    /// al cementerio de ese jugador, y para el jugador 2, se hace lo mismo.
    /// </remarks>
    private void SendCardsToGraveyard()
    {
        // Recorre las cartas del jugador 1 en el campo.
        foreach (var target in player1Manager.GetFieldPositionList())
        {
            // Si la carta está en el campo y su salud es 0 o menor, se destruye y se manda al cementerio de jugador 1.
            if (target.Card != null && target.Card.CurrentHealtPoints <= 0)
            {
                target.DestroyCard(player1Manager.GetGraveyard(), true);
            }
        }

        // Recorre las cartas del jugador 2 en el campo.
        foreach (var target in player2Manager.GetFieldPositionList())
        {
            // Si la carta está en el campo y su salud es 0 o menor, se destruye y se manda al cementerio de jugador 2.
            if (target.Card != null && target.Card.CurrentHealtPoints <= 0)
            {
                target.DestroyCard(player2Manager.GetGraveyard(), false);
            }
        }
    }


    /// <summary>
    /// Función RPC que inicia la ejecución del ataque en el cliente.
    /// </summary>
    /// <remarks>
    /// Esta función es llamada a través de la red para sincronizar y ejecutar un ataque en los clientes. Utiliza el sistema de RPC
    /// para invocar la ejecución de un ataque en el cliente correspondiente, pasándole los parámetros necesarios para ejecutar
    /// el ataque de un héroe o hechizo, según corresponda. Esta función inicia una corrutina para gestionar el proceso del ataque.
    /// </remarks>
    /// <param name="fieldPositionIndex">El índice de la posición de campo que está siendo atacada.</param>
    /// <param name="attackerClientId">El ID del cliente que está realizando el ataque.</param>
    /// <param name="movementToUseIndex">El índice del movimiento que se está utilizando para el ataque.</param>
    /// <param name="isHero">Un valor booleano que indica si el atacante es un héroe (true) o un hechizo (false).</param>
    /// <param name="heroUsesTheAttack">El índice del héroe que está usando el ataque (si es un héroe).</param>
    /// <param name="lastMove">Un valor booleano que indica si este es el último movimiento a ejecutar.</param>
    [ClientRpc]
    private void HeroAttackClientRpc(int fieldPositionIndex, ulong attackerClientId, int movementToUseIndex, bool isHero, int heroUsesTheAttack, bool lastMove)
    {
        // Inicia la corrutina para ejecutar el ataque en el cliente correspondiente.
        StartCoroutine(HeroAttackClient(fieldPositionIndex, attackerClientId, isHero, heroUsesTheAttack, movementToUseIndex, lastMove));
    }

    /// <summary>
    /// Ejecuta el ataque en el cliente, gestionando la animación y la aplicación del daño o efecto.
    /// </summary>
    /// <remarks>
    /// Esta función es utilizada para gestionar la ejecución del ataque en el cliente. Según el atacante y el objetivo, 
    /// se determina si se debe realizar un ataque directo o un ataque a un objetivo específico en el campo. También se encarga
    /// de sincronizar las animaciones y los efectos del ataque, como el daño o el efecto positivo, dependiendo del tipo de ataque.
    /// </remarks>
    /// <param name="fieldPositionIndex">El índice de la posición del campo que está siendo atacada.</param>
    /// <param name="attackerClientId">El ID del cliente que está realizando el ataque.</param>
    /// <param name="isHero">Indica si el atacante es un héroe (true) o un hechizo (false).</param>
    /// <param name="heroUsesTheAttack">El índice del héroe que está usando el ataque (si es un héroe).</param>
    /// <param name="movementToUseIndex">El índice del movimiento que se está utilizando para el ataque.</param>
    /// <param name="lastMove">Indica si este es el último movimiento a ejecutar.</param>
    private IEnumerator HeroAttackClient(int fieldPositionIndex, ulong attackerClientId, bool isHero, int heroUsesTheAttack, int movementToUseIndex, bool lastMove)
    {
        // Si este cliente es el host, no se ejecuta la lógica en él.
        if (IsHost) yield break;

        // Determina qué carta está realizando el ataque (héroe o hechizo).
        Card attackerCard = isHero ?
            (NetworkManager.Singleton.LocalClientId == attackerClientId ? player1Manager.GetFieldPositionList()[heroUsesTheAttack].Card : player2Manager.GetFieldPositionList()[heroUsesTheAttack].Card)
            : (NetworkManager.Singleton.LocalClientId == attackerClientId ? player1Manager.SpellFieldPosition.Card : player2Manager.SpellFieldPosition.Card);


        int playerRole = (NetworkManager.Singleton.LocalClientId == attackerClientId) ? 1 : 2;
        var targetManager = (playerRole == 1) ? player2Manager : player1Manager;

        // Si el movimiento no es un efecto positivo, realiza un ataque directo o a un objetivo específico.
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

        // Restablece las variables de selección de objetivo.
        settingAttackTarget = false;
        cardSelectingTarget = null;
    }


    /// <summary>
    /// Avanza al siguiente turno de los héroes en el duelo. Si es el último héroe del turno, se realiza la preparación para el siguiente ciclo de cartas.
    /// </summary>
    /// <remarks>
    /// Esta función gestiona el avance del turno en el juego, incrementando el índice del héroe en turno y comenzando el siguiente turno para ese héroe. 
    /// Si todos los héroes han terminado su turno, se resetean las cartas destruidas, se regeneran las defensas y se pasa a la fase de robar cartas.
    /// </remarks>
    private void NextTurn()
    {
        // Si no hemos llegado al final del arreglo de héroes, avanzar al siguiente héroe en turno.
        if (heroesInTurnIndex < turns.Length - 1)
        {
            heroesInTurnIndex++; // Avanzar al siguiente héroe en turno.
            StartHeroTurnClientRpc(heroesInTurnIndex); // Notificar a los clientes sobre el cambio de turno.
            BeginHeroTurn(); // Iniciar el turno del nuevo héroe.
        }
        else // Si hemos llegado al último héroe, reiniciar el ciclo de turnos.
        {
            heroesInTurnIndex = 0; // Reiniciar el índice de héroe al primero.
            RemoveDestroyedCards(); // Eliminar cartas destruidas de los campos de juego.
            RegenerateDefense(); // Regenerar las defensas de los héroes.
            duelPhase.Value = DuelPhase.DrawingCards; // Pasar a la fase de robar cartas.
        }
    }


    /// <summary>
    /// Inicia el turno de un héroe en el cliente cuando se recibe una solicitud desde el host.
    /// </summary>
    /// <param name="heroesInTurnIndex">El índice del héroe cuyo turno comienza.</param>
    /// <remarks>
    /// Esta función es llamada en el cliente cuando el servidor (host) notifica que un nuevo héroe debe comenzar su turno.
    /// La función actualiza el índice del héroe en turno y llama a la función para iniciar dicho turno en el cliente.
    /// </remarks>
    [ClientRpc]
    private void StartHeroTurnClientRpc(int heroesInTurnIndex)
    {
        // Si el cliente es el host, no realiza ninguna acción.
        if (IsHost) return;

        // Actualiza el índice del héroe en turno en el cliente.
        this.heroesInTurnIndex = heroesInTurnIndex;

        // Inicia el turno del héroe en el cliente.
        BeginHeroTurn();

        // Imprime el índice del héroe en turno para fines de depuración.
        Debug.Log(heroesInTurnIndex);
    }


    /// <summary>
    /// Elimina las cartas que han sido destruidas del campo de batalla.
    /// </summary>
    /// <remarks>
    /// Esta función elimina todas las cartas en el campo de batalla que han sido destruidas, es decir, aquellas
    /// cuyo `FieldPosition` es nulo (lo que indica que ya no están en el campo).
    /// Después de eliminar las cartas destruidas en el lado del servidor, llama a un RPC para notificar a los clientes.
    /// </remarks>
    private void RemoveDestroyedCards()
    {
        // Elimina todas las cartas que no tienen una posición en el campo (es decir, cartas destruidas).
        HeroCardsOnTheField.RemoveAll(card => card.FieldPosition == null);

        // Llama al RPC para notificar a los clientes de las cartas destruidas.
        RemoveDestroyedCardsClientRpc();
    }


    /// <summary>
    /// Elimina las cartas destruidas en el cliente, sincronizando el estado del campo de batalla con el servidor.
    /// </summary>
    /// <remarks>
    /// Esta función se llama en los clientes a través de un RPC (Remote Procedure Call) para asegurarse de que las cartas destruidas
    /// sean eliminadas también en los clientes. El comportamiento de eliminación es el mismo que en el servidor, eliminando todas las cartas
    /// que no tienen una posición en el campo (lo que indica que han sido destruidas).
    /// </remarks>
    [ClientRpc]
    private void RemoveDestroyedCardsClientRpc()
    {
        // Asegúrate de que solo los clientes (no el host) ejecuten esta función.
        if (IsHost) return;

        // Elimina todas las cartas en el campo de batalla que no tienen una posición (indicando que han sido destruidas).
        HeroCardsOnTheField.RemoveAll(card => card.FieldPosition == null);
    }

    /// <summary>
    /// Regenera la defensa de las cartas en el campo de batalla y sincroniza este cambio con los clientes.
    /// </summary>
    /// <remarks>
    /// Esta función recorre todas las cartas en el campo de batalla y regenera su defensa. Luego, sincroniza el cambio de defensa
    /// con los clientes llamando a un RPC (Remote Procedure Call). Esto asegura que todos los jugadores vean la actualización
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
    /// Sincroniza la regeneración de defensa de una carta en los clientes.
    /// </summary>
    /// <remarks>
    /// Este método es llamado en los clientes para sincronizar la regeneración de defensa de una carta en el campo de batalla.
    /// Asegura que cuando un cliente recibe la actualización, la carta en su campo de batalla tenga su defensa regenerada.
    /// </remarks>
    /// <param name="fieldPositionIndex">Índice de la posición del campo donde se encuentra la carta cuyo defensa será regenerada.</param>
    /// <param name="ownerClientId">ID del cliente propietario de la carta cuya defensa será regenerada.</param>
    [ClientRpc]
    private void RegenerateDefenseClientRpc(int fieldPositionIndex, ulong ownerClientId)
    {
        // Si el cliente es el host, no se hace nada, ya que el host maneja la lógica del servidor.
        if (IsHost) return;

        Card card = null;

        // Determina la carta a regenerar la defensa según el clientId del propietario.
        if (NetworkManager.Singleton.LocalClientId == ownerClientId)
        {
            // Si el cliente es el propietario de la carta, obtiene la carta desde la posición del campo del jugador 1.
            card = player1Manager.GetFieldPositionList()[fieldPositionIndex].Card;
        }
        else
        {
            // Si el cliente no es el propietario de la carta, obtiene la carta desde la posición del campo del jugador 2.
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
        // Muestra la interfaz de usuario de finalización del duelo, indicando si el jugador ha ganado o perdido.
        endDuelUI.Show(playerVictory);
    }


    /// <summary>
    /// Obtiene el `PlayerManager` correspondiente al jugador que posee el héroe especificado.
    /// </summary>
    /// <param name="hero">El objeto `Card` que representa el héroe cuyo jugador se desea obtener.</param>
    /// <returns>El `PlayerManager` del jugador que posee el héroe, o null si el héroe no pertenece a ningún jugador.</returns>
    public PlayerManager GetMyPlayerManager(Card hero)
    {
        // Si el héroe está en el campo del jugador 1, retorna el PlayerManager del jugador 1.
        if (player1Manager.GetFieldPositionList().Contains(hero.FieldPosition))
        {
            return player1Manager;
        }
        // Si el héroe está en el campo del jugador 2, retorna el PlayerManager del jugador 2.
        else if (player2Manager.GetFieldPositionList().Contains(hero.FieldPosition))
        {
            return player2Manager;
        }

        // Si no se encuentra el héroe en los campos de ninguno de los jugadores, retorna null.
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
