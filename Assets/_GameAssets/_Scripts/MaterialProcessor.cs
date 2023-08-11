using System.Collections.Generic;

namespace HLProject
{
    public enum MaterialType
    {
        Base, Wood, Metal, Concrete, Dirt, Flesh, Glass, Duct, Grass, Gravel, MetalBox, MetalGrate, Chain, Mud, Sand, WoodPanel
    }

    public class MaterialProcessor
    {
        static readonly Dictionary<string, MaterialType> materialTypeDictionary = new Dictionary<string, MaterialType>()
        {
            { "DevFloor", MaterialType.Base },
            { "DevWall", MaterialType.Wood },
        };

        public static MaterialType GetMaterialType(string materialName)
        {
            try { return materialTypeDictionary[materialName]; }
            catch { return MaterialType.Base; }
        }
    }
}
