using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static OVRPlugin;

public class RoomBoundingBox : MonoBehaviour
{
    /*
     * 
     *  Room Bounding Box will try to find the vertices of the room "box".
     *  
     *  In order to find the vertices, we need to know that vertices are at the intesection
     *  of 3 planes, one of them being the ceiling or floor plane.
     *  
     *  We can iterate thru all the room meshes, label them with either "wall", "ceiling"
     *  or "floor", and find all the points where 2 "walls" are in contact with the "ceiling"
     *  or the "floor".
     * 
     */

    // label all the room meshes with related tag

    // a placeholder prefab to spawn at the corners of the room box
    [SerializeField] GameObject ball;
    [SerializeField] GameObject sphereParent;

    private GameObject room;
    public List<GameObject> targets;

    private HashSet<Vector3> spherePos;

    public Vector3 minX;
    public Vector3 maxX;
    public Vector3 minZ;
    public Vector3 maxZ;

    public void StartFill()
    {
        if (sphereParent == null)
            sphereParent = new GameObject("SphereParent");

        if (sphereParent.transform.childCount == 0)
        {
            targets = new List<GameObject>();
            spherePos = new HashSet<Vector3>();

            FindAllWalls();

            int count = 0;
            foreach (GameObject go in targets)
            {
                string name = "Wall";
                Vector3[] vertices = GetBoxColliderVertices(go.transform.GetChild(1).GetChild(0).GetComponent<BoxCollider>());

                float avgHeight = 0;
                for (int i = 0; i < vertices.Length; i++)
                {
                    if (SpawnSphere(vertices[i], count, name, sphereParent.transform))
                        count++;
                    avgHeight += vertices[i].y;
                }

                // find average height line
                avgHeight /= count;

                FindCornerBounds(vertices, avgHeight);
            }
        }
    }

    private void Update()
    {
        // Draw wireframe representation of the box
        // DebugDrawBox(target.transform.position, size / 2f, target.transform.rotation, Color.green);
    }

    private void FindCornerBounds(Vector3[] vertices, float avgHeight)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i];
            if (vertex.y < avgHeight)
            {
                if (vertex.x < minX.x) minX = vertex;
                if (vertex.x > maxX.x) maxX = vertex;
                if (vertex.z < minZ.z) minZ = vertex;
                if (vertex.z > maxZ.z) maxZ = vertex;
            }
        }
    }

    private void LabelRoomMesh()
    {
        // Find Room object in scene
        GameObject[] allGameObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();

        foreach (GameObject go in allGameObjects)
        {
            if (go.name.StartsWith("Room"))
            {
                // Do something with the GameObject
                room = go;
                break;
            }
        }

        // Iterate thru all children under room and label
        for (int i = 0; i < room.transform.childCount; i++)
        {
            Transform child = room.transform.GetChild(i);
            string childName = child.name;

            if (childName.StartsWith("Wall"))
                child.tag = "Wall";
            else if (childName.StartsWith("Ceiling"))
                child.tag = "Ceiling";
            else if (childName.StartsWith("Floor"))
                child.tag = "Floor";
        }
    }

    private void FindAllWalls()
    {
        // Find Room object in scene
        GameObject[] allGameObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();

        foreach (GameObject go in allGameObjects)
        {
            if (go.name.StartsWith("Wall"))
            {
                go.tag = "Wall";
                targets.Add(go);
            }
        }
    }
    private bool SpawnSphere(Vector3 position, int i, string name, Transform parent)
    {
        // Instantiate a sphere at the given position
        // Check set if position has sphere overlap
        if(!spherePos.Contains(position))
        {
            GameObject go = Instantiate(ball, position, Quaternion.identity);
            go.transform.parent = parent;
            go.name += name + " num " + i;

            spherePos.Add(position);

            return true;
        }
        return false;
    }

    private Vector3[] GetBoxColliderVertices(BoxCollider boxCollider)
    {
        // Calculate the vertices of the box collider
        // Since walls are quads, we only use front 4 points
        Vector3 center = boxCollider.center;
        Vector3 size = boxCollider.size;
        Vector3 halfSize = size / 2f;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = RoundVerticesToTenth(boxCollider.transform.TransformPoint(center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z))); // Front bottom left
        vertices[1] = RoundVerticesToTenth(boxCollider.transform.TransformPoint(center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z)));  // Front bottom right
        vertices[2] = RoundVerticesToTenth(boxCollider.transform.TransformPoint(center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z)));  // Front top left
        vertices[3] = RoundVerticesToTenth(boxCollider.transform.TransformPoint(center + new Vector3(halfSize.x, halfSize.y, -halfSize.z)));   // Front top right

        return vertices;
    }

    private Vector3 RoundVerticesToTenth(Vector3 vector)
    {
        return new Vector3(
            Mathf.Round(vector.x * 10f) / 10f,
            Mathf.Round(vector.y * 10f) / 10f,
            Mathf.Round(vector.z * 10f) / 10f
        );
    }

    void DebugDrawBox(Vector3 center, Vector3 halfExtents, Quaternion rotation, Color color)
    {
        // Calculate the corners of the box
        Vector3[] corners = new Vector3[8];
        corners[0] = center + rotation * new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z);
        corners[1] = center + rotation * new Vector3(-halfExtents.x, -halfExtents.y, halfExtents.z);
        corners[2] = center + rotation * new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z);
        corners[3] = center + rotation * new Vector3(-halfExtents.x, halfExtents.y, halfExtents.z);
        corners[4] = center + rotation * new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z);
        corners[5] = center + rotation * new Vector3(halfExtents.x, -halfExtents.y, halfExtents.z);
        corners[6] = center + rotation * new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z);
        corners[7] = center + rotation * new Vector3(halfExtents.x, halfExtents.y, halfExtents.z);

        // Draw lines connecting the corners to form the box
        Debug.DrawLine(corners[0], corners[1], color);
        Debug.DrawLine(corners[1], corners[3], color);
        Debug.DrawLine(corners[3], corners[2], color);
        Debug.DrawLine(corners[2], corners[0], color);
        Debug.DrawLine(corners[4], corners[5], color);
        Debug.DrawLine(corners[5], corners[7], color);
        Debug.DrawLine(corners[7], corners[6], color);
        Debug.DrawLine(corners[6], corners[4], color);
        Debug.DrawLine(corners[0], corners[4], color);
        Debug.DrawLine(corners[1], corners[5], color);
        Debug.DrawLine(corners[2], corners[6], color);
        Debug.DrawLine(corners[3], corners[7], color);
    }

    // Measures the execution time for function's performance
    void MeasureExecutionTime(System.Action function)
    {
        // Create a Stopwatch instance
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

        // Start the stopwatch
        stopwatch.Start();

        // Call the function whose execution time you want to measure
        LabelRoomMesh();

        // Stop the stopwatch
        stopwatch.Stop();

        // Get the elapsed time
        TimeSpan elapsedTime = stopwatch.Elapsed;

        // Output the elapsed time to the console
        UnityEngine.Debug.Log("Function execution time: " + elapsedTime.TotalMilliseconds + " milliseconds");
    }

}
