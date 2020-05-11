using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class ParticleSystemController : MonoBehaviour {

    public int numberOfParticles = 100;
    public float particleSize = 0.1f;
    private ParticleSystem.Particle[] particles;
    private bool particlesUpdated = false;
    private ParticleSystem particleSys;
    private ParticleSystemRenderer particleSysRend;
    private ParticleSystemAsciiDataReader particleTextData;
    private ParticleFieldHandleController particleHandleController;

    private Vector3[] newPositions;
    private Color[] newColors;

    private int particleNumberChangeHolder;
    private float particleSizeChangeHolder;

    public Vector3 maxValues;
    public Vector3 minValues;
    private Vector3 maxValueHolder;
    private Vector3 minValueHolder;

    //Isoquant controls
    public float isoquantHigh = 0f;
    public float isoquantLow = 0f;
    private float isoquantHighHolder = 0f;
    private float isoquantLowHolder = 0f;

    void Start () {
        //Get particle system and renderer
        particleSys = gameObject.GetComponent<ParticleSystem>();
        particleSysRend = gameObject.GetComponent<ParticleSystemRenderer>();
        particleTextData = gameObject.GetComponent<ParticleSystemAsciiDataReader>();
        particleHandleController = gameObject.GetComponent<ParticleFieldHandleController>();

        //Initialize particle data
        particleTextData.InitializeParticleData();

        //Initializes the isoquant to be the global max and min
        isoquantHigh = particleTextData.dataSetList.ElementAt(0).dataList.Max(point => point.scalarValue);
        isoquantLow = particleTextData.dataSetList.ElementAt(0).dataList.Min(point => point.scalarValue);
        isoquantHighHolder = isoquantHigh;
        isoquantLowHolder = isoquantLow;

        //TODO: Make this general for any position element
        UpdateParticles(particleTextData.dataSetList.ElementAt(0));

        maxValues = new Vector3(particleTextData.dataSetList.ElementAt(0).dataList.Max(point => point.position.x),
            particleTextData.dataSetList.ElementAt(0).dataList.Max(point => point.position.y), particleTextData.dataSetList.ElementAt(0).dataList.Max(point => point.position.z));
        minValues = new Vector3(particleTextData.dataSetList.ElementAt(0).dataList.Min(point => point.position.x),
            particleTextData.dataSetList.ElementAt(0).dataList.Min(point => point.position.y), particleTextData.dataSetList.ElementAt(0).dataList.Min(point => point.position.z));

        maxValueHolder = maxValues;
        minValueHolder = minValues;

        particleHandleController.UpdateHandlePositions(maxValues, minValues);

        //Keep placeholders to watch for system changes
        particleSizeChangeHolder = particleSize;
        particleNumberChangeHolder = numberOfParticles;
	}
	

	void Update () {
        //
        if (particlesUpdated)
        {
            particleSys.SetParticles(particles, particles.Length);
            particlesUpdated = false;
        }

        if((particleNumberChangeHolder != numberOfParticles))
        {
            // CreateParticleArrays();
            UpdateParticles(particleTextData.dataSetList.ElementAt(0));
            particleNumberChangeHolder = numberOfParticles;
        }

        if(particleSizeChangeHolder != particleSize)
        {
            UpdateParticleSize(particleSize);
            particleSizeChangeHolder = particleSize;
        }

        //If the handle moves, move the boundary with it
        if (maxValues != particleHandleController.GetMaxVector() || minValues != particleHandleController.GetMinVector())
        {
            maxValues = particleHandleController.GetMaxVector();
            minValues = particleHandleController.GetMinVector();
        }

        //If any max or min is changed in any way, resize the particle field accordingly.
        if (maxValues != maxValueHolder || minValues != minValueHolder)
        {
            //TODO: make this general for any element of dataSetList
            particleTextData.dataSetList.ElementAt(0).PositionsBoundsResize(maxValues, minValues);
            maxValueHolder = maxValues;
            minValueHolder = minValues;
            UpdateParticles(particleTextData.dataSetList.ElementAt(0));
            //Incase the boundaries have been changed by button
            particleHandleController.UpdateHandlePositions(maxValues, minValues);
        }

        //isoquant controls
        if(isoquantHigh != isoquantHighHolder || isoquantLow != isoquantLowHolder)
        {
            particleTextData.dataSetList.ElementAt(0).IsolateQuantity(isoquantLow, isoquantHigh);
            isoquantHighHolder = isoquantHigh;
            isoquantLowHolder = isoquantLow;
            UpdateParticles(particleTextData.dataSetList.ElementAt(0));
        }


	}

    public void UpdateParticles(DataSet newDataSet)
    {
        particles = new ParticleSystem.Particle[newDataSet.dataList.Count];

        numberOfParticles = newDataSet.dataList.Count;

        for(int i = 0; i < newDataSet.dataList.Count; ++i)
        {
            particles[i].position = newDataSet.dataList[i].position;
            particles[i].startColor = newDataSet.dataList[i].color;
            particles[i].startSize = particleSize;
        }

        particlesUpdated = true;
    }

    public void UpdateParticleSize(float newSize)
    {
        Debug.Log("Updating Particle Size...");
        for (int i = 0; i < particles.Length; ++i)
        {
            particles[i].startSize = newSize;
        }

         particlesUpdated = true;

        Debug.Log("Done Updating Particle Size.");
    }

}
