using System.Collections.Generic;
using UnityEngine;

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