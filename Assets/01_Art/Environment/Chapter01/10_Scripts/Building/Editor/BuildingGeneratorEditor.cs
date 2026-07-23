// BuildingGeneratorEditor.cs
// ⚠️ 반드시 "Editor" 라는 이름의 폴더 안에 위치해야 함 (Unity 규칙 — 빌드 시 자동 제외됨)
//
// ⚠️ 주의: 프로젝트에 이미 BuildingGenerator용 커스텀 에디터가 있다면
// [CustomEditor(typeof(BuildingGenerator))]가 중복되어 컴파일 에러(CS0101류)가 남.
// 이 파일을 넣기 전에 Assets 전체에서 "CustomEditor(typeof(BuildingGenerator))"를
// 검색해서 기존에 또 있는지 먼저 확인할 것.

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BuildingGenerator))]
public class BuildingGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var gen = (BuildingGenerator)target;

        EditorGUILayout.Space(8);

        if (GUILayout.Button("Generate", GUILayout.Height(32)))
        {
            gen.Generate();
        }

        if (GUILayout.Button("Clear"))
        {
            gen.Clear();
        }
    }
}
#endif
