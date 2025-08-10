using System.Collections.Generic;
using UnityEngine;

public class BCCGenerator : MonoBehaviour
{
    public GameObject atomPrefab;
    public GameObject cylinderPrefab;
    public float a = 1f;
    public int nx = 1, ny = 1, nz = 1;
    public Color atomColor = Color.white;
    public float atomScale = 0.2f;
    public Color bondColor = Color.yellow;
    public float bondRadius = 0.02f;

    [Header("Cube Face Planes")]
    public Material planeMaterial;
    public bool showPlanes = true;
    public Color planeColor = new Color(1f, 0f, 0f, 0.3f); // Semi-transparent red

    private List<GameObject> atoms = new List<GameObject>();
    private List<GameObject> bonds = new List<GameObject>();
    private List<GameObject> planes = new List<GameObject>();

    void Start()
    {
        GenerateBCC();
    }

    public void GenerateBCC()
    {
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

        // Store atom positions for fast lookup
        HashSet<Vector3> cornerPositions = new HashSet<Vector3>();
        HashSet<Vector3> centerPositions = new HashSet<Vector3>();

        // Generate corner atoms
        for (int i = 0; i <= nx; i++)
            for (int j = 0; j <= ny; j++)
                for (int k = 0; k <= nz; k++)
                {
                    Vector3 pos = new Vector3(i * a, j * a, -k * a);
                    cornerPositions.Add(pos);
                }

        // Generate body-centered atoms (center of each unit cell)
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < ny; j++)
                for (int k = 0; k < nz; k++)
                {
                    Vector3 cellOrigin = new Vector3(i * a, j * a, -k * a);
                    Vector3 centerPos = cellOrigin + new Vector3(0.5f * a, 0.5f * a, -0.5f * a);
                    centerPositions.Add(centerPos);
                }

        // Create atoms
        foreach (var pos in cornerPositions)
            atoms.Add(CreateAtom(pos));
        foreach (var pos in centerPositions)
            atoms.Add(CreateAtom(pos));

        // Connect bonds in BCC structure:
        // Only connect corner atoms to adjacent corner atoms (like SC structure)
        // Center atoms remain isolated (not connected to any other atoms)
        HashSet<string> connected = new HashSet<string>();

        // Connect corner atoms to adjacent corner atoms (cube edges)
        Vector3[] directions = new Vector3[]
        {
            new Vector3(a, 0, 0),   // X direction
            new Vector3(0, a, 0),   // Y direction
            new Vector3(0, 0, -a)    // Z direction
        };

        foreach (var cornerPos in cornerPositions)
        {
            foreach (var dir in directions)
            {
                Vector3 neighbor = cornerPos + dir;
                if (cornerPositions.Contains(neighbor))
                {
                    string key = cornerPos.ToString() + neighbor.ToString();
                    string keyRev = neighbor.ToString() + cornerPos.ToString();
                    if (!connected.Contains(key) && !connected.Contains(keyRev))
                    {
                        bonds.Add(CreateBond(cornerPos, neighbor));
                        connected.Add(key);
                    }
                }
            }
        }

        // Generate Miller planes
        if (showPlanes)
        {
            GenerateMillerPlanes();
        }
    }

    #region Miller Planes Generation
    void GenerateMillerPlanes()
    {
        // Generate the 6 main crystallographic planes for the cube
        GenerateMillerPlane(1, 0, 0, "(100)"); // Right face - X = nx*a
        GenerateMillerPlane(-1, 0, 0, "(1̄00)"); // Left face - X = 0
        GenerateMillerPlane(0, 1, 0, "(010)"); // Top face - Y = ny*a
        GenerateMillerPlane(0, -1, 0, "(01̄0)"); // Bottom face - Y = 0
        GenerateMillerPlane(0, 0, 1, "(001)"); // Back face - Z = nz*a
        GenerateMillerPlane(0, 0, -1, "(001̄)"); // Front face - Z = 0
    }

    void GenerateMillerPlane(int h, int k, int l, string planeName)
    {
        // Calculate cube dimensions
        float cubeWidth = nx * a;
        float cubeHeight = ny * a;
        float cubeDepth = nz * a;

        // Calculate all possible intersection points with cube edges
        List<Vector3> intersectionPoints = GetPlaneEdgeIntersections(h, k, l, cubeWidth, cubeHeight, cubeDepth);
        
        // Remove duplicates and points outside cube
        List<Vector3> validPoints = new List<Vector3>();
        foreach (var point in intersectionPoints)
        {
            if (IsPointInCube(point, cubeWidth, cubeHeight, cubeDepth) && !ContainsPoint(validPoints, point))
            {
                validPoints.Add(point);
            }
        }

        // Create the plane surface if we have at least 3 points
        if (validPoints.Count >= 3)
        {
            CreatePlaneMesh(validPoints, planeName);
        }
    }

    List<Vector3> GetPlaneEdgeIntersections(int h, int k, int l, float cubeWidth, float cubeHeight, float cubeDepth)
    {
        List<Vector3> points = new List<Vector3>();
        
        // Determine plane equation constant based on Miller indices (adjusted for flipped Z)
        float d = 0;
        if (h > 0) d = h * cubeWidth;
        else if (h < 0) d = 0;
        else if (k > 0) d = k * cubeHeight;
        else if (k < 0) d = 0;
        else if (l > 0) d = 0;  // For positive Z Miller index, plane is at Z = 0
        else if (l < 0) d = -l * cubeDepth;  // For negative Z Miller index, plane is at Z = -cubeDepth
        
        // Define cube vertices (adjusted for flipped Z axis)
        Vector3[] cubeVertices = {
            new Vector3(0, 0, 0), new Vector3(cubeWidth, 0, 0), new Vector3(cubeWidth, cubeHeight, 0), new Vector3(0, cubeHeight, 0),
            new Vector3(0, 0, -cubeDepth), new Vector3(cubeWidth, 0, -cubeDepth), new Vector3(cubeWidth, cubeHeight, -cubeDepth), new Vector3(0, cubeHeight, -cubeDepth)
        };
        
        // Define cube edges (pairs of vertex indices)
        int[,] edges = {
            {0,1}, {1,2}, {2,3}, {3,0}, // bottom face
            {4,5}, {5,6}, {6,7}, {7,4}, // top face
            {0,4}, {1,5}, {2,6}, {3,7}  // vertical edges
        };
        
        for (int i = 0; i < edges.GetLength(0); i++)
        {
            Vector3 p1 = cubeVertices[edges[i, 0]];
            Vector3 p2 = cubeVertices[edges[i, 1]];
            
            Vector3 intersection;
            if (GetLinePlaneIntersection(p1, p2, h, k, l, d, out intersection))
            {
                if (IsPointOnSegment(p1, p2, intersection))
                {
                    points.Add(intersection);
                }
            }
        }
        
        return points;
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

    bool IsPointInCube(Vector3 point, float cubeWidth, float cubeHeight, float cubeDepth)
    {
        float tolerance = 0.0001f;
        return point.x >= -tolerance && point.x <= cubeWidth + tolerance && 
               point.y >= -tolerance && point.y <= cubeHeight + tolerance && 
               point.z <= tolerance && point.z >= -cubeDepth - tolerance;
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
        
        // Find a reference direction (pick the direction to the first point)
        Vector3 reference = (points[0] - center).normalized;
        
        // Sort points by angle around the center using proper 3D angle calculation
        points.Sort((a, b) => {
            Vector3 dirA = (a - center).normalized;
            Vector3 dirB = (b - center).normalized;
            
            // Calculate angles using atan2 with proper cross product
            Vector3 crossA = Vector3.Cross(reference, dirA);
            Vector3 crossB = Vector3.Cross(reference, dirB);
            
            float angleA = Mathf.Atan2(Vector3.Dot(crossA, normal), Vector3.Dot(reference, dirA));
            float angleB = Mathf.Atan2(Vector3.Dot(crossB, normal), Vector3.Dot(reference, dirB));
            
            // Normalize angles to [0, 2π]
            if (angleA < 0) angleA += 2 * Mathf.PI;
            if (angleB < 0) angleB += 2 * Mathf.PI;
            
            return angleA.CompareTo(angleB);
        });
        
        // Create a mesh for the polygon
        GameObject planeObj = new GameObject($"MillerPlane_{planeName}");
        planeObj.transform.parent = transform;
        
        MeshFilter meshFilter = planeObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = planeObj.AddComponent<MeshRenderer>();
        
        Mesh mesh = new Mesh();
        
        // Create vertices (double the vertices for double-sided mesh)
        Vector3[] vertices = new Vector3[points.Count * 2];
        for (int i = 0; i < points.Count; i++)
        {
            vertices[i] = planeObj.transform.InverseTransformPoint(points[i]);
            vertices[i + points.Count] = vertices[i]; // Duplicate for back face
        }
        
        // Create triangles for both front and back faces
        List<int> triangles = new List<int>();
        
        // Front face triangles (fan triangulation)
        for (int i = 1; i < points.Count - 1; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);
        }
        
        // Back face triangles (reverse winding order)
        for (int i = 1; i < points.Count - 1; i++)
        {
            triangles.Add(points.Count); // First vertex of back face
            triangles.Add(points.Count + i + 1); // Reverse order
            triangles.Add(points.Count + i);
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
        
        // Use the custom plane material if provided, otherwise create a default one
        if (planeMaterial != null)
        {
            meshRenderer.material = planeMaterial;
        }
        else
        {
            // Create default material
            Material defaultMaterial = new Material(Shader.Find("Standard"));
            defaultMaterial.color = planeColor;
            defaultMaterial.SetFloat("_Mode", 3); // Transparent
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

    // Method to show only specific Miller index planes
    public void ShowMillerPlanes(bool show100, bool show010, bool show001)
    {
        foreach (var plane in planes)
        {
            if (plane == null) continue;
            
            string planeName = plane.name;
            bool shouldShow = false;
            
            if (show100 && (planeName.Contains("(100)") || planeName.Contains("(1̄00)"))) shouldShow = true;
            if (show010 && (planeName.Contains("(010)") || planeName.Contains("(01̄0)"))) shouldShow = true;
            if (show001 && (planeName.Contains("(001)") || planeName.Contains("(001̄)"))) shouldShow = true;
            
            plane.SetActive(shouldShow && showPlanes);
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
}