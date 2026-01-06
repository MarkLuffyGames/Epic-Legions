using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ExposureLevel
{
    None,
    Low,
    Medium,
    High
}

public enum ProtectionActionType
{
    None,
    ProtectFront,
    ProtectBack,
    ProtectBoth
}

public class CardPlacementEvaluator
{
    private bool showDebugLogs;

    // Matriz de valor de posiciones (3 filas × 5 columnas)
    private double[,] positionValues = new double[3, 5]
    {
        // Fila Delantera (0): para tanques/protectores
        { 5, 6, 8, 6, 5 },
        // Fila Media (1): balanceada
        { 7, 8, 10, 8, 7 },
        // Fila Trasera (2): para ranged/support
        { 9, 10, 12, 10, 9 }
    };

    public CardPlacementEvaluator(bool showDebugLogs = false)
    {
        this.showDebugLogs = showDebugLogs;
    }

    public (int bestPosition, double score) EvaluateBestPositionForHero(HeroCardSO hero, PlayerManager playerManager)
    {
        var fieldPositions = playerManager.GetFieldPositionList();
        var availablePositions = new List<int>();

        // Encontrar posiciones disponibles
        for (int i = 0; i < fieldPositions.Count; i++)
        {
            if (fieldPositions[i].Card == null)
                availablePositions.Add(i);
        }

        if (availablePositions.Count == 0)
            return (-1, 0);

        // Evaluar cada posición disponible
        int bestPos = availablePositions[0];
        double bestScore = 0;

        foreach (var pos in availablePositions)
        {
            double score = CalculatePositionScore(hero, pos);

            if (score > bestScore)
            {
                bestScore = score;
                bestPos = pos;
            }
        }

        if (showDebugLogs)
            Debug.Log($"Mejor posición para {hero.CardName}: {bestPos} (score: {bestScore:F1})");

        return (bestPos, bestScore);
    }

    private double CalculatePositionScore(HeroCardSO hero, int position)
    {
        int row = position / 5;
        int col = position % 5;

        double score = positionValues[row, col];

        // Ajustes según clase del héroe
        switch (hero.HeroClass)
        {
            case HeroClass.Hunter:
                // Cazadores prefieren fila trasera
                score += (row * 5); // +0 fila 0, +5 fila 1, +10 fila 2
                break;

            case HeroClass.Assassin:
                // Asesinos prefieren laterales para flanquear
                if (col == 0 || col == 4) score += 3;
                // Asesinos también pueden atacar desde atrás
                score += (2 - row) * 2; // Bonus por estar atrás
                break;

            default:
                // Otros héroes prefieren fila delantera
                score += (2 - row) * 3; // +6 fila 0, +3 fila 1, +0 fila 2
                break;
        }

        // Bonus por estar en el centro
        if (col == 2) score += 2;

        return score;
    }
}

public class HeroValueEvaluator
{
    public double EvaluateHeroValue(
        Card hero,
        PlayerManager ai,
        PlayerManager enemy)
    {
        var heroSO = hero.cardSO as HeroCardSO;
        double value = 0;

        // 🔹 Impacto por acción
        foreach (var move in heroSO.Moves)
        {
            value += move.Damage * 0.3;

            if (move.MoveEffect != null)
            {
                value += 5;

                if (move.MoveEffect is Heal) value += 8;
                if (move.MoveEffect is HeroControl) value += 12;
                if (move.MoveEffect is Recharge) value += 10;
            }
        }

        // 🔹 Eficiencia energética
        value -= heroSO.Energy * 1.2;

        // 🔹 Persistencia
        value += heroSO.Health * 0.2;

        // 🔹 Inversión (equipos ya puestos)
        foreach(var equp in hero.EquipmentCard)
        {
            if(equp != null)
                value += 10;
        }

        return value;
    }
}

public class HeroExposureEvaluator
{
    public ExposureLevel EvaluateExposure(
        Card hero,
        PlayerManager ai,
        PlayerManager enemy)
    {
        int position = hero.FieldPosition.PositionIndex;
        int row = position / 5;
        int col = position % 5;

        bool hasFront = HasHeroInFront(ai, col, row);
        bool hasBack = HasHeroBehind(ai, col, row);

        // 🔹 ¿Es targeteable?
        bool targeteable =
            (row == 0) ||
            (row == 1 && !hasFront) ||
            (row == 2 && !hasFront);

        if (!targeteable)
            return ExposureLevel.None;

        // 🔹 Riesgo base
        ExposureLevel exposure = ExposureLevel.Medium;

        if (!hasFront)
            exposure = ExposureLevel.High;

        // 🔹 Asesinos (riesgo trasero)
        if (!hasBack)
            exposure = IncreaseExposure(exposure);

        // 🔹 Riesgo elemental
        if (HasElementalDisadvantage(hero, enemy))
            exposure = IncreaseExposure(exposure);

        return exposure;
    }

    private bool HasHeroInFront(PlayerManager pm, int col, int row)
    {
        for (int r = 0; r < row; r++)
        {
            if (pm.GetFieldPositionList()[r * 5 + col].Card != null)
                return true;
        }
        return false;
    }

    private bool HasHeroBehind(PlayerManager pm, int col, int row)
    {
        for (int r = row + 1; r < 3; r++)
        {
            if (pm.GetFieldPositionList()[r * 5 + col].Card != null)
                return true;
        }
        return false;
    }

    private bool HasElementalDisadvantage(Card hero, PlayerManager enemy)
    {
        foreach (var enemyHero in enemy.GetAllCardInField())
        {
            if (CardSO.GetEffectiveness(hero.GetElement(), enemyHero.GetElement()) < 0)
            {
                return true;
            }
        }
        return false;
    }

    private ExposureLevel IncreaseExposure(ExposureLevel level)
    {
        return level switch
        {
            ExposureLevel.Low => ExposureLevel.Medium,
            ExposureLevel.Medium => ExposureLevel.High,
            _ => level
        };
    }
}

public class ProtectionDecisionMaker
{
    public ProtectionActionType DecideProtection(
        Card hero,
        ExposureLevel exposure,
        double heroValue,
        int availableEnergy)
    {
        if (exposure == ExposureLevel.None)
            return ProtectionActionType.None;

        if (heroValue > 50 && exposure == ExposureLevel.High)
        {
            if (availableEnergy >= 2)
                return ProtectionActionType.ProtectBoth;

            return ProtectionActionType.ProtectFront;
        }

        if (heroValue > 30 && exposure >= ExposureLevel.Medium)
        {
            return ProtectionActionType.ProtectFront;
        }

        return ProtectionActionType.None;
    }
}

