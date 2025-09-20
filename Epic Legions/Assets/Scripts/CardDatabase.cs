using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardDatabase : MonoBehaviour
{
    public static CardDatabase Instance;

    public List<CardSO> AllCards;
    public static Dictionary<int, CardSO> allCards; // Lista de todas las cartas disponibles.  


    public List<HeroClassIcon> classIcons;
    public List<HeroElementIcon> elementIcons;
    public List<MoveTypeIcon> moveTypeIcons;
    private static Dictionary<HeroClass, Sprite> _iconClassDictionary;
    private static Dictionary<CardElement, Sprite> _iconElementDictionary;
    private static Dictionary<MoveType, Sprite> _iconMoveTypeDictionary;

    // Método para buscar una carta por su ID.  
    public static CardSO GetCardById(int cardId)
    {
        allCards.TryGetValue(cardId, out CardSO card);
        return card;
    }

    private void Awake()
    {
        if( Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        foreach (CardSO card in AllCards)
        {
            allCards[card.CardID] = card;
        }
        foreach (var item in classIcons)
        {
            if (!_iconClassDictionary.ContainsKey(item.heroClass))
                _iconClassDictionary.Add(item.heroClass, item.icon);
        }
        foreach (var item in elementIcons)
        {
            if (!_iconElementDictionary.ContainsKey(item.heroElement))
                _iconElementDictionary.Add(item.heroElement, item.icon);
        }
        foreach (var item in moveTypeIcons)
        {
            if (!_iconMoveTypeDictionary.ContainsKey(item.moveType))
                _iconMoveTypeDictionary.Add(item.moveType, item.icon);
        }
        DontDestroyOnLoad(gameObject);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeCardDatabase()
    {
        Instance = null;
        allCards = new Dictionary<int, CardSO>();
        _iconClassDictionary = new Dictionary<HeroClass, Sprite>();
        _iconElementDictionary = new Dictionary<CardElement, Sprite>();
        _iconMoveTypeDictionary = new Dictionary<MoveType, Sprite>();
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

    [System.Serializable]
    public class HeroElementIcon
    {
        public CardElement heroElement;
        public Sprite icon;
    }

    [System.Serializable]
    public class MoveTypeIcon
    {
        public MoveType moveType;
        public Sprite icon;
    }
    public static Sprite GetClassIcon(HeroClass heroClass)
    {
        if (_iconClassDictionary.TryGetValue(heroClass, out Sprite icon))
            return icon;

        Debug.LogWarning($"No icon found for class {heroClass}");
        return null;
    }

    public static Sprite GetElementIcon(CardElement heroElement)
    {
        if (_iconElementDictionary.TryGetValue(heroElement, out Sprite icon))
            return icon;

        Debug.LogWarning($"No icon found for class {heroElement}");
        return null;
    }

    public static Sprite GetMoveTypeIcon(MoveType moveType)
    {
        if (_iconMoveTypeDictionary.TryGetValue(moveType, out Sprite icon))
            return icon;
        Debug.LogWarning($"No icon found for class {moveType}");
        return null;
    }
}
