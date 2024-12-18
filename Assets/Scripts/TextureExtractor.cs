using UnityEngine;
using System.IO;

public class TextureExtractor : MonoBehaviour
{
    public SpriteRenderer spriteRenderer; // Assign the SpriteRenderer in the Inspector
    public OnlineMaps onlineMaps;

    public void ExtractTexture()
    {
        // Extract and save the texture on Start (you can call this method elsewhere)
        SaveSpriteTextureAsPNG(spriteRenderer,"Map" + onlineMaps.position.ToString() +".png");
    }

    void SaveSpriteTextureAsPNG(SpriteRenderer sr, string fileName)
    {
        if (sr == null || sr.sprite == null)
        {
            Debug.LogError("SpriteRenderer or Sprite is missing.");
            return;
        }

        // Get the texture from the Sprite
        Texture2D texture = sr.sprite.texture;

        // Create a new texture and crop it to the sprite's rect
        Rect spriteRect = sr.sprite.rect;
        Texture2D croppedTexture = new Texture2D((int)spriteRect.width, (int)spriteRect.height);

        Color[] pixels = texture.GetPixels(
            (int)spriteRect.x,
            (int)spriteRect.y,
            (int)spriteRect.width,
            (int)spriteRect.height
        );
        croppedTexture.SetPixels(pixels);
        croppedTexture.Apply();

        // Convert to PNG
        byte[] pngBytes = croppedTexture.EncodeToPNG();

        // Save the PNG file to the persistent data path
        string filePath = Path.Combine(Application.dataPath, fileName);
        File.WriteAllBytes(filePath, pngBytes);

        Debug.Log("Texture saved to: " + filePath);

        // Cleanup
        Destroy(croppedTexture);
    }
}
