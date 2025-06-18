using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[System.Serializable]
public class Helper
{
    private static Helper _instance;

    // Private constructor to prevent instantiation
    private Helper() { }

    public static Helper Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new Helper();
            }
            return _instance;
        }
    }



    // -------------------------- VARIABLES -------------------------- 

    ThreadRandomiser random = ThreadRandomiser.Instance;

    GameObject MapGenParent = null;

    public List<GameObject> m_Build = new List<GameObject>();

    bool GenerateFloor;

    DateTime start;
    DateTime end;

    // --------------- Variable Controls ------------------

    public void START() { start = DateTime.Now; }
    public void END() { end = DateTime.Now; }

    public TimeSpan GetTotalTime() { return (end - start); }
    // Setters:

    public void SetMapBuildParent(GameObject newParent)  {
        MapGenParent = newParent;
    }

    public void SetBuildList(ref List<GameObject> build)
    {
        m_Build = build;
    }

    public void SetGenerateFloor(bool state) { GenerateFloor = state; }


    // Gettters:

    public bool GetGenerateFloor() { return GenerateFloor; }


    // -------------------- End --------------------


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




    // --------------- MAP FUNCTIONS --------------- 

    public void BuildMap(ref Sc_MapModule[,,] Map)
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
        if (_mod == null) return false;
        if (!_mod.IsCollapsed()) _mod.Collapse();
        if (_mod.GetModule() == null) return false;
 
        mod = _mod.GetModule().GetMesh();
        GameObject obj;
        if (MapGenParent)
        {
             obj = GameObject.Instantiate(mod, _mod.mapPos, Quaternion.Euler(ModRotation(_mod.GetModule())), MapGenParent.transform);
        }
        else
        {
            obj = GameObject.Instantiate(mod, _mod.mapPos, Quaternion.Euler(ModRotation(_mod.GetModule())));
        }
       
        m_Build.Add(obj);
        return true;
    }

    Vector3 ModRotation(Sc_Module _mod)
    {
        return new Vector3(0f, _mod.GetRotation() * 90f, 0);
    }


    public void SetLevelToType(ref Sc_MapModule[,,] Map, LayerMask _layer, int _level)
    {
        foreach (Sc_MapModule mod in GetModulesFromLevel(ref Map, _level))
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


    List<Sc_MapModule> GetModulesFromLevel(ref Sc_MapModule[,,] Map, int _level)
    {
        List<Sc_MapModule> modules = new List<Sc_MapModule>();

        for (int z = 0; z < Map.GetLength(2); z++)
        {
            for (int x = 0; x < Map.GetLength(0); x++)
            {
                modules.Add(Helper.Instance.GetModule(ref Map, new Vector3(x, _level, z)));
            }
        }

        return modules;
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
            GameObject.Destroy(obj);
        else
            GameObject.DestroyImmediate(obj);
    }
}
