using UnityEngine;

public class DestroySelf : MonoBehaviour
{
    [SerializeField] private float timeDestroy = 1f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Invoke("DestroyGameObject", timeDestroy);
    }

    private void DestroyGameObject()
    {
        Destroy(gameObject);
    }
}
