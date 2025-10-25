using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;


[CreateAssetMenu(fileName = "Recharge", menuName = "Hemera Legions/Card Effects/ Recharge")]
public class Recharge : CardEffect
{
    [SerializeField] private int amount;
    private Card caster;

    [SerializeField] private float duration = 3f;
    [SerializeField] private float archHeight = 1.5f;
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private GameObject sparkPrefab;
    public override void ActivateEffect(Card caster, Card target)
    {
        GameObject p = Instantiate(particlePrefab, caster.transform.position, Quaternion.identity);
        CorrutinaHelper.Instancia.EjecutarCorrutina(MoveInArc(p, caster));
    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {
        
    }

    public override void DeactivateEffect(Effect effect)
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateEffect(Effect effect)
    {
        throw new System.NotImplementedException();
    }

    IEnumerator MoveInArc(GameObject p, Card caster)
    {
        float tiempo = 0f;

        var target = caster.DuelManager.GetPlayerManagerForCard(caster).GetHandCardHandler().transform.position;

        while (tiempo < duration)
        {
            float t = tiempo / duration;
            
            // Lerp base
            Vector3 punto = Vector3.Lerp(caster.transform.position, target, t);

            // Altura parabólica (arco)
            float altura = Mathf.Sin(t * Mathf.PI) * archHeight;
            punto.y += altura;

            p.transform.position = punto;

            tiempo += Time.deltaTime;
            yield return null;
        }

        p.transform.position = target;
        Destroy(p);

        Instantiate(sparkPrefab, target, Quaternion.identity);

        this.caster = caster;

        caster.RechargeEnergy(amount + caster.GetEnergyBonus());
    }
}
