using UnityEngine;

public class FieldPosition : MonoBehaviour
{
    private Card card;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    [SerializeField] private Color isFreeColor;

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
    public void SetCard(Card card)
    {
        this.card = card;
        card.isVisible = true;
        card.transform.parent = transform;
        card.transform.localScale = Vector3.one;
        card.MoveToPosition(Vector3.up * 0.01f, 20, false, true);
        card.RotateToAngle(new Vector3(90, 0, 0), 20);
        card.SetSortingOrder(0);
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