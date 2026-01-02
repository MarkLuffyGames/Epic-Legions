using UnityEngine;

[CreateAssetMenu(fileName = "IDGenerator", menuName = "Hemera Legions/IDGenerator")]
public class IDGenerator : ScriptableObject
{
    public int CardLastId = 1000;
    public int MoveLastId = 1000;
    public int EffectLastId = 1000;

    public int CardID()
    {
        CardLastId++;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif

        return CardLastId;
    }

    public int MoveID()
    {
        MoveLastId++;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif

        return MoveLastId;
    }

    public int EffectID()
    {
        EffectLastId++;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif

        return EffectLastId;
    }
}
