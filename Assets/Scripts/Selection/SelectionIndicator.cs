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
        frameRoot.transform.SetParent(shipController.modelRoot.transform);
        frameRoot.transform.localPosition = Vector3.zero;
        frameRoot.transform.localRotation = Quaternion.identity;

        Material lineMaterial = new Material(Shader.Find("Unlit/Color"));
        lineMaterial.color = frameColor;

        // Create 12 lines: 4 top, 4 bottom, 4 vertical
        for (int i = 0; i < 12; i++)
        {
            GameObject lineGO = new GameObject($"Line{i}");
            lineGO.transform.SetParent(frameRoot.transform);
            LineRenderer lr = lineGO.AddComponent<LineRenderer>();
            lr.material = lineMaterial;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.positionCount = 2;
            lr.useWorldSpace = false;
            frameLines.Add(lr);
        }
    }

    public void Show(bool show)
    {
        isSelected = show;
        if (shipController.impostorRoot.activeSelf)
        {
            // For impostor, change sprite
            if (shipController.impostorImage != null)
            {
                shipController.impostorImage.sprite = show ? shipController.selectionImpostorSprite : shipController.normalImpostorSprite;
            }
            frameRoot.SetActive(false);
        }
        else
        {
            // For 3D model, show/hide frame
            frameRoot.SetActive(show);
            if (show)
            {
                UpdateFrameBounds();
            }
        }
    }

    private void UpdateFrameBounds()
    {
        if (shipController.existingCells.Count == 0) return;

        // Calculate bounds from existingCells
        int minX = int.MaxValue, maxX = int.MinValue, minZ = int.MaxValue, maxZ = int.MinValue;
        foreach (var cell in shipController.existingCells)
        {
            minX = Mathf.Min(minX, cell.x);
            maxX = Mathf.Max(maxX, cell.x);
            minZ = Mathf.Min(minZ, cell.y);
            maxZ = Mathf.Max(maxZ, cell.y);
        }

        float cellSize = shipController.cellSize;
        float halfThickness = shipController.foundation.thickness / 2f;
        float height = halfThickness + cellSize; // Temporary, can be improved to max module height

        Vector3 center = new Vector3(
            (minX + maxX) * cellSize / 2f,
            0,
            (minZ + maxZ) * cellSize / 2f
        );

        float width = (maxX - minX + 1) * cellSize + frameOffset * 2;
        float depth = (maxZ - minZ + 1) * cellSize + frameOffset * 2;

        frameRoot.transform.localPosition = center;

        // Set line positions
        // Top square
        SetLine(0, new Vector3(-width/2, height + frameOffset, -depth/2), new Vector3(width/2, height + frameOffset, -depth/2));
        SetLine(1, new Vector3(width/2, height + frameOffset, -depth/2), new Vector3(width/2, height + frameOffset, depth/2));
        SetLine(2, new Vector3(width/2, height + frameOffset, depth/2), new Vector3(-width/2, height + frameOffset, depth/2));
        SetLine(3, new Vector3(-width/2, height + frameOffset, depth/2), new Vector3(-width/2, height + frameOffset, -depth/2));

        // Bottom square
        SetLine(4, new Vector3(-width/2, -halfThickness - frameOffset, -depth/2), new Vector3(width/2, -halfThickness - frameOffset, -depth/2));
        SetLine(5, new Vector3(width/2, -halfThickness - frameOffset, -depth/2), new Vector3(width/2, -halfThickness - frameOffset, depth/2));
        SetLine(6, new Vector3(width/2, -halfThickness - frameOffset, depth/2), new Vector3(-width/2, -halfThickness - frameOffset, depth/2));
        SetLine(7, new Vector3(-width/2, -halfThickness - frameOffset, depth/2), new Vector3(-width/2, -halfThickness - frameOffset, -depth/2));

        // Vertical lines
        SetLine(8, new Vector3(-width/2, height + frameOffset, -depth/2), new Vector3(-width/2, -halfThickness - frameOffset, -depth/2));
        SetLine(9, new Vector3(width/2, height + frameOffset, -depth/2), new Vector3(width/2, -halfThickness - frameOffset, -depth/2));
        SetLine(10, new Vector3(width/2, height + frameOffset, depth/2), new Vector3(width/2, -halfThickness - frameOffset, depth/2));
        SetLine(11, new Vector3(-width/2, height + frameOffset, depth/2), new Vector3(-width/2, -halfThickness - frameOffset, depth/2));
    }

    private void SetLine(int index, Vector3 start, Vector3 end)
    {
        frameLines[index].SetPosition(0, start);
        frameLines[index].SetPosition(1, end);
    }
}