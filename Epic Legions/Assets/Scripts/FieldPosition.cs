using System.Collections.Generic;
using UnityEngine;

public class FieldPosition : MonoBehaviour
{
    private Card card;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    [SerializeField] private Color isFreeColor;
    [SerializeField] private int positionIndex;

    public Card Card => card;
    public int PositionIndex => positionIndex;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    /// <summary>
    /// Metodo para saber si esta posicion no esta ocupada por un heroe.
    /// </summary>
    /// <returns></returns>
    public bool IsFree()
   {
        return card == null;
   }

    /// <summary>
    /// Establece la carta en esta pocion del campo.
    /// </summary>
    /// <param name="card"></param>
    public void SetCard(Card card, bool isPlayer)
    {
        this.card = card;
        card.isVisible = true;
        card.transform.parent = transform;
        card.transform.localScale = Vector3.one;
        StartCoroutine(card.MoveToPosition(Vector3.back * 0.01f, Card.cardMovementSpeed, false, true));
        card.RotateToAngle(new Vector3(90, 0, isPlayer? 0 : 180), Card.cardMovementSpeed, false);
        card.SetSortingOrder(0);
        card.SetFieldPosition(this);
    }

    public void DestroyCard(Transform graveyard, bool isPlayer)
    {
        card.isVisible = true;
        card.transform.parent = graveyard;
        card.transform.localScale = Vector3.one;
        StartCoroutine(card.MoveToPosition(Vector3.up * 0.01f, Card.cardMovementSpeed, false, true));
        card.RotateToAngle(new Vector3(90, 0, isPlayer ? 0 : 180), Card.cardMovementSpeed, false);
        card.SetSortingOrder(0);
        card.ToGraveyard();
        card.SetFieldPosition(null);
        card = null;
    }

    /// <summary>
    /// Resalta esta posicion si esta libre para indicar al jugador que puede colocar la carta aqui.
    /// </summary>
    public void Highlight()
    {
        if(IsFree())
        {
            spriteRenderer.color = isFreeColor;
        }
    }

    /// <summary>
    /// Quita el resaltado de esta posicion del campo.
    /// </summary>
    public void RemoveHighlight()
    {
        spriteRenderer.color = originalColor;
    }
}
