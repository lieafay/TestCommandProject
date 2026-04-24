using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class ObjectSelector : MonoBehaviour
{
    [SerializeField] private bool useNewInputSystem = true;
    private SelectionManager selectionManager;
    private SelectionInput selectionInput;
    private Camera mainCamera;

    // Drag selection
    private bool isDragging = false;
    private bool pointerDown = false;
    private Vector2 dragStartPos;
    private Vector2 dragCurrentPos;

    void Awake()
    {
        if (useNewInputSystem)
        {
            selectionInput = gameObject.AddComponent<SelectionInput>();
        }
    }

    void Start()
    {
        selectionManager = SelectionManager.Instance;
        if (selectionManager == null)
        {
            Debug.LogError("ObjectSelector: SelectionManager not found!");
            return;
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("ObjectSelector: Main camera not found!");
            return;
        }

        selectionManager.OnSelectionChanged += OnSelectionChanged;
    }

    void OnEnable()
    {
        if (useNewInputSystem && selectionInput != null && selectionInput.SelectAction != null)
        {
            selectionInput.SelectAction.performed += OnSelectPerformed;
            selectionInput.SelectAction.canceled += OnSelectCanceled;
        }

        if (useNewInputSystem && selectionInput != null && selectionInput.DeselectAllAction != null)
        {
            selectionInput.DeselectAllAction.performed += OnDeselectAllPerformed;
        }
    }

    void OnDisable()
    {
        if (useNewInputSystem && selectionInput != null && selectionInput.SelectAction != null)
        {
            selectionInput.SelectAction.performed -= OnSelectPerformed;
            selectionInput.SelectAction.canceled -= OnSelectCanceled;
        }

        if (useNewInputSystem && selectionInput != null && selectionInput.DeselectAllAction != null)
        {
            selectionInput.DeselectAllAction.performed -= OnDeselectAllPerformed;
        }
    }

    void Update()
    {
        if (!useNewInputSystem)
        {
            HandleLegacyInput();
        }

        if (pointerDown && !isDragging)
        {
            CheckDragStart();
        }

        if (isDragging)
        {
            UpdateDragSelection();
        }
    }

    private void HandleLegacyInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            OnSelectPressed(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            OnSelectReleased(Input.mousePosition);
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            selectionManager.DeselectAll();
        }
    }

    private void OnSelectPerformed(InputAction.CallbackContext context)
    {
        OnSelectPressed(Input.mousePosition);
    }

    private void OnSelectCanceled(InputAction.CallbackContext context)
    {
        OnSelectReleased(Input.mousePosition);
    }

    private void OnDeselectAllPerformed(InputAction.CallbackContext context)
    {
        selectionManager.DeselectAll();
    }

    private void OnSelectPressed(Vector2 screenPos)
    {
        dragStartPos = screenPos;
        isDragging = false;
        pointerDown = true;
        dragCurrentPos = screenPos;
    }

    private void OnSelectReleased(Vector2 screenPos)
    {
        pointerDown = false;
        if (isDragging)
        {
            // End drag selection
            PerformDragSelection(dragStartPos, screenPos);
        }
        else
        {
            // Single click
            PerformSingleSelection(screenPos);
        }
        isDragging = false;
    }

    private void UpdateDragSelection()
    {
        dragCurrentPos = useNewInputSystem && Mouse.current != null ?
            Mouse.current.position.ReadValue() :
            (Vector2)Input.mousePosition;
    }

    private void CheckDragStart()
    {
        dragCurrentPos = useNewInputSystem && Mouse.current != null ?
            Mouse.current.position.ReadValue() :
            (Vector2)Input.mousePosition;

        if (Vector2.Distance(dragStartPos, dragCurrentPos) > 5f)
        {
            isDragging = true;
        }
    }

    private void PerformSingleSelection(Vector2 screenPos)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            RTSActorParent actor = hit.collider.GetComponentInParent<RTSActorParent>();
            if (actor != null)
            {
                bool isModifierPressed = useNewInputSystem ?
                    selectionInput.ModifierAction.ReadValue<float>() > 0.5f :
                    Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

                if (isModifierPressed)
                {
                    if (selectionManager.IsSelected(actor))
                    {
                        selectionManager.Deselect(actor);
                    }
                    else
                    {
                        selectionManager.Select(actor);
                    }
                }
                else
                {
                    selectionManager.DeselectAll();
                    selectionManager.Select(actor);
                }
            }
            else
            {
                selectionManager.DeselectAll();
            }
        }
        else
        {
            selectionManager.DeselectAll();
        }
    }

    private void PerformDragSelection(Vector2 startPos, Vector2 endPos)
    {
        Rect selectionRect = new Rect(
            Mathf.Min(startPos.x, endPos.x),
            Mathf.Min(startPos.y, endPos.y),
            Mathf.Abs(endPos.x - startPos.x),
            Mathf.Abs(endPos.y - startPos.y)
        );

        if (selectionRect.width < 5f || selectionRect.height < 5f)
        {
            selectionManager.DeselectAll();
            return;
        }

        Plane selectionPlane = new Plane(Vector3.up, Vector3.zero);
        Vector3 worldA = ScreenPointToPlane(selectionRect.min, selectionPlane);
        Vector3 worldB = ScreenPointToPlane(new Vector2(selectionRect.xMax, selectionRect.yMin), selectionPlane);
        Vector3 worldC = ScreenPointToPlane(new Vector2(selectionRect.xMin, selectionRect.yMax), selectionPlane);
        Vector3 worldD = ScreenPointToPlane(new Vector2(selectionRect.xMax, selectionRect.yMax), selectionPlane);

        Vector3 min = Vector3.Min(Vector3.Min(worldA, worldB), Vector3.Min(worldC, worldD));
        Vector3 max = Vector3.Max(Vector3.Max(worldA, worldB), Vector3.Max(worldC, worldD));

        Vector3 center = new Vector3((min.x + max.x) * 0.5f, 0f, (min.z + max.z) * 0.5f);
        Vector3 halfExtents = new Vector3(Mathf.Max(0.01f, (max.x - min.x) * 0.5f), 10f, Mathf.Max(0.01f, (max.z - min.z) * 0.5f));

        Collider[] overlaps = Physics.OverlapBox(center, halfExtents, Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
        List<RTSActorParent> selectedInDrag = new List<RTSActorParent>();
        foreach (var collider in overlaps)
        {
            RTSActorParent actor = collider.GetComponentInParent<RTSActorParent>();
            if (actor != null && !selectedInDrag.Contains(actor))
            {
                selectedInDrag.Add(actor);
            }
        }

        bool isModifierPressed = useNewInputSystem ?
            selectionInput.ModifierAction.ReadValue<float>() > 0.5f :
            Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (!isModifierPressed)
        {
            selectionManager.DeselectAll();
        }

        foreach (var actor in selectedInDrag)
        {
            if (isModifierPressed)
            {
                if (selectionManager.IsSelected(actor))
                {
                    selectionManager.Deselect(actor);
                }
                else
                {
                    selectionManager.Select(actor);
                }
            }
            else
            {
                selectionManager.Select(actor);
            }
        }

        Debug.Log($"Drag selection hit {selectedInDrag.Count} actor(s)");
    }

    private Vector3 ScreenPointToPlane(Vector2 screenPoint, Plane plane)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPoint);
        if (plane.Raycast(ray, out float enter))
        {
            return ray.GetPoint(enter);
        }

        Vector3 fallback = mainCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, mainCamera.nearClipPlane));
        fallback.y = 0.5f;
        return fallback;
    }

    private void OnGUI()
    {
        if (!isDragging) return;

        Rect rect = new Rect(
            Mathf.Min(dragStartPos.x, dragCurrentPos.x),
            Screen.height - Mathf.Max(dragStartPos.y, dragCurrentPos.y), // GUI использует перевёрнутую ось Y
            Mathf.Abs(dragCurrentPos.x - dragStartPos.x),
            Mathf.Abs(dragCurrentPos.y - dragStartPos.y)
        );

        // Создаём стиль для зелёного полупрозрачного прямоугольника с обводкой
        GUIStyle style = new GUIStyle();
        Texture2D background = new Texture2D(1, 1);
        background.SetPixel(0, 0, new Color(0, 1, 0, 0.2f));
        background.Apply();
        style.normal.background = background;
        style.border = new RectOffset(1, 1, 1, 1);

        GUI.Box(rect, GUIContent.none, style);
    }

    private void OnSelectionChanged()
    {
        Debug.Log($"Selection changed. Selected count: {selectionManager.SelectedActors.Count}");
    }
}