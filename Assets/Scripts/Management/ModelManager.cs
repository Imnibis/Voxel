using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class ModelManager
{
    static Dictionary<string, Vector2[]> textureUvs = new Dictionary<string, Vector2[]>();

    public static void Init()
    {
        textureUvs.Add("transparent", TextureManager.RegisterBlockTexture("transparent"));
    }

    public static List<Model> InitializeBlockModel(string modelPath)
    {
        if (textureUvs.Count == 0)
            Init();
        Debug.Log("Creating block model: " + modelPath);
        JsonObjects.BlockModel blockModel = ParseModel(modelPath);
        List<Model> models = new List<Model>();
        if (blockModel.elements == null)
            return models;
        int i = 1;
        foreach(JsonObjects.BlockModelElement element in blockModel.elements)
        {
            Debug.Log("Creating submodel " + i);
            models.Add(CreateModel(blockModel, element));
            i++;
        }
        return models;
    }

    static Model CreateModel(JsonObjects.BlockModel blockModel, JsonObjects.BlockModelElement element)
    {
        Vector3[] voxelVerts = VoxelData.voxelVerts;
        Vector3[] faceChecks = VoxelData.faceChecks;
        Vector2[,] voxelUvs = new Vector2[6, 6];
        List<int> tintedFaces = new List<int>();

        DefineVertices(ref voxelVerts, element);

        Dictionary<string, int> directions = new Dictionary<string, int>();
        directions.Add("down", 3);
        directions.Add("up", 2);
        directions.Add("north", 4);
        directions.Add("south", 5);
        directions.Add("east", 1);
        directions.Add("west", 0);

        List<string> definedDirections = new List<string>();

        List<string> textures = new List<string>();
        
        if (element.faces == null)
            element.faces = new Dictionary<string, JsonObjects.BlockModelFace>();

        RegisterFaceTexture(blockModel, element);

        foreach (KeyValuePair<string, JsonObjects.BlockModelFace> pair in element.faces)
        {
            CreateFace(blockModel, pair, directions, ref definedDirections, ref faceChecks, ref voxelUvs, ref tintedFaces);
        }
        /* Part blocks
        foreach(KeyValuePair<string, int> pair in directions)
        {
            if(!definedDirections.Contains(pair.Key))
            {
                Debug.Log(pair.Key);
                CreateFace(blockModel, new KeyValuePair<string, JsonObjects.BlockModelFace>(pair.Key, new JsonObjects.BlockModelFace("transparent", "all")), directions, ref definedDirections, ref faceChecks, ref voxelUvs);
            }
        }
        */
        return new Model(voxelVerts, faceChecks, VoxelData.voxelTris, tintedFaces, voxelUvs);
    }

    static void CreateFace(JsonObjects.BlockModel blockModel, KeyValuePair<string, JsonObjects.BlockModelFace> pair, Dictionary<string, int> directions, ref List<string> definedDirections, ref Vector3[] faceChecks, ref Vector2[,] voxelUvs, ref List<int> tintedFaces)
    {
        definedDirections.Add(pair.Key);

        JsonObjects.BlockModelFace face = pair.Value;

        string realTexturePath = GetRealTexturePath(blockModel, face.texture);
        if (face.cullface != null && face.cullface != "all")
            faceChecks[directions[pair.Key]] = VoxelData.faceChecks[directions[face.cullface]];
        else if (face.cullface == "all")
            faceChecks[directions[pair.Key]] = Vector3.zero;
        else
            faceChecks[directions[pair.Key]] = new Vector3(2, 2, 2);
        int[] uv = face.uv != null ? face.uv : new int[4] { 0, 0, 16, 16 };
        Vector2[] uvs = VoxelData.voxelUvs;
        for (int i = 0; i < uvs.Length; i++)
        {
            voxelUvs[directions[pair.Key], i].x = uvs[i].x == 0 ? textureUvs[realTexturePath][0].x + uv[0] / 16 : textureUvs[realTexturePath][1].x + (uv[2] / 16) - 1;
            voxelUvs[directions[pair.Key], i].y = uvs[i].y == 0 ? textureUvs[realTexturePath][0].y + uv[1] / 16 : textureUvs[realTexturePath][1].y + (uv[3] / 16) - 1;
        }

        if (face.tintindex != null)
        {
            tintedFaces.Add(directions[pair.Key]);
        }
    }

    static void RegisterFaceTexture(JsonObjects.BlockModel blockModel, JsonObjects.BlockModelElement element)
    {
        foreach (KeyValuePair<string, JsonObjects.BlockModelFace> pair in element.faces)
        {
            JsonObjects.BlockModelFace face = pair.Value;

            string realTexturePath = GetRealTexturePath(blockModel, face.texture);

            if (!textureUvs.ContainsKey(realTexturePath))
            {
                textureUvs.Add(realTexturePath, TextureManager.RegisterBlockTexture(realTexturePath));
            }
        }
    }

    static void DefineVertices(ref Vector3[] voxelVerts, JsonObjects.BlockModelElement element)
    {
        for (int i = 0; i < voxelVerts.Length; i++)
        {
            voxelVerts[i].x = voxelVerts[i].x == 0 ? element.from[0] / 16f : element.to[0] / 16f;
            voxelVerts[i].y = voxelVerts[i].y == 0 ? element.from[1] / 16f : element.to[1] / 16f;
            voxelVerts[i].z = voxelVerts[i].z == 0 ? element.from[2] / 16f : element.to[2] / 16f;
        }
    }

    static string GetRealTexturePath(JsonObjects.BlockModel blockModel, string texturePath)
    {
        if (texturePath.StartsWith("#"))
        {
            string newTexturePath = blockModel.textures[texturePath.Substring(1)];
            return GetRealTexturePath(blockModel, newTexturePath);
        }
        else return texturePath;
    }

    static JsonObjects.BlockModel ParseModel(string modelPath)
    {
        TextAsset textAsset = Resources.Load<TextAsset>("models/" + modelPath);
        JsonObjects.BlockModel blockModel = JsonConvert.DeserializeObject<JsonObjects.BlockModel>(textAsset.text);

        if (blockModel.parent != null)
        {
            blockModel = MergeModels(ParseModel(blockModel.parent), blockModel);
        }

        return blockModel;
    }

    static JsonObjects.BlockModel MergeModels(JsonObjects.BlockModel parent, JsonObjects.BlockModel child)
    {
        MergeProperties<JsonObjects.BlockModelElement>(ref parent.elements, child.elements);

        if (child.display != null)
        {
            if (parent.display == null)
            {
                parent.display = child.display;
            }
            else
            {

                MergeProperties<JsonObjects.BlockModelTransform>(ref parent.display.gui, child.display.gui);
                MergeProperties<JsonObjects.BlockModelTransform>(ref parent.display.ground, child.display.ground);
                MergeProperties<JsonObjects.BlockModelTransform>(ref parent.display.@fixed, child.display.@fixed);
                MergeProperties<JsonObjects.BlockModelTransform>(ref parent.display.head, child.display.head);
                MergeProperties<JsonObjects.BlockModelTransform>(ref parent.display.thirdperson_righthand, child.display.thirdperson_righthand);
                MergeProperties<JsonObjects.BlockModelTransform>(ref parent.display.firstperson_righthand, child.display.firstperson_righthand);
                MergeProperties<JsonObjects.BlockModelTransform>(ref parent.display.firstperson_lefthand, child.display.firstperson_lefthand);
            }
        }
        if (child.textures == null)
            return parent;
        if (parent.textures == null)
            parent.textures = child.textures;
        else
        {
            foreach (KeyValuePair<string, string> entry in child.textures)
            {
                if (parent.textures.ContainsKey(entry.Key))
                    parent.textures[entry.Key] = entry.Value;
                else
                    parent.textures.Add(entry.Key, entry.Value);
            }
        }
        return parent;
    }

    static void MergeProperties<T>(ref T parent, T child)
    {
        if (child != null)
            parent = child;
    }

    static void MergeProperties<T>(ref T[] parent, T[] child)
    {
        if (child != null)
            parent = child;
    }
}

public class Model
{
    public Vector3[] voxelVerts = new Vector3[8];
    public Vector3[] faceChecks = new Vector3[6];
    public int[,] voxelTris = new int[6, 6];
    public Vector2[,] voxelUvs = new Vector2[6, 6];

    public List<int> tintedFaces = new List<int>();

    public Model(Vector3[] voxelVerts, Vector3[] faceChecks, int[,] voxelTris, List<int> tintedFaces, Vector2[,] voxelUvs)
    {
        this.voxelVerts = voxelVerts;
        this.faceChecks = faceChecks;
        this.voxelTris = voxelTris;
        this.voxelUvs = voxelUvs;
        this.tintedFaces = tintedFaces;
    }
}