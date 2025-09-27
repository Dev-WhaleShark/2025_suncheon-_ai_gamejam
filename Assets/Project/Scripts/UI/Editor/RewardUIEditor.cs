using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RewardUI))]
public class RewardUIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var ui = (RewardUI)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("RewardUI Debug", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Play Mode에서만 테스트 가능", MessageType.Info);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Test Show Dummy"))
            {
                ui.TestShowRandom();
            }
            if (GUILayout.Button("Hide Immediate"))
            {
                ui.HideUIImmediate();
            }
            GUILayout.EndHorizontal();
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }
}

