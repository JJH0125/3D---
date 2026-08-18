using System.Collections.Generic;
using UnityEngine;

namespace Squad
{
    /// <summary>
    /// Shared "squad memory" — the generalization of Half-Life's squad-leader
    /// information relay. Any chaser's perception writes here; every chaser's
    /// GOAP planner reads here. One sighting propagates to the whole squad.
    ///
    /// This is a scene-level singleton (one squad). For multiple independent
    /// squads, make it a regular component and give each chaser a reference
    /// to its own squad's blackboard.
    /// </summary>
    public class SquadBlackboard : MonoBehaviour
    {
        [Header("○ 튜닝 값 — 자유롭게 조절")]
        [SerializeField]
        [Tooltip("적이 Alerted에서 Suspicious로 다운되기까지 걸리는 시간")]
        private float AlertedToSuspiciousTime = 5f;
        [SerializeField]
        [Tooltip("적이 Suspicious에서 Calm으로 다운되기까지 걸리는 시간")]
        private float SuspiciousToCalmTime = 5f;
        
        public static SquadBlackboard Instance { get; private set; }
        // 3가지 경계 상태 : Calm < Suspicious < Alerted
        public enum AlertLevel { Calm, Suspicious, Alerted }

        // **** 경계 상태 정보 ****
        
        // 적의 경계 상태 (기본값 : Calm)
        public AlertLevel Alert { get; private set; } = AlertLevel.Calm;
        // 현재 경계 상태에 머문 시간
        private float TimeInAlertState;

        // **** 플레이어에 대한 정보 ****

        // 플레이어가 지금 보이는지 여부
        public bool PlayerCurrentlyVisible { get; private set; }
        // 플레이어가 마지막까지 있었던 위치
        public Vector3 LastPlayerPosition { get; private set; }
        // 플레이어가 마지막으로 보인 뒤 지난 시간
        // 지금은 상태 감소를 TimeInAlertState가 맡기 때문에
        // 사용하지 않지만 추후 사용될 여지 있음
        public float TimeSinceLastSeen { get; private set; } = Mathf.Infinity;

        // --- Sound facts (for the InvestigateSound goal) ---
        // HasSound is true while there's an un-investigated sound to check out.
        // LastSoundPosition is where it came from. A chaser walks there (MoveToSound),
        // searches (SearchSoundArea), then calls ClearSound() so it isn't chased
        // forever. Generator sounds and footsteps both land here; the difference
        // (footsteps stay in one dimension, generator sound crosses) is enforced
        // by WHO calls ReportSound — see the detection system, not here.

        // **** 소리에 대한 정보 ****
        // 아직 조사하지 않은 소리가 있는지 여부
        public bool HasSound { get; private set; }
        // 그 소리의 위치
        public Vector3 LastSoundPosition { get; private set; }

        // 다중 추격자 전용 //
        // 추격, 매복 등등의 역할을 나누고, 각 추격자들이 일제히 같은 행동을 하지 않도록 조율
        // <(역할), (추격자 번호)> 타입의 Dictionary로 역할을 현재 수행 중인 추격자를 기록
        private readonly Dictionary<string, int> _roleClaims = new();

        private void Awake()
        {
            // 기존에 있던 BlackBoard Instance 제거
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            if (!PlayerCurrentlyVisible)
                TimeSinceLastSeen += Time.deltaTime;
            TimeInAlertState += Time.deltaTime;

            /// 시간이 지남에 따라 경계 상태를 서서히 품
            /// Alerted → Suspicious
            if (Alert == AlertLevel.Alerted && 
            TimeInAlertState > AlertedToSuspiciousTime)
                SetAlert(AlertLevel.Suspicious);
            /// Suspicious → Calm
            else if (Alert == AlertLevel.Suspicious && 
            TimeInAlertState > SuspiciousToCalmTime)
                SetAlert(AlertLevel.Calm);
        }

        /// 현재 Alert 상태를 바꾸고 TimeInAlertState를 초기화한다.
        private void SetAlert(AlertLevel level)
        {
            if (Alert == level)
                return;
            Alert = level;
            TimeInAlertState = 0f;
        }

        /// 플레이어가 시야에 들어왔을 때
        /// 플레이어의 정보가 업데이트되고, 경계 상태는 무조건 Alerted로 격상된다
        public void ReportSighting(Vector3 playerPosition)
        {
            TimeSinceLastSeen = 0f;
            LastPlayerPosition = playerPosition;
            PlayerCurrentlyVisible = true;
            SetAlert(AlertLevel.Alerted);
        }

        /// 플레이어가 시야에서 사라졌을 때
        /// Visible 정보를 false로 설정한다.
        /// LastPlayerPosition은 지우지 않고 남겨두어
        /// 시야에서 사라져도 잠시동안은 그 주변을 수색하도록 한다.
        public void ReportLostSight() => PlayerCurrentlyVisible = false;

        /// <summary>
        /// Report a heard sound (footstep, generator, etc.) — softer than a
        /// sighting. Sets HasSound so an idle chaser will pick up the
        /// InvestigateSound goal and walk over to check it out.
        /// The caller (detection system) decides whether a given chaser can hear
        /// this sound at all — e.g. a generator sound is reported to chasers in
        /// both dimensions, a footstep only to chasers in the player's dimension.
        /// </summary>
        
        /// 소리가 유발하는 경계 상태를 먼저 본 뒤
        /// 현재 상태보다 높다면 그에 맞게 격상시키고
        /// 소리의 정보가 업데이트된다
        public void ReportSound(Vector3 soundPosition, Sound sound)
        {
            // 더 높은 레벨의 소리를 들었다면 격상시킨다.
            if (sound.Alert > Alert)
                SetAlert(sound.Alert);
            // 같은 레벨의 소리를 들었다면 타이머만 유지한다.
            else if (sound.Alert == Alert)
                TimeInAlertState = 0f;
            // 더 낮은 소리에는 반응하지 않고 아무 행동도 취하지 않는다.
            
            HasSound = true;
            LastSoundPosition = soundPosition;

            /// 벽 너머의 플레이어가 내는 소리, 발전기 소리 등등
            /// 소리는 들리지만 플레이어가 보이지 않는 상황일 때
            if (!PlayerCurrentlyVisible)
                LastPlayerPosition = soundPosition;
        }

        /// 소리에 대한 조사를 끝냈을 때
        public void ClearSound() => HasSound = false;

        /// 다중 추격자 전용 ///
        public bool TryClaimRole(string role, int chaserId)
        {
            // 역할을 선점한 다른 추격자가 이미 존재
            if (_roleClaims.TryGetValue(role, out int owner) && owner != chaserId)
                return false;
            // 이 추격자에게 역할 부여
            _roleClaims[role] = chaserId;
            return true;
        }

        public void ReleaseRole(string role, int chaserId)
        {
            if (_roleClaims.TryGetValue(role, out int owner) && owner == chaserId)
                _roleClaims.Remove(role);
        }
    }
}
