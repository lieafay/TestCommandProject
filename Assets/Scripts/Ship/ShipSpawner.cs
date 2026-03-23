using UnityEngine;
using System.Collections.Generic;

public static class ShipSpawner
{
    public static GameObject SpawnShip(ShipData data, Material foundationMaterial)
    {
        GameObject shipObject = new GameObject(data.shipType);
        shipObject.transform.position = data.position;
        shipObject.transform.rotation = data.rotation;

        ShipController controller = shipObject.AddComponent<ShipController>();
        // После Awake у нас уже есть modelRoot и impostorRoot
        // Создаём ProceduralFoundation на modelRoot
        ProceduralFoundation foundation = controller.modelRoot.AddComponent<ProceduralFoundation>();
        foundation.foundationMaterial = foundationMaterial;
        foundation.cellSize = data.cellSize;
        foundation.thickness = data.foundationThickness;
        controller.foundation = foundation; // связываем

        controller.InitializeFromData(data);

        controller.GenerateImpostor();

        LODController lod = shipObject.GetComponent<LODController>();
        if (lod == null) lod = shipObject.AddComponent<LODController>();
        lod.modelRoot = controller.modelRoot;
        lod.impostorRoot = controller.impostorRoot;
        lod.distanceToSwitch = 50f;

        return shipObject;
    }
}