using UnityEngine;
using System.Collections.Generic;

// n = total Elements in Map (Map Size)
// x = total Module Options
// O(n^2 + (n * x) + (n * n^6))

[ExecuteInEditMode]
[RequireComponent(typeof(Sc_ModGenerator))]
public class Sc_MapGenerator
{
    // Random Number Generator
    ThreadRandomiser random;

    // the Wave Function Collapse 3D List Container
    Sc_MapModule[,,] Map;

    // Thread ID
    int ThreadID = 0;


    /// this WFC cycles through the whole array every time,
    /// using overlapping chunks will help with performance and accuracy.
    
    // Generate Sets up all the default variables
    public Sc_MapGenerator(ref Sc_MapModule[,,] _map, int _threadID = 0)
    { // see if size is needed Vector3 _size
        Map = _map;
        Debug.Log(Map.Length);

        for (int y = 0; y < _map.GetLength(1); y++)
        {
            for (int z = 0; z < _map.GetLength(2); z++)
            {
                for (int x = 0; x < _map.GetLength(0); x++)
                {
                    Map[x, y, z] = _map[x, y, z];
                }
            }
        }

        ThreadID = _threadID;
        random = ThreadRandomiser.Instance;
    }

    // starts generating the map
    public void GenerateMap(Vector3 _localSize)
    {
       
        // Loops until the all Modules are collapsed - this is where the loop needs to be freed to properly generate it correctly
        while (!Collapsed(_localSize))
        {
            if (!Iterate(_localSize))
            {
                return;
            }
        }
    }


    // Checks if all Modules within a select area are currently collapsed : returns FALSE if they aren't all collapsed and TRUE if they are all collapsed
    private bool Collapsed(Vector3 _localSize) {

        for (int y = 0; y < (int)_localSize.y; y++)
        {
            for (int z = 0; z < (int)_localSize.z; z++)
            {
                for (int x = 0; x < (int)_localSize.x; x++)
                {
                    bool check = Helper.Instance.GetModule(ref Map, new Vector3(x, y, z)).IsCollapsed();
                    if (!check) { return false; }
                }
            }
        }

        return true;
    }



    // iterates through the WFC 
    private bool Iterate(Vector3 _localSize) {
        var coords = GetMinEntropyCoords(_localSize);
        if(coords == null || coords.x == -1) return false;

        // Collapse the current Min Entropy
        Helper.Instance.GetModule(ref Map, coords).Collapse(ThreadID);

        //Instantiate(go, new Vector3(coords.x, 0, coords.y), Quaternion.identity);
        // Propagate this Coordinate within these Coordinates
        // Propagate(This Coord, From This Coord, to this Coord)
        Propagate(coords, _localSize);
        return true;
    }

    // finds and returns the location of *minimum entropy
    // *if more than 1 it will randomize between modules
    Vector3 GetMinEntropyCoords(Vector3 _localSize) {
        double _lowestEntropy = int.MaxValue; // sets lowest entropy to int Max to ensure the correct lowest entropy selection

        //if the entropy is 0 that means it only has 1 option left thus it is certain
        List<Vector3> lowestEntropyModules = new List<Vector3>();

        // Checking for lowest Entropy Map Module within a select Area

        for (int y = 0; y < (int)_localSize.y; y++)
        {
            for (int z = 0; z < (int)_localSize.z; z++)
            {
                for (int x = 0; x < (int)_localSize.x; x++)
                {
                    Sc_MapModule module = Helper.Instance.GetModule(ref Map, new Vector3(x, y, z));
                    if (!module.IsCollapsed())
                    { // filters in only modules that aren't yet collapsed
                        if (module.GetEntropy() < _lowestEntropy)
                        { // finding the newest lowest entropy
                            lowestEntropyModules.Clear();
                            _lowestEntropy = module.GetEntropy();
                        }
                        if (module.GetEntropy() == _lowestEntropy)
                        { // Checking for any modules with the same entropy
                            lowestEntropyModules.Add(new Vector3(x, y, z));
                        }
                    }
                }
            }
        }

        //choosing on random if needed the returned module
        if (lowestEntropyModules.Count > 1)
        {
            // if there is more than one, select one at random
            float RandomVal = random.GetRandomNumber(ThreadID) % lowestEntropyModules.Count;
            return lowestEntropyModules[(int)RandomVal];
        }
        else if (lowestEntropyModules.Count == 0) return new Vector3(-1, -1);
        return lowestEntropyModules[0];
    }

    // Waves through all modules and adjusts all modules based on the current change
    public void Propagate(Vector3 _coords, Vector3 _localSize)
    {
        // New Propagation Model

        // Create open list
        List<Vector3> OpenList = new List<Vector3>();
        OpenList.Add(_coords);

        // While the OpenList is empty Propagate
        while(OpenList.Count > 0) {
            // set a local variable and POP first element off openList
            var currentVec = OpenList[0];
            OpenList.RemoveAt(0);


            // Check around Module  
            Sc_MapModule currentMod = Helper.Instance.GetModule(ref Map, currentVec);

            if (CheckModuleEdge(currentMod, currentVec.x + 1, currentVec + new Vector3(1, 0, 0), edge.X, _localSize.x)) OpenList.Add(currentVec + new Vector3(1, 0, 0));
            if (CheckModuleEdge(currentMod, currentVec.x - 1, currentVec - new Vector3(1, 0, 0), edge.nX, _localSize.x)) OpenList.Add(currentVec - new Vector3(1, 0, 0));
            if (CheckModuleEdge(currentMod, currentVec.y + 1, currentVec + new Vector3(0, 1, 0), edge.Y, _localSize.y)) OpenList.Add(currentVec + new Vector3(0, 1, 0));
            if (CheckModuleEdge(currentMod, currentVec.y - 1, currentVec - new Vector3(0, 1, 0), edge.nY, _localSize.y)) OpenList.Add(currentVec - new Vector3(0, 1, 0));
            if (CheckModuleEdge(currentMod, currentVec.z + 1, currentVec + new Vector3(0, 0, 1), edge.Z, _localSize.z)) OpenList.Add(currentVec + new Vector3(0, 0, 1));
            if (CheckModuleEdge(currentMod, currentVec.z - 1, currentVec - new Vector3(0, 0, 1), edge.nZ, _localSize.z)) OpenList.Add(currentVec - new Vector3(0, 0, 1));
        }

    }


    private bool CheckModuleEdge(Sc_MapModule currentMod, float _comparedAxis, Vector3 _comparedCoord, edge _comparingEdge, float _max)
    {
        if ((_comparedAxis >= 0 && _comparedAxis < _max))
        {
            Sc_MapModule comparedModule = Helper.Instance.GetModule(ref Map, _comparedCoord);
            bool removed = false;
            List<Sc_Module> modules = new List<Sc_Module>(CompareModulesOptions(currentMod, comparedModule, _comparingEdge));

            for(int i = 0; i < modules.Count; i++) 
            {
                removed = true;
                comparedModule.RemoveOption(modules[i]);
            }

            if (removed)
            {
                return true;
            }
        }

        return false;
    }

    List<Sc_Module> CompareModulesOptions(Sc_MapModule _mainModule, Sc_MapModule _comparedModule /*propagated coord*/, edge _edge)
    {
        // List to return that will be removed
        List<Sc_Module> toRemove = new List<Sc_Module>();

        // turns the Compared module's into a list
        List<Sc_Module> comparedModules = new List<Sc_Module>(_comparedModule.GetOptions());

        // creates list of modules options OR modules options neighbours options depending if it is Collapsed refering to if it is the main collapsed tile
        List<Sc_Module> mainModules = new List<Sc_Module>((_mainModule.IsCollapsed() && _mainModule.GetModule() != null) ? GetCollapsedModuleList(_mainModule, _edge) : GetOpenModuleList(_mainModule, _edge));

        // looks through the compared modules list
        for(int i = 0; i < comparedModules.Count; i++) {
            // Checks if main module contains any modules from the comparedModules list, if it does then it will do nothing if it DOES NOT then it will add it to the remove list
            if (!mainModules.Contains(comparedModules[i]))
            {
                toRemove.Add(comparedModules[i]);
            }
        }
        return toRemove;
    }

    // #0 will get the current Options based on the module already selected
    // #1 will get the options of an open object
    // #2 will get the options of those open object options

    // RETURNS LIST OF MODULES FROM THE MAIN MODULE'S LIST OPTIONS' NEIGHBOURS OPTIONS LIST - BASED ON AN EDGE & MAP_MODULE
    public List<Sc_Module> GetOpenModuleList(Sc_MapModule _mod, edge _edge)
    {
        // List to contain the returning modules
        List<Sc_Module> returningModuleList = new List<Sc_Module>();

        List<Sc_Module> options = new List<Sc_Module>(_mod.GetOptions());


        // For each module in the open module's options, get all options based on an options edge
        for (int o = 0; o < options.Count; o++ )
        {
            List<Sc_Module> modules = new List<Sc_Module>(options[o].GetNeighbour(_edge).GetOptions());
            // for each module in the neighbour's edge specific options add it to the main returning list
            for (int m = 0; m < modules.Count; m++)
            {
                returningModuleList.Add(modules[m]);
            }
        }

        // return all the modules that have been found in the module options edge options.
        return returningModuleList;
    }

    // RETURNS LIST OF MODULES FROM THE MAIN MODULE'S OPTIONS LIST - BASED ON AN EDGE & MAP_MODULE
    public List<Sc_Module> GetCollapsedModuleList(Sc_MapModule _mod, edge _edge)
    { 
        // List to contain the returning modules
        List<Sc_Module> returningModuleList = new List<Sc_Module>();

        // gets all the options on a single edge of a singular module
        foreach(Sc_Module module in _mod.GetModule().GetNeighbour(_edge).GetOptions())
        {
            returningModuleList.Add(module);
        }

        // returns list of modules that are within the passed in module
        return returningModuleList;
    }
}



