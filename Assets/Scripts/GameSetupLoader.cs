using UnityEngine;
using System.Collections.Generic;

public static class GameSetupLoader
{
    private const string SetupResourcePath = "GameSetup";

    [System.Serializable]
    private class SetupContainer
    {
        public List<ShipData> ships;
    }

    public static List<ShipData> LoadStartShips()
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>(SetupResourcePath);
        if (jsonAsset == null)
        {
            Debug.LogWarning("GameSetup.json не найден в Resources.");
            return null;
        }
        SetupContainer container;
        try
        {
            container = JsonUtility.FromJson<SetupContainer>(jsonAsset.text);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка парсинга GameSetup.json: {e.Message}");
            return null;
        }
        if (container == null || container.ships == null || container.ships.Count == 0)
        {
            Debug.LogWarning("GameSetup.json не содержит кораблей.");
            return new List<ShipData>();
        }
        return container.ships;
    }
}
