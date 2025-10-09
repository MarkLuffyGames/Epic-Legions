using UnityEngine;

public class EffectHandler : MonoBehaviour
{
    [SerializeField] private GameObject effect;
    private void OnEnable()
    {
        effect.SetActive(true);
    }

    private void OnDisable()
    {
        effect.SetActive(false);
    }
}
