using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(MeshFilter))]
public class VisionCone : MonoBehaviour
{
    public float viewDistance = 5f;
    public float fovAngle = 90f;
    public int rayCount = 50;
    public LayerMask obstacleMask;

    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;

    private Light2D visionLight;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        visionLight = GetComponent<Light2D>();
    }

    void LateUpdate()
    {
        GenerateCone();
        
    }

    void GenerateCone()
    {
        float currentAngle = -fovAngle / 2;
        float angleStep = fovAngle / rayCount;

        vertices = new Vector3[rayCount + 2];
        triangles = new int[rayCount * 3];

        vertices[0] = Vector3.zero;

        for (int i = 0; i <= rayCount; i++)
        {
            Vector3 direction = Quaternion.Euler(0, 0, currentAngle) * transform.up;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, viewDistance, obstacleMask);

            if (hit.collider != null)
                vertices[i + 1] = transform.InverseTransformPoint(hit.point);
            else
                vertices[i + 1] = transform.InverseTransformPoint(transform.position + direction * viewDistance);

            if (i < rayCount)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            currentAngle += angleStep;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        if (visionLight != null)
        {
            visionLight.SetShapePath(vertices);
        }

        List<Vector2> vertices2D = new List<Vector2>();
        foreach (Vector3 vertex in mesh.vertices)
        {
            vertices2D.Add(new Vector2(vertex.x, vertex.y));
        }

        // Use PolygonCollider2D for a closed, filled shape
        PolygonCollider2D polyCollider = GetComponent<PolygonCollider2D>();
        if (polyCollider == null)
        {
            polyCollider = gameObject.AddComponent<PolygonCollider2D>();
        }

        // PolygonCollider2D can use the vertices to define its shape
        polyCollider.points = vertices2D.ToArray();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Debug.Log("Enemy in sight");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Debug.Log("Enemy out of sight");
        }
    }

}