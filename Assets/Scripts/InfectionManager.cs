using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class InfectionManager : MonoBehaviour
{
    public Text txtPopulationInfo;
    public Text txtAverageTraits;
    public Slider averageHealth;
    public Slider averageEnergy;

    const string AVG_DISEASE_TRAITS_FILENAME = "CSV_DATA\\AverageTraitMultipliers.csv";
    const string INFECTION_SUMMARY_FILENAME = "CSV_DATA\\InfectionSummary.csv";
    const string ENTITY_STATES_FILENAME = "CSV_DATA\\EntityStates.csv";

    private int initialEntityCount;

    void Start()
    {
        InvokeRepeating("CollectAndSaveData", 1F, 5F);

        File.WriteAllText(AVG_DISEASE_TRAITS_FILENAME, string.Join(";", Entity.TRAIT_KEYS)+"\r\n");
        File.WriteAllText(INFECTION_SUMMARY_FILENAME, "healthy;infected;dead;#dominant_virus\r\n");
        File.WriteAllText(ENTITY_STATES_FILENAME, "at_home;at_hospital;wandering;chasing;going_home;going_to_hospital\r\n");

        int totalInhabitants = 0;
        foreach(GameObject house in GameObject.FindGameObjectsWithTag("HouseEntry")) {
            totalInhabitants += house.GetComponent<House>().inhabitantCount;
        }
        initialEntityCount = GameObject.FindGameObjectsWithTag("Entity").Length;
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            if (Time.timeScale == 1) {
                Time.timeScale = Input.GetKey(KeyCode.LeftShift) ? 16 : 8;
            } else {
                Time.timeScale = 1;
            }
        }
    }

    void CollectAndSaveData()
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("EN-US");

        int infections = 0;
        int healthyCount = 0;
        float healthSum = 0;
        float energySum = 0;
        Dictionary<string, int> entitiesPerVirusGeneration = new Dictionary<string, int>();
        Dictionary<string, float> traitMultiplierSums = new Dictionary<string, float>();
        Dictionary<EntityState, int> entityStateSummary = new Dictionary<EntityState, int>();

        foreach (string traitName in Entity.TRAIT_KEYS) {
            traitMultiplierSums.Add(traitName, 0);
        }

        int aliveEntities = 0;
        foreach (GameObject entityObj in GameObject.FindGameObjectsWithTag("Entity")) {
            Entity entity = entityObj.GetComponent<Entity>();
            Virus infection = entity.Infection;

            EntityState state = entity.CurrentState;
            if (entityStateSummary.ContainsKey(state))
                entityStateSummary[state]++;
            else
                entityStateSummary.Add(state, 1);

            if (entity.Health > 0) {
                aliveEntities++;
                healthSum += entity.Health;
                energySum += entity.Energy;

                if (infection != null) {
                    infections++;

                    if (entitiesPerVirusGeneration.ContainsKey(infection.ToString())) {
                        entitiesPerVirusGeneration[infection.ToString()]++;
                    } else {
                        entitiesPerVirusGeneration[infection.ToString()] = 1;
                    }

                    foreach (string traitName in Entity.TRAIT_KEYS) {
                        traitMultiplierSums[traitName] += infection.HostTraitMultipliers[traitName];
                    }
                } else {
                    healthyCount++;
                }
            }

        }

        int deaths = initialEntityCount - aliveEntities;

        float[] traitMultiplierAverages = new float[traitMultiplierSums.Count];
        StringBuilder sb = new StringBuilder();

        int i = 0;
        foreach (string key in traitMultiplierSums.Keys) {
            traitMultiplierAverages[i] = traitMultiplierSums[key] / infections;
            if (i > 0) sb.Append("\n");
            sb.Append(key+"="+traitMultiplierAverages[i]);
            i++;
        }

        string dominantVirusGeneration = null;
        foreach (string virusGeneration in entitiesPerVirusGeneration.Keys) {
            if (dominantVirusGeneration == null || entitiesPerVirusGeneration[virusGeneration] > entitiesPerVirusGeneration[dominantVirusGeneration]) {
                dominantVirusGeneration = virusGeneration;
            }
        }

        txtPopulationInfo.text = "Healthy: "+healthyCount+"\nInfected: "+infections+"\nDead: "+deaths+"\n\nDominant virus: "+dominantVirusGeneration;
        txtAverageTraits.text = sb.ToString();

        averageHealth.value = healthSum / aliveEntities / 100;
        averageEnergy.value = energySum / aliveEntities / 100;

        using (StreamWriter sw = File.AppendText(AVG_DISEASE_TRAITS_FILENAME)) {
            sw.WriteLine(string.Join(";", traitMultiplierAverages));
        }

        using (StreamWriter sw = File.AppendText(INFECTION_SUMMARY_FILENAME)) {
            sw.WriteLine(healthyCount+";"+infections+";"+deaths+";"+dominantVirusGeneration);
        }

        foreach (EntityState entityState in System.Enum.GetValues(typeof(EntityState))) {
            if (!entityStateSummary.ContainsKey(entityState))
                entityStateSummary.Add(entityState, 0);
        }

        using (StreamWriter sw = File.AppendText(ENTITY_STATES_FILENAME)) {
            sw.WriteLine(entityStateSummary[EntityState.At_Home]+";"+entityStateSummary[EntityState.At_Hospital]+";"+entityStateSummary[EntityState.Wandering]+";"+entityStateSummary[EntityState.Chasing]+";"+entityStateSummary[EntityState.Going_Home]+";"+entityStateSummary[EntityState.Going_To_Hospital]);
        }
    }
}
