
public static partial class Settings {
  [Setting("Network", true)]
  public static Observable<string> IPAddress { get; set; } = "127.0.0.1";

  [Setting("Network", true)]
  public static Observable<int> Port { get; set; } = 7777;

  [Setting("Network", true)]
  public static Observable<int> MaxPlayers { get; set; } = 16;
}
