using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Map : MonoBehaviour
{
    // The Width(X), Height(Y), and Length(Z) of the Map
    [Header("- MAP DIMENSIONS -")]
    [SerializeField] int Width = 5;
    [SerializeField] int Height = 1;
    [SerializeField] int Length = 5;
    
    // VARIANT VECTOR 3 of the MAP DIMENSIONS, (WIDTH, HEIGHT, LENGTH)
    Vector3 MapDimensions;

    [Tooltip("Turns on Custom Threading Value, Default Value is 4000 in relation to the number of modules within the map until multi threading initializes")]
    [SerializeField] bool ControlMultiThreading = false;

    // THE  MINIMUM SIZE TO INITIALIZE MULTI THREADING 
    const int MinimumSizeForMultiThreading = 4000;

    [SerializeField] int CustomMinimumThreadingValue = MinimumSizeForMultiThreading;


    

    // MAP ARRAY. CONTAINS ALL MODULE ELEMENTS
    Sc_MapModule[,,] MapArr;


    // VARIABLE CONTROLS
    [Header("- MAP CONTROLS -")]
    // PATH CONTROLS
    [SerializeField] bool Generate_Path = false;
    [SerializeField] int Total_Paths = 1;
    [Tooltip("Name of layer associated with Path Based Tiles")]
    [SerializeField] string Path;

    // FLOOR CONTROLS
    [SerializeField] bool Generate_Floor = false;
    [Tooltip("Name of layer associated with Floor Based Tiles")]
    [SerializeField] string Floor;

    // THE LOCATION MODULES ARE BUILT TO
    [Header("- BUILD TRANSFORM -")]
    [Tooltip("The PARENT of the generated map pieces")]
    [SerializeField] GameObject LocalMapTransform = null;

    // LIST OF BUILT ELEMENTS WITHIN THE SCENE
    List<GameObject> Build = new List<GameObject>();


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
    public void GenerateMap() {
        // SPEED DEBUGGING COMMAND - Begin
        // Helper.Instance.START();

        // Sets Map Generator Map and Elements
        if (!SetupForGeneration()) return;

        // Sets Map Generator with Variable Changes
        if(!SetupVariablesForGeneration()) return;

        Generate();
    }


    // Setup Elements for Map Generation 
    private bool SetupForGeneration()
    {
        if (Sc_ModGenerator.Instance.GetModules() == null || Sc_ModGenerator.Instance.GetModules().Count == 0) return false;

        // Check for miss feed of map dimensions
        if (Width <= 0 || Height <= 0 || Length <= 0)
        {
            Debug.LogError("1 OR more MAP DIMENSIONS are 0 OR negative");
            return false;
        }

        // Sets the dimensions of the Map into a Vector 3
        MapDimensions = new Vector3(Width, Height, Length);

        //  Generates new Array with map Dimensions
        MapArr = new Sc_MapModule[Width, Height, Length];

        // creates new modules with COORDINATES, MODULE CHOICES and SETS THEM EMPTY
        for (int y = 0; y < Height; y++)
        {
            for (int z = 0; z < Length; z++)
            {
                for (int x = 0; x < Width; x++)
                {
                    //Creates new Module with modules
                    Sc_MapModule mod = new Sc_MapModule(new Vector3(x, y, z));
                    mod.ResetModule(Sc_ModGenerator.Instance.GetModules());
                    MapArr[x, y, z] = mod;
                }
            }
        }

        // Set up / Generate Randoms based on the maps size
        ThreadRandomiser.Instance.GenerateRandomNumbers(MapArr.Length);

        // Set up Helpers connections
        // - Sets Instantiation transform to the Dungeon Reference 
        // - Sets the local list to a reference of the BUILD List
        Helper.Instance.SetMapBuildParent(LocalMapTransform);
        Helper.Instance.SetBuildList(ref Build);

        // Pre Checks before Creation
        AstarPF = new Sc_AstarPathFinding();
        if (AstarPF == null) return false;

        MapGen = new Sc_MapGenerator(ref MapArr);
        if (MapGen == null) return false;

        // Clears objects in scene
        Helper.Instance.ClearGOList();

        // All has passed so return TRUE
        return true;
    }

    // Sets up all the Variable elements users can change to the map
    private bool SetupVariablesForGeneration()
    {
        // Generates basic random path based on user input
        if (Generate_Path) {
            if (!GeneratePath()) return false;
        }

        // Sets if user wants floor.
        Helper.Instance.SetGenerateFloor(Generate_Floor);
        if (Generate_Floor) Helper.Instance.SetLevelToType(ref MapArr, LayerMask.NameToLayer(Floor), 0);


        // ALL PASS so returns true
        return true;
    }

    // Generates path by assigning modules as PATH
    private bool GeneratePath()
    {   
        // Check if user has set 'impossible' values
        if(Total_Paths <=  0)
        {
            Debug.LogError("TOTAL PATHS is 0 OR negative: " + Total_Paths);
            return false;
        }


        // Generate list of Pathing Elements
        List<Sc_MapModule> Pathing = AstarPF.GeneratePath(MapArr, MapDimensions, Total_Paths + 1);

        // sets all the path modules in the map to a pathing specific module
        foreach (Sc_MapModule module in Pathing)
        {
            // sets module to only include "PATH" type modules
            module.SetModuleTypeBasedOnLayer(LayerMask.NameToLayer(Path));
            // Propagate the path
            MapGen.Propagate(module.mapPos, MapDimensions);
        }

        // sets all NON path modules in the map to not include "PATH" specific modules
        foreach (Sc_MapModule module in MapArr)
        {
            if (!Pathing.Contains(module))
            {
                module.RemoveModuleTypeBasedOnLayer(LayerMask.NameToLayer(Path));
            }
        }

        // returns true since all succeeded
        return true;
    }

    // Generates the MAP
    private void Generate()
    {
        // gets the current total modules that the generator will have to create
        int currentModuleCount = Height * Width * Length;

        // depending whether the current module count is larger than the minimum Multi-Threading will take over.
        if (currentModuleCount > (ControlMultiThreading ? CustomMinimumThreadingValue : MinimumSizeForMultiThreading))
        {
            // Sets new Multi Threader Class
            MultiThreadMap = new MapMultiThreader(ref MapArr, ref MapGen, ref MapDimensions);
            // Starts corutine within MultiThreader  Class
            StartCoroutine(MultiThreadMap.GenerateMultiThreadMap(MapDimensions));
        }
        else
        {
            MapGen.GenerateMap(MapDimensions);
            // Builds map based on map's modules
            Helper.Instance.BuildMap(ref MapArr);

            // SPEED DEBUGGING COMMANDS - END + PRINT
            //Helper.Instance.END();
            //Debug.Log(Helper.Instance.GetTotalTime());
        }
    }



}


