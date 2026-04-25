using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(ShipController))]
public class SelectionIndicator : MonoBehaviour
{
    private ShipController shipController;
    private GameObject frameRoot;
    private List<LineRenderer> frameLines = new List<LineRenderer>();
    private bool isSelected = false;
    private bool wasModelActive = true;

    [SerializeField] private float frameOffset = 0.1f;
    [SerializeField] private Color frameColor = Color.green;
    [SerializeField] private float lineWidth = 0.05f;

    void Awake()
    {
        shipController = GetComponent<ShipController>();
        CreateFrame();
    }

    void Update()
    {
        bool isModelActive = shipController.modelRoot.activeSelf;
        if (isModelActive != wasModelActive)
        {
            wasModelActive = isModelActive;
            Show(isSelected);
        }
    }

    private void CreateFrame()
    {
        frameRoot = new GameObject("SelectionFrame");
        // ВАЖНО: открепляем от родителя, чтобы позиция задавалась в мировых координатах без искажений
        frameRoot.transform.SetParent(null);
        frameRoot.transform.position = Vector3.zero;

        Material lineMaterial = new Material(Shader.Find("Unlit/Color"));
        lineMaterial.color = frameColor;

        for (int i = 0; i < 12; i++)
        {
            GameObject lineGO = new GameObject($"Line{i}");
            lineGO.transform.SetParent(frameRoot.transform);
            lineGO.transform.localPosition = Vector3.zero;
            LineRenderer lr = lineGO.AddComponent<LineRenderer>();
            lr.material = lineMaterial;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.positionCount = 2;
            lr.useWorldSpace = false;   // координаты относительны frameRoot
            frameLines.Add(lr);
        }
    }

    public void Show(bool show)
    {
        isSelected = show;
        if (shipController.impostorRoot != null && shipController.impostorRoot.activeSelf)
        {
            if (shipController.impostorImage != null)
                shipController.impostorImage.sprite = show ? shipController.selectionImpostorSprite : shipController.normalImpostorSprite;
            frameRoot.SetActive(false);
        }
        else
        {
            frameRoot.SetActive(show);
            if (show)
                UpdateFrameBounds();
        }
    }

    private void UpdateFrameBounds()
    {
        if (shipController == null) return;

        BoxCollider box = shipController.GetComponent<BoxCollider>();
        if (box == null)
        {
            Debug.LogWarning($"SelectionIndicator: BoxCollider not found on {shipController.name}");
            return;
        }

        Bounds worldBounds = box.bounds;
        Vector3 center = worldBounds.center;
        Vector3 extent = worldBounds.extents;

        // Устанавливаем позицию рамки в мировой центр коллайдера
        frameRoot.transform.position = new Vector3(center.x, shipController.transform.position.y, center.z);

        float w = extent.x;
        float h = extent.y;
        float d = extent.z;

        // Верхняя грань
        SetLine(0, new Vector3(-w, h, -d), new Vector3(w, h, -d));
        SetLine(1, new Vector3(w, h, -d), new Vector3(w, h, d));
        SetLine(2, new Vector3(w, h, d), new Vector3(-w, h, d));
        SetLine(3, new Vector3(-w, h, d), new Vector3(-w, h, -d));

        // Нижняя грань
        SetLine(4, new Vector3(-w, -h, -d), new Vector3(w, -h, -d));
        SetLine(5, new Vector3(w, -h, -d), new Vector3(w, -h, d));
        SetLine(6, new Vector3(w, -h, d), new Vector3(-w, -h, d));
        SetLine(7, new Vector3(-w, -h, d), new Vector3(-w, -h, -d));

        // Вертикальные рёбра
        SetLine(8,  new Vector3(-w, h, -d), new Vector3(-w, -h, -d));
        SetLine(9,  new Vector3(w, h, -d), new Vector3(w, -h, -d));
        SetLine(10, new Vector3(w, h, d), new Vector3(w, -h, d));
        SetLine(11, new Vector3(-w, h, d), new Vector3(-w, -h, d));

        Debug.Log($"[FrameUpdate] {shipController.name}: pos={frameRoot.transform.position}, w={w}, h={h}, d={d}");
    }

    private void SetLine(int index, Vector3 start, Vector3 end)
    {
        if (index < 0 || index >= frameLines.Count) return;
        frameLines[index].SetPosition(0, start);
        frameLines[index].SetPosition(1, end);
    }
}