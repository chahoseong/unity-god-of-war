using System;
using System.Collections.Generic;
using System.IO;
using GodOfWar.Characters;
using UnityEditor;
using UnityEngine;

namespace GodOfWar.EditorTools.Characters
{
    /// <summary>
    /// Humanoid 클립의 루트 궤적(RootT)을 직선 변위로 구워 <see cref="BakedRootMotion"/>에 채운다.
    /// 어떤 액션에 쓰일지는 모르며, 클립 하나에서 에셋 하나를 만든다.
    /// </summary>
    public static class BakedRootMotionExtractor
    {
        public const string OutputFolder = "Assets/_Project/Data/RootMotion";

        private const string MenuPath = "Assets/GodOfWar/Bake Root Motion";

        // 이보다 짧은 순 이동은 제자리 클립으로 보고 거리 0으로 굽는다 (휴먼 정규화 단위).
        private const float MinNetDisplacement = 1e-3f;

        /// <summary>순수 변환 결과. 거리는 미터, 커브는 정규화 시간 → 정규화 변위.</summary>
        public readonly struct Result
        {
            public Result(float distance, float duration, AnimationCurve curve)
            {
                Distance = distance;
                Duration = duration;
                Curve = curve;
            }

            public float Distance { get; }
            public float Duration { get; }
            public AnimationCurve Curve { get; }
        }

        /// <summary>
        /// 프레임마다 균등하게 샘플링한 루트 수평 위치(XZ, 휴먼 정규화 단위)를 순 이동 방향(끝 − 시작)에 투영해
        /// 정규화 변위 커브를 만든다. 원본 모션의 미세한 반동(1 초과·0 미만)은 그대로 담는다.
        /// </summary>
        public static Result Extract(IReadOnlyList<Vector2> rootPositions, float frameRate, float humanScale)
        {
            if (rootPositions == null)
            {
                throw new ArgumentNullException(nameof(rootPositions));
            }

            if (rootPositions.Count < 2)
            {
                throw new ArgumentException("At least two samples are required.", nameof(rootPositions));
            }

            int last = rootPositions.Count - 1;
            float duration = last / frameRate;
            Vector2 start = rootPositions[0];
            Vector2 net = rootPositions[last] - start;
            float netLength = net.magnitude;

            if (netLength < MinNetDisplacement)
            {
                return new Result(0f, duration, AnimationCurve.Linear(0f, 0f, 1f, 1f));
            }

            Vector2 direction = net / netLength;
            var samples = new float[rootPositions.Count];
            for (int i = 0; i <= last; i++)
            {
                samples[i] = Vector2.Dot(rootPositions[i] - start, direction) / netLength;
            }

            return new Result(netLength * humanScale, duration, FromFrameSamples(samples));
        }

        /// <summary><paramref name="clip"/>의 루트 궤적을 구워 <paramref name="target"/>에 덮어쓴다 (Undo 가능).</summary>
        public static void Bake(BakedRootMotion target, AnimationClip clip)
        {
            Result result = Extract(SampleRootPositions(clip), clip.frameRate, GetHumanScale(clip));
            if (result.Distance <= 0f)
            {
                Debug.LogWarning($"'{clip.name}' has no net root displacement. Baked as distance 0.", target);
            }

            var serialized = new SerializedObject(target);
            FindProperty(serialized, "_sourceClip").objectReferenceValue = clip;
            FindProperty(serialized, "_distance").floatValue = result.Distance;
            FindProperty(serialized, "_duration").floatValue = result.Duration;
            FindProperty(serialized, "_curve").animationCurveValue = result.Curve;
            serialized.ApplyModifiedProperties();
        }

        /// <summary>원본 클립 이름에서 Synty 접두어·태그를 뺀 에셋 이름 (예: A_Dodge_F_RootMotion_Sword → Dodge_F).</summary>
        public static string GetAssetName(AnimationClip clip)
        {
            string name = clip.name;
            if (name.StartsWith("A_", StringComparison.Ordinal))
            {
                name = name.Substring(2);
            }

            return name.Replace("_RootMotion", string.Empty).Replace("_Sword", string.Empty);
        }

        [MenuItem(MenuPath)]
        private static void BakeSelection()
        {
            List<AnimationClip> clips = CollectSelectedClips();
            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                Directory.CreateDirectory(OutputFolder);
                AssetDatabase.Refresh();
            }

            foreach (AnimationClip clip in clips)
            {
                string path = $"{OutputFolder}/{GetAssetName(clip)}.asset";
                var asset = AssetDatabase.LoadAssetAtPath<BakedRootMotion>(path);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<BakedRootMotion>();
                    AssetDatabase.CreateAsset(asset, path);
                }

                Bake(asset, clip);
                Debug.Log($"Baked '{clip.name}' → {path} ({asset.Distance:F3} m, {asset.Duration:F3} s)", asset);
            }

            AssetDatabase.SaveAssets();
        }

        [MenuItem(MenuPath, true)]
        private static bool CanBakeSelection()
        {
            return CollectSelectedClips().Count > 0;
        }

        /// <summary>
        /// 선택한 클립과, 선택한 모델 파일에서 파일 이름과 같은 클립을 모은다.
        /// 모델 파일의 WindUp·Hit 같은 구간 클립은 직접 선택했을 때만 굽는다.
        /// </summary>
        private static List<AnimationClip> CollectSelectedClips()
        {
            var clips = new List<AnimationClip>();
            foreach (UnityEngine.Object selected in Selection.objects)
            {
                if (selected is AnimationClip clip)
                {
                    clips.Add(clip);
                    continue;
                }

                string path = AssetDatabase.GetAssetPath(selected);
                if (!(AssetImporter.GetAtPath(path) is ModelImporter))
                {
                    continue;
                }

                string fileName = Path.GetFileNameWithoutExtension(path);
                foreach (UnityEngine.Object sub in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (sub is AnimationClip subClip && subClip.name == fileName)
                    {
                        clips.Add(subClip);
                    }
                }
            }

            return clips;
        }

        private static Vector2[] SampleRootPositions(AnimationClip clip)
        {
            AnimationCurve x = GetRootCurve(clip, "RootT.x");
            AnimationCurve z = GetRootCurve(clip, "RootT.z");

            int frameCount = Mathf.RoundToInt(clip.length * clip.frameRate);
            var positions = new Vector2[frameCount + 1];
            for (int i = 0; i <= frameCount; i++)
            {
                float time = i / clip.frameRate;
                positions[i] = new Vector2(x.Evaluate(time), z.Evaluate(time));
            }

            return positions;
        }

        private static AnimationCurve GetRootCurve(AnimationClip clip, string propertyName)
        {
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.type == typeof(Animator) && binding.propertyName == propertyName)
                {
                    return AnimationUtility.GetEditorCurve(clip, binding);
                }
            }

            throw new InvalidOperationException(
                $"'{clip.name}' has no '{propertyName}' curve. Only Humanoid clips can be baked.");
        }

        /// <summary>RootT는 휴먼 정규화 단위라, 클립이 기준으로 삼은 Avatar의 humanScale을 곱해야 미터가 된다.</summary>
        private static float GetHumanScale(AnimationClip clip)
        {
            string clipPath = AssetDatabase.GetAssetPath(clip);
            var importer = AssetImporter.GetAtPath(clipPath) as ModelImporter;
            string modelPath = importer != null && importer.sourceAvatar != null
                ? AssetDatabase.GetAssetPath(importer.sourceAvatar)
                : clipPath;

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            GameObject instance = UnityEngine.Object.Instantiate(model);
            instance.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var animator = instance.GetComponent<Animator>();
                if (animator == null || !animator.isHuman)
                {
                    throw new InvalidOperationException($"'{modelPath}' has no Humanoid Animator to read humanScale from.");
                }

                animator.Rebind();
                return animator.humanScale;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static SerializedProperty FindProperty(SerializedObject serialized, string name)
        {
            return serialized.FindProperty(name)
                ?? throw new InvalidOperationException($"{serialized.targetObject.GetType().Name} has no serialized field '{name}'.");
        }

        /// <summary>
        /// 0~1 정규화 시간에 균등 배치한 샘플을 선형 탄젠트로 잇는다.
        /// 탄젠트를 0으로 두면 키마다 속도가 0으로 떨어져 매 프레임 멈칫거린다.
        /// </summary>
        private static AnimationCurve FromFrameSamples(float[] samples)
        {
            int last = samples.Length - 1;
            var keys = new Keyframe[samples.Length];
            for (int i = 0; i <= last; i++)
            {
                float inSlope = i > 0 ? (samples[i] - samples[i - 1]) * last : 0f;
                float outSlope = i < last ? (samples[i + 1] - samples[i]) * last : 0f;
                keys[i] = new Keyframe((float)i / last, samples[i], inSlope, outSlope);
            }

            return new AnimationCurve(keys);
        }
    }
}
