using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Entity))]
public class PatientZero : MonoBehaviour
{
    public string virusName;

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Entity>().SetInfection(new Virus(virusName, 0));    
    }
}
