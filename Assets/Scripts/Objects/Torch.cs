using UnityEngine;

namespace Squad
{
    /// <summary>
    /// 횃불. 플레이어가 들고 있는 동안에는 아무 일도 하지 않고, 던져지면 포물선을 그리며 날아가 땅에 떨어진다.
    /// 떨어진 자리에서 미끼 소리(SoundList.Decoy)를 한 번 내서, 들은 적이 조사하러 오게 만든다.
    /// 떨어진 뒤에도 얼마 동안은 그 자리에서 타면서 주변을 비추다가 사라진다.
    /// (손에 들고 있는 횃불은 꺼지지 않는다. 사라지는 것은 던져서 땅에 떨어진 횃불뿐이다)
    ///
    /// 날아가는 동안의 움직임은 물리 엔진에 맡기지 않고 직접 계산한다.
    /// 매 프레임 속도에 중력을 더하고, 이번 프레임에 지나갈 구간에 무엇이 있는지 검사한다.
    ///   - 위를 향한 면(땅)에 닿으면 그 자리에 떨어진다
    ///   - 서 있는 면(나무 기둥, 벽)에 닿으면 힘을 잃고 튕겨 나온다
    ///
    /// 불빛은 같은 오브젝트의 FireSource가 맡는다.
    ///
    /// 씬 구성:
    ///   횃불 프리팹의 가장 위 오브젝트에 FireSource와 함께 붙인다. 횃불 모델은 자식으로 둔다.
    /// </summary>
    public class Torch : MonoBehaviour
    {
        [Header("○ 튜닝 값 — 자유롭게 조절")]
        [Tooltip("던져진 횃불에 작용하는 중력 가속도(음수)")]
        [SerializeField] private float gravity = -20f;
        [Tooltip("횃불이 부딪히는 대상의 레이어 (땅, 나무 기둥, 벽)")]
        [SerializeField] private LayerMask landMask = (1 << 0) | (1 << 3);   // Default + Unwalkable
        [Tooltip("날아가는 동안 도는 빠르기(도/초)")]
        [SerializeField] private float spinSpeed = 540f;
        [Tooltip("떨어진 뒤 땅에서 띄워 둘 높이(m). 모델이 땅에 묻히면 올린다")]
        [SerializeField] private float restHeight = 0.2f;
        [Tooltip("땅에 떨어진 뒤 사라지기까지의 시간(초). 0이면 사라지지 않는다")]
        [SerializeField] private float lifetimeAfterLanding = 10f;

        // 면이 위를 향하는 정도(normal.y)가 이 값 이상이어야 땅으로 본다. 그보다 가파르면 벽으로 보고 튕긴다.
        private const float GroundNormalY = 0.5f;
        // 벽에 튕긴 뒤 남는 속도의 비율
        private const float BounceKeep = 0.3f;

        private bool _flying;
        private Vector3 _velocity;
        // 던진 순간 플레이어가 있던 차원. 미끼 소리는 이 차원의 적에게만 들린다.
        private Dimension _dimension;
        private LayerMask _enemyLayer;

        /// <summary>
        /// 횃불을 velocity의 속도로 던진다. 부르기 전에 플레이어의 손에서 떼어 놓아야 한다.
        /// </summary>
        public void Throw(Vector3 velocity, Dimension dimension, LayerMask enemyLayer)
        {
            _velocity = velocity;
            _dimension = dimension;
            _enemyLayer = enemyLayer;
            _flying = true;
        }

        private void Update()
        {
            if (!_flying)
                return;

            _velocity.y += gravity * Time.deltaTime;

            Vector3 from = transform.position;
            Vector3 to = from + _velocity * Time.deltaTime;

            // 이번 프레임에 지나갈 구간에 걸리는 것이 있는가
            if (Physics.Linecast(from, to, out RaycastHit hit, landMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.normal.y >= GroundNormalY)
                {
                    Land(hit.point);
                    return;
                }

                // 벽이나 기둥: 부딪힌 면을 기준으로 속도를 반사시키고 힘을 줄인다.
                _velocity = Vector3.Reflect(_velocity, hit.normal) * BounceKeep;
                // 면에 딱 붙어 있으면 다음 프레임에 같은 면에 또 걸리므로 살짝 떼어 놓는다.
                transform.position = hit.point + hit.normal * 0.05f;
                return;
            }

            transform.position = to;
            transform.Rotate(Vector3.right, spinSpeed * Time.deltaTime, Space.Self);

            // 맵 밖으로 떨어져 끝없이 내려가는 경우에 대한 방어.
            if (to.y < -100f)
                Destroy(gameObject);
        }

        private void Land(Vector3 point)
        {
            _flying = false;

            // 바닥에 눕힌다. 위에서 봤을 때의 방향은 날아오던 방향을 그대로 둔다.
            transform.position = point + Vector3.up * restHeight;
            transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);

            // 출처로 자기 자신을 넘긴다. 적이 한 번 조사하고 나면 같은 횃불에는 다시 반응하지 않는다.
            SoundEmitter.Emit(point, SoundList.Decoy, _enemyLayer, _dimension, gameObject);

            // 오브젝트가 사라지면 같이 붙어 있는 불빛(FireSource)과 불꽃도 함께 사라진다.
            if (lifetimeAfterLanding > 0f)
                Destroy(gameObject, lifetimeAfterLanding);
        }
    }
}
