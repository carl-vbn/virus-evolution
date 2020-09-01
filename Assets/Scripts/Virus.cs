using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Virus
{
    public string Name {get; private set;}
    public int Generation {get; private set;}
    public Dictionary<string, float> HostTraitMultipliers {get; private set;}

    public Virus(string name, int generation) {
        this.Name = name;
        this.Generation = generation;

        HostTraitMultipliers = new Dictionary<string, float>();

        foreach (string key in Entity.TRAIT_KEYS) {
            HostTraitMultipliers[key] = 1F;
        }
    }

    public Virus CreateCopy(float mutationRate, float mutationForce) {
        bool mutation = Random.value < mutationRate;

        Virus copy = new Virus(this.Name, this.Generation+(mutation ? 1 : 0));
        
        foreach (string traitName in HostTraitMultipliers.Keys) {
            copy.HostTraitMultipliers[traitName] = this.HostTraitMultipliers[traitName];
            
            if (Random.value < mutationRate) {
                copy.HostTraitMultipliers[traitName] += Random.Range(-mutationForce, mutationForce);
            }
        }

        return copy;
    }
 
    public override string ToString()
    {
        return Name+"-G"+Generation;
    }
}
