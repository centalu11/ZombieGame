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
                _players.Add(player);
        }

        public void RemovePlayer(GameObject player)
        {
            if (_players.Contains(player))
                _players.Remove(player);
        }
    }
}