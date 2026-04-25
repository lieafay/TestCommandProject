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

        List<ShipData> startShips = GameSetupLoader.LoadStartShips();
        if (startShips != null && startShips.Count > 0)
        {
            FixStartShipPositions(startShips);
            foreach (var shipData in startShips)
            {
                GameObject ship = ShipSpawner.SpawnShip(shipData, foundationMaterial);
                currentShips.Add(shipData);
            }
        }
        else
        {
            Debug.LogWarning("Не удалось загрузить стартовые корабли из JSON. Сцена будет пустой.");
        }
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

    private void FixStartShipPositions(List<ShipData> startShips)
    {
        if (startShips == null || startShips.Count == 0)
            return;

        // Станция всегда фиксируется в центре и должна идти первой
        startShips[0].position = Vector3.zero;
        List<Bounds> placedBounds = new List<Bounds> { GetShipBoundsXZ(startShips[0]) };

        for (int index = 1; index < startShips.Count; index++)
        {
            ShipData ship = startShips[index];
            int attempts = 0;

            while (attempts < 1000)
            {
                Bounds shipBounds = GetShipBoundsXZ(ship);
                float shiftX = 0f;

                foreach (Bounds placed in placedBounds)
                {
                    if (BoundsOverlapXZ(shipBounds, placed))
                    {
                        shiftX = Mathf.Max(shiftX, placed.max.x - shipBounds.min.x + 1.0f);
                    }
                }

                if (shiftX <= 0f)
                    break;

                ship.position += new Vector3(shiftX, 0f, 0f);
                attempts++;
            }

            if (attempts >= 1000)
            {
                Debug.LogWarning($"Не удалось разместить корабль {ship.shipType} без перекрытия после {attempts} попыток.");
            }

            placedBounds.Add(GetShipBoundsXZ(ship));
        }
    }

    private Bounds GetShipBoundsXZ(ShipData ship)
    {
        if (ship == null)
            return new Bounds();

        if (ship.existingCells == null || ship.existingCells.Count == 0)
        {
            return new Bounds(ship.position, new Vector3(ship.cellSize, 1f, ship.cellSize));
        }

        int minCellX = int.MaxValue;
        int maxCellX = int.MinValue;
        int minCellZ = int.MaxValue;
        int maxCellZ = int.MinValue;

        foreach (var cell in ship.existingCells)
        {
            minCellX = Mathf.Min(minCellX, cell.x);
            maxCellX = Mathf.Max(maxCellX, cell.x);
            minCellZ = Mathf.Min(minCellZ, cell.y);
            maxCellZ = Mathf.Max(maxCellZ, cell.y);
        }

        float halfCell = ship.cellSize * 0.5f;
        float minX = ship.position.x + minCellX * ship.cellSize - halfCell;
        float maxX = ship.position.x + maxCellX * ship.cellSize + halfCell;
        float minZ = ship.position.z + minCellZ * ship.cellSize - halfCell;
        float maxZ = ship.position.z + maxCellZ * ship.cellSize + halfCell;

        Vector3 center = new Vector3((minX + maxX) * 0.5f, ship.position.y, (minZ + maxZ) * 0.5f);
        Vector3 size = new Vector3(maxX - minX, 1f, maxZ - minZ);
        return new Bounds(center, size);
    }

    private bool BoundsOverlapXZ(Bounds a, Bounds b)
    {
        return a.min.x < b.max.x && a.max.x > b.min.x &&
               a.min.z < b.max.z && a.max.z > b.min.z;
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