using UnityEngine;

namespace Squad
{
    /// <summary>
    /// 횃불. 플레이어가 들고 있는 동안에는 아무 일도 하지 않고, 던져지면 포물선을 그리며 날아가 땅에 떨어진다.
    /// 땅에 닿으면 몇 번 튕긴 뒤 멈추고, 멈춘 자리에서 미끼 소리(SoundList.Decoy)를 한 번 내서
    /// 들은 적이 조사하러 오게 만든다.
    /// 멈춘 뒤에도 얼마 동안은 그 자리에서 타면서 주변을 비추다가 사라진다.
    /// (손에 들고 있는 횃불은 꺼지지 않는다. 사라지는 것은 던져서 땅에 떨어진 횃불뿐이다)
    ///
    /// 날아가는 동안의 움직임은 물리 엔진에 맡기지 않고 직접 계산한다.
    /// 매 프레임 속도에 중력을 더하고, 이번 프레임에 지나갈 구간에 무엇이 있는지 검사한다.
    ///   - 땅에 닿으면 튕긴다. 튕길 힘이 남지 않았으면 그 자리에 멈춘다
    ///   - 맵 가장자리의 벽에 닿으면 튕겨 나온다
    ///   - 나무는 그냥 지나간다 (나무가 빽빽한 숲에서 던질 때마다 걸리지 않도록)
    ///
    /// 불빛은 같은 오브젝트의 FireSource가 맡는다.
    ///
    /// 씬 구성:
    ///   횃불 프리팹의 가장 위 오브젝트에 FireSource와 함께 붙인다. 횃불 모델은 자식으로 둔다.
    /// </summary>
    public class Torch : MonoBehaviour
    {
        [Header("○ 튜닝 값 — 자유롭게 조절")]
        [Tooltip("던져진 횃불에 작용하는 중력 가속도(음수). 플레이어의 중력보다 일부러 크게 둔다. " +
                 "던지는 속도를 2배로 하면서 중력을 4배로 하면, 날아가는 길은 그대로이고 걸리는 시간만 절반이 된다")]
        [SerializeField] private float gravity = -80f;
        [Tooltip("횃불이 부딪히는 대상의 레이어. 땅(지형)이 속한 레이어가 반드시 들어 있어야 하고, 빠지면 횃불이 땅을 뚫고 떨어진다. " +
                 "나무 기둥의 레이어(Unwalkable)를 넣으면 나무에도 튕긴다")]
        [SerializeField] private LayerMask landMask = (1 << 0) | (1 << 10);   // Default + Floor (땅, 맵 가장자리의 벽)
        [Tooltip("튕기는 정도. 부딪힌 면에서 튀어 오르는 속도가 부딪힌 속도의 몇 배인가. 0이면 튕기지 않는다")]
        [Range(0f, 1f)]
        [SerializeField] private float bounciness = 0.4f;
        [Tooltip("땅에서 튕길 수 있는 최대 횟수")]
        [SerializeField] private int maxBounces = 2;
        [Tooltip("날아가는 동안 도는 빠르기(도/초)")]
        [SerializeField] private float spinSpeed = 1080f;
        [Tooltip("멈춘 뒤 땅에서 띄워 둘 높이(m). 모델이 땅에 묻히면 올린다")]
        [SerializeField] private float restHeight = 0.2f;
        [Tooltip("땅에 멈춘 뒤 사라지기까지의 시간(초). 0이면 사라지지 않는다")]
        [SerializeField] private float lifetimeAfterLanding = 10f;

        // 면이 위를 향하는 정도(normal.y)가 이 값 이상이어야 땅으로 본다. 그보다 가파르면 벽으로 본다.
        private const float GroundNormalY = 0.5f;
        // 튕길 때 면을 따라 미끄러지는 속도가 남는 비율 (땅에 쓸리면서 느려진다)
        private const float SlideKeep = 0.6f;
        // 튀어 오르는 속도가 이 값(m/s)보다 작으면 더 튕기지 않고 멈춘다
        private const float MinBounceSpeed = 4f;

        private bool _flying;
        private Vector3 _velocity;
        private int _bouncesLeft;
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
            _bouncesLeft = maxBounces;
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
                HitSurface(hit);
                return;
            }

            transform.position = to;
            transform.Rotate(Vector3.right, spinSpeed * Time.deltaTime, Space.Self);

            // 맵 밖으로 떨어져 끝없이 내려가는 경우에 대한 방어.
            if (to.y < -100f)
                Destroy(gameObject);
        }

        // 면에 부딪혔을 때. 튕기거나, 땅이고 튕길 힘이 없으면 멈춘다.
        private void HitSurface(RaycastHit hit)
        {
            // 속도를 "면을 뚫고 들어가는 방향"과 "면을 따라 미끄러지는 방향"으로 나눈다.
            Vector3 intoSurface = Vector3.Project(_velocity, hit.normal);
            Vector3 alongSurface = _velocity - intoSurface;
            float bounceSpeed = intoSurface.magnitude * bounciness;

            bool isGround = hit.normal.y >= GroundNormalY;
            if (isGround)
            {
                if (_bouncesLeft <= 0 || bounceSpeed < MinBounceSpeed)
                {
                    Land(hit.point, hit.normal);
                    return;
                }
                _bouncesLeft--;
            }

            // 들어가던 방향은 뒤집어서 줄이고, 미끄러지던 방향은 조금 줄여서 그대로 둔다.
            _velocity = alongSurface * SlideKeep - intoSurface * bounciness;
            // 면에 딱 붙어 있으면 다음 프레임에 같은 면에 또 걸리므로 살짝 떼어 놓는다.
            transform.position = hit.point + hit.normal * 0.05f;
        }

        private void Land(Vector3 point, Vector3 groundNormal)
        {
            _flying = false;

            // 땅의 기울기에 맞춰 눕힌다. 수평으로만 눕히면 비탈에서 한쪽 끝이 땅속에 묻힌다.
            // 횃불의 길이 방향(오브젝트의 위쪽)을, 날아오던 방향을 땅에 눕힌 쪽으로 향하게 한다.
            Vector3 heading = Vector3.ProjectOnPlane(_velocity, groundNormal);
            if (heading.sqrMagnitude < 0.0001f)
                heading = Vector3.ProjectOnPlane(transform.forward, groundNormal);
            if (heading.sqrMagnitude < 0.0001f)
                heading = Vector3.Cross(groundNormal, Vector3.right);

            transform.position = point + groundNormal * restHeight;
            transform.rotation = Quaternion.LookRotation(-groundNormal, heading.normalized);

            // 출처로 자기 자신을 넘긴다. 적이 한 번 조사하고 나면 같은 횃불에는 다시 반응하지 않는다.
            SoundEmitter.Emit(point, SoundList.Decoy, _enemyLayer, _dimension, gameObject);

            // 오브젝트가 사라지면 같이 붙어 있는 불빛(FireSource)과 불꽃도 함께 사라진다.
            if (lifetimeAfterLanding > 0f)
                Destroy(gameObject, lifetimeAfterLanding);
        }
    }
}
