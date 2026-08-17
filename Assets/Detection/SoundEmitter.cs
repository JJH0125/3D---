using UnityEngine;

namespace Squad
{
    public static class SoundEmitter
    {
        // 소리가 영향을 미칠 수 있는 최대 적 : 16
        private const int MaxHits = 16;
        private static readonly Collider[] _hits = new Collider[MaxHits];
        
        // pos에서 sound를 방출한다. sound.Radius 안에 있으면서 enemyLayer 안에 속한 개체에게 이 소리를 보고한다.
        public static void Emit(Vector3 pos, Sound sound, LayerMask enemyLayer)
        {
            if (SquadBlackboard.Instance == null)
                return;
            
            int count = Physics.OverlapSphereNonAlloc(
                pos, sound.Radius, _hits, enemyLayer, QueryTriggerInteraction.Ignore);

            if (count == 0)
                return;

            SquadBlackboard.Instance.ReportSound(pos, sound);
        }
    }
}
