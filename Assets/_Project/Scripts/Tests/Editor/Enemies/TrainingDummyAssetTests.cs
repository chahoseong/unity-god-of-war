using GodOfWar.Combat;
using GodOfWar.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using static GodOfWar.Tests.Combat.AnimationAssetAssertions;

namespace GodOfWar.Tests.Enemies
{
    /// <summary>
    /// 훈련용 더미 프리팹의 역경직 반응 연결을 검사한다. 엉뚱한 클립이나 변위를 연결해도 재생은 되어 플레이로는 잘 드러나지 않는다.
    /// 원본 클립은 Synty 에셋이므로 Synty가 없는 환경에서는 실패한다.
    /// </summary>
    public class TrainingDummyAssetTests
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/Enemies/TrainingDummy.prefab";
        private const string ControllerPath = "Assets/_Project/Animations/Enemies/TrainingDummy.controller";

        // 반응 클립 길이로 대기 복귀 시점을 정하므로, 애니메이터가 재생하는 클립과 다르면 반응이 끊기거나 멈춰 선다.
        [Test]
        public void DeflectedClip_ForTrainingDummy_MatchesMotionOfStateWithSameName()
        {
            HitReaction reaction = LoadDeflectedReaction();

            Assert.That(FindStateMotion(ControllerPath, reaction.StateName), Is.SameAs(reaction.Clip));
        }

        [Test]
        public void DeflectedMovement_ForTrainingDummy_IsBakedFromRootMotionVersionOfClip()
        {
            HitReaction reaction = LoadDeflectedReaction();

            AssertBakedFromRootMotionVersion(reaction.Movement, reaction.Clip);
        }

        private static HitReaction LoadDeflectedReaction()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null, $"Missing training dummy prefab at '{PrefabPath}'.");
            var dummy = prefab.GetComponentInChildren<TrainingDummy>(true);
            Assert.That(dummy, Is.Not.Null, $"'{PrefabPath}' has no {nameof(TrainingDummy)}.");

            // 반응은 비공개 직렬화 필드라 에디터 직렬화로 읽는다.
            var serialized = new SerializedObject(dummy);
            SerializedProperty property = serialized.FindProperty("_deflectedReaction");
            Assert.That(property, Is.Not.Null, $"{nameof(TrainingDummy)} has no serialized deflected reaction.");
            return (HitReaction)property.boxedValue;
        }
    }
}
