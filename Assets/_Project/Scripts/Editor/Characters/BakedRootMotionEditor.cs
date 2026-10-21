using GodOfWar.Characters;
using UnityEditor;
using UnityEngine;

namespace GodOfWar.EditorTools.Characters
{
    [CustomEditor(typeof(BakedRootMotion))]
    public sealed class BakedRootMotionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var rootMotion = (BakedRootMotion)target;
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(rootMotion.SourceClip == null))
            {
                // 다시 구우면 인스펙터에서 튜닝한 시간·X/Z 커브가 원본 값으로 덮인다.
                if (GUILayout.Button("원본 클립에서 다시 굽기"))
                {
                    BakedRootMotionExtractor.Bake(rootMotion, rootMotion.SourceClip);
                }
            }
        }
    }
}
