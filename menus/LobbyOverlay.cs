using Godot;
using System.Collections.Generic;

public partial class LobbyOverlay : Control {
  [Export] Texture2D UnreadyIconTexture;
  [Export] Texture2D ReadyIconTexture;

  [Export] VBoxContainer Player1VBox;
  [Export] VBoxContainer Player2VBox;
  [Export] VBoxContainer Player3VBox;
  [Export] VBoxContainer Player4VBox;

  private TextureRect _player1ReadyIcon;
  private TextureRect _player2ReadyIcon;
  private TextureRect _player3ReadyIcon;
  private TextureRect _player4ReadyIcon;

  private Dictionary<Player, int> PlayerToIndex = [];
  private List<Texture2D> _allPortraits = new();
  private List<Texture2D> _availablePortraits = new();
  private RandomNumberGenerator _rng = new();

  public override void _Ready() {
    _rng.Randomize();

    // Gather existing portrait textures from the BoxChar nodes
    var p1Char = Player1VBox.GetNodeOrNull<TextureRect>("Player1Box/BoxChar");
    var p2Char = Player2VBox.GetNodeOrNull<TextureRect>("Player2Box/BoxChar");
    var p3Char = Player3VBox.GetNodeOrNull<TextureRect>("Player3Box/BoxChar");
    var p4Char = Player4VBox.GetNodeOrNull<TextureRect>("Player4Box/BoxChar");

    if (p1Char?.Texture != null) _allPortraits.Add(p1Char.Texture);
    if (p2Char?.Texture != null) _allPortraits.Add(p2Char.Texture);
    if (p3Char?.Texture != null) _allPortraits.Add(p3Char.Texture);
    if (p4Char?.Texture != null) _allPortraits.Add(p4Char.Texture);

    _availablePortraits = new List<Texture2D>(_allPortraits);

    _player1ReadyIcon = Player1VBox.GetNodeOrNull<TextureRect>("ReadyState");
    _player2ReadyIcon = Player2VBox.GetNodeOrNull<TextureRect>("ReadyState");
    _player3ReadyIcon = Player3VBox.GetNodeOrNull<TextureRect>("ReadyState");
    _player4ReadyIcon = Player4VBox.GetNodeOrNull<TextureRect>("ReadyState");

    Player1VBox.Visible = false;
    Player2VBox.Visible = false;
    Player3VBox.Visible = false;
    Player4VBox.Visible = false;
  }

  public void SetPlayerName(Player player, string name) {
    int playerIndex;
    if (PlayerToIndex.ContainsKey(player)) {
      playerIndex = PlayerToIndex[player];
    } else {
      playerIndex = PlayerToIndex.Count + 1;
      PlayerToIndex[player] = playerIndex;
    }

    switch (playerIndex) {
      case 1:
        var nameLabel1 = Player1VBox.GetNodeOrNull<Label>("PlayerNameLabel");
        if (nameLabel1 != null)
          nameLabel1.Text = name;
        break;
      case 2:
        var nameLabel2 = Player2VBox.GetNodeOrNull<Label>("PlayerNameLabel");
        if (nameLabel2 != null)
          nameLabel2.Text = name;
        break;
      case 3:
        var nameLabel3 = Player3VBox.GetNodeOrNull<Label>("PlayerNameLabel");
        if (nameLabel3 != null)
          nameLabel3.Text = name;
        break;
      case 4:
        var nameLabel4 = Player4VBox.GetNodeOrNull<Label>("PlayerNameLabel");
        if (nameLabel4 != null)
          nameLabel4.Text = name;
        break;
      default:
        GD.PrintErr($"LobbyOverlay::SetPlayerName: Invalid player index: {playerIndex}");
        break;
    }
  }

  public void SetPlayerColor(Player player, Color color) {
    int playerIndex;
    if (PlayerToIndex.ContainsKey(player)) {
      playerIndex = PlayerToIndex[player];
    } else {
      playerIndex = PlayerToIndex.Count + 1;
      PlayerToIndex[player] = playerIndex;
    }

    switch (playerIndex) {
      case 1:
        var p1BoxChar = Player1VBox.GetNodeOrNull<TextureRect>("Player1Box/BoxChar");
        p1BoxChar.Modulate = color;
        break;
      case 2:
        var p2BoxChar = Player2VBox.GetNodeOrNull<TextureRect>("Player2Box/BoxChar");
        p2BoxChar.Modulate = color;
        break;
      case 3:
        var p3BoxChar = Player3VBox.GetNodeOrNull<TextureRect>("Player3Box/BoxChar");
        p3BoxChar.Modulate = color;
        break;
      case 4:
        var p4BoxChar = Player4VBox.GetNodeOrNull<TextureRect>("Player4Box/BoxChar");
        p4BoxChar.Modulate = color;
        break;
      default:
        GD.PrintErr($"LobbyOverlay::SetPlayerColor: Invalid player index: {playerIndex}");
        break;
    }
  }

  public void SetPlayerPortrait(Player player) {
    int playerIndex;
    if (PlayerToIndex.ContainsKey(player)) {
      playerIndex = PlayerToIndex[player];
    } else {
      playerIndex = PlayerToIndex.Count + 1;
      PlayerToIndex[player] = playerIndex;
    }

    var portrait = GetRandomPortrait();
    if (portrait == null) return;

    switch (playerIndex) {
      case 1:
        var p1BoxChar = Player1VBox.GetNodeOrNull<TextureRect>("Player1Box/BoxChar");
        if (p1BoxChar != null) p1BoxChar.Texture = portrait;
        break;
      case 2:
        var p2BoxChar = Player2VBox.GetNodeOrNull<TextureRect>("Player2Box/BoxChar");
        if (p2BoxChar != null) p2BoxChar.Texture = portrait;
        break;
      case 3:
        var p3BoxChar = Player3VBox.GetNodeOrNull<TextureRect>("Player3Box/BoxChar");
        if (p3BoxChar != null) p3BoxChar.Texture = portrait;
        break;
      case 4:
        var p4BoxChar = Player4VBox.GetNodeOrNull<TextureRect>("Player4Box/BoxChar");
        if (p4BoxChar != null) p4BoxChar.Texture = portrait;
        break;
      default:
        GD.PrintErr($"LobbyOverlay::SetPlayerPortrait: Invalid player index: {playerIndex}");
        break;
    }
  }

  private Texture2D GetRandomPortrait() {
    if (_availablePortraits.Count == 0)
      _availablePortraits = new List<Texture2D>(_allPortraits);

    if (_availablePortraits.Count == 0) return null;

    int index = _rng.RandiRange(0, _availablePortraits.Count - 1);
    var portrait = _availablePortraits[index];
    _availablePortraits.RemoveAt(index);
    return portrait;
  }

  public void SetPlayerActiveState(Player player, bool isActive) {
    int playerIndex;
    if (PlayerToIndex.ContainsKey(player)) {
      playerIndex = PlayerToIndex[player];
    } else {
      playerIndex = PlayerToIndex.Count + 1;
      PlayerToIndex[player] = playerIndex;
    }

    switch (playerIndex) {
      case 1:
        Player1VBox.Visible = isActive;
        break;
      case 2:
        Player2VBox.Visible = isActive;
        break;
      case 3:
        Player3VBox.Visible = isActive;
        break;
      case 4:
        Player4VBox.Visible = isActive;
        break;
      default:
        GD.PrintErr($"LobbyOverlay::SetPlayerActiveState: Invalid player index: {playerIndex}");
        break;
    }
  }

  public void SetPlayerReadyState(Player player, bool isReady) {
    int playerIndex;

    if (PlayerToIndex.ContainsKey(player)) {
      playerIndex = PlayerToIndex[player];
    } else {
      playerIndex = PlayerToIndex.Count + 1;
      PlayerToIndex[player] = playerIndex;
    }

    Texture2D iconTexture = isReady ? ReadyIconTexture : UnreadyIconTexture;

    switch (playerIndex) {
      case 1:
        if (_player1ReadyIcon != null)
          _player1ReadyIcon.Texture = iconTexture;
        break;
      case 2:
        if (_player2ReadyIcon != null)
          _player2ReadyIcon.Texture = iconTexture;
        break;
      case 3:
        if (_player3ReadyIcon != null)
          _player3ReadyIcon.Texture = iconTexture;
        break;
      case 4:
        if (_player4ReadyIcon != null)
          _player4ReadyIcon.Texture = iconTexture;
        break;
      default:
        GD.PrintErr($"LobbyOverlay::SetPlayerReadyState: Invalid player index: {playerIndex}");
        break;
    }
  }
}
