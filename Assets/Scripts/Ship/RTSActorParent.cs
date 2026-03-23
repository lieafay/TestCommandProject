using UnityEngine;

/// <summary>
/// Базовый класс для всех объектов, которые можно выделять в игре.
/// </summary>
public class RTSActorParent : MonoBehaviour
{
    /// <summary>
    /// Вызывается при выделении объекта.
    /// </summary>
    public virtual void Select()
    {
        // Можно добавить визуальный эффект, например, подсветку или обводку
        Debug.Log($"[RTSActorParent] Selected: {gameObject.name}");
    }

    /// <summary>
    /// Вызывается при снятии выделения.
    /// </summary>
    public virtual void Deselect()
    {
        Debug.Log($"[RTSActorParent] Deselected: {gameObject.name}");
    }
}