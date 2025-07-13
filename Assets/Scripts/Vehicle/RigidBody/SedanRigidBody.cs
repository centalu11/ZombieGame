using UnityEngine;

namespace ZombieGame.Vehicle.RigidBody
{
    public class SedanRigidBody : VehicleRigidBody
    {
        protected override void Awake()
        {
            // Sedan-specific physics values
            defaultMass = 1500f;        // Average sedan weight (e.g., Toyota Camry)
            defaultDrag = 0.3f;         // Medium drag coefficient
            defaultAngularDrag = 0.05f; // Standard angular resistance
            defaultCenterOfMass = new Vector3(0, -0.4f, 0); // Slightly lower center for stability

            // Sedan-specific wheel setup
            wheelRadius = 0.35f;        // Standard sedan wheel size
            wheelMass = 20f;
            suspensionDistance = 0.3f;
            
            // Softer suspension for comfort
            suspensionSpring = 35000f;
            suspensionDamper = 4500f;
            suspensionTargetPosition = 0.5f;
            
            // Moderate friction settings for balanced handling
            forwardStiffness = 1.5f;
            sidewaysStiffness = 1.5f;

            base.Awake();
        }
    }
} 