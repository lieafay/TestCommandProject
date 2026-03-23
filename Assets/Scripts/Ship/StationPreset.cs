using UnityEngine;
using System.Collections.Generic;

public static class StationPreset
{
    public static ShipData CreateDefaultStation(Vector3 position)
    {
        ShipData data = new ShipData();
        data.shipType = "Station";
        data.position = position;
        data.rotation = Quaternion.identity;
        data.cellSize = 1f;
        data.foundationThickness = 0.2f;

        // Задаём существующие ячейки (форма корабля) – например, 5x5 прямоугольник
        // Можно задать произвольную форму, если нужно
        data.existingCells = new List<Vector2Int>();
        for (int x = 0; x < 5; x++)
            for (int z = 0; z < 5; z++)
                data.existingCells.Add(new Vector2Int(x, z));

        data.existingCells.Add(new Vector2Int(4, 5));
        // Ядро в центре (2,2)
        data.blocks.Add(new BlockData { x = 2, z = 2, type = BlockType.Core });

        // Несколько дополнительных модулей для наглядности
        data.blocks.Add(new BlockData { x = 2, z = 3, type = BlockType.Engine });
        data.blocks.Add(new BlockData { x = 3, z = 2, type = BlockType.Weapon });
        data.blocks.Add(new BlockData { x = 1, z = 2, type = BlockType.Armor });

        return data;
    }
}