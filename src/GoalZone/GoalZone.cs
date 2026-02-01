using System.Collections.Generic;
using System.ComponentModel;
using Godot;

[Tool]
public partial class GoalZone : Area3D {
  const int NEUTRAL_OWNER_ID = -67;

  [Signal] public delegate void CapturedEventHandler(Player player);

  [Description("Mesh representing the visual size of the goal zone")]
  [Export]
  public MeshInstance3D VisualMesh;

  [Description("Collision shape for the goal zone that triggers captures")]
  [Export]
  public CollisionShape3D CollisionShape;

  [Export]
  public Color NeutralColor { get; set; } = new Color(0.5f, 0.5f, 0.5f, 0.5f);

  [Export]
  public Vector3 ZoneSize { get; set; } = new Vector3(3, 1, 3);

  [Export]
  public int Mask { get; set; } = 0;

  [Export]
  public float CaptureTime { get; set; } = 5.0f;

  [Export]
  public float CaptureLockTime { get; set; } = 15.0f;

  [Export]
  public float PointInterval { get; set; } = 5.0f;

  [Export]
  public int PointsPerInterval { get; set; } = 2;

  [Export]
  public float PopupDuration { get; set; } = 2.0f;

  [Export]
  public float PopupHeight { get; set; } = 3.0f;

  public int OwnerPlayerId { get; private set; } = NEUTRAL_OWNER_ID;
  public bool IsLocked { get; private set; } = false;

  private ShaderMaterial _materialInstance;
  private Player _ownerPlayer = null;
  private Player _capturingPlayer = null;
  private float _captureProgress = 0f;
  private int _partialCapturePlayerId = NEUTRAL_OWNER_ID;
  private float _lockTimeRemaining = 0f;
  private float _pointAccrualTimer = 0f;
  private HashSet<Player> _playersInZone = new();

  public override void _Ready() {
    SetupVisuals();
    SetupCollision();

    BodyEntered += OnBodyEntered;
    BodyExited += OnBodyExited;
  }

  public override void _Process(double delta) {
    if (Engine.IsEditorHint()) return;

    float dt = (float)delta;

    UpdateLockTimer(dt);
    UpdatePointAccrual(dt);
    UpdateCaptureProgress(dt);
  }

  private void UpdateLockTimer(float delta) {
    if (!IsLocked) return;

    _lockTimeRemaining -= delta;
    if (_lockTimeRemaining <= 0f) {
      IsLocked = false;
      GD.Print($"Zone unlocked, can be recaptured");
    }
  }

  private void UpdatePointAccrual(float delta) {
    if (_ownerPlayer == null) return;
    if (!IsInstanceValid(_ownerPlayer)) {
      _ownerPlayer = null;
      return;
    }

    _pointAccrualTimer += delta;
    if (_pointAccrualTimer >= PointInterval) {
      _pointAccrualTimer -= PointInterval;
      _ownerPlayer.AddScore(PointsPerInterval);
      GD.Print($"Zone awarded {PointsPerInterval} points to Player {OwnerPlayerId}");
    }
  }

  private void UpdateCaptureProgress(float delta) {
    // Find a valid capturing player
    Player validCapturer = null;
    bool ownerInZone = false;

    foreach (var player in _playersInZone) {
      if (!IsInstanceValid(player)) continue;

      int playerId = player.PlayerController.DeviceId;

      // Check if owner is in zone
      if (playerId == OwnerPlayerId) {
        ownerInZone = true;
      }

      // Check if this player can capture
      if (playerId != OwnerPlayerId && !IsLocked) {
        // Check mask requirement
        if (Mask == 0 || (player.Mask & Mask) == Mask) {
          validCapturer = player;
        }
      }
    }

    // Check if the partial capture holder is still in zone
    bool partialCapturerInZone = false;
    if (_partialCapturePlayerId != NEUTRAL_OWNER_ID) {
      foreach (var player in _playersInZone) {
        if (IsInstanceValid(player) && player.PlayerController.DeviceId == _partialCapturePlayerId) {
          partialCapturerInZone = true;
          break;
        }
      }
    }

    // Owner defense logic
    if (ownerInZone) {
      if (partialCapturerInZone) {
        // Both owner and capturer present - freeze progress
        return;
      } else {
        // Owner defending, capturer left - decay at 2x speed
        if (_captureProgress > 0f) {
          _captureProgress -= delta * 2f;
          if (_captureProgress <= 0f) {
            ResetCaptureProgress();
          } else {
            UpdateCaptureVisual();
          }
        }
        return;
      }
    }

    // No valid capturer in zone
    if (validCapturer == null) {
      if (_captureProgress > 0f) {
        // Decay at 1x speed
        _captureProgress -= delta;
        if (_captureProgress <= 0f) {
          ResetCaptureProgress();
        } else {
          UpdateCaptureVisual();
        }
      }
      return;
    }

    int capturerId = validCapturer.PlayerController.DeviceId;

    if (_captureProgress <= 0f) {
      // Fresh capture start
      _capturingPlayer = validCapturer;
      _partialCapturePlayerId = capturerId;
      _captureProgress = delta;
      UpdateCaptureVisual();
    } else if (capturerId == _partialCapturePlayerId) {
      // Same player continuing capture
      _capturingPlayer = validCapturer;
      _captureProgress += delta;
      UpdateCaptureVisual();

      if (_captureProgress >= CaptureTime) {
        CompleteClaim(_capturingPlayer);
      }
    } else {
      // Contested - different player, decay at 2x speed
      _captureProgress -= delta * 2f;
      if (_captureProgress <= 0f) {
        // New player takes over
        _captureProgress = delta;
        _partialCapturePlayerId = capturerId;
        _capturingPlayer = validCapturer;
      }
      UpdateCaptureVisual();
    }
  }

  private void ResetCaptureProgress() {
    if (_captureProgress > 0f || _capturingPlayer != null || _partialCapturePlayerId != NEUTRAL_OWNER_ID) {
      _captureProgress = 0f;
      _capturingPlayer = null;
      _partialCapturePlayerId = NEUTRAL_OWNER_ID;
      UpdateCaptureVisual();
    }
  }

  private void SetupVisuals() {
    VisualMesh ??= GetNodeOrNull<MeshInstance3D>("VisualMesh");
    if (VisualMesh == null) return;

    var originalMaterial = VisualMesh.GetActiveMaterial(0) as ShaderMaterial;
    if (originalMaterial == null) {
      GD.PushWarning("GoalZone VisualMesh does not have a ShaderMaterial.");
      return;
    }

    _materialInstance = (ShaderMaterial)originalMaterial.Duplicate();
    VisualMesh.SetSurfaceOverrideMaterial(0, _materialInstance);

    UpdateVisualColor(NeutralColor);
  }

  private void SetupCollision() {
    CollisionShape ??= GetNodeOrNull<CollisionShape3D>("CollisionShape");
  }

  private void OnBodyEntered(Node3D body) {
    if (body is Player player) {
      _playersInZone.Add(player);
    }
  }

  private void OnBodyExited(Node3D body) {
    if (body is Player player) {
      _playersInZone.Remove(player);
    }
  }

  private void CompleteClaim(Player player) {
    int playerId = player.PlayerController.DeviceId;

    OwnerPlayerId = playerId;
    _ownerPlayer = player;
    IsLocked = true;
    _lockTimeRemaining = CaptureLockTime;
    _pointAccrualTimer = 0f;

    ResetCaptureProgress();
    UpdateVisualColor(player.PlayerInfo.UIColor);
    SpawnCapturePopup(player);

    EmitSignal(SignalName.Captured, player);
    GD.Print($"Zone captured by Player {playerId}, locked for {CaptureLockTime}s");
  }

  private void UpdateVisualColor(Color color) {
    if (_materialInstance == null) return;
    _materialInstance.SetShaderParameter("base_color", color);
  }

  private void UpdateCaptureVisual() {
    if (_materialInstance == null) return;

    float progress = Mathf.Clamp(_captureProgress / CaptureTime, 0f, 1f);
    _materialInstance.SetShaderParameter("capture_progress", progress);

    if (_capturingPlayer != null && progress > 0f) {
      _materialInstance.SetShaderParameter("capture_color", _capturingPlayer.PlayerInfo.UIColor);
    }
  }

  private void SpawnCapturePopup(Player player) {
    var popup = new CapturePopup();
    popup.Color = player.PlayerInfo.UIColor;
    popup.Duration = PopupDuration;
    popup.Position = new Vector3(0, PopupHeight, 0);
    AddChild(popup);
  }
}
