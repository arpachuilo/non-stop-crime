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

  public override void _Ready() {
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
