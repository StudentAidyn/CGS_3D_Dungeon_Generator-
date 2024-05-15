using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sc_MapModule
{
    // Pathing Based Variables -------------------------------

    //Caluculating the costs
    public int gScore; // the cost of the tile
    public int hScore; // the distance to the goal tile
    public int fScore; // the Combination of the G and H costs

    // The Previous Module and the Modules location in the map
    public Sc_MapModule previousModule;
    public Vector3 mapPos;

    // --------------------------------------------------------

    // list of possible options that the module could become
    List<Sc_Module> m_options = new List<Sc_Module>();
    bool m_collapsed; // to state if the module has already been collapsed
    Sc_Module m_module; // the module it has become when it gets collapsed

    
    public Sc_MapModule(Vector3 _mapPos)
    {
        mapPos = _mapPos;
    }

    // Getter and Setter for Options
    public void ResetModule(List<Sc_Module> _options)
    {
        m_options.Clear();
        m_options = new List<Sc_Module>(_options);

        m_collapsed = false;
        m_module = null;
    }

    public List<Sc_Module> GetOptions() { return m_options; }


    // removes option in parameter
    public void RemoveOption(Sc_Module _mod) {
        List<Sc_Module> toRemove = new List<Sc_Module>();
        foreach (Sc_Module mod in m_options)
        {
            if (mod == _mod)
            {
                toRemove.Add(mod);
            }
        }

        foreach (Sc_Module mod in toRemove)
        {
            m_options.Remove(mod);
        }

        toRemove.Clear();
    }

    // returns module it has collapsed to
    public Sc_Module GetModule()
    {
        return m_module;
    }


    // Checks if the current Module has been collapsed
    public bool isCollapsed()
    {
        return m_collapsed;
    }

    // Collapses the current Module into one of the options taking in consideration the weights of the objects
    public GameObject Collapse()
    {
        m_collapsed = true;

        // Calculate total weight
        float totalWeight = 0;
        foreach (Sc_Module tile in m_options)
        {
            totalWeight += tile.GetWeight();
        }

        // Generate a random value within the range of total weight
        float randomValue = Random.Range(0f, totalWeight);

        // Find the tile corresponding to the random value
        float cumulativeWeight = 0;
        foreach (Sc_Module tile in m_options)
        {
            cumulativeWeight += tile.GetWeight();
            if (randomValue <= cumulativeWeight)
            {
                m_module = tile;
                return tile.GetMesh();
            }
        }

        //if fails
        return null;
    }

    // returns the current Entropy of the object (returns total options) : TODO: change it so the Entropy is effected by the weight
    public double GetEntropy() {
        float totalWeight = 0;
        foreach (Sc_Module mod in m_options)
        {
            totalWeight += mod.GetWeight();
        }

        float sumWeightLogWeight = 0;
        foreach (var tile in m_options)
        {
            sumWeightLogWeight += tile.GetWeight() * Mathf.Log(tile.GetWeight());
        }

        double shannon_entropy_for_module = Mathf.Log(totalWeight) - (sumWeightLogWeight / totalWeight);

        shannon_entropy_for_module = shannon_entropy_for_module <= 0 ? 0 : shannon_entropy_for_module;

        return shannon_entropy_for_module;

    }


    public void SetModuleTypeBasedOnLayer(LayerMask _layer)
    {
        List<Sc_Module> toRemove = new List<Sc_Module>();

        // Foreach module possible it checks and confirms the layer type with each module adding it to be removed if not containing the same layer value
        foreach (Sc_Module option in GetOptions()) {
            if (option.GetLayerType() != (option.GetLayerType() | (1 << _layer))) {
                toRemove.Add(option); 
            }
        }

        foreach (Sc_Module option in toRemove) { RemoveOption(option); }
    }

    public void RemoveModuleTypeBasedOnLayer(LayerMask _layer)
    {
        List<Sc_Module> toRemove = new List<Sc_Module>();

        // Foreach module possible it checks and confirms the layer type with each module adding it to be removed if not containing the same layer value
        foreach (Sc_Module option in GetOptions())
        {
            if (option.GetLayerType() == (option.GetLayerType() | (1 << _layer)))
            {
                toRemove.Add(option);
            }
        }

        foreach (Sc_Module option in toRemove) { RemoveOption(option); }
    }

    public void CalculateFScore()
    {
        fScore = gScore + hScore;
    }

}
