using System.Collections.Generic;
using UnityEngine;

namespace Squad
{
    /// <summary>
    /// 디버그용. 플레이 중에 F1을 누르면 모든 적을 플레이어 앞에 나란히 세워서 겉모습을 비교할 수 있게 한다.
    /// 다시 누르면 적들이 원래대로 움직이기 시작한다.
    ///
    /// 비교하는 동안에는
    ///   - 적의 AI와 시야 감지를 꺼서, 움직이지 않고 플레이어를 발견하지도 않는다
    ///   - 차원과 상관없이 모든 적이 보인다 (평소에는 플레이어와 같은 차원의 적만 보인다)
    ///   - 위에서 흰 조명을 비춰서, 어두운 맵에서도 색이 제대로 보인다
    ///
    /// 씬이나 프리팹에 붙일 필요가 없다. 게임이 시작될 때 스스로 만들어진다.
    /// 필요 없어지면 이 파일만 지우면 된다.
    /// </summary>
    public class EnemyCompareDebug : MonoBehaviour
    {
        // 비교를 켜고 끄는 키
        private const KeyCode ToggleKey = KeyCode.F1;
        // 플레이어에서 적들을 세울 줄까지의 거리(m)
        private const float DistanceFromPlayer = 14f;
        // 적과 적 사이의 간격(m). 팔을 벌린 폭이 9m쯤 되므로 겹치지 않게 넉넉히 둔다.
        private const float Spacing = 12f;
        // 적 오브젝트를 땅에서 띄울 높이(m). ChaserLocomotion의 heightAboveGround와 같은 값.
        private const float HeightAboveGround = 1f;

        private bool _comparing;
        private readonly List<HorrorChaserAgent> _enemies = new();
        private GameObject _lightObject;

        // 게임이 시작되고 첫 씬이 열린 직후에 Unity가 한 번 불러 준다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var debugObject = new GameObject("Enemy Compare Debug");
            debugObject.AddComponent<EnemyCompareDebug>();
            // Stage가 라운드마다 바뀌어도 이 오브젝트는 남아 있어야 한다.
            DontDestroyOnLoad(debugObject);
        }

        private void Update()
        {
            if (!Input.GetKeyDown(ToggleKey))
                return;

            if (_comparing)
                StopComparing();
            else
                StartComparing();
        }

        // 차원을 넘나들면 DimensionController가 다른 차원의 적을 다시 숨긴다.
        // 비교하는 동안에는 그 뒤에 매번 다시 켜서, 어느 차원에서 보든 모두 보이게 한다.
        private void LateUpdate()
        {
            if (!_comparing)
                return;

            foreach (HorrorChaserAgent enemy in _enemies)
                if (enemy != null && enemy.TryGetComponent(out DimensionMember member))
                    member.SetRenderer(true);
        }

        private void StartComparing()
        {
            Player player = FindObjectOfType<Player>();
            if (player == null)
            {
                Debug.Log("[EnemyCompareDebug] 플레이 중인 라운드가 없어서 비교할 수 없습니다.");
                return;
            }

            _enemies.Clear();
            _enemies.AddRange(FindObjectsOfType<HorrorChaserAgent>());
            // 누를 때마다 순서가 바뀌지 않도록 이름순으로 세운다.
            _enemies.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            if (_enemies.Count == 0)
                return;

            // 플레이어가 보는 방향으로 조금 떨어진 곳에, 플레이어를 마주 보는 가로줄로 세운다.
            Vector3 forward = player.transform.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 rowCenter = player.transform.position + forward * DistanceFromPlayer;

            for (int i = 0; i < _enemies.Count; i++)
            {
                HorrorChaserAgent enemy = _enemies[i];

                // 움직이지도, 플레이어를 발견하지도 않게 한다.
                enemy.enabled = false;
                if (enemy.TryGetComponent(out VisionCensor vision))
                    vision.enabled = false;

                // 가운데를 기준으로 좌우로 벌린다. (2마리면 -0.5칸, +0.5칸)
                float offset = (i - (_enemies.Count - 1) * 0.5f) * Spacing;
                Vector3 spot = rowCenter + right * offset;
                spot.y = GroundHeightAt(spot, enemy.transform.position.y);

                enemy.transform.SetPositionAndRotation(spot, Quaternion.LookRotation(-forward, Vector3.up));
            }

            // 색을 제대로 비교할 수 있도록 줄 한가운데 위에서 흰 빛을 비춘다.
            _lightObject = new GameObject("Enemy Compare Light");
            _lightObject.transform.position = rowCenter + Vector3.up * 16f - forward * 8f;
            Light light = _lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Color.white;
            light.intensity = 2.5f;
            light.range = 60f;

            _comparing = true;
            Debug.Log("[EnemyCompareDebug] 비교 시작. 왼쪽부터: " +
                      string.Join(", ", _enemies.ConvertAll(e => e.name)) + ". F1을 다시 누르면 끝납니다.");
        }

        private void StopComparing()
        {
            _comparing = false;

            foreach (HorrorChaserAgent enemy in _enemies)
            {
                // 그 사이에 라운드가 끝나서 적이 사라졌을 수 있다.
                if (enemy == null)
                    continue;

                enemy.enabled = true;
                if (enemy.TryGetComponent(out VisionCensor vision))
                    vision.enabled = true;

                // 보이고 숨는 것을 원래 규칙(플레이어와 같은 차원의 적만 보인다)으로 되돌린다.
                if (DimensionController.Instance != null && enemy.TryGetComponent(out DimensionMember member))
                    member.SetRenderer(DimensionController.Instance.CompareDimension(member));
            }
            _enemies.Clear();

            if (_lightObject != null)
                Destroy(_lightObject);

            Debug.Log("[EnemyCompareDebug] 비교 끝.");
        }

        // spot 자리에서 적이 서야 할 높이. 지형이 없으면 지금 높이를 그대로 쓴다.
        private static float GroundHeightAt(Vector3 spot, float fallback)
        {
            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null)
                return fallback;

            return terrain.SampleHeight(spot) + terrain.transform.position.y + HeightAboveGround;
        }
    }
}
