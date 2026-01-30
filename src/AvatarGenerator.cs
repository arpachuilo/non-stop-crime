// Ported from
// https://github.com/FlyAndNotDown/github-avatar-generator/blob/master/src/main/java/pers/kindem/gh/avatar/Generator.java

using Godot;
using System.Security.Cryptography;
using System.Text;

public static class AvatarGenerator {
  private const int _blockRow = 5;
  private const int _blockRowHalf = _blockRow / 2 + 1;
  private const int _blockWidth = 70;
  private const int _remainsWidth = 35;
  private const int _imageWidth = _remainsWidth * 2 + _blockWidth * _blockRow;
  private const int _imageHeight = _remainsWidth * 2 + _blockWidth * _blockRow;
  private static readonly Color _backgroundColor = new(0.9f, 0.9f, 0.9f);

  private class AvatarInfo(Color color) {
    public Color Color { get; } = color;
    private readonly bool[] _blocks = new bool[_blockRow * _blockRow];

    public void SetBlockValue(int index, bool value) {
      _blocks[index] = value;
    }

    public bool GetBlockValue(int index) {
      return _blocks[index];
    }
  }

  public static Image NextAvatar(string seed) {
    AvatarInfo avatarInfo = NextAvatarInfo(seed);

    // Create image with RGBA8 format
    Image image = Image.CreateEmpty(_imageWidth, _imageHeight, false, Image.Format.Rgba8);

    // Fill with background color
    image.Fill(_backgroundColor);

    // Draw the avatar blocks
    int count = 0;
    for (int i = 0; i < _blockRowHalf; i++) {
      for (int j = 0; j < _blockRow; j++) {
        if (!avatarInfo.GetBlockValue(count++)) {
          continue;
        }
        FillImageBlock(image, i, j, avatarInfo.Color);
        FillImageBlock(image, _blockRow - 1 - i, j, avatarInfo.Color);
      }
    }

    return image;
  }

  /// <summary>
  /// Returns the avatar as an ImageTexture ready for use in Godot UI elements
  /// </summary>
  public static ImageTexture NextAvatarTexture(string seed) {
    Image image = NextAvatar(seed);
    return ImageTexture.CreateFromImage(image);
  }

  private static void FillImageBlock(Image image, int row, int col, Color color) {
    int pixelRowStart = _remainsWidth + row * _blockWidth;
    int pixelColStart = _remainsWidth + col * _blockWidth;

    for (int x = pixelRowStart; x < pixelRowStart + _blockWidth; x++) {
      for (int y = pixelColStart; y < pixelColStart + _blockWidth; y++) {
        image.SetPixel(x, y, color);
      }
    }
  }

  private static AvatarInfo NextAvatarInfo(string seed) {
    byte[] hash = NextHash(seed);

    // 3 bytes for color, 15 bytes for block
    int[] info = new int[18];
    for (int i = 0; i < hash.Length; i++) {
      int index = i % 18;
      info[index] = (info[index] + hash[i]) % 256;
    }

    Color color = new(info[0] / 255f, info[1] / 255f, info[2] / 255f);
    AvatarInfo avatarInfo = new(color);

    for (int i = 3; i < 18; i++) {
      avatarInfo.SetBlockValue(i, info[i] > 127);
    }

    return avatarInfo;
  }

  private static byte[] NextHash(string seed) {
    using SHA256 sha256 = SHA256.Create();
    byte[] inputBytes = Encoding.UTF8.GetBytes(seed);
    return sha256.ComputeHash(inputBytes);
  }
}

