using UnityEngine;

public static class Helpers
{
    /// <summary>
    /// MeshRenderer.bounds does not update in real time to account for scaling, rotation, etc.,
    /// so it is helpful to be able to calculate
    /// </summary>
    /// <remarks>
    /// This is untested on high-vertex-count meshes and could impact performance
    /// </remarks>
    public static Bounds RealTimeBounds(MeshFilter meshFilter)
    {
        Mesh mesh = meshFilter.mesh;
        Vector3[] vertices = mesh.vertices;
        if (vertices.Length == 0)
        {
            Debug.LogWarning("Trying to calculate bounds of a mesh with no vertices");
        }

        // TransformPoint converts the local mesh vertice dependent on the transform
        // position, scale and orientation into a global position
        var min = meshFilter.transform.TransformPoint(vertices[0]);
        var max = min;

        for (var i = 1; i < vertices.Length; i++)
        {
            var V = meshFilter.transform.TransformPoint(vertices[i]);

            // Go through X,Y and Z of the Vector3
            for (var n = 0; n < 3; n++)
            {
                max = Vector3.Max(V, max);
                min = Vector3.Min(V, min);
            }
        }

        Bounds bounds = new();
        bounds.SetMinMax(min, max);

        return bounds;
    }

    public static Bounds RealTimeBounds(GameObject obj)
    {
        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogWarning("Trying to calculate bounds of a GameObject with no MeshFilter");
        }

        return RealTimeBounds(meshFilter);
    }
}