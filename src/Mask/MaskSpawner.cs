using Godot;
using System.Collections.Generic;

public partial class MaskSpawner : Node3D
{
    [Export] public Vector3 SpawnAreaSize { get; set; } = new(10, 0, 10);
    [Export] public string MaskDataDirectory { get; set; } = "res://src/Mask/MaskData";
    [Export] public PackedScene MaskPickupScene { get; set; }
    [Export] public float MinDistance { get; set; } = 3.0f;
    [Export] public float MinDistanceFromZones { get; set; } = 4.0f;
    [Export] public int MaxPlacementAttempts { get; set; } = 30;

    private List<MaskData> _availableMasks = new();
    private Dictionary<MaskData, MaskPickup> _spawnedPickups = new();
    private Dictionary<MaskData, Player> _equippedByPlayer = new();
    private RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        _rng.Randomize();
        LoadMasksFromDirectory();
        CallDeferred(nameof(InitializeSpawning));
    }

    private void InitializeSpawning()
    {
        SubscribeToPlayerResets();
        foreach (var maskData in _availableMasks)
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

    private void LoadMasksFromDirectory()
    {
        var dir = DirAccess.Open(MaskDataDirectory);
        if (dir == null)
        {
            GD.PrintErr($"MaskSpawner: Could not open directory {MaskDataDirectory}");
            return;
        }

        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (fileName != "")
        {
            if (!dir.CurrentIsDir() && (fileName.EndsWith(".tres") || fileName.EndsWith(".res")))
            {
                string path = $"{MaskDataDirectory}/{fileName}";
                var resource = GD.Load<MaskData>(path);
                if (resource != null)
                {
                    _availableMasks.Add(resource);
                    GD.Print($"MaskSpawner: Loaded mask '{resource.Name}' from {path}");
                }
            }
            fileName = dir.GetNext();
        }
        dir.ListDirEnd();

        GD.Print($"MaskSpawner: Loaded {_availableMasks.Count} masks from {MaskDataDirectory}");
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
        pickup.GlobalPosition = GlobalPosition + position;
        pickup.PickedUp += (player) => OnMaskPickedUp(maskData, player);
        AddChild(pickup);

        _spawnedPickups[maskData] = pickup;
    }

    private Vector3 FindValidPosition()
    {
        Vector3 bestPosition = GenerateRandomPosition();
        float bestScore = 0f;

        for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
        {
            Vector3 candidate = GenerateRandomPosition();
            Vector3 worldCandidate = GlobalPosition + candidate;

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
