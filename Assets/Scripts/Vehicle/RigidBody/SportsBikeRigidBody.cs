using UnityEngine;

namespace ZombieGame.Vehicle.RigidBody
{
    public class SportsBikeRigidBody : VehicleRigidBody
    {
        protected override void Awake()
        {
            // Sports bike-specific physics values
            defaultMass = 180f;        // Performance bike weight (e.g., Ducati)
            defaultDrag = 0.15f;       // Very low drag for aerodynamics
            defaultAngularDrag = 0.02f; // Low angular resistance for quick turning
            defaultCenterOfMass = new Vector3(0, -0.35f, 0); // Low and forward center

            // Sports bike-specific wheel setup
            wheelRadius = 0.3f;        // Standard sports bike wheel size
            wheelMass = 12f;          // Performance wheels
            suspensionDistance = 0.2f; // Sport suspension travel
            
            // Performance suspension settings
            suspensionSpring = 40000f;
            suspensionDamper = 4000f;
            suspensionTargetPosition = 0.5f;
            
            // High-performance friction settings
            forwardStiffness = 2.0f;
            sidewaysStiffness = 1.8f;

            base.Awake();
        }
    }
} 