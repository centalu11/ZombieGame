using UnityEngine;

namespace ZombieGame.Vehicle.RigidBody
{
    public class MediumSuvRigidBody : VehicleRigidBody
    {
        protected override void Awake()
        {
            // Set medium SUV-specific default values before base.Awake() applies them
            defaultMass = 2000f;        // Average medium SUV weight (e.g., Honda CR-V)
            defaultDrag = 0.35f;        // Higher drag due to larger profile
            defaultAngularDrag = 0.07f; // Higher angular resistance due to higher center of gravity
            defaultCenterOfMass = new Vector3(0, -0.3f, 0); // Higher center of mass than sedan

            base.Awake();
        }
    }
} 