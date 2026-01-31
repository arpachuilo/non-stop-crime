using Godot;
using System.Collections.Generic;

public partial class MaskSpawner : Node3D
{
    [Export] public Vector3 SpawnAreaSize { get; set; } = new(10, 0, 10);
    [Export] public float RespawnDelay { get; set; } = 5.0f;
    [Export] public string MaskDataDirectory { get; set; } = "res://src/Mask/MaskData";
    [Export] public int MaxActiveMasks { get; set; } = 3;
    [Export] public PackedScene MaskPickupScene { get; set; }

    private List<MaskData> _availableMasks = new();
    private int _activeMasks = 0;

    public override void _Ready()
    {
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

        var position = GetRandomSpawnPosition();
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
        pickup.PickedUp += OnMaskPickedUp;
        AddChild(pickup);
        _activeMasks++;
    }

    private Vector3 GetRandomSpawnPosition()
    {
        return new Vector3(
            (float)GD.RandRange(-SpawnAreaSize.X / 2, SpawnAreaSize.X / 2),
            0,
            (float)GD.RandRange(-SpawnAreaSize.Z / 2, SpawnAreaSize.Z / 2)
        );
    }

    private MaskData GetRandomMaskData()
    {
        if (_availableMasks.Count == 0) return null;
        return _availableMasks[GD.RandRange(0, _availableMasks.Count - 1)];
    }

    private void OnMaskPickedUp()
    {
        _activeMasks--;
        GetTree().CreateTimer(RespawnDelay).Timeout += SpawnRandomMask;
    }
}
