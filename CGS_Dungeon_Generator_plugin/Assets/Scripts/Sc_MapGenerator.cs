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


[ExecuteInEditMode]
[RequireComponent(typeof(Sc_ModGenerator))]
class Sc_MapGenerator : MonoBehaviour
{
    //Fail indicator
    [SerializeField] GameObject FAIL = null;
    [SerializeField] GameObject Dungeon = null;

    [Header("Width, Height and Length")]
    // size x and z will be horizontal and Y will be vertical, basically resembling the total number of floors.
    Vector3 SIZE;

    // the Wave Function Collapse 3D List Container
    List<Sc_MapModule> m_map = new List<Sc_MapModule>();

    [SerializeField] List<GameObject> m_Build = new List<GameObject>();

    [SerializeField] bool GenerateFloor = true;


    /// this WFC cycles through the whole array every time,
    /// using overlapping chunks will help with performance and accuracy.


    // Generate Sets up all the default variables
    public void Generate(List<Sc_MapModule> _moduleMap, Vector3 _size) {
        // creates a new array (to hold the map) to this size
        m_map = _moduleMap;
        Debug.Log(m_map.Count + " / " + _moduleMap.Count);

        SIZE = _size;

        // Clears objects in scene
        ClearGOList();

        GenerateMap();
    }


    // starts generating the map
    public void GenerateMap()
    {
        if (GenerateFloor) { SetLevelToType(LayerMask.NameToLayer("FLOOR"), 0); }
        // Loops until the all Modules are collapsed - this is where the loop needs to be freed to properly generate it correctly
        while (!Collapsed()) {
            if (!Iterate()) {
                Debug.Log("ITERATE FAILED");
                return;
            }
        }

        Debug.Log("SUCCESS");
    }

    // Checks if all Modules are currently collapsed : returns FALSE if they aren't all collapsed and TRUE if they are all collapsed
    private bool Collapsed() {

        foreach(Sc_MapModule module in m_map) {
            if (!module.isCollapsed()) return false;
        }

        Debug.Log("ALL MODULES ARE COLLAPSED");
        return true;
    }

    // iterates through the WFC 
    private bool Iterate() {
        var coords = GetMinEntropyCoords();
        if(coords == null || coords.x == -1) return false;

        // attempts to build the minimum entropy object
        if (!AttemptBuild(coords)) return false;

        //Instantiate(go, new Vector3(coords.x, 0, coords.y), Quaternion.identity);
        Propagate(coords);
        return true;
    }

    // finds and returns the location of *minimum entropy
    // *if more than 1 it will randomize between modules
    Vector3 GetMinEntropyCoords() {
        double _lowestEntropy = int.MaxValue; // sets lowest entropy to int Max to ensure the correct lowest entropy selection

        //if the entropy is 0 that means it only has 1 option left thus it is certain
        List<Sc_MapModule> lowestEntropyModules = new List<Sc_MapModule>();


        foreach (Sc_MapModule module in m_map) {
            if (!module.isCollapsed()) { // filters in only modules that aren't yet collapsed
                if (module.GetEntropy() < _lowestEntropy) { // finding the newest lowest entropy
                    lowestEntropyModules.Clear();
                    _lowestEntropy = module.GetEntropy();
                }
                if (module.GetEntropy() == _lowestEntropy) { // Checking for any modules with the same entropy
                    lowestEntropyModules.Add(module);
                }
            }
        }

        // choosing on random if needed the returned module
        if (lowestEntropyModules.Count > 1)
        {
            // if there is more than one, select one at random
            return lowestEntropyModules[Random.Range(0, lowestEntropyModules.Count - 1)].mapPos;
        }
        else if(lowestEntropyModules.Count == 0) return new Vector3 (-1, -1);
        return lowestEntropyModules[0].mapPos;
    }

    // Attempts to build the GameObject, if the object fails it sends back false restarting the whole build
    bool AttemptBuild(Vector3 _coords) {
        Sc_MapModule WFCMod = GetVectorModule(_coords);
        GameObject mod = WFCMod.Collapse();

        if(mod == null) {
            FailBuild(_coords); 
            return false;
        }

        GameObject obj = Instantiate(mod, WFCMod.mapPos, Quaternion.Euler(ModRotation(WFCMod.GetModule())), Dungeon.transform);
        m_Build.Add(obj);
        return true;
    }

    public void Propagate(Vector3 _coords)
    {
        // Check around Module  
        Sc_Module mods = GetVectorModule(_coords).GetModule();
    

        // Compares the surrounding area around X first
        Vector3 posX = new Vector3(_coords.x + 1, _coords.y, _coords.z);
        if (posX.x < SIZE.x && !IsCollapsed(posX))
        {
            foreach (Sc_Module mod in CompareOptions(mods, posX, edge.X))
            {
                GetVectorModule(posX).RemoveOption(mod);
            }
            //PropagateSurroundings(posX);
        }

        Vector3 negX = new Vector3(_coords.x - 1, _coords.y, _coords.z);
        if (negX.x >= 0 && !IsCollapsed(negX))
        {
            foreach (Sc_Module mod in CompareOptions(mods, negX, edge.nX))
            {
                GetVectorModule(negX).RemoveOption(mod);
            }
           // PropagateSurroundings(negX);
        }

        //TODO: Add Comparing Y Coords (UP and DOWN)

        // Compares area around Z last
        Vector3 posY = new Vector3(_coords.x, _coords.y + 1, _coords.z);
        if (posY.y < SIZE.y && !IsCollapsed(posY))
        {
            foreach (Sc_Module mod in CompareOptions(mods, posY, edge.Y))
            {
                GetVectorModule(posY).RemoveOption(mod);
            }
           // PropagateSurroundings(posY);
        }

        Vector3 negY = new Vector3(_coords.x, _coords.y - 1, _coords.z);
        if (negY.y >= 0 && !IsCollapsed(negY))
        {
            foreach (Sc_Module mod in CompareOptions(mods, negY, edge.nY))
            {
                GetVectorModule(negY).RemoveOption(mod);
            }
           // PropagateSurroundings(negY);
        }


        // Compares area around Z last
        Vector3 posZ = new Vector3(_coords.x, _coords.y, _coords.z + 1);
        if (posZ.z < SIZE.z && !IsCollapsed(posZ))
        {
            foreach (Sc_Module mod in CompareOptions(mods, posZ, edge.Z))
            {
                GetVectorModule(posZ).RemoveOption(mod);
            }
            //PropagateSurroundings(posZ);
        }

        Vector3 negZ = new Vector3(_coords.x, _coords.y, _coords.z - 1);
        if (negZ.z >= 0 && !IsCollapsed(negZ))
        {
            foreach (Sc_Module mod in CompareOptions(mods, negZ, edge.nZ))
            {
                GetVectorModule(negZ).RemoveOption(mod);
            }
           // PropagateSurroundings(negZ);
        }

    }

    // Propogate the surrounding area near the newly propogated WFCModule, TODO: alter it so it compares all options and passes back a false only if all options fail - CHECK WHAT IS WRONG OR UPDATE PROPOGATE
    void PropagateSurroundings(Vector3 _coords)
    {
        // Check around Module  
        Sc_MapModule mods = GetVectorModule(_coords);

        // Compares the surrounding area around X first
        Vector3 posX = new Vector3(_coords.x + 1, _coords.y, _coords.z);
        if (posX.x < SIZE.x && !IsCollapsed(posX))
        {
            foreach (Sc_Module mod in CompareOptionsAdvanced(mods, posX, edge.X))
            {
                GetVectorModule(posX).RemoveOption(mod);
            }
        }

        Vector3 negX = new Vector3(_coords.x - 1, _coords.y, _coords.z);
        if (negX.x >= 0 && !IsCollapsed(negX))
        {
            // Main Mod , compared Mod, Edge of Comparison
            foreach (Sc_Module mod in CompareOptionsAdvanced(mods, negX, edge.nX))
            {
                GetVectorModule(negX).RemoveOption(mod);
            }

        }

        //TODO: Add Comparing Y Coords (UP and DOWN)

        // Compares area around Z last
        Vector3 posY = new Vector3(_coords.x, _coords.y + 1, _coords.z);
        if (posY.y < SIZE.y && !IsCollapsed(posY))
        {
            foreach (Sc_Module mod in CompareOptionsAdvanced(mods, posY, edge.Y))
            {
                GetVectorModule(posY).RemoveOption(mod);
            }

        }

        Vector3 negY = new Vector3(_coords.x, _coords.y - 1, _coords.z);
        if (negY.y >= 0 && !IsCollapsed(negY))
        {
            foreach (Sc_Module mod in CompareOptionsAdvanced(mods, negY, edge.nY))
            {
                GetVectorModule(negY).RemoveOption(mod);
            }
        }


        // Compares area around Z last
        Vector3 posZ = new Vector3(_coords.x, _coords.y, _coords.z + 1);
        if (posZ.z < SIZE.z && !IsCollapsed(posZ))
        {
            foreach (Sc_Module mod in CompareOptionsAdvanced(mods, posZ, edge.Z))
            {
                GetVectorModule(posZ).RemoveOption(mod);
            }
        }

        Vector3 negZ = new Vector3(_coords.x, _coords.y, _coords.z - 1);
        if (negZ.z >= 0 && !IsCollapsed(negZ))
        {
            foreach (Sc_Module mod in CompareOptionsAdvanced(mods, negZ, edge.nZ))
            {
                GetVectorModule(negZ).RemoveOption(mod);
            }
        }

    }

    int ConvertVec3ToListCoord(Vector3 _coord)
    {
        return (int)(_coord.x + (_coord.y * SIZE.x * SIZE.z) + (_coord.z * SIZE.x));
    }

    Vector3 ModRotation(Sc_Module _mod)
    {
        return new Vector3(0f, _mod.GetRotation() * 90f, 0);
    }

    // Passes back option removal list
    List<Sc_Module> CompareOptions(Sc_Module _mod, Vector3 _coord /*propagated coord*/, edge _edge)
    {
        List<Sc_Module> toRemove = new List<Sc_Module>();
        // gets the new vector coordinate and compares the options of the new coordinate
        foreach (Sc_Module mod in GetVectorModule(_coord).GetOptions())
        {
            // Compared Mod, Main Mod's Neighbours
            if (!Compare(mod, _mod.GetNeighbour(_edge).GetOptions()))
            {
                toRemove.Add(mod);
            }
        }

        return toRemove;
    }

    List<Sc_Module> CompareOptionsAdvanced(Sc_MapModule _mod, Vector3 _coord /*propagated coord*/, edge _edge)
    {
        List<Sc_Module> toRemove = new List<Sc_Module>();
        // gets the new vector coordinate and compares the options of the new coordinate
        foreach (Sc_Module mod in GetVectorModule(_coord).GetOptions())
        {
            // Compared Mod, Main Mod's Neighbours
            if (!Compare(mod,GetAllNeighboursAlongAnEdge(_mod.GetOptions(), _edge)))
            {
                toRemove.Add(mod);
            }
        }

        return toRemove;
    }

    List<Sc_Module> GetAllNeighboursAlongAnEdge(List<Sc_Module> _mod, edge _edge)
    {
        List<Sc_Module> neighbours = new List<Sc_Module>();
        foreach (Sc_Module mod in _mod)
        {
            foreach(Sc_Module neighMod in mod.GetNeighbour(_edge).GetOptions())
            {
                neighbours.Add(neighMod);
            }
        }

        return neighbours;
    }


    // add compare function to check if the the compare function does not include any of the following
    bool Compare(Sc_Module _mod, List<Sc_Module> _compare) {
        foreach (Sc_Module comp in _compare) {
            if(_mod == comp) {
                return true;
            }
        }
        return false;
    }



    // returns WFC Module using the Vector2 Coordinates of itself
    Sc_MapModule GetVectorModule(Vector3 _coords) {
        return m_map[ConvertVec3ToListCoord(_coords)];
    }

    // check if a specific coordinate is collapsed
    bool IsCollapsed(Vector3 _coords)
    {
        Sc_MapModule wfc = m_map[ConvertVec3ToListCoord(_coords)];
        //Debug.Log(wfc.mapPos);
        if (wfc != null)
        {
            if (m_map[ConvertVec3ToListCoord(_coords)].isCollapsed())
            {
                return true;
            }
        }

        return false;
    }



    void SetLevelToType(LayerMask _layer, int _level)
    {
        foreach (Sc_MapModule mod in GetModulesFromLevel(_level))
        {
            List<Sc_Module> toRemove = new List<Sc_Module>();
            foreach(Sc_Module option in mod.GetOptions())
            {
                if(option.GetLayerType() != (option.GetLayerType() | (1 << _layer)))
                {
                    toRemove.Add(option);
                }
            }

            foreach(Sc_Module option in toRemove)
            {
                mod.RemoveOption(option);
            }
        }
    }

    List<Sc_MapModule> GetModulesFromLevel(int _level)
    {
        List<Sc_MapModule> modules = new List<Sc_MapModule>();

        for (int x = 0; x < SIZE.x; x++)
        {
            for (int z = 0; z < SIZE.z; z++)
            {
                modules.Add(GetVectorModule(new Vector3(x, _level, z)));
            }
        }

        return modules;
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
    void DestroyObj(Object obj)
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
}

/*
how to get all 4 sides ? generate four types? 

m_options will include the sides too
*/


