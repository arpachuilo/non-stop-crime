using Godot;

public partial class PlayerCorpse : Node3D {
  private static Texture2D _deathTexture;

  public Color Color { get; set; } = Colors.White;

  public override void _Ready() {
    _deathTexture ??= GD.Load<Texture2D>("res://player/assets/death.png");
    GD.Print($"PlayerCorpse spawned at {GlobalPosition} with color {Color}, texture loaded: {_deathTexture != null}");
    CreateSprite();
  }

  private void CreateSprite() {
    var sprite = new Sprite3D();
    sprite.Texture = _deathTexture;
    sprite.Billboard = BaseMaterial3D.BillboardModeEnum.FixedY;
    sprite.PixelSize = 0.01f;
    sprite.Position = new Vector3(0, 2.0f, 0);
    sprite.Modulate = Color;
    sprite.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
    AddChild(sprite);
  }
}
