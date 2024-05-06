using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEngine.UIElements;

[RequireComponent(typeof(Sc_ModGenerator))]
class Sc_MapGenerator : MonoBehaviour
{
    // size x and z will be horizontal and Y will be vertical, basically resembling the total number of floors.
    [SerializeField] int SIZE_X = 1;
    [SerializeField] int SIZE_Y = 1;
    [SerializeField] int SIZE_Z = 1;
    // the Wave Function Collapse 3D Array Container
    WFCModule[,] m_WFC;
    
    /// this WFC cycles through the whole array every time,
    /// using overlapping chunks will help with performance and accuracy.

    public void GenerateMap()
    {
        while (!Collapsed())
        {
           Iterate();
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

    // iterates through the WFC 
    private void Iterate()
    {
        var coords = GetMinEntropyCoords();

        Instantiate(CollapseAt(coords), new Vector3(coords.x, 0, coords.y), Quaternion.identity);
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

    void UpdateOptions(Vector2 coords)
    {
        // Check in a plus around it
        //UP = 
        List<Sc_Module> toRemove = new List<Sc_Module>();
        Vector2 posY = new Vector2(coords.x, coords.y + 1);
        if (coords.y + 1 < SIZE_Y || IsCollapsed(posY))
        {
            foreach (Sc_Module mod in GetVectorModule(posY).GetOptions())
            {
                if(!Compare(mod, GetVectorModule(coords).GetModule().GetNeighbour("posY").GetOptions()))
                {
                    toRemove.Add(mod);
                }
            }
        }
        
    }

    // add compare function to check if the the compare function does not include any of the following

    bool Compare(Sc_Module _mod, List<Option> _compare)
    {
        
        return false;
    }

    Mesh CollapseAt(Vector2 value) // collapses at the position
    {
        Mesh newObject = m_WFC[(int)value.x, (int)value.y].Collapse();

        return newObject;
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

    WFCModule GetVectorModule(Vector2 coords)
    {
        return m_WFC[(int)coords.x, (int)coords.y];
    }
}

class WFCModule : MonoBehaviour
{
    List<Sc_Module> m_options;
    bool m_collapsed;
    Sc_Module m_collapsedMod = null;
    
    public WFCModule(List<Sc_Module> _options)
    {
        m_options = _options;
    }

    public ref List<Sc_Module> GetOptions()
    {
        return ref m_options;
    }

    public bool isCollapsed()
    {
        return m_collapsed;
    }

    public Mesh Collapse()
    {
        // collapses the current tile based on the principle that it has the lowest entropy so it must close, if there is more than one ooption apply randomization
        m_collapsedMod = m_options[Random.Range(0, m_options.Count - 1)];
        m_collapsed = true;
        return m_collapsedMod.GetMesh();
    }

    public int GetEntropy()
    {
        return m_options.Count;
    }

    public Sc_Module GetModule()
    {
        return m_collapsedMod;
    }

}