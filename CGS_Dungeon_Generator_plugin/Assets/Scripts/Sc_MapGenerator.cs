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
    [SerializeField] int SIZE_X = 10;
    [SerializeField] int SIZE_Y = 10;
    [SerializeField] int SIZE_Z = 10;
    // the Wave Function Collapse 3D Array Container
    WFCModule[,,] m_WFC ;

    [SerializeField] List<GameObject> m_Build = new List<GameObject>();

    int attemptCounter = 0;
    [SerializeField] int MAX_ATTEMPTS = 3;
    bool _isBuilt = false;
    [SerializeField] List<Sc_Module> modules = new List<Sc_Module>();

    [SerializeField] bool GenerateFloor = true;


    /// this WFC cycles through the whole array every time,
    /// using overlapping chunks will help with performance and accuracy.


    // Map Generator, Called to initialize the map generation
    public void Generate()
    {
        attemptCounter = 0;
        modules = GetComponent<Sc_ModGenerator>().GetModules();

        if (modules == null || modules.Count == 0)
        {
            Debug.LogError("Failed to fetch modules from Sc_ModGenerator.");
            return;
        }

        // creates a new array (to hold the map) to this size
        m_WFC = new WFCModule[SIZE_X, SIZE_Y, SIZE_Z];
        // creates new Wave Function Collapse Modules with Modules List
        for (int x = 0; x < SIZE_X; x++)
        {
            for (int y = 0; y < SIZE_Y; y++)
            {
                for (int z = 0; z < SIZE_Z; z++)
                {
                    m_WFC[x, y, z] = new WFCModule(modules);
                }
            }
        }        

        _isBuilt = false;
        GenerateMap();

        //attemptCounter = MAX_ATTEMPTS;
    }


    // incase the build fails it will restart
    void ResetGenerator()
    {
        attemptCounter++;

        ClearGOList();

        for (int x = 0; x < SIZE_X; x++) {
            for (int y = 0; y < SIZE_Y; y++) {
                for (int z = 0; z < SIZE_Z; z++)
                {
                    m_WFC[x, y, z].ResetOptions(modules);
                }
            }
        }

        Debug.Log("RESET");
    }

    // starts generating the map
    // clears all objects in scene and itself then obtains the modules

    public void GenerateMap()
    {
        while (attemptCounter < MAX_ATTEMPTS && _isBuilt == false)
        {
            if (GenerateFloor) { SetLevelToType(LayerMask.NameToLayer("FLOOR"), 0); }
            // Clears objects in scene
            ClearGOList();

            // picks and generates the first collapsed module
            int X = Random.Range(0, SIZE_X);
            int Y = 0;
                //Random.Range(0, SIZE_Y);
            int Z = Random.Range(0, SIZE_Z);
            bool function = true;
            if (!AttemptBuild(new Vector3(X, Y, Z)))
            {
                Debug.Log("FIRST_BUILD_FAILIER");
                function = false;
            }
            else
            {
                // propagates the modules around the collapsed module
                Propagate(new Vector3(X, Y, Z),  true);
            }

            // Loops until the all Modules are collapsed - this is where the loop needs to be freed to properly generate it correctly
            while (!Collapsed() && function)
            {
                if (!Iterate())
                {
                    function = false;
                }
                
            }
            if (Collapsed())
                _isBuilt = true; // COULD USE BREAK INSTEAD BUT I LIKE THE CONTROL
            if (!function)
            {
                if(attemptCounter != MAX_ATTEMPTS - 1)
                {
                    ResetGenerator();
                }
                else
                {
                    attemptCounter = MAX_ATTEMPTS;
                }
                
            }
        }
        Debug.Log(_isBuilt ? "SUCCESS" : "FAILED");
    }

    // Checks if all Modules are currently collapsed 
    private bool Collapsed() {

        for (int x = 0; x < SIZE_X; x++) {
            for (int y = 0; y < SIZE_Y; y++) {
                for (int z = 0; z < SIZE_Z; z++) {
                    if (!m_WFC[x, y, z].isCollapsed()) return false;
                }
            }
        }

        return true;
    }

    // iterates through the WFC 
    private bool Iterate() {
        var coords = GetMinEntropyCoords(); 
        if(coords.x == -1) return false;

        // attempts to build the minimum entropy object
        if (!AttemptBuild(coords)) return false;

        //Instantiate(go, new Vector3(coords.x, 0, coords.y), Quaternion.identity);
        Propagate(coords, true);
        return true;
    }

    // finds and returns the location of *minimum entropy
    // *if more than 1 it will randomize between modules
    Vector3 GetMinEntropyCoords()
    {
        // cycle through the tiles
        // find the lowest number (lowest entropy)
        // cycle through again and check for the number found (checking for doubles)
        double _lowestEntropy = 1;
        bool _firstPass = true;

        for (int x = 0; x < SIZE_X; x++) {
            for (int y = 0; y < SIZE_Y; y++) {
                for (int z = 0; z < SIZE_Z; z++) {
                    if (!m_WFC[x, y, z].isCollapsed()) // checks for collapsed modules
                    {
                        if (_firstPass) {
                            _lowestEntropy = m_WFC[x, y, z].GetEntropy();
                            _firstPass = false;
                        }
                        if (m_WFC[x, y, z].GetEntropy() < _lowestEntropy) {
                            _lowestEntropy = m_WFC[x, y, z].GetEntropy();
                        }
                    }
                }
            }
        }
        //if the entropy is 0 that means it only has 1 option left thus it is certain

        // gets all modules that have the same entropy
        List<Vector3> temp = new List<Vector3>();
        for (int x = 0; x < SIZE_X; x++) {
            for (int y = 0; y < SIZE_Y; y++) { 
                for (int z = 0; z < SIZE_Z; z++) {
                    if (!m_WFC[x, y, z].isCollapsed()) // checks for collapsed modules
                    {
                        if (m_WFC[x, y, z].GetEntropy() == _lowestEntropy)
                        {
                            temp.Add(new Vector3(x, y, z));
                        }
                    }
                }

            }
        }



        // choosing on random if needed the returned module
        Vector3 value = new Vector3(0, 0, 0);
        if (temp.Count > 1)
        {
            // if there is more than one, select one at random
            value = temp[Random.Range(0, temp.Count)];
        }
        else
        {
            value = temp[0];
        }

        Debug.Log(value + " : with lowest Entropy " + _lowestEntropy);

        return value;
    }

    // Attempts to build the GameObject, if the object fails it sends back false restarting the whole build
    bool AttemptBuild(Vector3 _coords) {
        WFCModule WFCMod = GetVectorModule(_coords);
        GameObject mod = WFCMod.Collapse();
        Debug.Log(_coords + " : [" + mod + "] on attempt: " + attemptCounter);


        if(mod == null) {
            FailBuild(_coords); 
            return false;
        }


        GameObject obj = Instantiate(mod, new Vector3(_coords.x, _coords.y, _coords.z), Quaternion.Euler(ModRotation(WFCMod.GetModule())), Dungeon.transform);
        m_Build.Add(obj);
        return true;
    }

    private void Propagate(Vector3 _coords, bool _double)
    {
        // Check around Module  
        Sc_Module mods = GetVectorModule(_coords).GetModule();
    

        // Compares the surrounding area around X first
        Vector3 posX = new Vector3(_coords.x + 1, _coords.y, _coords.z);
        if (posX.x < SIZE_X && !IsCollapsed(posX))
        {
            foreach (Sc_Module mod in CompareOptions(mods, posX, "posX"))
            {
                GetVectorModule(posX).RemoveOption(mod);
            }
            //PropagateSurroundings(posX);
        }

        Vector3 negX = new Vector3(_coords.x - 1, _coords.y, _coords.z);
        if (negX.x >= 0 && !IsCollapsed(negX))
        {
            foreach (Sc_Module mod in CompareOptions(mods, negX, "negX"))
            {
                GetVectorModule(negX).RemoveOption(mod);
            }
            //PropagateSurroundings(negX);
        }

        //TODO: Add Comparing Y Coords (UP and DOWN)

        // Compares area around Z last
        Vector3 posY = new Vector3(_coords.x, _coords.y + 1, _coords.z);
        Debug.Log(posY);
        if (posY.y < SIZE_Y && !IsCollapsed(posY))
        {
            foreach (Sc_Module mod in CompareOptions(mods, posY, "posY"))
            {
                GetVectorModule(posY).RemoveOption(mod);
            }
            //PropagateSurroundings(posY);
        }

        Vector3 negY = new Vector3(_coords.x, _coords.y - 1, _coords.z);
        if (negY.y >= 0 && !IsCollapsed(negY))
        {
            foreach (Sc_Module mod in CompareOptions(mods, negY, "negY"))
            {
                GetVectorModule(negY).RemoveOption(mod);
            }
            //PropagateSurroundings(negY);
        }


        // Compares area around Z last
        Vector3 posZ = new Vector3(_coords.x, _coords.y, _coords.z + 1);
        if (posZ.z < SIZE_Z && !IsCollapsed(posZ))
        {
            foreach (Sc_Module mod in CompareOptions(mods, posZ, "posZ"))
            {
                GetVectorModule(posZ).RemoveOption(mod);
            }
            //PropagateSurroundings(posZ);
        }

        Vector3 negZ = new Vector3(_coords.x, _coords.y, _coords.z - 1);
        if (negZ.z >= 0 && !IsCollapsed(negZ))
        {
            foreach (Sc_Module mod in CompareOptions(mods, negZ, "negZ"))
            {
                GetVectorModule(negZ).RemoveOption(mod);
            }
            //PropagateSurroundings(negZ);
        }

    }

    // Propogate the surrounding area near the newly propogated WFCModule, TODO: alter it so it compares all options and passes back a false only if all options fail
    void PropagateSurroundings(Vector3 _coords)
    {
        // Check around Module  
        WFCModule mods = GetVectorModule(_coords);

        // Compares the surrounding area around X first
        Vector3 posX = new Vector3(_coords.x + 1, _coords.y, _coords.z);
        if (posX.x < SIZE_X && !IsCollapsed(posX))
        {
            foreach (Sc_Module mod in CompareOptionsAdvanced(mods, posX, "posX"))
            {
                GetVectorModule(posX).RemoveOption(mod);
            }
        }

        Vector3 negX = new Vector3(_coords.x - 1, _coords.y, _coords.z);
        if (negX.x >= 0 && !IsCollapsed(negX))
        {
            // Main Mod , compared Mod, Edge of Comparison
            foreach (Sc_Module mod in CompareOptionsAdvanced(mods, negX, "negX"))
            {
                GetVectorModule(negX).RemoveOption(mod);
            }

        }

        //TODO: Add Comparing Y Coords (UP and DOWN)

        // Compares area around Z last
        Vector3 posY = new Vector3(_coords.x, _coords.y + 1, _coords.z);
        if (posY.y < SIZE_Y && !IsCollapsed(posY))
        {
            foreach (Sc_Module mod in CompareOptionsAdvanced(mods, posY, "posY"))
            {
                GetVectorModule(posY).RemoveOption(mod);
            }

        }

        Vector3 negY = new Vector3(_coords.x, _coords.y - 1, _coords.z);
        if (negY.y >= 0 && !IsCollapsed(negY))
        {
            foreach (Sc_Module mod in CompareOptionsAdvanced(mods, negY, "negY"))
            {
                GetVectorModule(negY).RemoveOption(mod);
            }
        }


        // Compares area around Z last
        Vector3 posZ = new Vector3(_coords.x, _coords.y, _coords.z + 1);
        if (posZ.z < SIZE_Z && !IsCollapsed(posZ))
        {
            foreach (Sc_Module mod in CompareOptionsAdvanced(mods, posZ, "posZ"))
            {
                GetVectorModule(posZ).RemoveOption(mod);
            }
        }

        Vector3 negZ = new Vector3(_coords.x, _coords.y, _coords.z - 1);
        if (negZ.z >= 0 && !IsCollapsed(negZ))
        {
            foreach (Sc_Module mod in CompareOptionsAdvanced(mods, negZ, "negZ"))
            {
                GetVectorModule(negZ).RemoveOption(mod);
            }
        }

    }

    Vector3 ModRotation(Sc_Module _mod)
    {
        return new Vector3(0f, _mod.GetRotation() * 90f, 0);
    }

    // Passes back option removal list
    List<Sc_Module> CompareOptions(Sc_Module _mod, Vector3 _coord /*propagated coord*/, string _edge)
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

    List<Sc_Module> CompareOptionsAdvanced(WFCModule _mod, Vector3 _coord /*propagated coord*/, string _edge)
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

    List<Sc_Module> GetAllNeighboursAlongAnEdge(List<Sc_Module> _mod, string _edge)
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
    WFCModule GetVectorModule(Vector3 _coords)
    {
        return m_WFC[(int)_coords.x, (int)_coords.y, (int)_coords.z];
    }

    // check if a specific coordinate is collapsed
    bool IsCollapsed(Vector3 _coords)
    {
        WFCModule wfc = m_WFC[(int)_coords.x, (int)_coords.y, (int)_coords.z];
        if (wfc != null)
        {
            if (m_WFC[(int)_coords.x, (int)_coords.y, (int)_coords.z].isCollapsed())
            {
                return true;
            }
        }

        return false;
    }



    void SetLevelToType(LayerMask _layer, int _level)
    {
        foreach (WFCModule mod in GetModulesFromLevel(_level))
        {
            List<Sc_Module> toRemove = new List<Sc_Module>();
            foreach(Sc_Module option in mod.GetOptions())
            {
                if(option.GetType() != (option.GetType() | (1 << _layer)))
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

    List<WFCModule> GetModulesFromLevel(int _level)
    {
        List<WFCModule> modules = new List<WFCModule>();

        for (int x = 0; x < SIZE_X; x++)
        {
            for (int z = 0; z < SIZE_Z; z++)
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
class WFCModule
{
    // list of possible options that the module could become
    List<Sc_Module> m_options;
    bool m_collapsed; // to state if the module has already been collapsed
    Sc_Module m_module; // the module it has become when it gets collapsed
    
    // Constructor w/ List of module Parameter
    public WFCModule(List<Sc_Module> _options)
    {
        SetOptions(_options);
    }

    public void SetOptions(List<Sc_Module> _options)
    {
        m_options = new List<Sc_Module>(_options);
    }


    // Getter and Setter for Options
    public void ResetOptions(List<Sc_Module> _options)
    {
        m_options.Clear();
        SetOptions(_options);

        m_collapsed = false;
        m_module = null;
    }
    public List<Sc_Module> GetOptions()
    {
        return m_options;
    }


    // removes option in parameter
    public void RemoveOption(Sc_Module _mod)
    {
        List<Sc_Module> toRemove = new List<Sc_Module>();
        foreach(Sc_Module mod in m_options)
        {
            if (mod == _mod) 
            {
                toRemove.Add(mod);
            }
        }

        foreach(Sc_Module mod in toRemove)
        {
            m_options.Remove(mod);
        }

        toRemove.Clear();
    }

    // returns module it has collapsed to
    public Sc_Module GetModule()
    {
        return m_module;
    }


    // Checks if the current Module has been collapsed
    public bool isCollapsed()
    {
        return m_collapsed;
    }

    // Collapses the current Module into one of the options taking in consideration the weights of the objects
    public GameObject Collapse()
    {
        Debug.Log(m_options.Count);


        // Calculate total weight
        float totalWeight = 0;
        foreach (Sc_Module tile in m_options)
        {
            totalWeight += tile.GetWeight();
        }

        // Generate a random value within the range of total weight
        float randomValue = Random.Range(0f, totalWeight);

        // Find the tile corresponding to the random value
        float cumulativeWeight = 0;
        foreach (Sc_Module tile in m_options)
        {
            cumulativeWeight += tile.GetWeight();
            if (randomValue <= cumulativeWeight)
            {
                m_module = tile;
                m_collapsed = true;
                return tile.GetMesh();
            }
        }

        //if fails
        if (m_options.Count > 0)
        {
            // collapses the current tile based on the principle that it has the lowest entropy so it must close, if there is more than one ooption apply randomization
            m_module = m_options[Random.Range(0, m_options.Count)];
            m_collapsed = true;
            return m_module.GetMesh();
        }
        return null;

    }

    // returns the current Entropy of the object (returns total options) : TODO: change it so the Entropy is effected by the weight
    public double GetEntropy()
    {
        float sumWeightLogWeight = 0;
        foreach (var tile in m_options) {
            sumWeightLogWeight += tile.GetWeight() * Mathf.Log(tile.GetWeight());
        }

        double shannon_entropy_for_module = Mathf.Log(GetTotalWeight()) - (sumWeightLogWeight / GetTotalWeight());

        return shannon_entropy_for_module;

    }

    float GetTotalWeight()
    {
        float weight = 0;
        foreach(Sc_Module mod in m_options)
        {
            weight += mod.GetWeight();
        }
        if(weight == 0) { return 1; }
        return weight;
    }

    float GetModuleWeight(Sc_Module _mod)
    {
        float weight = _mod.GetWeight()/GetTotalWeight();

        return weight;
    }

}


/*
 
 When Generating the map it needs to cycle through all the options

What happens if it fails to find a possible collapse
needs to clear itself entirely then restart not just call restart, it will need to just be set to restart
 


TODO: ADD ROTATION INTO THE BUILDING
 */
