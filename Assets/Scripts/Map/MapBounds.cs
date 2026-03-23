using UnityEngine;

/// <summary>
/// Управляет границами карты. Поддерживает круглую карту
/// </summary>
public class MapBounds : MonoBehaviour
{
    [SerializeField] private float mapRadius = 50f;// Радиус круглой карты
    [SerializeField] private bool isCircularMap = true;// Для того, если карта не будет круглой в будущем
    [SerializeField] private Vector3 mapCenter = Vector3.zero; //Центр карты, возможно будет движение станции
    [SerializeField] private bool showBoundsGizmo = true;//Показывать ли границы в редакторе
    [SerializeField] private Color boundsGizmoColor = new Color(0, 1, 0, 0.3f);//Цвет границ в редакторе

    
    //singleton, гарантия существования только одного объекта данного класса
    private static MapBounds instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    public static MapBounds Instance => instance;

    /// <summary>
    /// Проверяет, находится ли позиция в границах карты
    /// </summary>
    public static bool IsWithinBounds(Vector3 position)
    {
        if (instance == null)
            return true;

        return instance.IsPositionValid(position);
    }

    /// <summary>
    /// Возвращает позицию, скорректированную если она за границами карты
    /// </summary>
    public static Vector3 ClampToBounds(Vector3 position)
    {
        if (instance == null)
            return position;

        return instance.ClampPosition(position);
    }

    private bool IsPositionValid(Vector3 position)
    {
        if (isCircularMap)
        {
            float distance = Vector3.Distance(position, mapCenter);
            return distance <= mapRadius;
        }
        else
        {
            return true; // Прямоугольная карта (можно добавить позже)
        }
    }

    private Vector3 ClampPosition(Vector3 position)
    {
        if (isCircularMap)
        {
            Vector3 directionFromCenter = (position - mapCenter).normalized;
            float distance = Vector3.Distance(position, mapCenter);

            if (distance > mapRadius)
            {
                return mapCenter + directionFromCenter * mapRadius;
            }
        }

        return position;
    }

    public float GetMapRadius() => mapRadius;
    public Vector3 GetMapCenter() => mapCenter;
    public bool IsCircular() => isCircularMap;

//Рисовалка круга для редактора
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!showBoundsGizmo)
            return;

        Gizmos.color = boundsGizmoColor;

        if (isCircularMap)
        {
            DrawCircle(mapCenter, mapRadius, 64);
        }
    }

    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angle = 0f;
        float angleStep = 360f / segments;
        Vector3 lastPoint = center + new Vector3(radius, 0, 0);

        for (int i = 0; i <= segments; i++)
        {
            float rad = angle * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(rad) * radius, 0, Mathf.Sin(rad) * radius);
            Gizmos.DrawLine(lastPoint, newPoint);
            lastPoint = newPoint;
            angle += angleStep;
        }
    }
#endif
}
