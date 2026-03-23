using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Тип блока (модуля)
/// </summary>
[Serializable]
public enum BlockType
{
    Empty = 0,
    Core,
    Weapon,
    Engine,
    Armor,
    Shield
}

/// <summary>
/// Данные одного блока (модуля)
/// </summary>
[Serializable]
public class BlockData
{
    public int x;               // координата по X в сетке
    public int z;               // координата по Z в сетке
    public BlockType type;      // тип блока
    public int health = 100;    // текущая прочность (может пригодиться)
}

/// <summary>
/// Полные данные корабля/станции для сохранения/загрузки
/// </summary>
[System.Serializable]
public class ShipData
{
    public string shipType;
    public Vector3 position;
    public Quaternion rotation;
    public List<Vector2Int> existingCells = new List<Vector2Int>();
    public float cellSize = 1f;
    public float foundationThickness = 0.2f;
    public List<BlockData> blocks = new List<BlockData>();
}