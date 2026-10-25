using GodOfWar.Combat;
using GodOfWar.Input;
using GodOfWar.Player;
using GodOfWar.Player.States;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GodOfWar.Tests.Player.States
{
    public class AttackStateTests
    {
        private const string AttackSettingsPath = "Assets/_Project/Data/Player/Settings/Attack.asset";
        private const string AttacksFolder = "Assets/_Project/Data/Player/Actions/Attacks/";

        private AttackSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = AssetDatabase.LoadAssetAtPath<AttackSettings>(AttackSettingsPath);
            Assert.That(_settings, Is.Not.Null, $"Missing attack settings at '{AttackSettingsPath}'.");
        }

        [Test]
        public void ResolveFollowUp_WithSameInputAndNext_ReturnsNext()
        {
            AttackAction current = LoadAttack("LightAttack1");

            AttackAction next = AttackState.ResolveFollowUp(
                current, PlayerAction.LightAttack, PlayerAction.LightAttack, _settings);

            Assert.That(next, Is.SameAs(LoadAttack("LightAttack2")));
        }

        [Test]
        public void ResolveFollowUp_WithSameInputWithoutNext_ReturnsEntryOfSameInput()
        {
            AttackAction current = LoadAttack("LightAttack3");

            AttackAction next = AttackState.ResolveFollowUp(
                current, PlayerAction.LightAttack, PlayerAction.LightAttack, _settings);

            Assert.That(next, Is.SameAs(_settings.GetEntry(PlayerAction.LightAttack)));
        }

        [Test]
        public void ResolveFollowUp_WithDifferentInput_ReturnsEntryOfPressedInput()
        {
            AttackAction current = LoadAttack("LightAttack1");

            AttackAction next = AttackState.ResolveFollowUp(
                current, PlayerAction.LightAttack, PlayerAction.HeavyAttack, _settings);

            Assert.That(next, Is.SameAs(_settings.GetEntry(PlayerAction.HeavyAttack)));
        }

        private static AttackAction LoadAttack(string name)
        {
            string path = AttacksFolder + name + ".asset";
            var action = AssetDatabase.LoadAssetAtPath<AttackAction>(path);
            Assert.That(action, Is.Not.Null, $"Missing attack action at '{path}'.");
            return action;
        }
    }
}
