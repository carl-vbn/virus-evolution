#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Entity))]
public class EntityCustomEditor : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        Entity entity = (Entity) target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Infection info", EditorStyles.boldLabel);

        Virus currentInfection = entity.Infection;
        if (currentInfection != null) {
            EditorGUILayout.LabelField("Virus name: "+currentInfection.Name);
            EditorGUILayout.LabelField("Virus generation: "+currentInfection.Generation);
        } else {
            EditorGUILayout.LabelField("Entity is healthy.");
        }

        if (entity.BaseTraits != null) {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Traits", EditorStyles.boldLabel);
            foreach (string traitName in entity.BaseTraits.Keys) {
                EditorGUILayout.LabelField(traitName+"="+entity.GetTraitValue(traitName)+(entity.Infection != null ? " [x"+entity.Infection.HostTraitMultipliers[traitName]+"]" : ""));
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Health: "+entity.Health);
        EditorGUILayout.LabelField("Energy: "+entity.Energy);
        EditorGUILayout.LabelField("State: "+entity.CurrentState);
        //EditorGUILayout.LabelField("Speed: "+entity.GetComponent<UnityEngine.AI.NavMeshAgent>().velocity.magnitude);

        if (GUILayout.Button("Kill")) {
            entity.SendMessage("Die");
        }
    }
}
#endif