using System.Collections;
using UnityEngine;

public class CorrutinaHelper : MonoBehaviour
{
    public static CorrutinaHelper Instancia;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (Instancia == null)
        {
            GameObject helperObject = new GameObject("CorrutinaHelper");
            Instancia = helperObject.AddComponent<CorrutinaHelper>();
            DontDestroyOnLoad(helperObject);
        }
    }

    public void EjecutarCorrutina(IEnumerator corrutina)
    {
        StartCoroutine(corrutina);
    }
}
