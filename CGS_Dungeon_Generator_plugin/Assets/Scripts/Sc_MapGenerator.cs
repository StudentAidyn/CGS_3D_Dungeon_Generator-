using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEngine.UIElements;

class Sc_MapGenerator : MonoBehaviour
{
    // size x and z will be horizontal and Y will be vertical, basically resembling the total number of floors.
    [SerializeField] const int SIZE_X = 1, SIZE_Y = 1, SIZE_Z = 1;
    // the Wave Function Collapse 3D Array Container
    WFCModule[,] m_WFC = new WFCModule[SIZE_X, SIZE_Y];
    
    /// this WFC cycles through the whole array every time,
    /// using overlapping chunks will help with performance and accuracy.

    private void Start()
    {
        if(File.Exists(Application.dataPath + "/JSON/Js_WFC-Tileset.json"))
        {
            string str = File.ReadAllText(Application.dataPath + "/JSON/Jsn_SimpleTiles.json");
        }
    }

    private void GenerateMap()
    {
        while (!Collapsed())
        {
           // Iterate();
        }
    }

    private bool Collapsed()
    {
        for(int i = 0; i < SIZE_X; i++)
        {
            for(int j = 0; j < SIZE_Y; j++)
            {
                if (!m_WFC[i, j].isCollapsed()) return false; 
            }
        }
        return true;
    }

    private void Iterate()
    {
        var coords = GetMinEntropyCoords();
        CollapseAt(coords);
        Propagate(coords);

    }

    private void Propagate(Vector2 coords)
    {

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
        for (int i = 0; i < SIZE_X; i++) {
            for (int j = 0; j < SIZE_Y; j++) {
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

        // gets all modules that have the same entropy
        List<Vector2> temp = new List<Vector2>();
        for (int i = 0; i < SIZE_X; i++)
        {
            for (int j = 0; j < SIZE_Y; j++)
            {
                if (m_WFC[i, j].GetEntropy() == _lowestEntropy)
                {
                    temp.Add(new Vector2(i, j));
                }
            }
        }

        // choosing on random if needed the returned module
        Vector2 value = new Vector2(0, 0);
        if(temp.Count > 1) {
            // if there is more than one, select one at random
            value = temp[Random.Range(0, temp.Count)];
        }
        else
        {
            value = temp[0];
        }

        return value;

    }

    void CollapseAt(Vector2 value) // collapses at the position
    {
        m_WFC[(int)value.x, (int)value.y].Collapse();
    }
    public void CheckRotations()
    {
        // checks each horizontal side
    }
}

class WFCModule : MonoBehaviour
{
    Mesh _mesh;
    int _currentRotation;
    bool _collapsed;
    Vector2 _coords;
    

    public bool isCollapsed()
    {
        return _collapsed;
    }

    public void Collapse()
    {
        // collapses the current tile based on the principle that it has the lowest entropy so it must close, if there is more than one ooption apply randomization
    }

    public int GetEntropy()
    {
        return 1;
    }

}