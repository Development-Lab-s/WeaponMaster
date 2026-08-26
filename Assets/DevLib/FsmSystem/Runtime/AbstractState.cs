using UnityEngine;

namespace DevLib.FsmSystem.Runtime
{
    public class AbstractState
    {
        protected GameObject _owner;
        protected StateSO _stateSO;

        public AbstractState(GameObject owner, StateSO stateData)
        {
            _owner = owner;
            _stateSO = stateData;
        }
        
        public virtual void Enter() {}

        public void Update() => OnUpdate();

        //매 프레임 반환값을 통해서 부모 상태의 실패시 추가적인 상태 실행을 안하도록 한다.
        protected virtual bool OnUpdate() => false;
        
        public virtual void Exit() {}
    }
}