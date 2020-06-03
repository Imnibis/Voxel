using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockRegistry
{
    public static Dictionary<string, Block> Blocks = new Dictionary<string, Block>();

    public static void InitializeBlocks()
    {
        foreach(KeyValuePair<string, Block> entry in Blocks)
        {
            entry.Value.Initialize();
        }
    }
}
