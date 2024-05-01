using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sc_ : MonoBehaviour
{

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