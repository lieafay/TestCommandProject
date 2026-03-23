using UnityEngine;

public class ObjectSelector : MonoBehaviour
{
    private RTSActorParent currentSelection;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                RTSActorParent entity = hit.collider.GetComponentInParent<RTSActorParent>();
                if (entity != null)
                {
                    if (currentSelection != null)
                        currentSelection.Deselect();
                    currentSelection = entity;
                    currentSelection.Select();
                }
                else
                {
                    if (currentSelection != null)
                        currentSelection.Deselect();
                    currentSelection = null;
                }
            }
        }
    }
}