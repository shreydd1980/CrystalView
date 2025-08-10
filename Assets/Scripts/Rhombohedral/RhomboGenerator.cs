using System.Collections.Generic;
using UnityEngine;

public class RhomboGenerator : MonoBehaviour
{
    public GameObject atomPrefab;
    public GameObject cylinderPrefab;
    public float a = 1f; // Lattice constant for all sides
    [Range(1, 89)] public int angle = 60; // All angles (in degrees), never 90
    public int nx = 1, ny = 1, nz = 1;
    public Color atomColor = Color.white;
    public float atomScale = 0.2f;
    public Color bondColor = Color.yellow;
    public float bondRadius = 0.02f;

    [Header("Miller Plane Visualization")]
    public Material planeMaterial;
    public bool showPlanes = true;
    public Color planeColor = new Color(0, 1, 1, 0.3f);
    public int h = 1, k = 1, l = 1; // Miller indices

    private List<GameObject> atoms = new List<GameObject>();
    private List<GameObject> bonds = new List<GameObject>();
    private List<GameObject> planes = new List<GameObject>();

    void Start()
    {
        GenerateRhombohedral();
    }

    public void GenerateRhombohedral()
    {
        // Ensure angle is never 90
        if (angle == 90) angle = 89;

        // Destroy previous atoms, bonds, and planes
        foreach (var atom in atoms)
            if (atom != null) DestroyImmediate(atom);
        foreach (var bond in bonds)
            if (bond != null) DestroyImmediate(bond);
        foreach (var plane in planes)
            if (plane != null) DestroyImmediate(plane);
        atoms.Clear();
        bonds.Clear();
        planes.Clear();

        // Convert angle to radians
        float theta = Mathf.Deg2Rad * angle;

        // Rhombohedral lattice vectors (all sides equal, all angles equal, not 90)
        Vector3 a1 = new Vector3(a, 0, 0);
        Vector3 a2 = new Vector3(a * Mathf.Cos(theta), a * Mathf.Sin(theta), 0);
        float cz = a * Mathf.Sqrt(1 - 2 * Mathf.Cos(theta) * Mathf.Cos(theta) + Mathf.Cos(theta) * Mathf.Cos(theta));
        float cx = a * Mathf.Cos(theta);
        float cy = a * (Mathf.Cos(theta) - Mathf.Cos(theta) * Mathf.Cos(theta)) / Mathf.Sin(theta);
        Vector3 a3 = new Vector3(cx, cy, -cz);

        // Store atom positions for fast lookup
        HashSet<Vector3> atomPositions = new HashSet<Vector3>(new Vector3Comparer());

        // Generate only corner atoms (rhombohedral lattice)
        for (int i = 0; i <= nx; i++)
            for (int j = 0; j <= ny; j++)
                for (int k = 0; k <= nz; k++)
                {
                    Vector3 pos = i * a1 + j * a2 + k * a3;
                    atomPositions.Add(pos);
                }

        // Create atoms
        foreach (var pos in atomPositions)
            atoms.Add(CreateAtom(pos));

        // Only connect bonds between corner atoms along cell edges
        Vector3[] directions = new Vector3[] { a1, a2, a3 };
        HashSet<string> connected = new HashSet<string>();

        foreach (var pos in atomPositions)
        {
            foreach (var dir in directions)
            {
                Vector3 neighbor = pos + dir;
                if (atomPositions.Contains(neighbor))
                {
                    string key = pos.ToString() + neighbor.ToString();
                    string keyRev = neighbor.ToString() + pos.ToString();
                    if (!connected.Contains(key) && !connected.Contains(keyRev))
                    {
                        bonds.Add(CreateBond(pos, neighbor));
                        connected.Add(key);
                    }
                }
            }
        }

        // Generate Miller planes for all faces
        if (showPlanes)
        {
            GenerateAllFacePlanes(a1, a2, a3);
        }
    }

    #region Miller Planes Generation
    void GenerateAllFacePlanes(Vector3 a1, Vector3 a2, Vector3 a3)
    {
        // Generate all 6 faces of the rhombohedral parallelepiped
        // Use a more robust approach to avoid duplicates
        
        // Generate the 6 faces directly based on the rhombohedral cell geometry
        GenerateRhombohedralFace(a1, a2, a3, "Face_1", Vector3.zero, ny * a2, nz * a3); // Face at origin (a1=0)
        GenerateRhombohedralFace(a1, a2, a3, "Face_2", nx * a1, ny * a2, nz * a3); // Face at end of a1
        GenerateRhombohedralFace(a1, a2, a3, "Face_3", Vector3.zero, nx * a1, nz * a3); // Face at origin (a2=0)
        GenerateRhombohedralFace(a1, a2, a3, "Face_4", ny * a2, nx * a1, nz * a3); // Face at end of a2
        GenerateRhombohedralFace(a1, a2, a3, "Face_5", Vector3.zero, nx * a1, ny * a2); // Face at origin (a3=0)
        GenerateRhombohedralFace(a1, a2, a3, "Face_6", nz * a3, nx * a1, ny * a2); // Face at end of a3

        // Also generate the user-specified Miller plane if it's different from face planes
        if (h != 0 || k != 0 || l != 0)
        {
            // Check if it's not one of the basic face planes
            if (!((h == 1 && k == 0 && l == 0) || (h == 0 && k == 1 && l == 0) || (h == 0 && k == 0 && l == 1)))
            {
                GenerateMillerPlane(h, k, l, a1, a2, a3, $"({h}{k}{l})");
            }
        }
    }

    void GenerateRhombohedralFace(Vector3 a1, Vector3 a2, Vector3 a3, string faceName, Vector3 origin, Vector3 vec1, Vector3 vec2)
    {
        // Generate a parallelogram face defined by origin and two vectors
        List<Vector3> faceVertices = new List<Vector3>();
        
        // Calculate the 4 corners of the parallelogram face
        faceVertices.Add(origin);
        faceVertices.Add(origin + vec1);
        faceVertices.Add(origin + vec1 + vec2);
        faceVertices.Add(origin + vec2);

        // Create the plane surface
        CreatePlaneMesh(faceVertices, faceName);
    }

    void GenerateMillerPlane(int h, int k, int l, Vector3 a1, Vector3 a2, Vector3 a3, string planeName)
    {
        if (h == 0 && k == 0 && l == 0) return;

        // For face planes, we define the faces directly based on the rhombohedral cell
        List<Vector3> faceVertices = new List<Vector3>();
        
        if (planeName.Contains("Face_a1+"))
        {
            // Face at the end of a1 vector
            Vector3 basePos = nx * a1;
            faceVertices.Add(basePos);
            faceVertices.Add(basePos + ny * a2);
            faceVertices.Add(basePos + ny * a2 + nz * a3);
            faceVertices.Add(basePos + nz * a3);
        }
        else if (planeName.Contains("Face_a1-"))
        {
            // Face at the origin in a1 direction
            Vector3 basePos = Vector3.zero;
            faceVertices.Add(basePos);
            faceVertices.Add(basePos + nz * a3);
            faceVertices.Add(basePos + ny * a2 + nz * a3);
            faceVertices.Add(basePos + ny * a2);
        }
        else if (planeName.Contains("Face_a2+"))
        {
            // Face at the end of a2 vector
            Vector3 basePos = ny * a2;
            faceVertices.Add(basePos);
            faceVertices.Add(basePos + nx * a1);
            faceVertices.Add(basePos + nx * a1 + nz * a3);
            faceVertices.Add(basePos + nz * a3);
        }
        else if (planeName.Contains("Face_a2-"))
        {
            // Face at the origin in a2 direction
            Vector3 basePos = Vector3.zero;
            faceVertices.Add(basePos);
            faceVertices.Add(basePos + nz * a3);
            faceVertices.Add(basePos + nx * a1 + nz * a3);
            faceVertices.Add(basePos + nx * a1);
        }
        else if (planeName.Contains("Face_a3+"))
        {
            // Face at the end of a3 vector
            Vector3 basePos = nz * a3;
            faceVertices.Add(basePos);
            faceVertices.Add(basePos + nx * a1);
            faceVertices.Add(basePos + nx * a1 + ny * a2);
            faceVertices.Add(basePos + ny * a2);
        }
        else if (planeName.Contains("Face_a3-"))
        {
            // Face at the origin in a3 direction
            Vector3 basePos = Vector3.zero;
            faceVertices.Add(basePos);
            faceVertices.Add(basePos + ny * a2);
            faceVertices.Add(basePos + nx * a1 + ny * a2);
            faceVertices.Add(basePos + nx * a1);
        }
        else
        {
            // For custom Miller planes, use the original intersection method
            List<Vector3> cellVertices = GetRhombohedralVertices(a1, a2, a3);
            List<Vector3> intersectionPoints = GetPlaneEdgeIntersections(h, k, l, cellVertices, a1, a2, a3);
            
            foreach (var point in intersectionPoints)
            {
                if (IsPointInRhombohedralCell(point, a1, a2, a3) && !ContainsPoint(faceVertices, point))
                {
                    faceVertices.Add(point);
                }
            }
        }

        // Create the plane surface if we have at least 3 points
        if (faceVertices.Count >= 3)
        {
            CreatePlaneMesh(faceVertices, planeName);
        }
    }

    List<Vector3> GetRhombohedralVertices(Vector3 a1, Vector3 a2, Vector3 a3)
    {
        List<Vector3> vertices = new List<Vector3>();
        
        // Generate the 8 vertices of the rhombohedral parallelepiped
        // Only need the corner vertices (0,0,0), (nx,0,0), (0,ny,0), etc.
        for (int i = 0; i <= 1; i++)
            for (int j = 0; j <= 1; j++)
                for (int k = 0; k <= 1; k++)
                {
                    Vector3 vertex = (i * nx) * a1 + (j * ny) * a2 + (k * nz) * a3;
                    vertices.Add(vertex);
                }
        
        return vertices;
    }

    List<Vector3> GetPlaneEdgeIntersections(int h, int k, int l, List<Vector3> vertices, Vector3 a1, Vector3 a2, Vector3 a3)
    {
        List<Vector3> points = new List<Vector3>();
        
        // Calculate plane constant d based on Miller indices and lattice vectors
        float d = CalculatePlaneConstant(h, k, l, a1, a2, a3);
        
        // Define edges of rhombohedral cell
        List<(Vector3, Vector3)> edges = GetRhombohedralEdges(vertices, a1, a2, a3);
        
        foreach (var edge in edges)
        {
            Vector3 intersection;
            if (GetLinePlaneIntersection(edge.Item1, edge.Item2, h, k, l, d, out intersection))
            {
                if (IsPointOnSegment(edge.Item1, edge.Item2, intersection))
                {
                    points.Add(intersection);
                }
            }
        }
        
        return points;
    }

    float CalculatePlaneConstant(int h, int k, int l, Vector3 a1, Vector3 a2, Vector3 a3)
    {
        // For face planes, calculate based on which face we're looking at
        if (h > 0 && k == 0 && l == 0) return nx; // a1+ face
        if (h < 0 && k == 0 && l == 0) return 0;  // a1- face
        if (h == 0 && k > 0 && l == 0) return ny; // a2+ face
        if (h == 0 && k < 0 && l == 0) return 0;  // a2- face
        if (h == 0 && k == 0 && l > 0) return nz; // a3+ face
        if (h == 0 && k == 0 && l < 0) return 0;  // a3- face
        
        // For general Miller planes, find intercepts and calculate plane constant
        Vector3 intercept = Vector3.zero;
        if (h != 0) intercept += a1 / h;
        if (k != 0) intercept += a2 / k;
        if (l != 0) intercept += a3 / l;
        
        return h * intercept.x + k * intercept.y + l * intercept.z;
    }

    List<(Vector3, Vector3)> GetRhombohedralEdges(List<Vector3> vertices, Vector3 a1, Vector3 a2, Vector3 a3)
    {
        List<(Vector3, Vector3)> edges = new List<(Vector3, Vector3)>();
        float tolerance = 0.001f;
        
        // Connect vertices that differ by exactly one lattice vector
        foreach (var v1 in vertices)
        {
            foreach (var v2 in vertices)
            {
                Vector3 diff = v2 - v1;
                
                // Check if difference matches any of the actual lattice vectors
                if (Vector3.Distance(diff, a1) < tolerance ||
                    Vector3.Distance(diff, a2) < tolerance ||
                    Vector3.Distance(diff, a3) < tolerance ||
                    Vector3.Distance(diff, -a1) < tolerance ||
                    Vector3.Distance(diff, -a2) < tolerance ||
                    Vector3.Distance(diff, -a3) < tolerance)
                {
                    // Avoid duplicate edges
                    bool alreadyExists = false;
                    foreach (var existingEdge in edges)
                    {
                        if ((Vector3.Distance(existingEdge.Item1, v1) < tolerance && Vector3.Distance(existingEdge.Item2, v2) < tolerance) ||
                            (Vector3.Distance(existingEdge.Item1, v2) < tolerance && Vector3.Distance(existingEdge.Item2, v1) < tolerance))
                        {
                            alreadyExists = true;
                            break;
                        }
                    }
                    
                    if (!alreadyExists)
                    {
                        edges.Add((v1, v2));
                    }
                }
            }
        }
        
        return edges;
    }

    bool GetLinePlaneIntersection(Vector3 p1, Vector3 p2, int h, int k, int l, float d, out Vector3 intersection)
    {
        intersection = Vector3.zero;
        
        Vector3 direction = p2 - p1;
        float denominator = h * direction.x + k * direction.y + l * direction.z;
        
        if (Mathf.Abs(denominator) < 0.0001f) // Line is parallel to plane
            return false;
        
        float t = (d - (h * p1.x + k * p1.y + l * p1.z)) / denominator;
        intersection = p1 + t * direction;
        return true;
    }

    bool IsPointOnSegment(Vector3 p1, Vector3 p2, Vector3 point)
    {
        float tolerance = 0.0001f;
        return Vector3.Distance(p1, point) + Vector3.Distance(point, p2) <= Vector3.Distance(p1, p2) + tolerance;
    }

    bool ContainsPoint(List<Vector3> points, Vector3 point)
    {
        foreach (var p in points)
        {
            if (Vector3.Distance(p, point) < 0.0001f)
                return true;
        }
        return false;
    }

    bool IsPointInRhombohedralCell(Vector3 point, Vector3 a1, Vector3 a2, Vector3 a3)
    {
        // Check if point is within the rhombohedral parallelepiped
        // Transform point to unit cell coordinates
        Matrix4x4 latticeMatrix = new Matrix4x4(a1, a2, a3, Vector3.zero);
        Vector3 coords = latticeMatrix.inverse.MultiplyPoint3x4(point);
        
        float tolerance = 0.001f;
        return coords.x >= -tolerance && coords.x <= nx + tolerance &&
               coords.y >= -tolerance && coords.y <= ny + tolerance &&
               coords.z >= -tolerance && coords.z <= nz + tolerance;
    }

    void CreatePlaneMesh(List<Vector3> points, string planeName)
    {
        if (points.Count < 3) return;
        
        // Calculate center point
        Vector3 center = Vector3.zero;
        foreach (var point in points)
            center += point;
        center /= points.Count;
        
        // Calculate normal vector
        Vector3 normal = Vector3.Cross(points[1] - points[0], points[2] - points[0]).normalized;
        
        // Find a reference direction
        Vector3 reference = (points[0] - center).normalized;
        
        // Sort points by angle around the center
        points.Sort((a, b) => {
            Vector3 dirA = (a - center).normalized;
            Vector3 dirB = (b - center).normalized;
            
            Vector3 crossA = Vector3.Cross(reference, dirA);
            Vector3 crossB = Vector3.Cross(reference, dirB);
            
            float angleA = Mathf.Atan2(Vector3.Dot(crossA, normal), Vector3.Dot(reference, dirA));
            float angleB = Mathf.Atan2(Vector3.Dot(crossB, normal), Vector3.Dot(reference, dirB));
            
            if (angleA < 0) angleA += 2 * Mathf.PI;
            if (angleB < 0) angleB += 2 * Mathf.PI;
            
            return angleA.CompareTo(angleB);
        });
        
        // Create plane GameObject
        GameObject planeObj = new GameObject($"MillerPlane_{planeName}");
        planeObj.transform.parent = transform;
        
        MeshFilter meshFilter = planeObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = planeObj.AddComponent<MeshRenderer>();
        
        Mesh mesh = new Mesh();
        
        // Create vertices for double-sided mesh
        Vector3[] vertices = new Vector3[points.Count * 2];
        for (int i = 0; i < points.Count; i++)
        {
            vertices[i] = planeObj.transform.InverseTransformPoint(points[i]);
            vertices[i + points.Count] = vertices[i];
        }
        
        // Create triangles for both faces
        List<int> triangles = new List<int>();
        
        // Front face
        for (int i = 1; i < points.Count - 1; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);
        }
        
        // Back face (reverse winding)
        for (int i = 1; i < points.Count - 1; i++)
        {
            triangles.Add(points.Count);
            triangles.Add(points.Count + i + 1);
            triangles.Add(points.Count + i);
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
        
        // Use the assigned plane material
        if (planeMaterial != null)
        {
            meshRenderer.material = planeMaterial;
        }
        else
        {
            // Create default transparent material only if no plane material is assigned
            Material defaultMaterial = new Material(Shader.Find("Standard"));
            defaultMaterial.color = planeColor;
            defaultMaterial.SetFloat("_Mode", 3);
            defaultMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            defaultMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            defaultMaterial.SetInt("_ZWrite", 0);
            defaultMaterial.DisableKeyword("_ALPHATEST_ON");
            defaultMaterial.EnableKeyword("_ALPHABLEND_ON");
            defaultMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            defaultMaterial.renderQueue = 3000;
            defaultMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            
            meshRenderer.material = defaultMaterial;
        }
        
        planes.Add(planeObj);
    }

    public void TogglePlanes()
    {
        showPlanes = !showPlanes;
        foreach (var plane in planes)
        {
            if (plane != null)
                plane.SetActive(showPlanes);
        }
    }
    #endregion

    GameObject CreateAtom(Vector3 pos)
    {
        if (atomPrefab == null) return null;
        GameObject atom = Instantiate(atomPrefab, pos, Quaternion.identity, transform);
        atom.transform.localScale = Vector3.one * atomScale;
        Renderer r = atom.GetComponent<Renderer>();
        if (r != null)
        {
            r.material = new Material(r.material);
            r.material.color = atomColor;
        }
        return atom;
    }

    GameObject CreateBond(Vector3 start, Vector3 end)
    {
        if (cylinderPrefab == null) return null;

        Vector3 dir = end - start;
        float length = dir.magnitude;
        Vector3 mid = (start + end) * 0.5f;

        GameObject bond = Instantiate(cylinderPrefab, mid, Quaternion.identity, transform);
        bond.transform.up = dir.normalized;
        bond.transform.localScale = new Vector3(bondRadius * 2, length * 0.5f, bondRadius * 2);

        Renderer r = bond.GetComponent<Renderer>();
        if (r != null)
        {
            r.material = new Material(r.material);
            r.material.color = bondColor;
        }
        return bond;
    }

    class Vector3Comparer : IEqualityComparer<Vector3>
    {
        public bool Equals(Vector3 a, Vector3 b)
        {
            return Mathf.Abs(a.x - b.x) < 1e-4f &&
                   Mathf.Abs(a.y - b.y) < 1e-4f &&
                   Mathf.Abs(a.z - b.z) < 1e-4f;
        }
        public int GetHashCode(Vector3 obj)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Mathf.RoundToInt(obj.x * 10000);
                hash = hash * 23 + Mathf.RoundToInt(obj.y * 10000);
                hash = hash * 23 + Mathf.RoundToInt(obj.z * 10000);
                return hash;
            }
        }
    }
}