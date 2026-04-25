using UnityEngine;

/// <summary>
/// Управляет переключением между 3D-моделью и импостером в зависимости от расстояния до камеры.
/// modelRoot и impostorRoot – дочерние объекты корневого контроллера.
/// </summary>
public class LODController : MonoBehaviour
{
    public GameObject modelRoot;      // Дочерний объект с 3D-моделью
    public GameObject impostorRoot;   // Дочерний объект с Canvas (импостер)
    public float distanceToSwitch = 30f; // Порог переключения

    private Camera mainCamera;

    void Start()
    {
        RTSCameraController rtsCam = FindObjectOfType<RTSCameraController>();
        if (rtsCam != null)
            mainCamera = rtsCam.GetComponent<Camera>();
        else
            mainCamera = Camera.main;

        if (mainCamera == null)
            Debug.LogError("LODController: не найдена камера!");

        UpdateLOD();
    }

    void Update()
    {
        if (mainCamera == null) return;
        UpdateLOD();
    }

    void UpdateLOD()
    {
        float dist = Vector3.Distance(transform.position, mainCamera.transform.position);
        bool useImpostor = dist > distanceToSwitch;

        // Включаем/выключаем соответствующие дочерние объекты
        if (modelRoot != null) modelRoot.SetActive(!useImpostor);
        impostorRoot.SetActive(useImpostor);
    }
}