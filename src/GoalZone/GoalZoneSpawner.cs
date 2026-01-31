using Godot;
using System.Collections.Generic;

public partial class GoalZoneSpawner : Node3D {
  [Export] public Vector3 SpawnAreaSize { get; set; } = new Vector3(16, 0, 16);
  [Export] public float MinDistance { get; set; } = 5f;
  [Export] public int MaxPlacementAttempts { get; set; } = 30;
  [Export] public float MinDistanceFromPlayerSpawns { get; set; } = 5f;
  [Export] public Godot.Collections.Array<Node3D> PlayerSpawnLocations { get; set; } = new();
  [Export] public string PropDirectory { get; set; } = "res://levels/props";
  [Export] public PackedScene GoalZoneScene { get; set; }
  [Export] public MaskDataArray _availableMasks = new();
  [Export] public bool SpawnOnReady { get; set; } = true;

  private List<PackedScene> _propScenes = new();
  private List<Vector3> _placedPositions = new();
  private RandomNumberGenerator _rng = new();

  public override void _Ready() {
    _rng.Randomize();
    LoadProps();

    if (SpawnOnReady)
      SpawnPropsWithZones();
  }

  public void StartSpawning() {
    SpawnPropsWithZones();
  }

  private void LoadProps() {
    var dir = DirAccess.Open(PropDirectory);
    if (dir == null) {
      GD.PrintErr($"GoalZoneSpawner: Could not open directory {PropDirectory}");
      return;
    }

    dir.ListDirBegin();
    string fileName = dir.GetNext();
    while (fileName != "") {
      if (!dir.CurrentIsDir() && fileName.EndsWith(".tscn")) {
        string path = $"{PropDirectory}/{fileName}";
        var scene = GD.Load<PackedScene>(path);
        if (scene != null) {
          _propScenes.Add(scene);
          GD.Print($"GoalZoneSpawner: Loaded prop from {path}");
        }
      }
      fileName = dir.GetNext();
    }
    dir.ListDirEnd();

    GD.Print($"GoalZoneSpawner: Loaded {_propScenes.Count} props from {PropDirectory}");
  }

  private void SpawnPropsWithZones() {
    if (GoalZoneScene == null) {
      GD.PrintErr("GoalZoneSpawner: GoalZoneScene not assigned");
      return;
    }

    int maskIndex = 0;
    foreach (var propScene in _propScenes) {
      Vector3 position = FindValidPosition();
      _placedPositions.Add(position);

      // Spawn prop
      var prop = propScene.Instantiate<Node3D>();
      prop.Position = position;
      AddChild(prop);

      // Create and attach goal zone
      var zone = GoalZoneScene.Instantiate<GoalZone>();
      zone.Position = Vector3.Zero;

      // Assign mask if available
      if (_availableMasks.Masks.Count > 0) {
        zone.Mask = _availableMasks.Masks[maskIndex % _availableMasks.Masks.Count].MaskBits;
        maskIndex++;
      }

      prop.AddChild(zone);
      zone.AddToGroup("goal_zones");

      GD.Print($"GoalZoneSpawner: Spawned prop at {position} with goal zone (mask bits: {zone.Mask})");
    }

    GD.Print($"GoalZoneSpawner: Spawned {_propScenes.Count} props with goal zones");
  }

  private Vector3 FindValidPosition() {
    Vector3 bestPosition = GenerateRandomPosition();
    float bestMinDistance = 0f;

    for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++) {
      Vector3 candidate = GenerateRandomPosition();

      if (IsPositionNearPlayerSpawn(candidate))
        continue;

      float minDistToExisting = GetMinDistanceToPlaced(candidate);

      if (minDistToExisting >= MinDistance) {
        return candidate;
      }

      if (minDistToExisting > bestMinDistance) {
        bestMinDistance = minDistToExisting;
        bestPosition = candidate;
      }
    }

    return bestPosition;
  }

  private bool IsPositionNearPlayerSpawn(Vector3 position) {
    Vector3 worldPosition = GlobalPosition + position;
    foreach (var spawn in PlayerSpawnLocations) {
      if (spawn == null) continue;
      float dist = worldPosition.DistanceTo(spawn.GlobalPosition);
      if (dist < MinDistanceFromPlayerSpawns)
        return true;
    }
    return false;
  }

  private Vector3 GenerateRandomPosition() {
    float x = _rng.RandfRange(-SpawnAreaSize.X / 2f, SpawnAreaSize.X / 2f);
    float z = _rng.RandfRange(-SpawnAreaSize.Z / 2f, SpawnAreaSize.Z / 2f);
    return new Vector3(x, SpawnAreaSize.Y, z);
  }

  private float GetMinDistanceToPlaced(Vector3 position) {
    if (_placedPositions.Count == 0)
      return float.MaxValue;

    float minDist = float.MaxValue;
    foreach (var placed in _placedPositions) {
      float dist = position.DistanceTo(placed);
      if (dist < minDist)
        minDist = dist;
    }
    return minDist;
  }
}
