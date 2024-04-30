using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEngine.UIElements;

class Sc_MapGenerator : MonoBehaviour
{
    // size x and z will be horizontal and Y will be vertical, basically resembling the total number of floors.
    [SerializeField] const int SIZE_X = 1, SIZE_Y = 1, SIZE_Z = 1;
    // the Wave Function Collapse 3D Array Container
    WFCObject[,] m_WFC = new WFCObject[SIZE_X, SIZE_Y];
    


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
                if (!m_WFC[i, j].Collapsed()) return false; 
            }
        }
        return true;
    }

    //private void Iterate()
    //{
    //    var coords = GetMinEntropyCoords();
    //    CollapseAt(coords);


    //}

    //Vector2 GetMinEntropyCoords()
    //{
    //    List<Vector2> temp = new List<Vector2>();
    //    for (int i = 0; i < SIZE_X; i++)
    //    {
    //        for (int j = 0; j < SIZE_Y; j++)
    //        {
    //            temp.Add()
    //        }
    //    }
    //}
    public void CheckRotations()
    {
        // checks each horizontal side
    }
}

class WFCObject : MonoBehaviour
{
    Mesh _mesh;
    int _currentRotation;
    bool _collapsed;
    Vector2 _coords;
    

    public bool Collapsed()
    {
        return _collapsed;
    }

    public void Collaps()
    {
        // collapses the current tile based on the principle that it has the lowest entropy so it must close, if there is more than one ooption apply randomization
    }

}