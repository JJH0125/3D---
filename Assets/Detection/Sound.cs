using UnityEngine;

namespace Squad
{
    public class Sound
    {
        // 소리의 이름 (어떤 소리인지)
        public string Name {get;}
        // 소리가 들릴 반경
        public float Radius {get;}
        // 소리의 지속시간
        public float Duration {get;}
        // 소리를 들은 적이 취할 Alert 단계
        public SquadBlackboard.AlertLevel Alert {get;}
        // 소리가 차원을 초월하는지
        public bool CanCrossDimension {get;}

        public Sound(string name, float radius, float duration,
        SquadBlackboard.AlertLevel alert, bool canCrossDimension)
        {
            Name = name;
            Radius = radius;
            Duration = duration;
            Alert = alert;
            CanCrossDimension = canCrossDimension;
        }
    }

    public static class SoundList
    {
        public static readonly Sound Walking =
            new Sound("Walking", 5f, 0f, SquadBlackboard.AlertLevel.Suspicious, false);
        public static readonly Sound Running =
            new Sound("Running", 10f, 0f, SquadBlackboard.AlertLevel.Suspicious, false);
        public static readonly Sound Generator =
            new Sound("Generator", 15f, 6f, SquadBlackboard.AlertLevel.Alerted, true);
        public static readonly Sound Decoy =
            new Sound("Decoy", 10f, 0f, SquadBlackboard.AlertLevel.Suspicious, false);
    }
}
