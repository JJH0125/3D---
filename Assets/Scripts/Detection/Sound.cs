using UnityEngine;

namespace Squad
{
    /// <summary>
    /// 소리 한 종류의 데이터. 소리마다 실행 코드가 다르지 않고 값만 다르므로
    /// 상속 없이 값을 채워 쓴다(Goal과 같은 원리).
    ///
    /// [System.Serializable]이라 SoundLibrary 에셋의 인스펙터에서 값을 조절할 수 있다.
    /// </summary>
    [System.Serializable]
    public class Sound
    {
        [Tooltip("소리의 이름 (어떤 소리인지)")]
        [SerializeField] private string name;
        [Tooltip("소리가 들릴 반경(m)")]
        [SerializeField] private float radius;
        [Tooltip("소리의 지속시간(초)")]
        [SerializeField] private float duration;
        [Tooltip("소리를 들은 적이 취할 경계 단계")]
        [SerializeField] private SquadBlackboard.AlertLevel alert;
        [Tooltip("소리가 차원을 초월하는지")]
        [SerializeField] private bool canCrossDimension;

        public string Name => name;
        public float Radius => radius;
        public float Duration => duration;
        public SquadBlackboard.AlertLevel Alert => alert;
        public bool CanCrossDimension => canCrossDimension;

        public Sound(string name, float radius, float duration,
        SquadBlackboard.AlertLevel alert, bool canCrossDimension)
        {
            this.name = name;
            this.radius = radius;
            this.duration = duration;
            this.alert = alert;
            this.canCrossDimension = canCrossDimension;
        }
    }

    /// <summary>
    /// 소리를 쓰는 쪽(발소리, 발전기 등)이 SoundList.Walking처럼 부르는 창구.
    /// 실제 값은 Resources/SoundLibrary 에셋에서 읽는다.
    /// 에셋이 없으면 경고를 한 번 띄우고 기본값으로 동작한다.
    /// </summary>
    public static class SoundList
    {
        private const string LibraryPath = "SoundLibrary";
        private static SoundLibrary _library;

        private static SoundLibrary Library
        {
            get
            {
                if (_library == null)
                {
                    _library = Resources.Load<SoundLibrary>(LibraryPath);
                    if (_library == null)
                    {
                        Debug.LogWarning("[SoundList] Resources/SoundLibrary 에셋이 없어 기본값을 사용합니다. " +
                                         "Assets/Resources에서 Create > Squad > Sound Library로 만들어 주세요.");
                        _library = ScriptableObject.CreateInstance<SoundLibrary>();
                    }
                }
                return _library;
            }
        }

        public static Sound Walking => Library.Walking;
        public static Sound Running => Library.Running;
        public static Sound Generator => Library.Generator;
        public static Sound Decoy => Library.Decoy;
    }
}
