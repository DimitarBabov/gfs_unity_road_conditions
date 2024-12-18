using UnityEngine;

public class MapMaskLatLonUpdater : MonoBehaviour
{
    public Material roadShaderMaterial; // The material using your custom shader

    // If the mask bounding box is fixed (e.g. entire US), set them here
    public float maskMinLon = -125f, maskMaxLon = -66f, maskMinLat = 24f, maskMaxLat = 49f;

    void Update()
    {
        // Get bounding corners from Online Maps
        Vector2 topLeft = OnlineMaps.instance.topLeftPosition;       // (lon, lat)
        Vector2 bottomRight = OnlineMaps.instance.bottomRightPosition; // (lon, lat)

        float mapMinLon = topLeft.x;
        float mapMaxLon = bottomRight.x;
        float mapMaxLat = topLeft.y;
        float mapMinLat = bottomRight.y;

        // Assign these to the shader
        roadShaderMaterial.SetFloat("_MapMinLon", mapMinLon);
        roadShaderMaterial.SetFloat("_MapMaxLon", mapMaxLon);
        roadShaderMaterial.SetFloat("_MapMinLat", mapMinLat);
        roadShaderMaterial.SetFloat("_MapMaxLat", mapMaxLat);

        roadShaderMaterial.SetFloat("_MaskMinLon", maskMinLon);
        roadShaderMaterial.SetFloat("_MaskMaxLon", maskMaxLon);
        roadShaderMaterial.SetFloat("_MaskMinLat", maskMinLat);
        roadShaderMaterial.SetFloat("_MaskMaxLat", maskMaxLat);
    }
}
