using System.Collections.Generic;
using UnityEngine;

public class CardDatabase : MonoBehaviour
{
    public List<CardSO> AllCards;
    public static Dictionary<int, CardSO> allCards; // Lista de todas las cartas disponibles.  

    // Método para buscar una carta por su ID.  
    public static CardSO GetCardById(int cardId)
    {
        allCards.TryGetValue(cardId, out CardSO card);
        return card;
    }

    private void Awake()
    {
        foreach (CardSO card in AllCards)
        {
            allCards[card.CardID] = card;
        }
        DontDestroyOnLoad(gameObject);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeCardDatabase()
    {
        allCards = new Dictionary<int, CardSO>();
    }

    public static int[] ShuffleArray(int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            // Generar un índice aleatorio  
            int randomIndex = Random.Range(0, i + 1);

            // Intercambiar el elemento actual con el elemento aleatorio  
            int temp = array[i];
            array[i] = array[randomIndex];
            array[randomIndex] = temp;
        }

        return array;
    }
}
