using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Squad
{
    /// <summary>
    /// ChaserAction이 동작하는 데 필요한 정보를
    /// 하나로 묶어 전달하는 정보 꾸러미.
    /// </summary>
    public class ChaserContext
    {
        // 이 정보 꾸러미를 소유한 추격자.
        public SquadAgent Agent;
        // 사용할 공유 칠판.
        public SquadBlackboard Blackboard;
        // 추격자 자신의 Transform.
        public Transform Self;
        // 추격할 플레이어의 Transform.
        public Transform Player;

        // Used by the horror-chaser action set (chase / investigate / wander).
        // Filled in by HorrorChaserAgent; left null by the squad demo, which
        // uses its own placeholder movement instead.
        public ChaserLocomotion Locomotion;
        public float CatchRadius = 1.2f;
        public float ArriveRadius = 0.6f;
        // 한 WanderStep 완료 후 다음 WanderStep 시작 전 대기하는 시간 (이 범위에서 매번 랜덤 선택).
        public float WanderPauseMin = 1.5f;
        public float WanderPauseMax = 4f;
    }
}
