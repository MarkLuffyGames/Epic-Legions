using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Loader
{
    public static string sceneToLoad;
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

    public static void SetDecks()
    {
        /*for (int i = 0; i < 20; i++)
        {
            player1deckCardIds[i] = CardDatabase.allCards[1081].CardID;
        }*/
        for (int i = 0; i < 40; i++)
        {
            player1deckCardIds[i] = CardDatabase.GetRandomCards();
        }
        for (int i = 0; i < 40; i++)
        {
            player2deckCardIds[i] = CardDatabase.GetRandomCards();
        }
    }
}
