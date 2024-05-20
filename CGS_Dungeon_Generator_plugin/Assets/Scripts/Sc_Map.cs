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
    [Header("Map Dimensions")]
    // The Width(X), Height(Y), and Length(Z) of the Map
    [SerializeField] int Width = 5;
    [SerializeField] int Height = 5;
    [SerializeField] int Length = 5;

    Vector3 MapDimensions;

    // Map list, this is the main list that all the processes use
    // 1). 
    // 2). Path Finding
    // 3). Map Generator
    //only Visible during This Construction Phase
    List<Sc_MapModule> Map = new List<Sc_MapModule>();
    List<Sc_Module> modules = new List<Sc_Module>();

    [Header("Final Build")]
    [SerializeField] GameObject Dungeon = null;
    [SerializeField] public List<GameObject> m_Build = new List<GameObject>();



    List<Sc_MapModule> LastPath = new List<Sc_MapModule>();




    [Header("Variations")]
    // Declarations
    [SerializeField] bool Generate_Shape = false; // Currently not in use
    [SerializeField] bool Generate_Path = false;
    [SerializeField] int Total_Paths = 1;
    [SerializeField] bool Generate_Floor = false;


    //Fail indicator
    [Header("Fail Indicator")]
    [SerializeField] GameObject FAIL = null;
  



    // Scripts that are combined in this
    Sc_AstarPathFinding AstarPF;
    Sc_MapGenerator MapGen;


    // Threads for Multi Threading 
    Thread TopLeftThread;
    Thread TopRightThread;
    Thread BottomLeftThread;
    Thread BottomRightThread;


    // Randomiser
    ThreadRandomiser random;

    // Generates map based on initial inputs
    public void GenerateMap()
    {

        modules.Clear();
        modules = new List<Sc_Module>(GetComponent<Sc_ModGenerator>().GetModules());
        if (modules == null || modules.Count == 0) return;

        if (Width <= 0 || Height <= 0 || Length <= 0)
        {
            Debug.LogError("1 OR more MAP SIZES are 0 OR negative");
            return;
        }

        // Sets the dimensions of the Map into a Vector 3
        MapDimensions = new Vector3(Width, Height, Length);

        Map.Clear();

        // creates new Wave Function Collapse Modules with Size and Modules List
        for (int y = 0; y < Height; y++)
        {
            for (int z = 0; z < Length; z++)
            {
                for (int x = 0; x < Width; x++)
                {
                    //Creates new Module with modules
                    Sc_MapModule mod = new Sc_MapModule(new Vector3(x, y, z));
                    mod.ResetModule(modules);
                    Map.Add(mod);
                }
            }
        }


        // Pre Checks before Creation
        AstarPF = GetComponent<Sc_AstarPathFinding>();
        if (AstarPF == null) return;

        MapGen = new Sc_MapGenerator(Map);
        if (MapGen == null) return;

        random = new ThreadRandomiser();


        /* Due to the way I am Calculating the positions of the Modules while they are compiled in a string, Y needs to be calculate first then Z and finally X
            - Y is multiplied by both Z and X's max sizes meaning during the setting phase it will be assigned the least
            - Z is multiplied by X's max size meaning it will be in the middle and added per Y
            - X is the base so it will added per X and Z
         */
        Debug.Log(Map.Count);


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

                    foreach (Sc_MapModule module in Pathing)
                    {
                        module.SetModuleTypeBasedOnLayer(LayerMask.NameToLayer("PATH"));
                        //MapGen.Propagate(module.mapPos);
                        //Instantiate(AstarPF.PathDetector, module.mapPos + new Vector3(0, 1, 0), Quaternion.identity, transform);
                    }

                    foreach (Sc_MapModule module in Map)
                    {

                        if (!LastPath.Contains(module))
                        {
                            module.RemoveModuleTypeBasedOnLayer(LayerMask.NameToLayer("PATH"));
                            //MapGen.Propagate(module.mapPos);
                        }
                    }
                }

            } else { Debug.Log("PATH GEN FAILED"); }

        }



        if (Generate_Floor) { SetLevelToType(LayerMask.NameToLayer("FLOOR"), 0); }

        // Clears objects in scene
        ClearGOList();

        if (MapDimensions.x > 15 && MapDimensions.z > 15)
        {
            GenerateThreadMapping(MapDimensions);
            StartCoroutine(GenerateMultiThreadMap(MapDimensions));
        }
        else
        {
            MapGen.GenerateMap(new Vector2(0, 0), new Vector2(MapDimensions.x, MapDimensions.z), MapDimensions);
            BuildMap();
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
                modules.Add(GetVectorModule(new Vector3(x, _level, z)));
            }
        }

        return modules;
    }

    public Sc_MapModule GetVectorModule(Vector3 _coords)
    {

        return Map[ConvertVec3ToListCoord(_coords)];
    }

    int ConvertVec3ToListCoord(Vector3 _coord)
    {
        return (int)(_coord.x + (_coord.y * MapDimensions.x * MapDimensions.z) + (_coord.z * MapDimensions.x));
    }



    private void GenerateThreadMapping(Vector3 _size)
    {
        // The Vectors of the Top Left Quadrant
        //   -> [X][O]
        //      [O][O]

        Vector2 TopLeft = new Vector2(0, 0);
        Vector2 BottomRight = new Vector2((int)_size.x / 2, (int)_size.z / 2);


        TopLeftThread = new Thread(() => MapGen.GenerateMap(TopLeft, BottomRight, _size));
        TopRightThread = new Thread(() => MapGen.GenerateMap(new Vector2(BottomRight.x, TopLeft.y), new Vector2(_size.x, BottomRight.y), _size));
        BottomLeftThread = new Thread(() => MapGen.GenerateMap(new Vector2(TopLeft.x, BottomRight.y), new Vector2(BottomRight.x, _size.z), _size));
        BottomRightThread = new Thread(() => MapGen.GenerateMap(new Vector2(BottomRight.x, BottomRight.y), new Vector2(_size.x, _size.z), _size));


        TopLeftThread.Start();
        TopRightThread.Start();
        BottomLeftThread.Start();
        BottomRightThread.Start();

        
    }

    private IEnumerator GenerateMultiThreadMap(Vector3 _size)
    {
        GenerateThreadMapping(_size);

        while (CheckThreadState()) // Check
        {
            yield return null;
        }

        BuildMap();
    }

    // The .isAlive property will return TRUE if the current thread is Active, FALSE if the current thread has finished or aborted
    private bool CheckThreadState()
    {
        if (TopLeftThread.IsAlive == true || TopRightThread.IsAlive == true || BottomLeftThread.IsAlive == true || BottomRightThread.IsAlive == true)
        {
            return true;
        }
        return false;
    }

    private void BuildMap()
    {
        Debug.Log("BuildMap");
        foreach (Sc_MapModule module in Map)
        {
            AttemptBuild(module);
        }
    }

    public bool AttemptBuild(Sc_MapModule _mod)
    {
        GameObject mod;

        if (!_mod.isCollapsed())
        {
            _mod.Collapse(random);
        }
        if (_mod.GetModule() == null)
        {
            FailBuild(_mod.mapPos);
            return false;
        }
        mod = _mod.GetModule().GetMesh();

        GameObject obj = Instantiate(mod, _mod.mapPos, Quaternion.Euler(ModRotation(_mod.GetModule())), Dungeon.transform);
        m_Build.Add(obj);
        return true;
    }

    // Clears the GameObject list
    public void ClearGOList()
    {
        if (m_Build.Count < 1) return;
        foreach (GameObject obj in m_Build)
        {
            DestroyObj(obj);
        }

        m_Build.Clear();
    }


    // destroys objects during edit and play mode
    void DestroyObj(UnityEngine.Object obj)
    {
        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }


    void FailBuild(Vector3 _coords)
    {
        GameObject fail = Instantiate(FAIL, new Vector3(_coords.x, _coords.y, _coords.z), Quaternion.identity, Dungeon.transform);
        m_Build.Add(fail);
    }

    Vector3 ModRotation(Sc_Module _mod)
    {
        return new Vector3(0f, _mod.GetRotation() * 90f, 0);
    }

}


