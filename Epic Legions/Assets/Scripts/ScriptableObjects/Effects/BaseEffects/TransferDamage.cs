using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CreateAssetMenu(fileName = "Transfer Damage", menuName = "Hemera Legions/Card Effects/ Transfer Damage")]
public class TransferDamage : CardEffect
{

    [SerializeField] private int numberTurns;
    private Card caster;

    [SerializeField] private float duration = 3f;
    [SerializeField] private float archHeight = 1.5f;
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private GameObject sparkPrefab;

    [SerializeField]  public int NumberTurns => numberTurns * DuelManager.NumberOfTurns;
    public Card Caster => caster;
    public override void ActivateEffect(Card caster, Card target)
    {
        Instantiate(visualEffectCardEffect, caster.FieldPosition.transform.position + Vector3.up * 0.1f, Quaternion.identity);

        GameObject p = Instantiate(particlePrefab, caster.transform.position, Quaternion.identity);
        CorrutinaHelper.Instancia.EjecutarCorrutina(MoveInArc(p, caster, target));
    }

    public override void ActivateEffect(Card caster, List<Card> target)
    {
        Instantiate(visualEffectCardEffect, caster.FieldPosition.transform.position + Vector3.up * 0.1f, Quaternion.identity);
        foreach (Card card in target)
        {
            GameObject p = Instantiate(particlePrefab, caster.transform.position, Quaternion.identity);
            CorrutinaHelper.Instancia.EjecutarCorrutina(MoveInArc(p, caster, card));
        }
    }

    public override void UpdateEffect(Effect effect, SimCardState simCardState)
    {
        effect.durability--;
        effect.SetEffectDescription(DescriptionText(effect));
    }

    public override void DeactivateEffect(Effect effect)
    {
        effect.durability = 0;
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

        Instantiate(sparkPrefab, target.transform.position, Quaternion.identity);

        this.caster = caster;
        target.AddEffect(new Effect(this, target));
    }

    public string DescriptionText(Effect effect)
    {
        return $"Transfer all damage received to {caster.cardSO.CardName} for {effect.Durability} turn{(effect.Durability > 1 ? "s" : "")}";
    }

    public override void ActivateEffect(SimCardState caster, SimCardState target)
    {
        target.AddEffect(new Effect(this, target.OriginalCard));
    }
}