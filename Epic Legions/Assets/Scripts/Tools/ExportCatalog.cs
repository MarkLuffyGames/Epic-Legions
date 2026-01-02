using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


public static class ExportCatalog
{
# if UNITY_EDITOR
    private static readonly string OutputDir =
        @"D:\MarkLuffyGames\Projects\hemera-backend\Hemera.Server\Seed";

    [MenuItem("Tools/Hemera Legion/Export Effects + Moves JSON")]
    public static void ExportEffectsAndMoves()
    {
        Directory.CreateDirectory(OutputDir);

        // 1) EFFECTS
        var effects = LoadAll<CardEffect>();
        var effRows = new List<EffectRow>();
        foreach (var eff in effects)
        {
            var id = eff.Id;
            var row = new EffectRow
            {
                id = id,
                className = eff.GetType().Name,
                @params = SerializeEffect(eff) // diccionario con parámetros
            };
            effRows.Add(row);
        }
        WriteJson(Path.Combine(OutputDir, "effects.json"), effRows);

        // 2) MOVES
        var moves = LoadAll<MoveSO>();
        var mvRows = new List<MoveRow>();
        foreach (var mv in moves)
        {
            mvRows.Add(new MoveRow
            {
                id = mv.Id,
                name = mv.MoveName,
                damage = mv.Damage,
                energyCost = mv.EnergyCost,
                element = mv.Element.ToString(), // o (int)mv.Element
                needTarget = mv.NeedTarget,
                onMyself = mv.OnMyself,
                moveType = (int)mv.MoveType,
                targetsType = (int)mv.TargetsType,
                effectId = mv.MoveEffect.Id, // si el Move referencia un CardEffect
                effectParams = null // si quisieras overrides por movimiento
            });
        }
        WriteJson(Path.Combine(OutputDir, "moves.json"), mvRows);

        AssetDatabase.Refresh();
        Debug.Log($"Export OK -> {OutputDir}");
    }

    // ===== helpers =====


    private static Dictionary<string, object> SerializeEffect(CardEffect eff)
    {
        var p = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        // Multiplicador de turnos (exporta “turnos efectivos” para no depender del server)
        int NT(int baseTurns) => baseTurns * DuelManager.NumberOfTurns;

        switch (eff)
        {
            case AbsorbDamage a:
                p["amount"] = a.Amount;
                p["turns"] = NT(a.NumberTurns / DuelManager.NumberOfTurns); // deja “ticks” efectivos
                break;

            case ModifyAttack m:
                p["amount"] = m.Amount;
                p["turns"] = NT(m.NumberTurns / DuelManager.NumberOfTurns);
                p["isIncrease"] = m.IsIncrease;
                break;

            case Stun _:
                p["turns"] = NT(1);
                break;

            case FullDamageReflection r:
                p["turns"] = NT(r.NumberTurns / DuelManager.NumberOfTurns);
                break;

            case Heal h:
                p["amount"] = /* tu getter del amount en Heal */ GetPrivateInt(h, "amount");
                break;

            case Burn _:
                // sin params
                break;

            default:
                // Si tienes más efectos, agrega aquí sus params mínimos
                break;
        }

        return p;
    }

    // --- utilidades de export ---

    private static void WriteJson<T>(string path, List<T> rows)
    {
        var env = new Envelope<T> { version = 1, exportedAt = DateTime.UtcNow.ToString("o"), items = rows };
        var json = JsonUtility.ToJson(env, true);
        File.WriteAllText(path, json);
    }

    private static List<T> LoadAll<T>() where T : UnityEngine.Object
    {
        var list = new List<T>();
        foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var obj = AssetDatabase.LoadAssetAtPath<T>(path);
            if (obj) list.Add(obj);
        }
        return list;
    }

    [Serializable] private class Envelope<T> { public int version; public string exportedAt; public List<T> items; }
    [Serializable] private class EffectRow { public int id; public string className; public Dictionary<string, object> @params; }
    [Serializable]
    private class MoveRow
    {
        public int id; public string name; public int damage; public int energyCost;
        public string element; public bool needTarget; public bool onMyself;
        public int moveType; public int targetsType;
        public int effectId; public Dictionary<string, object> effectParams;
    }

    // Si necesitas leer campos privados (como amount en Heal), puedes usar SerializedObject o reflection.
    private static int GetPrivateInt(object obj, string fieldName)
    {
        var f = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return f != null ? (int)f.GetValue(obj) : 0;
    }
#endif
}
