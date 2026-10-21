using GodOfWar.Combat;
using NUnit.Framework;
using UnityEditor;

namespace GodOfWar.Tests.Combat
{
    public class AttackActionTests
    {
        private const string ActionPath = "Assets/_Project/Data/Player/Actions/Attacks/LightAttack1.asset";
        private const float Epsilon = 1e-3f;

        private AttackAction _action;

        [SetUp]
        public void SetUp()
        {
            _action = AssetDatabase.LoadAssetAtPath<AttackAction>(ActionPath);
            Assert.That(_action, Is.Not.Null, $"Missing attack action at '{ActionPath}'.");
        }

        [Test]
        public void IsInComboWindow_AtComboWindowStart_ReturnsTrue()
        {
            Assert.That(_action.IsInComboWindow(_action.ComboWindowStart), Is.True);
        }

        [Test]
        public void IsInComboWindow_BeforeComboWindowStart_ReturnsFalse()
        {
            Assert.That(_action.IsInComboWindow(_action.ComboWindowStart - Epsilon), Is.False);
        }

        [Test]
        public void IsInComboWindow_AtComboWindowEnd_ReturnsTrue()
        {
            Assert.That(_action.IsInComboWindow(_action.ComboWindowEnd), Is.True);
        }

        [Test]
        public void IsInComboWindow_AfterComboWindowEnd_ReturnsFalse()
        {
            Assert.That(_action.IsInComboWindow(_action.ComboWindowEnd + Epsilon), Is.False);
        }
    }
}
