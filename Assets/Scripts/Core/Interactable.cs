using UnityEngine;

namespace ZombieGame.Core
{
    public class Interactable : MonoBehaviour
    {
        public enum InteractableType
        {
            Item,
            Door,
            Vehicle
        }

        [Tooltip("Maximum distance at which the player can interact with this object")]
        [SerializeField] private float interactionThreshold = 2f;

        [SerializeField] private InteractableType type = InteractableType.Item;
        public InteractableType Type 
        { 
            get => type;
            set 
            {
                type = value;
                // Auto-set threshold to 7 for vehicles
                if (type == InteractableType.Vehicle && interactionThreshold == 2f)
                {
                    interactionThreshold = 7f;
                }
            }
        }

        public float InteractionThreshold => interactionThreshold;

        private void OnValidate()
        {
            // Auto-set threshold to 7 for vehicles when changed in inspector
            if (type == InteractableType.Vehicle && interactionThreshold == 2f)
            {
                interactionThreshold = 7f;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw interaction radius in editor
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, interactionThreshold);
        }
    }
} 