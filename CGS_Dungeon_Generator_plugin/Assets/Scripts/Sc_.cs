using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Sc_ : MonoBehaviour
{
    // list of all the possible modules
    List<Sc_Module> m_modules = new List<Sc_Module>();

    // generate connections based on the connection rules - can generate during editor (out of play state)
    void CreateConnections()
    {
        // cycle through each module and compare module sides to the other modules sides. long process
        // X and Z can connect to each other and themselves
        // Y can only connect to itself

        foreach (Sc_Module mod in m_modules){
            // each side of the module and then compare that sides connection to each other, make functions for x and z connections and then one for y connections??

            // get module and compare each module side to the other modules
            CompareModules(mod);
            
        }


    }

    // compares the X and Z connection types against themselves and each other
    // could save time if the connection is added to to both modules being tested then never testing prior connections
    void CompareModules(Sc_Module _mod)
    {
        foreach (Sc_Module _other in m_modules)
        {
            CompareX(_mod, _other);
            CompareY(_mod, _other);
            CompareZ(_mod, _other);
        }
    }


    void CompareX(Sc_Module _mod, Sc_Module _other)
    {
        if (CompareEdges(_mod.m_posX, _other.m_negX))
        {
            AddModToNeighbour(_mod, _other, "m_posX");
            AddModToNeighbour(_other, _mod, "m_negX");
        }

        if (CompareEdges(_mod.m_negX, _other.m_posX))
        {
            AddModToNeighbour(_mod, _other, "m_negX");
            AddModToNeighbour(_other, _mod, "m_posX");
        }
    }
    void CompareY(Sc_Module _base, Sc_Module _mod)
    {
        //if
    }

    // Compares the Z Edges of the Modules
    void CompareZ(Sc_Module _mod, Sc_Module _other)
    {
        if (CompareEdges(_mod.m_posZ, _other.m_negZ))
        {
            AddModToNeighbour(_mod, _other, "m_posZ");
            AddModToNeighbour(_other, _mod, "m_negZ");
        }

        if (CompareEdges(_mod.m_negZ, _other.m_posZ))
        {
            AddModToNeighbour(_mod, _other, "m_negZ");
            AddModToNeighbour(_other, _mod, "m_posZ");
        }
    }



    // Compares 2 Edges passed through based on the rules given 
    bool CompareEdges(string _baseEdge, string _otherEdge)
    {
        // converts string to uppercase then converts them into chars for easier comparisons
        char[] baseEdge = (_baseEdge.ToUpper()).ToCharArray();
        char[] otherEdge = (_otherEdge.ToUpper()).ToCharArray();
        // Compare if they share the same first edge identifier
        if (baseEdge[0] == otherEdge[0])
        {
            // check size then check if both have an F if so then it fails if not then it passes
            if (baseEdge.Length == 2 && otherEdge.Length == 2)
            {
                // Compares the last 2, if they are both S then they succeed
                // if not then it means at least 1 was an F causing it to fail since it knows it already knows there is 2 chars in this string
                if (baseEdge[1] == 'S' && otherEdge[1] == 'S') {
                    return true;
                }
            }
            // if baseEdge length is larger than otherEdge then it has 2 chars and if any of those chars = f then it can connect
            else if (baseEdge.Length > otherEdge.Length)
            {
                if (baseEdge[1] == 'F') { return true; }
            }
            else if (baseEdge.Length < otherEdge.Length) {
                if (otherEdge[1] == 'F') { return true; }
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

    /*Rules:
     A T can only connect to a B (with the correct orientation) and with the correct number

     */

    void AddModToNeighbour(Sc_Module _base, Sc_Module _mod, string _edge)
    {
        for (int i = 0; i < _base.GetNeighbours().Length; i++)
        {
            if (_base.GetNeighbours()[i].GetEdge() == _edge)
            {
                _base.GetNeighbours()[i].AddNeighbour(_mod);
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



 
 */