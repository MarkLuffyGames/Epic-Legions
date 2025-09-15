using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.EventSystems.EventTrigger;

public class CardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI lastNameText;

    [SerializeField] private TextMeshProUGUI healtText;
    [SerializeField] private TextMeshProUGUI defenseText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI energyText;
    [SerializeField] private TextMeshProUGUI description;
    [SerializeField] private Image cardImage;
    [SerializeField] private Image classIcon;
    [SerializeField] private Image elementIcon;
    [SerializeField] private MovementUI movementUI1;
    [SerializeField] private MovementUI movementUI2;

    List<Movement> moves = new List<Movement>();
    public void SetCard(CardSO cardSO)
    {
        nameText.text = cardSO.CardName;
        lastNameText.text = cardSO.CardLastName;

        cardImage.sprite = cardSO.CardSprite;

        if (cardSO is HeroCardSO heroCardSO)
        {
            foreach (var move in heroCardSO.Moves)
            {
                moves.Add(new Movement(move));
                //if (move.AlwaysActive) moves[moves.Count - 1].ActivateEffect(this, this);
            }

            classIcon.sprite = CardDatabase.GetClassIcon(heroCardSO.HeroClass);
            elementIcon.sprite = CardDatabase.GetElementIcon(heroCardSO.CardElemnt);

            ActivateHeroStats(true);
            description.text = "";

            healtText.text = heroCardSO.Healt.ToString();
            defenseText.text = heroCardSO.Defence.ToString();
            speedText.text = heroCardSO.Speed.ToString();
            energyText.text = heroCardSO.Energy.ToString();
        }
        else if (cardSO is SpellCardSO spellCardSO)
        {
            moves.Add(new Movement(spellCardSO.Move));
            description.text = spellCardSO.Move.EffectDescription;
            ActivateHeroStats(false);
        }
        else if (cardSO is EquipmentCardSO equipmentCardSO)
        {
            foreach (var move in equipmentCardSO.Moves)
            {
                moves.Add(new Movement(move));
            }

            ActivateHeroStats(false);

            elementIcon.sprite = CardDatabase.GetElementIcon(equipmentCardSO.CardElemnt);
            elementIcon.enabled = true;
            description.text = equipmentCardSO.Description;
        }


        movementUI1.SetMoveUI(moves.Count > 0 ? moves[0] : null, moves.Count < 2, 1);
        movementUI2.SetMoveUI(moves.Count > 1 ? moves[1] : null, moves.Count < 2, 2);
    }

    private void ActivateHeroStats(bool activate)
    {
        healtText.enabled = activate;
        defenseText.enabled = activate;
        speedText.enabled = activate;
        energyText.enabled = activate;
        classIcon.enabled = activate;
        elementIcon.enabled = activate;
    }
}
