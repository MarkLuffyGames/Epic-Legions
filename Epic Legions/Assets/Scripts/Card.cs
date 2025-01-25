using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private Image cardSelectedImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI lastNameText;
    [SerializeField] private TextMeshProUGUI healtText;
    [SerializeField] private TextMeshProUGUI defenceText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI energyText;
    [SerializeField] private TextMeshProUGUI move1NameText;
    [SerializeField] private TextMeshProUGUI move1DescriptionText;
    [SerializeField] private TextMeshProUGUI move2NameText;
    [SerializeField] private TextMeshProUGUI move2DescriptionText;
    [SerializeField] private TextMeshProUGUI description;
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

    private List<Movement> moves = new List<Movement>();
    private FieldPosition fieldPosition;
    public bool isAttackable;

    public int HealtPoint => healt;
    public int CurrentHealtPoints => currentHealt;
    public int CurrentDefensePoints => currentDefense + GetDefenseModifier();
    public int CurrentSpeedPoints => speed + GetSpeedModifier();
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
        lastNameText.text = cardSO.CardLastName;
        cardImage.sprite = cardSO.CardSprite;
        if(cardSO is HeroCardSO heroCardSO)
        {
            healt = heroCardSO.Healt;
            defense = heroCardSO.Defence;
            speed = heroCardSO.Speed;
            energy = heroCardSO.Energy;
            
            foreach(var move in heroCardSO.Moves)
            {
                moves.Add(new Movement(move));
            }

            currentHealt = healt;
            currentDefense = defense;

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
        else if (cardSO is SpellCardSO spellCardSO)
        {
            moves.Add(new Movement(spellCardSO.Move));
            description.text = spellCardSO.Move.EffectDescription;
        }
    }

    public void SetFieldPosition(FieldPosition fieldPosition)
    {
        this.fieldPosition = fieldPosition;
        AdjustUIcons();
    }

    public void UpdateText()
    {
        healtText.text = currentHealt.ToString();
        defenceText.text = CurrentDefensePoints.ToString();
        speedText.text = CurrentSpeedPoints.ToString();
        energyText.text = energy.ToString();
    }

    /// <summary>
    /// Mueve la carta a una posición específica
    /// </summary>
    /// <param name="targetPosition">Posicion a la que se desea mover la carta</param>
    /// <param name="speed">Velocidad a la que se va a mover la carta</param>
    /// <param name="temporalPosition">La posicion a la que se va a mover la carta es temporal?</param>
    /// <param name="isLocal">La posicion que se desea mover es la posicion local?</param>
    public IEnumerator MoveToPosition(Vector3 targetPosition, float speed, bool temporalPosition, bool isLocal)
    {
        StopAllCoroutines();
        yield return StartCoroutine(MoveSmoothly(targetPosition, speed, temporalPosition, isLocal));
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
        StartCoroutine(MoveToPosition(lastPosition, 20, false, true));
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
            StartCoroutine(MoveToPosition(new Vector3(0, 9.18f, -4.3f), 20, true, false));
            RotateToAngle(Vector3.right * 70, 20);
            ChangedSortingOrder(110);
            cardSelected.enabled = false;
            cardActions.enabled = activeActions;
            isFocused = true;
            AdjustUIcons();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Devuelve la carta a su tamaño y posicion original
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
            AdjustUIcons();
        }
    }

    private void AdjustUIcons()
    {
        if (cardSO is HeroCardSO)
        {
            if (isFocused)
            {
                healtText.gameObject.transform.localScale = Vector3.one;
                defenceText.gameObject.transform.localScale = Vector3.one;
                speedText.gameObject.transform.localScale = Vector3.one;
            }
            else if (!isFocused && fieldPosition != null)
            {
                healtText.gameObject.transform.localScale = Vector3.one * 5;
                defenceText.gameObject.transform.localScale = Vector3.one * 5;
                speedText.gameObject.transform.localScale = Vector3.one * 5;
            }
        }
    }

    private void ChangedSortingOrder(int sortingOrder)
    {
        canvasFront.sortingOrder = sortingOrder;
        canvasBack.sortingOrder = isVisible ? sortingOrder - 1 : sortingOrder + 1;
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
        DuelManager.instance.UseMovement(movementNumber, null);
        cardActions.enabled = false;
        ResetSize();
    }

    public void ActiveSelectableTargets(Color color)
    {
        cardSelected.enabled = true;
        cardSelectedImage.color = color;
        isAttackable = true;
    }

    public void DesactiveSelectableTargets()
    {
        cardSelected.enabled = false;
        isAttackable = false;
        if (isMyTurn) ActiveSelectableTargets(Color.yellow);
    }

    public IEnumerator MeleeAttackAnimation(int player, Card cardToAttak, Movement movement)
    {
        if(player == 1)
        {
            yield return MoveToPosition(cardToAttak.gameObject.transform.position + new Vector3(0, 0.5f, -2), 20, true, false);
            Instantiate(movement.MoveSO.VisualEffect, transform.position + Vector3.forward, Quaternion.identity);
        }
        else
        {
            yield return MoveToPosition(cardToAttak.gameObject.transform.position + new Vector3(0, 0.5f, 2), 20, true, false);
            Instantiate(movement.MoveSO.VisualEffect, transform.position + Vector3.back, Quaternion.identity);
        }
    }

    public void RangedMovementAnimation()
    {
        StartCoroutine(MoveToPosition(transform.localPosition + Vector3.back, 20, true, true));
    }

    public void AnimationReceivingMovement(Movement movement)
    {
        //Efecto de daño.
        Instantiate(movement.MoveSO.VisualEffectHit, isFocused ? lastPosition : transform.position, Quaternion.identity);

        var protector = HasProtector();
        if (protector != null && protector.hasProtector && movement.MoveSO.Damage > 0)
        {
            protector.damageReceiver.ProtectAlly(fieldPosition);
        }
    }

    /// <summary>
    /// Recibe el daño de un ataque o efecto.
    /// </summary>
    /// <param name="amountDamage"></param>
    /// <returns>Si el heroe se queda sin energia debuelve true</returns>
    public bool ReceiveDamage(int amountDamage)
    {
        var protector = HasProtector();
        if (protector != null && protector.hasProtector)
        {
            return protector.damageReceiver.ReceiveDamage(amountDamage);
        }

        amountDamage -= GetDamageAbsorbed();
        if(amountDamage < 0) amountDamage = 0;

        int remainingDamage = ReceiveDamageToShield(amountDamage);
        
        if(remainingDamage > 0)
        {
            ShowTextDamage(false, currentHealt > remainingDamage ? remainingDamage : currentHealt);
            currentHealt -= remainingDamage;

            if (currentHealt <= 0)
            {
                return true;
            }
        }

        MoveToLastPosition();

        UpdateText();

        return false;
    }

    public void ProtectAlly(FieldPosition position)
    {
        StartCoroutine(MoveToPosition(position.transform.position + Vector3.up * 0.1f, 20, true,false));
    }
    public void ToGraveyard()
    {

        CancelAllEffects();
        AdjustUIcons();
        currentDefense = defense;
        currentHealt = healt;
        UpdateText();
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
                move.UpdateEffect();
            }
        }
    }

    private void CancelAllEffects()
    {
        stunned = 0;
        stunEffect.SetActive(false);
        fieldPosition.statModifier.Clear();
        foreach (var move in moves)
        {
            if(move.effect != null) move.CancelEffect();
        }
    }

    public void RegenerateDefense()
    {
        if(currentDefense < defense)
        {
            currentDefense = defense;
            foreach (Effect statModifier in fieldPosition.statModifier)
            {
                statModifier.currentDefense = statModifier.defense;
            }
            Instantiate(regenerateDefenseEffect, transform.position, Quaternion.identity);
            UpdateText();   
        }
    }

    public void AddEffect(Effect statModifier)
    {
        fieldPosition.statModifier.Add(statModifier);
        UpdateText();
    }

    private int GetDefenseModifier()
    {
        int defenceModifier = 0;
        if(fieldPosition == null) return defenceModifier;

        foreach(Effect effect in fieldPosition.statModifier)
        {
            defenceModifier += effect.currentDefense;
        }

        return defenceModifier;
    }

    private int GetSpeedModifier()
    {
        int speedModifier = 0;
        if (fieldPosition == null) return speedModifier;

        foreach (Effect effect in fieldPosition.statModifier)
        {
            speedModifier += effect.speed;
        }

        return speedModifier;
    }

    private int GetDamageAbsorbed()
    {
        int damageAbsorbed = 0;
        if (fieldPosition == null) return damageAbsorbed;

        foreach (Effect effect in fieldPosition.statModifier)
        {
            damageAbsorbed += effect.absorbDamage;
        }

        return damageAbsorbed;
    }

    private Effect HasProtector()
    {
        foreach (Effect effect in fieldPosition.statModifier)
        {
            if(effect.hasProtector) return effect;
        }

        return null;
    }

    private int ReceiveDamageToShield(int damage)
    {
        if(CurrentDefensePoints <= damage)
        {
            if(CurrentDefensePoints != 0)ShowTextDamage(true, CurrentDefensePoints);

            var remainingDamage = CurrentDefensePoints - damage;
            currentDefense = 0;
            foreach (Effect statModifier in fieldPosition.statModifier)
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
                foreach (Effect statModifier in fieldPosition.statModifier)
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

    public void ToHeal(int amount)
    {
        ShowTextToHeal(healt - currentHealt < amount ? healt - currentHealt : amount);
        currentHealt += amount;
        if(currentHealt > healt)currentHealt = healt;
        UpdateText();
    }

    private void ShowTextToHeal(int amount)
    {
        var position = healtText.transform.position;
        position += Vector3.up;

        var popUp = Instantiate(energyPopUpPrefab, healtText.transform.position, Quaternion.identity);
        var text = popUp.GetComponentInChildren<TextMeshProUGUI>();
        text.color = Color.green;
        text.text = $"+{amount}";
        text.color = Color.red;
    }
}

