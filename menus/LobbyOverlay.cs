using Godot;
using System;

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

  public void SetPlayerActiveState(int playerIndex, bool isActive) {
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

  public void SetPlayerReadyState(int playerIndex, bool isReady) {
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
