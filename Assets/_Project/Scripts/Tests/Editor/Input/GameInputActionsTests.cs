using System;
using System.Collections.Generic;
using System.Linq;
using GodOfWar.Input;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;

namespace GodOfWar.Tests.Input
{
    /// <summary>
    /// 입력 리더가 이름으로 찾는 액션들이 에셋에 올바른 형태로 존재하는지 검증한다.
    /// 에디터에서 액션 이름이나 타입을 바꾸면 컴파일은 통과하고 런타임에서만 깨지므로 이 테스트가 막아 준다.
    /// </summary>
    public class GameInputActionsTests
    {
        private const string AssetPath = "Assets/_Project/Input/GameInputActions.inputactions";
        private const string PlayerMapName = "Player";

        private static readonly string[] AxisActionNames = { "Move", "Look" };
        private static readonly string[] ControlSchemes = { "Keyboard&Mouse", "Gamepad" };

        private InputActionMap _playerMap;

        [SetUp]
        public void SetUp()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetPath);
            Assert.That(asset, Is.Not.Null, $"Input actions asset not found at '{AssetPath}'.");
            _playerMap = asset.FindActionMap(PlayerMapName, throwIfNotFound: true);
        }

        [TestCaseSource(nameof(PlayerActions))]
        public void PlayerMap_WithPlayerActionName_ContainsButtonAction(PlayerAction playerAction)
        {
            InputAction action = _playerMap.FindAction(playerAction.ToString());

            Assert.That(action, Is.Not.Null);
            Assert.That(action.type, Is.EqualTo(InputActionType.Button));
        }

        [TestCaseSource(nameof(AxisActionNames))]
        public void PlayerMap_WithAxisActionName_ContainsVector2ValueAction(string actionName)
        {
            InputAction action = _playerMap.FindAction(actionName);

            Assert.That(action, Is.Not.Null);
            Assert.That(action.type, Is.EqualTo(InputActionType.Value));
            Assert.That(action.expectedControlType, Is.EqualTo("Vector2"));
        }

        [TestCaseSource(nameof(ActionAndSchemePairs))]
        public void PlayerMapAction_ForControlScheme_HasAtLeastOneBinding(string actionName, string scheme)
        {
            InputAction action = _playerMap.FindAction(actionName, throwIfNotFound: true);

            bool hasBinding = action.bindings.Any(binding => IsInGroup(binding, scheme));

            Assert.That(hasBinding, Is.True);
        }

        private static IEnumerable<PlayerAction> PlayerActions() =>
            Enum.GetValues(typeof(PlayerAction)).Cast<PlayerAction>();

        private static IEnumerable<TestCaseData> ActionAndSchemePairs()
        {
            IEnumerable<string> actionNames = AxisActionNames.Concat(PlayerActions().Select(a => a.ToString()));
            foreach (string actionName in actionNames)
            {
                foreach (string scheme in ControlSchemes)
                {
                    yield return new TestCaseData(actionName, scheme);
                }
            }
        }

        // 컴포지트 본체는 그룹이 비어 있고 파트 바인딩에 그룹이 붙으므로 파트까지 함께 검사한다.
        private static bool IsInGroup(InputBinding binding, string group) =>
            !string.IsNullOrEmpty(binding.groups) &&
            binding.groups.Split(InputBinding.Separator).Contains(group);
    }
}
