using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleFieldHandleController : MonoBehaviour {

    public GameObject xMinMarker;
    public GameObject xMaxMarker;
    public GameObject yMinMarker;
    public GameObject yMaxMarker;
    public GameObject zMinMarker;
    public GameObject zMaxMarker;

    private Vector3 minVector;
    private Vector3 maxVector;

    public BoxCollider xMinCollider;
    public BoxCollider xMaxCollider;
    public BoxCollider yMinCollider;
    public BoxCollider yMaxCollider;
    public BoxCollider zMinCollider;
    public BoxCollider zMaxCollider;


    public void UpdateHandlePositions(Vector3 maxValues, Vector3 minValues)
    {
        //Don't allow axis crossing
        if (maxValues.x > minValues.x && maxValues.y > minValues.y && maxValues.z > minValues.z)
        {
            //the xmin marker will always be at the minimum x value, max always at max x value, etc
            xMinMarker.transform.position = new Vector3(minValues.x, 0, 0);
            xMaxMarker.transform.position = new Vector3(maxValues.x, 0, 0);
            yMinMarker.transform.position = new Vector3(0, minValues.y, 0);
            yMaxMarker.transform.position = new Vector3(0, maxValues.y, 0);
            zMinMarker.transform.position = new Vector3(0, 0, minValues.z);
            zMaxMarker.transform.position = new Vector3(0, 0, maxValues.z);

            //Resize colliders
            xMinCollider.size = new Vector3((yMaxMarker.transform.localPosition.y - yMinMarker.transform.localPosition.y), 0.2f, (zMaxMarker.transform.localPosition.z - zMinMarker.transform.localPosition.z));
            xMinCollider.center = new Vector3((yMaxMarker.transform.localPosition.y + yMinMarker.transform.localPosition.y) / 2, 0.2f, (zMaxMarker.transform.localPosition.z + zMinMarker.transform.localPosition.z) / 2);

            xMaxCollider.size = xMinCollider.size;
            xMaxCollider.center = new Vector3(-xMinCollider.center.x, xMinCollider.center.y, xMinCollider.center.z);

            yMinCollider.size = new Vector3((xMaxMarker.transform.localPosition.x - xMinMarker.transform.localPosition.x), 0.2f, (zMaxMarker.transform.localPosition.z - zMinMarker.transform.localPosition.z));
            yMinCollider.center = new Vector3((xMaxMarker.transform.localPosition.x + xMinMarker.transform.localPosition.x) / 2, 0.2f, -(zMaxMarker.transform.localPosition.z + zMinMarker.transform.localPosition.z) / 2);

            yMaxCollider.size = yMinCollider.size;
            yMaxCollider.center = new Vector3(yMinCollider.center.x, yMinCollider.center.y, -yMinCollider.center.z);

            zMinCollider.size = new Vector3((xMaxMarker.transform.localPosition.x - xMinMarker.transform.localPosition.x), 0.2f, (yMaxMarker.transform.localPosition.y - yMinMarker.transform.localPosition.y));
            zMinCollider.center = new Vector3((xMaxMarker.transform.localPosition.x + xMinMarker.transform.localPosition.x) / 2, 0.2f, (yMaxMarker.transform.localPosition.y + yMinMarker.transform.localPosition.y) / 2);

            zMaxCollider.size = zMinCollider.size;
            zMaxCollider.center = new Vector3(zMinCollider.center.x, zMinCollider.center.y, -zMinCollider.center.z);


            //Update max holders and min holders
            minVector = new Vector3(xMinMarker.transform.position.x, yMinMarker.transform.position.y, zMinMarker.transform.position.z);
            maxVector = new Vector3(xMaxMarker.transform.position.x, yMaxMarker.transform.position.y, zMaxMarker.transform.position.z);
        }
    }

    private void Update()
    {
        if (minVector != new Vector3(xMinMarker.transform.position.x, yMinMarker.transform.position.y, zMinMarker.transform.position.z) ||
             maxVector != new Vector3(xMaxMarker.transform.position.x, yMaxMarker.transform.position.y, zMaxMarker.transform.position.z))
        {
            minVector = new Vector3(xMinMarker.transform.position.x, yMinMarker.transform.position.y, zMinMarker.transform.position.z);
            maxVector = new Vector3(xMaxMarker.transform.position.x, yMaxMarker.transform.position.y, zMaxMarker.transform.position.z);
        }
    }

    public Vector3 GetMinVector()
    {
        return minVector;
    }

    public Vector3 GetMaxVector()
    {
        return maxVector;
    }

}
