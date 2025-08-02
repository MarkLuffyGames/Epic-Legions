using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Loader
{
    public static string sceneToLoad = string.Empty;
    public static int[] player1deckCardIds, player2deckCardIds;

    public static bool isSinglePlayer = false;
}
