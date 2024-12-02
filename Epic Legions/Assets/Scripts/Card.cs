using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    [SerializeField] private Canvas canvasFront;
    [SerializeField] private Canvas canvasBack;
    [SerializeField] private Canvas cardSelected;
    [SerializeField] private Canvas cardActions;
    [SerializeField] private Image cardImage;
    [SerializeField] private TextMeshProUGUI cardName;
    [SerializeField] private TextMeshProUGUI cardAttack;
    [SerializeField] private TextMeshProUGUI cardDefence;
    [SerializeField] private TextMeshProUGUI cardSpeed;
    [SerializeField] private TextMeshProUGUI cardEnergy;
    [SerializeField] private GameObject EnergyPopUpPrefab;
    [SerializeField] private GameObject DefencePopUpPrefab;
    [SerializeField] private GameObject HitEffect;

    public CardSO cardSO;

    private Vector3 offset;
    private bool isDragging = false;
    private bool isMyTurn;
    private bool activeActions;
    private int sortingOrder;

    private Vector3 lastPosition;
    private Vector3 lastRotation;

    private bool isFocused;
    private bool isHighlight;
    public bool isTemporalPosition;

    public bool isVisible;
    public bool waitForServer;

    private int attack;
    private int defence;
    private int speed;
    private int energy;
    private int currentEnergy;
    private ulong ownerClientId;
    private FieldPosition fieldPosition; 

    public int AttackPoint => attack;
    public int DefencePoint => defence;
    public int SpeedPoint => speed;
    public int MaxEnergy => energy;
    public int CurrentEnergy => currentEnergy;
    public FieldPosition FieldPosition => fieldPosition;


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
            attack = heroCardSO.Attack;
            defence = heroCardSO.Defence;
            speed = heroCardSO.Speed;
            energy = heroCardSO.Energy;
            currentEnergy = energy;

            UpdateText();
        }
    }

    public void SetFieldPosition(FieldPosition fieldPosition)
    {
        this.fieldPosition = fieldPosition;
    }

    private void UpdateText()
    {
        cardAttack.text = attack.ToString();
        cardDefence.text = defence.ToString();
        cardSpeed.text = speed.ToString();
        cardEnergy.text = currentEnergy.ToString();
    }

    /// <summary>
    /// Mueve la carta a una posición específica
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
    /// Corrutina para mover la carta suavemente a la posición objetivo
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
    /// Rota la carta a un ángulo específico
    /// </summary>
    /// <param name="targetRotation">Angulo al que se quiere rotar la carta</param>
    /// <param name="speed">Velocidad a la que se quiere rotar la carta</param>
    public void RotateToAngle(Vector3 targetRotation, float speed)
    {
        StartCoroutine(RotateSmoothly(targetRotation, speed));
    }

    /// <summary>
    /// Corrutina para rotar la carta suavemente al ángulo objetivo
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
        // Aumenta ligeramente el tamaño de la carta.
        isHighlight = true;
        transform.localScale = new Vector3(1.05f, 1.05f, 1.05f); // Ejemplo: agrandar
        transform.localPosition += Vector3.up * 0.1f;
        ChangedSortingOrder(110);
    }

    /// <summary>
    /// Quita el resaltado de la carta
    /// </summary>
    public void RemoveHighlight()
    {
        // Vuelve la carta al tamaño original.
        if (isHighlight)
        {
            isHighlight = false;
            transform.localScale = new Vector3(1, 1, 1); // Ejemplo: volver al tamaño original
            transform.localPosition -= Vector3.up * 0.1f;
            ChangedSortingOrder(sortingOrder);
        }
    }

    /// <summary>
    /// Establece el orden de dibujado en pantalla de la carta.
    /// </summary>
    /// <param name="sortingOrder"></param>
    public void SetSortingOrder(int sortingOrder)
    {
        this.sortingOrder = sortingOrder;
        ChangedSortingOrder(sortingOrder);
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
            ChangedSortingOrder(110);
            cardSelected.enabled = false;
            cardActions.enabled = activeActions;
            isFocused = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Devuelve la carta a su tamaño y posicion original
    /// </summary>
    public void ResetSize()
    {
        MoveToLastPosition();
        RotateToAngle(lastRotation, 20);
        ChangedSortingOrder(sortingOrder);
        if(isMyTurn) cardSelected.enabled = true;
        cardActions.enabled = false;
        isFocused = false;
    }

    private void ChangedSortingOrder(int sortingOrder)
    {
        canvasFront.sortingOrder = sortingOrder;
        canvasBack.sortingOrder = sortingOrder;
        cardSelected.sortingOrder = sortingOrder;
        cardActions.sortingOrder = sortingOrder;
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
    public void StopDragging(bool returnLastPosition)
    {
        isDragging = false;
        if (!isFocused && returnLastPosition)
        {
            MoveToLastPosition();
        }
    }

    /// <summary>
    /// Actualiza la posición de la carta mientras se arrastra
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
    /// Obtiene la posición del mouse en el espacio del mundo
    /// </summary>
    /// <returns>Devuelve la posicion del mouse en pantalla</returns>
    private Vector3 GetMouseWorldPosition()
    {

        Vector3 mousePos = Vector3.zero;

        // Detectar entrada del mouse
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            mousePos = Mouse.current.position.ReadValue();
        }

        // Detectar entrada táctil
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            mousePos = Touchscreen.current.primaryTouch.position.ReadValue();
        }

        mousePos.z = Camera.main.WorldToScreenPoint(transform.position).z; // Distancia de la cámara
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

    public bool IsHighlight()
    {
        return isHighlight;
    }

    public void SetTurn(bool isPlayer)
    {
        isMyTurn = true;
        cardSelected.enabled = true;

        if(isPlayer)
        {
            activeActions = true;
        }
    }

    public void EndTurn()
    {
        isMyTurn = false;
        cardSelected.enabled = false;
        activeActions = false;
    }

    public void Attack()
    {
        DuelManager.instance.settingAttackTarget = true;
        cardActions.enabled = false;
        ResetSize();
    }

    public void AttackAnimation()
    {

    }

    /// <summary>
    /// Recibe el daño de un ataque o efecto.
    /// </summary>
    /// <param name="amountDamage"></param>
    /// <returns>Si el heroe se queda sin energia debuelve true</returns>
    public bool ReceiveDamage(int amountDamage)
    {
        Instantiate(HitEffect, transform.position, Quaternion.identity);

        if (defence >= amountDamage)
        {
            defence -= amountDamage;
            ShowTextDamage(true, amountDamage);
        }
        else if(defence < amountDamage)
        {
            if (defence > 0) ShowTextDamage(true, defence);
            defence -= amountDamage;
            currentEnergy += defence;
            ShowTextDamage(false, Mathf.Abs(defence));
            defence = 0;

            if (currentEnergy <= 0)
            {
                currentEnergy = 0;
                return true;
            }
        }

        UpdateText();

        return false;
    }

    private void ShowTextDamage(bool isDefence, int amountDamage)
    {
        var position = isDefence ? cardDefence.transform.position : cardEnergy.transform.position;
        position += Vector3.up;

        var popUp = Instantiate(isDefence ? DefencePopUpPrefab : EnergyPopUpPrefab, position, Quaternion.identity);
        popUp.GetComponentInChildren<TextMeshProUGUI>().text = $"-{amountDamage}";
    }
}