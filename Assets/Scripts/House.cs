using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class House : MonoBehaviour
{
    public int inhabitantCount;
    public GameObject entityPrefab;
    public Material inhabitantMaterial;

    private List<Entity> inhabitants;

    // Start is called before the first frame update
    void Start()
    {
        inhabitants = new List<Entity>();
        for (int i = 0; i<inhabitantCount; i++) {
            GameObject instance = Instantiate(entityPrefab);
            Entity entity = instance.GetComponent<Entity>();
            instance.GetComponent<MeshRenderer>().material = inhabitantMaterial;
            entity.healthyMaterial = inhabitantMaterial;
            entity.home = this;
            instance.transform.position = transform.position;
            instance.GetComponent<NavMeshAgent>().Warp(transform.position);
            inhabitants.Add(entity);
        }
    }

    void OnTriggerEnter(Collider other) {
        if (other.gameObject.CompareTag("Entity")) {
            Entity entity = other.GetComponent<Entity>();
            if (entity.CurrentState == EntityState.Going_Home) {
                entity.SendMessage("EnterHome");
            }
        }
    }

    // Every healthy person that is currently in the house will have a high chance of getting infected when this is called
    public void SpreadInfection(Entity infector, Virus virus) {
        foreach(Entity inhabitant in inhabitants) {
            if (inhabitant.CurrentState == EntityState.At_Home && inhabitant.Infection == null && Random.value < 0.75F) {
                infector.Infect(inhabitant);
            }
        }
    }
}
