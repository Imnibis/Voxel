using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureManager
{
    public static Texture2D texture;

    static int textureSize = 64;
    static int size = 0;
    static Vector2Int nextFreeCell = new Vector2Int(1, 0);
    public static void Initialize()
    {
        size = Mathf.CeilToInt(Mathf.Sqrt(BlockRegistry.Blocks.Count * 3));
        texture = new Texture2D(size * textureSize, size * textureSize);
        texture.anisoLevel = 16;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Point;
    }

    public static Vector2[] RegisterBlockTexture(string texturePath)
    {
        if (size == 0)
            Initialize();

        if(texturePath == "transparent")
        {
            List<Color> colors = new List<Color>();
            for (var i = 0; i < textureSize * textureSize; i++)
            {
                colors.Add(new Color(0, 0, 0, 0));
            }
            texture.SetPixels(0, 0, textureSize, textureSize, colors.ToArray());
            return new Vector2[] { Vector2.zero, new Vector2(1 / (float) size, 1 / (float) size) };
        }

        Texture2D blockTexture = Resources.Load<Texture2D>("textures/" + texturePath);
        Debug.Log("Texture: " + texturePath);
        texture.SetPixels(nextFreeCell.x * textureSize, nextFreeCell.y * textureSize, textureSize, textureSize, blockTexture.GetPixels());
        Vector2 uv1 = new Vector2(nextFreeCell.x / (float) size,  nextFreeCell.y / (float) size);
        Vector2 uv2 = new Vector2((nextFreeCell.x + 1) / (float) size, ( nextFreeCell.y + 1) / (float) size);
        nextFreeCell.x++;
        if(nextFreeCell.x == size)
        {
            nextFreeCell.x = 0;
            nextFreeCell.y++;
        }

        return new Vector2[] {uv1, uv2};
    }

    public static void SetTerrainTexture(Material terrain, Material tinted)
    {
        texture.Apply();
        terrain.SetTexture("_BaseColorMap", texture);
        tinted.SetTexture("_Terrain", texture);
    }
}
