using Godot;
using System.Collections.Generic;

public partial class MaskSpawner : Node3D
{
    [Export] public Vector3 SpawnAreaSize { get; set; } = new(10, 0, 10);
    [Export] public float RespawnDelay { get; set; } = 5.0f;
    [Export] public string MaskDataDirectory { get; set; } = "res://src/Mask/MaskData";
    [Export] public int MaxActiveMasks { get; set; } = 3;
    [Export] public PackedScene MaskPickupScene { get; set; }
    [Export] public float MinDistance { get; set; } = 3.0f;
    [Export] public float MinDistanceFromZones { get; set; } = 4.0f;
    [Export] public int MaxPlacementAttempts { get; set; } = 30;

    private List<MaskData> _availableMasks = new();
    private Dictionary<MaskPickup, Vector3> _activeMaskPositions = new();
    private RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        _rng.Randomize();
        LoadMasksFromDirectory();

        // Initial spawn
        for (int i = 0; i < MaxActiveMasks; i++)
            SpawnRandomMask();
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

    private void SpawnRandomMask()
    {
        if (_availableMasks.Count == 0)
        {
            GD.PrintErr("MaskSpawner: No masks found in directory");
            return;
        }

        var position = FindValidPosition();
        var maskData = GetRandomMaskData();

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
        _activeMaskPositions[pickup] = pickup.GlobalPosition;
        pickup.PickedUp += () => OnMaskPickedUp(pickup);
        AddChild(pickup);
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
        if (_activeMaskPositions.Count == 0)
            return float.MaxValue;

        float minDist = float.MaxValue;
        foreach (var pos in _activeMaskPositions.Values)
        {
            float dist = worldPosition.DistanceTo(pos);
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

    private MaskData GetRandomMaskData()
    {
        if (_availableMasks.Count == 0) return null;
        return _availableMasks[GD.RandRange(0, _availableMasks.Count - 1)];
    }

    private void OnMaskPickedUp(MaskPickup pickup)
    {
        _activeMaskPositions.Remove(pickup);
        GetTree().CreateTimer(RespawnDelay).Timeout += SpawnRandomMask;
    }
}
