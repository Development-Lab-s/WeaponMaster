using Fusion;
using UnityEngine;

namespace DefaultNamespace
{
    public class NetworkConfigurableJoint : NetworkBehaviour
    {
        private ConfigurableJoint _joint;

        public override void Spawned()
        {
            _joint = GetComponent<ConfigurableJoint>();
        }

        public override void Render()
        {
            // _joint.connectedBody 
        }
    }
}