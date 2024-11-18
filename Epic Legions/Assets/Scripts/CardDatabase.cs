using System.Collections.Generic;
using UnityEngine;

public class CardDatabase : MonoBehaviour
{
    public List<CardSO> AllCards;
    public static List<CardSO> allCards; // Lista de todas las cartas disponibles.

    // Método para buscar una carta por su ID.
    public static CardSO GetCardById(int cardId)
    {
        return allCards.Find(card => card.CardID == cardId);
    }

    private void Awake()
    {
        allCards = AllCards;
    }

    public static int[] ShuffleArray(int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            // Generar un índice aleatorio con UnityEngine.Random
            int randomIndex = Random.Range(0, i + 1);

            // Intercambiar el elemento actual con el elemento aleatorio
            int temp = array[i];
            array[i] = array[randomIndex];
            array[randomIndex] = temp;
        }

        return array;
    }
}
