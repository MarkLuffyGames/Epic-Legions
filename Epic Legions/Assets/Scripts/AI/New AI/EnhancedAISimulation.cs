using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnhancedAISimulation
{
    private PlayerManager myManager;
    private PlayerManager enemyManager;
    private DuelManager duelManager;
    private SimSnapshot currentSnapshot;

    private bool showDebugLogs;

    public EnhancedAISimulation(PlayerManager myManager, PlayerManager enemyManager, DuelManager duelManager, bool showDebugLogs = false)
    {
        this.myManager = myManager;
        this.enemyManager = enemyManager;
        this.duelManager = duelManager;
        this.showDebugLogs = showDebugLogs;
    }

    public SimSnapshot BuildSimSnapshot()
    {
        currentSnapshot = new SimSnapshot
        {
            MyLife = myManager.PlayerHealt,
            EnemyLife = enemyManager.PlayerHealt,
            MyEnergy = myManager.PlayerEnergy,
            EnemyEnergy = enemyManager.PlayerEnergy,
            CurrentSubTurn = duelManager.HeroesInTurnIndex,
            OriginalSubTurn = duelManager.HeroesInTurnIndex,
            SubTurnsPassedInSimulation = 0
        };

        // Procesar héroes de ambos jugadores
        ProcessHeroesForSnapshot(myManager, currentSnapshot, true);
        ProcessHeroesForSnapshot(enemyManager, currentSnapshot, false);

        // Reconstruir listas controladas
        RebuildControlledLists(currentSnapshot);

        if (showDebugLogs)
            Debug.Log($"Snapshot creado: {currentSnapshot.MyControlledHeroes.Count} héroes controlados, {currentSnapshot.EnemyHeroes.Count} enemigos");

        return currentSnapshot;
    }

    private void ProcessHeroesForSnapshot(PlayerManager manager, SimSnapshot snapshot, bool isOwnerMine)
    {
        var fieldCards = manager.GetAllCardInField();

        foreach (var card in fieldCards)
        {
            if (card.cardSO is HeroCardSO heroSO)
            {
                var simState = CreateSimCardState(card, isOwnerMine, heroSO, snapshot);
                snapshot.CardStates[card] = simState;
            }
        }
    }

    private SimCardState CreateSimCardState(Card card, bool ownerIsMine, HeroCardSO heroSO, SimSnapshot snapshot)
    {
        var state = new SimCardState
        {
            OriginalCard = card,
            OwnerIsMine = ownerIsMine,
            HP = heroSO.Health,
            CurrentHP = card.CurrentHealthPoints,
            DEF = heroSO.Defense,
            CurrentDEF = card.CurrentDefensePoints,
            SPD = card.Speed,
            CurrentSPD = card.CurrentSpeedPoints,
            FieldIndex = card.FieldPosition?.PositionIndex ?? -1,
            Alive = card.CurrentHealthPoints > 0,
            moves = new List<Movement>(card.Moves),
            equipmentCard = (Card[])card.EquipmentCard.Clone(),
            activeEffects = new List<Effect>(card.ActiveEffects),
            snapshot = snapshot
        };

        // Determinar controlador
        state.ControllerIsMine = DetermineController(card, ownerIsMine);

        // Aplicar equipamiento
        ApplyEquipmentEffects(card, state);

        return state;
    }

    private bool DetermineController(Card card, bool ownerIsMine)
    {
        if (card.IsControlled())
        {
            var controller = card.GetController();
            if (controller != null)
            {
                // Verificar si el controlador es de la IA
                return duelManager.GetPlayerManagerForCard(controller) == myManager;
            }
        }
        return ownerIsMine;
    }

    private void ApplyEquipmentEffects(Card card, SimCardState state)
    {
        // Aplicar efectos de equipamiento si existen
        foreach (var equipment in card.EquipmentCard)
        {
            if (equipment != null && equipment.cardSO is EquipmentCardSO equipmentSO)
            {
                if (equipmentSO.Effect != null)
                {
                    // Aquí podrías aplicar efectos pasivos del equipamiento
                    // Por ahora, solo aplicamos el efecto si no es pasivo
                    if (!equipmentSO.Effect.isPassive)
                    {
                        // Simular efecto de equipamiento (simplificado)
                    }
                }
            }
        }
    }

    private void RebuildControlledLists(SimSnapshot snapshot)
    {
        snapshot.MyControlledHeroes.Clear();
        snapshot.EnemyHeroes.Clear();

        foreach (var state in snapshot.CardStates.Values)
        {
            if (!state.Alive) continue;

            if (state.ControllerIsMine)
                snapshot.MyControlledHeroes.Add(state);
            else
                snapshot.EnemyHeroes.Add(state);
        }
    }

    public bool IsCardControlledByAI(Card card)
    {
        if (currentSnapshot == null || !currentSnapshot.CardStates.TryGetValue(card, out var state))
            return false;

        return state.ControllerIsMine;
    }

    public SimCardState GetSimCardState(Card card)
    {
        if (currentSnapshot == null || !currentSnapshot.CardStates.TryGetValue(card, out var state))
            return null;

        return state;
    }
}