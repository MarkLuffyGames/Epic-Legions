using UnityEngine;

[CreateAssetMenu(fileName = "IDGenerator", menuName = "Hemera Legions/IDGenerator")]
public class IDGenerator : ScriptableObject
{
    public int lastId = 1000;

    public int ID()
    {
        lastId++;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif

        return lastId;
    }
}
