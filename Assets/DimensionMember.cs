using UnityEngine;

namespace Squad
{
    /// 차원 정의
    public enum Dimension { Real, Fake }

    /// <summary>
    /// 두 차원에 동시에 존재할 수 없으며 사이를 넘나들 수도 없는 Enemy에게만 붙이는 차원 태그.
    /// </summary>
    public class DimensionMember : MonoBehaviour
    {
        [SerializeField] private Dimension dimension;

        public Dimension Dimension => dimension;
    }
}