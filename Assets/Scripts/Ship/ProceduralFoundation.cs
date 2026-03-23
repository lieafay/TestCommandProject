using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Генерирует процедурную подложку (основу) корабля на основе заданных ячеек.
/// Использует greedy meshing для объединения соседних ячеек верхней грани в прямоугольники,
/// что значительно снижает количество вершин и треугольников.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralFoundation : MonoBehaviour
{
    [Tooltip("Размер одной ячейки в мировых единицах (ширина и глубина).")]
    public float cellSize = 1f;

    [Tooltip("Толщина подложки. Верхняя грань находится на высоте thickness/2, нижняя на -thickness/2.")]
    public float thickness = 0.2f;

    [Tooltip("Материал, которым будет отрисована подложка.")]
    public Material foundationMaterial;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh currentMesh;
    private HashSet<Vector2Int> cells = new HashSet<Vector2Int>(); // Множество существующих ячеек (x,z)

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        // Материал будет установлен из ShipSpawner перед вызовом SetCells
    }

    /// <summary>
    /// Устанавливает множество ячеек, которые образуют форму подложки, и перестраивает меш.
    /// </summary>
    /// <param name="cellsSet">Множество координат ячеек (x, z), где ячейка существует.</param>
    public void SetCells(HashSet<Vector2Int> cellsSet)
    {
        cells = new HashSet<Vector2Int>(cellsSet);
        RebuildMesh();
    }

    /// <summary>
    /// Перестраивает меш подложки с текущим набором ячеек.
    /// </summary>
    private void RebuildMesh()
    {
        if (foundationMaterial == null)
        {
            Debug.LogError("ProceduralFoundation: foundationMaterial is null! Cannot build mesh.");
            return;
        }
        if (currentMesh != null)
            DestroyImmediate(currentMesh);

        currentMesh = GenerateMesh();
        meshFilter.mesh = currentMesh;
        meshRenderer.material = foundationMaterial;
    }

    /// <summary>
    /// Генерирует меш подложки, состоящий из верхней грани (объединённой), нижней грани и боковых граней.
    /// </summary>
    /// <returns>Сгенерированный меш.</returns>
    private Mesh GenerateMesh()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uv = new List<Vector2>();

        float halfSize = cellSize / 2f;    // половина ширины ячейки
        float halfThick = thickness / 2f;  // половина толщины подложки

        // 1. Верхняя грань (Y = +halfThick) – объединяем соседние ячейки в прямоугольники (greedy meshing)
        AddTopSurface(vertices, triangles, uv, halfSize, halfThick);

        // 2. Нижняя грань (Y = -halfThick) и боковые грани (где нет соседа)
        //    Для простоты нижняя грань строится как отдельные квадраты для каждой ячейки,
        //    а боковые грани – по периметру, если у ячейки нет соседа в данном направлении.
        AddSideAndBottom(vertices, triangles, uv, halfSize, halfThick);

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uv.ToArray();
        mesh.RecalculateNormals();   // корректное освещение
        mesh.RecalculateBounds();    // для камеры и отсечения
        return mesh;
    }

    /// <summary>
    /// Добавляет верхнюю грань подложки, используя greedy meshing для объединения соседних ячеек.
    /// Алгоритм: находим минимальные и максимальные индексы, создаём двумерную сетку occupied,
    /// затем проходим по ней и для каждого непрерывного блока ячеек (прямоугольника) создаём один четырёхугольник.
    /// Это значительно сокращает количество вершин и треугольников.
    /// </summary>
    /// <param name="vertices">Список вершин (пополняется).</param>
    /// <param name="triangles">Список индексов треугольников (пополняется).</param>
    /// <param name="uv">Список UV-координат (пополняется).</param>
    /// <param name="halfSize">Половина размера ячейки.</param>
    /// <param name="halfThick">Половина толщины подложки (высота верхней грани).</param>
    private void AddTopSurface(List<Vector3> vertices, List<int> triangles, List<Vector2> uv, float halfSize, float halfThick)
    {
        // Определяем границы сетки по существующим ячейкам
        int minX = int.MaxValue, maxX = int.MinValue;
        int minZ = int.MaxValue, maxZ = int.MinValue;
        foreach (var cell in cells)
        {
            minX = Mathf.Min(minX, cell.x);
            maxX = Mathf.Max(maxX, cell.x);
            minZ = Mathf.Min(minZ, cell.y);
            maxZ = Mathf.Max(maxZ, cell.y);
        }

        // Создаём двумерный массив occupied, где true означает, что ячейка существует
        int width = maxX - minX + 1;
        int height = maxZ - minZ + 1;
        bool[,] occupied = new bool[width, height];
        foreach (var cell in cells)
        {
            occupied[cell.x - minX, cell.y - minZ] = true;
        }

        bool[,] processed = new bool[width, height]; // отмечает уже обработанные ячейки

        // Greedy meshing: проходим по строкам (z) и столбцам (x)
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                if (occupied[x, z] && !processed[x, z])
                {
                    // Начинаем новый прямоугольник: расширяем вправо, пока можно
                    int rx = x;
                    while (rx + 1 < width && occupied[rx + 1, z] && !processed[rx + 1, z])
                        rx++;

                    // Расширяем вниз (по z), пока можно (все ячейки в этом столбце от x до rx тоже свободны и не обработаны)
                    int rz = z;
                    bool ok = true;
                    while (rz + 1 < height && ok)
                    {
                        for (int cx = x; cx <= rx; cx++)
                        {
                            if (!occupied[cx, rz + 1] || processed[cx, rz + 1])
                            {
                                ok = false;
                                break;
                            }
                        }
                        if (ok) rz++;
                    }

                    // Помечаем все ячейки прямоугольника как обработанные
                    for (int cx = x; cx <= rx; cx++)
                        for (int cz = z; cz <= rz; cz++)
                            processed[cx, cz] = true;

                    // Вычисляем мировые координаты прямоугольника
                    float worldMinX = (x + minX) * cellSize - halfSize;
                    float worldMaxX = (rx + minX) * cellSize + halfSize;
                    float worldMinZ = (z + minZ) * cellSize - halfSize;
                    float worldMaxZ = (rz + minZ) * cellSize + halfSize;

                    // Вершины прямоугольника (верхняя грань)
                    Vector3[] rectVerts = new Vector3[4];
                    rectVerts[0] = new Vector3(worldMinX, halfThick, worldMinZ);
                    rectVerts[1] = new Vector3(worldMaxX, halfThick, worldMinZ);
                    rectVerts[2] = new Vector3(worldMaxX, halfThick, worldMaxZ);
                    rectVerts[3] = new Vector3(worldMinX, halfThick, worldMaxZ);

                    int startIdx = vertices.Count;
                    vertices.AddRange(rectVerts);

                    // Два треугольника для прямоугольника
                    triangles.Add(startIdx);
                    triangles.Add(startIdx + 1);
                    triangles.Add(startIdx + 2);
                    triangles.Add(startIdx);
                    triangles.Add(startIdx + 2);
                    triangles.Add(startIdx + 3);

                    // UV-координаты (повторяются по размеру прямоугольника, чтобы текстура не растягивалась)
                    float widthWorld = worldMaxX - worldMinX;
                    float lengthWorld = worldMaxZ - worldMinZ;
                    uv.Add(new Vector2(0, 0));
                    uv.Add(new Vector2(widthWorld / cellSize, 0));
                    uv.Add(new Vector2(widthWorld / cellSize, lengthWorld / cellSize));
                    uv.Add(new Vector2(0, lengthWorld / cellSize));
                }
            }
        }
    }

    /// <summary>
    /// Добавляет боковые грани (там, где нет соседней ячейки) и нижнюю грань.
    /// Для простоты используется подход "каждая ячейка создаёт свои грани".
    /// Нижняя грань создаётся как отдельные квадраты для каждой ячейки (можно было бы тоже объединить, но это не критично для производительности).
    /// Боковые грани создаются для каждого направления, где нет соседа.
    /// </summary>
    /// <param name="vertices">Список вершин (пополняется).</param>
    /// <param name="triangles">Список индексов треугольников (пополняется).</param>
    /// <param name="uv">Список UV-координат (пополняется).</param>
    /// <param name="halfSize">Половина размера ячейки.</param>
    /// <param name="halfThick">Половина толщины подложки.</param>
    private void AddSideAndBottom(List<Vector3> vertices, List<int> triangles, List<Vector2> uv, float halfSize, float halfThick)
    {
        // ----- Нижняя грань -----
        foreach (var cell in cells)
        {
            float cx = cell.x * cellSize;
            float cz = cell.y * cellSize;
            float minX = cx - halfSize;
            float maxX = cx + halfSize;
            float minZ = cz - halfSize;
            float maxZ = cz + halfSize;

            // Четыре вершины нижнего квадрата
            Vector3[] bottomVerts = new Vector3[4];
            bottomVerts[0] = new Vector3(minX, -halfThick, minZ);
            bottomVerts[1] = new Vector3(maxX, -halfThick, minZ);
            bottomVerts[2] = new Vector3(maxX, -halfThick, maxZ);
            bottomVerts[3] = new Vector3(minX, -halfThick, maxZ);

            int startIdx = vertices.Count;
            vertices.AddRange(bottomVerts);
            triangles.Add(startIdx);
            triangles.Add(startIdx + 1);
            triangles.Add(startIdx + 2);
            triangles.Add(startIdx);
            triangles.Add(startIdx + 2);
            triangles.Add(startIdx + 3);
            for (int i = 0; i < 4; i++) uv.Add(Vector2.zero); // простые UV, можно заменить на текстурные координаты при необходимости
        }

        // ----- Боковые грани -----
        // Направления: право, лево, вперёд (Z+), назад (Z-)
        Vector2Int[] dirs = new Vector2Int[]
        {
            new Vector2Int(1, 0),  // +X
            new Vector2Int(-1, 0), // -X
            new Vector2Int(0, 1),  // +Z
            new Vector2Int(0, -1)  // -Z
        };

        foreach (var cell in cells)
        {
            float cx = cell.x * cellSize;
            float cz = cell.y * cellSize;
            float minX = cx - halfSize;
            float maxX = cx + halfSize;
            float minZ = cz - halfSize;
            float maxZ = cz + halfSize;

            foreach (var dir in dirs)
            {
                Vector2Int neighbor = new Vector2Int(cell.x + dir.x, cell.y + dir.y);
                // Если соседа нет – нужно добавить боковую грань
                if (!cells.Contains(neighbor))
                {
                    Vector3[] sideVerts;
                    if (dir.x == 1) // правая грань (нормаль +X)
                    {
                        sideVerts = new Vector3[]
                        {
                            new Vector3(maxX, -halfThick, minZ),
                            new Vector3(maxX, -halfThick, maxZ),
                            new Vector3(maxX,  halfThick, maxZ),
                            new Vector3(maxX,  halfThick, minZ)
                        };
                    }
                    else if (dir.x == -1) // левая грань (нормаль -X)
                    {
                        sideVerts = new Vector3[]
                        {
                            new Vector3(minX, -halfThick, maxZ),
                            new Vector3(minX, -halfThick, minZ),
                            new Vector3(minX,  halfThick, minZ),
                            new Vector3(minX,  halfThick, maxZ)
                        };
                    }
                    else if (dir.y == 1) // передняя грань (Z+)
                    {
                        sideVerts = new Vector3[]
                        {
                            new Vector3(minX, -halfThick, maxZ),
                            new Vector3(maxX, -halfThick, maxZ),
                            new Vector3(maxX,  halfThick, maxZ),
                            new Vector3(minX,  halfThick, maxZ)
                        };
                    }
                    else // задняя грань (Z-)
                    {
                        sideVerts = new Vector3[]
                        {
                            new Vector3(maxX, -halfThick, minZ),
                            new Vector3(minX, -halfThick, minZ),
                            new Vector3(minX,  halfThick, minZ),
                            new Vector3(maxX,  halfThick, minZ)
                        };
                    }

                    int start = vertices.Count;
                    vertices.AddRange(sideVerts);
                    triangles.Add(start);
                    triangles.Add(start + 1);
                    triangles.Add(start + 2);
                    triangles.Add(start);
                    triangles.Add(start + 2);
                    triangles.Add(start + 3);
                    for (int i = 0; i < 4; i++) uv.Add(Vector2.zero);
                }
            }
        }
    }
}