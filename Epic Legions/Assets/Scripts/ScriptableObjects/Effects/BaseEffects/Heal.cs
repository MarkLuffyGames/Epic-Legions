using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Heal", menuName = "Hemera Legions/Card Effects/ Heal")]
public class Heal : CardEffect
{
    [SerializeField] private int amount;

    [SerializeField] private float duration = 3f;
    [SerializeField] private float archHeight = 1.5f;
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private GameObject arcPrefab;
    [SerializeField] private GameObject receivePrefab;

    public int Amount => amount;
    public override void ActivateEffect(Card caster, Card target)
    {
        if(particlePrefab != null && arcPrefab != null && receivePrefab != null)
        {
            Instantiate(particlePrefab, caster.FieldPosition.transform.position + Vector3.up * 0.5f, Quaternion.identity);

            GameObject p = Instantiate(arcPrefab, caster.transform.position, Quaternion.identity);
            CorrutinaHelper.Instancia.EjecutarCorrutina(MoveInArc(p, caster, target));
        }
        else
        {
            if (visualEffectCardEffect)
                Instantiate(visualEffectCardEffect, target.FieldPosition.transform.position, Quaternion.identity);

            target.ToHeal(amount);
        }

    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {
        if (particlePrefab != null && arcPrefab != null && receivePrefab != null)
        {
            Instantiate(particlePrefab, caster.FieldPosition.transform.position + Vector3.up * 0.5f, Quaternion.identity);

            foreach (Card card in target)
            {
                GameObject p = Instantiate(arcPrefab, caster.transform.position, Quaternion.identity);
                CorrutinaHelper.Instancia.EjecutarCorrutina(MoveInArc(p, caster, card));
            }
        }
        else
        {
            foreach (Card card in target)
            {
                card.ToHeal(amount);
            }
        }
        
    }

    public override void ActivateEffect(SimCardState caster, SimCardState target)
    {
        target.ToHeal(amount);
    }

    public override void DeactivateEffect(Effect effect)
    {
        throw new System.NotImplementedException();
    }

    public override void UpdateEffect(Effect effect, SimCardState simCardState)
    {
        throw new System.NotImplementedException();
    }

    IEnumerator MoveInArc(GameObject p, Card caster, Card target)
    {
        float tiempo = 0f;

        while (tiempo < duration)
        {
            float t = tiempo / duration;

            // Lerp base
            Vector3 punto = Vector3.Lerp(caster.transform.position, target.transform.position, t);

            // Altura parabólica (arco)
            float altura = Mathf.Sin(t * Mathf.PI) * archHeight;
            punto.y += altura;

            p.transform.position = punto;

            tiempo += Time.deltaTime;
            yield return null;
        }

        p.transform.position = target.transform.position;
        Destroy(p);

        Instantiate(receivePrefab, target.transform.position, Quaternion.identity);

        yield return new WaitForSeconds(0.5f);

        Instantiate(visualEffectCardEffect, target.FieldPosition.transform.position, Quaternion.identity);
        target.ToHeal(amount);
    }
}
