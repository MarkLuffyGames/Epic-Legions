using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Loader
{
    static string sceneToLoad;
    public static string SceneToLoad { get { return sceneToLoad; } }
    public static int[] player1deckCardIds, player2deckCardIds;

    public static bool isSinglePlayer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeCardDatabase()
    {
        sceneToLoad = string.Empty;
        player1deckCardIds = new int[40];
        player2deckCardIds = new int[40];
        isSinglePlayer = false;
    }

    public static void LoadScene(string sceneName, bool SinglePlayer)
    {
        sceneToLoad = sceneName;
        isSinglePlayer = SinglePlayer;

        if (sceneName == "GameScene" || sceneName == "TutorialScene")
        {
            SetDecks();
            SceneManager.LoadScene("LoadingScene");
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    public static void SetDecks()
    {
        /*player1deckCardIds = new int[60];
        for (int i = 40; i < 60; i++)
        {
            player1deckCardIds[i] = CardDatabase.allCards[1036].CardID;
        }*/
        player1deckCardIds = GameData.Instance.CurrentDeck.cardsIds.ToArray();

        player2deckCardIds = GameData.Instance.CurrentDeck.cardsIds.ToArray();

        if(sceneToLoad == "TutorialScene")
        {
            player1deckCardIds = GameData.Instance.tutorialDeck1.cardsIds.ToArray();
            player2deckCardIds = GameData.Instance.tutorialDeck2.cardsIds.ToArray();
        }
    }
}
