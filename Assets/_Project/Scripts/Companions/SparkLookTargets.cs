using System.Collections.Generic;
using ProjectSparks.Player;
using UnityEngine;

namespace ProjectSparks.Companions
{
    public sealed class SparkLookTargets : ISparkLookTargets
    {
        private readonly List<SparkInteractable> _sparks = new();
        private PlayerLookTarget _player;

        public Transform PlayerTarget => _player != null ? _player.Target : null;
        public IReadOnlyList<SparkInteractable> Sparks => _sparks;

        public void RegisterPlayer(PlayerLookTarget player)
        {
            _player = player;
        }

        public void UnregisterPlayer(PlayerLookTarget player)
        {
            if (_player == player)
            {
                _player = null;
            }
        }

        public void RegisterSpark(SparkInteractable spark)
        {
            if (!_sparks.Contains(spark))
            {
                _sparks.Add(spark);
            }
        }

        public void UnregisterSpark(SparkInteractable spark)
        {
            _sparks.Remove(spark);
        }
    }
}
