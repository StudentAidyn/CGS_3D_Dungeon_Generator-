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
    Sc_MapModule[,,] Map;
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


    // Thread Quadrant sizes
    Vector2 TopLeft;
    Vector2 BottomRight;

    // Threads for Multi Threading 
    Thread TopLeftThread;
    Thread TopRightThread;
    Thread BottomLeftThread;
    Thread BottomRightThread;

    Thread RefactorThread;

    // Map Quadrants
    Sc_MapModule[,,] TopLeftMapQuadrant;
    Sc_MapModule[,,] TopRightMapQuadrant;
    Sc_MapModule[,,] BottomLeftMapQuadrant;
    Sc_MapModule[,,] BottomRightMapQuadrant;


    // Map Gens for multi threading
    Sc_MapGenerator MapGenThread1;
    Sc_MapGenerator MapGenThread2;
    Sc_MapGenerator MapGenThread3;
    Sc_MapGenerator MapGenThread4;



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
                    mod.ResetModule(modules);
                    Map[x, y, z] = mod;
                }
            }
        }


        // Pre Checks before Creation
        AstarPF = GetComponent<Sc_AstarPathFinding>();
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
                        MapGen.Propagate(module.mapPos, new Vector2(0, 0), MapDimensions, MapDimensions);
                    }

                    // sets all NON path modules in the map to not include "PATH" specific modules
                    foreach (Sc_MapModule module in Map)
                    {
                        if (!LastPath.Contains(module))
                        {
                            module.RemoveModuleTypeBasedOnLayer(LayerMask.NameToLayer("PATH"));
                            MapGen.Propagate(module.mapPos, new Vector2(0, 0), MapDimensions, MapDimensions);
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
        return Map[(int)_coords.x, (int)_coords.y, (int)_coords.z];
    }



    private void GenerateThreadMapping(Vector3 _size)
    {
        // The Vectors of the Top Left Quadrant
        //   -> [X][O]
        //      [O][O]

        TopLeft = new Vector2(0, 0);
        BottomRight = new Vector2((int)_size.x / 2, (int)_size.z / 2);


        TopLeftMapQuadrant     = new Sc_MapModule[(int)BottomRight.x, (int)_size.y, (int)BottomRight.y];
        TopRightMapQuadrant    = new Sc_MapModule[(int)BottomRight.x, (int)_size.y, (int)BottomRight.y];
        BottomLeftMapQuadrant  = new Sc_MapModule[(int)BottomRight.x, (int)_size.y, (int)BottomRight.y];
        BottomRightMapQuadrant = new Sc_MapModule[(int)BottomRight.x, (int)_size.y, (int)BottomRight.y];

        FillQuadrantArray(ref TopLeftMapQuadrant, TopLeft, BottomRight, BottomRight);
        FillQuadrantArray(ref TopRightMapQuadrant, new Vector2(BottomRight.x, TopLeft.y), new Vector2(_size.x, BottomRight.y ), BottomRight);
        FillQuadrantArray(ref BottomLeftMapQuadrant, new Vector2(TopLeft.x, BottomRight.y), new Vector2(BottomRight.x, _size.z), BottomRight);
        FillQuadrantArray(ref BottomRightMapQuadrant, new Vector2(BottomRight.x, BottomRight.y), new Vector2(_size.x, _size.z), BottomRight);


        MapGenThread1 = new Sc_MapGenerator(TopLeftMapQuadrant, 0);
        MapGenThread2 = new Sc_MapGenerator(TopRightMapQuadrant, 1);
        MapGenThread3 = new Sc_MapGenerator(BottomLeftMapQuadrant, 2);
        MapGenThread4 = new Sc_MapGenerator(BottomRightMapQuadrant, 3);

        Vector3 size = _size - new Vector3(1, 0, 1);

        // Adjust Values if odd number

        /*
         + new Vector2(1, 0)
         + new Vector2(0, 1)
         + new Vector2(1, 1)
         
         
         */

        TopLeftThread = new Thread(() => MapGenThread1.GenerateMap(TopLeft, BottomRight, size));
        TopRightThread      = new Thread(() => MapGenThread2.GenerateMap(TopLeft , BottomRight, size));
        BottomLeftThread    = new Thread(() => MapGenThread3.GenerateMap(TopLeft , BottomRight, size));
        BottomRightThread   = new Thread(() => MapGenThread4.GenerateMap(TopLeft , BottomRight, size));


        TopLeftThread.Start();
        TopRightThread.Start();
        BottomLeftThread.Start();
        BottomRightThread.Start();

        
    }

    // fills a quadrant of the main map
    void FillQuadrantArray(ref Sc_MapModule[,,] _moduleQuadrant, Vector2 TopLeft, Vector2 BottomRight, Vector2 Max) {
        
        for(int y = 0; y < MapDimensions.y; y++)
        {
            for (int z = (int)TopLeft.y; z < BottomRight.y; z++)
            {
                for (int x = (int)TopLeft.x; x < BottomRight.x; x++)
                {
                    _moduleQuadrant[x % (int)Max.x, y, z % (int)Max.y] = Map[x, y, z];
                }
            }
        }
        
    }

    private IEnumerator GenerateMultiThreadMap(Vector3 _size)
    {
        GenerateThreadMapping(_size);

        while (CheckThreadState()) // Check
        {
            yield return null;
        }

        RebuildArrayMap(ref TopLeftMapQuadrant, TopLeft, BottomRight, BottomRight);
        RebuildArrayMap(ref TopRightMapQuadrant, new Vector2(BottomRight.x, TopLeft.y), new Vector2(_size.x, BottomRight.y), BottomRight);
        RebuildArrayMap(ref BottomLeftMapQuadrant, new Vector2(TopLeft.x, BottomRight.y), new Vector2(BottomRight.x , _size.z), BottomRight);
        RebuildArrayMap(ref BottomRightMapQuadrant, new Vector2(BottomRight.x, BottomRight.y), new Vector2(_size.x, _size.z), BottomRight);

        StartCoroutine(RefactorThreadMap());
    }


    private IEnumerator RefactorThreadMap()
    {
        RefactorThread = new Thread(() => FixMap());

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

    void RebuildArrayMap(ref Sc_MapModule[,,] _moduleQuadrant, Vector2 TopLeft, Vector2 BottomRight, Vector2 Max) {
        
        for(int y = 0; y<MapDimensions.y; y++)
        {
            for (int z = (int) TopLeft.y; z < BottomRight.y; z++)
            {
                for (int x = (int) TopLeft.x; x < BottomRight.x; x++)
                {
                    Map[x, y, z] = _moduleQuadrant[x % (int)Max.x, y, z % (int)Max.y];
                }
            }
        }
        
    }

    /* Fix Map with final thread
     * This Function corrects the map where there are mistakes, feeding over the whole map to check if each module works with each other
     * This works by 
     * #1 Checking if the current module has been collapsed correctly ? passover to next check : activate current correction proceedure (checking valid connections around it and adjusting those connections or resetting the area)
     * #2 the next check => scanning the the area around the module checking for valid connections ? proceed to the next module : remake the current incorrect connection
     * #3 Fail Safe reboot => if it fails to recoup the section it will reset a 3x3x3 area around the effected module, RESET => PROPAGATE => REGENERATE
     */
    void FixMap() {
        for (int z = 0; z < BottomRight.y; z++) {
            for (int y = 0; y < MapDimensions.y; y++) {
                for (int x = 0; x < BottomRight.x; x++) {

                    Sc_MapModule current = GetVectorModule(new Vector3(x, y, z));
                    // #1 success - check connections
                    if (Map[x, y, z].GetModule() != null)
                    {
                        // Check if the current Module is one of its neigbouring modules' neighbours if so then it passes
                        // OR it could check if the current module's options are it's neighbours
                        
                        if(!CheckModule(current, new Vector3(x, y, z), x - 1, edge.X, MapDimensions.x) ||
                        !CheckModule(current, new Vector3(x, y, z), y - 1, edge.Y, MapDimensions.y) ||
                        !CheckModule(current, new Vector3(x, y, z), z - 1, edge.Z, MapDimensions.z))
                        {
                            // #3 if it fails to refactor the current module then it will apply the fail safe refactoration of the area
                            if (!AttemptToRefactorModule(current)) RefactorFailSafe(current);
                        }
                    }
                    // #1 fail - Correct the current module by checking the surrounding modules
                    else
                    {
                        // #3 if it fails to refactor the current module then it will apply the fail safe refactoration of the area
                        if (!AttemptToRefactorModule(current)) RefactorFailSafe(current);
                    }
                }
            }
        }
    }


    // Compares the current module to its neighbours options.
    // Param: Map Module current, Vector3 compared module coordinate, float compared axis value, the edge being checked, the Max Value of the map 
    private bool CheckModule(Sc_MapModule currentMod, Vector3 comparedCoord,float _comparedAxis, edge _edge, float _max)
    {
        if ((_comparedAxis >= 0 && _comparedAxis < _max) && GetVectorModule(comparedCoord).GetModule() == null)
        {
            return GetVectorModule(comparedCoord).GetModule().GetNeighbour(_edge).GetOptions().Contains(currentMod.GetModule());
        }

        return true;
    }

    // Attempts to refactor module
    private bool AttemptToRefactorModule(Sc_MapModule _module)
    {

        // sets a local vector of the current module map position 
        Vector3 currentVec = _module.mapPos;

        // Checks each coherent edge and removes unrelated options from the current module
        // since this is refactoring a singular module AND it is the centre module comparing being compared by its surrounding modules module options
        RefactorModuleOptions(_module, currentVec.x + 1, _module.mapPos + new Vector3(1, 0, 0), edge.nX,     0, MapDimensions.x);
        RefactorModuleOptions(_module, currentVec.x - 1, _module.mapPos - new Vector3(1, 0, 0), edge.X,    0, MapDimensions.x);
        RefactorModuleOptions(_module, currentVec.y + 1, _module.mapPos + new Vector3(0, 1, 0), edge.nY,     0, MapDimensions.y);
        RefactorModuleOptions(_module, currentVec.y - 1, _module.mapPos - new Vector3(0, 1, 0), edge.Y,    0, MapDimensions.y);
        RefactorModuleOptions(_module, currentVec.z + 1, _module.mapPos + new Vector3(0, 0, 1), edge.nZ,     0, MapDimensions.z);
        RefactorModuleOptions(_module, currentVec.z - 1, _module.mapPos - new Vector3(0, 0, 1), edge.Z,    0, MapDimensions.z);


        _module.Collapse(random);
        return _module.GetModule();
    }

    // removes options from the current module based on the compared modules input
    private void RefactorModuleOptions(Sc_MapModule currentMod, float _comparedAxis, Vector3 _comparedCoord, edge _comparingEdge, float _min, float _max)
    {
        // check if it is within the maps limitations and if it has a module selected
        if ((_comparedAxis >= _min && _comparedAxis < _max) && GetVectorModule(_comparedCoord).GetModule())
        {
            List<Sc_Module> toRemove = new List<Sc_Module>();

            // gets list of options from the compared modules module options (based on edge)
            List<Sc_Module> modules = new List<Sc_Module>(MapGen.GetOpenModuleList(GetVectorModule(_comparedCoord), _comparingEdge));

            //compare each option against the current Mods options
            for (int i = 0; i < currentMod.GetOptions().Count; i++)
            {
                if (!modules.Contains(currentMod.GetOptions()[i])) toRemove.Add(currentMod.GetOptions()[i]);
            }

            for (int i = 0; i < toRemove.Count; i++) //(Sc_Module mod in )
            {
                currentMod.RemoveOption(toRemove[i]);
            }
        }

    }

    void RefactorFailSafe(Sc_MapModule currentModule) {
        // does a breakdown of a 3x3x3 area around the current module paramater and refactors the whole 3x3x3 space
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


