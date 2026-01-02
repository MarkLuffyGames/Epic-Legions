using UnityEngine;

public class RecordCards : MonoBehaviour
{
    public Card cardToRecord;
    public CardDatabase cardDatabase;

    private int i = 0;
    public void RecordCard()
    {
        if (cardToRecord != null && cardDatabase != null)
        {
            cardToRecord.SetNewCard(cardDatabase.AllCards[i], null);
            i++;
            if(i >= cardDatabase.AllCards.Count)
            {
                i = 0;
            }
        }
        else
        {
            Debug.LogWarning("Card to record or Card Database is not assigned.");
        }
    }

    private void Update()
    {
        RecordCard();
    }
}
