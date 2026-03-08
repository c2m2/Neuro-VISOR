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

    [Header("Terminal mode")]
    [SerializeField] private GameObject diskComponents;
    [SerializeField] private Transform body1;
    [SerializeField] private Transform body2;
    [SerializeField] private Transform disk1;
    [SerializeField] private Transform disk2;

    [SerializeField] private ParticleSystem particleSystem;
    private Mesh originalMesh;

    private ParticleSystem.Particle[] m_Particles;

    private float particleSize;

    private float previousCleftLength = 1f;

    public enum VisualMode { Arrow, Disk }
    private static VisualMode globalMode = VisualMode.Arrow;
    private VisualMode mode;

    Vector3 _p_pre;
    Vector3 _direction;
    float _fullLength;
    float _r_pre;
    float _r_post;



    //TODO: rename body to shaft?
    //TODO: cache the arrays of mesh vertices to bring the GC allocation closer to 0 
    void Start()
    {
        // In UpdateBodySegment(), the vertices of the cylinder mesh are deformed to interpolate the mesh's y-axis between the radii of the the pre and post synapses
        // Each body uses the same cylinder asset. 
        // Using sharedMesh directly, the asset would be modified for every instance. Instantiate() is used to create a per-instance clone
        meshFilter = arrowBody.GetComponent<MeshFilter>();
        originalMesh = Instantiate(meshFilter.sharedMesh);
        meshFilter.mesh = originalMesh;

        SetGlobalMode(globalMode);
    }


    void Update()
    {
        //Debug.Log(1f/0f);

        //Debug.Log("exp^5000" + 0.0/0.0);
        if (preSynapse == null || postSynapse == null)
        {
            Destroy(gameObject);
            return;
        }

        nameField.text = pre.currentModel.Value.getModelName();

        Vector3 p_pre = preSynapse.position;
        Vector3 p_post = postSynapse.position;
        Vector3 direction = (p_post - p_pre).normalized;
        float fullLength = Vector3.Distance(p_pre, p_post);

        float r_pre = GetVisualRadius(preSynapse);
        float r_post = GetVisualRadius(postSynapse);

        // These are all to cache local variables and invoke in other methods. 
        _p_pre = p_pre;
        _direction = direction;
        _fullLength = fullLength;
        _r_pre = r_pre;
        _r_post = r_post;


        if (mode == VisualMode.Arrow)
        {
            // The arrow contains a single continuous body that goes from 0 to 0.8. 
            float arrowBodyStart = 0f;
            float arrowBodyEnd = 0.8f;
            UpdateBodySegment(arrowBody, arrowBodyStart, arrowBodyEnd);

            // The arrowhead starts where the arrow body ends and ends at 1. 
            UpdateArrowhead(arrowBodyEnd, 1f);
        }


        if (mode == VisualMode.Disk)
        {
            float referenceRadius = (r_pre + r_post) * 0.5f; // use average of synaptic vertex radii as reference length scale
            float cleftMinimumLength = referenceRadius * 2f; // minimum length of synaptic cleft (when synapse long enough)
            float cleftMaximumLength = referenceRadius * 3f; // maximum length of synaptic cleft

            float cleftLength = Mathf.Clamp(0.2f * fullLength, cleftMinimumLength, cleftMaximumLength); // if within above bounds, make cleft 0.2 of synapse length
            float cleftCenterPosition = 0.7f; // center point of synaptic cleft (when synapse long enough)

            float synapsePositionPresynVertex = 0f;
            float synapsePositionPresynCleft = Mathf.Max(cleftCenterPosition - cleftLength / fullLength * 0.5f, 0.2f); // relative presynaptic terminal position
            float synapsePositionPostsynCleft = Mathf.Min(cleftCenterPosition + cleftLength / fullLength * 0.5f, 0.8f); // relative postsynaptic terminal position
            float synapsePositionPostsynVertex = 1f;

            Debug.Log("synapsePositionPresynCleft: " + synapsePositionPresynCleft + "synapsePositionPostsynCleft" + synapsePositionPostsynCleft);
            //this is the radius at the midpoint of the synapse interpolated between pre/post
            float radius = Mathf.Lerp(r_pre, r_post, 0.5f);

            //float particleSpeed = (synapsePositionPostsynCleft - synapsePositionPresynCleft) * fullLength * 0.1f;
            float cleftWorldLength = (synapsePositionPostsynCleft - synapsePositionPresynCleft) * fullLength;


            UpdateBodySegment(body1, synapsePositionPresynVertex, synapsePositionPresynCleft);
            UpdateBodySegment(body2, synapsePositionPostsynCleft, synapsePositionPostsynVertex);

            Vector3 preSynPos = PositionAlongArrow(p_pre, p_post, synapsePositionPresynCleft);
            Vector3 postSynPos = PositionAlongArrow(p_pre, p_post, synapsePositionPostsynCleft);

            UpdateDisk(disk1, preSynPos, direction, radius, fullLength);
            UpdateDisk(disk2, postSynPos, -direction, radius, fullLength);
            UpdateParticleSystem(cleftWorldLength);
        
        }

        UpdateMaterial();
        UpdateLabel(p_pre, p_post, r_pre, r_post);
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
            textMaterial = pre.NMDAMat;

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
        else // Terminal mode
        {
            body1.GetComponent<MeshRenderer>().material = textMaterial;
            body2.GetComponent<MeshRenderer>().material = textMaterial;

            disk1.GetComponent<MeshRenderer>().material = typeMaterial;
            disk2.GetComponent<MeshRenderer>().material = typeMaterial;
        }

    }

    void UpdateLabel(Vector3 p_pre, Vector3 p_post, float r_pre, float r_post)
    {
        Vector3 midpoint = (p_pre + p_post) * 0.5f;
        midpoint.y += r_pre + r_post * 0.6f;
        nameField.fontSize = r_pre + r_post * 15f;
        nameField.transform.position = midpoint;
    }

    // This is a linear interpolation between two given radii. The mesh's original vertices are stored and a copy is created to avoid applying the interpolation to each instance,
    // keeping it on a per-instance basis. Each of its vertices are looped through, and its x and y coordinates are are adjusted to fit the linear interpolation.

    // UV tiling is also in here... this needs to be hashed out. Maintain aspect ratio. It should just be moved to a new method. 
    void UpdateBodySegment(Transform body, float startLocation, float endLocation)
    {
        float segmentLength = (endLocation - startLocation) * _fullLength;

        float relativeMidpoint = (startLocation + endLocation) * 0.5f;
        Vector3 segmentMidpoint = _p_pre + _direction * (relativeMidpoint * _fullLength);

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

        float referenceRadius = (_r_pre + _r_post) * 0.5f;
        const float tileSize = 10f;
        float tilingY = (segmentLength / referenceRadius) / tileSize;               //longer segment, bigger UV span, more repeats
        float offsetY = (startLocation * _fullLength / referenceRadius) / tileSize;  //keeps body1/body2 aligned
        for (int i = 0; i < uvs.Length; i++)
        {
            uvs[i].y = originalVertices[i].y * tilingY + offsetY;
        }

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
        float diskThickness = radius * 1f; //y coordinate direction

        //comment
        disk.localScale = new Vector3(diskRadius, diskThickness, diskRadius);


        var shape = particleSystem.shape;
        shape.radius = disk.localScale.x * 0.3f;
        //Debug.Log(shape.radius);

        var main = particleSystem.main;
        particleSize = radius * 0.4f;
        main.startSize = particleSize;
    }

    Vector3 PositionAlongArrow(Vector3 p_pre, Vector3 p_post, float s)
    {
        return Vector3.Lerp(p_pre, p_post, s);
    }


    void UpdateArrowhead(float startS, float endS)
    {
        float arrowHeadLength = (endS - startS) * _fullLength;
        float rShaft = Mathf.Lerp(_r_pre, _r_post, startS);
        float coneWidthMultiplier = 3f;
        float arrowHeadCenter = 0.5f * (startS + endS);
        Vector3 arrowHeadMidpoint = _p_pre + _direction * (arrowHeadCenter * _fullLength);

        arrowHead.position = arrowHeadMidpoint;
        arrowHead.up = _direction;
        arrowHead.localScale = new Vector3(rShaft * coneWidthMultiplier, arrowHeadLength * 0.5f, rShaft * coneWidthMultiplier);
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


    void UpdateParticleSystem(float cleftLength)
    {

        if (particleSystem == null || post == null) return;
        
        var emissionModule = particleSystem.emission;
        var main = particleSystem.main;

        float iSyn = (float)post.currentIsyn;
        float iMax = (float)post.currentModel.Value.getImax();
        //This routine calculates the emissions rate as a function of the Isyn
        float EmMax = 300f; // Constant for emissions rate
        float EmissionRate = Mathf.Clamp(Mathf.Abs(iSyn) / iMax, 0.0001f, 1f);
        //Debug.Log("Isyn over Imax: " + Mathf.Abs(iSyn) / iMax);



        float traversalTime = 0.5f;
        float currentSpeed = cleftLength/traversalTime;
        if (Mathf.Abs(iSyn) > 0f)
        {
            emissionModule.enabled = true;
            emissionModule.rateOverTime = EmissionRate * EmMax;
            main.startLifetime = traversalTime;
            main.startSpeed = currentSpeed;
        }
        else
        {
            emissionModule.enabled = false;
        }

       
        Color32 liveColor;
        if (iSyn >= 0f)
        {
            main.startColor = Color.red;
        }


        else
        {
            main.startColor = Color.cyan;  
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
        float jitterStrength = 0.05f * target_radius;

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

            m_Particles[i].velocity = m_Particles[i].velocity.normalized * currentSpeed;

            m_Particles[i].remainingLifetime *= cleftLength / previousCleftLength;

            m_Particles[i].position = position;
            m_Particles[i].startSize = particleSize;
        }

        //Apply the changes
        particleSystem.SetParticles(m_Particles, numParticlesAlive);
        previousCleftLength = cleftLength;
    }

}

