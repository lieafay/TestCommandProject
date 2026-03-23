using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // --- Singleton ---
    public static GameManager Instance { get; private set; }

    // --- Настройки ---
    [Header("Ресурсы")]
    public Material foundationMaterial; // перетащите сюда материал из проекта

    [Header("Настройки сохранения")]
    public string saveFileName = "game.save";

    [Header("Префабы (если понадобятся)")]
    public GameObject shipPrefab; // необязательно, если спавним чисто из данных

    // --- Состояние игры ---
    private List<ShipData> currentShips = new List<ShipData>();
    private bool isLoading = false;

    void Awake()
    {
        // Реализация синглтона
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Здесь определяем: новая игра или загрузка?
        // Для примера: если есть сохранение, загружаем, иначе создаём новую игру.
        if (SaveExists())
        {
            LoadGame();
        }
        else
        {
            NewGame();
        }
    }

    /// <summary>
    /// Начать новую игру
    /// </summary>
    public void NewGame()
    {
        Debug.Log("Начинаем новую игру");
        isLoading = false;

        // Очищаем сцену от всего, что может быть
        ClearAllShips();

        // Создаём станцию в центре
        ShipData stationData = StationPreset.CreateDefaultStation(Vector3.zero);
        GameObject station = ShipSpawner.SpawnShip(stationData, foundationMaterial);
        currentShips.Add(stationData); // сохраняем данные в списке

        // Здесь можно добавить спавн стартовых кораблей, ресурсов и т.д.
    }

    /// <summary>
    /// Загрузить сохранённую игру
    /// </summary>
    public void LoadGame()
    {
        Debug.Log("Загружаем сохранение");
        isLoading = true;

        // Читаем файл
        string path = GetSavePath();
        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream stream = new FileStream(path, FileMode.Open))
            {
                SaveData saveData = (SaveData)formatter.Deserialize(stream);
                // Восстанавливаем корабли
                LoadShips(saveData.ships);
            }
        }
        else
        {
            Debug.LogWarning("Файл сохранения не найден, начинаем новую игру");
            NewGame();
        }
    }

    /// <summary>
    /// Сохранить текущее состояние игры
    /// </summary>
    public void SaveGame()
    {
        Debug.Log("Сохраняем игру");

        // Собираем данные всех существующих кораблей
        List<ShipData> ships = new List<ShipData>();
        foreach (var ship in FindObjectsOfType<ShipController>())
        {
            ships.Add(ship.SaveToData());
        }

        SaveData saveData = new SaveData { ships = ships };
        string path = GetSavePath();
        BinaryFormatter formatter = new BinaryFormatter();
        using (FileStream stream = new FileStream(path, FileMode.Create))
        {
            formatter.Serialize(stream, saveData);
        }
    }

    // --- Вспомогательные методы ---

    private bool SaveExists()
    {
        return File.Exists(GetSavePath());
    }

    private string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, saveFileName);
    }

    private void ClearAllShips()
    {
        foreach (var ship in FindObjectsOfType<ShipController>())
        {
            Destroy(ship.gameObject);
        }
        currentShips.Clear();
    }

    private void LoadShips(List<ShipData> ships)
    {
        ClearAllShips();
        foreach (var data in ships)
        {
            GameObject ship = ShipSpawner.SpawnShip(data, foundationMaterial);
            // Можно добавить в список currentShips, но он будет перезаписан при повторном сохранении.
            // Лучше не хранить отдельно, а всегда собирать из сцены.
        }
        currentShips = new List<ShipData>(ships); // для быстрого доступа
    }
}

// --- Класс для сериализации всего сохранения ---
[System.Serializable]
public class SaveData
{
    public List<ShipData> ships;
}