using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MapMultiThreader
{
    Sc_MapModule[,,] Map; // Local Map

    Sc_MapGenerator MapGen;//erator

    Vector3 MapDimensions;

    // Thread Quadrant sizes
    Vector3 LocalSize;
    // Variant size to fix sizing issues with map generation with odd numbers
    Vector3 LocalSizeAdjustments;

    // Threads for Multi Threading 
    Thread TopLeftThread;
    Thread TopRightThread;
    Thread BottomLeftThread;
    Thread BottomRightThread;

    // Thread To Refactor the Current Map
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


    public MapMultiThreader(ref Sc_MapModule[,,] _map, ref Sc_MapGenerator _mapGen, ref Vector3 _mapDimensions)
    {
        Map = _map;
        MapGen = _mapGen;
        MapDimensions = _mapDimensions;
    }

    public void GenerateThreadMapping(Vector3 _mapSize)
    {

        // if one section is a negative then it won't work correctly, to prevent gaps an additional pass will be added to fix it

        LocalSize = new Vector3((int)_mapSize.x / 2, (int)_mapSize.y, (int)_mapSize.z / 2);

        LocalSizeAdjustments = new Vector3((_mapSize.x % 2 == 0) ? (int)LocalSize.x : (int)LocalSize.x + 1, LocalSize.y, (_mapSize.z % 2 == 0) ? (int)LocalSize.z : (int)LocalSize.z + 1);

        TopLeftMapQuadrant = new Sc_MapModule[(int)LocalSizeAdjustments.x, (int)LocalSize.y, (int)LocalSizeAdjustments.z];
        TopRightMapQuadrant = new Sc_MapModule[(int)LocalSize.x, (int)LocalSize.y, (int)LocalSizeAdjustments.z];
        BottomLeftMapQuadrant = new Sc_MapModule[(int)LocalSizeAdjustments.x, (int)LocalSize.y, (int)LocalSize.z];
        BottomRightMapQuadrant = new Sc_MapModule[(int)LocalSize.x, (int)LocalSize.y, (int)LocalSize.z];

        // The Vectors of the Top Left Quadrant
        //   -> [X][O]
        //      [O][O]

        FillQuadrantArray(ref TopLeftMapQuadrant, new Vector3(0, 0, 0), LocalSizeAdjustments, LocalSizeAdjustments);

        // The Vectors of the Top Right Quadrant
        //   -> [O][X]
        //      [O][O]
        FillQuadrantArray(ref TopRightMapQuadrant, new Vector3(LocalSizeAdjustments.x, 0, 0), new Vector3(_mapSize.x, _mapSize.y, LocalSizeAdjustments.z), new Vector3(LocalSize.x, LocalSize.y, LocalSizeAdjustments.z));

        // The Vectors of the Bottom Left Quadrant
        //   -> [O][O]
        //      [X][O]
        FillQuadrantArray(ref BottomLeftMapQuadrant, new Vector3(0, 0, LocalSizeAdjustments.z), new Vector3(LocalSizeAdjustments.x, _mapSize.y, _mapSize.z), new Vector3(LocalSizeAdjustments.x, LocalSize.y, LocalSize.z));

        // The Vectors of the Bottom Right Quadrant
        //   -> [O][O]
        //      [O][X]
        FillQuadrantArray(ref BottomRightMapQuadrant, new Vector3(LocalSizeAdjustments.x, 0, LocalSizeAdjustments.z), _mapSize, LocalSize);


        MapGenThread1 = new Sc_MapGenerator(ref TopLeftMapQuadrant, 0);
        MapGenThread2 = new Sc_MapGenerator(ref TopRightMapQuadrant, 1);
        MapGenThread3 = new Sc_MapGenerator(ref BottomLeftMapQuadrant, 2);
        MapGenThread4 = new Sc_MapGenerator(ref BottomRightMapQuadrant, 3);

        Vector3 size = _mapSize - new Vector3(1, 0, 1);


        TopLeftThread = new Thread(() => MapGenThread1.GenerateMap(LocalSizeAdjustments));
        TopRightThread = new Thread(() => MapGenThread2.GenerateMap(new Vector3(LocalSize.x, LocalSize.y, LocalSizeAdjustments.z)));
        BottomLeftThread = new Thread(() => MapGenThread3.GenerateMap(new Vector3(LocalSizeAdjustments.x, LocalSize.y, LocalSize.z)));
        BottomRightThread = new Thread(() => MapGenThread4.GenerateMap(LocalSize));


        TopLeftThread.Start();
        TopRightThread.Start();
        BottomLeftThread.Start();
        BottomRightThread.Start();


    }

    // fills a quadrant of the main map
    void FillQuadrantArray(ref Sc_MapModule[,,] _moduleQuadrant, Vector3 _topLeftOfQuadrant, Vector3 _bottomRightOfQuadrant, Vector3 _maxQuadrantSize)
    {

        for (int z = (int)_topLeftOfQuadrant.z; z < _bottomRightOfQuadrant.z; z++)
        {
            for (int y = (int)_topLeftOfQuadrant.y; y < _bottomRightOfQuadrant.y; y++)
            {
                for (int x = (int)_topLeftOfQuadrant.x; x < _bottomRightOfQuadrant.x; x++)
                {
                    _moduleQuadrant[x % (int)_maxQuadrantSize.x, y, z % (int)_maxQuadrantSize.z] = Map[x, y, z];
                }
            }
        }

    }

    public IEnumerator GenerateMultiThreadMap(Vector3 _size)
    {
        GenerateThreadMapping(_size);

        while (CheckThreadState()) // Check
        {
            yield return null;
        }

        // Map Quadrant Array || Minimum Corner (Vector3) || Maximum Corner (Vector3)
        RebuildArrayMap(ref TopLeftMapQuadrant, new Vector3(0, 0, 0), LocalSizeAdjustments, LocalSizeAdjustments);
        RebuildArrayMap(ref TopRightMapQuadrant, new Vector3(LocalSizeAdjustments.x, 0, 0), new Vector3(_size.x, _size.y, LocalSizeAdjustments.z), new Vector3(LocalSize.x, LocalSize.y, LocalSizeAdjustments.z));
        RebuildArrayMap(ref BottomLeftMapQuadrant, new Vector3(0, 0, LocalSizeAdjustments.z), new Vector3(LocalSizeAdjustments.x, _size.y, _size.z), new Vector3(LocalSizeAdjustments.x, LocalSize.y, LocalSize.z));
        RebuildArrayMap(ref BottomRightMapQuadrant, new Vector3(LocalSizeAdjustments.x, 0, LocalSizeAdjustments.z), _size, LocalSize);
        Helper.Instance.BuildMap(ref Map);

        //StartCoroutine(RefactorThreadMap());
        RefactorThreadMap();
    }


    private void RefactorThreadMap()
    {
        Debug.Log("REFACTORING MAP");

        FixMap();
        FixMap();

        Helper.Instance.BuildMap(ref Map, true);

        Helper.Instance.END();
        Debug.Log(Helper.Instance.GetTotalTime());
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

    void RebuildArrayMap(ref Sc_MapModule[,,] _moduleQuadrant, Vector3 TopLeft, Vector3 BottomRight, Vector3 QuadrantSize)
    {
        // 101x1x101
        // 51x51 50x51 
        // 51x50 50x50

        for (int z = (int)TopLeft.z; z < BottomRight.z; z++)
        {
            for (int y = (int)TopLeft.y; y < BottomRight.y; y++)
            {
                for (int x = (int)TopLeft.x; x < BottomRight.x; x++)
                {
                    Map[x, y, z] = _moduleQuadrant[x % (int)QuadrantSize.x, y, z % (int)QuadrantSize.z];
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
    void FixMap()
    {
        for (int z = 0; z < MapDimensions.z; z++)
        {
            for (int y = 0; y < MapDimensions.y; y++)
            {
                for (int x = 0; x < MapDimensions.x; x++)
                {

                    Sc_MapModule current = Helper.Instance.GetModule(ref Map, new Vector3(x, y, z));

                    // #1 success - check connections
                    if (current.GetModule() != null)
                    {
                        // Check if the current Module is one of its neigbouring modules' neighbours if so then it passes

                        if (!CheckModule(current, new Vector3(x, y, z) - new Vector3(1, 0, 0), x - 1, edge.X, MapDimensions.x) ||
                            !CheckModule(current, new Vector3(x, y, z) + new Vector3(1, 0, 0), x + 1, edge.nX, MapDimensions.x) ||
                            !CheckModule(current, new Vector3(x, y, z) - new Vector3(0, 1, 0), y - 1, edge.Y, MapDimensions.y) ||
                            !CheckModule(current, new Vector3(x, y, z) + new Vector3(0, 1, 0), y + 1, edge.nY, MapDimensions.y) ||
                            !CheckModule(current, new Vector3(x, y, z) - new Vector3(0, 0, 1), z - 1, edge.Z, MapDimensions.z) ||
                            !CheckModule(current, new Vector3(x, y, z) + new Vector3(0, 0, 1), z + 1, edge.nZ, MapDimensions.z))
                        {
                            // #3 if it fails to refactor the current module then it will apply the fail safe refactoration of the area
                            if (!AttemptToRefactorModule(ref current))
                            {
                                RefactorFailSafe(current);
                                //MapGen.GenerateMap(new Vector2(0, 0), new Vector2(MapDimensions.x, MapDimensions.z), MapDimensions);
                            }
                        }
                    }
                    // #1 fail - Correct the current module by checking the surrounding modules
                    else
                    {
                        // #3 if it fails to refactor the current module then it will apply the fail safe refactoration of the area
                        if (!AttemptToRefactorModule(ref current))
                        {
                            RefactorFailSafe(current);

                        }

                    }


                }
            }
        }



    }


    // Compares the current module to its neighbours options.
    // Param: Map Module current, Vector3 compared module coordinate, float compared axis value, the edge being checked, the Max Value of the map 
    private bool CheckModule(Sc_MapModule currentMod, Vector3 comparedCoord, float _comparedAxis, edge _edge, float _max)
    {
        if (_comparedAxis >= 0 && _comparedAxis < _max)
        {
            if (Helper.Instance.GetModule(ref Map, comparedCoord).GetModule())
            {
                bool result = Helper.Instance.GetModule(ref Map, comparedCoord).GetModule().GetNeighbour(_edge).GetOptions().Contains(currentMod.GetModule());
                //Debug.Log(currentMod.GetModule() + " at " + currentMod.mapPos + " is " + (result ? "" : "NOT") + " Contained in " + GetVectorModule(comparedCoord).GetModule() + "'s edge: " + _edge);
                return result;
            }
        }
        return true;
    }

    // Attempts to refactor module
    private bool AttemptToRefactorModule(ref Sc_MapModule _module)
    {


        // sets a local vector of the current module map position 
        Vector3 currentVec = _module.mapPos;


        //Debug.Log("Attempting to REFACTOR: " + _module.mapPos + " as: " + _module.GetModule());

        _module.ResetModule(Sc_ModGenerator.Instance.GetModules());

        // Checks each coherent edge and removes unrelated options from the current module
        // since this is refactoring a singular module AND it is the centre module comparing being compared by its surrounding modules module options
        // due to the nature of the refactorisation the refactoring will only consider the connections below a module as considering the top could cause further issues
        RefactorModuleOptions(_module, currentVec.x + 1, currentVec + new Vector3(1, 0, 0), edge.nX, 0, MapDimensions.x);
        RefactorModuleOptions(_module, currentVec.x - 1, currentVec - new Vector3(1, 0, 0), edge.X, 0, MapDimensions.x);
        RefactorModuleOptions(_module, currentVec.y - 1, currentVec - new Vector3(0, 1, 0), edge.Y, 0, MapDimensions.y);
        RefactorModuleOptions(_module, currentVec.z + 1, currentVec + new Vector3(0, 0, 1), edge.nZ, 0, MapDimensions.z);
        RefactorModuleOptions(_module, currentVec.z - 1, currentVec - new Vector3(0, 0, 1), edge.Z, 0, MapDimensions.z);

        //Debug.Log(_module.GetOptions().Count);
        _module.Collapse();
        return _module.GetModule();
    }

    // removes options from the current module based on the compared modules input
    private void RefactorModuleOptions(Sc_MapModule currentMod, float _comparedAxis, Vector3 _comparedCoord, edge _comparingEdge, float _min, float _max)
    {

        // check if it is within the maps limitations and if it has a module selected
        if (_comparedAxis >= _min && _comparedAxis < _max)
        {
            Sc_MapModule comparedModule = Helper.Instance.GetModule(ref Map, _comparedCoord);
            if (comparedModule.GetModule() != null)
            {
                //Debug.Log(_comparingEdge);
                List<Sc_Module> toRemove = new List<Sc_Module>();

                // gets list of options from the compared modules module options (based on edge)
                List<Sc_Module> comparisonModules = new List<Sc_Module>(MapGen.GetCollapsedModuleList(comparedModule, _comparingEdge));
                //Debug.Log("Open Module list: " + comparisonModules.Count);
                //compare each option against the current Mods options
                for (int i = 0; i < currentMod.GetOptions().Count; i++)
                {
                    if (!comparisonModules.Contains(currentMod.GetOptions()[i])) toRemove.Add(currentMod.GetOptions()[i]);
                }

                for (int i = 0; i < toRemove.Count; i++) //(Sc_Module mod in )
                {
                    currentMod.RemoveOption(toRemove[i]);
                }
            }

        }

    }

    void RefactorFailSafe(Sc_MapModule currentModule)
    {

        // does a breakdown of the imediate area around the current module paramater and refactors


        //Get 3D plus formation first:
        Vector3 currentVectorPosition = currentModule.mapPos;

        List<Sc_MapModule> resetModules = new List<Sc_MapModule>();

        // the centre vector has to exist and will be collapsed first:
        Helper.Instance.GetModule(ref Map, new Vector3(currentVectorPosition.x, currentVectorPosition.y, currentVectorPosition.z)).ResetModule(Sc_ModGenerator.Instance.GetModules());
        // Checking X sides - X sides will check in a H format
        if (currentVectorPosition.x - 1 >= 0)
        {
            Sc_MapModule module = Helper.Instance.GetModule(ref Map, new Vector3(currentVectorPosition.x - 1, currentVectorPosition.y, currentVectorPosition.z));
            module.ResetModule(Sc_ModGenerator.Instance.GetModules());
            resetModules.Add(module);

        }
        if (currentVectorPosition.x + 1 < MapDimensions.x)
        {
            Sc_MapModule module = Helper.Instance.GetModule(ref Map, new Vector3(currentVectorPosition.x + 1, currentVectorPosition.y, currentVectorPosition.z));
            module.ResetModule(Sc_ModGenerator.Instance.GetModules());
            resetModules.Add(module);
        }


        // Checking Y sides - Y checks in a + pattern
        if (currentVectorPosition.y - 1 >= 0)
        {
            Sc_MapModule module = Helper.Instance.GetModule(ref Map, new Vector3(currentVectorPosition.x, currentVectorPosition.y - 1, currentVectorPosition.z));
            module.ResetModule(Sc_ModGenerator.Instance.GetModules());
            resetModules.Add(module);
        }


        if (currentVectorPosition.y + 1 < MapDimensions.y)
        {
            Sc_MapModule module = Helper.Instance.GetModule(ref Map, new Vector3(currentVectorPosition.x, currentVectorPosition.y + 1, currentVectorPosition.z));
            module.ResetModule(Sc_ModGenerator.Instance.GetModules());
            resetModules.Add(module);
        }

        // Checking Z sides
        if (currentVectorPosition.z - 1 >= 0)
        {
            Sc_MapModule module = Helper.Instance.GetModule(ref Map, new Vector3(currentVectorPosition.x, currentVectorPosition.y, currentVectorPosition.z - 1));
            module.ResetModule(Sc_ModGenerator.Instance.GetModules());
            resetModules.Add(module);
        }

        if (currentVectorPosition.z + 1 < MapDimensions.z)
        {
            Sc_MapModule module = Helper.Instance.GetModule(ref Map, new Vector3(currentVectorPosition.x, currentVectorPosition.y, currentVectorPosition.z + 1));
            module.ResetModule(Sc_ModGenerator.Instance.GetModules());
            resetModules.Add(module);
        }

        for (int i = 0; i < resetModules.Count; i++)
        {
            var module = resetModules[i];
            AttemptToRefactorModule(ref module);
            resetModules[i] = module;
        }

        AttemptToRefactorModule(ref currentModule);

        currentModule.Collapse();


        RebuildMap();

        /// TODO
        /// ADD WAY TO REBUILD MAP
        /// ----------------------------------------------------- DON'T FORGET
    }

    void RebuildMap()
    {
        if (Helper.Instance.GetGenerateFloor()) { Helper.Instance.SetLevelToType(ref Map, LayerMask.NameToLayer("FLOOR"), 0); }
        MapGen.GenerateMap(MapDimensions);
    }


}
