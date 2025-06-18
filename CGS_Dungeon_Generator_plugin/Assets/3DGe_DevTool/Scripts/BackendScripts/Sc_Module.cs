using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Mod_", menuName = "ScriptableObjects/Module", order = 1)]
public class Sc_Module : ScriptableObject
{
    [Header("Object Details")]
    // Mesh and Rotation and the chance Weight of selection
    [SerializeField] GameObject m_mesh = null;
    [SerializeField] int m_rotation = 0;
    [SerializeField] float m_weight = 1;
    [SerializeField] LayerMask m_type;
    public LayerMask GetLayerType() { return m_type; }

    // Sets values
    public void SetUp(GameObject _mesh, int _rotation, float _weight, LayerMask _type) { m_mesh = _mesh; m_rotation = _rotation; m_weight = _weight; m_type = _type; }

    [Header("Edge Connections")]

    [SerializeField] bool m_sameSides = false;
    [SerializeField] public bool m_removeAfterBuild = false;

    public bool SameSides() { return m_sameSides; }

    public void SetEdges(string _X, string _nX, string _Y, string _nY, string _Z, string _nZ)
    {
        m_neighbours[(int)edge.Z].SetEdgeType(_Z);
        m_neighbours[(int)edge.nZ].SetEdgeType(_nZ);
        m_neighbours[(int)edge.X].SetEdgeType(_X);
        m_neighbours[(int)edge.nX].SetEdgeType(_nX);
        m_neighbours[(int)edge.Y].SetEdgeType(_Y);
        m_neighbours[(int)edge.nY].SetEdgeType(_nY);
    }
    // an array of valid neighbours
     [SerializeField] Neighbour[] m_neighbours = {
        new Neighbour(edge.Z),
        new Neighbour(edge.X),
        new Neighbour(edge.nZ),
        new Neighbour(edge.nX),
        new Neighbour(edge.Y),
        new Neighbour(edge.nY)
    };

    public Neighbour[] GetNeighbours() { return m_neighbours; }
    public Neighbour GetNeighbour(edge _edge)
    {
        for (int i = 0; i < m_neighbours.Length; i++)
        {
            if (m_neighbours[i].GetEdge() == _edge)
            {
                return m_neighbours[i];
            }
            
        }
        return null;
    }

    public GameObject GetMesh() { return m_mesh; }
    public int GetRotation() { return m_rotation; }
    public float GetWeight() { return m_weight; }
}

[System.Serializable]
// Neighbour class that contains the name of the edge and the name of the valid neighbours
public class Neighbour
{
    // the name of the side
    public edge m_edge = 0; // by default every edge will be set to Z
    public string m_type = null;
    public List<Sc_Module> _validOptions = new List<Sc_Module>();

    public Neighbour(edge _edge) {
        m_edge = _edge;
    }

    public edge GetEdge() { return m_edge; }

    public void SetEdgeType(string _type) { m_type = _type; }
    public string GetEdgeType() { return m_type; }

    // adds new neighbour to valid options
    public void AddNeighbour(Sc_Module _newNeighbour)
    {
        _validOptions.Add(_newNeighbour);
    }

    // clears neighbours
    public void ClearNeighbours()
    {
        _validOptions.Clear();
    }

    public List<Sc_Module> GetOptions()
    {
        return _validOptions;
    }
}

public enum edge
{
    Z = 0,
    X = 1,
    nZ = 2,
    nX = 3,
    Y = 4,
    nY = 5
}