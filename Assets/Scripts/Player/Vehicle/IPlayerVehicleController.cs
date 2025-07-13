using UnityEngine;
using ZombieGame.Vehicle;

namespace ZombieGame.Player.Vehicle
{
    public interface IPlayerVehicleController
    {
        /// <summary>
        /// Enter the specified vehicle
        /// </summary>
        /// <param name="vehicle">The vehicle GameObject to enter</param>
        void EnterVehicle(GameObject vehicle);
        
        /// <summary>
        /// Exit the current vehicle
        /// </summary>
        void ExitVehicle();
        
        /// <summary>
        /// Check if the player is currently in a vehicle
        /// </summary>
        /// <returns>True if player is in a vehicle, false otherwise</returns>
        bool IsInVehicle();
        
        /// <summary>
        /// Get the current vehicle controller
        /// </summary>
        /// <returns>The current vehicle GameObject or null if not in a vehicle</returns>
        GameObject GetCurrentVehicle();
    }
}
