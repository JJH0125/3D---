using UnityEngine;

namespace Squad
{
    /// <summary>
    /// 게임에 쓰이는 모든 소리의 값을 한 곳에 모아 두는 에셋.
    ///
    /// 만드는 법: Project 창에서 Assets/Resources 폴더를 만들고, 그 안에서
    ///   우클릭 > Create > Squad > Sound Library
    ///   Resources.Load 함수가 이를 찾을 수 있도록 이름은 반드시 "SoundLibrary"로 둔다
    ///
    /// 에셋을 선택하면 인스펙터에서 소리별 반경 등을 조절할 수 있고,
    /// 플레이 중에 바꿔도 바로 반영된다.
    /// </summary>
    [CreateAssetMenu(menuName = "Squad/Sound Library", fileName = "SoundLibrary")]
    public class SoundLibrary : ScriptableObject
    {
        // Resources 폴더에서 asset 파일을 찾지 못했을 때 불러오는 기본값.
        [Header("○ 튜닝 값 — 자유롭게 조절")]
        [Tooltip("Walk 키를 누르고 천천히 걸을 때의 발소리")]
        [SerializeField] private Sound walking = new Sound("Walking", 5f, 0f, SquadBlackboard.AlertLevel.Suspicious, false);
        [Tooltip("기본 이동(뛰기) 발소리")]
        [SerializeField] private Sound running = new Sound("Running", 10f, 0f, SquadBlackboard.AlertLevel.Suspicious, false);
        [Tooltip("작동 중인 발전기 소리")]
        [SerializeField] private Sound generator = new Sound("Generator", 15f, 6f, SquadBlackboard.AlertLevel.Alerted, true);
        [Tooltip("미끼 소리")]
        [SerializeField] private Sound decoy = new Sound("Decoy", 10f, 0f, SquadBlackboard.AlertLevel.Suspicious, false);

        public Sound Walking => walking;
        public Sound Running => running;
        public Sound Generator => generator;
        public Sound Decoy => decoy;
    }
}
