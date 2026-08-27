using UnityEngine;

namespace Squad
{
    /// <summary>
    /// 적들의 DimensionMember를 모아 MeshRenderer를 관리하는 클래스.
    /// 플레이어와 다른 차원에 있는 적의 Renderer를 비활성화
    /// </summary>
    public class DimensionController : MonoBehaviour
    {
        var memberSet = new HashSet<DimensionMember>();
    }    
}