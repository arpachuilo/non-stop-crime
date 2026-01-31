using Godot;
using System.Collections.Generic;

public partial class NPCSpawner : Node3D {
    [Export] public PackedScene NPCScene { get; set; }
    [Export] public float SpawnDelay { get; set; } = 10.0f;
    [Export] public float SpawnHeight { get; set; } = 20.0f;
    [Export] public Vector3 SpawnAreaSize { get; set; } = new(20, 0, 20);
    [Export] public Vector3 GoalZoneSize { get; set; } = new(2, 1, 2);
    [Export] public Color GoalZoneColor { get; set; } = new Color(1f, 0.5f, 0f, 0.5f);
    [Export] public int MaxPlacementAttempts { get; set; } = 30;
    [Export] public float MinDistanceFromPlayerSpawns { get; set; } = 5f;
    [Export] public float MinDistanceFromZones { get; set; } = 4.0f;
    [Export] public float CollisionCheckRadius { get; set; } = 1.5f;
    [Export] public Godot.Collections.Array<Node3D> PlayerSpawnLocations { get; set; } = new();

    private List<NPC> _spawnedNPCs = new();
    private RandomNumberGenerator _rng = new();
    private Timer _spawnTimer;
    private Poller _lifetimePoller = new(5.0f);

    public override void _Ready() {
        _rng.Randomize();
        _lifetimePoller.Interval = SpawnDelay;
    }

  public override void _Process(double delta) {
    base._Process(delta);
    _lifetimePoller?.Poll(SpawnNPC);
  }

    // private void StartSpawnTimer()
    // {
    //     _spawnTimer = new Timer
    //     {
    //         WaitTime = SpawnDelay,
    //         Autostart = true
    //     };
    //     _spawnTimer.Timeout += OnSpawnTimerTimeout;
    //     AddChild(_spawnTimer);
    // }

    // private void OnSpawnTimerTimeout()
    // {
    //     SpawnNPC();
    //     _spawnTimer.QueueFree();
    // }

    public void SpawnNPC() {
        if (NPCScene == null) {
            GD.PrintErr("NPCSpawner: NPCScene is not set!");
            return;
        }

        var position = FindValidPosition();
        var spawnPosition = GlobalPosition + position;
        spawnPosition.Y = SpawnHeight;

        var npc = NPCScene.Instantiate<NPC>();
        npc.GlobalPosition = spawnPosition;

        var goalZone = CreateGoalZone(npc);
        npc.AddChild(goalZone);

        GetTree().Root.AddChild(npc);
        _spawnedNPCs.Add(npc);

        npc.TreeExiting += () => OnNPCRemoved(npc);

        GD.Print($"NPCSpawner: Spawned NPC at {spawnPosition}");
    }

    private GoalZone CreateGoalZone(NPC npc) {
        var goalZone = new GoalZone {
            ZoneSize = GoalZoneSize,
            NeutralColor = GoalZoneColor,
            Position = Vector3.Zero
        };
        goalZone.AddToGroup("goal_zones");

        goalZone.Captured += (player) => OnGoalCaptured(npc, player);

        return goalZone;
    }

    private void OnGoalCaptured(NPC npc, Player player) {
        npc.OnGoalCaptured(player);
    }

    private void OnNPCRemoved(NPC npc) {
        _spawnedNPCs.Remove(npc);
    }

    private Vector3 FindValidPosition() {
        Vector3 bestPosition = GenerateRandomPosition();
        float bestScore = 0f;

        for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++) {
            Vector3 candidate = GenerateRandomPosition();
            Vector3 worldCandidate = GlobalPosition + candidate;

            if (IsPositionNearPlayerSpawn(worldCandidate))
                continue;

            if (IsPositionNearGoalZone(worldCandidate))
                continue;

            if (IsPositionCollidingWithBody(worldCandidate))
                continue;

            float minDistToNPCs = GetMinDistanceToActiveNPCs(worldCandidate);

            if (minDistToNPCs >= MinDistanceFromZones) {
                return candidate;
            }

            if (minDistToNPCs > bestScore) {
                bestScore = minDistToNPCs;
                bestPosition = candidate;
            }
        }

        return bestPosition;
    }

    private Vector3 GenerateRandomPosition() {
        float x = _rng.RandfRange(-SpawnAreaSize.X / 2f, SpawnAreaSize.X / 2f);
        float z = _rng.RandfRange(-SpawnAreaSize.Z / 2f, SpawnAreaSize.Z / 2f);
        return new Vector3(x, 0, z);
    }

    private bool IsPositionNearPlayerSpawn(Vector3 worldPosition) {
        foreach (var spawn in PlayerSpawnLocations) {
            if (spawn == null) continue;
            float dist = worldPosition.DistanceTo(spawn.GlobalPosition);
            if (dist < MinDistanceFromPlayerSpawns)
                return true;
        }
        return false;
    }

    private bool IsPositionNearGoalZone(Vector3 worldPosition) {
        var zones = GetTree().GetNodesInGroup("goal_zones");

        foreach (var node in zones) {
            if (node is Node3D zone) {
                float dist = worldPosition.DistanceTo(zone.GlobalPosition);
                if (dist < MinDistanceFromZones)
                    return true;
            }
        }
        return false;
    }

    private float GetMinDistanceToActiveNPCs(Vector3 worldPosition) {
        if (_spawnedNPCs.Count == 0)
            return float.MaxValue;

        float minDist = float.MaxValue;
        foreach (var npc in _spawnedNPCs) {
            if (!IsInstanceValid(npc)) continue;
            float dist = worldPosition.DistanceTo(npc.GlobalPosition);
            if (dist < minDist)
                minDist = dist;
        }
        return minDist;
    }

    private bool IsPositionCollidingWithBody(Vector3 worldPosition) {
        var spaceState = GetWorld3D().DirectSpaceState;
        if (spaceState == null)
            return false;

        var shape = new SphereShape3D();
        shape.Radius = CollisionCheckRadius;

        var query = new PhysicsShapeQueryParameters3D();
        query.Shape = shape;
        query.Transform = new Transform3D(Basis.Identity, worldPosition);
        query.CollideWithBodies = true;
        query.CollideWithAreas = false;

        var results = spaceState.IntersectShape(query, 1);
        return results.Count > 0;
    }
}
