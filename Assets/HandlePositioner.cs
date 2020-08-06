using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace C2M2.Interaction
{
    /// <summary>
    /// Follows a center target if given, prevents handles travelling through eachother
    /// </summary>
    public class HandlePositioner : MonoBehaviour
    {
        public Transform partner = null;
        public enum Axis { x, y, z }
        public Axis axis = Axis.x;

        public float distanceThreshold = 0.03f;
        private float Dist { get { return Vector3.Distance(partner.position, transform.position); } }
        private Vector3 prevPos;
        private Vector3 PrevPos
        {
            get
            {
                return prevPos;
            }
            set
            {
                if(prevDist >= distanceThreshold && !Backwards())
                {

                    prevPos = value;
                }
            }
        }
        public float prevDist = -1;
        private bool frontFacing = true;

        private void Awake()
        {
            if (partner == null)
            {
                Debug.LogError("No partner given");
                Destroy(this);
            }

            prevPos = transform.position;
            switch (axis)
            {
                case (Axis.x):
                    LimitDistance = LimitX;
                    Backwards = BackwardsX;
                    if (transform.position.x > partner.position.x) frontFacing = true;
                    else frontFacing = false;
                    break;
                case (Axis.y):
                    LimitDistance = LimitY;
                    Backwards = BackwardsY;
                    if (transform.position.y > partner.position.y) frontFacing = true;
                    else frontFacing = false;
                    break;
                case (Axis.z):
                    LimitDistance = LimitZ;
                    Backwards = BackwardsZ;
                    if (transform.position.z > partner.position.z) frontFacing = true;
                    else frontFacing = false;
                    break;
            }
        }

        // Update is called once per frame
        void Update()
        {
            //transform.position = centerTarget.position;
           // partner.position = centerTarget.position;
            LimitDistance();
            PrevPos = transform.position;
            
        }

        private DistanceLimitMethod LimitDistance;
        private delegate void DistanceLimitMethod();
        private void LimitX()
        {
            float dist = Mathf.Abs(partner.position.x - transform.position.x);

            // If we are too near, don't allow any further movement unless it is moving away
            if(dist < distanceThreshold || Backwards())
            {
                transform.position = new Vector3(prevPos.x, transform.position.y, transform.position.z);
            }

            prevDist = dist;
        }
        private void LimitY()
        {
            float dist = Mathf.Abs(partner.position.y - transform.position.y);
            if (dist < distanceThreshold || Backwards())
            {
                transform.position = new Vector3(transform.position.x, prevPos.y, transform.position.z);
            }
            prevDist = dist;
        }
        private void LimitZ()
        {
            float dist = Mathf.Abs(partner.position.z - transform.position.z);
            if (dist < distanceThreshold || Backwards())
            {
                transform.position = new Vector3(transform.position.x, transform.position.y, prevPos.z);
            }
            prevDist = dist;
        }

        private BackwardsCheck Backwards;
        private delegate bool BackwardsCheck();
        // If it's supposed to be in front, but it is behind
        // Or if it's supposed to be behind, but it is in front
        private bool BackwardsX() => (frontFacing && (transform.position.x < partner.position.x)) || (!frontFacing && (transform.position.x > partner.position.x));
        private bool BackwardsY() => (frontFacing && (transform.position.y < partner.position.y)) || (!frontFacing && (transform.position.y > partner.position.y));
        private bool BackwardsZ() => (frontFacing && (transform.position.z < partner.position.z)) || (!frontFacing && (transform.position.z > partner.position.z));

    }
}