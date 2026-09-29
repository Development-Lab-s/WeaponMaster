using UnityEngine;

namespace WorkFolder.PAP.Scripts.WeaponSystem
{
    // 철퇴 공처럼 무기 안에서 따로 Rigidbody를 가진 부분에 붙는다. 충돌 이벤트는 그 Rigidbody의
    // 오브젝트로만 오기 때문에, 무기 루트의 WeaponCollideModule로 넘겨 같은 판정을 받게 한다.
    public sealed class WeaponHitRelay : MonoBehaviour
    {
        private WeaponCollideModule _module;

        public void Bind(WeaponCollideModule module)
        {
            _module = module;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_module != null)
                _module.HandleCollision(collision);
        }
    }
}
