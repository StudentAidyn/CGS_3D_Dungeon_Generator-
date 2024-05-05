using System.Collections.Generic;
using UnityEngine;

public class Sc_Module : MonoBehaviour
{
    // Mesh and Rotation and the chance Weight of selection
    [SerializeField] Mesh m_mesh = null;
    [SerializeField] int m_rotation = 0;
    [SerializeField] int m_weight = 1;

    // Edges - type of connections based on the edges
    [SerializeField] public string m_posX = string.Empty;
    [SerializeField] public string m_negX = string.Empty;    
    [SerializeField] public string m_posY = string.Empty;    // UP
    [SerializeField] public string m_negY = string.Empty;    // DOWN
    [SerializeField] public string m_posZ = string.Empty;
    [SerializeField] public string m_negZ = string.Empty;

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

}

// Neighbour class that contains the name of the edge and the name of the valid neighbours
public class Neighbour
{
    // the name of the side
    public string m_edge = string.Empty;
    public List<Sc_Module> _validOptions = new List<Sc_Module>();

    public Neighbour(string edge) {
        m_edge = edge;
    }

    public string GetEdge() { return m_edge; }

    public void AddNeighbour(Sc_Module _newNeighbour)
    {
        _validOptions.Add(_newNeighbour);
    }
}

/*
 instead of Neighbour class, it becomes the edge class and contains the connection type, name and neighbours instead
 */
