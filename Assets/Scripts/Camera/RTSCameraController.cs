using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Статический класс для глобальных настроек LOD (доступны из любого скрипта).
/// Используется в ShipController для генерации текстур и настройки LOD.
/// </summary>
public static class LODSettings
{
    public static int ImpostorResolution = 256;      // разрешение текстуры импостера
    public static float ImpostorScale = 1.2f;        // не используется (оставлено для совместимости)
    public static float LOD0Transition = 0.3f;       // не используется
    public static float LOD1Transition = 0.05f;      // не используется
    public static float ImpostorPitchAngle = 45f;    // угол съёмки при генерации текстуры (0 – вид спереди, 90 – сверху)
}

/// <summary>
/// Управляет камерой в RTS-стиле:
/// - Орбитальное вращение (yaw) вокруг целевой точки (Q/E или средняя кнопка мыши)
/// - Наклон (pitch), зависящий от зума (через AnimationCurve)
/// - Панорамирование (WASD, края экрана)
/// - Зум (скролл мыши) с плавной интерполяцией
/// - Скорость движения зависит от зума (чем ближе к земле, тем медленнее)
/// - Привязка к границам карты (если включено)
/// - Глобальные настройки LOD, передаваемые в ShipController
/// </summary>
public class RTSCameraController : MonoBehaviour
{
    [Header("Движение")]
    [Tooltip("Кривая зависимости скорости перемещения от зума (x=0 – минимальный зум, x=1 – максимальный зум). " +
             "Позволяет сделать камеру более отзывчивой при приближении.")]
    [SerializeField] private AnimationCurve moveSpeedCurve = AnimationCurve.EaseInOut(0, 40f, 1, 10f);
    [Tooltip("Ширина области у краёв экрана, при наведении мыши на которую начинается панорамирование.")]
    [SerializeField] private float edgePanThreshold = 20f;
    [Tooltip("Включить панорамирование мышью у краёв экрана.")]
    [SerializeField] private bool enableEdgePanning = true;

    [Header("Зум")]
    [Tooltip("Минимальное расстояние от камеры до целевой точки (приближение).")]
    [SerializeField] private float minZoom = 5f;
    [Tooltip("Максимальное расстояние от камеры до целевой точки (отдаление).")]
    [SerializeField] private float maxZoom = 150f;
    [Tooltip("Текущее расстояние (будет плавно изменяться).")]
    [SerializeField] private float currentZoom = 50f;
    private float targetZoom;                         // целевое расстояние для плавного зума
    [Tooltip("Скорость интерполяции зума (чем выше, тем быстрее).")]
    [SerializeField] private float zoomLerpSpeed = 8f;
    [Tooltip("Чувствительность скролла мыши.")]
    [SerializeField] private float scrollSensitivity = 3f;

    [Header("Питч (наклон) от зума")]
    [Tooltip("Кривая зависимости угла наклона камеры от зума (x=0 – minZoom, x=1 – maxZoom). " +
             "Позволяет плавно переходить от бокового вида к виду сверху.")]
    [SerializeField] private AnimationCurve zoomToPitchCurve = AnimationCurve.EaseInOut(0, 20f, 1, 75f);
    [Tooltip("Минимальный угол наклона (при максимальном приближении).")]
    [SerializeField] private float minPitch = 20f;
    [Tooltip("Максимальный угол наклона (при максимальном отдалении).")]
    [SerializeField] private float maxPitch = 75f;

    [Header("Yaw вращение вокруг оси")]
    [Tooltip("Включить вращение камеры вокруг целевой точки (Q/E или средняя кнопка мыши).")]
    [SerializeField] private bool enableYawRotation = true;
    [Tooltip("Скорость вращения при движении мышью (градусы в секунду на единицу перемещения).")]
    [SerializeField] private float yawRotationSpeed = 100f;
    private float currentYaw = 0f;                   // текущий угол поворота (градусы)
    [Tooltip("Скорость вращения клавишами Q/E (градусы в секунду).")]
    [SerializeField] private float keyboardYawSpeed = 60f;

    [Header("Клавиши управления")]
    [SerializeField] private KeyCode moveForwardKey = KeyCode.W;
    [SerializeField] private KeyCode moveBackwardKey = KeyCode.S;
    [SerializeField] private KeyCode moveLeftKey = KeyCode.A;
    [SerializeField] private KeyCode moveRightKey = KeyCode.D;
    [SerializeField] private KeyCode rotateLeftKey = KeyCode.Q;
    [SerializeField] private KeyCode rotateRightKey = KeyCode.E;
    [Tooltip("Кнопка мыши для вращения (0 – левая, 1 – правая, 2 – средняя).")]
    [SerializeField] private int rotateMouseButton = 2;

    [Header("Input System (опционально)")]
    [Tooltip("Использовать новую систему ввода (Input System). Если false, используется старый Input.")]
    [SerializeField] private bool useNewInputSystem = false;
#if ENABLE_INPUT_SYSTEM
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference zoomAction;
    [SerializeField] private InputActionReference pointerDeltaAction;
    [SerializeField] private InputActionReference rotateMouseButtonAction;
    [SerializeField] private InputActionReference rotateLeftAction;
    [SerializeField] private InputActionReference rotateRightAction;
#endif

    [Header("Границы")]
    [Tooltip("Ограничивать движение камеры границами карты (MapBounds).")]
    [SerializeField] private bool clampToMapBounds = true;

    [Header("LOD и биллборды")]
    [Tooltip("Разрешение текстур импостера (чем выше, тем чётче, но дольше генерация).")]
    [SerializeField] private int impostorResolution = 256;
    [Tooltip("Масштаб импостера (не используется в текущей версии, оставлен для совместимости).")]
    [SerializeField] private float impostorScale = 1.2f;
    [Tooltip("Порог LOD0 (не используется, оставлен для совместимости).")]
    [SerializeField] private float lod0Transition = 0.3f;
    [Tooltip("Порог LOD1 (не используется, оставлен для совместимости).")]
    [SerializeField] private float lod1Transition = 0.05f;
    [Tooltip("Угол съёмки при генерации текстуры импостера (0 – вид спереди, 90 – сверху).")]
    [SerializeField] private float impostorPitchAngle = 45f;

    private Camera mainCamera;        // ссылка на камеру
    private Vector3 targetPosition;   // точка, вокруг которой вращается камера (центр карты или позиция по умолчанию)
    private Vector3 lastMousePosition; // предыдущая позиция мыши для расчёта дельты вращения
    private bool hasInitialized = false;

    private void Start()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Настраиваем камеру как перспективную
        mainCamera.orthographic = false;
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 2000f;
        mainCamera.fieldOfView = 60f;

        targetZoom = currentZoom;

        // Устанавливаем целевую точку (центр карты, если есть MapBounds)
        MapBounds mapBounds = MapBounds.Instance;
        if (mapBounds != null)
            targetPosition = mapBounds.GetMapCenter();
        else
            targetPosition = transform.position;

        lastMousePosition = Input.mousePosition;
        hasInitialized = true;

        // Передаём настройки LOD в глобальный статический класс для использования в ShipController
        LODSettings.ImpostorResolution = impostorResolution;
        LODSettings.ImpostorScale = impostorScale;
        LODSettings.LOD0Transition = lod0Transition;
        LODSettings.LOD1Transition = lod1Transition;
        LODSettings.ImpostorPitchAngle = impostorPitchAngle;
    }

#if ENABLE_INPUT_SYSTEM
    // Включение/отключение действий Input System при активации/деактивации компонента
    private void OnEnable()
    {
        if (!useNewInputSystem) return;
        if (moveAction?.action != null) moveAction.action.Enable();
        if (zoomAction?.action != null) zoomAction.action.Enable();
        if (pointerDeltaAction?.action != null) pointerDeltaAction.action.Enable();
        if (rotateMouseButtonAction?.action != null) rotateMouseButtonAction.action.Enable();
        if (rotateLeftAction?.action != null) rotateLeftAction.action.Enable();
        if (rotateRightAction?.action != null) rotateRightAction.action.Enable();
    }

    private void OnDisable()
    {
        if (!useNewInputSystem) return;
        if (moveAction?.action != null) moveAction.action.Disable();
        if (zoomAction?.action != null) zoomAction.action.Disable();
        if (pointerDeltaAction?.action != null) pointerDeltaAction.action.Disable();
        if (rotateMouseButtonAction?.action != null) rotateMouseButtonAction.action.Disable();
        if (rotateLeftAction?.action != null) rotateLeftAction.action.Disable();
        if (rotateRightAction?.action != null) rotateRightAction.action.Disable();
    }
#endif

    private void Update()
    {
        if (!hasInitialized) return;

        HandleMovement();
        HandleZoom();
        HandleYawRotation();
        UpdateCameraPosition();

        lastMousePosition = Input.mousePosition;

        // Отладка: нажатие F12 выводит информацию о зуме, питче и состоянии LOD всех кораблей
        if (Input.GetKeyDown(KeyCode.F12))
        {
            float zoomNormalized = Mathf.Clamp01((currentZoom - minZoom) / (maxZoom - minZoom));
            float pitch = zoomToPitchCurve.Evaluate(zoomNormalized);
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            Debug.Log($"Zoom: {currentZoom}, Pitch: {pitch}");

            var ships = FindObjectsOfType<ShipController>();
            foreach (var s in ships)
            {
                LODController lod = s.GetComponent<LODController>();
                if (lod != null)
                    Debug.Log($"{s.name}: LOD active: {(lod.impostorRoot.activeSelf ? "Impostor" : "Model")}, distance={Vector3.Distance(transform.position, s.transform.position):F1}");
            }
        }
    }

    /// <summary>
    /// Обрабатывает перемещение камеры (WASD, края экрана).
    /// Скорость зависит от зума (чем ближе к земле, тем медленнее).
    /// </summary>
    private void HandleMovement()
    {
        Vector3 moveDirection = Vector3.zero;

        // Вычисляем текущую скорость движения в зависимости от зума
        float zoomNormalized = Mathf.Clamp01((currentZoom - minZoom) / (maxZoom - minZoom));
        float currentMoveSpeed = moveSpeedCurve.Evaluate(zoomNormalized);

        // Направления движения относительно текущего yaw (поворота камеры)
        Quaternion yawRotation = Quaternion.Euler(0, currentYaw, 0);
        Vector3 cameraForward = yawRotation * Vector3.forward;
        Vector3 cameraRight = yawRotation * Vector3.right;

        // Панорамирование краями экрана
        if (enableEdgePanning && hasInitialized)
        {
            Vector3 mousePos = Input.mousePosition;
            if (mousePos.x < edgePanThreshold)
                moveDirection -= cameraRight;
            else if (mousePos.x > Screen.width - edgePanThreshold)
                moveDirection += cameraRight;
            if (mousePos.y < edgePanThreshold)
                moveDirection -= cameraForward;
            else if (mousePos.y > Screen.height - edgePanThreshold)
                moveDirection += cameraForward;
            moveDirection = moveDirection.normalized;
        }

        // Ввод с клавиатуры (WASD)
#if ENABLE_INPUT_SYSTEM
        if (useNewInputSystem && moveAction?.action != null)
        {
            Vector2 mv = moveAction.action.ReadValue<Vector2>();
            moveDirection += cameraForward * mv.y;
            moveDirection += cameraRight * mv.x;
        }
        else
#endif
        {
            if (Input.GetKey(moveForwardKey))
                moveDirection += cameraForward;
            if (Input.GetKey(moveBackwardKey))
                moveDirection -= cameraForward;
            if (Input.GetKey(moveRightKey))
                moveDirection += cameraRight;
            if (Input.GetKey(moveLeftKey))
                moveDirection -= cameraRight;
        }

        targetPosition += moveDirection * currentMoveSpeed * Time.deltaTime;
    }

    /// <summary>
    /// Обрабатывает зум (скролл мыши) и плавно интерполирует текущее расстояние.
    /// </summary>
    private void HandleZoom()
    {
        float scrollInput = 0f;

#if ENABLE_INPUT_SYSTEM
        if (useNewInputSystem && zoomAction?.action != null)
            scrollInput = zoomAction.action.ReadValue<float>();
        else
#endif
            scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            targetZoom -= scrollInput * scrollSensitivity;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }

        currentZoom = Mathf.Lerp(currentZoom, targetZoom, zoomLerpSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Обрабатывает вращение камеры (yaw) с помощью мыши (средняя кнопка) и клавиш Q/E.
    /// </summary>
    private void HandleYawRotation()
    {
        if (!enableYawRotation || !hasInitialized) return;

#if ENABLE_INPUT_SYSTEM
        if (useNewInputSystem)
        {
            bool rotatingMouse = false;
            if (rotateMouseButtonAction?.action != null && rotateMouseButtonAction.action.ReadValue<float>() > 0f)
                rotatingMouse = true;

            if (rotatingMouse && pointerDeltaAction?.action != null)
            {
                Vector2 delta = pointerDeltaAction.action.ReadValue<Vector2>();
                currentYaw += delta.x * yawRotationSpeed * Time.deltaTime;
            }

            if (rotateLeftAction?.action != null && rotateLeftAction.action.ReadValue<float>() > 0f)
                currentYaw -= keyboardYawSpeed * Time.deltaTime;
            if (rotateRightAction?.action != null && rotateRightAction.action.ReadValue<float>() > 0f)
                currentYaw += keyboardYawSpeed * Time.deltaTime;
        }
        else
#endif
        {
            // Вращение мышью (средняя кнопка)
            if (Input.GetMouseButton(rotateMouseButton))
            {
                float mouseDeltaX = Input.mousePosition.x - lastMousePosition.x;
                currentYaw += mouseDeltaX * yawRotationSpeed / 1000f;
            }

            // Вращение клавиатурой
            if (Input.GetKey(rotateLeftKey))
                currentYaw -= keyboardYawSpeed * Time.deltaTime;
            if (Input.GetKey(rotateRightKey))
                currentYaw += keyboardYawSpeed * Time.deltaTime;
        }

        currentYaw = currentYaw % 360f; // нормализуем угол
    }

    /// <summary>
    /// Вычисляет желаемую позицию камеры на основе текущего зума, питча и yaw,
    /// затем плавно перемещает камеру к этой позиции.
    /// </summary>
    private void UpdateCameraPosition()
    {
        // Ограничиваем целевую точку границами карты, если включено
        if (clampToMapBounds)
            targetPosition = MapBounds.ClampToBounds(targetPosition);

        // Вычисляем текущий угол наклона (питч) по кривой от зума
        float zoomNormalized = Mathf.Clamp01((currentZoom - minZoom) / (maxZoom - minZoom));
        float pitch = zoomToPitchCurve.Evaluate(zoomNormalized);
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion yawRotation = Quaternion.Euler(0, currentYaw, 0);
        float pitchRad = pitch * Mathf.Deg2Rad;
        // Расстояние от целевой точки до камеры в горизонтальной плоскости и по высоте
        float horizontalDistance = currentZoom * Mathf.Cos(pitchRad);
        float heightAboveTarget = currentZoom * Mathf.Sin(pitchRad);

        // Вектор от целевой точки к камере (направление "назад" от цели)
        Vector3 backDirection = yawRotation * new Vector3(0, 0, -horizontalDistance);
        Vector3 desiredPosition = targetPosition + backDirection + Vector3.up * heightAboveTarget;

        // Плавное перемещение
        Vector3 newPosition = Vector3.Lerp(transform.position, desiredPosition, 8f * Time.deltaTime);
        transform.position = newPosition;

        // Устанавливаем поворот камеры (питч + yaw)
        transform.rotation = Quaternion.Euler(pitch, currentYaw, 0f);
    }

    /// <summary>
    /// Устанавливает новую целевую точку (центр камеры) и фокусируется на ней.
    /// </summary>
    public void FocusOn(Vector3 position) => targetPosition = position;

    /// <summary>
    /// Устанавливает целевое расстояние зума (будет достигнуто плавно).
    /// </summary>
    public void SetZoom(float zoom) => targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);

    /// <summary>
    /// Возвращает текущее расстояние зума.
    /// </summary>
    public float GetCurrentZoom() => currentZoom;

    /// <summary>
    /// Возвращает текущую целевую точку (точку, вокруг которой вращается камера).
    /// </summary>
    public Vector3 GetTargetPosition() => targetPosition;
}