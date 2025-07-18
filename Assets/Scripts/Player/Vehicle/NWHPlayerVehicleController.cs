using UnityEngine;
using ZombieGame.Vehicle;
using ZombieGame.Core;

namespace ZombieGame.Player.Vehicle
{
    public class NWHPlayerVehicleController : MonoBehaviour, IPlayerVehicleController
    {
        private void Awake()
        {
            // Register this implementation in the service locator
            ServiceLocator.Register<IPlayerVehicleController>(this);
        }

        private void OnDestroy()
        {
            // Unregister when destroyed
            ServiceLocator.Unregister<IPlayerVehicleController>();
        }

        public void EnterVehicle(GameObject vehicle)
        {
            throw new System.NotImplementedException();
        }

        public void ExitVehicle()
        {

        }

        public bool IsInVehicle()
        {
            return false;
        }

        public GameObject GetCurrentVehicle()
        {
            return null;
        }
    }
}