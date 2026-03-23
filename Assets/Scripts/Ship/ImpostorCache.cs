using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Кэширование сгенерированных спрайтов, чтобы не генерировать их повторно для одинаковых кораблей.
/// Пока не используется, но может пригодиться в будущем.
/// </summary>
public static class ImpostorCache
{
    private static Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static Sprite GetOrGenerate(GameObject ship, string key, int resolution = 256, float pitchAngle = 45f)
    {
        if (cache.TryGetValue(key, out Sprite spr))
            return spr;

        Texture2D tex = ImpostorGenerator.GenerateImpostorTexture(ship, resolution, pitchAngle);
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        cache[key] = sprite;
        return sprite;
    }
}