using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


/// WARNING: TODO
/// CONVERT HELPER TO SEARCH FOR MONOBEHAVIOUR, WILL BE ATTACHED TO THE MAP GENERATOR
public class Helper : MonoBehaviour 
{
    private static Helper _instance;

    // Private constructor to prevent instantiation
    private Helper() { }

    public static Helper Instance
    {
        get
        {
            // If the instance is null, create a new instance
            if (_instance == null)
            {
                _instance = new Helper();
            }
            return _instance;
        }
    }


    // -------------------------- VARIABLES -------------------------- 

    ThreadRandomiser random = ThreadRandomiser.Instance;

    //Fail indicator
    [Header("Fail Indicator")]
    [SerializeField] GameObject FAIL = null;

    [Header("Final Build")]
    [SerializeField] GameObject Dungeon = null;
    [SerializeField] GameObject REFACTOR = null;
    [SerializeField] public List<GameObject> m_Build = new List<GameObject>();


    // list of all the possible modules
    [SerializeField] List<Sc_Module> m_modules = new List<Sc_Module>();
    public List<Sc_Module> GetModules() { return m_modules; }

    // --------------- MAP MODULES --------------- 

    public ref Sc_MapModule GetModule(ref Sc_MapModule[,,] Map, Vector3 _coords)
    {
        return ref Map[(int)_coords.x, (int)_coords.y, (int)_coords.z];
    }

    //  Returns the percent of collapsed modules
    public float GetPercentOfCollapsed(ref Sc_MapModule[,,] Map, Vector3 _size)
    {
        // Variables
        int totalCollapsed = 0; // Counter
        float percent = 0f; // Pre-defining percent

        for (int z = 0; z < _size.z; z++)
        {
            for (int y = 0; y < _size.y; y++)
            {
                for (int x = 0; x < _size.x; x++)
                {
                    if (GetModule(ref Map, new Vector3(x, y, z)).IsCollapsed()) totalCollapsed++;

                }
            }
        }

        percent = (totalCollapsed / Map.Length) * 100;

        return percent;
    }

    // --------------- END --------------- 




    // --------------- MAP BUILDER --------------- 

    public void BuildMap(ref Sc_MapModule[,,] Map, bool refact = false)
    {
        Debug.Log("BuildMap");
        foreach (Sc_MapModule module in Map)
        {
            AttemptBuild(module, refact);
        }
    }

    public bool AttemptBuild(Sc_MapModule _mod, bool refact = false)
    {
        GameObject mod;

        if (!_mod.IsCollapsed())
        {
            _mod.Collapse(random);
        }
        if (_mod.GetModule() == null)
        {
            FailBuild(_mod.mapPos);
            return false;
        }
        mod = _mod.GetModule().GetMesh();

        GameObject obj = Instantiate(mod, _mod.mapPos, Quaternion.Euler(ModRotation(_mod.GetModule())), refact ? REFACTOR.transform : Dungeon.transform);
        m_Build.Add(obj);
        return true;
    }

    Vector3 ModRotation(Sc_Module _mod)
    {
        return new Vector3(0f, _mod.GetRotation() * 90f, 0);
    }


    void FailBuild(Vector3 _coords)
    {
        GameObject fail = Instantiate(FAIL, new Vector3(_coords.x, _coords.y, _coords.z), Quaternion.identity, Dungeon.transform);
        m_Build.Add(fail);
    }

    // --------------- END --------------- 


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
    public void DestroyObj(UnityEngine.Object obj)
    {
        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }
}
