using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace GodOfWar.Combat
{
    /// <summary>
    /// 한 프레임 동안 기록된 타격을 모아, 모든 LateUpdate가 끝난 뒤 같은 프레임 안에 한꺼번에 전하는 시스템.
    /// 그래서 서로 동시에 친 두 칼은, 먼저 전해진 타격에 상대 칼이 닫혀도 둘 다 맞힌다.
    /// 피격자가 내린 판정은 기록할 때 넘긴 콜백으로만 돌려주므로, 판정을 알릴 수 있는 것은 이 시스템뿐이다.
    /// 씬에 배치하지 않아도 동작하도록 플레이어 루프에 직접 끼워 넣는다.
    /// </summary>
    public static class HitSystem
    {
        private readonly struct PendingHit
        {
            public PendingHit(IHittable target, AttackInfo attack, Action<HitEvent> onResolved)
            {
                Target = target;
                Attack = attack;
                OnResolved = onResolved;
            }

            public IHittable Target { get; }
            public AttackInfo Attack { get; }
            public Action<HitEvent> OnResolved { get; }
        }

        // 전하는 도중에 기록이 생겨도 이번 목록을 건드리지 않도록 두 목록을 번갈아 쓴다.
        private static List<PendingHit> s_pending = new List<PendingHit>();
        private static List<PendingHit> s_delivering = new List<PendingHit>();

        /// <summary>
        /// <paramref name="target"/>에게 전할 타격을 기록한다. 이번 프레임의 모든 LateUpdate가 끝난 뒤에 전하고,
        /// 피격자가 내린 판정을 <paramref name="onResolved"/>로 돌려준다.
        /// </summary>
        public static void Record(IHittable target, in AttackInfo attack, Action<HitEvent> onResolved)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (attack.Attacker == null)
            {
                throw new ArgumentException("The attack has no attacker.", nameof(attack));
            }

            if (onResolved == null)
            {
                throw new ArgumentNullException(nameof(onResolved));
            }

            s_pending.Add(new PendingHit(target, attack, onResolved));
        }

        // 도메인 리로드를 끈 프로젝트라 정적 목록과 플레이어 루프가 플레이 세션 사이에 남으므로,
        // 시작할 때 비우고 이전 등록을 지운 뒤 다시 끼워 넣는다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install()
        {
            s_pending.Clear();
            s_delivering.Clear();

            PlayerLoopSystem root = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoopSystem[] phases = root.subSystemList;
            for (int i = 0; i < phases.Length; i++)
            {
                if (phases[i].type == typeof(PreLateUpdate))
                {
                    phases[i].subSystemList = InsertAfterLateUpdate(phases[i].subSystemList);
                    root.subSystemList = phases;
                    PlayerLoop.SetPlayerLoop(root);
                    return;
                }
            }

            throw new InvalidOperationException($"The player loop has no {nameof(PreLateUpdate)} phase.");
        }

        private static PlayerLoopSystem[] InsertAfterLateUpdate(PlayerLoopSystem[] systems)
        {
            // 이전 플레이 세션에서 넣은 항목이 남아 있을 수 있어, 빼고 다시 넣는다.
            // 그러면 항목 수가 그대로일 수 있으므로 끼워 넣었는지는 따로 기록한다.
            var result = new List<PlayerLoopSystem>(systems.Length + 1);
            bool inserted = false;
            foreach (PlayerLoopSystem system in systems)
            {
                if (system.type == typeof(HitSystem))
                {
                    continue;
                }

                result.Add(system);
                if (system.type == typeof(PreLateUpdate.ScriptRunBehaviourLateUpdate))
                {
                    result.Add(new PlayerLoopSystem { type = typeof(HitSystem), updateDelegate = Deliver });
                    inserted = true;
                }
            }

            if (!inserted)
            {
                throw new InvalidOperationException(
                    $"The player loop has no {nameof(PreLateUpdate.ScriptRunBehaviourLateUpdate)} system.");
            }

            return result.ToArray();
        }

        private static void Deliver()
        {
            if (s_pending.Count == 0)
            {
                return;
            }

            (s_pending, s_delivering) = (s_delivering, s_pending);
            foreach (PendingHit hit in s_delivering)
            {
                DeliverOne(hit);
            }

            s_delivering.Clear();
        }

        private static void DeliverOne(in PendingHit hit)
        {
            // 인터페이스 참조는 파괴된 Unity 오브젝트를 == null로 거를 수 없어 Unity 오브젝트로 확인한다.
            // 공격자가 없으면 피격자가 방향을 판정할 수 없다.
            if (IsDestroyed(hit.Target) || hit.Attack.Attacker == null)
            {
                return;
            }

            // 한 대상의 오류로 같은 프레임의 다른 타격까지 사라지지 않도록 기록하고 이어서 전한다.
            try
            {
                HitResult result = hit.Target.ReceiveHit(hit.Attack);
                // 기록한 뒤 판정을 받을 쪽이 파괴됐으면 알릴 곳이 없다.
                if (IsDestroyed(hit.OnResolved.Target))
                {
                    return;
                }

                hit.OnResolved(new HitEvent(hit.Target, hit.Attack, result));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, hit.Target as UnityEngine.Object);
            }
        }

        private static bool IsDestroyed(object value)
        {
            return value is UnityEngine.Object unityObject && unityObject == null;
        }
    }
}
