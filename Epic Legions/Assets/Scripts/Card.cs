using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    [SerializeField] private Canvas canvasFront;
    [SerializeField] private Canvas canvasBack;
    [SerializeField] private Canvas cardActions;
    [SerializeField] private Canvas cardBorder;
    [SerializeField] private Button move1Button;
    [SerializeField] private Button move2Button;
    [SerializeField] private Button rechargeButton;
    [SerializeField] private Image cardImage;
    [SerializeField] private Image classIcon;
    [SerializeField] private Image elementIcon;
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
    private Material borderMaterial;

    public CardSO cardSO;

    private Vector3 offset;
    private bool isDragging = false;
    public bool isPlayer;
    public bool isMyTurn;
    private bool isMoving;
    private int sortingOrder;
    public static int cardMovementSpeed = 20;


    [RuntimeInitializeOnLoadMethod]
    private static void InitializeOnLoad()
    {
        cardMovementSpeed = 20; // Inicializa el valor de cardMovementSpeed
    }

    private List<Effect> statModifier;
    public List<Effect> StatModifier => statModifier;

    private Vector3 lastPosition;
    private Vector3 defaultRotation;

    private bool isHighlight;
    public bool isTemporalPosition;

    public bool isVisible;
    public bool waitForServer;
    public bool actionIsReady;
    public bool turnCompleted;

    private int maxHealt;
    private int defense;
    private int speed;
    private int energy;

    private int currentHealt;
    private int currentDefense;

    private List<Movement> moves = new List<Movement>();
    private Card[] equipmentCard = new Card[3];
    private Card heroOwner;
    private Card copiedCard;
    private FieldPosition fieldPosition;
    public Graveyard graveyard;
    private DuelManager duelManager;
    public bool isAttackable;

    public int lastDamageInflicted = 0;

    public int HealtPoint => maxHealt;
    public int CurrentHealtPoints => currentHealt;
    public int CurrentDefensePoints => Mathf.Clamp(currentDefense + GetDefenseModifier(), 0, 99);
    public int CurrentSpeedPoints => Mathf.Clamp(speed + GetSpeedModifier(), 1, 99);
    public List<Movement> Moves => moves;
    public Card[] EquipmentCard => equipmentCard;
    public Card HeroOwner => heroOwner;
    public FieldPosition FieldPosition => fieldPosition;
    public DuelManager DuelManager => duelManager;

    Coroutine moveCoroutine;
    Coroutine rotateCoroutine;
    /// <summary>
    /// Establece todos los datos de la carta.
    /// </summary>
    /// <param name="cardSO">Scriptable Object que contiene los datos de la carta que se desea establecer</param>
    public void SetNewCard(CardSO cardSO, DuelManager duelManager)
    {
        this.cardSO = cardSO;
        this.duelManager = duelManager;

        var cardImage = cardBorder.GetComponentInChildren<RawImage>();
        borderMaterial =  new Material(cardImage.material);
        cardImage.material = borderMaterial;

        SetCard();
    }

    private void SetCard()
    {
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

            foreach (var move in heroCardSO.Moves)
            {
                moves.Add(new Movement(move));
                //if (move.AlwaysActive) moves[moves.Count - 1].ActivateEffect(this, this);
            }

            currentHealt = maxHealt;
            currentDefense = defense;

            classIcon.sprite = CardDatabase.GetClassIcon(heroCardSO.HeroClass);
            elementIcon.sprite = CardDatabase.GetElementIcon(heroCardSO.CardElemnt);
            ActivateMoveText1(true);
            ActivateMoveText2(true);
            ActivateHeroStats(true);
            description.text = "";

            if (moves[0] != null)
            {
                move1NameText.rectTransform.localPosition = new Vector3(0.041f, -0.499f, -0.0001f);
                move1Button.transform.localPosition = new Vector3(0.0123f, -0.544f, 0.0f);

                SetMovement1UI(moves[0]);
                
            }
            if (moves[1] != null)
            {
                SetMovement2UI(moves[1]);
                
            }

            statModifier = new List<Effect>();
            UpdateText();
        }
        else if (cardSO is SpellCardSO spellCardSO)
        {
            moves.Add(new Movement(spellCardSO.Move));
            description.text = spellCardSO.Move.EffectDescription;

            ActivateMoveText1(false);
            ActivateMoveText2(false);
            ActivateHeroStats(false);
        }
        else if (cardSO is EquipmentCardSO equipmentCardSO)
        {
            foreach (var move in equipmentCardSO.Moves)
            {
                moves.Add(new Movement(move));
            }

            if (moves.Count > 0 && moves[0] != null)
            {
                ActivateMoveText1(true);

                move1NameText.rectTransform.localPosition = new Vector3(0.041f, -0.601f, -0.0001f);
                move1Button.transform.localPosition = new Vector3(0.0123f, -0.63f, 0.0f);
                SetMovement1UI(moves[0]);
            }
            else
            {
                ActivateMoveText1(false);
            }

            
            ActivateMoveText2(false);
            ActivateHeroStats(false);
            description.text = equipmentCardSO.Description;
        }
    }

    private void SetMovement1UI(Movement movement)
    {
        move1NameText.text = movement.MoveSO.MoveName;
        move1EnergyCostText.text = movement.MoveSO.EnergyCost.ToString();
        move1DamageText.text = movement.MoveSO.Damage.ToString();
        move1DescriptionText.text = movement.MoveSO.EffectDescription;
        if (movement.MoveSO.Damage < 1)
        {
            move1DamageText.enabled = false;
            move1DamageImage.enabled = false;
        }
    }

    private void SetMovement2UI(Movement movement)
    {
        move2NameText.text = movement.MoveSO.MoveName;
        move2EnergyCostText.text = movement.MoveSO.EnergyCost.ToString();
        move2DamageText.text = movement.MoveSO.Damage.ToString();
        move2DescriptionText.text = movement.MoveSO.EffectDescription;
        if (movement.MoveSO.Damage < 1)
        {
            move2DamageText.enabled = false;
            move2DamageImage.enabled = false;
        }
    }

    private void ActivateMoveText1(bool activate)
    {
        move1NameText.enabled = activate;
        move1EnergyImage.enabled = activate;
        move1EnergyCostText.enabled = activate;
        move1DamageImage.enabled = activate;
        move1DamageText.enabled = activate;
        move1DescriptionText.enabled = activate;
    }
    private void ActivateMoveText2(bool activate)
    {
        move2NameText.enabled = activate;
        move2EnergyImage.enabled = activate;
        move2EnergyCostText.enabled = activate;
        move2DamageImage.enabled = activate;
        move2DamageText.enabled = activate;
        move2DescriptionText.enabled = activate;
    }

    private void ActivateHeroStats(bool activate)
    {
        healtText.enabled = activate;
        defenceText.enabled = activate;
        speedText.enabled = activate;
        energyText.enabled = activate;
        classIcon.enabled = activate;
        elementIcon.enabled = activate;
    }

    public void CopyCard(Card card, DuelManager duelManager)
    {
        cardSO = card.cardSO;
        copiedCard = card;
        this.duelManager = duelManager;
        SetCard();
    }

    public void CleanCard()
    {
        cardSO = null;
        copiedCard = null;
        duelManager = null;
        moves.Clear();
    }

    public Card GetCopiedCard()
    {
        return copiedCard;
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

    public void AddEquipment(Card card)
    {
        equipmentCard[Array.FindIndex(equipmentCard, c => c == null)] = card;
        card.isVisible = true;
        card.transform.parent = transform;
        card.transform.localScale = Vector3.one;
        StartCoroutine(card.MoveToPosition(Vector3.back * -0.05f, cardMovementSpeed, false, true));
        card.RotateToAngle(new Vector3(90, 0, isPlayer ? 0 : 0), cardMovementSpeed, false);
        card.SetSortingOrder(0);
        card.SetEquipmentOwner(this);
        if(card.moves.Count > 0)
        {
            foreach (var move in card.moves)
            {
                moves.Add(move);
            }
        }
        if(card.cardSO is EquipmentCardSO equipment)
        { 
            if(equipment.Effect != null && !equipment.Effect.isPassive) equipment.Effect.ActivateEffect(card, this); 
        }
    }

    public void SetEquipmentOwner(Card heroOwner)
    {
        this.heroOwner = heroOwner;
    }

    public int GetEquipmentCounts()
    {
        int count = 0;
        foreach (var item in equipmentCard)
        {
            if (item != null) count++;
        }

        return count;
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

            healtText.text = (currentHealt > 0 ? currentHealt : 0).ToString();
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
        ShowFront(true);
        ShowBack(true);

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        yield return moveCoroutine = StartCoroutine(MoveSmoothly(targetPosition, speed, temporalPosition, isLocal));

        ShowFront(isVisible);
        ShowBack(!isVisible);
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

    public IEnumerator ScaleCard(Vector3 targetScale, float speed, bool temporalScale)
    {
        Vector3 initialScale = transform.localScale;
        Vector3 target = temporalScale ? targetScale : Vector3.one;
        while (Vector3.Distance(transform.localScale, target) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, target, speed * Time.deltaTime);
            yield return null;
        }
        transform.localScale = target;
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
    public void Enlarge()
    {
        if (Vector3.Distance(lastPosition, transform.localPosition) < 0.2f)
        {
            duelManager.sampleCard.Enlarge(this);
        }
    }

    public void EnlargeEquipment()
    {
        for (int i = 0; i < equipmentCard.Length; i++)
        {
            if (equipmentCard[i] != null)
            {
                equipmentCard[i].StartCoroutine(MoveToPosition(focusPosition + Vector3.right * (i * 0.25f) + new Vector3(-0.25f, 0, 0), cardMovementSpeed, true, false));
                equipmentCard[i].RotateToAngle(Vector3.right * 53, cardMovementSpeed, true);
                equipmentCard[i].ChangedSortingOrder(110);
            }
        }
    }

    public void EnableActions(bool enable)
    {
        cardActions.enabled = enable;
        if (cardActions.isActiveAndEnabled)
        {
            if (moves.Count > 0)
            {
                move1Button.gameObject.SetActive(copiedCard.UsableMovement(0, duelManager.Player1Manager));
            }
            else
            {
                move1Button.gameObject.SetActive(false);
            }

            if (moves.Count > 1)
            {
                move2Button.gameObject.SetActive(copiedCard.UsableMovement(1, duelManager.Player1Manager));
            }
            else
            {
                move2Button.gameObject.SetActive(false);
            }

            if (cardSO is not HeroCardSO)
            {
                rechargeButton.gameObject.SetActive(false);
            }
        }
    }

    public bool  UsableMovement(int moveIndex, PlayerManager playerManager)
    {
        PlayerManager otherPlayerManager = playerManager == duelManager.Player1Manager ? duelManager.Player2Manager : duelManager.Player1Manager;

        return moves[moveIndex].MoveSO.EnergyCost <= playerManager.PlayerEnergy
                && (duelManager.ObtainTargets(this, moveIndex).Count > 0 || (moves[moveIndex].MoveSO.MoveType == MoveType.PositiveEffect ? !moves[moveIndex].MoveSO.NeedTarget :
                otherPlayerManager.GetFieldPositionList().All(field => field.Card == null) && moves[moveIndex].MoveSO.Damage > 0));
    }

    /// <summary>
    /// Ajusta el tamaño de los iconos, agrandandolos si esta en el campo o su tamaño original si no lo está.
    /// </summary>
    private void AdjustUIcons(bool defaultValue = false)
    {
        if (cardSO is HeroCardSO)
        {
            if (defaultValue)
            {
                healtText.gameObject.transform.localScale = Vector3.one;
                defenceText.gameObject.transform.localScale = Vector3.one;
                speedText.gameObject.transform.localScale = Vector3.one;
            }
            else if (fieldPosition != null)
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
    public void ChangedSortingOrder(int sortingOrder)
    {
        canvasFront.sortingOrder = sortingOrder;
        if(canvasBack != null) canvasBack.sortingOrder = isVisible ? sortingOrder - 1 : sortingOrder + 1;
        if (cardBorder != null) cardBorder.sortingOrder = sortingOrder;
        cardActions.sortingOrder = sortingOrder + 1;
    }

    public void ShowFront(bool showFront)
    {
        if (canvasBack != null) canvasFront.gameObject.SetActive(showFront);
    }
    public void ShowBack(bool showFront)
    {
        if(canvasBack != null) canvasBack.gameObject.SetActive(showFront);
    }

    
    private float fadeDuration = 0.3f;
    private Coroutine fadeRoutine;

    public void ShowCardBorder(bool showCardBorder)
    {
        if (cardBorder == null) return;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeAlpha(showCardBorder ? 1f : 0f));
    }
    private IEnumerator FadeAlpha(float target)
    {
        float start = borderMaterial.GetFloat("_Alpha");
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeDuration;
            float newAlpha = Mathf.Lerp(start, target, t);
            borderMaterial.SetFloat("_Alpha", newAlpha);
            yield return null;
        }

        borderMaterial.SetFloat("_Alpha", target);
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
        if (!duelManager.sampleCard.Card == this && returnLastPosition)
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
        }
        else if( cardSO is SpellCardSO spellCardSO)
        {
            if ((duelManager.GetCurrentDuelPhase() == DuelPhase.Preparation) &&
                spellCardSO.Move.EnergyCost <= playerManager.PlayerEnergy &&
                !duelManager.SettingAttackTarget &&
                duelManager.ObtainTargets(this, 0).Count > 0)
            {
                return true;
            }
        }
        else if (cardSO is EquipmentCardSO equipmentCardSO)
        {
            if ((duelManager.GetCurrentDuelPhase() == DuelPhase.Preparation) &&
                !duelManager.SettingAttackTarget && playerManager.GetAllCardInField().Count > 0)
            {
                foreach (var card in playerManager.GetAllCardInField())
                {
                    HeroCardSO HCSO = card.cardSO as HeroCardSO;
                    if (equipmentCardSO.SupportedClasses.Contains(HCSO.HeroClass) && !card.IsEquipped(equipmentCardSO.EquipmentType))
                    {
                        return true;
                    }
                }
                
                return false;
            }
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
    public void SetTurn()
    {
        isPlayer = IsControlledByPlayer();
        isMyTurn = true;

        fieldPosition.ChangeEmission(isPlayer ? fieldPosition.PlayerTurnColor : fieldPosition.TurnColor);

        ActivatePassiveSkills(PassiveSkillActivationPhase.StartOfTurn);

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
        duelManager.UseMovement(cardSO is EquipmentCardSO e ? movementNumber + 3 : movementNumber, cardSO is EquipmentCardSO eq ? copiedCard.HeroOwner: copiedCard);
        cardActions.enabled = false;
        duelManager.sampleCard.ResetSize();
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
        if (isMyTurn)
        {
            fieldPosition.ChangeEmission(isPlayer ? fieldPosition.PlayerTurnColor : fieldPosition.TurnColor);
        }
        else
        {
            fieldPosition.RestoreOriginalColor();
        }

        isAttackable = false;
    }

    public IEnumerator AttackAnimation(int player, Card cardToAttack, Movement movement)
    {
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
            Instantiate(movement.MoveSO.VisualEffectHit, transform.position, Quaternion.identity);
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
        if (attacker != null)
        {
            if (IsBurned()) amountDamage += 10;

            if (attacker.cardSO is HeroCardSO && HasFullDamageReflection())
            {
                attacker.ReceiveDamage(amountDamage, ignoredDefense, null);
            }
        }

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
        if (amountDamage < 0) amountDamage = 0;

        ignoredDefense = Mathf.Min(ignoredDefense, amountDamage);
        int remainingDamage = ReceiveDamageToShield(amountDamage - ignoredDefense) + ignoredDefense;

        if(remainingDamage > 0)
        {
            damageInflicted = currentHealt > remainingDamage ? remainingDamage : currentHealt;
            ShowTextDamage(false, damageInflicted);
            currentHealt -= remainingDamage;
        }

        MoveToLastPosition();

        UpdateText();

        return damageInflicted;
    }

    public void ReceivePoisonDamage(int amount)
    {
        int damageInflicted = currentHealt > amount ? amount : currentHealt;
        ShowTextDamage(false, damageInflicted);
        currentHealt -= amount;
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
    public void ToGraveyard(Graveyard graveyard)
    {
        foreach (var equipment in equipmentCard)
        {
            if (equipment != null)
            {
                equipment.transform.parent = graveyard.gameObject.transform;
                equipment.ToGraveyard(graveyard);
            }
        }
        equipmentCard = new Card[3];
        CancelAllEffects();
        AdjustUIcons(true);
        currentDefense = defense;
        currentHealt = maxHealt;
        UpdateText();
        AudioManager.Instance.PlayCardDestroy();
        this.graveyard = graveyard;
        graveyard.AddCard(this);
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
            if (stunEffect != null) stunEffect.SetActive(false);
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
        if(stunEffect != null)stunEffect.SetActive(IsStunned());
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
        if (statModifier.Any(x => x.MoveEffect is NoHealing))amount = 0;

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
        if(amount <= 0) return;

        var position = healtText.transform.position;
        position += Vector3.up;

        var popUp = Instantiate(energyPopUpPrefab, position, Quaternion.identity);
        var text = popUp.GetComponentInChildren<TextMeshProUGUI>();
        text.color = Color.green;
        text.text = $"+{amount}";
    }

    public void RechargeEnergy(int amount)
    {
        var player = IsControlled() ? duelManager.GetOpposingPlayerManager(duelManager.GetPlayerManagerForCard(this)) : duelManager.GetPlayerManagerForCard(this);
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
        ReceivePoisonDamage(amount);
        ActivateVisualEffects();
    }

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
                var poison = poisonedcounterattack.PoisonEffect;
                poison.ActivateEffect(item.casterHero, card);
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

    public bool IsInLethargy()
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
    public int GetAttackModifier()
    {
        int attackModifier = 0;

        if(statModifier == null) return attackModifier;

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

    public Card GetController()
    {
        if(statModifier == null) return null;

        foreach (Effect effect in statModifier)
        {
            if (effect.MoveEffect is HeroControl heroControl) return heroControl.Caster;
        }

        return null;
    }

    public Effect GetControllerEffect()
    {
        foreach (Effect effect in statModifier)
        {
            if (effect.MoveEffect is HeroControl heroControl) return effect;
        }

        return null;
    }

    /// <summary>
    /// Devuelve true si esta carta está siendo controlada por el efecto de control.
    /// </summary>
    /// <returns></returns>
    public bool IsControlled()
    {
        if (statModifier == null) return false;

        foreach (Effect effect in statModifier)
        {
            if (effect.MoveEffect is HeroControl)
            {
                return true;
            }
        }
        return false;
    }
    private bool IsControlledByPlayer()
    {
        Card controller = null;
        foreach (Effect effect in statModifier)
        {
            if (effect.MoveEffect is HeroControl)
            {
                controller = effect.casterHero;
                break;
            }
        }

        if (controller != null)
        {
            return duelManager.Player1Manager == duelManager.GetPlayerManagerForCard(controller);
        }
        else
        {
            return duelManager.Player1Manager == duelManager.GetPlayerManagerForCard(this);
        }
    }

    public bool IsParalyzed()
    {
        foreach (Effect effect in statModifier)
        {
            if (effect.MoveEffect is Paralysis)
            {
                return true;
            }
        }
        return false;
    }

    public bool IsBurned()
    {
        foreach (Effect effect in statModifier)
        {
            if (effect.MoveEffect is Burn)
            {
                return effect.IsBurned();
            }
        }
        return false;
    }



    public bool IsAvailableEquipmentSlot(EquipmentCardSO equipmentCard)
    {
        HeroCardSO HCSO = cardSO as HeroCardSO;

        return equipmentCard.SupportedClasses.Contains(HCSO.HeroClass) && !IsEquipped(equipmentCard.EquipmentType); // La carta a queipar es compatible con el heroe y no esta equipada ya otra carta del mismo tipo
    }

    public void ActivatePassiveSkills(PassiveSkillActivationPhase passiveSkillActivationPhase)
    {
        if(IsEquipped(EquipmentType.Accessory))
        {
            foreach (var item in equipmentCard)
            {
                if (item != null && item.cardSO is EquipmentCardSO equipmentCardSO)
                {
                    if (equipmentCardSO.EquipmentType == EquipmentType.Accessory)
                    {
                        if(passiveSkillActivationPhase == equipmentCardSO.Effect.passiveSkillActivationPhase)
                            equipmentCardSO.Effect.ActivateEffect(this, this);
                    }
                }
            }
        }
    }

    public bool IsEquipped(EquipmentType equipmentType)
    {
        foreach (var item in equipmentCard)
        {
            if (item != null && item.cardSO is EquipmentCardSO equipmentCardSO)
            {
                if (equipmentCardSO.EquipmentType == equipmentType)
                {
                    return true;
                }
            }
        }

        return false;
    }
}

