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
        m_options = new List<Sc_Module>(_options);

        m_collapsed = false;
        m_module = null;
    }

    public List<Sc_Module> GetOptions() {
        return m_options; }


    // removes option in parameter
    public void RemoveOption(Sc_Module _mod) {
        List<Sc_Module> toRemove = new List<Sc_Module>();
        
        for (int i = 0; i < m_options.Count; i++)
        {
            if (m_options[i] == _mod)
            {
                toRemove.Add(m_options[i]);
            }
        }

        if(m_options.Count <= 0) return;

        for (int i = 0; i < toRemove.Count; i++)
        {
            m_options.Remove(toRemove[i]);
        }
    }

    // returns module it has collapsed to
    public Sc_Module GetModule()
    {
        return m_module;
    }


    // Checks if the current Module has been collapsed
    public bool IsCollapsed()
    {
        return m_collapsed;
    }

    // Collapses the current Module into one of the options taking in consideration the weights of the objects
    public void Collapse(int _threadType = 0)
    {
        m_collapsed = true;

        // Calculate total weight
        float totalWeight = 0;
        foreach (Sc_Module tile in m_options)
        {
            totalWeight += tile.GetWeight();
        }
         if (totalWeight == 0) { return; }
        // Generate a random value within the range of total weight
        float randomValue = ThreadRandomiser.Instance.GetRandomNumber(_threadType) % totalWeight;

        // Find the tile corresponding to the random value
        float cumulativeWeight = 0;
        foreach (Sc_Module tile in m_options)
        {
            // Increasing weight of each tile until it hits a corresponding tile
            cumulativeWeight += tile.GetWeight();
            if (randomValue <= cumulativeWeight)
            {
                m_module = tile;
                return;
            }
        }
        // if it fails it already has a procedure for failed tiles
    }

    // returns the current Entropy of the object (returns total options) : TODO: change it so the Entropy is effected by the weight
    public double GetEntropy() {
        float totalWeight = 0;
        for (int i = 0; i < m_options.Count; i++)
        {
            totalWeight += m_options[i].GetWeight();
        }

        float sumWeightLogWeight = 0;
        for (int i = 0; i < m_options.Count; i++)
        {
            sumWeightLogWeight += m_options[i].GetWeight() * Mathf.Log(m_options[i].GetWeight());
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
