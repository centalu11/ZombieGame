using System;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

namespace ZombieGame.Core
{
    public class PlayerRegistry : MonoBehaviour
    {
        public static PlayerRegistry Instance { get; private set; }

        private readonly List<GameObject> _players = new List<GameObject>();
        public IReadOnlyList<GameObject> Players => _players;
        
        /// <summary>
        /// Helper accessor to get all currently tracked players.
        /// Returns a read-only view of the internal list.
        /// </summary>
        public IReadOnlyList<GameObject> GetAllPlayers()
        {
            return Players;
        }
        
        // Events
        public event Action<GameObject> PlayerAdded;
        public event Action<GameObject> PlayerRemoved;
        public event Action<IReadOnlyList<GameObject>> PlayersChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void AddPlayer(GameObject player)
        {
            if (!_players.Contains(player))
            {
                _players.Add(player);
                PlayerAdded?.Invoke(player);
                PlayersChanged?.Invoke(Players);
            }
        }

        public void RemovePlayer(GameObject player)
        {
            if (_players.Contains(player))
            {
                _players.Remove(player);
                PlayerRemoved?.Invoke(player);
                PlayersChanged?.Invoke(Players);
            }
        }
    }
}