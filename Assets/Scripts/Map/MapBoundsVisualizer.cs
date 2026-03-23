using UnityEngine;

/// <summary>
/// Визуализирует границы карты в виде пончика
/// Пончик становится видимым при приближении камеры
/// </summary>
public class MapBoundsVisualizer : MonoBehaviour
{
    [SerializeField] private Material boundaryMaterial;
    [SerializeField] private float torusMinorRadius = 1f; // Радиус трубки 
    [SerializeField] private int segmentsCircle = 32; // Сегменты по окружности (главный радиус)
    [SerializeField] private int segmentsTube = 16; // Сегменты трубки (толщина)

    
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MapBounds mapBounds;
    private Mesh torusMesh;

    private void Start()
    {
        mapBounds = FindObjectOfType<MapBounds>();
        if (mapBounds == null)
        {
            Debug.LogError("MapBounds не найден в сцене");
            return;
        }

        // Добавляем компоненты если их нет
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        // Добавляем коллайдер если нужно
        if (GetComponent<MeshCollider>() == null)
            gameObject.AddComponent<MeshCollider>();

        // Создаём тор
        if (mapBounds.IsCircular())
        {
            CreateTorusBoundary(mapBounds);
        }

        
    }

    private void CreateTorusBoundary(MapBounds mapBounds)
    {
        float majorRadius = mapBounds.GetMapRadius();
        Vector3 center = mapBounds.GetMapCenter();

        // Генерируем тор
        torusMesh = GenerateTorus(majorRadius, torusMinorRadius, segmentsCircle, segmentsTube);
        meshFilter.mesh = torusMesh;

        // Позиционируем центр
        transform.position = center;

        // Применяем материал
        if (boundaryMaterial != null)
        {
            meshRenderer.material = boundaryMaterial;
        }

        // Отключаем shadow casting
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;


    }

    private Mesh GenerateTorus(float majorRadius, float minorRadius, int majorSegments, int minorSegments)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Torus";

        int vertexCount = majorSegments * minorSegments;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        Vector2[] uv = new Vector2[vertexCount];

        // Генерируем вершины
        for (int i = 0; i < majorSegments; i++)
        {
            float theta = (i / (float)majorSegments) * 2f * Mathf.PI;
            float cosTheta = Mathf.Cos(theta);
            float sinTheta = Mathf.Sin(theta);

            for (int j = 0; j < minorSegments; j++)
            {
                float phi = (j / (float)minorSegments) * 2f * Mathf.PI;
                float cosPhi = Mathf.Cos(phi);
                float sinPhi = Mathf.Sin(phi);

                int vertexIndex = i * minorSegments + j;

                // Позиция вершины на торе
                float x = (majorRadius + minorRadius * cosPhi) * cosTheta;
                float y = minorRadius * sinPhi;
                float z = (majorRadius + minorRadius * cosPhi) * sinTheta;

                vertices[vertexIndex] = new Vector3(x, y, z);

                // Нормаль
                Vector3 normal = new Vector3(cosPhi * cosTheta, sinPhi, cosPhi * sinTheta).normalized;
                normals[vertexIndex] = normal;

                // UV координаты
                uv[vertexIndex] = new Vector2(i / (float)majorSegments, j / (float)minorSegments);
            }
        }

        // Генерируем треугольники
        int triangleCount = majorSegments * minorSegments * 2;
        int[] triangles = new int[triangleCount * 3];
        int triangleIndex = 0;

        for (int i = 0; i < majorSegments; i++)
        {
            for (int j = 0; j < minorSegments; j++)
            {
                int nextI = (i + 1) % majorSegments;
                int nextJ = (j + 1) % minorSegments;

                int v0 = i * minorSegments + j;
                int v1 = nextI * minorSegments + j;
                int v2 = i * minorSegments + nextJ;
                int v3 = nextI * minorSegments + nextJ;

                // Первый треугольник
                triangles[triangleIndex++] = v0;
                triangles[triangleIndex++] = v1;
                triangles[triangleIndex++] = v2;

                // Второй треугольник
                triangles[triangleIndex++] = v2;
                triangles[triangleIndex++] = v1;
                triangles[triangleIndex++] = v3;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.RecalculateBounds();

        return mesh;
    }

    private void Update()
    {
        // Просто держим границу видимой с полной прозрачностью
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        MapBounds mapBounds = FindObjectOfType<MapBounds>();
        if (mapBounds == null) return;

        Gizmos.color = Color.green;
        float radius = mapBounds.GetMapRadius();
        Vector3 center = mapBounds.GetMapCenter();

        // Рисуем круг в редакторе для отладки
        int segments = 64;
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (i / (float)segments) * 360f * Mathf.Deg2Rad;
            float angle2 = ((i + 1) / (float)segments) * 360f * Mathf.Deg2Rad;

            Vector3 p1 = center + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
            Vector3 p2 = center + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);

            Gizmos.DrawLine(p1, p2);
        }
    }
#endif
}
