using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

// TODO: Deprecate this, we don't use it currently


/// <summary>
/// 1. Take in names of of OVR hand components and details of hand component colliders in the editor.
/// 2. Locate hand components after they are generated at runtime.
/// 3. Attach colliders and rigidbodies to appropriate hand components and store them in a collider list
/// 4. Convert collider list to array and send to each hand's OVRGrabber m_grabVolume.
/// </summary>
public class InitializeHandColliders : MonoBehaviour
{

    //Attach to LocalAvatar     
    [Tooltip("Physics layer of all grabber components")]
    public string grabberLayerName = "Grabber";
    [Tooltip("OVRGrabber component of the right hand.")]
    public C2M2.Interaction.VR.PublicOVRGrabber rightGrabber;
    [Tooltip("OVRGrabber component of the left hand.")]
    public C2M2.Interaction.VR.PublicOVRGrabber leftGrabber;
    [Tooltip("Hierarchy paths and collider info for hand components")]
    public AvatarPaths avatar;
    public enum Direction { XAxis = 0, YAxis = 1, ZAxis = 2}

    private bool foundLeft = false;
    private bool foundRight = false;
    private Transform leftHandParent;
    private Transform rightHandParent;
    private List<Collider> rightColliders = new List<Collider>();          //Used to store generated colliders and send them to the grabber component
    private List<Collider> leftColliders = new List<Collider>();
    private List<string> leftButtonPresserList = new List<string>();
    private List<string> rightButtonPresserList = new List<string>();

    private Collider leftIndexFingerTipCollider;
    private Collider rightIndexFingerTipCollider;



    private void Start()
    {
        leftHandParent = transform.Find(avatar.leftHand.name);
        rightHandParent = transform.Find(avatar.rightHand.name);
    }

    // OVR Manager builds hands around frame 30. So check the size of the transform array every frame until they exist
    void Update()
    {

        if (!foundRight && rightHandParent.gameObject.GetComponentsInChildren<Transform>(true).Length > 1)
        {         
            CheckRight();
        }
        if(!foundLeft && leftHandParent.gameObject.GetComponentsInChildren<Transform>(true).Length > 1)
        {
            CheckLeft();
        }
        

        //TODO: you could probably just disable the script once you are done adding the colliders
        if (foundLeft && foundRight)                                                                /*If we have found both fingertips*/
        {
            Debug.Log("Hand Colliders Intiialized.");

            ///MAYBE you should initialize the menu right here

            Destroy(this);
        }
    }



    private void CheckLeft()
    {
        Transform[] transforms = leftHandParent.GetComponentsInChildren<Transform>(true);

        //All objects in the hand should be on the grabber layer
        foreach (Transform t in transforms)
        {
            t.gameObject.layer = LayerMask.NameToLayer(grabberLayerName);
        }

        foreach (Finger f in avatar.leftHand.fingers)          //Test against all fingers in the hand
        {
            foreach (Joint j in f.joints)                           //Test against all joints in all fingers
            {
                foreach (Transform t in transforms)                      //Test against the names of all of the transforms
                {
                    if (t.name == j.name)                                    //If we find the name of the current joint in the transform array
                    {
                        for (int i = 0; i < j.colliderInfo.sphereColliders.Length; i++)
                        {
                            SphereColliderInfo sphereColInfo = j.colliderInfo.sphereColliders[i];
                            SphereCollider sphereCol = t.gameObject.AddComponent<SphereCollider>();
                            sphereCol.isTrigger = true;
                            sphereCol.center = sphereColInfo.center;
                            sphereCol.radius = sphereColInfo.radius;
                            if (sphereColInfo.includeInGrabber)
                            {
                                leftColliders.Add(sphereCol);
                            }
                        }

                        for (int i = 0; i < j.colliderInfo.boxColliders.Length; i++)
                        {
                            BoxColliderInfo boxColInfo = j.colliderInfo.boxColliders[i];
                            BoxCollider boxCol = t.gameObject.AddComponent<BoxCollider>();
                            boxCol.isTrigger = true;
                            boxCol.center = boxColInfo.center;
                            boxCol.size = boxColInfo.size;

                            if (boxColInfo.includeInGrabber)
                            {
                                leftColliders.Add(boxCol);
                            }
                        }

                        for (int i = 0; i < j.colliderInfo.capsuleColliders.Length; i++)
                        {
                            CapsuleColliderInfo capsuleColInfo = j.colliderInfo.capsuleColliders[i];
                            CapsuleCollider capsuleCol = t.gameObject.AddComponent<CapsuleCollider>();
                            capsuleCol.isTrigger = true;
                            capsuleCol.center = capsuleColInfo.center;
                            capsuleCol.height = capsuleColInfo.height;
                            capsuleCol.radius = capsuleColInfo.radius;
                            capsuleCol.direction = (int)capsuleColInfo.direction;
                            if (capsuleColInfo.includeInGrabber)
                            {
                                leftColliders.Add(capsuleCol);
                            }
                        }

                        foreach (GameObject prefab in j.Prefabs)            //Attach custom prefabs to joint
                        {
                            if (prefab != null)
                            {
                                Instantiate(prefab, t);
                            }
                        }


                        foreach (string script in j.CustomScripts)          //Attach custom behaviours to joint
                        {
                            if (Type.GetType(script) != null)
                            {
                                t.gameObject.AddComponent(Type.GetType(script));
                            }
                            else
                            {
                                throw new ArgumentException("Type <" + script + "> Not Found (in InitializeHandColliders>" + f.name + ">" + j.name + ">CustomScripts)");
                            }

                        }

                        if(j.tag != null)
                        {
                            t.tag = j.tag;
                        }

                        Rigidbody rb = t.gameObject.AddComponent<Rigidbody>();
                        rb.useGravity = false;
                        rb.isKinematic = true;

                        break;                      //Found this joint, so break the loop and move to the next joint
                    }
                }

            }
        }

        if (leftColliders.Count > 0)                //If there are colliders to add to the grabber, concatenate them in
        {
            //Send collider array to grabber
            Collider[] colArray = leftColliders.ToArray();
            Collider[] concatCols = new Collider[leftGrabber.M_GrabVolumes.Length + colArray.Length];
            leftGrabber.M_GrabVolumes.CopyTo(concatCols, 0);
            colArray.CopyTo(concatCols, leftGrabber.M_GrabVolumes.Length);
            leftGrabber.M_GrabVolumes = concatCols;
        }

        foundLeft = true;
    }

    private void CheckRight()
    {
        Transform[] transforms = rightHandParent.GetComponentsInChildren<Transform>(true);
        

        //All objects in the hand should be on the grabber layer
        foreach(Transform t in transforms)
        {
            t.gameObject.layer = LayerMask.NameToLayer(grabberLayerName);
        }

        foreach (Finger f in avatar.rightHand.fingers)          //Test against all fingers in the hand
        {
            foreach (Joint j in f.joints)                           //Test against all joints in all fingers
            {
                foreach(Transform t in transforms)                      //Test against the names of all of the transforms
                {
                    if(t.name == j.name)                                    //If we find the name of the current joint in the transform array
                    {
                        for(int i = 0; i < j.colliderInfo.sphereColliders.Length; i++)
                        {
                            SphereColliderInfo sphereColInfo = j.colliderInfo.sphereColliders[i];
                            SphereCollider sphereCol = t.gameObject.AddComponent<SphereCollider>();
                            sphereCol.isTrigger = true;
                            sphereCol.center = sphereColInfo.center;
                            sphereCol.radius = sphereColInfo.radius;
                            if (sphereColInfo.includeInGrabber)
                            {
                                rightColliders.Add(sphereCol);
                            }
                        }

                        for (int i = 0; i < j.colliderInfo.boxColliders.Length; i++)
                        {
                            BoxColliderInfo boxColInfo = j.colliderInfo.boxColliders[i];
                            BoxCollider boxCol = t.gameObject.AddComponent<BoxCollider>();
                            boxCol.isTrigger = true;
                            boxCol.center = boxColInfo.center;
                            boxCol.size = boxColInfo.size;

                            if (boxColInfo.includeInGrabber)
                            {
                                rightColliders.Add(boxCol);
                            }
                        }

                        for(int i = 0; i < j.colliderInfo.capsuleColliders.Length; i++)
                        {
           
                            CapsuleColliderInfo capsuleColInfo = j.colliderInfo.capsuleColliders[i];
                            CapsuleCollider capsuleCol = t.gameObject.AddComponent<CapsuleCollider>();
                            capsuleCol.isTrigger = true;
                            capsuleCol.center = capsuleColInfo.center;
                            capsuleCol.height = capsuleColInfo.height;
                            capsuleCol.radius = capsuleColInfo.radius;
                            capsuleCol.direction = (int)capsuleColInfo.direction;
                            if (capsuleColInfo.includeInGrabber)
                            {
                                rightColliders.Add(capsuleCol);
                            }                         
                        }

                        foreach (string script in j.CustomScripts)          //Attach custom behaviours to joint
                        {
                            if(Type.GetType(script) != null)
                            {
                                if (t.gameObject.AddComponent(Type.GetType(script)) == null)
                                {
                                    Debug.Log("[InitializeHandColliders] Error adding component <" + script + ">");
                                }
                            }
                            else
                            {
                                Debug.LogError("Type <" + script + "> Not Found (in InitializeHandColliders>" + f.name + ">" + j.name + ">CustomScripts)");
                            }
                            
                        }

                        foreach(GameObject prefab in j.Prefabs)
                        {
                            if(prefab != null)
                            {
                                Instantiate(prefab, t);
                            }
                        }

                        if (j.tag != null)
                        {
                            t.tag = j.tag;
                        }

                        Rigidbody rb = t.gameObject.AddComponent<Rigidbody>();
                         rb.useGravity = false;
                         rb.isKinematic = true;

                        break;                      //Found this joint, so break the loop and move to the next joint
                    }
                }
                    
            }
        }

        if(rightColliders.Count > 0)                //If there are colliders to add to the grabber, concatenate them in
        {
            //Send collider array to grabber
            Collider[] colArray = rightColliders.ToArray();
            Collider[] concatCols = new Collider[rightGrabber.M_GrabVolumes.Length + colArray.Length];
            rightGrabber.M_GrabVolumes.CopyTo(concatCols, 0);
            colArray.CopyTo(concatCols, rightGrabber.M_GrabVolumes.Length);
            rightGrabber.M_GrabVolumes = concatCols;
        }
        

        foundRight = true;
    }


    [System.Serializable]
    public class AvatarPaths
    {
        public Hand rightHand;
        public Hand leftHand;
    }

    [System.Serializable]
    public class Hand
    {
        [Tooltip("Hierarchy name of the hand (Can be found at runtime under OVRAvatar or in OvrAvatar script)")]
        public string name;
        public Finger[] fingers = new Finger[6];
    }

    [System.Serializable]
    public class Finger
    {
        public string name;
        [Tooltip("Joint on each finger (Grip has 1, Thumb has 3, index 3, middle 3, ring 3, & pinky 4 typically)")]
        public Joint[] joints = new Joint[3];
    }

    [System.Serializable]
    public class Joint
    {
        [Tooltip("Hierarchy name of the joint (Can be found at runtime under OVRAvatar or in OvrAvatar script)")]
        public string name;
        [Tooltip("Add this custom tag to your joint (leave blank for Untagged)")]
        public string tag;
        [Tooltip("Specify collider type and numbers for this joint")]
        public ColliderInfo colliderInfo;
        [Tooltip("Add these scripts to the joint at runtime")]
        public string[] CustomScripts;
        [Tooltip("Add these prefabs to the joint at runtime")]
        public GameObject[] Prefabs;
    }

    [System.Serializable]
    public class ColliderInfo
    {
        [Tooltip("Specify number of sphere colliders on this joint and their size/positioning")]
        public SphereColliderInfo[] sphereColliders;
        [Tooltip("Specify number of box colliders on this joint and their size/positioning")]
        public BoxColliderInfo[] boxColliders;
        [Tooltip("Specify number of capsule colliders on this joint and their size/positioning")]
        public CapsuleColliderInfo[] capsuleColliders;

    }

    [System.Serializable]
    public class SphereColliderInfo
    {
        public Vector3 center;
        public float radius;
        [Tooltip("Should this collider contribute to grabbing objects?")]
        public bool includeInGrabber = true;
    }

    [System.Serializable]
    public class BoxColliderInfo
    {
        public Vector3 center;
        public Vector3 size;
        [Tooltip("Should this collider contribute to grabbing objects?")]
        public bool includeInGrabber = true;
    }

    [System.Serializable]
    public class CapsuleColliderInfo
    {
        public Vector3 center;
        public float radius;
        public float height;
        public Direction direction;
        [Tooltip("Should this collider contribute to grabbing objects?")]
        public bool includeInGrabber = true;
    }
}
