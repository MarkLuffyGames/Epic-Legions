using UnityEditor;
using UnityEngine;

public enum CardElement { None, Fire, Water, Plant, Earth, Lightning, Wind, Light, Darkness }

public enum CardType { Heroes, Spells, Artifacts, Field }
public class CardSO : ScriptableObject
{
    [SerializeField] private int cardId;
    [SerializeField] public string cardName;
    [SerializeField] private string cardLastName;
    [SerializeField] public Sprite cardSprite;
    [SerializeField] private CardElement cardElemnt;

    public int CardID => cardId;
    public string CardName => cardName;
    public string CardLastName => cardLastName;
    public Sprite CardSprite => cardSprite;
    public CardElement CardElemnt => cardElemnt;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (cardId == 0)
        {
            var generator = AssetDatabase.LoadAssetAtPath<IDGenerator>("Assets/Scripts/ScriptableObjects/IDGenerator/IDGenerator.asset");
            if (generator != null)
            {
                cardId = generator.ID();
                EditorUtility.SetDirty(this);
                Debug.Log($"Card ID set to {cardId} for {cardName} {cardLastName}");
            }
            else
            {
                Debug.LogWarning("IDGenerator not found. Please create an IDGenerator asset at Assets/Scripts/ScriptableObjects/IdGenerator.asset");
            }
        }
    }
#endif

    private static readonly int[,] typeChart =
    {
        //         NON FIR WAT GRA GRO ELE WIN LIG DAR 
        /* NON */ {  0,  0,  0,  0,  0,  0,  0,  0,  0},
        /* FIR */ {  0,  0,-20, 20,  0,  0,  0,  0,  0},
        /* WAT */ {  0, 20,  0,-20,  0,  0,  0,  0,  0},
        /* GRA */ {  0,-20, 20,  0,  0,  0,  0,  0,  0},
        /* GRO */ {  0,  0,  0,  0,  0, 20,-20,  0,  0},
        /* ELE */ {  0,  0,  0,  0,-20,  0, 20,  0,  0},
        /* WIN */ {  0,  0,  0,  0, 20,-20,  0,  0,  0},
        /* LIG */ {  0,  0,  0,  0,  0,  0,  0,  0, 20},
        /* DAR */ {  0,  0,  0,  0,  0,  0,  0, 20,  0},
    };

    public static int GetEffectiveness(CardElement attacker, CardElement defender)
    {
        return typeChart[(int)attacker, (int)defender];
    }
}
