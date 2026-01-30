using Godot;

using GodotRay3D = Godot.Collections.Dictionary;

/// <summary>
/// Helper for dealing with Rayast results in typesafe way
/// Use by adding `using GodotRay3D = Godot.Collections.Dictionary;`
/// </summary>
public static class Ray3D {
  public static GodotObject Collider(this GodotRay3D result) {
    return result["collider"].AsGodotObject();
  }

  public static int ColliderID(this GodotRay3D result) {
    return result["collider_id"].AsInt32();
  }

  public static Vector3 Normal(this GodotRay3D result) {
    return result["normal"].AsVector3();
  }

  public static Vector3 Position(this GodotRay3D result) {
    return result["position"].AsVector3();
  }

  public static Rid Rid(this GodotRay3D result) {
    return result["rid"].AsRid();
  }

  public static int Shape(this GodotRay3D result) {
    return result["shape"].AsInt32();
  }

  public static int FaceIndex(this GodotRay3D result) {
    return result["face_index"].AsInt32();
  }

  public static bool IsEmpty(this GodotRay3D result) {
    return result.Count == 0;
  }
}
