#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;

[CustomEditor(typeof(FlyCamera))]
public class EditorCameraAnimation : Editor {

    private float speed = 1;
    private Vector3 firstPos;
    private Vector3 secondPos;

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Camera animation utility", EditorStyles.boldLabel);
        
        speed = EditorGUILayout.FloatField("Speed", speed);
        FlyCamera targetCam = (FlyCamera) target;

        if (GUILayout.Button("First position")) {
            firstPos = targetCam.transform.position;
        }

        if (GUILayout.Button("Second position")) {
            secondPos = targetCam.transform.position;
        }

        if (GUILayout.Button("Play")) {
            EditorCoroutineUtility.StartCoroutine(AnimationCoroutine(targetCam.transform), this);
        }
    }

    System.Collections.IEnumerator AnimationCoroutine(Transform cameraTransform) {
        for (float i = 0; i<100; i+=speed) {
            cameraTransform.position = Vector3.Lerp(firstPos, secondPos, i/100F);
            yield return new EditorWaitForSeconds(0.01F);
        }
    }
}
#endif