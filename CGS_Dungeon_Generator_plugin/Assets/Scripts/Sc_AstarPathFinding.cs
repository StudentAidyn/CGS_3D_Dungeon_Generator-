using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Sc_AstarPathFinding : MonoBehaviour
{
    // Generate Path Exact Or Branching

    // Array Path
    List<Sc_MapModule> modules;

    // List of Points to traverse to
    List<Sc_MapModule> traversalPoints;

    public List<Sc_MapModule> GeneratePath(List<Sc_MapModule> _modules, Vector3 _dimensions)
    {
        List<Sc_MapModule> result = new List<Sc_MapModule>();
        modules = _modules;





        return result;
    }

    List<Sc_MapModule> GeneratePathing(Sc_MapModule startNode, Sc_MapModule endNode)
    {
        List<Sc_MapModule> result = new List<Sc_MapModule>();

    https://www.youtube.com/watch?v=alU04hvz6L4&ab_channel=CodeMonkey 7:15

        return result;
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


// the Astar Path Finding will use an updated MAP MODULE piece to construct its path setting modules to be a path