using UnityEngine;

namespace ZombieGame.Vehicle.RigidBody
{
    // We can't directly inherit from Rigidbody because it's a Component class
    // Instead, we use RequireComponent to ensure the Rigidbody component exists
    [RequireComponent(typeof(Rigidbody))]
    public abstract class VehicleRigidBody : MonoBehaviour
    {
        private Rigidbody _rb;
        private WheelCollider[] _wheelColliders;
        private Transform[] _wheelMeshes;

        [Header("Wheel Setup")]
        [SerializeField] protected float wheelRadius = 0.35f;
        [SerializeField] protected float suspensionDistance = 0.3f;
        [SerializeField] protected float wheelMass = 20f;
        
        [Header("Suspension Settings")]
        [SerializeField] protected float suspensionSpring = 9000f;
        [SerializeField] protected float suspensionDamper = 900f;
        [SerializeField] protected float suspensionTargetPosition = 0.5f;
        [SerializeField] protected float forceAppPointDistance = -0.3f;
        [SerializeField] protected float wheelDampingRate = 0.25f;

        [Header("Friction Settings")]
        [SerializeField] protected float forwardExtremumSlip = 0.2f;
        [SerializeField] protected float forwardExtremumValue = 1f;
        [SerializeField] protected float forwardAsymptoteSlip = 0.01f;
        [SerializeField] protected float forwardAsymptoteValue = 0.75f;
        [SerializeField] protected float forwardStiffness = 2f;

        [SerializeField] protected float sidewaysExtremumSlip = 0.2f;
        [SerializeField] protected float sidewaysExtremumValue = 1f;
        [SerializeField] protected float sidewaysAsymptoteSlip = 0.5f;
        [SerializeField] protected float sidewaysAsymptoteValue = 0.75f;
        [SerializeField] protected float sidewaysStiffness = 2.2f;

        [Header("Vehicle Physics")]
        [SerializeField] protected float defaultMass = 1500f;
        [SerializeField] protected float defaultDrag = 0.1f;
        [SerializeField] protected float defaultAngularDrag = 0.05f;
        [SerializeField] protected Vector3 defaultCenterOfMass = new Vector3(0, -0.5f, 0);

        // Expose Rigidbody properties directly so it feels like one component
        public float Mass 
        { 
            get => _rb.mass;
            set => _rb.mass = value;
        }

        public float Drag
        {
            get => _rb.linearDamping;
            set => _rb.linearDamping = value;
        }

        public float AngularDrag
        {
            get => _rb.angularDamping;
            set => _rb.angularDamping = value;
        }

        public Vector3 CenterOfMass
        {
            get => _rb.centerOfMass;
            set => _rb.centerOfMass = value;
        }

        public Vector3 Velocity
        {
            get => _rb.linearVelocity;
            set => _rb.linearVelocity = value;
        }

        public Vector3 AngularVelocity
        {
            get => _rb.angularVelocity;
            set => _rb.angularVelocity = value;
        }

        public bool UseGravity
        {
            get => _rb.useGravity;
            set => _rb.useGravity = value;
        }

        public bool IsKinematic
        {
            get => _rb.isKinematic;
            set => _rb.isKinematic = value;
        }

        protected virtual void Awake()
        {
            InitializeRigidbody();
            SetupWheelColliders();
        }

        protected virtual void OnValidate()
        {
            // This ensures values update in the editor
            if (!Application.isPlaying)
            {
                InitializeRigidbody();
                SetupWheelColliders();
            }
        }

        private void InitializeRigidbody()
        {
            // Get or add the Rigidbody component
            _rb = GetComponent<Rigidbody>() ?? gameObject.AddComponent<Rigidbody>();
            
            // Apply default values
            _rb.mass = defaultMass;
            _rb.linearDamping = defaultDrag;
            _rb.angularDamping = defaultAngularDrag;
            
            // Disable automatic center of mass to use our custom value
            _rb.automaticCenterOfMass = false;
            _rb.centerOfMass = defaultCenterOfMass;
            
            // Set up common Rigidbody settings
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            _rb.useGravity = true;
            _rb.isKinematic = false;
        }

        private void SetupWheelColliders()
        {
            // Find wheel meshes based on common naming patterns
            Transform[] wheels = new Transform[4];
            wheels[0] = FindWheelByPattern("_fl"); // Front Left
            wheels[1] = FindWheelByPattern("_fr"); // Front Right
            wheels[2] = FindWheelByPattern("_rl"); // Rear Left
            wheels[3] = FindWheelByPattern("_rr"); // Rear Right

            _wheelMeshes = wheels;
            _wheelColliders = new WheelCollider[4];

            for (int i = 0; i < wheels.Length; i++)
            {
                if (wheels[i] != null)
                {
                    // Create or get existing wheel collider
                    WheelCollider wheelCollider = wheels[i].GetComponent<WheelCollider>();
                    if (wheelCollider == null)
                    {
                        wheelCollider = wheels[i].gameObject.AddComponent<WheelCollider>();
                    }

                    // Configure wheel collider
                    wheelCollider.radius = wheelRadius;
                    wheelCollider.suspensionDistance = suspensionDistance;
                    wheelCollider.mass = wheelMass;
                    wheelCollider.wheelDampingRate = wheelDampingRate;
                    wheelCollider.forceAppPointDistance = forceAppPointDistance;

                    // Set up suspension spring
                    JointSpring spring = new JointSpring();
                    spring.spring = suspensionSpring;
                    spring.damper = suspensionDamper;
                    spring.targetPosition = suspensionTargetPosition;
                    wheelCollider.suspensionSpring = spring;

                    // Configure wheel friction
                    WheelFrictionCurve forwardFriction = new WheelFrictionCurve();
                    forwardFriction.extremumSlip = forwardExtremumSlip;
                    forwardFriction.extremumValue = forwardExtremumValue;
                    forwardFriction.asymptoteSlip = forwardAsymptoteSlip;
                    forwardFriction.asymptoteValue = forwardAsymptoteValue;
                    forwardFriction.stiffness = forwardStiffness;
                    wheelCollider.forwardFriction = forwardFriction;

                    WheelFrictionCurve sidewaysFriction = new WheelFrictionCurve();
                    sidewaysFriction.extremumSlip = sidewaysExtremumSlip;
                    sidewaysFriction.extremumValue = sidewaysExtremumValue;
                    sidewaysFriction.asymptoteSlip = sidewaysAsymptoteSlip;
                    sidewaysFriction.asymptoteValue = sidewaysAsymptoteValue;
                    sidewaysFriction.stiffness = sidewaysStiffness;
                    wheelCollider.sidewaysFriction = sidewaysFriction;

                    _wheelColliders[i] = wheelCollider;
                }
            }
        }

        private Transform FindWheelByPattern(string pattern)
        {
            // Search through all children recursively
            foreach (Transform child in GetComponentsInChildren<Transform>())
            {
                if (child.name.ToLower().Contains("wheel") && child.name.ToLower().EndsWith(pattern.ToLower()))
                {
                    return child;
                }
            }
            return null;
        }

        public WheelCollider[] GetWheelColliders()
        {
            return _wheelColliders;
        }

        public Transform[] GetWheelMeshes()
        {
            return _wheelMeshes;
        }

        // Expose common Rigidbody methods
        public void AddForce(Vector3 force, ForceMode mode = ForceMode.Force)
        {
            _rb.AddForce(force, mode);
        }

        public void AddTorque(Vector3 torque, ForceMode mode = ForceMode.Force)
        {
            _rb.AddTorque(torque, mode);
        }

        public void AddRelativeForce(Vector3 force, ForceMode mode = ForceMode.Force)
        {
            _rb.AddRelativeForce(force, mode);
        }

        public void AddRelativeTorque(Vector3 torque, ForceMode mode = ForceMode.Force)
        {
            _rb.AddRelativeTorque(torque, mode);
        }
    }
} 