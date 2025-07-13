using UnityEngine;

namespace ZombieGame.Vehicle.RigidBody
{
    public class SuvRigidBody : VehicleRigidBody
    {
        protected override void Awake()
        {
            // SUV-specific physics values
            defaultMass = 2200f;        // Heavy SUV weight (e.g., Toyota Land Cruiser)
            defaultDrag = 0.4f;         // Higher drag due to larger profile
            defaultAngularDrag = 0.1f;  // More resistance to rotation
            defaultCenterOfMass = new Vector3(0, -0.2f, 0); // Higher center of mass

            // SUV-specific wheel setup
            wheelRadius = 0.45f;        // Larger wheels for SUV
            wheelMass = 30f;           // Heavier wheels
            suspensionDistance = 0.4f;  // More suspension travel
            
            // Stiffer suspension for handling the weight
            suspensionSpring = 45000f;
            suspensionDamper = 5500f;
            suspensionTargetPosition = 0.5f;
            
            // Adjusted friction for off-road capability
            forwardStiffness = 1.8f;
            sidewaysStiffness = 1.6f;

            base.Awake();
        }
    }
} 