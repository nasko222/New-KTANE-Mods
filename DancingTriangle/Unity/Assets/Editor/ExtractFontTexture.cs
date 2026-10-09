using UnityEngine;
using UnityEditor;
using System.IO;

public class ExtractFontTexture
{
    [MenuItem("Tools/Extract Selected Font Texture")]
    private static void Extract()
    {
        Font font = Selection.activeObject as Font;

        if (font == null)
        {
            Debug.LogError("Select the Font asset first.");
            return;
        }

        Texture2D source = font.material.mainTexture as Texture2D;

        if (source == null)
        {
            Debug.LogError("Font has no Texture2D atlas.");
            return;
        }

        RenderTexture rt = RenderTexture.GetTemporary(
            source.width,
            source.height,
            0,
            RenderTextureFormat.ARGB32
        );

        Graphics.Blit(source, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D copy = new Texture2D(
            source.width,
            source.height,
            TextureFormat.ARGB32,
            false
        );

        copy.ReadPixels(
            new Rect(0, 0, source.width, source.height),
            0,
            0
        );

        copy.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        byte[] png = copy.EncodeToPNG();

        string path = EditorUtility.SaveFilePanel(
            "Save Font Atlas",
            Application.dataPath,
            font.name + "_Atlas",
            "png"
        );

        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllBytes(path, png);
            Debug.Log("Saved font atlas to: " + path);
            AssetDatabase.Refresh();
        }

        Object.DestroyImmediate(copy);
    }
}