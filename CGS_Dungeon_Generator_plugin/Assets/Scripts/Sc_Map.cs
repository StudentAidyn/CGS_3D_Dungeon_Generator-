using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[ExecuteInEditMode]
public class Sc_Map : MonoBehaviour
{
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
    [SerializeField] List<Sc_MapModule> Map = new List<Sc_MapModule>();
    List<Sc_Module> modules = new List<Sc_Module>();



    List<Sc_MapModule> LastPath = new List<Sc_MapModule>();
    

    // Declarations
    [SerializeField] bool Generate_Shape = false; // Currently not in use
    [SerializeField] bool Generate_Path = false;
    [SerializeField] bool Generate_LastPath = false;
    [SerializeField] bool Generate_Floor = false;
    


    // Scripts that are combined in this
    Sc_AstarPathFinding AstarPF;
    Sc_MapGenerator MapGen;

    // Generates map based on initial inputs
    public void GenerateMap()
    {
        // Pre Checks before Creation
        AstarPF = GetComponent<Sc_AstarPathFinding>();
        if (AstarPF == null) return;

        MapGen = GetComponent<Sc_MapGenerator>();
        if (MapGen == null) return;

        modules.Clear();
        modules = new List<Sc_Module>(GetComponent<Sc_ModGenerator>().GetModules());
        if(modules == null || modules.Count == 0) return;

        if (Width <= 0 || Height <= 0 || Length <= 0) {
            Debug.LogError("1 OR more MAP SIZES are 0 OR negative");
            return;
        }

        // Sets the dimensions of the Map into a Vector 3
        MapDimensions = new Vector3(Width, Height, Length);

        Map.Clear();

        // creates new Wave Function Collapse Modules with Size and Modules List
        for (int y = 0; y < Height; y++) {
            for (int z = 0; z < Length; z++) {
                for (int x = 0; x < Width; x++) {
                    //Creates new Module with modules
                    Sc_MapModule mod = new Sc_MapModule(new Vector3(x, y, z));
                    mod.ResetModule(modules);
                    Map.Add(mod);
                }
            }
        }
        Debug.Log(Map.Count);


        if (Generate_Shape)
        {
            // MapShapeGenerator.GenerateMapShape([THE MAP]);
        }

        if (Generate_Path)
        {
            if (!Generate_LastPath || LastPath == null) {
                LastPath.Clear();
                LastPath = AstarPF.GeneratePath(Map, MapDimensions);
                Debug.Log((LastPath != null) ? "NEW PATH GENERATED: " + LastPath.Count : 0);
            }

            if(LastPath != null)
            {
                List<Sc_MapModule> Pathing = LastPath;

                foreach (Sc_MapModule module in Pathing)
                {
                    module.SetModuleTypeBasedOnLayer(LayerMask.NameToLayer("PATH"));
                    //MapGen.Propagate(module.mapPos);
                    //Instantiate(AstarPF.PathDetector, module.mapPos + new Vector3(0, 1, 0), Quaternion.identity, transform);
                }

                foreach (Sc_MapModule module in Map)
                {

                    if (!LastPath.Contains(module))
                    {
                        module.RemoveModuleTypeBasedOnLayer(LayerMask.NameToLayer("PATH"));
                        //MapGen.Propagate(module.mapPos);
                    }
                }
            }

        }

        MapGen.Generate(Map, MapDimensions);

    }

}