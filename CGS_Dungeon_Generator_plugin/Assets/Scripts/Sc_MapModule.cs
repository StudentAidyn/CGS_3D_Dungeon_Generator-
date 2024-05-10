using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sc_MapModule
{
    // list of possible options that the module could become
    List<Sc_Module> m_options;
    bool m_collapsed; // to state if the module has already been collapsed
    Sc_Module m_module; // the module it has become when it gets collapsed

    // Constructor w/ List of module Parameter
    public Sc_MapModule(List<Sc_Module> _options)
    {
        SetOptions(_options);
    }

    public void SetOptions(List<Sc_Module> _options)
    {
        m_options = new List<Sc_Module>(_options);
    }


    // Getter and Setter for Options
    public void ResetOptions(List<Sc_Module> _options)
    {
        m_options.Clear();
        SetOptions(_options);

        m_collapsed = false;
        m_module = null;
    }
    public List<Sc_Module> GetOptions()
    {
        return m_options;
    }


    // removes option in parameter
    public void RemoveOption(Sc_Module _mod)
    {
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
        Debug.Log(m_options.Count);


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
                m_collapsed = true;
                return tile.GetMesh();
            }
        }

        //if fails
        if (m_options.Count > 0)
        {
            // collapses the current tile based on the principle that it has the lowest entropy so it must close, if there is more than one ooption apply randomization
            m_module = m_options[Random.Range(0, m_options.Count)];
            m_collapsed = true;
            return m_module.GetMesh();
        }
        return null;

    }

    // returns the current Entropy of the object (returns total options) : TODO: change it so the Entropy is effected by the weight
    public double GetEntropy()
    {
        float sumWeightLogWeight = 0;
        foreach (var tile in m_options)
        {
            sumWeightLogWeight += tile.GetWeight() * Mathf.Log(tile.GetWeight());
        }

        double shannon_entropy_for_module = Mathf.Log(GetTotalWeight()) - (sumWeightLogWeight / GetTotalWeight());

        return shannon_entropy_for_module;

    }

    float GetTotalWeight()
    {
        float weight = 0;
        foreach (Sc_Module mod in m_options)
        {
            weight += mod.GetWeight();
        }
        if (weight == 0) { return 1; }
        return weight;
    }

    float GetModuleWeight(Sc_Module _mod)
    {
        float weight = _mod.GetWeight() / GetTotalWeight();

        return weight;
    }

}
