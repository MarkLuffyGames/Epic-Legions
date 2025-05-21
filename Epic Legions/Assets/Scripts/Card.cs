using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class Card : MonoBehaviour
{
    [SerializeField] private Canvas canvasFront;
    [SerializeField] private Canvas canvasBack;
    [SerializeField] private Canvas cardActions;
    [SerializeField] private Button move1Button;
    [SerializeField] private Button move2Button;
    [SerializeField] private Image cardImage;
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
    [SerializeField] private Vector3 focusPosition = new Vector3(0, 7.05f, -6.4f);
    [SerializeField] private float highlighterHeight = 0.1f;
    [SerializeField] private int angleHeldCard = 70;
    [SerializeField] private float heldCardHeight = 2.75f;
    [SerializeField] private List<GameObject> statsIcons = new List<GameObject>();

    public CardSO cardSO;

    private Vector3 offset;
    private bool isDragging = false;
    private bool isPlayer;
    public bool isMyTurn;
    private bool isMoving;
    private int sortingOrder;
    public static int cardMovementSpeed = 20;

    private List<Effect> statModifier;

    private Vector3 lastPosition;
    private Vector3 defaultRotation;

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
    private DuelManager duelManager;
    public bool isAttackable;

    public int lastDamageInflicted = 0;

    public int HealtPoint => maxHealt;
    public int CurrentHealtPoints => currentHealt;
    public int CurrentDefensePoints => Mathf.Clamp(currentDefense + GetDefenseModifier(), 0, 99);
    public int CurrentSpeedPoints => Mathf.Clamp(speed + GetSpeedModifier(), 1, 99);
    public List<Movement> Moves => moves;
    public FieldPosition FieldPosition => fieldPosition;

    Coroutine moveCoroutine;
    Coroutine rotateCoroutine;
    /// <summary>
    /// Establece todos los datos de la carta.
    /// </summary>
    /// <param name="cardSO">Scriptable Object que contiene los datos de la carta que se desea establecer</param>
    public void SetCard(CardSO cardSO, DuelManager duelManager)
    {
        this.cardSO = cardSO;
        this.duelManager = duelManager;

        defaultRotation = new Vector3(53, 0, 0);

        nameText.text = cardSO.CardName;
        lastNameText.text = cardSO.CardLastName;

        cardImage.sprite = cardSO.CardSprite;
        /*
        cardImage.material = new Material(cardImage.material);
        cardImage.material.SetTexture("_BaseMap", cardSO.CardSprite.texture);
        cardImage.material.SetTexture("_EmissionMap", cardSO.CardSprite.texture);*/

        if (cardSO is HeroCardSO heroCardSO)
        {
            maxHealt = heroCardSO.Healt;
            defense = heroCardSO.Defence;
            speed = heroCardSO.Speed;
            energy = heroCardSO.Energy;
            
            foreach(var move in heroCardSO.Moves)
            {
                moves.Add(new Movement(move));
                if(move.AlwaysActive) moves[moves.Count -1].ActivateEffect(this, this);
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

            statModifier = new List<Effect>();
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
    public void SetFieldPosition(FieldPosition fieldPosition, Vector3 rotation)
    {
        this.fieldPosition = fieldPosition;
        defaultRotation = rotation;
        AdjustUIcons();
    }

    /// <summary>
    /// Actualiza los datos de la carta.
    /// </summary>
    public void UpdateText()
    {
        if (cardSO is HeroCardSO) 
        {
            healtText.color = currentHealt < maxHealt ? Color.red : Color.yellow;
            defenceText.color = currentDefense < defense ? Color.red : CurrentDefensePoints > defense ? Color.green : Color.yellow;
            speedText.color = CurrentSpeedPoints < speed ? Color.red : CurrentSpeedPoints > speed ? Color.green : Color.yellow;

            healtText.text = currentHealt.ToString();
            defenceText.text = CurrentDefensePoints.ToString();
            speedText.text = CurrentSpeedPoints.ToString();
            energyText.text = energy.ToString(); 
        }
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
        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        yield return moveCoroutine = StartCoroutine(MoveSmoothly(targetPosition, speed, temporalPosition, isLocal));
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
        StartCoroutine(MoveToPosition(lastPosition, cardMovementSpeed, false, true));
    }

    /// <summary>
    /// Rota la carta a un ángulo específico
    /// </summary>
    /// <param name="targetRotation">Angulo al que se quiere rotar la carta</param>
    /// <param name="speed">Velocidad a la que se quiere rotar la carta</param>
    public void RotateToAngle(Vector3 targetRotation, float speed, bool temporalRotation)
    {
        if(rotateCoroutine != null)
            StopCoroutine(rotateCoroutine);

        rotateCoroutine = StartCoroutine(RotateSmoothly(targetRotation, speed, temporalRotation));
    }

    /// <summary>
    /// Corrutina para rotar la carta suavemente al ángulo objetivo
    /// </summary>
    private IEnumerator RotateSmoothly(Vector3 targetRotation, float speed, bool temporalRotation)
    {

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
        transform.localPosition += Vector3.up * highlighterHeight;
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
            transform.localScale = Vector3.one;
            transform.localPosition -= Vector3.up * highlighterHeight;
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
            StartCoroutine(MoveToPosition(focusPosition, cardMovementSpeed, true, false));
            RotateToAngle(Vector3.right * 53, cardMovementSpeed, true);
            ChangedSortingOrder(110);
            EnableActions(isPlayer && !actionIsReady);
            isFocused = true;
            AdjustUIcons();
            return true;
        }

        return false;
    }

    private void EnableActions(bool enable)
    {
        cardActions.enabled = enable;
        if (cardActions.isActiveAndEnabled)
        {
            move1Button.gameObject.SetActive(UsableMovement(0, duelManager.Player1Manager));

            move2Button.gameObject.SetActive(UsableMovement(1, duelManager.Player1Manager));
        }
    }

    public bool UsableMovement(int moveIndex, PlayerManager playerManager)
    {
        PlayerManager otherPlayerManager = playerManager == duelManager.Player1Manager ? duelManager.Player2Manager : duelManager.Player1Manager;

        return moves[moveIndex].MoveSO.EnergyCost <= playerManager.PlayerEnergy
                && (duelManager.ObtainTargets(this, moveIndex).Count > 0 || (moves[moveIndex].MoveSO.MoveType == MoveType.PositiveEffect ? !moves[moveIndex].MoveSO.NeedTarget :
                otherPlayerManager.GetFieldPositionList().All(field => field.Card == null)));
    }

    /// <summary>
    /// Devuelve la carta a su posicion.
    /// </summary>
    public void ResetSize()
    {
        if (isFocused)
        {
            MoveToLastPosition();
            RotateToAngle(defaultRotation, cardMovementSpeed, false);
            ChangedSortingOrder(sortingOrder);
            cardActions.enabled = false;
            isFocused = false;
            AdjustUIcons();
        }
    }

    /// <summary>
    /// Ajusta el tamaño de los iconos, agrandandolos si esta en el campo o su tamaño original si no lo está.
    /// </summary>
    private void AdjustUIcons(bool defaultValue = false)
    {
        if (cardSO is HeroCardSO)
        {
            if (isFocused || defaultValue)
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
        cardActions.sortingOrder = sortingOrder + 1;
    }


    float heldTime;
    /// <summary>
    /// Inicia el arrastre de la carta
    /// </summary>
    public void StartDragging(float heldTime)
    {
        this.heldTime = heldTime;
        if (isDragging)return;

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
            if(heldTime > CardSelector.clickHoldTime) RotateToAngle(new Vector3(angleHeldCard, 0, 0), cardMovementSpeed * 2, true);
            Vector3 newPosition = GetMouseWorldPosition() + offset;
            newPosition = new Vector3(newPosition.x, heldCardHeight, newPosition.z);

            while (Vector3.Distance(transform.position, newPosition) > 0.01f)
            {
                transform.position = Vector3.Lerp(transform.position, newPosition, cardMovementSpeed * Time.deltaTime);
                return;
            }

            transform.position = newPosition;
        }

    }

    public bool UsableCard(PlayerManager playerManager)
    {
        if (cardSO is HeroCardSO heroCardSO)
        {
            if(duelManager.GetCurrentDuelPhase() == DuelPhase.Preparation &&
                heroCardSO.Energy <= playerManager.PlayerEnergy)
            {
                return true;
            }

            return false;
        }
        else if( cardSO is SpellCardSO spellCardSO)
        {
            if ((duelManager.GetCurrentDuelPhase() == DuelPhase.Preparation ||
                duelManager.GetCurrentDuelPhase() == DuelPhase.Battle) &&
                spellCardSO.Move.EnergyCost <= playerManager.PlayerEnergy &&
                !duelManager.SettingAttackTarget &&
                duelManager.ObtainTargets(this, 0).Count > 0)
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
        this.isPlayer = isPlayer;
        isMyTurn = true;

        fieldPosition.ChangeEmission(Color.yellow);

        if (IsInLethargy())
        {
            EndTurn();
        }
    }

    public void PassTurn()
    {
        CancelStun();
    }

    /// <summary>
    /// Finaliza el turno de esta carta.
    /// </summary>
    public void EndTurn()
    {
        isMyTurn = false;
        fieldPosition.RestoreOriginalColor();
    }

    /// <summary>
    /// Indica al DuelManager que movimiento va a usar esta carta.
    /// </summary>
    /// <param name="movementNumber">Indice del movimiento a utilizar.</param>
    public void UseMovement(int movementNumber)
    {
        duelManager.UseMovement(movementNumber, this);
        cardActions.enabled = false;
        ResetSize();
    }

    /// <summary>
    /// Establece la carta como seleccionable para objetivo de un movimiento.
    /// </summary>
    /// <param name="color">Color del marcador de seleccion.</param>
    public void ActiveSelectableTargets(Color color)
    {
        fieldPosition.ChangeEmission(color);
        isAttackable = true;
    }


    /// <summary>
    /// Desmarca esta como seleccionable para objetivo de movimiento.
    /// </summary>
    public void DesactiveSelectableTargets()
    {
        if (isAttackable)
        {
            if (isMyTurn)
            {
                fieldPosition.ChangeEmission(Color.yellow);
            }
            else
            {
                fieldPosition.RestoreOriginalColor();
            }
            
            isAttackable = false;
        }
    }

    public IEnumerator AttackAnimation(int player, Card cardToAttack, Movement movement)
    {
        yield return new WaitWhile(() => isMoving);

        if (isFocused)
        {
            ResetSize();
            yield return null;
        }

        yield return new WaitWhile(() => isMoving);

        if(movement.MoveSO.MoveType == MoveType.PositiveEffect || movement.MoveSO.MoveType == MoveType.RangedAttack)
        {
            yield return MoveToPosition(lastPosition + Vector3.back, cardMovementSpeed, true, true);
        }
        else
        {
            if (cardToAttack != null)
            {
                if(movement.MoveSO.MoveType == MoveType.MeleeAttack)
                {
                    if (player == 1)
                    {
                        yield return MoveToPosition(cardToAttack.transform.position + new Vector3(0, 0.5f, -3), cardMovementSpeed, true, false);
                        if(movement.MoveSO.VisualEffect != null) Instantiate(movement.MoveSO.VisualEffect, transform.position + Vector3.forward + Vector3.up * 0.1f, Quaternion.Euler(Vector3.zero));
                    }
                    else
                    {
                        yield return MoveToPosition(cardToAttack.transform.position + new Vector3(0, 0.5f, 3), cardMovementSpeed, true, false);
                        if (movement.MoveSO.VisualEffect != null) Instantiate(movement.MoveSO.VisualEffect, transform.position + Vector3.back + Vector3.up * 0.1f, Quaternion.Euler(new Vector3(0, 180, 0)));
                    }
                }
                else
                {
                    if (player == 1)
                    {
                        yield return MoveToPosition(cardToAttack.transform.position + new Vector3(0, 0.5f, 3), cardMovementSpeed * 10, true, false);
                        if (movement.MoveSO.VisualEffect != null) Instantiate(movement.MoveSO.VisualEffect, transform.position + Vector3.forward + Vector3.up * 0.1f, Quaternion.Euler(new Vector3(0, 180, 0)));
                    }
                    else
                    {
                        yield return MoveToPosition(cardToAttack.gameObject.transform.position + new Vector3(0, 0.5f, -3), cardMovementSpeed * 10, true, false);
                        if (movement.MoveSO.VisualEffect != null) Instantiate(movement.MoveSO.VisualEffect, transform.position + Vector3.back + Vector3.up * 0.1f, Quaternion.Euler(Vector3.zero));
                    }
                }
                
            }
            else
            {
                if (player == 1)
                {
                    yield return MoveToPosition(new Vector3(0, 0.5f, 5), cardMovementSpeed, true, false);
                    if (movement.MoveSO.VisualEffect != null) Instantiate(movement.MoveSO.VisualEffect, transform.position + Vector3.forward, Quaternion.Euler(Vector3.zero));
                }
                else
                {
                    yield return MoveToPosition(new Vector3(0, 0.5f, -5), cardMovementSpeed, true, false);
                    if (movement.MoveSO.VisualEffect != null) Instantiate(movement.MoveSO.VisualEffect, transform.position + Vector3.back, Quaternion.Euler(new Vector3(0, 180, 0)));
                }
            }
        }
    }

    /// <summary>
    /// Realiza la animacion de recibir Movimiento.
    /// </summary>
    /// <param name="movement"></param>
    public void AnimationReceivingMovement(Movement movement)
    {
        //Efecto de daño.
        if (movement.MoveSO.VisualEffectHit != null)
        {
            Instantiate(movement.MoveSO.VisualEffectHit, isFocused ? lastPosition : transform.position, Quaternion.identity);
        }

        var protector = HasProtector();
        if (protector != null && protector.HasProtector() && movement.MoveSO.Damage > 0)
        {
            protector.casterHero.ProtectAlly(fieldPosition);
        }
    }

    /// <summary>
    /// Recibe el daño de un ataque o efecto.
    /// </summary>
    /// <param name="amountDamage"></param>
    /// <returns>Cantidad de vida perdida por el heroe</returns>
    public int ReceiveDamage(int amountDamage, int ignoredDefense, Card attacker)
    {
        amountDamage += GetAttackModifier();

        if (attacker != null && attacker.cardSO is HeroCardSO && HasFullDamageReflection()) attacker.ReceiveDamage(amountDamage, ignoredDefense, null);

        if (IsInLethargy() || HasPhantomShield())
        {
            amountDamage = 0;
        }

        var damageInflicted = 0;
        var protector = HasProtector();
        if (protector != null && protector.HasProtector())
        {
            return protector.casterHero.ReceiveDamage(amountDamage, ignoredDefense, attacker);
        }

        amountDamage -= GetDamageAbsorbed();
        if(amountDamage < 0) amountDamage = 0;

        int remainingDamage = ReceiveDamageToShield(amountDamage - ignoredDefense) + ignoredDefense;

        if(remainingDamage > 0)
        {
            damageInflicted = currentHealt > remainingDamage ? remainingDamage : currentHealt;
            ShowTextDamage(false, damageInflicted);
            currentHealt -= remainingDamage;
            if (currentHealt < 0) currentHealt = 0;
        }

        MoveToLastPosition();

        UpdateText();

        return damageInflicted;
    }


    /// <summary>
    /// Realiza la animacion de proteger a un aliado.
    /// </summary>
    /// <param name="position"></param>
    public void ProtectAlly(FieldPosition position)
    {
        StartCoroutine(MoveToPosition(position.transform.position + Vector3.up * 0.1f, cardMovementSpeed, true,false));
    }

    /// <summary>
    /// Atualiza los datos de la carta para cuando esta en el cementerio.
    /// </summary>
    public void ToGraveyard()
    {
        CancelAllEffects();
        AdjustUIcons(true);
        currentDefense = defense;
        currentHealt = maxHealt;
        UpdateText();
        AudioManager.Instance.PlayCardDestroy();
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
    /// Comprueba si el heroe esta aturdido.
    /// </summary>
    /// <returns></returns>
    public bool IsStunned()
    {
        return statModifier.Any(x => x.IsStunned());
    }

    private void CancelStun()
    {
        statModifier.ForEach(x => x.CancelStun());
    }

    /// <summary>
    /// Activa los efectos asignados a esta carta.
    /// </summary>
    public void ActivateEffect()
    {
        statModifier.ForEach(effect => effect.ActivateEffect());

        ActivateVisualEffects();

        UpdateText();
    }

    /// <summary>
    /// Actualiza el estado de los efectos activos en esta carta.
    /// </summary>
    public void ManageEffects()
    {
        foreach (var effect in statModifier)
        {
            if (effect.durability > 0)
            {
                effect.MoveEffect.UpdateEffect(effect);
            }
        }

        List<Effect> effects = new List<Effect>();
        foreach (var effect in statModifier)
        {
            if(effect.durability <= 0)
            {
                effects.Add(effect);
            }
        }

        statModifier.RemoveAll(stat => stat.durability <= 0);

        foreach (var effect in effects)
        {
            if(statModifier.All(x => x.MoveEffect != effect.MoveEffect))
            {
                HideIcon(effect.MoveEffect.iconSprite);
            }
        }

    }

    /// <summary>
    /// Cancela todos los efectos con relacion a esta carta.
    /// </summary>
    private void CancelAllEffects()
    {
        if(cardSO is HeroCardSO)
        {
            stunEffect.SetActive(false);
            foreach (var effect in statModifier)
            {
                HideIcon(effect.MoveEffect.iconSprite);
            }

            statModifier.Clear();
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
            foreach (Effect statModifier in statModifier)
            {
                statModifier.RegenerateDefense();
            }
            if(regenerateDefenseEffect != null) Instantiate(regenerateDefenseEffect, transform.position, Quaternion.identity);
            UpdateText();   
        }
    }

    /// <summary>
    /// Añade un efecto a esta carta.
    /// </summary>
    /// <param name="effect">Efecto que se añade a la carta.</param>
    public void AddEffect(Effect effect)
    {
        if(effect.MoveEffect is not Poison ||  statModifier.All(x => x.MoveEffect is not Antivenom))
        {
            ShowIcon(effect.MoveEffect.iconSprite);
            statModifier.Add(effect);
        }
        
        UpdateText();
    }

    private void ShowIcon(Sprite sprite)
    {
        foreach (GameObject icon in statsIcons)
        {
            if(!icon.activeInHierarchy)
            {
                icon.SetActive(true);
                icon.GetComponentInChildren<Image>().sprite = sprite;
                break;
            }
            else
            {
                if(icon.GetComponentInChildren<Image>().sprite == sprite)
                {
                    break;
                }
            }
        }
    }

    private void HideIcon(Sprite sprite)
    {
        foreach (GameObject icon in statsIcons)
        {
            var s = icon.GetComponentInChildren<Image>().sprite;

            if (icon.activeInHierarchy && s == sprite)
            {
                icon.SetActive(false);
            }
        }
    }

    public void ActivateVisualEffects()
    {
        stunEffect.SetActive(IsStunned());
        //TODO: Activar efecto visual de envenenamiento.
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
            foreach (Effect statModifier in statModifier)
            {
                if(statModifier.GetCurrentDefence() > 0)
                {
                    statModifier.SetCurrentDefence(0);
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
                foreach (Effect statModifier in statModifier)
                {
                    if(statModifier.GetCurrentDefence() > 0)
                    {
                        remainingDamage = damage - statModifier.GetCurrentDefence();
                        statModifier.SetCurrentDefence(statModifier.GetCurrentDefence() - damage);
                        if (statModifier.GetCurrentDefence() < 0) statModifier.SetCurrentDefence(0);
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
        if(statModifier.Any(x => x.MoveEffect is NoHealing))amount = 0;

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

    public void RechargeEnergy(int amount)
    {
        var player = duelManager.GetPlayerManagerForCard(this);
        if (player != null)
        {
            player.RechargeEnergy(amount); 
        }
        else
        {
            Debug.LogError("Jugador no encontrado"); 
        }
    }

    public void ApplyPoisonDamage(int amount)
    {
        ReceiveDamage(amount, amount, null);
        ActivateVisualEffects();
    }

    public DuelManager GetDuelManager() { return duelManager; }

    public IEnumerator Counter(Card card)
    {
        foreach (var item in statModifier)
        {
            if(item.MoveEffect is Counterattack counterattack)
            {
                yield return duelManager.Counterattack(item.casterHero, card, item.GetCounterattackDamage());
                statModifier.Remove(item);
                break;
            }
            else if(item.MoveEffect is ToxicContact poisonedcounterattack)
            {
                card.AddEffect(new Effect(new Poison(), card));

            }
        }
    }

    public void ClearAllEffects()
    {
        statModifier.RemoveAll(e => e.IsRemovable());
    }

    public void CleanAllNegativeEffects()
    {
        statModifier.RemoveAll(e => e.IsNegative());
    }

    /// <summary>
    /// Verifica si esta carta esta protegida por otra.
    /// </summary>
    /// <returns>True si tiene un protector.</returns>
    private Effect HasProtector()
    {
        foreach (Effect effect in statModifier)
        {
            if (effect.HasProtector()) return effect;
        }

        return null;
    }

    private bool IsInLethargy()
    {
        return statModifier.Any(x => x.MoveEffect is Lethargy);
    }

    private bool HasPhantomShield()
    {
        foreach (Effect effect in statModifier)
        {
            if (effect.MoveEffect is PhantomShield phantomShield)
            {
                effect.MoveEffect.DeactivateEffect(effect);
                return true;
            }
        }
        return false;
    }

    private bool HasFullDamageReflection()
    {
        return statModifier.Any(x => x.MoveEffect is FullDamageReflection);
    }

    /// <summary>
    /// Verifica los modificadores de defensa que tenga activo esta carta.
    /// </summary>
    /// <returns>Cantidad modificada.</returns>
    private int GetDefenseModifier()
    {
        int defenceModifier = 0;

        foreach (Effect effect in statModifier)
        {
            if(effect.MoveEffect is ModifyDefense) defenceModifier += effect.GetCurrentDefence();
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

        foreach (Effect effect in statModifier)
        {
            if (effect.MoveEffect is ModifySpeed) speedModifier += effect.GetSpeed();
        }

        return speedModifier;
    }

    /// <summary>
    /// Verifica los modificadores de ataque que tenga activo esta carta.
    /// </summary>
    /// <returns>Cantidad modificada.</returns>
    private int GetAttackModifier()
    {
        int attackModifier = 0;

        foreach (Effect effect in statModifier)
        {
            if (effect.MoveEffect is ModifyAttack) attackModifier += effect.GetAttack();
        }

        return attackModifier;
    }

    /// <summary>
    /// Verifica cuanto daño debe absorber esta carta segun los efectos activos.
    /// </summary>
    /// <returns>Cantidad absorbida.</returns>
    public int GetDamageAbsorbed()
    {
        int damageAbsorbed = 0;

        foreach (Effect effect in statModifier)
        {
            damageAbsorbed += effect.GetDamageAbsorbed();
        }

        return damageAbsorbed;
    }
}

