using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JsonObjects
{
    [System.Serializable]
    public class BlockModel
    {
        public string parent;
        public BlockModelDisplay display;
        public Dictionary<string, string> textures;
        public BlockModelElement[] elements;
    }

    [System.Serializable]
    public class BlockModelDisplay
    {
        public BlockModelTransform gui;
        public BlockModelTransform ground;
        public BlockModelTransform @fixed;
        public BlockModelTransform head;
        public BlockModelTransform thirdperson_righthand;
        public BlockModelTransform firstperson_righthand;
        public BlockModelTransform firstperson_lefthand;
    }

    [System.Serializable]
    public class BlockModelTransform
    {
        public float[] rotation;
        public float[] translation;
        public float[] scale;
    }

    [System.Serializable]
    public class BlockModelElement
    {
        public int[] from;
        public int[] to;
        public Dictionary<string, BlockModelFace> faces;
    }

    [System.Serializable]
    public class BlockModelFace
    {
        public int[] uv;
        public int? tintindex;
        public string texture;
        public string cullface;

        public BlockModelFace(string texture, string cullface)
        {
            this.texture = texture;
            this.cullface = cullface;
        }
    }
}

