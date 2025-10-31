using UnityEngine;

public class EffectHandler : MonoBehaviour
{
    [SerializeField] private GameObject effect;
    private void OnEnable()
    {
        if (effect != null) effect.SetActive(true);
    }

    private void OnDisable()
    {
        if (effect != null) effect.SetActive(false);
    }

    public void SetEffect(GameObject effect)
    {
        this.effect = effect;
    }
}
