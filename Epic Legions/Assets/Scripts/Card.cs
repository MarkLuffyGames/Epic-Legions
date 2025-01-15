using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
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
    [SerializeField] private Image cardSelectedImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI healtText;
    [SerializeField] private TextMeshProUGUI defenceText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI energyText;
    [SerializeField] private TextMeshProUGUI move1NameText;
    [SerializeField] private TextMeshProUGUI move1DescriptionText;
    [SerializeField] private TextMeshProUGUI move2NameText;
    [SerializeField] private TextMeshProUGUI move2DescriptionText;
    [SerializeField] private GameObject energyPopUpPrefab;
    [SerializeField] private GameObject defencePopUpPrefab;
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private GameObject stunEffect;
    [SerializeField] private GameObject regenerateDefenseEffect;

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

    private int healt;
    private int defense;
    private int speed;
    private int energy;

    private int currentHealt;
    private int currentDefense;
    private int currentSpeed;

    private List<Movement> moves;
    private FieldPosition fieldPosition;
    public bool isAttackable;

    public int CurrentHealtPoints => currentHealt;
    public int CurrentDefensePoints => currentDefense + GetDefenseModifier();
    public int CurrentSpeedPoints => currentSpeed;
    public List<Movement> Moves => moves;
    public FieldPosition FieldPosition => fieldPosition;

    public int stunned { get; private set; }

    /// <summary>
    /// Establece todos los datos de la carta.
    /// </summary>
    /// <param name="cardSO">Scriptable Object que contiene los datos de la carta que se desea establecer</param>
    public void SetCard(CardSO cardSO)
    {
        this.cardSO = cardSO;

        nameText.text = cardSO.CardName;
        cardImage.sprite = cardSO.CardSprite;
        if(cardSO is HeroCardSO heroCardSO)
        {
            healt = heroCardSO.Healt;
            defense = heroCardSO.Defence;
            speed = heroCardSO.Speed;
            energy = heroCardSO.Energy;
            
            moves = new List<Movement>();
            foreach(var move in heroCardSO.Moves)
            {
                moves.Add(new Movement(move));
            }

            currentHealt = healt;
            currentDefense = defense;
            currentSpeed = speed;

            if (moves[1] != null)
            {
                move1NameText.text = moves[0].MoveSO.MoveName;
                move1DescriptionText.text = moves[0].MoveSO.EffectDescription;
            }
            if (moves[1] != null)
            {
                move2NameText.text = moves[1].MoveSO.MoveName;
                move2DescriptionText.text = moves[1].MoveSO.EffectDescription;
            }

            UpdateText();
        }
    }

    public void SetFieldPosition(FieldPosition fieldPosition)
    {
        this.fieldPosition = fieldPosition;
    }

    public void UpdateText()
    {
        healtText.text = currentHealt.ToString();
        defenceText.text = CurrentDefensePoints.ToString();
        speedText.text = currentSpeed.ToString();
        energyText.text = energy.ToString();
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
        // Vuelve la carta al tama�o original.
        if (isHighlight)
        {
            isHighlight = false;
            transform.localScale = new Vector3(1, 1, 1); // Ejemplo: volver al tama�o original
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
    /// Devuelve la carta a su tama�o y posicion original
    /// </summary>
    public void ResetSize()
    {
        if (isFocused)
        {
            MoveToLastPosition();
            RotateToAngle(lastRotation, 20);
            ChangedSortingOrder(sortingOrder);
            if (isMyTurn) cardSelected.enabled = true;
            cardActions.enabled = false;
            isFocused = false;
        }
    }

    private void ChangedSortingOrder(int sortingOrder)
    {
        canvasFront.sortingOrder = sortingOrder;
        canvasBack.sortingOrder = sortingOrder;
        cardSelected.sortingOrder = sortingOrder;
        cardActions.sortingOrder = sortingOrder + 1;
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

        Vector3 mousePos = Vector3.zero;

        // Detectar entrada del mouse
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            mousePos = Mouse.current.position.ReadValue();
        }

        // Detectar entrada t�ctil
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            mousePos = Touchscreen.current.primaryTouch.position.ReadValue();
        }

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

    public bool IsHighlight()
    {
        return isHighlight;
    }

    public bool SetTurn(bool isPlayer)
    {
        isMyTurn = true;
        cardSelected.enabled = true;
        cardSelectedImage.color = Color.yellow;

        if (isPlayer)
        {
            activeActions = true;
        }

        return true;
    }

    public void EndTurn()
    {
        isMyTurn = false;
        cardSelected.enabled = false;
        activeActions = false;
    }

    public void UseMovement(int movementNumber)
    {
        DuelManager.instance.UseMovement(movementNumber);
        cardActions.enabled = false;
        ResetSize();
    }

    public void ActiveAttackableTarget()
    {
        cardSelected.enabled = true;
        cardSelectedImage.color = Color.red;
        isAttackable = true;
    }

    public void DesactiveAttackableTarget()
    {
        cardSelected.enabled = false;
        isAttackable = false;
    }

    public void AttackAnimation()
    {

    }

    public void AnimationReceivingMovement(GameObject visualEffect)
    {
        //Efecto de da�o.
        Instantiate(visualEffect, fieldPosition.transform.position, Quaternion.identity);
    }

    /// <summary>
    /// Recibe el da�o de un ataque o efecto.
    /// </summary>
    /// <param name="amountDamage"></param>
    /// <returns>Si el heroe se queda sin energia debuelve true</returns>
    public bool ReceiveDamage(int amountDamage)
    {
        int remainingDamage = ReceiveDamageToShield(amountDamage);
        
        if(remainingDamage > 0)
        {
            ShowTextDamage(false, currentHealt > remainingDamage ? remainingDamage : currentHealt);
            currentHealt -= remainingDamage;

            if (currentHealt <= 0)
            {
                currentDefense = defense;
                currentHealt = healt;
                currentSpeed = speed;
                UpdateText();
                CancelAllEffects();
                return true;
            }
        }

        UpdateText();

        return false;
    }

    private void ShowTextDamage(bool isDefence, int amountDamage)
    {
        var position = isDefence ? defenceText.transform.position : healtText.transform.position;
        position += Vector3.up;

        var popUp = Instantiate(isDefence ? defencePopUpPrefab : energyPopUpPrefab, position, Quaternion.identity);
        popUp.GetComponentInChildren<TextMeshProUGUI>().text = $"-{amountDamage}";
    }

    
    public void ToggleStunned()
    {
        if (stunned == 0) 
        {
            stunned = 2;
            stunEffect.SetActive(true); 
        }
        else
        {
            stunned -= 1;
        }
    }

    /// <summary>
    /// Maneja los efectos que tenga activo la carta y debuelve true si el heroe pierde el turno por algun efecto.
    /// </summary>
    /// <returns></returns>
    public bool HandlingStatusEffects()
    {
        if (stunned > 0)
        {
            ToggleStunned();
            if (stunned == 0) 
            {
                stunEffect.SetActive(false);
                return false;
            }
            return true;
        }

        return false;
    }

    public void ManageEffects()
    {
        foreach (var move in moves)
        {
            if (move.EffectIsActive())
            {
                var modifier = move.UpdateEffect();
            }
        }
    }

    private void CancelAllEffects()
    {
        stunned = 0;
        stunEffect.SetActive(false);
        fieldPosition.statModifier.Clear();
    }

    public void RegenerateDefense()
    {
        if(currentDefense < defense)
        {
            currentDefense = defense;
            foreach (StatModifier statModifier in fieldPosition.statModifier)
            {
                statModifier.currentDefense = statModifier.defense;
            }
            Instantiate(regenerateDefenseEffect, transform.position, Quaternion.identity);
            UpdateText();   
        }
    }

    public void AddModifier(StatModifier statModifier)
    {
        fieldPosition.statModifier.Add(statModifier);
        UpdateText();
    }

    private int GetDefenseModifier()
    {
        int defenceModifier = 0;
        if(fieldPosition == null) return defenceModifier;

        foreach(StatModifier statModifier in fieldPosition.statModifier)
        {
            defenceModifier += statModifier.currentDefense;
        }

        return defenceModifier;
    }

    private int ReceiveDamageToShield(int damage)
    {
        if(CurrentDefensePoints <= damage)
        {
            if(CurrentDefensePoints != 0)ShowTextDamage(true, CurrentDefensePoints);

            var remainingDamage = CurrentDefensePoints - damage;
            currentDefense = 0;
            foreach (StatModifier statModifier in fieldPosition.statModifier)
            {
                if(statModifier.currentDefense > 0)
                {
                    statModifier.currentDefense = 0;
                }
            }
            return Mathf.Abs(remainingDamage);
        }
        else
        {
            ShowTextDamage(true, damage);
            var remainingDamage = damage - currentDefense;
            currentDefense -= damage;
            if(currentDefense < 0) currentDefense = 0;
            damage = remainingDamage;

            if (damage <= 0) 
            {
                return 0; 
            }
            else
            {
                foreach (StatModifier statModifier in fieldPosition.statModifier)
                {
                    if(statModifier.currentDefense > 0)
                    {
                        remainingDamage = damage - statModifier.currentDefense;
                        statModifier.currentDefense -= damage;
                        if (statModifier.currentDefense < 0) statModifier.currentDefense = 0;
                        damage = remainingDamage;
                        if (damage <= 0) return 0;
                    }
                }
            }

        }

        return 0;   
    }
}

