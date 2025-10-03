using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace NewSR2MP.Component
{
    [RegisterTypeInIl2Cpp(false)]
    public class TransformSmoother : MonoBehaviour
    {
        public void SetRigidbodyState(bool enabled)
        {
            if (GetComponent<Rigidbody>() != null)
                GetComponent<Rigidbody>().constraints =
                    enabled 
                        ? RigidbodyConstraints.None 
                        : RigidbodyConstraints.FreezeAll;
        }

        
        void Start()
        {
            if (GetComponent<NetworkPlayer>() != null)
            {
                thisPlayer = GetComponent<NetworkPlayer>();
            }
            if (GetComponent<NetworkActor>() != null)
            {
                thisActor = GetComponent<NetworkActor>();
            }
        }

        public NetworkPlayer thisPlayer;

        public NetworkActor thisActor;
        
        /// <summary>
        /// Next rotation. The future rotation, this is the rotation the transform is smoothing to.
        /// </summary>
        public Vector3 nextRot;

        /// <summary>
        /// Next position. The future position, this is the position the transform is smoothing to.
        /// </summary>
        public Vector3 nextPos;

        /// <summary>
        ///  Interpolation Period. the speed at which the transform is smoothed.
        /// </summary>
        public float interpolPeriod = PlayerTimer;

        public Vector3 currPos => transform.position;
        private float positionTime;

        public Vector3 currRot => transform.eulerAngles;

        private uint frame;
        
        public void Update()
        {
            if (thisActor) SetRigidbodyState(thisActor.IsOwned);
            
            if (thisActor && thisActor.IsOwned) return;

            float t = 1.0f - ((positionTime - Time.unscaledTime) / interpolPeriod);
            transform.position = Vector3.Lerp(currPos, nextPos, t);

            transform.rotation = Quaternion.Lerp(Quaternion.Euler(currRot), Quaternion.Euler(nextRot), t);

            positionTime = Time.unscaledTime + interpolPeriod;
            
            
        }

        void OnDisable()
        {
            
            if (TryGetComponent<Rigidbody>(out var rb))
                rb.velocity = GetComponent<NetworkActorOwnerToggle>().savedVelocity;
        }
    }
}
