using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


/*
 TODO:
Update module to check edges based on rotation
and to not check rotation on objects that have all sides the same
 */


[ExecuteInEditMode]
public class Sc_ModGenerator : MonoBehaviour
{
    // list of all the possible modules
    [SerializeField] List<Sc_Module> m_modules = new List<Sc_Module>();

    // generate connections based on the connection rules - can generate during editor (out of play state)
    public void CreateConnections()
    {
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

    // compares the positive X of the main module to the negative X of the other,
    // then compares the negative X of the main mod to the positive of the other
    void CompareX(Sc_Module _mod, Sc_Module _other)
    {
        if (CompareEdges(_mod.m_posX, _other.m_negX))
        {
            AddModToNeighbour(_mod, _other, "posX");
            AddModToNeighbour(_other, _mod, "negX");
        }

        if (CompareEdges(_mod.m_negX, _other.m_posX))
        {
            AddModToNeighbour(_mod, _other, "negX");
            AddModToNeighbour(_other, _mod, "posX");
        }
        Debug.Log("Compared X");
    }
    void CompareY(Sc_Module _mod, Sc_Module _other)
    {
        if(CompareVerticalEdges(_mod.m_posY, _other.m_negY))
        {
            AddModToNeighbour(_mod, _other, "posY");
            AddModToNeighbour(_other, _mod, "negY");
        }

        if (CompareVerticalEdges(_mod.m_negY, _other.m_posY))
        {
            AddModToNeighbour(_mod, _other, "negY");
            AddModToNeighbour(_other, _mod, "posY");
        }
    }
    void CompareZ(Sc_Module _mod, Sc_Module _other)
    {
        if (CompareEdges(_mod.m_posZ, _other.m_negZ))
        {
            AddModToNeighbour(_mod, _other, "posZ");
            AddModToNeighbour(_other, _mod, "negZ");
        }

        if (CompareEdges(_mod.m_negZ, _other.m_posZ))
        {
            AddModToNeighbour(_mod, _other, "negZ");
            AddModToNeighbour(_other, _mod, "posZ");
        }
    }

    // Compares 2 Edges passed through based on the rules given 
    bool CompareEdges(string _edge, string _other)
    {
        // converts string to uppercase then converts them into chars for easier comparisons
        char[] edge = (_edge.ToUpper()).ToCharArray();
        char[] other = (_other.ToUpper()).ToCharArray();
        // Compare if they share the same first edge identifier
        if (edge[0] == other[0])
        {
            // check size then check if both have an F if so then it fails if not then it passes
            if (edge.Length == 2 && other.Length == 2)
            {
                // Compares the last 2, if they are both S then they succeed
                // if not then it means at least 1 was an F causing it to fail since it knows it already knows there is 2 chars in this string
                if (edge[1] == 'S' && other[1] == 'S') {
                    return true;
                }
            }
            // if baseEdge length is larger than otherEdge then it has 2 chars and if any of those chars = f then it can connect
            else if (edge.Length > other.Length)
            {
                if (edge[1] == 'F') { return true; }
            }
            else if (edge.Length < other.Length) {
                if (other[1] == 'F') { return true; }
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
        char[] edge = (_edge.ToUpper()).ToCharArray();
        char[] other = (_other.ToUpper()).ToCharArray();

        if (edge[0] == other[0])
        {
            return true;
        }

        return false;
    }

    /*Rules:
     A T can only connect to a B (with the correct orientation) and with the correct number

     */

    void AddModToNeighbour(Sc_Module _mod, Sc_Module _other, string _edge)
    {
        for (int i = 0; i < _mod.GetNeighbours().Length; i++)
        {
            Debug.Log(_mod.GetNeighbours()[i].GetEdge() + " :  " + _edge);
            if (_mod.GetNeighbours()[i].GetEdge() == _edge)
            {
                _mod.GetNeighbours()[i].AddNeighbour(_other, _other.GetRotation()); // TODO: Fix this so it records the current rotation when connecting then the current rotation of the object
            }
        }
    }

    
    
    public void CreateRotatedVariants()
    {
        CreateVariant(m_modules[0]);
    }

    // create simple check for if a module is the same on all  sides
    // with the top and bottom I realised they will need to be adjusted since the variable will need to know if it can loop,
    // I think by adding a new rule it can check if a top will need to be oriented if not the object can be kept the same

    void CreateVariant(Sc_Module mod)
    {
        Sc_Module newModule = ScriptableObject.CreateInstance<Sc_Module>();

        CloneRotatedValues(mod, newModule);//from Mod to NewMod

        string MOD = mod.name; // name of the asset;
        string text = newModule.GetRotation().ToString();
        UnityEditor.AssetDatabase.CreateAsset(newModule, $"Assets/Modules/{MOD}_{text}.asset");
        m_modules.Add(newModule);
    }
    
    void CloneRotatedValues(Sc_Module _mod, Sc_Module _newMod)
    {
        // Mesh, Rotation, Weight
        _newMod.SetUp(_mod.GetMesh(), _mod.GetRotation() + 1, _mod.GetWeight());

        //
        switch (_newMod.GetRotation()) {
            case 1:
                _newMod.SetEdges(
                    _mod.m_posZ,
                    _mod.m_negZ,
                    _mod.m_posY + "_" + _newMod.GetRotation().ToString(),
                    _mod.m_negY + "_" + _newMod.GetRotation().ToString(),
                    _mod.m_negX,
                    _mod.m_posX);
                break;
            case 2:
                break;
            case 3:
                break;
            default:
                _newMod.SetEdges(
                    _mod.m_posX,
                    _mod.m_negX,
                    _mod.m_posY,
                    _mod.m_negY,
                    _mod.m_posZ,
                    _mod.m_negZ);
                break;
        }
    }

    /*Rotation Rules:
     Rotation will occur clockwise
     */

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