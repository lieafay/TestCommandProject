using UnityEngine;
using UnityEngine.InputSystem;

public class SelectionInput : MonoBehaviour
{
    private InputActionAsset inputAsset;
    private InputAction selectAction;
    private InputAction modifierAction;
    private InputAction deselectAllAction;

    public InputAction SelectAction => selectAction;
    public InputAction ModifierAction => modifierAction;
    public InputAction DeselectAllAction => deselectAllAction;

    void Awake()
    {
        inputAsset = Resources.Load<InputActionAsset>("SelectionInput");
        if (inputAsset == null)
        {
            Debug.LogError("SelectionInput: Could not load SelectionInput.inputactions from Resources!");
            return;
        }

        var selectionMap = inputAsset.FindActionMap("Selection");
        selectAction = selectionMap.FindAction("Select");
        modifierAction = selectionMap.FindAction("Modifier");
        deselectAllAction = selectionMap.FindAction("DeselectAll");

        inputAsset.Enable();
    }

    void OnDestroy()
    {
        if (inputAsset != null)
        {
            inputAsset.Disable();
            // Do not destroy the asset, as it's loaded from Resources
        }
    }
}