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
    [SerializeField] private Image move1EnergyImage;
    [SerializeField] private TextMeshProUGUI move1EnergyCostText;
    [SerializeField] private Image move1DamageImage;
    [SerializeField] private TextMeshProUGUI move1DamageText;
    [SerializeField] private TextMeshProUGUI move1DescriptionText;
    [SerializeField] private TextMeshProUGUI move2NameText;
    [SerializeField] private Image move2EnergyImage;
    [SerializeField] private TextMeshProUGUI move2EnergyCostText;
    [SerializeField] private Image move2DamageImage;
    [SerializeField] private TextMeshProUGUI move2DamageText;
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
    private bool isMoving;
    private int sortingOrder;

    private Vector3 lastPosition;
    private Vector3 lastRotation;

    private bool isFocused;
    private bool isHighlight;
    public bool isTemporalPosition;

    public bool isVisible;
    public bool waitForServer;
    public bool actionIsReady;

    private int maxHealt;
    private int defense;
    private int speed;
    private int energy;

    private int currentHealt;
    private int currentDefense;

    private List<Movement> moves = new List<Movement>();
    private FieldPosition fieldPosition;
    public bool isAttackable;

    public int HealtPoint => maxHealt;
    public int CurrentHealtPoints => currentHealt;
    public int CurrentDefensePoints => Mathf.Max(currentDefense + GetDefenseModifier(), 0);
    public int CurrentSpeedPoints => Mathf.Max(speed + GetSpeedModifier(), 1);
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
            maxHealt = heroCardSO.Healt;
            defense = heroCardSO.Defence;
            speed = heroCardSO.Speed;
            energy = heroCardSO.Energy;
            
            foreach(var move in heroCardSO.Moves)
            {
                moves.Add(new Movement(move));
            }

            currentHealt = maxHealt;
            currentDefense = defense;

            if (moves[0] != null)
            {
                move1NameText.text = moves[0].MoveSO.MoveName;
                move1EnergyCostText.text = moves[0].MoveSO.EnergyCost.ToString();
                move1DamageText.text = moves[0].MoveSO.Damage.ToString();
                move1DescriptionText.text = moves[0].MoveSO.EffectDescription;
                if(moves[0].MoveSO.Damage == 0)
                {
                    move1DamageText.enabled = false;
                    move1DamageImage.enabled = false;
                }
            }
            if (moves[1] != null)
            {
                move2NameText.text = moves[1].MoveSO.MoveName;
                move2EnergyCostText.text = moves[1].MoveSO.EnergyCost.ToString();
                move2DamageText.text = moves[1].MoveSO.Damage.ToString();
                move2DescriptionText.text = moves[1].MoveSO.EffectDescription;
                if (moves[1].MoveSO.Damage == 0)
                {
                    move2DamageText.enabled = false;
                    move2DamageImage.enabled = false;
                }
            }

            UpdateText();
        }
        else if (cardSO is SpellCardSO spellCardSO)
        {
            moves.Add(new Movement(spellCardSO.Move));
            description.text = spellCardSO.Move.EffectDescription;

            move1NameText.enabled = false;
            move1EnergyImage.enabled = false;
            move1EnergyCostText.enabled = false;
            move1DamageImage.enabled = false;
            move1DamageText.enabled = false;
            move1DescriptionText.enabled = false;
            move2NameText.enabled = false;
            move2EnergyImage.enabled = false;
            move2EnergyCostText.enabled = false;
            move2DamageImage.enabled = false;
            move2DamageText.enabled = false;
            move2DescriptionText.enabled = false;
            healtText.enabled = false;
            defenceText.enabled = false;
            speedText.enabled = false;
            energyText.enabled = false;
        }
    }


    /// <summary>
    /// Establece la posicion en el campo de la carta.
    /// </summary>
    /// <param name="fieldPosition">Posicion del campo donde estara la carta.</param>
    public void SetFieldPosition(FieldPosition fieldPosition)
    {
        this.fieldPosition = fieldPosition;
        AdjustUIcons();
    }

    /// <summary>
    /// Actualiza los datos de la carta.
    /// </summary>
    public void UpdateText()
    {
        healtText.color = currentHealt < maxHealt ?  Color.red : Color.yellow;
        defenceText.color = currentDefense < defense ? Color.red : CurrentDefensePoints > defense ? Color.green : Color.yellow;
        speedText.color = CurrentSpeedPoints < speed ? Color.red : CurrentSpeedPoints > speed ? Color.green : Color.yellow;

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
        isMoving = true;
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

        isMoving = false;
        
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
    public void RotateToAngle(Vector3 targetRotation, float speed, bool temporalRotation)
    {
        StartCoroutine(RotateSmoothly(targetRotation, speed, temporalRotation));
    }

    /// <summary>
    /// Corrutina para rotar la carta suavemente al ángulo objetivo
    /// </summary>
    private IEnumerator RotateSmoothly(Vector3 targetRotation, float speed, bool temporalRotation)
    {

        if(temporalRotation) lastRotation = transform.eulerAngles;

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
    /// Posiciona la carta en un punto delante de la pantalla para verla detalladamente.
    /// </summary>
    /// <returns>Retorna true si la carta fue agrandada correctamente.</returns>
    public bool Enlarge()
    {
        if (Vector3.Distance(lastPosition, transform.localPosition) < 0.2f)
        {
            StartCoroutine(MoveToPosition(new Vector3(0, 9.18f, -4.3f), 20, true, false));
            RotateToAngle(Vector3.right * 70, 20, true);
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
    /// Devuelve la carta a su posicion.
    /// </summary>
    public void ResetSize()
    {
        if (isFocused)
        {
            MoveToLastPosition();
            RotateToAngle(lastRotation, 20, false);
            ChangedSortingOrder(sortingOrder);
            if (isMyTurn) cardSelected.enabled = true;
            cardActions.enabled = false;
            isFocused = false;
            AdjustUIcons();
        }
    }

    /// <summary>
    /// Ajusta el tamaño de los iconos, agrandandolos si esta en el campo o su tamaño original si no lo está.
    /// </summary>
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

    /// <summary>
    /// Cambia el Sort Order de la carta.
    /// </summary>
    /// <param name="sortingOrder"></param>
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
            RotateToAngle(new Vector3(70, 0, 0), 20, true);
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

    public bool UsableCard()
    {
        if (cardSO is HeroCardSO heroCardSO)
        {
            if(DuelManager.Instance.GetDuelPhase() == DuelPhase.Preparation)
            {
                return true;
            }

            return false;
        }
        else if( cardSO is SpellCardSO spellCardSO)
        {
            if ((DuelManager.Instance.GetDuelPhase() == DuelPhase.Preparation ||
                DuelManager.Instance.GetDuelPhase() == DuelPhase.Battle) &&
                !DuelManager.Instance.settingAttackTarget &&
                DuelManager.Instance.ObtainTargets(this, 0).Count > 0)
            {
                return true;
            }

            return false;
        }

        return false;
    }

    /// <summary>
    /// Obtiene la posición del mouse en el espacio del mundo
    /// </summary>
    /// <returns>La posicion del mouse en pantalla</returns>
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
    /// Metodo para consultar si la carta está siendo arrastrada.
    /// </summary>
    /// <returns>True si la carta está siendo arrastrada.</returns>
    public bool IsDragging()
    {
        return isDragging;
    }

    /// <summary>
    /// Metodo para consultar si la carta está enfocada.
    /// </summary>
    /// <returns>True si la carta está enfocada.</returns>
    public bool IsHighlight()
    {
        return isHighlight;
    }

    /// <summary>
    /// Prepara la carta para realizar las acciones de su turno.
    /// </summary>
    /// <param name="isPlayer">Bool esta carta es propiedad del jugador.</param>
    public void SetTurn(bool isPlayer)
    {
        isMyTurn = true;

        if (isPlayer)
        {
            activeActions = true;

            cardSelected.enabled = true;
            cardSelectedImage.color = Color.yellow;
        }
    }

    /// <summary>
    /// Finaliza el turno de esta carta.
    /// </summary>
    public void EndTurn()
    {
        isMyTurn = false;
        cardSelected.enabled = false;
        activeActions = false;
    }

    /// <summary>
    /// Indica al DuelManager que movimiento va a usar esta carta.
    /// </summary>
    /// <param name="movementNumber">Indice del movimiento a utilizar.</param>
    public void UseMovement(int movementNumber)
    {
        DuelManager.Instance.UseMovement(movementNumber, this);
        cardActions.enabled = false;
        ResetSize();
    }

    /// <summary>
    /// Establece la carta como seleccionable para objetivo de un movimiento.
    /// </summary>
    /// <param name="color">Color del marcador de seleccion.</param>
    public void ActiveSelectableTargets(Color color)
    {
        cardSelected.enabled = true;
        cardSelectedImage.color = color;
        isAttackable = true;
    }


    /// <summary>
    /// Desmarca esta como seleccionable para objetivo de movimiento.
    /// </summary>
    public void DesactiveSelectableTargets()
    {
        if (isAttackable)
        {
            cardSelected.enabled = false;
            isAttackable = false;
        }
    }

    /// <summary>
    /// Realiza la animacion de ataque cuerpo acuerpo.
    /// </summary>
    /// <param name="player">Numero del jugador que utiliza el atque.</param>
    /// <param name="cardToAttak">Carta a la que se dirige el atque.</param>
    /// <param name="movement">Movimiento utilizado.</param>
    /// <returns></returns>
    public IEnumerator MeleeAttackAnimation(int player, Card cardToAttak, Movement movement)
    {
        yield return new WaitWhile(() => isMoving);

        if (isFocused)
        {
            ResetSize();
            yield return null;
        }

        yield return new WaitWhile(() => isMoving);

        if (cardToAttak != null)
        {
            if (player == 1)
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
        else
        {
            if (player == 1)
            {
                yield return MoveToPosition(new Vector3(0, 0.5f, 5), 20, true, false);
                Instantiate(movement.MoveSO.VisualEffect, transform.position + Vector3.forward, Quaternion.identity);
            }
            else
            {
                yield return MoveToPosition(new Vector3(0, 0.5f, -5), 20, true, false);
                Instantiate(movement.MoveSO.VisualEffect, transform.position + Vector3.back, Quaternion.identity);
            }
        }
    }

    /// <summary>
    /// Realiza la animacion de movimiento a distancia.
    /// </summary>
    /// <returns></returns>
    public IEnumerator RangedMovementAnimation()
    {
        yield return new WaitWhile(() => isMoving);

        if (isFocused)
        {
            ResetSize();
            yield return null;
        }

        yield return new WaitWhile(() => isMoving);

        yield return MoveToPosition(lastPosition + Vector3.back, 20, true, true);
    }

    /// <summary>
    /// Realiza la animacion de recibir Movimiento.
    /// </summary>
    /// <param name="movement"></param>
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
    public void ReceiveDamage(int amountDamage)
    {
        var protector = HasProtector();
        if (protector != null && protector.hasProtector)
        {
            protector.damageReceiver.ReceiveDamage(amountDamage);
            return;
        }

        amountDamage -= GetDamageAbsorbed();
        if(amountDamage < 0) amountDamage = 0;

        int remainingDamage = ReceiveDamageToShield(amountDamage);
        
        if(remainingDamage > 0)
        {
            ShowTextDamage(false, currentHealt > remainingDamage ? remainingDamage : currentHealt);
            currentHealt -= remainingDamage;
            if (currentHealt < 0) currentHealt = 0;
        }

        MoveToLastPosition();

        UpdateText();
    }


    /// <summary>
    /// Realiza la animacion de proteger a un aliado.
    /// </summary>
    /// <param name="position"></param>
    public void ProtectAlly(FieldPosition position)
    {
        StartCoroutine(MoveToPosition(position.transform.position + Vector3.up * 0.1f, 20, true,false));
    }

    /// <summary>
    /// Atualiza los datos de la carta para cuando esta en el cementerio.
    /// </summary>
    public void ToGraveyard()
    {
        CancelAllEffects();
        AdjustUIcons();
        currentDefense = defense;
        currentHealt = maxHealt;
        UpdateText();
    }

    /// <summary>
    /// Muestra el contador de daño sobre la carta.
    /// </summary>
    /// <param name="isDefence"></param>
    /// <param name="amountDamage"></param>
    private void ShowTextDamage(bool isDefence, int amountDamage)
    {
        var position = isDefence ? defenceText.transform.position : healtText.transform.position;
        position += Vector3.up;

        var popUp = Instantiate(isDefence ? defencePopUpPrefab : energyPopUpPrefab, position, Quaternion.identity);
        popUp.GetComponentInChildren<TextMeshProUGUI>().text = $"-{amountDamage}";
    }

    /// <summary>
    /// Maneja el efecto de aturdir.
    /// </summary>
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

    /// <summary>
    /// Actualiza el estado de los efectos activos por movimientos de esta carta.
    /// </summary>
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

    /// <summary>
    /// Cancela todos los efectos con relacion a esta carta.
    /// </summary>
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

    /// <summary>
    /// Regenera la defensa de esta carta.
    /// </summary>
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

    /// <summary>
    /// Añade un efcto a esta carta.
    /// </summary>
    /// <param name="effect">Efecto que se añade a la carta.</param>
    public void AddEffect(Effect effect)
    {
        fieldPosition.statModifier.Add(effect);
        UpdateText();
    }

    /// <summary>
    /// Verifica los modificadores de defensa que tenga activo esta carta.
    /// </summary>
    /// <returns>Cantidad modificada.</returns>
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

    /// <summary>
    /// Verifica los modificadores de velocidad que tenga activo esta carta.
    /// </summary>
    /// <returns>Cantidad modificada.</returns>
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

    /// <summary>
    /// Verifica cuanto daño debe absorber esta carta segun los efectos activos.
    /// </summary>
    /// <returns>Cantidad absorbida.</returns>
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

    /// <summary>
    /// Verifica si esta carta esta protegida por otra.
    /// </summary>
    /// <returns>True si tiene un protector.</returns>
    private Effect HasProtector()
    {
        foreach (Effect effect in fieldPosition.statModifier)
        {
            if(effect.hasProtector) return effect;
        }

        return null;
    }

    /// <summary>
    /// Recibe el daño del escudo.
    /// </summary>
    /// <param name="damage">Daño aplicado a esta carta.</param>
    /// <returns>Cantidad de daño que no pudo proteger el escudo.</returns>
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

    /// <summary>
    /// Sana la vida de esta carta.
    /// </summary>
    /// <param name="amount">Cantidad a sanar.</param>
    public void ToHeal(int amount)
    {
        ShowTextToHeal(maxHealt - currentHealt < amount ? maxHealt - currentHealt : amount);
        currentHealt += amount;
        if(currentHealt > maxHealt)currentHealt = maxHealt;
        UpdateText();
    }

    /// <summary>
    /// Muestra en pantalla el numero de cuanta vida recupero esta carta.
    /// </summary>
    /// <param name="amount"></param>
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

