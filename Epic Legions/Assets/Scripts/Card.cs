using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    [SerializeField] private Canvas canvasFront;
    [SerializeField] private Canvas canvasBack;
    [SerializeField] private Image cardImage;
    [SerializeField] private TextMeshProUGUI cardName;
    [SerializeField] private TextMeshProUGUI cardAttack;
    [SerializeField] private TextMeshProUGUI cardDefence;
    [SerializeField] private TextMeshProUGUI cardSpeed;
    [SerializeField] private TextMeshProUGUI cardEnergy;

    public CardSO cardSO;

    private Vector3 offset;
    private bool isDragging = false;

    private int sortingOrder;

    private Vector3 lastPosition;
    private Vector3 lastRotation;

    private bool isFocused;
    public bool isTemporalPosition;

    public bool isVisible;

    /// <summary>
    /// Establece todos los datos de la carta.
    /// </summary>
    /// <param name="cardSO">Scriptable Object que contiene los datos de la carta que se desea establecer</param>
    public void SetCard(CardSO cardSO)
    {
        this.cardSO = cardSO;

        cardName.text = cardSO.CardName;
        cardImage.sprite = cardSO.CardSprite;
        if(cardSO is HeroCardSO heroCardSO)
        {
            cardAttack.text = heroCardSO.Attack.ToString();
            cardDefence.text = heroCardSO.Defence.ToString();
            cardSpeed.text = heroCardSO.Speed.ToString();
            cardEnergy.text = heroCardSO.Energy.ToString();
        }
    }

    /// <summary>
    /// Mueve la carta a una posici�n espec�fica
    /// </summary>
    /// <param name="targetPosition">Posicion a la que se desea mover la carta</param>
    /// <param name="speed">Velocidad a la que se va a mover la carta</param>
    /// <param name="temporalPosition">La posicion a la que se va a mover la carta es temporal?</param>
    /// <param name="isLocal">La posicion que se desea mover es la posicion local?</param>
    public void MoveToPosition(Vector3 targetPosition, float speed, bool temporalPosition, bool isLocal)
    {
        StopAllCoroutines();
        StartCoroutine(MoveSmoothly(targetPosition, speed, temporalPosition, isLocal));
    }
    /// <summary>
    /// Corrutina para mover la carta suavemente a la posici�n objetivo
    /// </summary>
    private IEnumerator MoveSmoothly(Vector3 targetPosition, float speed, bool temporalPosition, bool isLocal)
    {
        isTemporalPosition = temporalPosition;

        if (!isDragging || isTemporalPosition)
        {
            if (!temporalPosition)
            {
                lastPosition = targetPosition;
            }

            while (Vector3.Distance(isLocal ? transform.localPosition : transform.position, targetPosition) > 0.01f)
            {
                if (isLocal)
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, speed * Time.deltaTime);
                }
                else
                {
                    transform.position = Vector3.Lerp(transform.position, targetPosition, speed * Time.deltaTime);
                }
                yield return null;
            }

            if (isLocal)
            {
                transform.localPosition = targetPosition;
            }
            else
            {
                transform.position = targetPosition;
            }
        }
        
    }

    /// <summary>
    /// Mueve la carta a su posicion anterior.
    /// </summary>
    public void MoveToLastPosition()
    {
        MoveToPosition(lastPosition, 20, false, true);
    }

    /// <summary>
    /// Rota la carta a un �ngulo espec�fico
    /// </summary>
    /// <param name="targetRotation">Angulo al que se quiere rotar la carta</param>
    /// <param name="speed">Velocidad a la que se quiere rotar la carta</param>
    public void RotateToAngle(Vector3 targetRotation, float speed)
    {
        StartCoroutine(RotateSmoothly(targetRotation, speed));
    }

    /// <summary>
    /// Corrutina para rotar la carta suavemente al �ngulo objetivo
    /// </summary>
    private IEnumerator RotateSmoothly(Vector3 targetRotation, float speed)
    {

        lastRotation = transform.eulerAngles;

        Quaternion targetQuaternion = Quaternion.Euler(targetRotation);
        while (Quaternion.Angle(transform.rotation, targetQuaternion) > 0.1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetQuaternion, speed * Time.deltaTime);
            yield return null;
        }
        transform.rotation = targetQuaternion;
    }

    /// <summary>
    /// Resalta la carta
    /// </summary>
    public void Highlight()
    {
        // Aumenta ligeramente el tama�o de la carta.
        transform.localScale = new Vector3(1.05f, 1.05f, 1.05f); // Ejemplo: agrandar
        transform.localPosition += Vector3.up * 0.1f;
        canvasFront.sortingOrder = 110;
        canvasBack.sortingOrder = 110;
    }

    /// <summary>
    /// Quita el resaltado de la carta
    /// </summary>
    public void RemoveHighlight()
    {
        // Vuelve la carta al tama�o original.
        transform.localScale = new Vector3(1, 1, 1); // Ejemplo: volver al tama�o original
        transform.localPosition -= Vector3.up * 0.1f;
        canvasFront.sortingOrder = sortingOrder;
        canvasBack.sortingOrder = sortingOrder;
    }

    /// <summary>
    /// Establece el orden de dibujado en pantalla de la carta.
    /// </summary>
    /// <param name="sortingOrder"></param>
    public void SetSortingOrder(int sortingOrder)
    {
        this.sortingOrder = sortingOrder;
        canvasFront.sortingOrder = sortingOrder;
        canvasBack.sortingOrder = sortingOrder;
    }

    /// <summary>
    /// Agranda la carta para verla mejor
    /// </summary>
    /// <returns>Retorna true si la carta fue agrandada correctamente.</returns>
    public bool Enlarge()
    {
        if (Vector3.Distance(lastPosition, transform.localPosition) < 0.2f)
        {
            MoveToPosition(new Vector3(0, 9.18f, -4.3f), 20, true, false);
            RotateToAngle(Vector3.right * 70, 20);
            canvasFront.sortingOrder = 110;
            canvasBack.sortingOrder = 110;
            isFocused = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Devuelve la carta a su tama�o y posicion original
    /// </summary>
    public void ResetSize()
    {
        MoveToPosition(lastPosition, 20, false, true);
        RotateToAngle(lastRotation, 20);
        canvasFront.sortingOrder = sortingOrder;
        canvasBack.sortingOrder = sortingOrder;
        isFocused = false;
    }

    /// <summary>
    /// Inicia el arrastre de la carta
    /// </summary>
    public void StartDragging()
    {
        if(isDragging)return;

        isDragging = true;
        offset = transform.position - GetMouseWorldPosition();
    }

    /// <summary>
    /// Termina el arrastre de la carta
    /// </summary>
    public void StopDragging()
    {
        isDragging = false;
        if (!isFocused)
        {
            MoveToPosition(lastPosition, 20, false, true);
        }
    }

    /// <summary>
    /// Actualiza la posici�n de la carta mientras se arrastra
    /// </summary>
    private void Update()
    {
        if (isDragging && !isTemporalPosition)
        {
            RotateToAngle(new Vector3(70, 0, 0), 20);
            Vector3 newPosition = GetMouseWorldPosition() + offset;
            newPosition = new Vector3(newPosition.x, 3.7f, newPosition.z);

            while (Vector3.Distance(transform.position, newPosition) > 0.01f)
            {
                transform.position = Vector3.Lerp(transform.position, newPosition, 20 * Time.deltaTime);
                return;
            }

            transform.position = newPosition;
        }
    }

    /// <summary>
    /// Obtiene la posici�n del mouse en el espacio del mundo
    /// </summary>
    /// <returns>Devuelve la posicion del mouse en pantalla</returns>
    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Camera.main.WorldToScreenPoint(transform.position).z; // Distancia de la c�mara
        return Camera.main.ScreenToWorldPoint(mousePos);
    }

    /// <summary>
    /// Metodo para consultar si la carta esta siendo arrastrada
    /// </summary>
    /// <returns>Devuelve true si la carta esta siendo arrastrada</returns>
    public bool IsDragging()
    {
        return isDragging;
    }
}