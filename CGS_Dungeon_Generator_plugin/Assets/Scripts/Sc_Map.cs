


using System.Collections.Generic;



using UnityEngine;



[ExecuteInEditMode]
public class Sc_Map : MonoBehaviour
{

    [Header("Map Dimensions")]
    // The Width(X), Height(Y), and Length(Z) of the Map
    [SerializeField] int Width = 5;
    [SerializeField] int Height = 5;
    [SerializeField] int Length = 5;


    [SerializeField] int MinimumSizeForMultiThreading = 15;

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


    [Header("Final Build")]
    [SerializeField] GameObject Dungeon = null;
    [SerializeField] GameObject REFACTOR = null;

    [SerializeField] List<GameObject> Build = new List<GameObject>();


    // Scripts that are combined in this
    Sc_AstarPathFinding AstarPF;
    Sc_MapGenerator MapGen;

    // Multi Thread Map Builder
    MapMultiThreader MultiThreadMap;
    
    public void ClearMap()
    {
        Helper.Instance.SetBuildList(ref Build);
        Helper.Instance.ClearGOList();
    }

    // Generates map based on initial inputs
    public void GenerateMap()
    {


        Helper.Instance.START();

        if (!SetupForGeneration()) return;


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
                        MapGen.Propagate(module.mapPos, MapDimensions);
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

        Helper.Instance.SetGenerateFloor(Generate_Floor);
        if (Generate_Floor) Helper.Instance.SetLevelToType(ref Map, LayerMask.NameToLayer("FLOOR"), 0);

        // Clears objects in scene
        Helper.Instance.ClearGOList();

        if (MapDimensions.x > MinimumSizeForMultiThreading && MapDimensions.z > MinimumSizeForMultiThreading)
        {
            MultiThreadMap = new MapMultiThreader(ref Map, ref MapGen, ref MapDimensions);
            StartCoroutine(MultiThreadMap.GenerateMultiThreadMap(MapDimensions));
        }
        else
        {
            MapGen.GenerateMap(MapDimensions);
            // Builds map based on map's modules
            Helper.Instance.BuildMap(ref Map);
            Helper.Instance.END();
            Debug.Log(Helper.Instance.GetTotalTime());
        }
    }


    // Setup Generation 
    bool SetupForGeneration()
    {
        if (Sc_ModGenerator.Instance.GetModules() == null || Sc_ModGenerator.Instance.GetModules().Count == 0) return false;

        if (Width <= 0 || Height <= 0 || Length <= 0)
        {
            Debug.LogError("1 OR more MAP SIZES are 0 OR negative");
            return false;
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
                    mod.ResetModule(Sc_ModGenerator.Instance.GetModules());
                    Map[x, y, z] = mod;
                }
            }
        }

        // Set up / Generate Randoms based on the maps size
        ThreadRandomiser.Instance.GenerateRandomNumbers(Map.Length);

        // Set up Helpers connections
        Helper.Instance.SetMapBuildParent(Dungeon, REFACTOR);
        Helper.Instance.SetBuildList(ref Build);

        // Pre Checks before Creation
        AstarPF = new Sc_AstarPathFinding();
        if (AstarPF == null) return false;

        MapGen = new Sc_MapGenerator(ref Map);

        return true;
    }





}


