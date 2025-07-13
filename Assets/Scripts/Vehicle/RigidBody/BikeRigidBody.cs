using UnityEngine;

namespace ZombieGame.Vehicle.RigidBody
{
    public class BikeRigidBody : VehicleRigidBody
    {
        protected override void Awake()
        {
            // Scooter-specific physics values
            defaultMass = 95f;         // Light scooter weight
            defaultDrag = 0.2f;        // Lower drag due to smaller profile
            defaultAngularDrag = 0.03f; // Less angular resistance for agility
            defaultCenterOfMass = new Vector3(0, -0.3f, 0); // Lower center for stability

            // Scooter-specific wheel setup
            wheelRadius = 0.25f;       // Smaller wheels for scooter
            wheelMass = 8f;           // Lighter wheels
            suspensionDistance = 0.15f; // Less suspension travel
            
            // Lighter suspension settings
            suspensionSpring = 25000f;
            suspensionDamper = 3000f;
            suspensionTargetPosition = 0.5f;
            
            // Quick response friction settings
            forwardStiffness = 1.2f;
            sidewaysStiffness = 1.0f;

            base.Awake();
        }
    }
} 