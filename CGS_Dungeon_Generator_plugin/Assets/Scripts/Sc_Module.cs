using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Mod_", menuName = "ScriptableObjects/Module", order = 1)]
public class Sc_Module : ScriptableObject
{
    [Header("Object Details")]
    // Mesh and Rotation and the chance Weight of selection
    [SerializeField] Mesh m_mesh = null;
    [SerializeField] int m_rotation = 0;
    [SerializeField] int m_weight = 1;

    // Sets values
    public void SetUp(Mesh _mesh, int _rotation, int _weight) { m_mesh = _mesh; m_rotation = _rotation; m_weight = _weight; }

    [Header("Edge Connections")]
    // Edges - type of connections based on the edges
    [SerializeField] public string m_posX = string.Empty;
    [SerializeField] public string m_negX = string.Empty;    
    [SerializeField] public string m_posY = string.Empty;    // UP
    [SerializeField] public string m_negY = string.Empty;    // DOWN
    [SerializeField] public string m_posZ = string.Empty;
    [SerializeField] public string m_negZ = string.Empty;

    public void SetEdges(string _posX, string _negX, string _posY, string _negY, string _posZ, string _negZ)
    {
        m_posX = _posX;
        m_negX = _negX;
        m_posY = _posY;
        m_negY = _negY;
        m_posZ = _posZ;
        m_negZ = _negZ;
    }

    // an array of valid neighbours
     [SerializeField] Neighbour[] m_neighbours = {
        new Neighbour("posX"),
        new Neighbour("negX"),
        new Neighbour("posY"),
        new Neighbour("negY"),
        new Neighbour("posZ"),
        new Neighbour("negZ")
    };

    public Neighbour[] GetNeighbours() { return m_neighbours; }
    public Neighbour GetNeighbour(string _type)
    {
        for (int i = 0; i < m_neighbours.Length; i++)
        {
            if (m_neighbours[i].GetEdge() == _type)
            {
                return m_neighbours[i];
            }
            
        }
        return null;
    }

    public Mesh GetMesh() { return m_mesh; }
    public int GetRotation() { return m_rotation; }
    public int GetWeight() { return m_weight; }
}

[System.Serializable]
// Neighbour class that contains the name of the edge and the name of the valid neighbours
public class Neighbour
{
    // the name of the side
    public string m_edge = string.Empty;
    public List<Option> _validOptions = new List<Option>();

    public Neighbour(string edge) {
        m_edge = edge;
    }

    public string GetEdge() { return m_edge; }

    // adds new neighbour to valid options
    public void AddNeighbour(Sc_Module _newNeighbour, int _rotation)
    {
        _validOptions.Add(new Option(_newNeighbour, _rotation));
    }

    // clears neighbours
    public void ClearNeighbours()
    {
        _validOptions.Clear();
    }

    public List<Option> GetOptions()
    {
        return _validOptions;
    }
}

/*
 instead of Neighbour class, it becomes the edge class and contains the connection type, name and neighbours instead
 */

public struct Option
{
    public Sc_Module m_mod;
    public int m_rotation;
    public Option(Sc_Module _mod, int _rotation) { m_mod = _mod; m_rotation = _rotation; }
}