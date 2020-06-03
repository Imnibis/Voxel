using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Block", menuName = "Minecraft/Block"), System.Serializable]
public class Block : ScriptableObject
{
    public string blockID = "stone";
    public string modelPath = "block/stone";

    [HideInInspector] public List<Model> models = new List<Model>();

    public void Initialize()
    {
        models = ModelManager.InitializeBlockModel(modelPath);
    }
}