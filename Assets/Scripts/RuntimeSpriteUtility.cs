using UnityEngine;

public static class RuntimeSpriteUtility
{
    private static Sprite cachedWhiteSprite;

    public static Sprite WhiteSprite
    {
        get
        {
            if (cachedWhiteSprite == null)
            {
                Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                cachedWhiteSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }

            return cachedWhiteSprite;
        }
    }
}
