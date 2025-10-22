using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "pattern", menuName = "Game of Life/Pattern")]
public class Pattern : ScriptableObject {
    public Vector2Int[] cells;
}
