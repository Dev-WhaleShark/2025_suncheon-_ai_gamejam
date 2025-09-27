#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RewardData))]
public class RewardDataEditor : Editor
{
    private SerializedProperty idProp;
    private SerializedProperty nameProp;
    private SerializedProperty descProp;
    private SerializedProperty iconProp;
    private SerializedProperty rarityProp;

    // 미리보기 크기 (필요시 조절 가능)
    private const float PreviewMaxSize = 256f;
    private float previewSize = 160f; // 기본 표시 크기

    private void OnEnable()
    {
        idProp = serializedObject.FindProperty("id");
        nameProp = serializedObject.FindProperty("displayName");
        descProp = serializedObject.FindProperty("description");
        iconProp = serializedObject.FindProperty("icon");
        rarityProp = serializedObject.FindProperty("rarityWeight");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Reward Data", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(idProp);
        EditorGUILayout.PropertyField(nameProp);
        EditorGUILayout.PropertyField(rarityProp);

        // Description (멀티라인 크게)
        EditorGUILayout.LabelField("Description");
        descProp.stringValue = EditorGUILayout.TextArea(descProp.stringValue, GUILayout.MinHeight(60));

        EditorGUILayout.Space();
        DrawIconSection();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawIconSection()
    {
        EditorGUILayout.LabelField("Icon", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.PropertyField(iconProp, GUIContent.none, GUILayout.Width(200));
            using (new EditorGUILayout.VerticalScope())
            {
                previewSize = EditorGUILayout.Slider("Preview Size", previewSize, 64f, PreviewMaxSize);
                if (iconProp.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("아이콘(Sprite)을 지정하세요.", MessageType.Info);
                }
            }
        }

        var sprite = iconProp.objectReferenceValue as Sprite;
        if (sprite != null)
        {
            Rect r = GUILayoutUtility.GetRect(previewSize, previewSize, GUILayout.ExpandWidth(false));
            DrawSpritePreview(r, sprite);
        }
    }

    private void DrawSpritePreview(Rect r, Sprite sprite)
    {
        if (sprite == null) return;
        Texture2D tex = sprite.texture;
        if (tex == null) return;

        // 스프라이트가 아틀라스 일부일 수 있으므로 UV 계산
        Rect tr = sprite.textureRect;
        Rect uv = new Rect(
            tr.x / tex.width,
            tr.y / tex.height,
            tr.width / tex.width,
            tr.height / tex.height);

        // 정사각형 안에 맞춤
        float ratio = tr.width / tr.height;
        float drawWidth = r.width;
        float drawHeight = r.height;
        if (ratio > 1f) // 가로가 긴 경우 높이 줄임
        {
            drawHeight = drawWidth / ratio;
        }
        else // 세로가 긴 경우 너비 줄임
        {
            drawWidth = drawHeight * ratio;
        }
        Rect centered = new Rect(
            r.x + (r.width - drawWidth) * 0.5f,
            r.y + (r.height - drawHeight) * 0.5f,
            drawWidth,
            drawHeight);

        // 배경(체커) 그리기
        DrawChecker(centered, 8, new Color(0.18f,0.18f,0.18f), new Color(0.24f,0.24f,0.24f));

        // 스프라이트
        GUI.DrawTextureWithTexCoords(centered, tex, uv, true);
        // 테두리
        Handles.color = new Color(1f,1f,1f,0.5f);
        Handles.DrawAAPolyLine(2f, new Vector3[] {
            new(centered.xMin, centered.yMin),
            new(centered.xMax, centered.yMin),
            new(centered.xMax, centered.yMax),
            new(centered.xMin, centered.yMax),
            new(centered.xMin, centered.yMin)
        });
    }

    private void DrawChecker(Rect r, int size, Color c0, Color c1)
    {
        int cols = Mathf.CeilToInt(r.width / size);
        int rows = Mathf.CeilToInt(r.height / size);
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                Rect cr = new Rect(r.x + x * size, r.y + y * size, size, size);
                EditorGUI.DrawRect(cr, ((x + y) % 2 == 0) ? c0 : c1);
            }
        }
    }
}
#endif
