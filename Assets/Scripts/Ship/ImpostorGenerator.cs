using UnityEngine;

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
        // --- Сохраняем исходное состояние корабля ---
        // Временное перемещение корабля в центр (0,0,0) и обнуление поворота нужно для
        // того, чтобы временная камера могла снимать объект без влияния его текущей позиции и вращения.
        // После рендеринга всё будет восстановлено.
        bool wasActive = ship.activeSelf;
        Vector3 originalPos = ship.transform.position;
        Quaternion originalRot = ship.transform.rotation;

        ship.transform.position = Vector3.zero;
        ship.transform.rotation = Quaternion.identity;
        ship.SetActive(true); // убеждаемся, что объект виден камере

        // --- Вычисляем габариты корабля ---
        // Это необходимо для настройки ортографической камеры, чтобы корабль полностью помещался в кадр.
        Bounds bounds = CalculateBounds(ship);
        float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

        // Размер камеры выбирается чуть больше половины максимального размера, чтобы оставить отступ по краям.
        // Умножаем на 0.9, чтобы объект не упирался в границы кадра.
        float orthoSize = maxSize * 0.9f;

        // --- Создаём временную ортографическую камеру ---
        // Ортографическая камера гарантирует, что размер объекта на текстуре не зависит от расстояния,
        // что удобно для получения равномерного масштаба на текстуре.
        GameObject tempCameraGO = new GameObject("TempImpostorCamera");
        Camera tempCamera = tempCameraGO.AddComponent<Camera>();
        tempCamera.orthographic = true;
        tempCamera.orthographicSize = orthoSize;
        tempCamera.backgroundColor = new Color(0, 0, 0, 0); // полностью прозрачный фон
        tempCamera.clearFlags = CameraClearFlags.Color;    // заливаем прозрачным цветом
        tempCamera.nearClipPlane = 0.1f;
        tempCamera.farClipPlane = 1000f;

        // --- Позиционируем камеру относительно центра корабля ---
        // Расстояние выбирается таким, чтобы корабль полностью влезал в ортографическую проекцию.
        // Ортографическая камера не зависит от расстояния, но мы всё равно задаём его для направления.
        float distance = maxSize * 1.8f;
        // Направление: поворачиваем вектор вперёд (0,0,1) на угол pitchAngle вокруг оси X.
        Vector3 direction = Quaternion.Euler(pitchAngle, 0, 0) * Vector3.forward;
        tempCamera.transform.position = direction.normalized * distance;
        // Камера смотрит точно на центр корабля (0,0,0)
        tempCamera.transform.LookAt(Vector3.zero);

        // --- Создаём RenderTexture и рендерим ---
        RenderTexture rt = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4; // включить сглаживание для более чёткого изображения
        tempCamera.targetTexture = rt;

        tempCamera.Render(); // выполняем рендеринг в RenderTexture

        // --- Читаем пиксели из RenderTexture в Texture2D ---
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false);
        tex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
        tex.Apply(); // применяем изменения (обязательно после ReadPixels)

        // --- Очистка временных ресурсов ---
        tempCamera.targetTexture = null;
        RenderTexture.active = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tempCameraGO);

        // --- Восстанавливаем исходное состояние корабля ---
        ship.SetActive(wasActive);
        ship.transform.position = originalPos;
        ship.transform.rotation = originalRot;

        return tex;
    }

    /// <summary>
    /// Вычисляет общие габариты объекта и всех его дочерних объектов, имеющих Renderer.
    /// Используется для настройки размера камеры.
    /// </summary>
    private static Bounds CalculateBounds(GameObject obj)
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
    public static void AddSelectionBorder(Texture2D texture, int borderWidth = 2)
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
        AddSelectionBorder(tex);
        return tex;
    }
}