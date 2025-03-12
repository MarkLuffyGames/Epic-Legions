using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEditor.PackageManager;
using UnityEngine;

public class DuelManagerAI : MonoBehaviour
{
    private DuelPhase duelPhase = DuelPhase.None;

    [SerializeField] private PlayerManager player1Manager;
    [SerializeField] private PlayerManager player2Manager;
    [SerializeField] private List<int> deckCardIds;

    private Dictionary<int, List<int>> playerDecks = new Dictionary<int, List<int>>();
    public void InitializeDuel()
    {
        // Establece la fase del duelo a "Preparando el Duelo"
        duelPhase = DuelPhase.PreparingDuel;

        SetDeck(player1Manager, deckCardIds.ToArray());
        SetDeck(player2Manager, deckCardIds.ToArray());
    }

    private void SetDeck(PlayerManager playerManager, int[] deckCardIds)
    {
        // Validar si el mazo es válido
        if (deckCardIds == null || deckCardIds.Length == 0)
        {
            Debug.LogWarning($"Player {playerManager} sent an invalid deck.");
            return;
        }

        deckCardIds = CardDatabase.ShuffleArray(deckCardIds); // Barajar el mazo

        playerDecks[playerManager == player1Manager ? 0 : 1] = deckCardIds.ToList(); // Guardar en la lista de mazos

        foreach (var cardId in deckCardIds)
        {
            var card = CardDatabase.GetCardById(cardId);

            if (card != null)
            {
                // Asigna las cartas al mazo del jugador correspondiente
                playerManager.AddCardToPlayerDeck(card, deckCardIds.Length);
            }
            else
            {
                Debug.LogWarning($"Player {playerManager} tried to add an invalid card ID {cardId} to their deck.");
            }
        }
    }
}
