using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using UnityEngine;



[ExecuteInEditMode]
public class Sc_Map : MonoBehaviour
{
    // Helper
    Helper helper = Helper.Instance;


    [Header("Map Dimensions")]
    // The Width(X), Height(Y), and Length(Z) of the Map
    [SerializeField] int Width = 5;
    [SerializeField] int Height = 5;
    [SerializeField] int Length = 5;

    [Range(0, 5)]
    public int cool = 5;

    Vector3 MapDimensions;

    // Map list, this is the main list that all the processes use
    // 1). 
    // 2). Path Finding
    // 3). Map Generator
    //only Visible during This Construction Phase
    Sc_MapModule[,,] Map;

    List<Sc_MapModule> LastPath = new List<Sc_MapModule>();


    [Header("Variations")]
    // Declarations
    [SerializeField] bool Generate_Shape = false; // Currently not in use
    [SerializeField] bool Generate_Path = false;
    [SerializeField] int Total_Paths = 1;
    [SerializeField] bool Generate_Floor = false;




    // Scripts that are combined in this
    Sc_AstarPathFinding AstarPF;
    Sc_MapGenerator MapGen;

    // Multi Thread Map Builder
    MapMultiThreader MultiThreadMap;

    // Randomiser
    ThreadRandomiser random;

    // Generates map based on initial inputs
    public void GenerateMap()
    {

        helper.GetModules();
        if (helper.GetModules() == null || helper.GetModules().Count == 0) return;

        if (Width <= 0 || Height <= 0 || Length <= 0)
        {
            Debug.LogError("1 OR more MAP SIZES are 0 OR negative");
            return;
        }

        // Sets the dimensions of the Map into a Vector 3
        MapDimensions = new Vector3(Width, Height, Length);

        Map = new Sc_MapModule[Width, Height, Length];

        // creates new Wave Function Collapse Modules with Size and Modules List
        for (int y = 0; y < Height; y++)
        {
            for (int z = 0; z < Length; z++)
            {
                for (int x = 0; x < Width; x++)
                {
                    //Creates new Module with modules
                    Sc_MapModule mod = new Sc_MapModule(new Vector3(x, y, z));
                    mod.ResetModule(helper.GetModules());
                    Map[x, y, z] = mod;
                }
            }
        }


        // Pre Checks before Creation
        AstarPF = new Sc_AstarPathFinding();
        if (AstarPF == null) return;

        MapGen = new Sc_MapGenerator(Map);


        random = ThreadRandomiser.Instance;
        Debug.Log(Map.Length);
        random.GenerateRandomNumbers(Map.Length);


        /* Due to the way I am Calculating the positions of the Modules while they are compiled in a string, Y needs to be calculate first then Z and finally X
            - Y is multiplied by both Z and X's max sizes meaning during the setting phase it will be assigned the least
            - Z is multiplied by X's max size meaning it will be in the middle and added per Y
            - X is the base so it will added per X and Z
         */


        if (Generate_Shape)
        {
            // MapShapeGenerator.GenerateMapShape([THE MAP]);
        }

        if (Generate_Path)
        {
            LastPath = AstarPF.GeneratePath(Map, MapDimensions, Total_Paths + 1);
            if(LastPath != null || LastPath.Count <= 0)
            {
                Debug.Log((LastPath != null) ? "NEW PATH GENERATED: " + LastPath.Count : 0);

                if (LastPath != null)
                {
                    List<Sc_MapModule> Pathing = LastPath;

                    // sets all the path modules in the map to a pathing specific module
                    foreach (Sc_MapModule module in Pathing)
                    {
                        // sets module to only include "PATH" type modules
                        module.SetModuleTypeBasedOnLayer(LayerMask.NameToLayer("PATH"));
                        // Propagate the path
                        MapGen.Propagate(module.mapPos, MapDimensions, MapDimensions);
                    }

                    //// sets all NON path modules in the map to not include "PATH" specific modules
                    foreach (Sc_MapModule module in Map)
                    {
                        if (!LastPath.Contains(module))
                        {
                            module.RemoveModuleTypeBasedOnLayer(LayerMask.NameToLayer("PATH"));

                        }
                    }
                }

            } else { Debug.Log("PATH GEN FAILED"); }

        }



        if (Generate_Floor) { SetLevelToType(LayerMask.NameToLayer("FLOOR"), 0); }

        // Clears objects in scene
        helper.ClearGOList();

        if (MapDimensions.x > 15 && MapDimensions.z > 15)
        {
            StartCoroutine(MultiThreadMap.GenerateMultiThreadMap(MapDimensions));
        }
        else
        {
            MapGen.GenerateMap(new Vector2(MapDimensions.x, MapDimensions.z), MapDimensions);
            // Builds map based on map's modules
            helper.BuildMap(ref Map); 
        }
    }

    void SetLevelToType(LayerMask _layer, int _level)
    {
        foreach (Sc_MapModule mod in GetModulesFromLevel(_level))
        {
            List<Sc_Module> toRemove = new List<Sc_Module>();
            foreach (Sc_Module option in mod.GetOptions())
            {
                if (option.GetLayerType() != (option.GetLayerType() | (1 << _layer)))
                {
                    toRemove.Add(option);
                }
            }

            foreach (Sc_Module option in toRemove)
            {
                mod.RemoveOption(option);
            }
        }
    }


    List<Sc_MapModule> GetModulesFromLevel(int _level)
    {
        List<Sc_MapModule> modules = new List<Sc_MapModule>();

        for (int z = 0; z < MapDimensions.z; z++)     
        {
            for (int x = 0; x < MapDimensions.x; x++)
            {
                modules.Add(helper.GetModule(ref Map, new Vector3(x, _level, z)));
            }
        }

        return modules;
    }


    void RebuildMap()
    {
        if (Generate_Floor) { SetLevelToType(LayerMask.NameToLayer("FLOOR"), 0); }
        MapGen.GenerateMap(new Vector2(MapDimensions.x, MapDimensions.z), MapDimensions);
    }

}


