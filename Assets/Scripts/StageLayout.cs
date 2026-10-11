using System.Collections.Generic;
using Astar3D;
using UnityEngine;

namespace Squad
{
    /// <summary>
    /// 라운드가 시작될 때(Stage가 생성될 때) Stage 안의 오브젝트들을 지형 위에 배치한다.
    ///
    /// 고정 배치:
    ///   출구      항상 같은 자리
    ///   플레이어  출구 앞 (출구에서 지형 한가운데 쪽으로 조금 나온 자리)
    ///
    /// 무작위 배치 (라운드마다 달라진다):
    ///   발전기  대체로 봉우리 너머, 플레이어의 반대쪽에 서로 떨어뜨려 흩뿌린다
    ///   포탈    플레이어에게서 어느 정도 떨어진 아무 곳
    ///   적      플레이어에게서 적어도 봉우리의 지름만큼 떨어진 곳
    ///   모닥불  맵 곳곳에, 서로 떨어뜨려 놓는다 (길잡이 역할)
    ///
    /// 무작위 자리는 "뽑아 보고, 조건에 안 맞으면 다시 뽑는" 방식으로 정한다.
    /// 모든 무작위 자리에 공통으로 적용되는 조건:
    ///   - 지형의 가장자리에서 떨어져 있다
    ///   - 땅이 너무 가파르지 않다
    ///   - 길찾기 격자에서 걸을 수 있는 칸이다 (나무 기둥 안이 아니다)
    ///   - 먼저 놓인 오브젝트와 너무 가깝지 않다
    ///
    /// 높이는 프리팹에서 바닥 위로 띄워 둔 만큼을 그대로 지켜서 지형 위에 올린다.
    /// 그래서 프리팹에서 맞춰 둔 "발전기가 바닥에 닿는 높이" 같은 것이 깨지지 않는다.
    ///
    /// 발전기, 포탈, 적의 개수는 정하지 않는다. Stage 프리팹에 들어 있는 것을 모두 찾아서 옮긴다.
    /// 모닥불만은 프리팹 하나를 받아 정해진 개수만큼 여기서 만든다. (여러 개를 프리팹에 일일이 넣지 않도록)
    /// 씬에 지형(Terrain)이 없으면 아무것도 하지 않으므로, 프리팹의 배치가 그대로 쓰인다.
    ///
    /// 다른 스크립트의 Awake보다 먼저 실행된다.
    /// 적의 ChaserLocomotion이 Awake에서 "지금 서 있는 자리"의 땅 높이로 올라서기 때문에,
    /// 그 전에 자리를 옮겨 두어야 한다.
    ///
    /// 씬 구성:
    ///   Stage 프리팹의 가장 위 오브젝트에 붙인다.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class StageLayout : MonoBehaviour
    {
        [Header("○ 지형 — Forest Map Tool에 넣은 값과 같게")]
        [Tooltip("봉우리의 반경(m). 적은 플레이어에게서 이 값의 2배(지름) 이상 떨어져 생성된다")]
        [SerializeField] private float hillRadius = 40f;
        [Tooltip("프리팹에서 바닥으로 삼았던 높이. 각 오브젝트가 이 높이보다 위에 있는 만큼 지형 위로 띄운다")]
        [SerializeField] private float prefabGroundHeight = 0.89f;

        [Header("○ 고정 배치")]
        [Tooltip("출구의 자리. 지형 한가운데를 (0, 0)으로 본 (x, z)")]
        [SerializeField] private Vector2 exitPosition = new Vector2(0f, -84f);
        [Tooltip("플레이어가 출구에서 지형 한가운데 쪽으로 얼마나 나와서 시작할지(m)")]
        [SerializeField] private float playerDistanceFromExit = 25f;

        [Header("○ 무작위 배치")]
        [Tooltip("지형 가장자리에서 이만큼(m)은 비워 둔다")]
        [SerializeField] private float edgeMargin = 10f;
        [Tooltip("땅의 기울기가 이 각도(도)보다 가파른 곳에는 놓지 않는다")]
        [SerializeField] private float maxSlope = 25f;
        [Tooltip("발전기를 뿌릴 부채꼴의 절반 각도(도). 플레이어의 정반대 방향을 기준으로 좌우로 이만큼 벌어진다")]
        [Range(10f, 180f)]
        [SerializeField] private float generatorSpreadAngle = 80f;
        [Tooltip("발전기가 지형 한가운데에서 적어도 이만큼(m)은 떨어지게 한다")]
        [SerializeField] private float generatorMinRadius = 30f;
        [Tooltip("발전기끼리 적어도 이만큼(m)은 떨어지게 한다")]
        [SerializeField] private float generatorSpacing = 35f;
        [Tooltip("포탈이 플레이어에게서 적어도 이만큼(m)은 떨어지게 한다")]
        [SerializeField] private float portalMinPlayerDistance = 30f;
        [Tooltip("포탈, 적, 모닥불이 먼저 놓인 오브젝트에서 적어도 이만큼(m)은 떨어지게 한다")]
        [SerializeField] private float objectSpacing = 10f;

        [Header("○ 모닥불")]
        [Tooltip("모닥불 프리팹. 비워두면 모닥불을 만들지 않는다")]
        [SerializeField] private GameObject campfirePrefab;
        [Tooltip("라운드마다 만들 모닥불의 개수")]
        [SerializeField] private int campfireCount = 6;
        [Tooltip("모닥불끼리 적어도 이만큼(m)은 떨어지게 한다")]
        [SerializeField] private float campfireSpacing = 40f;

        // 조건에 맞는 자리가 나올 때까지 다시 뽑는 최대 횟수
        private const int MaxTries = 300;

        private Terrain _terrain;
        private Pathfinder _pathfinder;
        // 지형 한가운데의 월드 좌표 (높이는 쓰지 않는다)
        private Vector3 _center;
        // 가운데에서 가장자리 여백을 뺀 곳까지의 거리
        private float _halfExtent;
        // 이미 오브젝트를 놓은 자리들
        private readonly List<Vector3> _placed = new();

        private void Awake()
        {
            _terrain = Terrain.activeTerrain;
            if (_terrain == null)
                return;

            _pathfinder = FindObjectOfType<Pathfinder>();

            Vector3 size = _terrain.terrainData.size;
            _center = _terrain.transform.position + new Vector3(size.x * 0.5f, 0f, size.z * 0.5f);
            _halfExtent = Mathf.Min(size.x, size.z) * 0.5f - edgeMargin;

            Vector3 playerSpot = PlaceExitAndPlayer();
            PlaceGenerators(playerSpot);
            PlacePortals(playerSpot);
            PlaceEnemies(playerSpot);
            // 모닥불은 자리가 조금 달라져도 상관없으므로, 중요한 것들을 먼저 놓은 뒤 남은 자리에 놓는다.
            PlaceCampfires();
        }

        // ── 고정 배치 ────────────────────────────────────────────────

        /// <summary>
        /// terrain 위에서 출구와 플레이어가 놓일 자리(높이는 0)를 계산한다.
        /// 나무를 심는 에디터 도구도 이 자리를 비워 두어야 하므로, 계산을 한 곳에 모아 밖에서도 쓸 수 있게 했다.
        /// </summary>
        public void GetFixedSpots(Terrain terrain, out Vector3 exitSpot, out Vector3 playerSpot)
        {
            Vector3 size = terrain.terrainData.size;
            Vector3 center = terrain.transform.position + new Vector3(size.x * 0.5f, 0f, size.z * 0.5f);

            exitSpot = center + new Vector3(exitPosition.x, 0f, exitPosition.y);

            // 출구에서 지형 한가운데를 바라보는 방향으로 조금 나온 곳이 "출구 앞"이다.
            Vector3 toCenter = center - exitSpot;
            if (toCenter.sqrMagnitude < 0.0001f)
                toCenter = Vector3.forward;
            playerSpot = exitSpot + toCenter.normalized * playerDistanceFromExit;
        }

        // 출구와 플레이어를 놓고, 플레이어의 자리를 돌려준다.
        private Vector3 PlaceExitAndPlayer()
        {
            GetFixedSpots(_terrain, out Vector3 exitSpot, out Vector3 playerSpot);

            Exit exit = GetComponentInChildren<Exit>();
            if (exit != null)
                Place(exit.transform, exitSpot);

            Player player = GetComponentInChildren<Player>();
            if (player != null)
            {
                // CharacterController는 켜져 있는 동안 위치를 직접 바꾸면 무시할 수 있어서, 잠깐 끄고 옮긴다.
                CharacterController controller = player.GetComponent<CharacterController>();
                if (controller != null)
                    controller.enabled = false;

                Place(player.transform, playerSpot);

                if (controller != null)
                    controller.enabled = true;
            }

            return playerSpot;
        }

        // ── 무작위 배치 ──────────────────────────────────────────────

        private void PlaceGenerators(Vector3 playerSpot)
        {
            // 지형 한가운데에서 봤을 때 플레이어의 정반대 방향
            Vector3 away = _center - playerSpot;
            float awayAngle = Mathf.Atan2(away.z, away.x) * Mathf.Rad2Deg;

            foreach (Generator generator in GetComponentsInChildren<Generator>())
            {
                Vector3 spot = _center;
                bool found = false;

                for (int i = 0; i < MaxTries && !found; i++)
                {
                    float angle = (awayAngle + Random.Range(-generatorSpreadAngle, generatorSpreadAngle))
                                  * Mathf.Deg2Rad;
                    // 거리를 그냥 고르게 뽑으면 가운데 쪽에 몰린다. (바깥쪽 고리일수록 넓으므로)
                    // 거리의 제곱을 고르게 뽑아야 넓이 기준으로 고르게 퍼진다.
                    float radius = Mathf.Sqrt(Random.Range(
                        generatorMinRadius * generatorMinRadius, _halfExtent * _halfExtent));

                    spot = _center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                    found = IsUsable(spot) && IsFarFromPlaced(spot, generatorSpacing);
                }

                PlaceRandom(generator.transform, spot, found);
            }
        }

        private void PlacePortals(Vector3 playerSpot)
        {
            foreach (Portal portal in GetComponentsInChildren<Portal>())
                PlaceAwayFromPlayer(portal.transform, playerSpot, portalMinPlayerDistance);
        }

        private void PlaceEnemies(Vector3 playerSpot)
        {
            // 규칙: 적은 플레이어에게서 적어도 봉우리의 지름만큼 떨어진 곳에서 시작한다.
            float minDistance = hillRadius * 2f;

            foreach (HorrorChaserAgent enemy in GetComponentsInChildren<HorrorChaserAgent>())
                PlaceAwayFromPlayer(enemy.transform, playerSpot, minDistance);
        }

        // 모닥불을 개수만큼 만들어, 지형 전체에 서로 떨어뜨려 놓는다.
        private void PlaceCampfires()
        {
            if (campfirePrefab == null)
                return;

            var campfireSpots = new List<Vector3>();

            for (int n = 0; n < campfireCount; n++)
            {
                Vector3 spot = _center;
                bool found = false;

                for (int i = 0; i < MaxTries && !found; i++)
                {
                    spot = _center + new Vector3(
                        Random.Range(-_halfExtent, _halfExtent), 0f, Random.Range(-_halfExtent, _halfExtent));

                    found = IsUsable(spot) && IsFarFromPlaced(spot, objectSpacing);
                    foreach (Vector3 other in campfireSpots)
                        if (FlatDistance(spot, other) < campfireSpacing)
                            found = false;
                }

                // 자리가 없으면 억지로 놓지 않고 이번 것은 건너뛴다. 모닥불은 하나쯤 적어도 게임이 진행된다.
                if (!found)
                    continue;

                campfireSpots.Add(spot);
                // 모닥불 프리팹은 땅에 닿는 높이를 기준으로 만들어져 있으므로 띄우지 않고 그대로 땅에 놓는다.
                GameObject campfire = Instantiate(campfirePrefab, transform);
                PlaceOnGround(campfire.transform, spot, 0f);
            }
        }

        // 지형 전체에서 고르게 뽑되, 플레이어에게서 minPlayerDistance 이상 떨어진 자리에 놓는다.
        private void PlaceAwayFromPlayer(Transform target, Vector3 playerSpot, float minPlayerDistance)
        {
            Vector3 spot = _center;
            bool found = false;

            for (int i = 0; i < MaxTries && !found; i++)
            {
                spot = _center + new Vector3(
                    Random.Range(-_halfExtent, _halfExtent), 0f, Random.Range(-_halfExtent, _halfExtent));

                found = IsUsable(spot)
                        && FlatDistance(spot, playerSpot) >= minPlayerDistance
                        && IsFarFromPlaced(spot, objectSpacing);
            }

            PlaceRandom(target, spot, found);
        }

        // 무작위로 뽑은 자리에 놓는다. 끝내 조건에 맞는 자리를 못 찾았으면 마지막 후보에 놓고 알린다.
        private void PlaceRandom(Transform target, Vector3 spot, bool found)
        {
            if (!found)
                Debug.LogWarning(
                    $"[StageLayout] {target.name}: 조건에 맞는 자리를 찾지 못해 마지막 후보에 놓았습니다. " +
                    "거리 조건이 지형 크기에 비해 너무 크지 않은지 확인하세요.", target);

            Place(target, spot);
        }

        // ── 자리 검사 ────────────────────────────────────────────────

        // 무작위 자리가 공통 조건(가장자리, 기울기, 걸을 수 있는 칸)에 맞는가
        private bool IsUsable(Vector3 spot)
        {
            Vector3 fromCenter = spot - _center;
            if (Mathf.Abs(fromCenter.x) > _halfExtent || Mathf.Abs(fromCenter.z) > _halfExtent)
                return false;

            // GetSteepness는 지형의 한쪽 끝을 0, 반대쪽 끝을 1로 본 비율 좌표를 받는다.
            Vector3 size = _terrain.terrainData.size;
            Vector3 local = spot - _terrain.transform.position;
            if (_terrain.terrainData.GetSteepness(local.x / size.x, local.z / size.z) > maxSlope)
                return false;

            return _pathfinder == null || _pathfinder.IsWalkable(spot);
        }

        private bool IsFarFromPlaced(Vector3 spot, float minDistance)
        {
            foreach (Vector3 placed in _placed)
                if (FlatDistance(spot, placed) < minDistance)
                    return false;

            return true;
        }

        // 높이를 뺀, 위에서 내려다본 거리
        private static float FlatDistance(Vector3 a, Vector3 b)
            => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        // ── 놓기 ─────────────────────────────────────────────────────

        // target을 spot의 xz 자리로 옮기고, 프리팹에서 바닥 위로 띄워 둔 만큼 지형 위에 올린다.
        private void Place(Transform target, Vector3 spot)
            => PlaceOnGround(target, spot, target.localPosition.y - prefabGroundHeight);

        // target을 spot의 xz 자리로 옮기고, 그 자리의 땅에서 heightAboveGround만큼 위에 올린다.
        private void PlaceOnGround(Transform target, Vector3 spot, float heightAboveGround)
        {
            // SampleHeight는 지형 오브젝트의 위치를 뺀 높이를 주므로 다시 더한다.
            spot.y = _terrain.SampleHeight(spot) + _terrain.transform.position.y + heightAboveGround;
            target.position = spot;

            _placed.Add(spot);
        }
    }
}
