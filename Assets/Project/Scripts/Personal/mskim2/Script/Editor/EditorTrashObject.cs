using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(TrashObject))]
public class EditorTrashObject : Editor
{
    static int _testDamage = 1;
    static Vector2 _testDirection = Vector2.up;

    public override void OnInspectorGUI()
    {
        // 기본 인스펙터 먼저
        DrawDefaultInspector();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("TrashObject Test Utilities", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Play Mode에서 테스트 버튼이 활성화됩니다.", MessageType.Info);
            }

            _testDamage = EditorGUILayout.IntSlider("Test Damage", _testDamage, 1, 50);
            _testDirection = EditorGUILayout.Vector2Field("Hit Direction", _testDirection);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Test Hit"))
            {
                ApplyToTargets(t => t.Hit(_testDamage, (Vector2)t.transform.position - _testDirection.normalized * 0.05f, _testDirection, this));
            }
            if (GUILayout.Button("Test Kill"))
            {
                ApplyToTargets(t => { while (t.IsAlive) t.Hit(_testDamage, t.transform.position, _testDirection == Vector2.zero ? Vector2.up : _testDirection, this); });
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Reset (SetActive Toggle)"))
            {
                foreach (var o in targets.OfType<TrashObject>())
                {
                    if (!o.gameObject.activeSelf) o.gameObject.SetActive(true); // 풀링 케이스
                    o.gameObject.SetActive(false);
                    o.gameObject.SetActive(true);
                }
            }
        }
    }

    void ApplyToTargets(System.Action<TrashObject> action)
    {
        foreach (var o in targets.OfType<TrashObject>())
        {
            if (o != null && o.isActiveAndEnabled)
            {
                action(o);
            }
        }
    }
}
