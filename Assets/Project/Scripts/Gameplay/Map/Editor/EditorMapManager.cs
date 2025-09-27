using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Stage))]
public class MapManagerEditor : Editor
{
    private Stage mgr;
    private Vector2Int _lastPainted = new Vector2Int(int.MinValue, int.MinValue);
    private bool _dragging;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        mgr = (Stage)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("All Clean"))
            {
                mgr.SetAllClean();
                MarkDirty();
            }

            if (GUILayout.Button("All Poll ON"))
            {
                mgr.SetAllPollution(true);
                MarkDirty();
            }

            if (GUILayout.Button("All Poll OFF"))
            {
                mgr.SetAllPollution(false);
                MarkDirty();
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("All Trash ON"))
            {
                mgr.SetAllTrash(true);
                MarkDirty();
            }

            if (GUILayout.Button("All Trash OFF"))
            {
                mgr.SetAllTrash(false);
                MarkDirty();
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Rand Trash 5"))
            {
                mgr.TestRandomTrash();
                MarkDirty();
            }

            if (GUILayout.Button("Rand Poll 5"))
            {
                mgr.TestRandomPollution();
                MarkDirty();
            }

            if (GUILayout.Button("Rand Both 5"))
            {
                mgr.TestRandomBoth();
                MarkDirty();
            }
        }

        EditorGUILayout.HelpBox("Scene 뷰 페인트: 좌클릭 상태 순환 / Shift=Trash 토글 / Ctrl=Pollution 토글 / Alt=Clean. 드래그 가능.",
            MessageType.Info);
    }

    private void MarkDirty()
    {
        if (mgr != null)
        {
            EditorUtility.SetDirty(mgr);
        }
    }

    private void OnSceneGUI()
    {
        mgr = (Stage)target;
        if (Application.isPlaying) return; // 에디터 편집 전용
        if (mgr == null) return;
        if (!mgr.enabled) return;

        Event e = Event.current;
        int controlId = GUIUtility.GetControlID(FocusType.Passive);

        if (e.isMouse)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            float planeZ = mgr.transform.position.z;
            if (Mathf.Abs(ray.direction.z) < 0.0001f) return;
            float t = (planeZ - ray.origin.z) / ray.direction.z;
            if (t < 0) return;
            Vector3 hit = ray.origin + ray.direction * t;

            if (mgr.WorldToGrid(hit, out var cell))
            {
                Handles.color = new Color(1f, 1f, 1f, 0.25f);
                Vector3 c = mgr.GridToWorldCenter(cell);
                float s = Mathf.Max(0.01f, GetCellSize(mgr));
                Handles.DrawWireCube(c, new Vector3(s, s, 0));

                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    GUIUtility.hotControl = controlId;
                    _dragging = true;
                    _lastPainted = new Vector2Int(int.MinValue, int.MinValue);
                    Paint(cell, e);
                    e.Use();
                }
                else if (e.type == EventType.MouseDrag && e.button == 0 && _dragging)
                {
                    if (cell != _lastPainted)
                    {
                        Paint(cell, e);
                    }

                    e.Use();
                }
                else if (e.type == EventType.MouseUp && e.button == 0 && _dragging)
                {
                    _dragging = false;
                    GUIUtility.hotControl = 0;
                    e.Use();
                }
            }
        }
    }

    private float GetCellSize(Stage m)
    {
        var so = new SerializedObject(m);
        var prop = so.FindProperty("cellSize");
        return prop != null ? prop.floatValue : 1f;
    }

    private void Paint(Vector2Int cell, Event e)
    {
        if (mgr == null) return;
        Undo.RecordObject(mgr, "Paint Map Cell");
        bool shift = e.shift;
        bool ctrl = e.control || e.command;
        bool alt = e.alt;

        if (alt)
        {
            mgr.CleanCell(cell);
        }
        else if (shift && !ctrl)
        {
            mgr.SetTrash(cell, !mgr.HasTrash(cell));
        }
        else if (ctrl && !shift)
        {
            mgr.SetPollution(cell, !mgr.HasPollution(cell));
        }
        else if (shift && ctrl)
        {
            bool any = mgr.HasTrash(cell) || mgr.HasPollution(cell);
            mgr.SetTrash(cell, !any);
            mgr.SetPollution(cell, !any);
        }
        else
        {
            bool t = mgr.HasTrash(cell);
            bool p = mgr.HasPollution(cell);
            if (!t && !p)
            {
                mgr.SetTrash(cell, true);
            }
            else if (t && !p)
            {
                mgr.SetTrash(cell, false);
                mgr.SetPollution(cell, true);
            }
            else if (!t && p)
            {
                mgr.SetTrash(cell, true);
                mgr.SetPollution(cell, true);
            }
            else
            {
                mgr.CleanCell(cell);
            }
        }

        _lastPainted = cell;
        MarkDirty();
    }
}
