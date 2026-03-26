using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class ShipController : RTSActorParent
{
    [Header("Components")]
    public GameObject modelRoot;    // дочерний объект с 3D моделью
    public GameObject impostorRoot; // дочерний объект с Canvas
    public ProceduralFoundation foundation; // ссылка на компонент подложки

    private Dictionary<Vector2Int, BlockType> blocks = new Dictionary<Vector2Int, BlockType>();
    private List<Vector2Int> existingCells = new List<Vector2Int>();
    private float cellSize;
    private bool hasImpostor = false;
    private RectTransform impostorRect; // RectTransform изображения импостера
    private int maxExtensions;

    void Awake()
    {
        if (modelRoot == null)
        {
            modelRoot = new GameObject("ModelRoot");
            modelRoot.transform.SetParent(transform);
            modelRoot.transform.localPosition = Vector3.zero;
            modelRoot.transform.localRotation = Quaternion.identity;
        }

        if (impostorRoot == null)
        {
            impostorRoot = new GameObject("ImpostorRoot");
            impostorRoot.transform.SetParent(transform);
            impostorRoot.transform.localPosition = Vector3.zero;
            impostorRoot.transform.localRotation = Quaternion.identity;
        }
    }

    public void InitializeFromData(ShipData data)
    {
        if (foundation == null)
        {
            Debug.LogError("ShipController: foundation is null!");
            return;
        }

        foundation.cellSize = data.cellSize;
        foundation.thickness = data.foundationThickness;

        HashSet<Vector2Int> cellsSet = new HashSet<Vector2Int>(data.existingCells);
        foundation.SetCells(cellsSet);

        existingCells = new List<Vector2Int>(data.existingCells);
        cellSize = data.cellSize;
        maxExtensions = data.maxExtensions;

        // Если maxExtensions не задан (старое сохранение), вычисляем его из existingCells
        if (maxExtensions == 0 && data.existingCells.Count > 0)
        {
            int max = 0;
            foreach (var cell in data.existingCells)
                max = Mathf.Max(max, Mathf.Abs(cell.x), Mathf.Abs(cell.y));
            maxExtensions = max;
        }

        blocks.Clear();
        foreach (var block in data.blocks)
        {
            Vector2Int pos = new Vector2Int(block.x, block.z);
            blocks[pos] = block.type;
        }

        GenerateVisualModules();
    }

    private void GenerateVisualModules()
    {
        foreach (Transform child in modelRoot.transform)
        {
            if (child.GetComponent<Module>() != null)
                DestroyImmediate(child.gameObject);
        }

        foreach (var cell in existingCells)
        {
            if (blocks.TryGetValue(cell, out BlockType type))
            {
                CreateModuleVisual(cell.x, cell.y, type);
            }
        }
    }

    private void CreateModuleVisual(int x, int z, BlockType type)
    {
        GameObject module = GameObject.CreatePrimitive(PrimitiveType.Cube);
        module.AddComponent<Module>();
        module.transform.parent = modelRoot.transform;

        float posX = x * cellSize;
        float posZ = z * cellSize;
        float posY = foundation.thickness / 2f + cellSize / 2f;
        module.transform.localPosition = new Vector3(posX, posY, posZ);
        module.transform.localScale = Vector3.one * cellSize;

        var renderer = module.GetComponent<Renderer>();
        switch (type)
        {
            case BlockType.Core:
                renderer.material.color = Color.yellow;
                break;
            case BlockType.Weapon:
                renderer.material.color = Color.red;
                break;
            case BlockType.Engine:
                renderer.material.color = Color.blue;
                break;
            default:
                renderer.material.color = Color.gray;
                break;
        }
    }

    public void GenerateImpostor()
    {
        foreach (Transform child in impostorRoot.transform)
            DestroyImmediate(child.gameObject);

        GameObject canvasGO = new GameObject("ImpostorCanvas");
        canvasGO.transform.SetParent(impostorRoot.transform);
        canvasGO.transform.localPosition = Vector3.zero;
        canvasGO.transform.localRotation = Quaternion.identity;

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;

        GameObject imageGO = new GameObject("ImpostorImage");
        imageGO.transform.SetParent(canvasGO.transform);
        imageGO.transform.localPosition = Vector3.zero;
        imageGO.transform.localRotation = Quaternion.identity;
        impostorRect = imageGO.AddComponent<RectTransform>();
        impostorRect.sizeDelta = new Vector2(64, 64);

        Image image = imageGO.AddComponent<Image>();
        image.material = new Material(Shader.Find("Sprites/Default"));
        image.color = Color.white;

        Texture2D tex = ImpostorGenerator.GenerateImpostorTexture(gameObject, LODSettings.ImpostorResolution, LODSettings.ImpostorPitchAngle);
        if (tex == null)
        {
            Debug.LogError("Impostor texture generation failed!");
            return;
        }
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        image.sprite = sprite;

        canvasGO.AddComponent<Billboard>();

        hasImpostor = true;
        Debug.Log($"Impostor generated for {gameObject.name}");
    }

    private void UpdateImpostorOrientation()
    {
        if (impostorRect == null) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        // Направление "вверх" камеры, спроецированное на горизонтальную плоскость
        Vector3 cameraUp = cam.transform.up;
        Vector3 cameraUpFlat = Vector3.ProjectOnPlane(cameraUp, Vector3.up).normalized;

        // Если камера смотрит строго сверху/снизу, используем мировую вертикаль
        if (cameraUpFlat.sqrMagnitude < 0.01f)
            cameraUpFlat = Vector3.forward;

        // Угол между носом корабля и направлением "вверх" камеры
        float angle = Vector3.SignedAngle(transform.forward, cameraUpFlat, Vector3.up);

        // Поворачиваем спрайт
        impostorRect.localRotation = Quaternion.Euler(0, 0, angle);
    }

    void Update()
    {
        if (hasImpostor && impostorRoot != null && impostorRoot.activeSelf)
        {
            UpdateImpostorOrientation();
        }
    }

    public ShipData SaveToData()
    {
        ShipData data = new ShipData();
        data.shipType = gameObject.name;
        data.position = transform.position;
        data.rotation = transform.rotation;
        data.cellSize = foundation.cellSize;
        data.foundationThickness = foundation.thickness;
        data.existingCells = new List<Vector2Int>(existingCells);
        data.maxExtensions = maxExtensions;

        foreach (var kvp in blocks)
        {
            data.blocks.Add(new BlockData
            {
                x = kvp.Key.x,
                z = kvp.Key.y,
                type = kvp.Value,
                health = 100
            });
        }
        return data;
    }

    public void UpdateImpostor()
    {
        if (hasImpostor)
            GenerateImpostor();
    }
}