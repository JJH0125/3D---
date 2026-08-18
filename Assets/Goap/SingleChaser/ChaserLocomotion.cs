using UnityEngine;
using Astar3D;
using System.Collections.Generic;

namespace Squad
{
    /// <summary>
    /// Wraps the 3D A* Pathfinder so GOAP actions can just say "move to X"
    /// without knowing how pathfinding works. One of these lives on each chaser.
    ///
    /// This is the bridge between the two layers of A* we talked about:
    ///   - GOAP planner  = A* over WORLD STATES ("what to do")
    ///   - this class    = drives A* over the GRID       ("how to get there")
    ///
    /// It caches the current path and follows it waypoint by waypoint, replanning
    /// the grid path only when the target moves far or the path runs out.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class ChaserLocomotion : MonoBehaviour
    {
        [Header("■ 필수 연결 — 비워두면 에러")]
        [Tooltip("PathFinder")]
        [SerializeField] private Pathfinder pathfinder;

        [Header("○ 튜닝 값 — 자유롭게 조절")]
        [Tooltip("Calm 상태일 때 이동 속도")]
        [SerializeField] private float calmMoveSpeed = 5f;
        [Tooltip("Suspicious 상태일 때 이동 속도")]
        [SerializeField] private float suspiciousMoveSpeed = 8f;
        [Tooltip("Alerted 상태일 때 이동 속도")]
        [SerializeField] private float alertedMoveSpeed = 13f;
        [Tooltip("적이 고개를 돌리는 속도")]

        [SerializeField] private float turnSpeed = 8f;
        [Tooltip("waypoint에 접근하여 다음 waypoint로 이동을 시작하는 거리")]

        [SerializeField] private float waypointTolerance = 0.25f;
        [Tooltip("플레이어가 유의미하게 움직였다 의 기준")]

        [SerializeField] private float repathThreshold = 1.0f;
        [Tooltip("한 번의 Wander을 통해 이동하는 최대 거리")]

        [SerializeField] private float wanderRadius = 6f;

        private Rigidbody _rigidbody;
        private List<Vector3> _path;
        private int _waypointIndex;
        private Vector3 _lastPathTarget;
        private bool _hasWanderTarget;
        private Vector3 _wanderTarget;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            if (pathfinder == null)
                pathfinder = FindObjectOfType<Pathfinder>();
        }

        /// <summary>
        /// 플레이어의 위치와 도착 판정을 측정하는 반경을 받아
        /// 적과 플레이어의 거리가 반경 안에 들어오면 true를 return.
        /// </summary>
        /// <summary>
        /// 현재 경계 단계(alert)에 따라 이동 속도가 달라진다
        /// (Calm/Suspicious/Alerted 순으로 점점 빨라짐).
        /// </summary>
        public bool MoveTo(Vector3 target, float arriveRadius, SquadBlackboard.AlertLevel alert)
        {
            // 도착을 확인하면 true를 return.
            Vector3 flatSelf = Flat(_rigidbody.position);
            if (Vector3.Distance(flatSelf, Flat(target)) <= arriveRadius)
            {
                StopHorizontal();
                return true;
            }

            // path가 비어있거나
            // path를 모두 건너왔거나
            // 플레이어가 유의미하게 움직였다면
            if (_path == null ||
                _waypointIndex >= _path.Count ||
                Vector3.Distance(_lastPathTarget, target) > repathThreshold)
            {
                // pathfinder가 null인 실수가 일어났는지 검사 후,
                // path를 (재)생성
                _path = pathfinder != null ?
                pathfinder.FindPath(_rigidbody.position, target) : null;
                _waypointIndex = 0;
                _lastPathTarget = target;
            }

            FollowPath(MoveSpeedFor(alert));
            return false;
        }

        private float MoveSpeedFor(SquadBlackboard.AlertLevel alert) => alert switch
        {
            SquadBlackboard.AlertLevel.Calm => calmMoveSpeed,
            SquadBlackboard.AlertLevel.Suspicious => suspiciousMoveSpeed,
            _ => alertedMoveSpeed,
        };

        /// <summary>
        /// 제자리에 멈춰 주변을 느린 속도로 돌아보는 함수
        /// </summary>
        public void LookAround()
        {
            StopHorizontal();
            _rigidbody.MoveRotation(_rigidbody.rotation *
                Quaternion.AngleAxis(turnSpeed * 12f * Time.DeltaTime, Vector3.up));
        }

        /// <summary>
        /// 추격자 주변의 무작위 새로운 지점을 생성하여
        /// 추격자가 해당 지점으로 이동하도록 하는 함수.
        /// 배회할 지점이 없으면 새로 좌표를 정해서 return하고,
        /// 이미 있으면 있던 좌표를 return한다
        /// </summary>
        public Vector3 GetWanderTarget()
        {
            if (!_hasWanderTarget)
            {
                /// Random.insideUnitCircle은 반지름이 1인 원 안의
                /// 무작위 점을 뽑아내는 함수
                /// 거기에 wanderRadius를 곱했으니
                /// 반지름이 wanderRadius인 원 안의 무작위 점을 뽑아낸다.
                /// 10번을 시도하고 
                for (int i = 0; i < 10; i++)
                {
                    Vector2 r = Random.insideUnitCircle * wanderRadius;
                    Vector3 candidate = _rigidbody.position + new Vector3(r.x, 0f, r.y);
                    if (pathfinder != null && pathfinder.IsWalkable(candidate))
                    {
                        _wanderTarget = candidate;
                        _hasWanderTarget = true;
                        break;
                    }
                }
            }
            return _wanderTarget;
        }

        /// <summary>
        /// 이동을 마쳤을 경우 호출하여
        /// 새 wanderTarget을 받을 수 있도록 준비한다.
        /// </summary>
        public void ClearWanderTarget() => _hasWanderTarget = false;

        /// <summary>
        /// 적이 교착 상태에 빠지는 것을 막기 위한 방어 코드.
        /// 경로가 없는 노드가 목적지로 정해지는 경우를 탐지한다.
        /// </summary>
        public bool HasValidPath => _path != null && _path.Count > 0;

        /// <summary>
        /// MoveTo에서 최종 호출하는, 한 프레임씩 움직이는 작업을 담은 함수
        /// </summary>
        private void FollowPath(float speed)
        {
            if (_path == null || _waypointIndex >= _path.Count)
                return;

            Vector3 flatSelf = Flat(_rigidbody.position); // 내 위치의 평면 버전
            Vector3 wp = _path[_waypointIndex];

            // 웨이포인트 도달 체크
            if (Vector3.Distance(flatSelf, Flat(wp)) <= waypointTolerance)
            {
                _waypointIndex++;
                if (_waypointIndex >= _path.Count)
                    return;
                wp = _path[_waypointIndex];
            }

            Vector3 dir = (Flat(wp) - flatSelf).normalized; // 평면 상에서 방향 계산
            Vector3 step = dir * speed * Time.fixedDeltaTime;
            Vector3 next = _rigidbody.position + step;      // 실제 위치 + 이동
            next.y = _rigidbody.position.y;                 // 물리 작용 등등을 거쳐 y 성분이 미세하게
                                                            // 변할 수 있기 때문에 y를 고정하는 작업을
                                                            // 거친다
            _rigidbody.MovePosition(next);

            // 유의미한 이동을 한다면
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
                _rigidbody.MoveRotation(Quaternion.Slerp(
                    _rigidbody.rotation, look, turnSpeed * Time.fixedDeltaTime));
            }
        }

        /// <summary>
        /// x와 z축의 속도를 0으로 만들어 맵에서 정지시키는 함수
        /// </summary>
        private void StopHorizontal()
        {
            Vector3 v = _rigidbody.velocity;
            _rigidbody.velocity = new Vector3(0f, v.y, 0f);
        }

        /// <summary>
        /// xyz 위치를 xz 평면 상의 위치로 치환하는 함수
        /// </summary>
        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
