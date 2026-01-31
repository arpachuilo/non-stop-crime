using Godot;
using System.Collections.Generic;

public partial class GoalZoneSpawner : Node3D
{
    [Export] public int ZoneCount { get; set; } = 6;
    [Export] public Vector3 SpawnAreaSize { get; set; } = new Vector3(16, 0, 16);
    [Export] public float MinDistance { get; set; } = 5f;
    [Export] public int MaxPlacementAttempts { get; set; } = 30;
    [Export] public string MaskDataDirectory { get; set; } = "res://src/Mask/MaskData";
    [Export] public PackedScene GoalZoneScene { get; set; }

    private List<MaskData> _availableMasks = new();
    private List<Vector3> _placedPositions = new();
    private RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        _rng.Randomize();
        LoadMasks();
        SpawnZones();
    }

    private void LoadMasks()
    {
        var dir = DirAccess.Open(MaskDataDirectory);
        if (dir == null)
        {
            GD.PrintErr($"GoalZoneSpawner: Could not open directory {MaskDataDirectory}");
            return;
        }

        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (fileName != "")
        {
            if (!dir.CurrentIsDir() && fileName.EndsWith(".tres"))
            {
                string path = $"{MaskDataDirectory}/{fileName}";
                var resource = GD.Load<MaskData>(path);
                if (resource != null && resource.MaskBits != 0)
                {
                    _availableMasks.Add(resource);
                    GD.Print($"GoalZoneSpawner: Loaded mask '{resource.Name}' with bits {resource.MaskBits}");
                }
            }
            fileName = dir.GetNext();
        }
        dir.ListDirEnd();

        if (_availableMasks.Count == 0)
        {
            GD.PrintErr("GoalZoneSpawner: No masks with MaskBits != 0 found");
        }
    }

    private void SpawnZones()
    {
        if (GoalZoneScene == null)
        {
            GD.PrintErr("GoalZoneSpawner: GoalZoneScene not assigned");
            return;
        }

        for (int i = 0; i < ZoneCount; i++)
        {
            Vector3 position = FindValidPosition();
            _placedPositions.Add(position);

            var zone = GoalZoneScene.Instantiate<GoalZone>();
            zone.Position = position;

            // Assign random mask if available
            if (_availableMasks.Count > 0)
            {
                int maskIndex = _rng.RandiRange(0, _availableMasks.Count - 1);
                zone.Mask = _availableMasks[maskIndex].MaskBits;
                GD.Print($"GoalZoneSpawner: Zone {i} at {position} assigned mask bits {zone.Mask}");
            }

            AddChild(zone);
            zone.AddToGroup("goal_zones");
        }

        GD.Print($"GoalZoneSpawner: Spawned {ZoneCount} goal zones");
    }

    private Vector3 FindValidPosition()
    {
        Vector3 bestPosition = GenerateRandomPosition();
        float bestMinDistance = 0f;

        for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
        {
            Vector3 candidate = GenerateRandomPosition();
            float minDistToExisting = GetMinDistanceToPlaced(candidate);

            if (minDistToExisting >= MinDistance)
            {
                return candidate;
            }

            if (minDistToExisting > bestMinDistance)
            {
                bestMinDistance = minDistToExisting;
                bestPosition = candidate;
            }
        }

        // Return best effort position if no valid one found
        return bestPosition;
    }

    private Vector3 GenerateRandomPosition()
    {
        float x = _rng.RandfRange(-SpawnAreaSize.X / 2f, SpawnAreaSize.X / 2f);
        float z = _rng.RandfRange(-SpawnAreaSize.Z / 2f, SpawnAreaSize.Z / 2f);
        return new Vector3(x, SpawnAreaSize.Y, z);
    }

    private float GetMinDistanceToPlaced(Vector3 position)
    {
        if (_placedPositions.Count == 0)
            return float.MaxValue;

        float minDist = float.MaxValue;
        foreach (var placed in _placedPositions)
        {
            float dist = position.DistanceTo(placed);
            if (dist < minDist)
                minDist = dist;
        }
        return minDist;
    }
}
