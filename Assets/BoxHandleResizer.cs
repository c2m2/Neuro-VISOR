using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxHandleResizer : MonoBehaviour
{
    public Transform target = null;
    public Transform xMinHandle = null;
    public Transform xMaxHandle = null;
    public Transform yMinHandle = null;
    public Transform yMaxHandle = null;
    public Transform zMinHandle = null;
    public Transform zMaxHandle = null;
    public Transform center = null;

    public bool HasChanged {
        get
        {
            return xMinHandle.hasChanged || xMaxHandle.hasChanged
                || yMinHandle.hasChanged || yMaxHandle.hasChanged
                || zMinHandle.hasChanged || zMaxHandle.hasChanged;
        }
    }

    public Vector3 Min { get { return new Vector3(xMinHandle.position.x, yMinHandle.position.y, zMinHandle.position.z); } }
    public Vector3 Max { get { return new Vector3(xMaxHandle.position.x, yMaxHandle.position.y, zMaxHandle.position.z); } }
    public Vector3 Center {
        get
        {
            Vector3 min = Min;
            return min + ((Max - min) / 2);
        }
    }
    public Vector3 NewScale
    {
        get
        {
            Vector3 scalers = Scalers;
            return new Vector3(scalers.x * origScale.x, scalers.y * origScale.y, scalers.z * origScale.z);
        }
    }

    private Vector3 origScale;
    private Vector3 origDist;
    private Vector3 Dist {
        get
        {
            Vector3 min = Min;
            Vector3 max = Max;
            return new Vector3(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y), Mathf.Abs(max.z - min.z));
        }
    }
    private Vector3 Scalers
    {
        get
        {
            Vector3 curDist = Dist;
            return new Vector3(curDist.x / origDist.x, curDist.y / origDist.y, curDist.z / origDist.z);
        }
    }

    private Vector3 origLocalScale;

    private void Awake()
    {
        if (xMinHandle == null || xMaxHandle == null
            || yMinHandle == null || yMaxHandle == null
            || zMinHandle == null || zMaxHandle == null
            || center == null)
        {
            Debug.LogError("Not enough handles given");
            Destroy(this);
        }
        if (target == null)
        {
            target = transform;
        }

        origScale = target.localScale;
        origDist = Dist;

        origLocalScale = transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {


        if (HasChanged)
        {
            transform.parent = null;
            center.position = transform.position;
            target.transform.position = Center;
            target.transform.localScale = NewScale;

            UpdateHandlePosScale();
            transform.parent = target;
            ResetHasChanged();
        }

    }

    public override string ToString()
    {
        return "Min: " + Min.ToString("F5") + "\nMax: " + Max.ToString("F5");
    }

    private void ResetHasChanged()
    {
        xMinHandle.hasChanged = false;
        xMaxHandle.hasChanged = false;
        yMinHandle.hasChanged = false;
        yMaxHandle.hasChanged = false;
        zMinHandle.hasChanged = false;
        zMaxHandle.hasChanged = false;
    }

    private bool XChanged { get { return xMinHandle.hasChanged || xMaxHandle.hasChanged; } }
    private bool YChanged { get { return yMinHandle.hasChanged || yMaxHandle.hasChanged; } }
    private bool ZChanged { get { return zMinHandle.hasChanged || zMaxHandle.hasChanged; } }
    private Vector3 xMinPos { get { return new Vector3(xMinHandle.position.x, target.position.y, target.position.z); } }
    private Vector3 xMaxPos { get { return new Vector3(xMaxHandle.position.x, target.position.y, target.position.z); } }
    private Vector3 yMinPos { get { return new Vector3(target.position.x, yMinHandle.position.y, target.position.z); } }
    private Vector3 yMaxPos { get { return new Vector3(target.position.x, yMaxHandle.position.y, target.position.z); } }
    private Vector3 zMinPos { get { return new Vector3(target.position.x, target.position.y, zMinHandle.position.z); } }
    private Vector3 zMaxPos { get { return new Vector3(target.position.x, target.position.y, zMaxHandle.position.z); } }
    private void UpdateHandlePosScale()
    {
        Vector3 scalers = Scalers;
        bool updateX = false, updateY = false, updateZ = false;
        if (XChanged)
        {
            updateY = true;
            updateZ = true;
        }
        if (YChanged)
        {
            updateX = true;
            updateZ = true;
        }
        if (ZChanged)
        {
            updateX = true;
            updateY = true;
        }

        if (updateX)
        {
            xMinHandle.position = xMinPos;
            xMaxHandle.position = xMaxPos;
        }

        if (updateY)
        {
            yMinHandle.position = yMinPos;
            yMaxHandle.position = yMaxPos;
        }

        if (updateZ)
        {
            zMinHandle.position = zMinPos;
            zMaxHandle.position = zMaxPos;
        }

    }
}
