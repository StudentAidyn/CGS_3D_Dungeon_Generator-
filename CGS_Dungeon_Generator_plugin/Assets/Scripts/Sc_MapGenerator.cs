using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System.CodeDom.Compiler;
using Unity.VisualScripting;
using UnityEditor.PackageManager.Requests;
using UnityEngine.Rendering;
using static UnityEditor.Progress;
using static UnityEditor.Experimental.GraphView.GraphView;
using System.Threading;
using UnityEditor.Build.Content;
using System.Collections;
using System.Drawing;



[ExecuteInEditMode]
[RequireComponent(typeof(Sc_ModGenerator))]
class Sc_MapGenerator
{
    // Random Number Generator
    ThreadRandomiser random;



    // the Wave Function Collapse 3D List Container
    Sc_MapModule[,,] Map;

    int ThreadType = 0;




    /// this WFC cycles through the whole array every time,
    /// using overlapping chunks will help with performance and accuracy.


    /* The Addition of MultiThreading:
     To add multiThreading there will need to be 5 total Threads used.
    4 of the 5 Threads will be based on the selection of each Module; Collapsing a quadrant each of the dungeon/map. 
    the last thread will be used to scan and rearrange already selected modules to ensure each quadrant is coherent to each other.

    First the map generator function will be adjusted to take 2 vectors, 
    Vector2 #1 will take the top left corner of a quadrant and... 
    Vector2 #2 will take the bottom right of a quadrant

    All other sectioning related functions will need to take the 2 Vector2s to declare unmanageable areas.

    Step one of this adjustment process will include just this construction process and only the first 4 threads will be implemented.
    Secondly a fail safe will need to be added to adjust for maps that do not have 4 separate corners (for example: a map where the X or the Y is 1.)
    Thirdly, the map would need to be able to continue creation even if it fails or at least restart but not indefinitely as this could cause further issues.
    Finally, a reduction of the total times a thread needs to search the List for the map would also be necessary....
    or an adjusted version that would control whether the user can support multithreading
     */

    // Generate Sets up all the default variables
    public Sc_MapGenerator(Sc_MapModule[,,] _map, int _threadType = 0)
    { // see if size is needed Vector3 _size
        Map = new Sc_MapModule[_map.GetLength(0), _map.GetLength(1), _map.GetLength(2)];

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

        ThreadType = _threadType;
        random = ThreadRandomiser.Instance;
    }

    // starts generating the map
    public void GenerateMap(Vector2 TopCorner, Vector2 BottomCorner, Vector3 _size)
    {
        // Loops until the all Modules are collapsed - this is where the loop needs to be freed to properly generate it correctly
        while (!Collapsed(TopCorner, BottomCorner, _size)) {
            if (!Iterate(TopCorner, BottomCorner, _size)) {
                Debug.Log("ITERATE FAILED");
                return;
            }
        }

        Debug.Log("SUCCESS");
    }

    // Checks if all Modules within a select area are currently collapsed : returns FALSE if they aren't all collapsed and TRUE if they are all collapsed
    private bool Collapsed(Vector2 TopCorner, Vector2 BottomCorner, Vector3 _size) {

        for (int y = 0; y < (int)_size.y; y++)
        {
            for (int z = 0; z < (int)BottomCorner.y; z++)
            {
                for (int x = 0; x < (int)BottomCorner.x; x++)
                {
                    if (!GetVectorModule(new Vector3(x, y, z)).isCollapsed()) { return false; }
                }
            }
        }

        return true;
    }



    // iterates through the WFC 
    private bool Iterate(Vector2 TopCorner, Vector2 BottomCorner, Vector3 _size) {
        var coords = GetMinEntropyCoords(TopCorner, BottomCorner, _size);
        if(coords == null || coords.x == -1) return false;

        // Collapse the current Min Entropy
        GetVectorModule(coords).Collapse(random, ThreadType);

        //Instantiate(go, new Vector3(coords.x, 0, coords.y), Quaternion.identity);
        // Propagate this Coordinate within these Coordinates
        // Propagate(This Coord, From This Coord, to this Coord)
        Propagate(coords, TopCorner, BottomCorner, _size);
        return true;
    }

    // finds and returns the location of *minimum entropy
    // *if more than 1 it will randomize between modules
    Vector3 GetMinEntropyCoords( Vector2 TopCorner, Vector2 BottomCorner, Vector3 _size) {
        double _lowestEntropy = int.MaxValue; // sets lowest entropy to int Max to ensure the correct lowest entropy selection

        //if the entropy is 0 that means it only has 1 option left thus it is certain
        List<Vector3> lowestEntropyModules = new List<Vector3>();

        // Checking for lowest Entropy Map Module within a select Area

        for (int y = 0; y < (int)_size.y; y++)
        {
            for (int z = 0; z < (int)BottomCorner.y; z++)
            {
                for (int x = 0; x < (int)BottomCorner.x; x++)
                {
                    Sc_MapModule module = GetVectorModule(new Vector3(x, y, z));
                    if (!module.isCollapsed())
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
            float RandomVal = random.GetRandomNumber(ThreadType) % lowestEntropyModules.Count;
            return lowestEntropyModules[(int)RandomVal];
        }
        else if (lowestEntropyModules.Count == 0) return new Vector3(-1, -1);
        return lowestEntropyModules[0];
    }

    // Attempts to build the GameObject, if the object fails it sends back false restarting the whole build


    public void Propagate(Vector3 _coords, Vector2 TopCorner, Vector2 BottomCorner, Vector3 _size)
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
            Sc_MapModule currentMod = GetVectorModule(currentVec);

            if (CheckModuleEdge(currentMod, currentVec.x + 1, currentVec + new Vector3(1, 0, 0), edge.X, TopCorner.x, BottomCorner.x)) OpenList.Add(currentVec + new Vector3(1, 0, 0));
            if (CheckModuleEdge(currentMod, currentVec.x - 1, currentVec - new Vector3(1, 0, 0), edge.nX, TopCorner.x, BottomCorner.x)) OpenList.Add(currentVec - new Vector3(1, 0, 0));
            if (CheckModuleEdge(currentMod, currentVec.y + 1, currentVec + new Vector3(0, 1, 0), edge.Y, 0, _size.y)) OpenList.Add(currentVec + new Vector3(0, 1, 0));
            if (CheckModuleEdge(currentMod, currentVec.y - 1, currentVec - new Vector3(0, 1, 0), edge.nY, 0, _size.y)) OpenList.Add(currentVec - new Vector3(0, 1, 0));
            if (CheckModuleEdge(currentMod, currentVec.z + 1, currentVec + new Vector3(0, 0, 1), edge.Z, TopCorner.y, BottomCorner.y)) OpenList.Add(currentVec + new Vector3(0, 0, 1));
            if (CheckModuleEdge(currentMod, currentVec.z - 1, currentVec - new Vector3(0, 0, 1), edge.nZ, TopCorner.y, BottomCorner.y)) OpenList.Add(currentVec - new Vector3(0, 0, 1));
        }

    }


    private bool CheckModuleEdge(Sc_MapModule currentMod, float _comparedAxis, Vector3 _comparedCoord, edge _comparingEdge, float _min, float _max)
    {
        if ((_comparedAxis >= _min && _comparedAxis < _max) && !IsCollapsed(_comparedCoord))
        {
            bool removed = false;
            List<Sc_Module> modules = new List<Sc_Module>(CompareModulesOptions(currentMod, GetVectorModule(_comparedCoord), _comparingEdge));

            for(int i = 0; i < modules.Count; i++) //(Sc_Module mod in )
            {
                removed = true;
                GetVectorModule(_comparedCoord).RemoveOption(modules[i]);
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
        List<Sc_Module> mainModules = new List<Sc_Module>((_mainModule.isCollapsed() && _mainModule.GetModule() != null) ? GetCollapsedModuleList(_mainModule, _edge) : GetOpenModuleList(_mainModule, _edge));

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
    List<Sc_Module> GetCollapsedModuleList(Sc_MapModule _mod, edge _edge)
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


    // returns WFC Module using the Vector2 Coordinates of itself
    public Sc_MapModule GetVectorModule(Vector3 _coords) {
        return Map[(int)_coords.x, (int)_coords.y, (int)_coords.z];
    }

    // check if a specific coordinate is collapsed
    bool IsCollapsed(Vector3 _coords)
    {
        Sc_MapModule wfc = GetVectorModule(_coords);
        //Debug.Log(wfc.mapPos);
        if (wfc != null)
        {
            if (wfc.isCollapsed())
            {
                return true;
            }
        }

        return false;
    }
}

/*
how to get all 4 sides ? generate four types? 

m_options will include the sides too
*/


