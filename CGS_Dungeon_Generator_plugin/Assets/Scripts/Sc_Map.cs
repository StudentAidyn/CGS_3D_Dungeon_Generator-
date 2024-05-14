using System.Collections;
using System.Collections.Generic;
using UnityEngine;




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
    [SerializeField] List<Sc_MapModule> Map;



    List<Sc_MapModule> LastPath;
    

    // Declarations
    [SerializeField] bool Generate_Shape = false; // Currently not in use
    [SerializeField] bool Generate_Path = false; 
    [SerializeField] bool Generate_Floor = false;


    // Scripts that are combined in this
    Sc_AstarPathFinding AstarPF;

    // Generates map based on initial inputs
    public void GenerateMap()
    {
        // Sets the dimensions of the Map into a Vector 3
        MapDimensions = new Vector3(Width, Height, Length);

        if (Generate_Shape)
        {
            // MapShapeGenerator.GenerateMapShape([THE MAP]);
        }

        if (Generate_Path)
        {
            LastPath = AstarPF.GeneratePath(Map, MapDimensions);

        }



    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
