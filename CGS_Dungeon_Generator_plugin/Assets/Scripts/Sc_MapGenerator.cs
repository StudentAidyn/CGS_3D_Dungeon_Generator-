using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System.CodeDom.Compiler;
using Unity.VisualScripting;
using UnityEditor.PackageManager.Requests;
using UnityEngine.Rendering;


[ExecuteInEditMode]
[RequireComponent(typeof(Sc_ModGenerator))]
class Sc_MapGenerator : MonoBehaviour
{
    // size x and z will be horizontal and Y will be vertical, basically resembling the total number of floors.
    [SerializeField] const int SIZE_X = 20;
    [SerializeField] const int SIZE_Y = 30;
    [SerializeField] const int SIZE_Z = 10;
    // the Wave Function Collapse 3D Array Container
    WFCModule[,] m_WFC ;

    [SerializeField] List<GameObject> m_Build = new List<GameObject>();

    int attemptCounter = 0;
    [SerializeField] int MAX_ATTEMPTS = 3;
    bool _isBuilt = false;
    [SerializeField] List<Sc_Module> modules = new List<Sc_Module>();


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
        m_WFC = new WFCModule[SIZE_X, SIZE_Y];
        // creates new Wave Function Collapse Modules with Modules List
        for (int i = 0; i < SIZE_X; i++)
        {
            for (int j = 0; j < SIZE_Y; j++)
            {
                m_WFC[i, j] = new WFCModule(modules);
            }
        }
        _isBuilt = false;
        GenerateMap();
    }


    // incase the build fails it will restart
    void ResetGenerator()
    {
        attemptCounter++;

        ClearGOList();

        for (int i = 0; i < SIZE_X; i++) {
            for (int j = 0; j < SIZE_Y; j++) {
                m_WFC[i, j].ResetOptions(modules);
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
            // Clears objects in scene
            ClearGOList();

            // picks and generates the first collapsed module
            int X = Random.Range(0, SIZE_X);
            int Y = Random.Range(0, SIZE_Y);
            bool function = true;
            if (!AttemptBuild(new Vector2(X, Y)))
            {
                Debug.Log("FIRST_BUILD_FAILIER");
                function = false;
            }
            else
            {
                // propagates the modules around the collapsed module
                Propagate(new Vector2(X, Y),  true);
            }

            // Loops until the all Modules are collapsed - this is where the loop needs to be freed to properly generate it correctly
            while (!Collapsed() && function)
            {
                if (!Iterate())
                {
                    function = false;
                }
                
            }
            if (Collapsed()) _isBuilt = true; // COULD USE BREAK INSTEAD BUT I LIKE THE CONTROL
            if(!function) ResetGenerator();
        }
        Debug.Log(_isBuilt ? "SUCCESS" : "FAILED");
    }

    // Checks if all Modules are currently collapsed 
    private bool Collapsed() {
        for(int i = 0; i < SIZE_X; i++) {
            for(int j = 0; j < SIZE_Y; j++) {
                if (!m_WFC[i, j].isCollapsed()) return false; 
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
    Vector2 GetMinEntropyCoords()
    {
        // cycle through the tiles
        // find the lowest number (lowest entropy)
        // cycle through again and check for the number found (checking for doubles)
        int _lowestEntropy = 1;
        bool _firstPass = true;
        for (int i = 0; i < SIZE_X; i++)
        {
            for (int j = 0; j < SIZE_Y; j++)
            {
                if (!m_WFC[i, j].isCollapsed()) // checks for collapsed modules
                {
                    if (_firstPass)
                    {
                        _lowestEntropy = m_WFC[i, j].GetEntropy();
                        _firstPass = false;
                    }
                    if (m_WFC[i, j].GetEntropy() < _lowestEntropy)
                    {
                        _lowestEntropy = m_WFC[i, j].GetEntropy();
                    }
                }

            }
        }

        if(_lowestEntropy == 0)
        {
            return new Vector2(-1, -1);
        }

        // gets all modules that have the same entropy
        List<Vector2> temp = new List<Vector2>();
        for (int i = 0; i < SIZE_X; i++)
        {
            for (int j = 0; j < SIZE_Y; j++) { 
                if (!m_WFC[i, j].isCollapsed()) // checks for collapsed modules
                {
                    if (m_WFC[i, j].GetEntropy() == _lowestEntropy)
                    {
                        temp.Add(new Vector2(i, j));
                    }
                }
            }
        }

        // choosing on random if needed the returned module
        Vector2 value = new Vector2(0, 0);
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
    bool AttemptBuild(Vector2 _coords) {
        WFCModule WFCMod = m_WFC[(int)_coords.x, (int)_coords.y];
        GameObject mod = WFCMod.Collapse();
        Debug.Log(_coords + " : " + mod + " on attempt: " + attemptCounter);


        if(mod == null) {
            return false;
        }


        GameObject obj = Instantiate(mod, new Vector3(_coords.x, 0f, _coords.y), Quaternion.Euler(ModRotation(WFCMod.GetOption())));
        m_Build.Add(obj);
        return true;
    }

    private void Propagate(Vector2 _coords, bool _double)
    {
        // Check around Module  
        Option _opt = GetVectorModule(_coords).GetOption();


        Vector2 posY = new Vector2(_coords.x, _coords.y + 1);
        if (posY.y < SIZE_Y && !IsCollapsed(posY))
        {
            foreach (Option mod in CompareOptions(_opt, posY, "posZ"))
            {
                GetVectorModule(posY).RemoveOption(mod);
            }
            PropagateSurroundings(posY);
        }

        Vector2 negY = new Vector2(_coords.x, _coords.y - 1);
        if (negY.y > 0 && !IsCollapsed(negY))
        {
            foreach (Option mod in CompareOptions(_opt, negY, "negZ"))
            {
                GetVectorModule(negY).RemoveOption(mod);
            }
            PropagateSurroundings(negY);
        }

        Vector2 posX = new Vector2(_coords.x + 1, _coords.y);
        if (posX.x < SIZE_X && !IsCollapsed(posX))
        {
            foreach (Option mod in CompareOptions(_opt, posX, "posX"))
            {
                GetVectorModule(posX).RemoveOption(mod);
            }
            PropagateSurroundings(posX);
        }

        Vector2 negX = new Vector2(_coords.x - 1, _coords.y);
        if (negX.x > 0 && !IsCollapsed(negX))
        {
            foreach (Option mod in CompareOptions(_opt, negX, "negX"))
            {
                GetVectorModule(negX).RemoveOption(mod);
            }
            PropagateSurroundings(negX);
        }
    }

    void PropagateSurroundings(Vector2 _coords)
    {
        // Check around Module  
        List<Option> options = GetVectorModule(_coords).GetOptions();

        Vector2 posY = new Vector2(_coords.x, _coords.y + 1);
        if (posY.y < SIZE_Y && !IsCollapsed(posY))
        {
            foreach (Option _opt in options)
            {
                foreach (Option mod in CompareOptions(_opt, posY, "posZ"))
                {
                    GetVectorModule(posY).RemoveOption(mod);
                }
            }
        }

        Vector2 negY = new Vector2(_coords.x, _coords.y - 1);
        if (negY.y > 0 && !IsCollapsed(negY))
        {
            foreach (Option _opt in options) 
            { 
                foreach (Option mod in CompareOptions(_opt, negY, "negZ"))
                {
                    GetVectorModule(negY).RemoveOption(mod);
                }
            }
        }

        Vector2 posX = new Vector2(_coords.x + 1, _coords.y);
        if (posX.x < SIZE_X && !IsCollapsed(posX))
        {
            foreach (Option _opt in options)
            {
                foreach (Option mod in CompareOptions(_opt, posX, "posX"))
                {
                    GetVectorModule(posX).RemoveOption(mod);
                }
            }
        }

        Vector2 negX = new Vector2(_coords.x - 1, _coords.y);
        if (negX.x > 0 && !IsCollapsed(negX))
        {
            foreach (Option _opt in options)
            {
                foreach (Option mod in CompareOptions(_opt, negX, "negX"))
                {
                    GetVectorModule(negX).RemoveOption(mod);
                }
            }
        }
    }

    Vector3 ModRotation(Option _opt)
    {
        return new Vector3(0f, _opt.m_rotation * 90f, 0);
    }

    // Passes back option removal list
    List<Option> CompareOptions(Option _opt, Vector2 _coord, string _edge)
    {
        List<Option> toRemove = new List<Option>();
        // gets the new vector coordinate and compares the options of the new coordinate
        foreach (Option opt in GetVectorModule(_coord).GetOptions())
        {
            if (!Compare(opt, _opt.m_mod.GetNeighbour(_edge).GetOptions()))
            {
                toRemove.Add(opt);
            }
        }

        return toRemove;
    }


    // add compare function to check if the the compare function does not include any of the following
    bool Compare(Option _opt, List<Option> _compare) {
        foreach (Option comp in _compare) {
            if(_opt.m_mod == comp.m_mod && _opt.m_rotation == comp.m_rotation) {
                return true;
            }
        }
        return false;
    }



    // returns WFC Module using the Vector2 Coordinates of itself
    WFCModule GetVectorModule(Vector2 coords)
    {
        return m_WFC[(int)coords.x, (int)coords.y];
    }

    // check if a specific coordinate is collapsed
    bool IsCollapsed(Vector2 coords)
    {
        if (m_WFC[(int)coords.x, (int)coords.y].isCollapsed())
        {
            return true;
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
}

/*
how to get all 4 sides ? generate four types? 

m_options will include the sides too
*/
class WFCModule
{
    // list of possible options that the module could become
    List<Option> m_options;
    bool m_collapsed; // to state if the module has already been collapsed
    Option m_option; // the module it has become when it gets collapsed
    
    // Constructor w/ List of module Parameter
    public WFCModule(List<Sc_Module> _options)
    {
        SetOptions(_options);
    }

    public void SetOptions(List<Sc_Module> _options)
    {
        m_options = new List<Option>();
        foreach (Sc_Module module in _options)
        {
            for (int i = 0; i < 4; i++)
            {
                Option valid = new Option(module, i);
                m_options.Add(valid);
            }

        }
    }


    // Getter and Setter for Options
    public void ResetOptions(List<Sc_Module> _options)
    {
        m_options.Clear();
        SetOptions(_options);

        m_collapsed = false;
        m_option.Reset();
    }
    public List<Option> GetOptions()
    {
        return m_options;
    }


    // removes option in parameter
    public void RemoveOption(Option option)
    {
        List<Option> toRemove = new List<Option>();
        foreach(Option opt in m_options)
        {
            if (opt.m_mod == option.m_mod && opt.m_rotation == option.m_rotation) 
            {
                toRemove.Add(opt);
            }
        }


        foreach(Option opt in toRemove)
        {
            m_options.Remove(opt);
        }

        toRemove.Clear();
    }

    // returns module it has collapsed to
    public Option GetOption()
    {
        return m_option;
    }


    // Checks if the current Module has been collapsed
    public bool isCollapsed()
    {
        return m_collapsed;
    }

    // Collapses the current Module into one of the Options left
    public GameObject Collapse()
    {
        Debug.Log(m_options.Count);
        if(m_options.Count > 0)
        {
            // collapses the current tile based on the principle that it has the lowest entropy so it must close, if there is more than one ooption apply randomization
            m_option = m_options[Random.Range(0, m_options.Count)];
            m_collapsed = true;
            return m_option.m_mod.GetMesh();
        }
        return null;

    }

    // returns the current Entropy of the object (returns total options) : TODO: change it so the Entropy is effected by the weight
    public int GetEntropy()
    {
        return m_options.Count;
    }



}




/*
 
 When Generating the map it needs to cycle through all the options

What happens if it fails to find a possible collapse
needs to clear itself entirely then restart not just call restart, it will need to just be set to restart
 


TODO: ADD ROTATION INTO THE BUILDING
 */
