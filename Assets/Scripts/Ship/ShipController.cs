using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class ShipController : RTSActorParent
{
    [Header("Components")]
    public GameObject modelRoot;    // дочерний объект с 3D моделью
    public GameObject impostorRoot; // дочерний объект с Canvas
    public ProceduralFoundation foundation; // ссылка на компонент подложки

    // Public properties for SelectionIndicator
    public List<Vector2Int> existingCells { get; private set; } = new List<Vector2Int>();
    public Image impostorImage { get; private set; }
    public Sprite normalImpostorSprite { get; private set; }
    public Sprite selectionImpostorSprite { get; private set; }
    public float cellSize { get; private set; }

    private Dictionary<Vector2Int, BlockType> blocks = new Dictionary<Vector2Int, BlockType>();
    private RectTransform impostorRect; // RectTransform изображения импостера
    private int maxExtensions;
    private float _cellSize;
    private bool hasImpostor = false;
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

        // Ensure SelectionIndicator is attached
        if (GetComponent<SelectionIndicator>() == null)
        {
            gameObject.AddComponent<SelectionIndicator>();
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
        _cellSize = data.cellSize;
        maxExtensions = data.maxExtensions;

        // Если maxExtensions не задан (старое сохранение), вычисляем его из existingCells
        if (maxExtensions == 0 && data.existingCells.Count > 0)
        {
            int max = 0;
            foreach (var cell in data.existingCells)
                max = Mathf.Max(max, Mathf.Abs(cell.x), Mathf.Abs(cell.y));
            maxExtensions = max;
        }

        // Add BoxCollider for selection
        AddSelectionCollider();
        blocks.Clear();
        foreach (var block in data.blocks)
        {
            Vector2Int pos = new Vector2Int(block.x, block.z);
            blocks[pos] = block.type;
        }

        GenerateVisualModules();
    }

    private void AddSelectionCollider()
    {
        // Remove old MeshCollider if exists on this object and foundation
        MeshCollider oldCollider = GetComponent<MeshCollider>();
        if (oldCollider != null)
        {
            DestroyImmediate(oldCollider);
        }

        if (foundation != null)
        {
            MeshCollider foundationCollider = foundation.GetComponent<MeshCollider>();
            if (foundationCollider != null)
            {
                DestroyImmediate(foundationCollider);
            }
        }

        // Calculate bounds
        if (existingCells.Count == 0) return;

        int minX = int.MaxValue, maxX = int.MinValue, minZ = int.MaxValue, maxZ = int.MinValue;
        foreach (var cell in existingCells)
        {
            minX = Mathf.Min(minX, cell.x);
            maxX = Mathf.Max(maxX, cell.x);
            minZ = Mathf.Min(minZ, cell.y);
            maxZ = Mathf.Max(maxZ, cell.y);
        }

        float width = (maxX - minX + 1) * _cellSize;
        float depth = (maxZ - minZ + 1) * _cellSize;
        float height = foundation.thickness + _cellSize; // From bottom to top of modules

        Vector3 center = new Vector3(
            (minX + maxX) * _cellSize / 2f,
            0,
            (minZ + maxZ) * _cellSize / 2f
        );

        BoxCollider collider = gameObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.center = center;
        collider.size = new Vector3(width, height, depth);
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

        float posX = x * _cellSize;
        float posZ = z * _cellSize;
        float posY = foundation.thickness / 2f + _cellSize / 2f;
        module.transform.localPosition = new Vector3(posX, posY, posZ);
        module.transform.localScale = Vector3.one * _cellSize;

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
        impostorImage = image;

        // Generate normal sprite
        Texture2D normalTex = ImpostorGenerator.GenerateImpostorTexture(gameObject, LODSettings.ImpostorResolution, LODSettings.ImpostorPitchAngle);
        if (normalTex == null)
        {
            Debug.LogError("Impostor texture generation failed!");
            return;
        }
        normalImpostorSprite = Sprite.Create(normalTex, new Rect(0, 0, normalTex.width, normalTex.height), new Vector2(0.5f, 0.5f));

        // Generate selection sprite with border
        Texture2D selectionTex = ImpostorGenerator.GenerateImpostorTextureWithBorder(gameObject, LODSettings.ImpostorResolution, LODSettings.ImpostorPitchAngle);
        selectionImpostorSprite = Sprite.Create(selectionTex, new Rect(0, 0, selectionTex.width, selectionTex.height), new Vector2(0.5f, 0.5f));

        image.sprite = normalImpostorSprite;

        // Add BoxCollider to cover the sprite
        BoxCollider spriteCollider = imageGO.AddComponent<BoxCollider>();
        UpdateSpriteCollider();

        canvasGO.AddComponent<Billboard>();

        hasImpostor = true;
        Debug.Log($"Impostor generated for {gameObject.name}");
    }

    private void UpdateSpriteCollider()
    {
        if (impostorImage == null || impostorRect == null) return;

        Vector3[] corners = new Vector3[4];
        impostorRect.GetWorldCorners(corners);

        Bounds bounds = new Bounds(corners[0], Vector3.zero);
        for (int i = 1; i < 4; i++)
        {
            bounds.Encapsulate(corners[i]);
        }

        BoxCollider collider = impostorImage.GetComponent<BoxCollider>();
        if (collider != null)
        {
            collider.center = bounds.center - transform.position;
            collider.size = bounds.size;
        }
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

    public override void Select()
    {
        base.Select();
        SelectionIndicator indicator = GetComponent<SelectionIndicator>();
        if (indicator != null)
        {
            indicator.Show(true);
        }
    }

    public override void Deselect()
    {
        base.Deselect();
        SelectionIndicator indicator = GetComponent<SelectionIndicator>();
        if (indicator != null)
        {
            indicator.Show(false);
        }
    }
}