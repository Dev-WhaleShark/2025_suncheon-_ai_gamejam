#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Text;
using WhaleShark.Gameplay; // GameManager 접근

public class StageManagerDebugWindow : EditorWindow
{
    private StageManager cachedManager;
    private Vector2 scroll;
    private int loadIndex;
    private bool destroyOnReset = true;
    private double lastAutoFindTime;
    private const double AutoFindInterval = 1.5; // seconds

    [MenuItem("Tools/Stage Manager Debug")]
    public static void Open()
    {
        var win = GetWindow<StageManagerDebugWindow>(false, "Stage Debug", true);
        win.Show();
    }

    private void OnEnable()
    {
        titleContent = new GUIContent("Stage Debug");
        TryAutoFind();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        DrawManagerSection();
        EditorGUILayout.Space();
        DrawRuntimeControls();
        EditorGUILayout.Space();
        DrawStatus();
        EditorGUILayout.Space();
        DrawClearedTable();
        RepaintIfNeeded();
    }

    private void DrawManagerSection()
    {
        EditorGUILayout.LabelField("Stage Manager Reference", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            cachedManager = (StageManager)EditorGUILayout.ObjectField(cachedManager, typeof(StageManager), true);
            if (GUILayout.Button("Find", GUILayout.Width(60)))
            {
                TryAutoFind(true);
            }
        }
        if (cachedManager == null)
        {
            EditorGUILayout.HelpBox("씬에 StageManager 인스턴스를 찾을 수 없습니다.", MessageType.Warning);
        }
    }

    private void DrawRuntimeControls()
    {
        EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Play Mode 에서만 조작 가능합니다.", MessageType.Info);
            return;
        }
        if (cachedManager == null)
        {
            EditorGUILayout.HelpBox("StageManager 참조가 없습니다.", MessageType.Warning);
            return;
        }

        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            // Load by index
            using (new EditorGUILayout.HorizontalScope())
            {
                loadIndex = EditorGUILayout.IntField("Load Index", loadIndex);
                GUI.enabled = cachedManager.StageCount > 0;
                if (GUILayout.Button("Load", GUILayout.Width(70)))
                {
                    cachedManager.LoadStage(loadIndex);
                }
                GUI.enabled = true;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = cachedManager.CurrentStageIndex > 0;
                if (GUILayout.Button("Prev"))
                {
                    cachedManager.LoadStage(cachedManager.CurrentStageIndex - 1);
                }
                GUI.enabled = cachedManager.CurrentStageIndex < cachedManager.StageCount - 1 && cachedManager.StageCount > 0;
                if (GUILayout.Button("Next"))
                {
                    cachedManager.LoadNextStage();
                }
                GUI.enabled = true;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = cachedManager.CurrentStage != null;
                if (GUILayout.Button("Reload"))
                {
                    cachedManager.ReloadCurrentStage();
                }
                if (GUILayout.Button("Clear (Report)"))
                {
                    cachedManager.ReportStageCleared();
                }
                if (GUILayout.Button("Unload"))
                {
                    cachedManager.UnloadCurrentStage();
                }
                GUI.enabled = true;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                destroyOnReset = EditorGUILayout.ToggleLeft("Destroy Instances On Reset", destroyOnReset, GUILayout.Width(200));
                if (GUILayout.Button("Reset Progress"))
                {
                    cachedManager.ResetAllProgress(destroyOnReset);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Game Events", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                bool gmReady = GameManager.Instance != null;
                GUI.enabled = gmReady;
                if (GUILayout.Button("Game Clear"))
                {
                    GameManager.Instance.GameClear();
                }
                if (GUILayout.Button("Player Death"))
                {
                    GameManager.Instance.OnPlayerDied();
                }
                GUI.enabled = true;
            }
        }
    }

    private void DrawStatus()
    {
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
        if (cachedManager == null)
        {
            EditorGUILayout.LabelField("(No Manager)");
            return;
        }
        var sb = new StringBuilder();
        sb.AppendLine($"Stage Count : {cachedManager.StageCount}");
        sb.AppendLine($"Current Index : {cachedManager.CurrentStageIndex}");
        sb.AppendLine($"Current Stage : {(cachedManager.CurrentStage ? cachedManager.CurrentStage.name : "(none)")}");
        if (cachedManager.CurrentStage != null)
        {
            sb.AppendLine($"Cleared : {cachedManager.IsCleared(cachedManager.CurrentStageIndex)}");
        }
        EditorGUILayout.HelpBox(sb.ToString(), MessageType.None);
    }

    private void DrawClearedTable()
    {
        if (cachedManager == null) return;
        if (cachedManager.StageCount <= 0) return;
        EditorGUILayout.LabelField("Cleared Flags", EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(120));
        int cols = 8;
        int count = cachedManager.StageCount;
        int rows = (count + cols - 1) / cols;
        for (int r = 0; r < rows; r++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int c = 0; c < cols; c++)
                {
                    int idx = r * cols + c;
                    if (idx >= count) break;
                    bool cleared = cachedManager.IsCleared(idx);
                    var style = new GUIStyle(EditorStyles.miniButtonMid)
                    {
                        normal = { textColor = cleared ? Color.green : Color.white },
                        fontStyle = cleared ? FontStyle.Bold : FontStyle.Normal
                    };
                    if (GUILayout.Button($"{idx}\n{(cleared ? "OK" : "-")}", style, GUILayout.Width(50), GUILayout.Height(36)))
                    {
                        if (Application.isPlaying)
                        {
                            cachedManager.LoadStage(idx);
                        }
                    }
                }
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void TryAutoFind(bool force = false)
    {
        if (!force && EditorApplication.timeSinceStartup - lastAutoFindTime < AutoFindInterval) return;
        lastAutoFindTime = EditorApplication.timeSinceStartup;
        if (cachedManager == null)
        {
            cachedManager = FindObjectOfType<StageManager>();
        }
        if (cachedManager != null && loadIndex >= cachedManager.StageCount)
        {
            loadIndex = Mathf.Clamp(loadIndex, 0, Mathf.Max(0, cachedManager.StageCount - 1));
        }
    }

    private void RepaintIfNeeded()
    {
        if (Application.isPlaying)
        {
            // 자주 상태 변하므로 재도색
            Repaint();
        }
    }
}
#endif
