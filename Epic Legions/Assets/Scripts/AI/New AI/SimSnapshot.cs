// Nuevas clases para la simulación
using System.Collections.Generic;

public class SimSnapshot
{
    public Dictionary<Card, SimCardState> CardStates = new();
    public List<SimCardState> MyControlledHeroes = new();
    public List<SimCardState> EnemyHeroes = new();

    public int MyLife;
    public int EnemyLife;
    public int MyEnergy;
    public int EnemyEnergy;
    public int CurrentSubTurn;                    // Subturno absoluto (0-20)
    public int OriginalSubTurn;                   // Subturno en el que comenzó la simulación
    public int SubTurnsPassedInSimulation;        // Subturnos transcurridos en esta simulación

    public SimSnapshot Clone()
    {
        var clone = new SimSnapshot
        {
            MyLife = this.MyLife,
            EnemyLife = this.EnemyLife,
            MyEnergy = this.MyEnergy,
            EnemyEnergy = this.EnemyEnergy,
            CurrentSubTurn = this.CurrentSubTurn,
            OriginalSubTurn = this.OriginalSubTurn,
            SubTurnsPassedInSimulation = this.SubTurnsPassedInSimulation,
        };

        foreach (var kvp in this.CardStates)
        {
            clone.CardStates[kvp.Key] = kvp.Value.Clone(clone);
        }

        // Reconstruir listas de héroes
        foreach (var state in clone.CardStates.Values)
        {
            if (state.ControllerIsMine && state.Alive)
                clone.MyControlledHeroes.Add(state);
            else if (!state.ControllerIsMine && state.Alive)
                clone.EnemyHeroes.Add(state);
        }

        return clone;
    }
}

public class SimTemporalEffect
{
    public string EffectType;         // "Stun", "Burn", "Control", etc.
    public int Duration;              // Subturnos restantes

    public bool IsExpired(int SubTurnsPassedInSimulation)
    {
        return SubTurnsPassedInSimulation >= Duration;
    }
}