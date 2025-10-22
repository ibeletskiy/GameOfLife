// Assets/Editor/SavePatternFromScene.cs
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class SavePatternFromScene
{
    [MenuItem("Game of Life/Save Current Pattern From Scene...")]
    public static void SaveFromScene()
    {
        GameField field = null;
        if (Selection.activeGameObject != null)
            field = Selection.activeGameObject.GetComponent<GameField>();
        if (field == null)
            field = Object.FindObjectOfType<GameField>(true);

        if (field == null)
        {
            return;
        }

        var currentMap = GetPrivateField<Tilemap>(field, "current");
        var aliveTile  = GetPrivateField<Tile>(field, "aliveTile");

        if (currentMap == null || aliveTile == null)
        {
            return;
        }
        
        var bounds = currentMap.cellBounds;
        var alive = new List<Vector2Int>();
        foreach (var pos in bounds.allPositionsWithin)
        {
            if (currentMap.GetTile(pos) == aliveTile)
                alive.Add(new Vector2Int(pos.x, pos.y));
        }

        if (alive.Count == 0)
        {
            return;
        }
        
        bool center = EditorUtility.DisplayDialog(
            "Game of Life",
            $"Центрировать координаты к (0,0)?",
            "Да", "Нет"
        );

        if (center)
        {
            int mx = Median(alive.Select(v => v.x));
            int my = Median(alive.Select(v => v.y));
            for (int i = 0; i < alive.Count; i++)
                alive[i] = new Vector2Int(alive[i].x - mx + 3, alive[i].y - my + 3);
        }
        
        EnsureFolder("Assets/Patterns");
        string path = EditorUtility.SaveFilePanelInProject(
            "Сохранить Pattern",
            "pattern",
            "asset",
            "Place",
            "Assets/Patterns"
        );
        if (string.IsNullOrEmpty(path)) return;

        var asset = ScriptableObject.CreateInstance<Pattern>();
        asset.cells = alive.ToArray();

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;

        EditorUtility.DisplayDialog("Game of Life",
            $"Pattern сохранён: {path}\n", "OK");
    }
    
    static T GetPrivateField<T>(Object obj, string fieldName) where T : Object
    {
        var fi = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        return fi?.GetValue(obj) as T;
    }

    static int Median(IEnumerable<int> values)
    {
        var list = values.OrderBy(v => v).ToList();
        int n = list.Count;
        return (n % 2 == 1) ? list[n / 2] : Mathf.RoundToInt((list[n / 2 - 1] + list[n / 2]) * 0.5f);
    }

    static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;
        var parts = folderPath.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{cur}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }
}
