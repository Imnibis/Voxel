using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Biome", menuName = "Minecraft/Biome"), System.Serializable]
public class Biome : ScriptableObject
{
    public string biomeID;
    public float perlinScale;
    public List<BiomeLayer> layers;
}
