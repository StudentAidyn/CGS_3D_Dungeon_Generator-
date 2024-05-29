using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Sc_AstarPathFinding : MonoBehaviour
{
    private int DEFAULT_MOVEMENT_COST = 10;
    private int DIAGONAL_MOVEMENT_COST = 14;

    // Generate Path Exact Or Branching

    // Array Path
    Sc_MapModule[,,] Map;

    // Open and Closed Lists
    List<Sc_MapModule> openList;
    List<Sc_MapModule> closedList;

    // List of Points to traverse to
    List<Sc_MapModule> traversalPoints = new List<Sc_MapModule>();

    Vector3 mapDimensions;

    public List<Sc_MapModule> GeneratePath(Sc_MapModule[,,] _map, Vector3 _dimensions, int _traversalPoints = 2/*, bool _randomPoints = true */)
    {
        if(_traversalPoints < 2) return null;

        // Sets the local variables
        Map = _map;
        mapDimensions = _dimensions;

        // Points - randomly generated points wihtin the map
        List<Sc_MapModule> Points = new List<Sc_MapModule>();

        // Generates traversal points based on total number of points
        for (int i = 0; i < _traversalPoints; i++) {
            // Generates random  start and end points
            Vector3 randomPosition = new Vector3((int)Random.Range(0, mapDimensions.x), 0, (int)Random.Range(0, mapDimensions.z));
            Points.Add(GetVectorModule(randomPosition));
        }

        // Path list - to return
        List<Sc_MapModule> Path = new List<Sc_MapModule>();

        for (int i = 0; i < _traversalPoints - 1; i++) {
            List<Sc_MapModule> PathSegment = AstarPathing(Points[i], Points[i + 1]);
            if (PathSegment != null) {
                foreach (Sc_MapModule module in PathSegment)
                {
                    Path.Add(module);
                }
            }

        }

        return Path;
    }


    //https://www.youtube.com/watch?v=alU04hvz6L4&ab_channel=CodeMonkey 7:15
    List<Sc_MapModule> AstarPathing(Sc_MapModule startNode, Sc_MapModule endNode) { 

        // OpenList to check the area around it
        openList = new List<Sc_MapModule>();
        // Closed List to check the area already checked
        closedList = new List<Sc_MapModule>();

        // Setting each value within the Mapable Space (so only x and z of the lower layer
        for(int x = 0; x < mapDimensions.x; x++) {
            for(int z = 0; z < mapDimensions.z; z++) {
                Sc_MapModule mapModule = GetVectorModule(new Vector3(x, 0, z));
                mapModule.gScore = int.MaxValue;
                mapModule.CalculateFScore();

                mapModule.previousModule = null;
            }
        }

        startNode.gScore = 0;
        startNode.hScore = CalculateDistance(startNode, endNode);
        startNode.CalculateFScore();

        openList.Add(startNode);

        while (openList.Count > 0)
        {
            Sc_MapModule currentMod = GetLowestFScoreMod(openList);
            if(currentMod == endNode) {
                // reached final node
                return CalculatePath(endNode);
            }


            openList.Remove(currentMod);
            closedList.Add(currentMod);

            foreach(Sc_MapModule neighbour in GetNeighbourList(currentMod))
            {
                if (closedList.Contains(neighbour)) continue;

                int tentativeGScore = currentMod.gScore + CalculateDistance(currentMod, neighbour);
                if(tentativeGScore < neighbour.gScore) {
                    neighbour.previousModule = currentMod;
                    neighbour.gScore = tentativeGScore;
                    neighbour.hScore = CalculateDistance(neighbour, endNode);
                    neighbour.CalculateFScore();

                    if(!openList.Contains(neighbour)) {
                        openList.Add(neighbour);
                    }
                }
            }

        }

        Debug.Log("FAILED TO GENERATE PATH");
        return null;
    }


    private List<Sc_MapModule> GetNeighbourList(Sc_MapModule _mod) {
        List<Sc_MapModule> neighbours = new List<Sc_MapModule>();


        if(_mod.mapPos.x - 1 > 0) {
            neighbours.Add(GetVectorModule(_mod.mapPos - new Vector3(1, 0, 0)));
            if (_mod.mapPos.z - 1 > 0)
            {
                neighbours.Add(GetVectorModule(_mod.mapPos - new Vector3(1, 0, 1)));
            }
            if (_mod.mapPos.z + 1 < mapDimensions.z)
            {
                neighbours.Add(GetVectorModule(_mod.mapPos + new Vector3(-1, 0, 1)));
            }
        }
        if (_mod.mapPos.x + 1 < mapDimensions.x)
        {
            neighbours.Add(GetVectorModule(_mod.mapPos + new Vector3(1, 0, 0)));
            if (_mod.mapPos.z - 1 > 0)
            {
                neighbours.Add(GetVectorModule(_mod.mapPos - new Vector3(-1, 0, 1)));
            }
            if (_mod.mapPos.z + 1 < mapDimensions.z)
            {
                neighbours.Add(GetVectorModule(_mod.mapPos + new Vector3(1, 0, 1)));
            }
        }
        if (_mod.mapPos.z - 1 > 0)
        {
            neighbours.Add(GetVectorModule(_mod.mapPos - new Vector3(0, 0, 1)));
        }
        if (_mod.mapPos.z + 1 < mapDimensions.z)
        {
            neighbours.Add(GetVectorModule(_mod.mapPos + new Vector3(0, 0, 1)));
        }

        return neighbours;
    }

    public Sc_MapModule GetVectorModule(Vector3 _coords)
    {
        return Map[(int)_coords.x, (int)_coords.y, (int)_coords.z];
    }

    private List<Sc_MapModule> CalculatePath(Sc_MapModule _endMod) {
        List<Sc_MapModule> path = new List<Sc_MapModule>();
        path.Add(_endMod);
        Sc_MapModule current = _endMod;
        while (current.previousModule != null) { 
            path.Add(current.previousModule);
            current = current.previousModule;
        }
        path.Reverse();
        return path;
    }

    private int CalculateDistance(Sc_MapModule _mod, Sc_MapModule _other)
    {
        int xDistance = (int)Mathf.Abs(_mod.mapPos.x - _other.mapPos.x);
        int zDistance = (int)Mathf.Abs(_mod.mapPos.z - _other.mapPos.z);
        int remaining = Mathf.Abs(xDistance - zDistance);
        return DIAGONAL_MOVEMENT_COST * Mathf.Min(xDistance, zDistance) + DEFAULT_MOVEMENT_COST * remaining;
    }

    private Sc_MapModule GetLowestFScoreMod(List<Sc_MapModule> modList) {
        Sc_MapModule lowestFScoreMod = modList[0];

        for(int i = 1; i < modList.Count; i++) {
            if (modList[i].fScore  < lowestFScoreMod.fScore) {
                lowestFScoreMod = modList[i];
            }
        }

        return lowestFScoreMod;
    }
}


// the Astar Path Finding will use an updated MAP MODULE piece to construct its path setting modules to be a path