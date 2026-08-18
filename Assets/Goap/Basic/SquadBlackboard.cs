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

        public enum AlertLevel { Calm, Suspicious, Alerted }

        // 공유되는 정보 //

        // 적의 경계 상태
        public AlertLevel Alert { get; private set; } = AlertLevel.Calm;
        // 플레이어가 지금 보이는지 여부
        public bool PlayerCurrentlyVisible { get; private set; }
        // 플레이어가 마지막까지 있었던 위치
        public Vector3 LastPlayerPosition { get; private set; }
        // 플레이어가 마지막으로 보인 뒤 지난 시간
        public float TimeSinceLastSeen { get; private set; } = Mathf.Infinity;
        // 현재 경계 상태에 머문 시간.
        // TimeSinceLastSeen과 달리 상태가 바뀔 때마다(SetAlert) 0으로 리셋된다.
        private float _timeInAlertState;

        // --- Sound facts (for the InvestigateSound goal) ---
        // HasSound is true while there's an un-investigated sound to check out.
        // LastSoundPosition is where it came from. A chaser walks there (MoveToSound),
        // searches (SearchSoundArea), then calls ClearSound() so it isn't chased
        // forever. Generator sounds and footsteps both land here; the difference
        // (footsteps stay in one dimension, generator sound crosses) is enforced
        // by WHO calls ReportSound — see the detection system, not here.
        public bool HasSound { get; private set; }
        public Vector3 LastSoundPosition { get; private set; }

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
            _timeInAlertState += Time.deltaTime;

            // 시간이 지남에 따라 경계 상태를 서서히 품
            if (Alert == AlertLevel.Alerted && 
            _timeInAlertState > AlertedToSuspiciousTime)
                SetAlert(AlertLevel.Suspicious);

            else if (Alert == AlertLevel.Suspicious && 
            _timeInAlertState > SuspiciousToCalmTime)
                SetAlert(AlertLevel.Calm);
        }

        // Alert를 바꾸는 모든 경로가 거쳐가는 통로.
        // 상태가 바뀔 때만 _timeInAlertState를 리셋해서
        // "이 상태에 얼마나 머물렀는지"를 정확히 잰다.
        private void SetAlert(AlertLevel level)
        {
            if (Alert == level)
                return;
            Alert = level;
            _timeInAlertState = 0f;
        }

        /// <summary>플레이어가 시야에 들어왔을 때</summary>
        public void ReportSighting(Vector3 playerPos)
        {
            LastPlayerPosition = playerPos;
            PlayerCurrentlyVisible = true;
            TimeSinceLastSeen = 0f;
            SetAlert(AlertLevel.Alerted);
        }

        /// <summary>플레이어가 시야에서 사라졌을 때</summary>
        public void ReportLostSight()
        {
            PlayerCurrentlyVisible = false;
            // LastPlayerPosition은 지우지 않고 남겨두어
            // 시야에서 사라져도 잠시동안은 주변을 수색하도록 한다.
        }

        /// <summary>
        /// Report a heard sound (footstep, generator, etc.) — softer than a
        /// sighting. Sets HasSound so an idle chaser will pick up the
        /// InvestigateSound goal and walk over to check it out.
        /// The caller (detection system) decides whether a given chaser can hear
        /// this sound at all — e.g. a generator sound is reported to chasers in
        /// both dimensions, a footstep only to chasers in the player's dimension.
        /// </summary>
        public void ReportSound(Vector3 soundPos, Sound sound)
        {
            // 소리가 유발하는 경계가 현재 경계보다 높다면 경계를 격상
            if (sound.Alert > Alert)
                SetAlert(sound.Alert);
            HasSound = true;
            LastSoundPosition = soundPos;
            // 소리가 들렸지만 플레이어는 보이지 않을 때
            // 소리가 들린 위치를 플레이어가 2초 전(조금 전) 있었을 위치라고 추정
            if (!PlayerCurrentlyVisible)
            {
                LastPlayerPosition = soundPos;
                TimeSinceLastSeen = Mathf.Min(TimeSinceLastSeen, 2f);
            }
        }

        /// <summary>소리에 대한 조사를 끝냈을 때</summary>
        public void ClearSound() => HasSound = false;

        // --- Role arbitration ---
        // A chaser tries to claim a role; returns true if it got it.
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
