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
        data.maxExtensions = 10; // 

        // Задаём существующие ячейки (форма корабля) – например, 5x5 прямоугольник
        // Можно задать произвольную форму, если нужно
        data.existingCells = new List<Vector2Int>();
        for (int x = -3; x <= 3; x++)
            for (int z = -3; z <= 3; z++)
                data.existingCells.Add(new Vector2Int(x, z));

        data.existingCells.Add(new Vector2Int(-3, -4));
        data.existingCells.Add(new Vector2Int(-4, -4));
        // Ядро в центре (2,2)
        data.blocks.Add(new BlockData { x = 0, z = 0, type = BlockType.Core });

        // Несколько дополнительных модулей для наглядности
        /*data.blocks.Add(new BlockData { x = 0, z = 1, type = BlockType.Engine });
        data.blocks.Add(new BlockData { x = 1, z = 0, type = BlockType.Weapon });
        data.blocks.Add(new BlockData { x = 1, z = 1, type = BlockType.Armor });*/

        return data;
    }
}