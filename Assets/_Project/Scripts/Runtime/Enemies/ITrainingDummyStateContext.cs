using GodOfWar.Characters;
using GodOfWar.Combat;
using GodOfWar.Core.StateMachines;
using UnityEngine;

namespace GodOfWar.Enemies
{
    /// <summary>
    /// 훈련용 더미가 자기 상태들에게 열어 주는 창구.
    /// 외부 객체에는 보이지 않도록 <see cref="TrainingDummy"/>가 명시적으로 구현한다.
    /// </summary>
    public interface ITrainingDummyStateContext
    {
        /// <summary>상태가 이동을 맡기는 모터.</summary>
        CharacterMotor Motor { get; }

        /// <summary>상태가 자기 클립으로 크로스페이드할 때 쓰는 애니메이터.</summary>
        Animator Animator { get; }

        /// <summary>무기 판정. 공격 상태가 타격 구간에 맞춰 연다.</summary>
        Hitbox Hitbox { get; }

        /// <summary>상태가 전이를 요청하는 상태 머신.</summary>
        StateMachine Machine { get; }

        /// <summary>상태가 전이 대상 상태를 찾는 창구.</summary>
        IStateLocator States { get; }
    }
}
