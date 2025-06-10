using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FieldPosition : MonoBehaviour
{
    private Card card;
    [SerializeField] private Color originalColor;
    [SerializeField] private Color isFreeColor;
    [SerializeField] private Color isbusyColor;
    [SerializeField] private Color turnColor;
    [SerializeField] private Color playerTurnColor;
    [SerializeField] private int positionIndex;
    [SerializeField] private Renderer objRenderer;
    [SerializeField] private float intensity = 5;
    private MaterialPropertyBlock propertyBlock;

    public Color IsbusyColor => isbusyColor;
    public Color TurnColor => turnColor;
    public Color PlayerTurnColor => playerTurnColor;
    public float Intensity => intensity;
    public Card Card => card;
    public int PositionIndex => positionIndex;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();

        RestoreOriginalColor(0);
    }

    /// <summary>
    /// Metodo para saber si esta posicion no esta ocupada por un heroe.
    /// </summary>
    /// <returns></returns>
    public bool IsFree(CardSO cardSO)
    {
        if(cardSO is HeroCardSO)
        {
            return card == null && positionIndex != -1;
        }
        else if (cardSO is SpellCardSO)
        {
            return card == null && positionIndex == -1;
        }
        else if (cardSO is EquipmentCardSO equipmentCard)
        {
            return card != null && IsAvailableEquipmentSlot(equipmentCard);
        }

        return false;
    }

    public bool IsAvailableEquipmentSlot(EquipmentCardSO equipmentCard)
    {
        bool isAvailable = true;

        HeroCardSO HCSO = card.cardSO as HeroCardSO;
        if (!equipmentCard.SupportedClasses.Contains(HCSO.HeroClass)) // Si la clase del heroe no es compatible con el equipo, no se puede añadir
        {
            isAvailable = false;
        }

        // Si la carta ya tiene un equipo de este tipo, no se puede añadir otro
        if (isAvailable)
        {
            foreach (var item in card.EquipmentCard)
            {
                if (item != null && item.cardSO is EquipmentCardSO equipmentCardSO)
                {
                    if (equipmentCardSO.EquipmentType == equipmentCard.EquipmentType)
                    {
                        isAvailable = false;
                    }
                }
            }
        }

        return isAvailable;
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
        card.RemoveHighlight();
        StartCoroutine(card.MoveToPosition(Vector3.back * 0.01f, Card.cardMovementSpeed, false, true));
        card.RotateToAngle(new Vector3(90, 0, isPlayer? 0 : 0), Card.cardMovementSpeed, false);
        card.SetSortingOrder(0);
        card.SetFieldPosition(this, new Vector3(90, 0, isPlayer ? 0 : 0));
        ChangeEmission(isbusyColor, intensity);
    }

    public void DestroyCard(Graveyard graveyard, bool isPlayer)
    {
        card.isVisible = true;
        card.transform.parent = graveyard.gameObject.transform;
        card.transform.localScale = Vector3.one;
        card.RotateToAngle(new Vector3(90, 0, isPlayer ? 0 : 0), Card.cardMovementSpeed, false);
        card.SetSortingOrder(0);
        card.ToGraveyard(graveyard);
        card.SetFieldPosition(null, new Vector3(90, 0, isPlayer ? 0 : 0));
        card = null;
        RestoreOriginalColor();
    }

    /// <summary>
    /// Resalta esta posicion si esta libre para indicar al jugador que puede colocar la carta aqui.
    /// </summary>
    public void Highlight(CardSO cardSO)
    {
        if(IsFree(cardSO))
        {
            ChangeEmission(isFreeColor, intensity);
        }
    }

    /// <summary>
    /// Quita el resaltado de esta posicion del campo.
    /// </summary>
    public void RemoveHighlight()
    {
        if(card == null) { 
            RestoreOriginalColor();
        }
        else
        {
            if(card.isMyTurn)
                ChangeEmission(card.isPlayer ? PlayerTurnColor : TurnColor);
            else
                ChangeEmission(isbusyColor, intensity);
        }
    }

    private Coroutine currentCoroutine; // Para controlar la interpolación en curso

    public void ChangeEmission(Color nuevoColor, float intensidad = 3, float duracion = 0.3f)
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine); // Detener cualquier cambio en curso

        currentCoroutine = StartCoroutine(LerpEmision(nuevoColor, intensidad, duracion));
    }

    public void RestoreOriginalColor(float duracion = 0.3f)
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        if (card != null && card.isMyTurn)
        {
            ChangeEmission(card.isPlayer ? PlayerTurnColor : TurnColor);
        }
        else
        {
            currentCoroutine = StartCoroutine(LerpEmision(card == null ? originalColor : isbusyColor * Mathf.Pow(2, intensity), 0, duracion)); // Volver al color original
        }
    }

    private IEnumerator LerpEmision(Color targetColor, float intensidad, float duracion)
    {
        objRenderer.GetPropertyBlock(propertyBlock);
        Color startColor = propertyBlock.GetColor("_EmissionColor");

        float tiempo = 0f;
        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            Color colorInterpolado = Color.Lerp(startColor, targetColor * Mathf.Pow(2, intensidad), tiempo / duracion);
            propertyBlock.SetColor("_EmissionColor", colorInterpolado);
            objRenderer.SetPropertyBlock(propertyBlock);

            yield return null; // Esperar al siguiente frame
        }

        // Asegurar que el color final sea exactamente el deseado
        propertyBlock.SetColor("_EmissionColor", targetColor * Mathf.Pow(2, intensidad));
        objRenderer.SetPropertyBlock(propertyBlock);
    }
}
