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


[ExecuteInEditMode]
[RequireComponent(typeof(Sc_ModGenerator))]
class Sc_MapGenerator : MonoBehaviour
{
    // Random Number Generator
    ThreadRandomiser random;

    //Fail indicator
    [SerializeField] GameObject FAIL = null;
    [SerializeField] GameObject Dungeon = null;

    [Header("Width, Height and Length")]
    // size x and z will be horizontal and Y will be vertical, basically resembling the total number of floors.
    Vector3 SIZE;

    // the Wave Function Collapse 3D List Container
    List<Sc_MapModule> m_map = new List<Sc_MapModule>();

    [SerializeField] public List<GameObject> m_Build = new List<GameObject>();


    Thread TopLeftThread;
    Thread TopRightThread;
    Thread BottomLeftThread;
    Thread BottomRightThread;


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
    public void Generate(List<Sc_MapModule> _moduleMap, Vector3 _size) {
        random = ThreadRandomiser.Instance;
        random.GenerateRandomNumbers(_moduleMap.Count);


        // creates a new array (to hold the map) to this size
        m_map = _moduleMap;
        Debug.Log(m_map.Count + " / " + _moduleMap.Count);

        SIZE = _size;

        // Clears objects in scene
        ClearGOList();

        if (SIZE.x > 15 && SIZE.z > 15)
        {
            // Split it into 4 threads:
            // Split the Map into 4 quadrants

            StartCoroutine(GenerateMultiThreadMap(_size));
        }
        else
        {


            GenerateMap(new Vector2(0, 0), new Vector2(SIZE.x, SIZE.z));
        }

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

    void GenerateThreadMapping(Vector3 _size)
    {
        // The Vectors of the Top Left Quadrant
        //   -> [X][O]
        //      [O][O]

        Vector2 TopLeft = new Vector2(0, 0);
        Vector2 BottomRight = new Vector2((int)_size.x / 2, (int)_size.z / 2);

        TopLeftThread = new Thread(() => GenerateMap(TopLeft, BottomRight));
        TopRightThread = new Thread(() => GenerateMap(new Vector2(BottomRight.x, TopLeft.y), new Vector2(_size.x, BottomRight.y)));
        BottomLeftThread = new Thread(() => GenerateMap(new Vector2(TopLeft.x, BottomRight.y), new Vector2(BottomRight.x, _size.z)));
        BottomRightThread = new Thread(() => GenerateMap(new Vector2(BottomRight.x, BottomRight.y), new Vector2(_size.x, _size.z)));
        //GenerateMap();

        TopLeftThread.Start();
        TopRightThread.Start();
        BottomLeftThread.Start();
        BottomRightThread.Start();   

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

    // starts generating the map
    private void GenerateMap(Vector2 TopCorner, Vector2 BottomCorner)
    {

        // Loops until the all Modules are collapsed - this is where the loop needs to be freed to properly generate it correctly
        while (!Collapsed(TopCorner, BottomCorner)) {
            if (!Iterate(TopCorner, BottomCorner)) {
                Debug.Log("ITERATE FAILED");
                return;
            }
        }

        Debug.Log("SUCCESS");
    }

    private void BuildMap()
    {
        Debug.Log("BuildMap");
        foreach (Sc_MapModule module in m_map) {
            AttemptBuild(module);
        }
    }

    // Checks if all Modules within a select area are currently collapsed : returns FALSE if they aren't all collapsed and TRUE if they are all collapsed
    private bool Collapsed(Vector2 TopCorner, Vector2 BottomCorner) {


        for (int y = 0; y < (int)SIZE.y; y++)
        {
            for (int z = (int)TopCorner.y; z < (int)BottomCorner.y; z++)
            {
                for (int x = (int)TopCorner.x; x < (int)BottomCorner.x; x++)
                {
                    if (!GetVectorModule(new Vector3(x, y, z)).isCollapsed()) return false;
                }
            }
        }

        return true;
    }



    // iterates through the WFC 
    private bool Iterate(Vector2 TopCorner, Vector2 BottomCorner) {
        var coords = GetMinEntropyCoords(TopCorner, BottomCorner);
        if(coords == null || coords.x == -1) return false;

        // Collapse the current Min Entropy
        GetVectorModule(coords).Collapse();

        //Instantiate(go, new Vector3(coords.x, 0, coords.y), Quaternion.identity);
        // Propagate this Coordinate within these Coordinates
        // Propagate(This Coord, From This Coord, to this Coord)
        Propagate(coords, TopCorner, BottomCorner);
        return true;
    }

    // finds and returns the location of *minimum entropy
    // *if more than 1 it will randomize between modules
    Vector3 GetMinEntropyCoords(Vector2 TopCorner, Vector2 BottomCorner) {
        double _lowestEntropy = int.MaxValue; // sets lowest entropy to int Max to ensure the correct lowest entropy selection

        //if the entropy is 0 that means it only has 1 option left thus it is certain
        List<Sc_MapModule> lowestEntropyModules = new List<Sc_MapModule>();

        // Checking for lowest Entropy Map Module within a select Area

        for (int y = 0; y < (int)SIZE.y; y++)
        {
            for (int z = (int)TopCorner.y; z < (int)BottomCorner.y; z++)
            {
                for (int x = (int)TopCorner.x; x < (int)BottomCorner.x; x++)
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
                            lowestEntropyModules.Add(module);
                        }
                    }
                }
            }
        }

        //choosing on random if needed the returned module
        if (lowestEntropyModules.Count > 1)
        {
            // if there is more than one, select one at random
            return lowestEntropyModules[random.GetRandomNumber() % (lowestEntropyModules.Count - 1)].mapPos;
        }
        else if (lowestEntropyModules.Count == 0) return new Vector3(-1, -1);
        return lowestEntropyModules[0].mapPos;
    }

    // Attempts to build the GameObject, if the object fails it sends back false restarting the whole build
    bool AttemptBuild(Sc_MapModule _mod) {
        GameObject mod;

        if (!_mod.isCollapsed())
        {
            _mod.Collapse();
        }
        if(_mod.GetModule() == null)
        {
            FailBuild(_mod.mapPos);
            return false;
        }
        mod = _mod.GetModule().GetMesh();

        GameObject obj = Instantiate(mod, _mod.mapPos, Quaternion.Euler(ModRotation(_mod.GetModule())), Dungeon.transform);
        m_Build.Add(obj);
        return true;
    }

    public void Propagate(Vector3 _coords, Vector2 TopCorner, Vector2 BottomCorner)
    {
        // Check around Module  
        Sc_Module mods = GetVectorModule(_coords).GetModule();
    

        // Compares the surrounding area around X first
        Vector3 posX = new Vector3(_coords.x + 1, _coords.y, _coords.z);
        if (posX.x < BottomCorner.x && !IsCollapsed(posX))
        {
            foreach (Sc_Module mod in CompareOptions(mods, posX, edge.X))
            {
                GetVectorModule(posX).RemoveOption(mod);
            }
            //PropagateSurroundings(posX);
        }

        Vector3 negX = new Vector3(_coords.x - 1, _coords.y, _coords.z);
        if (negX.x >= TopCorner.x && !IsCollapsed(negX))
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
        if (posZ.z < BottomCorner.y && !IsCollapsed(posZ))
        {
            foreach (Sc_Module mod in CompareOptions(mods, posZ, edge.Z))
            {
                GetVectorModule(posZ).RemoveOption(mod);
            }
            //PropagateSurroundings(posZ);
        }

        Vector3 negZ = new Vector3(_coords.x, _coords.y, _coords.z - 1);
        if (negZ.z >= TopCorner.y && !IsCollapsed(negZ))
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
    public Sc_MapModule GetVectorModule(Vector3 _coords) {

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


