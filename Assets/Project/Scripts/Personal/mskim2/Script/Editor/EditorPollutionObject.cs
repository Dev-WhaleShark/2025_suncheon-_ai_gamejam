using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(PollutionObject))]
public class EditorPollutionObject : Editor
{
    private static int _testDamage = 1;
    private static Vector2 _testDirection = Vector2.up;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("PollutionObject Test Utilities", EditorStyles.boldLabel);

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
                ApplyToTargets(p => p.Hit(
                    _testDamage,
                    (Vector2)p.transform.position - _testDirection.normalized * 0.05f,
                    _testDirection == Vector2.zero ? Vector2.up : _testDirection,
                    this));
            }
            if (GUILayout.Button("Test Kill"))
            {
                ApplyToTargets(p =>
                {
                    while (p.IsAlive)
                    {
                        p.Hit(
                            _testDamage,
                            p.transform.position,
                            _testDirection == Vector2.zero ? Vector2.up : _testDirection,
                            this);
                    }
                });
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Force Kill (Instant)"))
            {
                ApplyToTargets(p => p.ForceKill());
            }

            if (GUILayout.Button("Reset (SetActive Toggle)"))
            {
                foreach (var o in targets.OfType<PollutionObject>())
                {
                    if (!o.gameObject.activeSelf)
                    {
                        o.gameObject.SetActive(true);
                    }
                    o.gameObject.SetActive(false);
                    o.gameObject.SetActive(true);
                }
            }
        }
    }

    private void ApplyToTargets(System.Action<PollutionObject> action)
    {
        foreach (var o in targets.OfType<PollutionObject>())
        {
            if (o != null && o.isActiveAndEnabled)
            {
                action(o);
            }
        }
    }
}

