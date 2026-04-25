using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Генерирует текстуру-импостер (биллборд) для корабля/станции.
/// Используется временная ортографическая камера для рендеринга объекта под заданным углом.
/// Результат – текстура с прозрачным фоном, которая затем используется в Canvas.
/// </summary>
public static class ImpostorGenerator
{
    /// <summary>
    /// Создаёт текстуру указанного разрешения, на которой отображён корабль под углом pitchAngle.
    /// </summary>
    /// <param name="ship">Объект корабля (включая все дочерние модули).</param>
    /// <param name="resolution">Разрешение текстуры (ширина и высота в пикселях).</param>
    /// <param name="pitchAngle">
    /// Угол наклона камеры в градусах относительно горизонтали:
    /// 0° – вид строго спереди (камера на уровне центра, смотрит на корабль),
    /// 45° – вид спереди-сверху,
    /// 90° – вид сверху (план).
    /// </param>
    /// <returns>Texture2D с прозрачным фоном, содержащая изображение корабля.</returns>
public static Texture2D GenerateImpostorTexture(GameObject ship, int resolution = 256, float pitchAngle = 90f)
{
    // 1. Сохраняем исходные слои объекта и всех детей
    int impostorLayer = LayerMask.NameToLayer("ImpostorCapture");
    if (impostorLayer < 0)
    {
        Debug.LogError("Слой 'ImpostorCapture' не найден! Добавьте его в Edit → Project Settings → Tags and Layers.");
        return null;
    }

    var originalLayers = new Dictionary<Transform, int>();
    foreach (var t in ship.GetComponentsInChildren<Transform>(includeInactive: true))
    {
        originalLayers[t] = t.gameObject.layer;
        t.gameObject.layer = impostorLayer;
    }

    // 2. Вычисляем мировые габариты (без перемещения)
    Bounds bounds = CalculateBounds(ship);
    float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
    float orthoSize = maxSize * 0.9f;
    float distance = maxSize * 1.8f;

    // 3. Создаём временную камеру, которая видит ТОЛЬКО слой ImpostorCapture
    GameObject tempCameraGO = new GameObject("TempImpostorCamera");
    Camera tempCamera = tempCameraGO.AddComponent<Camera>();
    tempCamera.orthographic = true;
    tempCamera.orthographicSize = orthoSize;
    tempCamera.backgroundColor = new Color(0, 0, 0, 0);
    tempCamera.clearFlags = CameraClearFlags.Color;
    tempCamera.cullingMask = 1 << impostorLayer;      // ← только наш объект
    tempCamera.nearClipPlane = 0.1f;
    tempCamera.farClipPlane = 1000f;

    // 4. Позиционируем камеру относительно реального центра объекта
    Vector3 direction = Quaternion.Euler(pitchAngle, 0, 0) * Vector3.forward;
    tempCamera.transform.position = bounds.center + direction.normalized * distance;
    tempCamera.transform.LookAt(bounds.center);

    // 5. Рендерим в текстуру
    RenderTexture rt = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32);
    rt.antiAliasing = 4;
    tempCamera.targetTexture = rt;
    tempCamera.Render();

    RenderTexture.active = rt;
    Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false);
    tex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
    tex.Apply();

    // 6. Очищаем временные объекты
    tempCamera.targetTexture = null;
    RenderTexture.active = null;
    Object.DestroyImmediate(rt);
    Object.DestroyImmediate(tempCameraGO);

    // 7. Восстанавливаем исходные слои
    foreach (var t in ship.GetComponentsInChildren<Transform>(includeInactive: true))
    {
        if (originalLayers.TryGetValue(t, out int originalLayer))
            t.gameObject.layer = originalLayer;
    }

    return tex;
}

    /// <summary>
    /// Вычисляет общие габариты объекта и всех его дочерних объектов, имеющих Renderer.
    /// Используется для настройки размера камеры.
    /// </summary>
   public static Bounds CalculateBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(obj.transform.position, Vector3.zero);
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    /// <summary>
    /// Добавляет зелёную рамку по краям текстуры.
    /// </summary>
    /// <param name="texture">Текстура, к которой добавляется обводка.</param>
    /// <param name="borderWidth">Толщина обводки в пикселях.</param>
    public static void AddSelectionBorder(Texture2D texture, int borderWidth = 6)
    {
        if (texture == null) return;

        int width = texture.width;
        int height = texture.height;
        Color[] pixels = texture.GetPixels();
        Color borderColor = Color.green;

        int minX = width;
        int maxX = 0;
        int minY = height;
        int maxY = 0;
        bool hasOpaque = false;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (pixels[y * width + x].a > 0.1f)
                {
                    hasOpaque = true;
                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }
            }
        }

        if (!hasOpaque || minX >= maxX || minY >= maxY)
        {
            return;
        }

        minX = Mathf.Max(0, minX - borderWidth);
        minY = Mathf.Max(0, minY - borderWidth);
        maxX = Mathf.Min(width - 1, maxX + borderWidth);
        maxY = Mathf.Min(height - 1, maxY + borderWidth);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                bool isBorder = x < minX + borderWidth || x > maxX - borderWidth || y < minY + borderWidth || y > maxY - borderWidth;
                if (!isBorder)
                    continue;

                int index = y * width + x;
                Color current = pixels[index];
                if (current.a < 0.1f)
                {
                    pixels[index] = borderColor;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
    }

    /// <summary>
    /// Генерирует текстуру импостера с зелёной обводкой для выделения.
    /// </summary>
    /// <param name="ship">Объект корабля.</param>
    /// <param name="resolution">Разрешение текстуры.</param>
    /// <param name="pitchAngle">Угол наклона.</param>
    /// <returns>Texture2D с обводкой.</returns>
    public static Texture2D GenerateImpostorTextureWithBorder(GameObject ship, int resolution = 256, float pitchAngle = 90f)
    {
        Texture2D tex = GenerateImpostorTexture(ship, resolution, pitchAngle);
        AddSelectionBorder(tex, 6);
        return tex;
    }
}