using System.Collections.Generic;
using UnityEngine;

public class GameData : MonoBehaviour
{
    public static GameData Instance;

    public DeckSO defaultDeck;
    public List<Deck> decks = new List<Deck>();
    public Deck CurrentDeck;


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        Instance = null;
    }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        LoadDecks();
    }
    public void SaveNewDeck(string deckName, List<int> cardsIndex)
    {
        var newDeck = new Deck();
        newDeck.deckName = GetDeckNameValidate(deckName, false, newDeck);
        newDeck.cardsIds = cardsIndex;
        decks.Add(newDeck);
        SaveDecksList();
    }

    public void UpdateDeck(Deck deck, string deckName, List<int> cardsIndex)
    {
        deck.deckName = GetDeckNameValidate(deckName, false, deck);
        deck.cardsIds = cardsIndex;
        SaveDecksList();
    }

    private string GetDeckNameValidate(string name, bool revalidate, Deck deckToSet)
    {
        foreach (var deck in decks)
        {
            if(deck.deckName == name && revalidate)
            {
                char lastChar = name[name.Length - 1];
                name = name.Replace(lastChar, (int.Parse(lastChar.ToString()) + 1).ToString()[0]);
                return GetDeckNameValidate(name, true, deckToSet);
            }
            if (deck.deckName == name && deck != deckToSet)
            {
                name += " 1";
                return GetDeckNameValidate(name, true, deckToSet);
            }
        }

        return name;
    }

    public void DeleteDeck(Deck deck)
    {
        decks.Remove(deck);
        SaveDecksList();
    }

    private void LoadDecks()
    {
        for (int i = 0; PlayerPrefs.HasKey("DeckName" + i); i++)
        {
            Deck deck = new Deck();
            deck.deckName = PlayerPrefs.GetString("DeckName" + i);

            for (int j = 0; PlayerPrefs.HasKey("DeckName" + i + j); j++)
            {
                deck.cardsIds.Add(PlayerPrefs.GetInt("DeckName" + i + j));
            }
            decks.Add(deck);
        }

        if (decks.Count == 0)
        {
            decks.Add(new Deck(defaultDeck));
        }

        if (PlayerPrefs.HasKey("CurrentDeck"))
        {
            int currentDeckIndex = PlayerPrefs.GetInt("CurrentDeck");
            if (currentDeckIndex >= 0 && currentDeckIndex < decks.Count)
            {
                SetCurrentDeck(decks[currentDeckIndex]);
            }
        }
        else
        {
            SetCurrentDeck(decks[0]);
        }
    }

    private void SaveDecksList()
    {
        //Eliminar decks guardados.
        for (int i = 0; PlayerPrefs.HasKey("DeckName" + i); i++)
        {
            PlayerPrefs.DeleteKey("DeckName" + i);

            for (int j = 0; PlayerPrefs.HasKey("DeckName" + i + j); j++)
            {
                PlayerPrefs.DeleteKey("DeckName" + i + j);
            }
        }

        //Guardar decks actuales.
        for (int i = 0; i < decks.Count; i++)
        {
            PlayerPrefs.SetString("DeckName" + i, decks[i].deckName);

            for (int j = 0; j < decks[i].cardsIds.Count; j++)
            {
                PlayerPrefs.SetInt("DeckName" + i + j, decks[i].cardsIds[j]);
            }
        }
    }

    public void SetCurrentDeck(Deck deckSelected)
    {
        CurrentDeck = deckSelected;
        PlayerPrefs.SetInt("CurrentDeck", decks.IndexOf(deckSelected));
        Loader.SetDecks();
    }
}
