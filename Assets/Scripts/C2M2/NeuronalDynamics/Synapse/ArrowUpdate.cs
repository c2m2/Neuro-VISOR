using UnityEngine;
using TMPro;

public class ArrowUpdate : MonoBehaviour
{
    public Transform preSynapse;
    public Transform postSynapse;
    public Synapse pre;
    public Synapse post;

    public TextMeshPro nameField;

    private MeshFilter meshFilter;

    [Header("Arrow mode")]
    [SerializeField] private GameObject arrowComponents;
    [SerializeField] private Transform arrowBody;
    [SerializeField] private Transform arrowHead;

    [Header("Disk mode")]
    [SerializeField] private GameObject diskComponents;
    [SerializeField] private Transform body1;
    [SerializeField] private Transform body2;
    [SerializeField] private Transform disk1;
    [SerializeField] private Transform disk2;

    [SerializeField] private ParticleSystem particleSystem;
    private Mesh originalMesh;

    private ParticleSystem.Particle[] m_Particles;

    private float particleSize;


    public enum VisualMode { Arrow, Disk }
    private static VisualMode globalMode = VisualMode.Arrow;
    private VisualMode mode;


    Vector3 _p1;
    Vector3 _direction;
    float _fullLength;
    float _r_pre;
    float _r_post;



    //TODO: rename r_pre/r_post to r_pre and r_post
    //TODO: direction vs length 
    void Start()
    {
        // In UpdateBodySegment(), the vertices of the cylinder mesh are deformed to interpolate the mesh's y-axis between the radii of the the pre and post synapses
        // Each body uses the same cylinder asset. 
        // Using sharedMesh directly, the asset would be modified for every instance. Instantiate() is used to create a per-instance clone
        meshFilter = arrowBody.GetComponent<MeshFilter>();
        originalMesh = Instantiate(meshFilter.sharedMesh);
        meshFilter.mesh = originalMesh;

        //mode = globalMode;
        SetGlobalMode(globalMode);
    }


    void Update()
    {
        if (preSynapse == null || postSynapse == null)
        {
            Destroy(gameObject);
            return;
        }

        nameField.text = pre.currentModel.Value.getModelName();

        Vector3 p1 = preSynapse.position;
        Vector3 p2 = postSynapse.position;
        Vector3 direction = (p2 - p1).normalized;
        float fullLength = Vector3.Distance(p1, p2);

        float r_pre = GetVisualRadius(preSynapse);
        float r_post = GetVisualRadius(postSynapse);

        _p1 = p1;
        _direction = direction;
        _fullLength = fullLength;
        _r_pre = r_pre;
        _r_post = r_post;


        if (mode == VisualMode.Arrow)
        {
            // a single continuous body from 0.0 to 0.8
            float arrowBodyStart = 0f;
            float arrowBodyEnd = 0.8f;
            UpdateBodySegment(arrowBody, arrowBodyStart, arrowBodyEnd);
            UpdateArrowhead(p1, direction, fullLength);
        }


        if (mode == VisualMode.Disk)
        {
            float cleftPosition1 = 0.6f;
            float cleftPosition2 = 0.8f;

            float radius = r_pre + (r_post - r_pre) * (cleftPosition1 + cleftPosition2) * 0.5f;

            UpdateBodySegment(body1, 0f, cleftPosition1);
            UpdateBodySegment(body2, cleftPosition2, 1f);


            Vector3 pos1 = PositionAlongArrow(p1, p2, cleftPosition1);
            Vector3 pos2 = PositionAlongArrow(p1, p2, cleftPosition2);


            UpdateDisk(disk1, pos1, direction, radius, fullLength);
            UpdateDisk(disk2, pos2, -direction, radius, fullLength);


            UpdateParticleSystem();
        }

        UpdateMaterial();
        UpdateLabel(p1, p2, r_pre, r_post);
    }

    public void SetMode(VisualMode newMode)
    {
        mode = newMode;
        arrowComponents.SetActive(mode == VisualMode.Arrow);
        diskComponents.SetActive(mode == VisualMode.Disk);
    }

    public static void SetGlobalMode(VisualMode newMode)
    {
        globalMode = newMode;
        foreach (ArrowUpdate arrow in FindObjectsOfType<ArrowUpdate>())
        {
            arrow.SetMode(globalMode);
        }
    }

    void UpdateMaterial()
    {
        var model = pre.currentModel.Value;

        // This material applies to the disks/arrowhead of the synapse. Either inhibitory or excitatory
        Material typeMaterial;
        if (model.isExcitatory())
            typeMaterial = pre.excitatoryMat;
        else
            typeMaterial = pre.inhibitoryMat;

        // This is a model specific override for the length(s) of the synapse
        Material textMaterial;

        if (model is ModelNMDA)
            textMaterial = pre.excitatoryMat;

        else if(model is ModelGABA)
        {
            textMaterial = pre.GABAMat;
        }
        
        else if(model is ModelAMPA)
        {
            textMaterial = pre.AMPAMat;
        }
        else
            textMaterial = typeMaterial; 


        if (mode == VisualMode.Arrow)
        {
            arrowBody.GetComponent<MeshRenderer>().material = textMaterial;
            arrowHead.GetComponent<MeshRenderer>().material = typeMaterial;
        }
        else // Disk mode
        {
            body1.GetComponent<MeshRenderer>().material = textMaterial;
            body2.GetComponent<MeshRenderer>().material = textMaterial;

            disk1.GetComponent<MeshRenderer>().material = typeMaterial;
            disk2.GetComponent<MeshRenderer>().material = typeMaterial;
        }

    }

    void UpdateLabel(Vector3 p1, Vector3 p2, float r_pre, float r_post)
    {
        Vector3 midpoint = (p1 + p2) * 0.5f;
        midpoint.y += r_pre + r_post * 0.6f;
        nameField.fontSize = r_pre + r_post * 15f;
        nameField.transform.position = midpoint;
    }

    void UpdateBodySegment(Transform body, float startLocation, float endLocation)
    {
        float segmentLength = (endLocation - startLocation) * _fullLength;

        float relativeMidpoint = (startLocation + endLocation) * 0.5f;
        Vector3 segmentMidpoint = _p1 + _direction * (relativeMidpoint * _fullLength);

        MeshFilter mf = body.GetComponent<MeshFilter>();
        Mesh mesh = mf.mesh;

        Vector3[] originalVertices = originalMesh.vertices;
        Vector3[] newVertices = new Vector3[originalVertices.Length];

        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 vertex = originalVertices[i];

            float yRange = Mathf.Lerp(startLocation, endLocation, vertex.y);
            float radiusAtPoint = Mathf.Lerp(_r_pre, _r_post, yRange);

            newVertices[i] = new Vector3(
                vertex.x * radiusAtPoint,
                vertex.y,
                vertex.z * radiusAtPoint
            );
        }

        mesh.vertices = newVertices;

        body.position = segmentMidpoint;
        body.up = _direction;
        body.localScale = new Vector3(1f, segmentLength * 0.5f, 1f);

        Vector2[] uvs = mesh.uv;
        const float tileSize = 0.5f;

        for (int i = 0; i < uvs.Length; i++)
            uvs[i].y = originalVertices[i].y / tileSize;

        mesh.uv = uvs;
    }



    void UpdateDisk(Transform disk, Vector3 position, Vector3 direction, float radius, float length)
    {
        //this is the radius at the midpoint; average between the radius of the pre and post synaptic vertices  
        //

        disk.position = position;
        disk.up = direction;

        //first and last arguments are the x and z coordinates, which are the transversal directions; the y coordinate is the longitudal direction 
        float diskRadius = radius * 3f; //x and z coordinates
        float diskThickness = length * 0.05f; //y coordinate direction

        //comment
        disk.localScale = new Vector3(diskRadius, diskThickness, diskRadius);


        var shape = particleSystem.shape;
        shape.radius = disk.localScale.x * 0.3f;
        //Debug.Log(shape.radius);

        var main = particleSystem.main;
        particleSize = radius * 0.5f;
        main.startSize = particleSize;
    }

    Vector3 PositionAlongArrow(Vector3 p1, Vector3 p2, float t)
    {
        return Vector3.Lerp(p1, p2, t);
    }


    void UpdateArrowhead(Vector3 p1, Vector3 direction, float fullLength)
    {
        float arrowBodyLength = fullLength * 0.8f;
        float coneLength = fullLength - arrowBodyLength;

        float r_pre = GetVisualRadius(preSynapse);
        float r_post = GetVisualRadius(postSynapse);
        float rShaft = Mathf.Lerp(r_pre, r_post, 0.8f); //the width of the arrowhead is rShaft * coneWidth
        float coneWidth = 3f;

        Vector3 arrowBodyEnd = p1 + direction * arrowBodyLength;
        Vector3 coneMidpoint = arrowBodyEnd + direction * (coneLength * 0.5f);

        arrowHead.position = coneMidpoint;
        arrowHead.up = direction;
        arrowHead.localScale = new Vector3(rShaft * coneWidth, coneLength * 0.5f, rShaft * coneWidth);
    }


    float GetVisualRadius(Transform mesh)
    {
        var mf = mesh.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            //defines the cylinder radius at the pre/post-synaptic vertices in terms of the respective radii of the two spheres
            return mf.sharedMesh.bounds.extents.x * mesh.lossyScale.x;
        }
        return 0.1f;
    }


    void UpdateParticleSystem()
    {

        if (particleSystem == null || post == null) return;
        
        var emissionModule = particleSystem.emission;
        var main = particleSystem.main;

        float iSyn = (float)post.currentIsyn;
        float iMax = (float)post.currentModel.Value.getImax();
        //This routine calculates the emissions rate as a function of the Isyn
        float EmMax = 1f; 
        float Em = Mathf.Clamp(Mathf.Abs(iSyn) / iMax, 0.0001f, 1f) * EmMax;
        float emissionRate = Mathf.Max(0.01f, Em);
        float speed = 0.75f * EmMax;

        if (Mathf.Abs(iSyn) > 0f)
        {
            emissionModule.enabled = true;
            emissionModule.rateOverTime = emissionRate * 80f;
            main.startSpeed = speed;
        }
        else
        {
            emissionModule.enabled = false;
        }

       
        Color32 liveColor;
        if (iSyn >= 0f)
        {
            liveColor = Color.red;
        }


        else
        {
            liveColor = Color.cyan;  
        }
    
        //Particle buffer is allocated
        if (m_Particles == null || m_Particles.Length < particleSystem.main.maxParticles)
        {
            m_Particles = new ParticleSystem.Particle[particleSystem.main.maxParticles];
        }

        // GetParticles is allocation free because we reuse the m_Particles buffer between updates
        int numParticlesAlive = particleSystem.GetParticles(m_Particles);

        //Particles move uniformly on the x-axis, but have an element of randomness to their movement on the y-axis

        float target_radius = GetVisualRadius(disk2) * 0.9f; // make bounding cylinder slightly smaller than receiving terminal
        float jitterStrength = 0.5f * target_radius;

        // Change only the particles that are alive
        for (int i = 0; i < numParticlesAlive; i++)
        {

            // non uniform in disk
            float jitter_r = Random.Range(0.0f, jitterStrength);
            float jitter_phi = Random.Range(0.0f,2.0f) * Mathf.PI;
            float xOffset = jitter_r * Mathf.Cos(jitter_phi);
            float yOffset = jitter_r * Mathf.Sin(jitter_phi);

            //yOffset = jitterStrength; // this is for testing

            Vector3 position = m_Particles[i].position;
            position.x += xOffset;
            position.y += yOffset;

            float dist_from_center_line = Mathf.Sqrt(Mathf.Pow(position.x, 2) + Mathf.Pow(position.y, 2))+1.0e-12f;
            float dist_factor = Mathf.Min(target_radius / dist_from_center_line, 1);
            position.x = position.x * dist_factor;
            position.y = position.y * dist_factor;



            Vector3 direction = m_Particles[i].velocity.normalized;

            m_Particles[i].velocity = direction * speed;
            m_Particles[i].position = position;
            m_Particles[i].startSize = particleSize;
            m_Particles[i].startColor = liveColor;
        }

        //Apply the changes
        particleSystem.SetParticles(m_Particles, numParticlesAlive);

    }

}

