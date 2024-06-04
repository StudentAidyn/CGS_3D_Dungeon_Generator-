using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Windows;


/*
 TODO:
Update module to check edges based on rotation
and to not check rotation on objects that have all sides the same
 */


[ExecuteInEditMode]
public class Sc_ModGenerator : MonoBehaviour
{

    private static Sc_ModGenerator _instance;

    // Private constructor to prevent instantiation
    private Sc_ModGenerator() { }

    public static Sc_ModGenerator Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<Sc_ModGenerator>();

                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject("Sc_ModGenerator");
                    _instance = singletonObject.AddComponent<Sc_ModGenerator>();
                    DontDestroyOnLoad(singletonObject);
                }
            }
            return _instance;
        }
    }


    // list of all the possible modules
    [SerializeField] List<Sc_Module> m_modules = new List<Sc_Module>();
    public List<Sc_Module> GetModules() { return m_modules; }

    [Header("Edge Types")]
    [SerializeField] char m_similarEdges = 'S';
    [SerializeField] char m_flippedEdges = 'F';
    [SerializeField] char m_dominantEdges = 'D';
    [SerializeField] char m_recessiveEdges = 'R';

    char[] EdgeTypes;

    [Header("To Prevent Accidental Variants")]
    [SerializeField] bool GENERATE = false;
    [SerializeField] bool DELETE = false;

    List<Sc_Module> m_Variants = new List<Sc_Module>();

    // generate connections based on the connection rules - can generate during editor (out of play state)
    public void CreateConnections()
    {
        EdgeTypes = new char[] { m_similarEdges, m_flippedEdges, m_dominantEdges, m_recessiveEdges, '_'};
        // Clear all previous connections
        ResetModuleNeighbours();
        // cycle through each module and compare module sides to the other modules sides. long process
        // X and Z can connect to each other and themselves
        // Y can only connect to itself

        int index = 0;

        foreach (Sc_Module mod in m_modules){
            // each side of the module and then compare that sides connection to each other, make functions for x and z connections and then one for y connections??

            // get module and compare each module side to the other modules
            CompareModules(mod, index);
            index++;            
        }
        Debug.Log("EdgesCreated");
    }

    // compares the X and Z connection types against themselves and each other
    // could save time if the connection is added to to both modules being tested then never testing prior connections
    void CompareModules(Sc_Module _mod, int _index)
    {
        for (int i = _index; i < m_modules.Count; i++) 
        {
            CompareX(_mod, m_modules[i]);
            CompareY(_mod, m_modules[i]);
            CompareZ(_mod, m_modules[i]);
        }
    }

    /* order:
     posX
     negZ
     negX
     posZ
     */


    // compares the positive X of the main module to the negative X of the other,
    // then compares the negative X of the main mod to the positive of the other
    void CompareX(Sc_Module _mod, Sc_Module _other)
    {
        if (CompareEdges(_mod.GetNeighbour(edge.X).GetEdgeType(), _other.GetNeighbour(edge.nX).GetEdgeType()))
        {
            AddModToNeighbour(_mod, _other, edge.X, 0);
            AddModToNeighbour(_other, _mod, edge.nX, 0);
        }

        // Checking the Other Side
        if (CompareEdges(_mod.GetNeighbour(edge.nX).GetEdgeType(), _other.GetNeighbour(edge.X).GetEdgeType()))
        {
            AddModToNeighbour(_mod, _other, edge.nX, 0);
            AddModToNeighbour(_other, _mod, edge.X, 0);
        }
    }
    void CompareY(Sc_Module _mod, Sc_Module _other)
    {
        if(CompareVerticalEdges(_mod.GetNeighbour(edge.Y).GetEdgeType(), _other.GetNeighbour(edge.nY).GetEdgeType()))
        {
            AddModToNeighbour(_mod, _other, edge.Y, 0);
            AddModToNeighbour(_other, _mod, edge.nY, 0);
        }

        if (CompareVerticalEdges(_mod.GetNeighbour(edge.nY).GetEdgeType(), _other.GetNeighbour(edge.Y).GetEdgeType()))
        {
            AddModToNeighbour(_mod, _other, edge.nY, 0);
            AddModToNeighbour(_other, _mod, edge.Y, 0);
        }
    }
    void CompareZ(Sc_Module _mod, Sc_Module _other)
    {
        if (CompareEdges(_mod.GetNeighbour(edge.Z).GetEdgeType(), _other.GetNeighbour(edge.nZ).GetEdgeType()))
        {
            AddModToNeighbour(_mod, _other, edge.Z, 0);
            AddModToNeighbour(_other, _mod, edge.nZ, 0);
        }

        // Comparing the other sides
        if (CompareEdges(_mod.GetNeighbour(edge.nZ).GetEdgeType(), _other.GetNeighbour(edge.Z).GetEdgeType()))
        {
            AddModToNeighbour(_mod, _other, edge.nZ, 0);
            AddModToNeighbour(_other, _mod, edge.Z, 0);
        }
    }

    // Compares 2 Edges passed through based on the rules given 
    bool CompareEdges(string _edge, string _other)
    {
        // converts string to uppercase then converts them into chars for easier comparisons
        char[] edge = (_edge.ToUpper()).ToCharArray();
        char[] other = (_other.ToUpper()).ToCharArray();
        // Compare if they share the same first edge identifier

        string edgeValue = "";

        for(int i = 0; i < edge.Length; i++)
        {
            if (!EdgeTypes.Contains(edge[i])) {
                edgeValue += edge[i];
            }   
        }

        string otherValue = "";

        for (int i = 0; i < other.Length; i++)
        {
            if (!EdgeTypes.Contains(other[i]))
            {
                otherValue += other[i];
            }
        }


        // Checks if both values are available
        if (edgeValue == otherValue)
        {
            char endEdge = edge[edge.Length - 1];
            char endOther = other[other.Length - 1];
            // Checks if the last values in the list are NOT Flipped Edges chars
            if (endEdge != m_flippedEdges && endOther != m_flippedEdges)
            {
                // Compares the last 2, if they are both S then it succeeds
                if (endEdge == m_similarEdges && endOther == m_similarEdges) {
                    return true;
                }
                if (endEdge == m_dominantEdges && (endOther == m_dominantEdges || endOther == m_recessiveEdges)) { return true; }
                if ((endEdge == m_dominantEdges || endEdge == m_recessiveEdges) && endOther == m_dominantEdges) { return true; }
            }
            // if baseEdge length is larger than otherEdge then it has 2 chars and if any of those chars = f then it can connect
            else if (endEdge == m_flippedEdges && endOther != m_flippedEdges)
            {        
                return true;
            }
            else if (endEdge != m_flippedEdges && endOther == m_flippedEdges) {
                return true;
            }
        }
        // else return false
        return false;
    }

    /* Rules:
    if the string ends with an S it is symetrical, it can be attached to the extact same string
    if the string doesn't end with anything then it can be attached to a string containing the same value as it but with an F on the end
    else they cannot be connected.
     */

    // Vertical connections
    bool CompareVerticalEdges(string _edge, string _other)
    {   
        if(_edge.Length <= 0 || _other.Length <= 0)
        {
            return false;
        }
        // convert this to check for rotation, a value after an '_' will indicate the orientation of the piece
        char[] edge = (_edge.ToUpper()).ToCharArray();
        char[] other = (_other.ToUpper()).ToCharArray();

        string edgeValue = "";

        for (int i = 0; i < edge.Length; i++)
        {
            if (!EdgeTypes.Contains(edge[i]))
            {
                edgeValue += edge[i];
            }
            else { break; }
        }

        string otherValue = "";

        for (int i = 0; i < other.Length; i++)
        {
            if (!EdgeTypes.Contains(other[i]))
            {
                otherValue += other[i];
            }
            else { break; }
        }


        if (edgeValue == otherValue)
        {
            if (edge[edge.Length - 1] == other[other.Length - 1])
            {
                return true;
            }
            else if (edge[edge.Length - 1] == 'X' || other[other.Length - 1] == 'X')
            {
                return true;
            }
        }

        return false;
    }



    /*Rules:
     A T can only connect to a B (with the correct orientation) and with the correct number

     */

    void AddModToNeighbour(Sc_Module _mod, Sc_Module _other, edge _edge, int _rotation)
    {
        for (int i = 0; i < _mod.GetNeighbours().Length; i++)
        {
            if (_mod.GetNeighbours()[i].GetEdge() == _edge)
            {
                _mod.GetNeighbours()[i].AddNeighbour(_other); // TODO: Fix this so it records the current rotation when connecting then the current rotation of the object
            }
        }
    }

    
    
    public void CreateRotatedVariants()
    {
        if (GENERATE)
        {
            // Pre-Delete || Just To Prevent Double Ups
            DELETE = true;
            DeleteAllVariantsInFolder(); 

            foreach (Sc_Module mod in m_modules)
            {
                if (!mod.SameSides())
                {
                    CreateVariant(mod);
                }
            }

            foreach (Sc_Module mod in m_Variants)
            {
                m_modules.Add(mod);
            }

            m_Variants.Clear();

            GENERATE = false;
        }

    }

    // Forces the modules with the same sides to generate an X at the end to help identify them
    void AdjustForVariant(Sc_Module _mod)
    {
        _mod.GetNeighbour(edge.Y).SetEdgeType(_mod.GetNeighbour(edge.Y).GetEdgeType() + "_X");
        _mod.GetNeighbour(edge.nY).SetEdgeType(_mod.GetNeighbour(edge.nY).GetEdgeType() + "_X");
    }

    // create simple check for if a module is the same on all  sides
    // with the top and bottom I realised they will need to be adjusted since the variable will need to know if it can loop,
    // I think by adding a new rule it can check if a top will need to be oriented if not the object can be kept the same

    void CreateVariant(Sc_Module mod)
    {

        for (int i = 1; i < 4; i++)
        {
            Sc_Module newModule = ScriptableObject.CreateInstance<Sc_Module>();

            CloneRotatedValues(mod, newModule, i);//from Mod to NewMod

            string MOD = mod.name; // name of the asset;
            string rot = newModule.GetRotation().ToString();
            UnityEditor.AssetDatabase.CreateAsset(newModule, $"Assets/Modules/Variants/{MOD}_{rot}.asset");
            m_Variants.Add(newModule);
        }
        CloneRotatedValues(mod, mod, 0); // passes the basic mod through so it will recieve the proper connectors

    }

    void CloneRotatedValues(Sc_Module _mod, Sc_Module _newMod, int _rotation)
    {
        // Mesh, Rotation, Weight, Layer Type
        _newMod.SetUp(_mod.GetMesh(), _rotation, _mod.GetWeight(), _mod.GetLayerType());

        // Adjusting X and Z values
        for (int i = 0; i < 4; i++)
        {
            int toPos = _rotation == 0 ? 0 + i : (i + _rotation) % 4;
            _newMod.GetNeighbours()[toPos].SetEdgeType(_mod.GetNeighbours()[i].GetEdgeType());
        }

        // Adjusting Positive Y
        string newY = _mod.GetNeighbours()[(int)edge.Y].GetEdgeType();
        string newRotation = RotationAdjust(newY, _rotation);
        RemoveRotationStringFromBase(ref newY);
        _newMod.GetNeighbours()[(int)edge.Y].SetEdgeType(newY + newRotation);

        // Adjusting Negative Y
        newY = _mod.GetNeighbours()[(int)edge.nY].GetEdgeType();
        newRotation = RotationAdjust(newY, _rotation);
        RemoveRotationStringFromBase(ref newY);
        _newMod.GetNeighbours()[(int)edge.nY].SetEdgeType(newY + newRotation);

    }

    string RotationAdjust(string mod, int _rotation)
    {
        int index = mod.IndexOf('_');
        string val;
        int rotationAdjust = 0;
        if (index >= 0)
        {
            val = mod.Substring(index + 1);
            rotationAdjust = int.TryParse(val, out int rotation) ? (rotation + _rotation) % 4 : 0;


                

        }
        
        string result = "_" + rotationAdjust;
        return result;
    }

    /*Rotation Rules:
     Rotation will occur clockwise
     */

    public void DeleteAllVariantsInFolder()
    {
        // to stop accidental deletion
        if (DELETE)
        {
            // folder should exist before running this Method
            string[] variantFolder = { "Assets/Modules/Variants" };
            foreach (var asset in AssetDatabase.FindAssets("", variantFolder))
            {
                var path = AssetDatabase.GUIDToAssetPath(asset);
                AssetDatabase.DeleteAsset(path);
            }

            List<Sc_Module> toRemove = new List<Sc_Module>();

            foreach (Sc_Module mod in m_modules)
            {
                if (mod == null)
                {
                    toRemove.Add(mod);
                }
                else
                {
                    if (!mod.SameSides())
                    {
                        //RemoveRotationStringFromBase(mod.GetNeighbour(edge.Y).GetEdgeType());
                        //RemoveRotationStringFromBase(mod.GetNeighbour(edge.nY).GetEdgeType());
                    }
                }
            }

            foreach (Sc_Module mod in toRemove)
            {
                m_modules.Remove(mod);
            }

            DELETE = false;
        }

    }

    void RemoveRotationStringFromBase(ref string _str)
    {
        int index = _str.IndexOf("_");
        if (index >= 0)
            _str = _str.Substring(0, index);
    }


    // function to clear all neighbours to reset the modules
    public void ResetModuleNeighbours()
    {
        foreach (Sc_Module mod in m_modules)
        {
            for (int i = 0; i < mod.GetNeighbours().Length; i++)
            {
                mod.GetNeighbours()[i].ClearNeighbours();
            } 
        }
    }

}



/*
1). algorithm that can assign what each tile can be connected to just by using their own sides.
this could work by assigning names to each side that would correlate with how they should interact.

for each side correlating rules apply:

for each side a numbers will be used to distinguish the difference between them.

if an 'S' follows the number that states the sides are symetrical,

if an 'F' follows the number that states it is a flip of number previously mentioned.

for vertical sides a combination is used to declare that it is a Vertical side, the top or bottom of the module,the section type, and the current rotation.
for example the Combination V1_30 declares it is a vertical section on the TOP with 3 sections at the rotation 0.
This would mean that the only connector is a V0_30


2). Collapsing - Every Square, Any thing all at once
All Squares at the start can be anything, create a container for each possible module that contains all possabilities. 

When a square is collapsed at the start of the sequence:
i).     the initial surrounding spaces (all 6) will have all non-possible meshes removed from its possabilities container
ii).    then with each collapsed square the possabilities of each connector piece are checked by looping through and comparing the possible pieces within each module against neighbouring module, this repeats until all modules have their possablitlies checked

BUT

ii).    THE REDO: if instead of forcing the calculation to check all squares everytime the area will be blocked out and only generated within that space, this will be done by creating a clamp.
iii).   Then the code will start the full collapse pattern:
    a).     checking for the lowest entropy module then collapsing it, 
    b).     applying the changes to the surrounding modules based on the collapsed module
    c).     repeat, until all modules are collapsed.





TODO: possibly change the compare algorithm to pass in one edge and then compare that edge to all 4 other sides 
 this would work BUT i haven't figured out how it can tell wether it is rotated or not
SO for now we will generate rotations
 
 */