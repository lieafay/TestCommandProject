using UnityEngine;

/// <summary>
/// Поворачивает объект так, чтобы его лицевая сторона всегда смотрела на камеру.
/// Используется для Canvas, чтобы спрайт всегда был видимым.
/// </summary>
public class Billboard : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCamera == null) return;
        // Направление "вперёд" объекта = направление от объекта к камере
        transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                         mainCamera.transform.rotation * Vector3.up);
    }
}