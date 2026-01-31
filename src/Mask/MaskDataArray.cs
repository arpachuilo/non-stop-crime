using Godot;

[GlobalClass]
[Tool]
public partial class MaskDataArray : Resource {
  [Export] public Godot.Collections.Array<MaskData> Masks { get; set; } = new();
}
