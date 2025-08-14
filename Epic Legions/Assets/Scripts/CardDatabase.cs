using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardDatabase : MonoBehaviour
{
    public List<CardSO> AllCards;
    public static Dictionary<int, CardSO> allCards; // Lista de todas las cartas disponibles.  


    public List<HeroClassIcon> classIcons;
    private static Dictionary<HeroClass, Sprite> _iconDictionary;

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
        foreach (var item in classIcons)
        {
            if (!_iconDictionary.ContainsKey(item.heroClass))
                _iconDictionary.Add(item.heroClass, item.icon);
        }
        DontDestroyOnLoad(gameObject);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeCardDatabase()
    {
        allCards = new Dictionary<int, CardSO>();
        _iconDictionary = new Dictionary<HeroClass, Sprite>();
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

    public static int GetRandomCards()
    {
        return allCards.Values.ToArray()[Random.Range(0, allCards.Count)].CardID;
    }

    [System.Serializable]
    public class HeroClassIcon
    {
        public HeroClass heroClass;
        public Sprite icon;
    }
    public static Sprite GetIcon(HeroClass heroClass)
    {
        if (_iconDictionary.TryGetValue(heroClass, out Sprite icon))
            return icon;

        Debug.LogWarning($"No icon found for class {heroClass}");
        return null;
    }

}
