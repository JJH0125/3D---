using System.Collections.Generic;
using UnityEngine;

// !!!길찾기 GOAP가 정상적으로 작동하는지 테스트하기 위한 추격자 이동 코드 !!!
// !!!실제 게임에서 사용하는 코드가 아님                                 !!!
namespace Astar3D
{
    // 이 스크립트를 오브젝트에 추가하면
    // Rigidbody가 자동으로 추가된다
    // Rigidbody가 있어야 하는 PathFinder에 있어서 에러 방지 역할을 함.
    [RequireComponent(typeof(Rigidbody))]
    public class ChaserAgent : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform target;
        [SerializeField] private Pathfinder pathfinder;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float waypointTolerance = 0.15f;
        [SerializeField] private float turnSpeed = 10f;

        [Header("Replanning")]
        // 경로 재계산 시간 간격
        [SerializeField] private float replanInterval = 0.25f;
        // 경로 재계산 조건 측정
        [SerializeField] private float targetMoveThreshold = 0.75f;

        private Rigidbody _rigidbody;
        private List<Vector3> _path;
        // 적이 경로의 몇번째 노드를 지나고 있는가?
        private int _waypointIndex;
        private float _replanTimer;
        private Vector3 _lastTargetPos;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            if (pathfinder == null)
                pathfinder = FindObjectOfType<Pathfinder>();
        }

        private void Update()
        {
            if (target == null)
                return;

            // 다음 경로 계산까지의 시간에서 매 프레임마다 1/60초씩 차감
            _replanTimer -= Time.deltaTime;
            // 재계산 조건 : 한 프레임동안 플레이어의 위치에
            //               유의미한 변화가 있을 만큼
            //               플레이어가 충분히 움직였는가
            // 계산을 제곱끼리 하여 시간이 오래 걸리는
            // 제곱근 계산을 피하기
            bool targetMovedFar = 
            (target.position - _lastTargetPos).sqrMagnitude
                                  > targetMoveThreshold * targetMoveThreshold;

            // 
            if (_replanTimer <= 0f || targetMovedFar || _path == null)
            {
                RequestPath();
                _replanTimer = replanInterval;
            }
        }

        // 프레임마다 호출되는 Update와는 달리,
        // FixedUpdate는 고정된 시간 간격마다 호출됨
        private void FixedUpdate() => FollowPath();

        private void RequestPath()
        {
            // 경로 산출
            _path = pathfinder.FindPath(transform.position, 
            target.position);
            // 노드 인덱스값 초기화
            _waypointIndex = 0;
            // 플레이어의 위치 업데이트
            _lastTargetPos = target.position;
        }

        // 실제로 이동하는 기능의 함수
        private void FollowPath()
        {
            // 이동을 멈추는 조건 :
            // 길이 없거나 경로를 모두 지났을 때
            if (_path == null || _waypointIndex >= _path.Count)
            {
                _rigidbody.velocity =
                new Vector3(0, _rigidbody.velocity.y, 0);
                return;
            }

            Vector3 current = _rigidbody.position;
            Vector3 wp = _path[_waypointIndex];

            // Compare on XZ only — ignore height difference.
            Vector3 flatCurrent = new Vector3(current.x, 0, current.z);
            Vector3 flatWp = new Vector3(wp.x, 0, wp.z);

            if (Vector3.Distance(flatCurrent, flatWp) 
            <= waypointTolerance)
            {
                _waypointIndex++;
                if (_waypointIndex >= _path.Count)
                {
                    _rigidbody.velocity = 
                    new Vector3(0, _rigidbody.velocity.y, 0);
                    return;
                }
                wp = _path[_waypointIndex];
                flatWp = new Vector3(wp.x, 0, wp.z);
            }

            // 벡터 정규화를 통해
            // 목표가 가깝든 멀든 일정한 속도로 움직임
            Vector3 dir = (flatWp - flatCurrent).normalized;
            // y 속도 보존, 나머지 업데이트하여 이동
            _rigidbody.velocity = new Vector3(dir.x * moveSpeed, _rigidbody.velocity.y, dir.z * moveSpeed);

            // 이동 중이라면
            if (dir.sqrMagnitude > 0.001f)
            {
                // 이동하는 방향을 바라본다
                // (방향을 돌리는 기준은 up벡터 즉 y축)
                Quaternion look = 
                Quaternion.LookRotation(dir, Vector3.up);
                _rigidbody.rotation = 
                Quaternion.Slerp(_rigidbody.rotation, look, 
                turnSpeed * Time.fixedDeltaTime);
            }
        }
    }
}
