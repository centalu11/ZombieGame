using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace ZombieGame.Vehicle
{
    public class VehicleController : MonoBehaviour
    {
        private bool _isEnabled = true;
        
        // References to the two child prefabs
        [SerializeField] private GameObject _interactableVehicle;
        [SerializeField] private GameObject _mvcVehicle;

        private void OnValidate()
        {
            // Find and set up the child prefabs automatically
            Transform interactableChild = transform.Find("InteractableVehicle");
            Transform mvcChild = transform.Find("MVCVehicle");

            // Only assign if the field is currently null
            if (interactableChild != null && _interactableVehicle == null)
            {
                _interactableVehicle = interactableChild.gameObject;
                
                // Set up default configuration for InteractableVehicle
                _interactableVehicle.tag = "Interactable";
                _interactableVehicle.layer = LayerMask.NameToLayer("Default");
                
                // Ensure it has a MeshCollider for interaction
                if (_interactableVehicle.GetComponent<MeshCollider>() == null)
                {
                    var meshCollider = _interactableVehicle.AddComponent<MeshCollider>();
                    meshCollider.convex = true;
                }
            }

            // Only assign if the field is currently null
            if (mvcChild != null && _mvcVehicle == null)
            {
                _mvcVehicle = mvcChild.gameObject;
                
                // Set up default configuration for MVCVehicle
                _mvcVehicle.tag = "Player";
                _mvcVehicle.layer = 6; // Vehicle layer
            }
        }

        private void Awake()
        {
            // Use the serialized fields directly (assigned via OnValidate or manually in inspector)
            if (_interactableVehicle == null)
            {
                Debug.LogError("InteractableVehicle reference is not assigned!");
                return;
            }

            if (_mvcVehicle == null)
            {
                Debug.LogError("MVCVehicle reference is not assigned!");
                return;
            }

            // Start with vehicle disabled (interactable mode)
            DisableVehicle();
        }



        public void DisableVehicle()
        {
            if (!_isEnabled) return;

            // Enable InteractableVehicle, disable MVCVehicle
            if (_interactableVehicle != null) _interactableVehicle.SetActive(true);
            if (_mvcVehicle != null) _mvcVehicle.SetActive(false);

            _isEnabled = false;
        }

        public void EnableVehicle()
        {
            if (_isEnabled) return;

            // Disable InteractableVehicle, enable MVCVehicle
            if (_interactableVehicle != null) _interactableVehicle.SetActive(false);
            if (_mvcVehicle != null) _mvcVehicle.SetActive(true);

            _isEnabled = true;
        }

        // Public method to check if vehicle is initialized
        public bool IsVehicleEnabled()
        {
            return _isEnabled;
        }

        // Public getter for the Interactable vehicle
        public GameObject GetInteractableVehicle()
        {
            return _interactableVehicle;
        }

        // Public getter for the MVC vehicle
        public GameObject GetMVCVehicle()
        {
            return _mvcVehicle;
        }
    }
}
