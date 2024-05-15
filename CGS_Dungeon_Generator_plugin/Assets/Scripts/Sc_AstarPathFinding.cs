using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Sc_AstarPathFinding : MonoBehaviour
{
    private int DEFAULT_MOVEMENT_COST = 10;

    // Generate Path Exact Or Branching

    // Array Path
    List<Sc_MapModule> Map;

    // Open and Closed Lists
    List<Sc_MapModule> openList;
    List<Sc_MapModule> closedList;

    // List of Points to traverse to
    List<Sc_MapModule> traversalPoints;

    Vector3 mapDimensions;

    public List<Sc_MapModule> GeneratePath(List<Sc_MapModule> _modules, Vector3 _dimensions)
    {
        Map = _modules;
        mapDimensions = _dimensions;
        Debug.Log(mapDimensions);

        Vector3 startPos = new Vector3((int)Random.Range(0, mapDimensions.x - 1), 0, (int)Random.Range(0, mapDimensions.z - 1));
        Sc_MapModule start = Map[ConvertVec3ToListCoord(startPos)];
        Vector3 endPos = new Vector3((int)Random.Range(0, mapDimensions.x - 1), 0, (int)Random.Range(0, mapDimensions.z - 1));
        Debug.Log(endPos);
        Sc_MapModule end = Map[ConvertVec3ToListCoord(endPos)];
        //Sc_MapModule end = Map[ConvertVec3ToListCoord(new Vector3(mapDimensions.x - 1, 0, mapDimensions.z - 1))];

        return AstarPathing(start, end);
    }


    //https://www.youtube.com/watch?v=alU04hvz6L4&ab_channel=CodeMonkey 7:15
    List<Sc_MapModule> AstarPathing(Sc_MapModule startNode, Sc_MapModule endNode) { 

        openList = new List<Sc_MapModule>();
        closedList = new List<Sc_MapModule>();

        for(int x = 0; x < mapDimensions.x; x++) {
            for(int z = 0; z < mapDimensions.z; z++) {
                Sc_MapModule mapModule = Map[(int)(x + (z * mapDimensions.x))];
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
            neighbours.Add(Map[ConvertVec3ToListCoord(_mod.mapPos - new Vector3(1, 0, 0))]);
        }
        if (_mod.mapPos.x + 1 < mapDimensions.x)
        {
            neighbours.Add(Map[ConvertVec3ToListCoord(_mod.mapPos + new Vector3(1, 0, 0))]);
        }
        if (_mod.mapPos.z - 1 > 0)
        {
            neighbours.Add(Map[ConvertVec3ToListCoord(_mod.mapPos - new Vector3(0, 0, 1))]);
        }
        if (_mod.mapPos.z + 1 < mapDimensions.z)
        {
            neighbours.Add(Map[ConvertVec3ToListCoord(_mod.mapPos + new Vector3(0, 0, 1))]);
        }

        return neighbours;
    }

    int ConvertVec3ToListCoord(Vector3 _coord) {
        return (int)(_coord.x + (_coord.y * mapDimensions.x * mapDimensions.z) + (_coord.z * mapDimensions.x));
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
        return DEFAULT_MOVEMENT_COST * remaining;
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