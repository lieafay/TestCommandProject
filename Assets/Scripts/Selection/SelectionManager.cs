using UnityEngine;
using System.Collections.Generic;
using System;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; private set; }

    private List<RTSActorParent> selectedActors = new List<RTSActorParent>();
    public IReadOnlyList<RTSActorParent> SelectedActors => selectedActors;

    public event Action OnSelectionChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Select(RTSActorParent actor)
    {
        if (!selectedActors.Contains(actor))
        {
            selectedActors.Add(actor);
            actor.Select();
            OnSelectionChanged?.Invoke();
        }
    }

    public void Deselect(RTSActorParent actor)
    {
        if (selectedActors.Remove(actor))
        {
            actor.Deselect();
            OnSelectionChanged?.Invoke();
        }
    }

    public void DeselectAll()
    {
        foreach (var actor in selectedActors)
        {
            actor.Deselect();
        }
        selectedActors.Clear();
        OnSelectionChanged?.Invoke();
    }

    public bool IsSelected(RTSActorParent actor)
    {
        return selectedActors.Contains(actor);
    }
}