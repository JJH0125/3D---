using UnityEngine;

namespace Squad
{
    public static class SoundEmitter
    {
        // 소리가 영향을 미칠 수 있는 최대 적 : 16
        private const int MaxHits = 16;
        private static readonly Collider[] _hits = new Collider[MaxHits];
        
        /// position에서 sound를 방출한다.
        /// Radius 안에 있으면서 enemyLayer 안에 속한 개체가 있다면 ReportSound한다.
        public static void Emit(Vector3 position, Sound sound, LayerMask enemyLayer,
            GameObject source = null)
        {
            if (SquadBlackboard.Instance == null)
                return;
            
            int count = Physics.OverlapSphereNonAlloc(
                position, sound.Radius, _hits, enemyLayer, QueryTriggerInteraction.Ignore);

            if (count == 0)
                return;

            SquadBlackboard.Instance.ReportSound(position, sound, source);
        }
    }
}
