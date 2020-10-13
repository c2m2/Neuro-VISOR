using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using C2M2.Utils;

public class GrabPointRescaler : MonoBehaviour
{
    public Vector3 handleSizeGlobal = new Vector3(0.025f, 0.1f, 0.025f);
    public Transform target = null;
    public Transform xP = null;
    public Transform xN = null;
    public Transform yP = null;
    public Transform yN = null;
    public Transform zP = null;
    public Transform zN = null;

    private bool AnyHandleHasChanged
    {
        get
        {
            return (xP.hasChanged 
                || xN.hasChanged
                || yP.hasChanged 
                || yN.hasChanged
                || zP.hasChanged 
                || zN.hasChanged);
        }
        set
        {
            xP.hasChanged = value;
            xN.hasChanged = value;
            yP.hasChanged = value;
            yN.hasChanged = value;
            zP.hasChanged = value;
            zN.hasChanged = value;
        }
    }

    private MeshFilter mf;
    private Vector3 LocalSize { get { return mf.sharedMesh.bounds.size; } }
    private Vector3 Max
    {
        get
        {
            return mf.sharedMesh.bounds.max;
        }
        set
        {
            Bounds bounds = mf.sharedMesh.bounds;
            bounds.max = value;
            mf.sharedMesh.bounds = bounds;
        }
    }
    private Vector3 Min { get { return mf.sharedMesh.bounds.min; } }
    private Vector3 LocalExtents { get { return LocalSize / 2; } }
    private Vector3 LocalCenter { get { return mf.sharedMesh.bounds.center; } }
    private Vector3 GlobalSize { get { return LocalSize.Dot(target.localScale); } }
    private Vector3 GlobalCenter { get { return target.position; } }

    private Vector3 CurCenter
    {
        get
        {
            return new Vector3(
                (xP.position.x - xN.position.x) / 2,
                (yP.position.y - yN.position.y) / 2,
                (zP.position.z - zN.position.z) / 2);
        }
    }

    /*
     *  GlobalSize.x = LocalSize.x * localScale.x
     *  localScale.x = GlobalSize.x / LocalSize.x;
     */

    private void Awake()
    {
        if (target == null) target = transform.parent;
        if (xP == null 
            || xN == null
            || yP == null 
            || yN == null 
            || zP == null 
            || zN == null)
        {
            Debug.LogError("Not all handles given!");
            Destroy(this);
        }
        mf = target.GetComponent<MeshFilter>();
        if(mf == null)
        {
            Debug.LogError("No MeshFilter found on target [" + target.name + "]");
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        if (mf.sharedMesh == null)
        {
            Debug.LogError("No shared mesh found on MeshFilter on  target [" + target.name + "]");
        }

        UpdateHandlePositions();
        UpdateHandleScales();
        AnyHandleHasChanged = false;
        //target.hasChanged = false;
    }

    // TODO: Handles will scale/follow correctly, but can't update target yet
    // Update is called once per frame
    void Update()
    {
        // If any handle has moved, we need to update the target to match scale/position
        if (AnyHandleHasChanged)
        {
            UpdateTarget();
            //UpdateHandlePositions();
            //UpdateHandleScales();
            AnyHandleHasChanged = false;
        }

    }

    private float XDist { get { return xP.position.x - xN.position.x; } }
    private float YDist { get { return yP.position.y - yN.position.y; } }
    private float ZDist { get { return zP.position.z - zN.position.z; } }
    private void UpdateTarget()
    {
        Vector3 globSize = GlobalSize;
        Vector3 localSize = LocalSize;

        Vector3 newScale = new Vector3(XDist / localSize.x, 
            YDist / localSize.y, 
            ZDist / localSize.z);

        target.localScale = newScale;

        target.position = CurCenter;
    }

    private void UpdateHandlePositions()
    {
        xP.localPosition = new Vector3(Max.x, 0f, 0f);
        xN.localPosition = new Vector3(Min.x, 0f, 0f);

        yP.localPosition = new Vector3(0f, Max.y, 0f);
        yN.localPosition = new Vector3(0f, Min.y, 0f);

        zP.localPosition = new Vector3(0f, 0f, Max.z);
        zN.localPosition = new Vector3(0f, 0f, Min.z);
    }

    private void UpdateHandleScales()
    {
        Vector3 scale = new Vector3(
            handleSizeGlobal.x / target.localScale.x,
            handleSizeGlobal.y / target.localScale.y,
            handleSizeGlobal.z / target.localScale.z);

        xP.localScale = scale;
        xN.localScale = scale;
        yP.localScale = scale;
        yN.localScale = scale;
        zP.localScale = scale;
        zN.localScale = scale;
    }
}
