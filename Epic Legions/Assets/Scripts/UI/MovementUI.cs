using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MovementUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moveNameText;
    [SerializeField] private Image moveEnergyImage;
    [SerializeField] private TextMeshProUGUI moveEnergyCostText;
    [SerializeField] private Image moveDamageImage;
    [SerializeField] private TextMeshProUGUI moveDamageText;
    [SerializeField] private TextMeshProUGUI moveDescriptionText;
    [SerializeField] private Button moveButton;

    public void SetMoveUI(Movement movement, bool center, int moveNumber)
    {
        moveNameText.enabled = movement != null;
        moveEnergyImage.enabled = movement != null;
        moveEnergyCostText.enabled = movement != null;
        moveDamageImage.enabled = movement != null;
        moveDamageText.enabled = movement != null;
        moveDescriptionText.enabled = movement != null;

        if (movement == null) return;

        if (center)
        {
            moveNameText.rectTransform.localPosition = new Vector3(-0.048f, -0.601f, -0.0001f);
            if(moveButton) moveButton.transform.localPosition = new Vector3(0.0123f, -0.63f, 0.0f);
        }
        else
        {
            moveNameText.rectTransform.localPosition = new Vector3(-0.048f, moveNumber == 1 ? - 0.499f : -0.654f, -0.0001f);
            if (moveButton) moveButton.transform.localPosition = new Vector3(0.0123f, moveNumber == 1 ? -0.544f : -0.704f, 0.0f);
        }

        moveNameText.text = movement.MoveSO.MoveName;
        moveEnergyCostText.text = movement.MoveSO.EnergyCost.ToString();
        moveDamageText.text = movement.MoveSO.Damage.ToString();
        moveDescriptionText.text = movement.MoveSO.EffectDescription;
        if (movement.MoveSO.Damage == 0)
        {
            moveDamageText.enabled = false;
            moveDamageImage.enabled = false;
        }
        else if(movement.MoveSO.Damage == -1)
        {
            moveDamageText.enabled = false;
            moveDamageImage.sprite = CardDatabase.GetMoveTypeIcon(movement.MoveSO.MoveType);
        }
        else
        {
            moveDamageImage.sprite = CardDatabase.GetMoveTypeIcon(movement.MoveSO.MoveType);
        }
    }

    public void SetButtonInteractable(bool interactable)
    {
        moveButton.gameObject.SetActive(interactable);
    }

    public void DisableButton()
    {
        moveButton.interactable = false;
    }

    public void EnableButton()
    {
        moveButton.interactable = true;
    }
}
