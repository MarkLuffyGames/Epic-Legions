using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardEffectActivated : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI descriptionText;
    public void SetCardEffect(Effect cardEffect)
    {
        iconImage.sprite = cardEffect.MoveEffect.iconSprite;
        descriptionText.text = cardEffect.GetEffectDescription();
    }
}
