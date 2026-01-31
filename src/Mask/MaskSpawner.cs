using Godot;
using System.Collections.Generic;

public partial class MaskSpawner : Node3D
{
    [Export] public Vector3 SpawnAreaSize { get; set; } = new(10, 0, 10);
    [Export] public PackedScene MaskPickupScene { get; set; }
    [Export] public float MinDistance { get; set; } = 3.0f;
    [Export] public float MinDistanceFromZones { get; set; } = 4.0f;
    [Export] public int MaxPlacementAttempts { get; set; } = 30;
    [Export] public float MinDistanceFromPlayerSpawns { get; set; } = 5f;
    [Export] public Godot.Collections.Array<Node3D> PlayerSpawnLocations { get; set; } = new();
    [Export] public MaskDataArray _availableMasks = new();

    private Dictionary<MaskData, MaskPickup> _spawnedPickups = new();
    private Dictionary<MaskData, Player> _equippedByPlayer = new();
    private RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        _rng.Randomize();
        CallDeferred(nameof(InitializeSpawning));
    }

    private void InitializeSpawning()
    {
        SubscribeToPlayerResets();
        foreach (var maskData in _availableMasks.Masks)
            SpawnMask(maskData);
    }

    private void SubscribeToPlayerResets()
    {
        var players = GetTree().GetNodesInGroup("Player");
        foreach (var node in players)
        {
            if (node is Player player)
                player.PlayerReset += () => OnPlayerReset(player);
        }
    }

    private void SpawnMask(MaskData maskData)
    {
        var position = FindValidPosition();

        MaskPickup pickup;
        if (MaskPickupScene != null)
        {
            pickup = MaskPickupScene.Instantiate<MaskPickup>();
        }
        else
        {
            pickup = new MaskPickup();
        }

        pickup.MaskData = maskData;
        pickup.Position = Position + position;
        pickup.PickedUp += (player) => OnMaskPickedUp(maskData, player);
        AddChild(pickup);

        _spawnedPickups[maskData] = pickup;
        GD.Print($"MaskSpawner: Spawned mask");
    }

    private Vector3 FindValidPosition()
    {
        Vector3 bestPosition = GenerateRandomPosition();
        float bestScore = 0f;

        for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
        {
            Vector3 candidate = GenerateRandomPosition();
            Vector3 worldCandidate = Position + candidate;

            if (IsPositionNearPlayerSpawn(worldCandidate))
                continue;

            if (IsPositionNearGoalZone(worldCandidate))
                continue;

            float minDistToMasks = GetMinDistanceToActiveMasks(worldCandidate);

            if (minDistToMasks >= MinDistance)
            {
                return candidate;
            }

            if (minDistToMasks > bestScore)
            {
                bestScore = minDistToMasks;
                bestPosition = candidate;
            }
        }

        return bestPosition;
    }

    private bool IsPositionNearPlayerSpawn(Vector3 worldPosition)
    {
        foreach (var spawn in PlayerSpawnLocations)
        {
            if (spawn == null) continue;
            float dist = worldPosition.DistanceTo(spawn.GlobalPosition);
            if (dist < MinDistanceFromPlayerSpawns)
                return true;
        }
        return false;
    }

    private Vector3 GenerateRandomPosition()
    {
        float x = _rng.RandfRange(-SpawnAreaSize.X / 2f, SpawnAreaSize.X / 2f);
        float z = _rng.RandfRange(-SpawnAreaSize.Z / 2f, SpawnAreaSize.Z / 2f);
        return new Vector3(x, 0, z);
    }

    private float GetMinDistanceToActiveMasks(Vector3 worldPosition)
    {
        if (_spawnedPickups.Count == 0)
            return float.MaxValue;

        float minDist = float.MaxValue;
        foreach (var pickup in _spawnedPickups.Values)
        {
            float dist = worldPosition.DistanceTo(pickup.GlobalPosition);
            if (dist < minDist)
                minDist = dist;
        }
        return minDist;
    }

    private bool IsPositionNearGoalZone(Vector3 worldPosition)
    {
        var zones = GetTree().GetNodesInGroup("goal_zones");

        foreach (var node in zones)
        {
            if (node is Node3D zone)
            {
                float dist = worldPosition.DistanceTo(zone.GlobalPosition);
                if (dist < MinDistanceFromZones)
                    return true;
            }
        }
        return false;
    }

    private void OnMaskPickedUp(MaskData maskData, Player player)
    {
        _spawnedPickups.Remove(maskData);
        _equippedByPlayer[maskData] = player;
    }

    private void OnPlayerReset(Player player)
    {
        MaskData maskToRespawn = null;
        foreach (var kvp in _equippedByPlayer)
        {
            if (kvp.Value == player)
            {
                maskToRespawn = kvp.Key;
                break;
            }
        }

        if (maskToRespawn != null)
        {
            _equippedByPlayer.Remove(maskToRespawn);
            SpawnMask(maskToRespawn);
        }
    }
}
