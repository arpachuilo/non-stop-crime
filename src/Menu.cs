using Godot;
using Steam;
using Steamworks;

public partial class Menu : Control {
  [Export]
  public Button HostSteamButton;

  [Export]
  public Button JoinSteamButton;

  [Export]
  public TextEdit LobbyId;

  [Export]
  public Button HostIPButton;

  [Export]
  public Button JoinIPButton;

  [Export]
  public TextEdit LobbyIP;

  [Export]
  public Button StartButton;

  [Export]
  public Button OpenInviteOverlayButton;

  [Export]
  public RichTextLabel Members;

  [Export]
  public Control AvatarsContainer;

  [Export]
  public Control FriendsContainer;

  private Poller _poller = new(5.0f);

  public override void _EnterTree() {
    OpenInviteOverlayButton.Pressed += OpenInviteOverlay;

    HostSteamButton.Pressed += HostSteam;
    JoinSteamButton.Pressed += JoinSteam;

    HostIPButton.Pressed += HostIP;
    JoinIPButton.Pressed += JoinIP;

    StartButton.Pressed += ChangeLevel;

    PopulateFriendsList();
    Lobby.OnMembersUpdated += () => {
      foreach (var child in AvatarsContainer.GetChildren()) {
        child.QueueFree();
      }

      Members.Text = "";
      foreach (var (_, member) in Lobby.MemberByPeerID) {
        Members.Text += $"{member}\n";

        var avatar = new TextureRect {
          Texture = member.Avatar
        };

        AvatarsContainer.AddChild(avatar);
      }
    };
  }

  public override void _ExitTree() {
    SteamManager.OnSteamInitialized -= PopulateFriendsList;
    OpenInviteOverlayButton.Pressed -= OpenInviteOverlay;

    HostSteamButton.Pressed -= HostSteam;
    JoinSteamButton.Pressed -= JoinSteam;

    HostIPButton.Pressed -= HostIP;
    JoinIPButton.Pressed -= JoinIP;

    StartButton.Pressed -= ChangeLevel;

    try {
      SteamManager.LeaveLobby();
    } catch {
    }
  }

  public override void _Process(double delta) {
    Visible = Input.MouseMode == Input.MouseModeEnum.Visible;
    _poller.Poll(PopulateFriendsList);
  }

  /// <summary>
  /// Populate with online friends playing this game
  ///
  /// Creates a button for each friend that, when pressed, sends a game invite.
  /// </summary>
  private void PopulateFriendsList() {
    if (!SteamClient.IsValid) return;
    foreach (var child in FriendsContainer.GetChildren()) {
      child.QueueFree();
    }

    var friends = SteamFriends.GetFriends();
    foreach (var friend in friends) {
      if (!friend.IsOnline) continue;
      // if (!friend.IsPlayingThisGame) continue;

      var entry = new Button {
        Text = friend.Name
      };

      entry.Pressed += () => {
        friend.InviteToGame("Hey");
      };

      FriendsContainer.AddChild(entry);
    }
  }

  private void HostSteam() {
    Lobby.SteamHost();
  }

  private void JoinSteam() {
    Lobby.SteamJoin(ulong.Parse(LobbyId.Text));
  }

  private void HostIP() {
    Lobby.ENetHost();
  }

  private void JoinIP() {
    Lobby.ENetJoin(LobbyIP.Text);
  }

  private void OpenInviteOverlay() {
    SteamManager.OpenFriendOverlayForInvite();
  }

  private void ChangeLevel() {
    MultiplayerLevelManager.ChangeLevel(ResourceLoader.Load<PackedScene>("res://L_DemoScene.tscn"));
  }
}
