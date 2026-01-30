using Godot;
using System.Collections.Generic;

[Tool]
public partial class Skyscraper : Node3D {
  [Export] public Texture2D Atlas;

  public enum Variant {
    BuildingSkyscraper1,
    BuildingSkyscraper2,
    BuildingSkyscraper3,
    BuildingSkyscraper4,
    BuildingSkyscraper5
  }

  private Variant _variant;

  [Export]
  public Variant PropVariant {
    get => _variant;
    set {
      _variant = value;
      ApplyVariant();
    }
  }

  // Regions are in x, y, w, h
  private static readonly Dictionary<Variant, Rect2I> Regions = new() {
    { Variant.BuildingSkyscraper1, new Rect2I(100, 126, 60, 258) },
    { Variant.BuildingSkyscraper2, new Rect2I(192, 152, 52, 232) },
    { Variant.BuildingSkyscraper3, new Rect2I(276, 181, 69, 203) },
    { Variant.BuildingSkyscraper4, new Rect2I(376, 224, 50, 160) },
    { Variant.BuildingSkyscraper5, new Rect2I(456, 224, 51, 160) }
  };

  private Sprite3D _sprite;

  public override void _Ready() {
    _sprite = GetNodeOrNull<Sprite3D>("SkyscraperStaticBody/Sprite3D");
    ApplyVariant();
  }

  public override void _EnterTree() {
    if (Engine.IsEditorHint()) {
      ApplyVariant();
    }
  }

  private void ApplyVariant() {
    _sprite ??= GetNodeOrNull<Sprite3D>("SkyscraperStaticBody/Sprite3D");
    if (_sprite == null || Atlas == null) {
      return;
    }

    var region = Regions[_variant];

    var atlasTexture = new AtlasTexture {
      Atlas = Atlas,
      Region = region
    };

    _sprite.Texture = atlasTexture;

    // Keep the *bottom* of the sprite on Y=0, regardless of sprite height.
    float worldHeight = region.Size.Y * _sprite.PixelSize;
    var pos = _sprite.Position;
    pos.Y = worldHeight * 0.5f;
    _sprite.Position = pos;
  }
}
