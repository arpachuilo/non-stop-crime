using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Steamworks;

using MembersList = Godot.Collections.Array<Godot.Collections.Dictionary>;

/// <summary>
/// Extension methods for converting between Member arrays and Godot-compatible MembersList.
/// Used for RPC serialization of member data across the network.
/// </summary>
public static class MembersListExtensions {
  /// <summary>
  /// Converts a Member array to a Godot-compatible MembersList for RPC transmission.
  /// </summary>
  /// <param name="members">Array of Member objects to convert</param>
  /// <returns>MembersList suitable for RPC calls</returns>
  public static MembersList ToMembersList(this Member[] members) {
    var list = new MembersList();
    foreach (var member in members) {
      list.Add(member.ToDictionary());
    }
    return list;
  }

  /// <summary>
  /// Converts a Godot MembersList back to a Member array after RPC reception.
  /// </summary>
  /// <param name="list">MembersList received from RPC</param>
  /// <returns>Array of reconstructed Member objects</returns>
  public static Member[] FromMembersList(this MembersList list) {
    var members = new Member[list.Count];
    for (int i = 0; i < list.Count; i++) {
      members[i] = Member.FromDictionary(list[i]);
    }
    return members;
  }
}

/// <summary>
/// Represents a player in the multiplayer lobby.
///
/// Contains
/// - Player identification (peer ID, Steam ID)
/// - Display information (name, avatar),
/// - Role status (host/client).
///
/// Avatars are cached per peer ID to avoid redundant loading
/// Steam avatars are loaded asynchronously and fall back to generated avatars
/// </summary>
public class Member {
  /// <summary>
  /// Display name of the player. Either Steam name or manually set name for ENet
  /// </summary>
  public string Name;

  /// <summary>
  /// Steam ID of the player. 0 if using ENet connection.
  /// </summary>
  public ulong SteamID;

  /// <summary>
  /// Godot multiplayer peer ID. Unique identifier for this connection.
  /// </summary>
  public int PeerID;

  /// <summary>
  /// Whether this member is the host of the lobby.
  /// </summary>
  public bool IsHost;

  /// <summary>
  /// Static cache mapping peer IDs to 128x128 avatar textures.
  /// Shared across all Member instances to avoid redundant loading.
  /// </summary>
  private static readonly Dictionary<int, Texture2D> _avatarCache = [];

  /// <summary>
  /// Static cache mapping peer IDs to 32x32 avatar textures.
  /// Shared across all Member instances to avoid redundant loading.
  /// </summary>
  private static readonly Dictionary<int, Texture2D> _avatarSmallCache = [];

  /// <summary>
  /// Large avatar texture (128x128) for this member.
  /// </summary>
  public Texture2D Avatar;

  /// <summary>
  /// Small avatar texture (32x32) for this member.
  /// </summary>
  public Texture2D AvatarSmall;

  /// <summary>
  /// Attempts to load avatar textures for this member.
  ///
  /// Uses cached avatars if available to avoid redundant loading.
  /// For Steam members, attempts to load Steam avatar asynchronously.
  /// Falls back to generated avatar if Steam avatar unavailable or not a Steam member.
  ///
  /// Creates both large (128x128) and small (32x32) versions and caches them.
  /// </summary>
  /// <param name="steamMember">Optional Steam Friend data for loading Steam avatar</param>
  public async Task TryLoadAvatar(Friend? steamMember) {
    if (_avatarCache.ContainsKey(PeerID) && _avatarSmallCache.ContainsKey(PeerID)) {
      SetAvatarsFromCache();
      return;
    }

    Image image = null;
    if (steamMember.HasValue) {
      image = await TryLoadSteamAvatar(steamMember.Value);
    }

    image ??= AvatarGenerator.NextAvatar(Name);

    if (image != null) {
      SetAvatars(image);
    }
  }

  /// <summary>
  /// Loads avatar textures from the static cache.
  /// </summary>
  private void SetAvatarsFromCache() {
    Avatar = _avatarCache[PeerID];
    AvatarSmall = _avatarSmallCache[PeerID];
  }

  /// <summary>
  /// Creates avatar textures from an image and caches them.
  ///
  /// Generates two versions: 128x128 for main avatar, 32x32 for small avatar.
  /// Both versions are stored in static caches for reuse.
  /// </summary>
  /// <param name="image">Source image to create avatars from</param>
  private void SetAvatars(Image image) {
    var main = new Image();
    main.CopyFrom(image);
    main.Resize(128, 128);
    Avatar = ImageTexture.CreateFromImage(main);
    _avatarCache[PeerID] = Avatar;

    var small = new Image();
    small.CopyFrom(image);
    small.Resize(32, 32);
    AvatarSmall = ImageTexture.CreateFromImage(small);
    _avatarSmallCache[PeerID] = AvatarSmall;
  }

  /// <summary>
  /// Attempts to asynchronously load a Steam avatar image.
  /// </summary>
  /// <param name="steamMember">Steam Friend to load avatar for</param>
  /// <returns>Godot Image if successful, null if avatar unavailable</returns>
  private async Task<Image> TryLoadSteamAvatar(Friend steamMember) {
    var swImage = await steamMember.GetLargeAvatarAsync();
    if (swImage == null) return null;

    return Image.CreateFromData(
        (int)swImage.Value.Width,
        (int)swImage.Value.Height,
        false,
        Image.Format.Rgba8,
        swImage.Value.Data
    );
  }

  /// <summary>
  /// Serializes this Member to a Godot Dictionary for RPC transmission.
  ///
  /// Note: Avatar textures are not serialized. They must be loaded separately on each client.
  /// </summary>
  /// <returns>Dictionary containing member data</returns>
  public Godot.Collections.Dictionary ToDictionary() {
    return new Godot.Collections.Dictionary
    {
            { "Name", Name },
            { "SteamID", SteamID },
            { "PeerID", PeerID },
            { "IsHost", IsHost },
        };
  }

  /// <summary>
  /// Deserializes a Member from a Godot Dictionary received via RPC.
  ///
  /// Note: Avatar textures are not included and must be loaded via TryLoadAvatar().
  /// </summary>
  /// <param name="dict">Dictionary containing serialized member data</param>
  /// <returns>Reconstructed Member object</returns>
  public static Member FromDictionary(Godot.Collections.Dictionary dict) {
    return new Member {
      Name = dict["Name"].AsString(),
      SteamID = dict["SteamID"].AsUInt64(),
      PeerID = dict["PeerID"].AsInt32(),
      IsHost = dict["IsHost"].AsBool()
    };
  }

  /// <summary>
  /// Returns a string representation of this member for debugging.
  /// </summary>
  /// <returns>Formatted string with member information</returns>
  public override string ToString() {
    return $"{Name} (IsHost: {IsHost}, SteamID: {SteamID}, PeerID: {PeerID})";
  }
}
