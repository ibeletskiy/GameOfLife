using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class GameField : MonoBehaviour
{
    [SerializeField] private Tilemap current;
    [SerializeField] private Tilemap next;
    [SerializeField] private Tile aliveTile;
    [SerializeField] private Tile deadTile;
    [SerializeField] private Pattern pattern;
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private float fillProbability = 0.25f;
    [SerializeField] private bool running = true;
    [SerializeField] private Camera cam;
    
    [SerializeField] private float panSpeed = 1f;
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minZoom = 10f;
    [SerializeField] private float maxZoom = 100f;
    
    [SerializeField] private bool centerOnStart = true;
    [SerializeField] private bool refitOnResize = true;
    [SerializeField] private float paddingCells = 1.5f;
    [SerializeField] private float fitSafety = 0.95f;

    public float UpdateInterval { get => updateInterval; set => updateInterval = Mathf.Max(0.01f, value); }
    public bool IsRunning => running;

    private HashSet<Vector3Int> aliveCells;
    private HashSet<Vector3Int> cellsToCheck;
    private BoundsInt _lastVisibleBounds;
    private Vector3 _lastMouseScreen;
    private int _lastScreenW, _lastScreenH;
    private bool _wasAutoCentered;

    void Awake()
    {
        aliveCells = new HashSet<Vector3Int>();
        cellsToCheck = new HashSet<Vector3Int>();
        if (cam == null) cam = Camera.main;
        if (cam != null) cam.orthographic = true;
        _lastScreenW = Screen.width;
        _lastScreenH = Screen.height;
    }

    void Start()
    {
        SetPattern(pattern);
        if (centerOnStart) { CenterOnAliveAndFit(true); _wasAutoCentered = true; }
        DrawVisibleDeadCells();
        StartCoroutine(Simulate());
    }
    
    bool PointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    void Update()
    {
        if (refitOnResize && (Screen.width != _lastScreenW || Screen.height != _lastScreenH))
        {
            _lastScreenW = Screen.width; _lastScreenH = Screen.height;
            if (_wasAutoCentered) CenterOnAliveAndFit(false); else FitAliveKeepCenter();
        }

        HandleMouseToggle();
        HandleCameraPanZoom();
        DrawVisibleDeadCells();
    }

    public void SetPattern(Pattern p)
    {
        Clear();
        aliveCells.Clear();
        if (p != null && p.cells != null)
        {
            for (int i = 0; i < p.cells.Length; i++)
            {
                Vector3Int cell = (Vector3Int)p.cells[i];
                current.SetTile(cell, aliveTile);
                aliveCells.Add(cell);
            }
        }
    }

    public void Clear()
    {
        current.ClearAllTiles();
        next.ClearAllTiles();
        aliveCells.Clear();
        _lastVisibleBounds = new BoundsInt(int.MaxValue, int.MaxValue, 0, 1, 1, 1);
        DrawVisibleDeadCells();
    }

    IEnumerator Simulate()
    {
        while (true)
        {
            if (running)
            {
                UpdateState();
                yield return new WaitForSeconds(updateInterval);
            }
            else yield return null;
        }
    }

    int CountAliveCells(Vector3Int position)
    {
        int count = 0;
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            if (aliveCells.Contains(position + new Vector3Int(dx, dy, 0))) count++;
        }
        return count;
    }

    void UpdateCellsToCheck(Vector3Int position)
    {
        cellsToCheck.Add(position);
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            cellsToCheck.Add(position + new Vector3Int(dx, dy, 0));
        }
    }
    
    void UpdateState()
    {
        cellsToCheck.Clear();
        foreach (var cell in aliveCells) UpdateCellsToCheck(cell);

        var newAlive = new HashSet<Vector3Int>();

        foreach (var cell in cellsToCheck)
        {
            int count = CountAliveCells(cell);
            bool alive = aliveCells.Contains(cell);

            if (!alive && count == 3)
            {
                newAlive.Add(cell);
                next.SetTile(cell, aliveTile);
            }
            else if (alive && (count == 2 || count == 3))
            {
                newAlive.Add(cell);
                next.SetTile(cell, aliveTile);
            }
            else
            {
                next.SetTile(cell, deadTile);
            }
        }

        (current, next) = (next, current);
        next.ClearAllTiles();
        aliveCells = newAlive;

        _lastVisibleBounds = new BoundsInt(int.MaxValue, int.MaxValue, 0, 1, 1, 1);
        DrawVisibleDeadCells();
    }

    void HandleMouseToggle()
    {
        if (!Input.GetMouseButtonDown(0) || cam == null) return;
        if (PointerOverUI()) return;

        var world = cam.ScreenToWorldPoint(Input.mousePosition);
        world.z = current.transform.position.z;
        var cell = current.WorldToCell(world);

        if (aliveCells.Contains(cell))
        {
            aliveCells.Remove(cell);
            current.SetTile(cell, deadTile);
        }
        else
        {
            aliveCells.Add(cell);
            current.SetTile(cell, aliveTile);
        }
        _lastVisibleBounds = new BoundsInt(int.MaxValue, int.MaxValue, 0, 1, 1, 1);
    }

    void HandleCameraPanZoom()
    {
        if (cam == null) return;

        if (!PointerOverUI())
        {
            if (Input.GetMouseButtonDown(1))
                _lastMouseScreen = Input.mousePosition;

            if (Input.GetMouseButton(1))
            {
                var prevWorld = cam.ScreenToWorldPoint(_lastMouseScreen);
                var currWorld = cam.ScreenToWorldPoint(Input.mousePosition);
                var delta = prevWorld - currWorld;
                cam.transform.position += delta * panSpeed;
                _lastMouseScreen = Input.mousePosition;
                _lastVisibleBounds = new BoundsInt(int.MaxValue, int.MaxValue, 0, 1, 1, 1);
                _wasAutoCentered = false;
            }
        }

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.0001f && !PointerOverUI())
        {
            var before = cam.ScreenToWorldPoint(Input.mousePosition);
            float size = cam.orthographicSize * Mathf.Exp(scroll * zoomSpeed * 0.1f);
            cam.orthographicSize = Mathf.Clamp(size, minZoom, maxZoom);
            var after = cam.ScreenToWorldPoint(Input.mousePosition);
            cam.transform.position += (before - after);
            _lastVisibleBounds = new BoundsInt(int.MaxValue, int.MaxValue, 0, 1, 1, 1);
            _wasAutoCentered = false;
        }
    }

    public void DrawVisibleDeadCells()
    {
        if (cam == null) return;
        var bounds = GetVisibleCellBounds(cam, current);
        if (bounds.Equals(_lastVisibleBounds)) return;
        _lastVisibleBounds = bounds;

        foreach (var pos in bounds.allPositionsWithin)
        {
            if (current.GetTile(pos) != aliveTile)
            {
                if (current.GetTile(pos) != deadTile)
                    current.SetTile(pos, deadTile);
            }
        }
    }

    static BoundsInt GetVisibleCellBounds(Camera cam, Tilemap map)
    {
        float z = map.transform.position.z;
        var min = cam.ViewportToWorldPoint(new Vector3(0f, 0f, cam.nearClipPlane));
        var max = cam.ViewportToWorldPoint(new Vector3(1f, 1f, cam.nearClipPlane));
        min.z = z; max.z = z;
        var cmin = map.WorldToCell(min);
        var cmax = map.WorldToCell(max);
        int xMin = Mathf.Min(cmin.x, cmax.x) - 1;
        int yMin = Mathf.Min(cmin.y, cmax.y) - 1;
        int xMax = Mathf.Max(cmin.x, cmax.x) + 1;
        int yMax = Mathf.Max(cmin.y, cmax.y) + 1;
        return new BoundsInt(xMin, yMin, 0, xMax - xMin + 1, yMax - yMin + 1, 1);
    }

    public void ToggleRunning() => running = !running;
    public void SetRunning(bool value) => running = value;

    public void RandomFillVisible()
    {
        if (cam == null) return;
        var bounds = GetVisibleCellBounds(cam, current);
        aliveCells.Clear();
        for (int x = bounds.xMin; x <= bounds.xMax; x++)
        for (int y = bounds.yMin; y <= bounds.yMax; y++)
        {
            var pos = new Vector3Int(x, y, 0);
            if (Random.value < fillProbability)
            {
                aliveCells.Add(pos);
                current.SetTile(pos, aliveTile);
            }
            else current.SetTile(pos, deadTile);
        }
    }

    void GetAliveMinMax(out Vector3Int min, out Vector3Int max)
    {
        if (aliveCells.Count == 0 && pattern != null && pattern.cells != null && pattern.cells.Length > 0)
        {
            int minX = pattern.cells.Min(c => c.x);
            int maxX = pattern.cells.Max(c => c.x);
            int minY = pattern.cells.Min(c => c.y);
            int maxY = pattern.cells.Max(c => c.y);
            min = new Vector3Int(minX, minY, 0);
            max = new Vector3Int(maxX, maxY, 0);
            return;
        }

        int miX = int.MaxValue, miY = int.MaxValue, maX = int.MinValue, maY = int.MinValue;
        foreach (var c in aliveCells)
        {
            if (c.x < miX) miX = c.x;
            if (c.y < miY) miY = c.y;
            if (c.x > maX) maX = c.x;
            if (c.y > maY) maY = c.y;
        }
        if (miX == int.MaxValue) { min = Vector3Int.zero; max = Vector3Int.zero; }
        else { min = new Vector3Int(miX, miY, 0); max = new Vector3Int(maX, maY, 0); }
    }

    void CenterOnAliveAndFit(bool alsoCenter)
    {
        if (cam == null) return;

        GetAliveMinMax(out var minCell, out var maxCell);
        int pad = Mathf.CeilToInt(paddingCells);
        var minPad = new Vector3Int(minCell.x - pad, minCell.y - pad, 0);
        var maxPad = new Vector3Int(maxCell.x + pad, maxCell.y + pad, 0);
        
        Vector3 worldMin = current.CellToWorld(minPad);
        Vector3 worldMax = current.CellToWorld(new Vector3Int(maxPad.x + 1, maxPad.y + 1, 0));

        float worldW = Mathf.Abs(worldMax.x - worldMin.x);
        float worldH = Mathf.Abs(worldMax.y - worldMin.y);

        float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        float neededHalfH = worldH * 0.5f;
        float neededHalfW = worldW * 0.5f;
        float sizeFit = Mathf.Max(neededHalfH, neededHalfW / aspect);
        
        sizeFit *= fitSafety;

        cam.orthographicSize = Mathf.Clamp(sizeFit, minZoom, maxZoom);

        if (alsoCenter)
        {
            Vector3 worldCenter = (worldMin + worldMax) * 0.5f;
            cam.transform.position = new Vector3(worldCenter.x, worldCenter.y, cam.transform.position.z);
        }

        _lastVisibleBounds = new BoundsInt(int.MaxValue, int.MaxValue, 0, 1, 1, 1);
        DrawVisibleDeadCells();
    }

    void FitAliveKeepCenter() => CenterOnAliveAndFit(false);
}
