using System.Collections;
using UnityEngine;

public class CorrutinaHelper : MonoBehaviour
{
    public static CorrutinaHelper Instancia;

    void Awake()
    {
        Instancia = this;
    }

    public void EjecutarCorrutina(IEnumerator corrutina)
    {
        StartCoroutine(corrutina);
    }
}
