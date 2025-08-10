using System.Collections.Generic;
using UnityEngine;

public class TriclinicGenerator : MonoBehaviour
{
    public GameObject atomPrefab;
    public GameObject cylinderPrefab;
    public float a = 1f; // Lattice constant for x
    public float b = 1.5f; // Lattice constant for y
    public float c = 2f; // Lattice constant for z
    [Range(1, 89)] public int alpha = 90; // Angle between b and c (in degrees)
    [Range(1, 89)] public int beta = 90;  // Angle between a and c (in degrees)
    [Range(1, 89)] public int gamma = 90; // Angle between a and b (in degrees)
    public int nx = 1, ny = 1, nz = 1;
    public Color atomColor = Color.white;
    public float atomScale = 0.2f;
    public Color bondColor = Color.yellow;
    public float bondRadius = 0.02f;

    [Header("Triclinic Face Planes")]
    public Material planeMaterial;
    public bool showPlanes = true;
    public Color planeColor = new Color(1f, 0f, 0f, 0.3f); // Semi-transparent red

    private List<GameObject> atoms = new List<GameObject>();
    private List<GameObject> bonds = new List<GameObject>();
    private List<GameObject> planes = new List<GameObject>();
    private Vector3 a1, a2, a3; // Store lattice vectors for plane calculations

    void Start()
    {
        GenerateTriclinic();
    }

    public void GenerateTriclinic()
    {
        // Ensure all angles are not equal to each other
        if (alpha == beta) beta = alpha + 1;
        if (alpha == gamma || beta == gamma) gamma = Mathf.Max(alpha, beta) + 1;
        if (gamma > 89) gamma = 89;

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

        // Convert angles to radians
        float alphaRad = Mathf.Deg2Rad * alpha;
        float betaRad = Mathf.Deg2Rad * beta;
        float gammaRad = Mathf.Deg2Rad * gamma;

        // Lattice vectors for triclinic cell
        a1 = new Vector3(a, 0, 0);
        a2 = new Vector3(
            b * Mathf.Cos(gammaRad),
            b * Mathf.Sin(gammaRad),
            0
        );
        float cx = c * Mathf.Cos(betaRad);
        float cy = c * (Mathf.Cos(alphaRad) - Mathf.Cos(betaRad) * Mathf.Cos(gammaRad)) / Mathf.Sin(gammaRad);
        float cz = Mathf.Sqrt(Mathf.Max(0f, c * c - cx * cx - cy * cy));
        a3 = new Vector3(cx, cy, -cz);

        // Fill the entire triclinic lattice with atoms at all integer grid points
        HashSet<Vector3> atomPositions = new HashSet<Vector3>(new Vector3Comparer());

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

        // Connect bonds between all nearest neighbors (cell edges)
        Vector3[] directions = new Vector3[]
        {
            a1,
            a2,
            a3
        };

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

        // Generate triclinic planes
        if (showPlanes)
        {
            GenerateTriclinicPlanes();
        }
    }

    #region Triclinic Planes Generation
    void GenerateTriclinicPlanes()
    {
        // Generate the 6 main triclinic cell face planes
        // Each pair represents opposite faces of the triclinic parallelepiped
        
        GenerateTriclinicPlane(1, 0, 0, "(100)"); // Face perpendicular to a1
        GenerateTriclinicPlane(-1, 0, 0, "(1̄00)"); // Opposite face
        GenerateTriclinicPlane(0, 1, 0, "(010)"); // Face perpendicular to a2
        GenerateTriclinicPlane(0, -1, 0, "(01̄0)"); // Opposite face
        GenerateTriclinicPlane(0, 0, 1, "(001)"); // Face perpendicular to a3
        GenerateTriclinicPlane(0, 0, -1, "(001̄)"); // Opposite face
    }

    void GenerateTriclinicPlane(int h, int k, int l, string planeName)
    {
        // Calculate all possible intersection points with triclinic cell edges
        List<Vector3> intersectionPoints = GetTriclinicPlaneIntersections(h, k, l);
        
        // Remove duplicates
        List<Vector3> validPoints = new List<Vector3>();
        foreach (var point in intersectionPoints)
        {
            if (!ContainsPoint(validPoints, point))
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

    List<Vector3> GetTriclinicPlaneIntersections(int h, int k, int l)
    {
        List<Vector3> points = new List<Vector3>();
        
        // Calculate the intercepts with the lattice vectors
        // For triclinic systems, we need to find where the plane intersects the parallelepiped edges
        
        // Define the 8 vertices of the triclinic parallelepiped
        Vector3[] vertices = new Vector3[8];
        for (int i = 0; i < 2; i++)
            for (int j = 0; j < 2; j++)
                for (int k_idx = 0; k_idx < 2; k_idx++)
                {
                    int idx = i * 4 + j * 2 + k_idx;
                    vertices[idx] = (i * nx) * a1 + (j * ny) * a2 + (k_idx * nz) * a3;
                }

        // Define the 12 edges of the parallelepiped
        int[,] edges = {
            {0,1}, {2,3}, {4,5}, {6,7}, // edges parallel to a1
            {0,2}, {1,3}, {4,6}, {5,7}, // edges parallel to a2
            {0,4}, {1,5}, {2,6}, {3,7}  // edges parallel to a3
        };

        // Calculate plane equation parameters
        // For triclinic, we need to transform Miller indices to Cartesian coordinates
        Vector3 normal = h * Vector3.Cross(a2, a3) + k * Vector3.Cross(a3, a1) + l * Vector3.Cross(a1, a2);
        normal = normal.normalized;
        
        // Determine d value based on which face we're creating
        float d = 0;
        if (h > 0) d = Vector3.Dot(normal, nx * a1);
        else if (h < 0) d = Vector3.Dot(normal, Vector3.zero);
        else if (k > 0) d = Vector3.Dot(normal, ny * a2);
        else if (k < 0) d = Vector3.Dot(normal, Vector3.zero);
        else if (l > 0) d = Vector3.Dot(normal, nz * a3);
        else if (l < 0) d = Vector3.Dot(normal, Vector3.zero);

        // Find intersections with each edge
        for (int i = 0; i < edges.GetLength(0); i++)
        {
            Vector3 p1 = vertices[edges[i, 0]];
            Vector3 p2 = vertices[edges[i, 1]];
            
            Vector3 intersection;
            if (GetLinePlaneIntersection(p1, p2, normal, d, out intersection))
            {
                if (IsPointOnSegment(p1, p2, intersection))
                {
                    points.Add(intersection);
                }
            }
        }
        
        return points;
    }
    
    bool GetLinePlaneIntersection(Vector3 p1, Vector3 p2, Vector3 normal, float d, out Vector3 intersection)
    {
        intersection = Vector3.zero;
        
        Vector3 direction = p2 - p1;
        float denominator = Vector3.Dot(normal, direction);
        
        if (Mathf.Abs(denominator) < 0.0001f) // Line is parallel to plane
            return false;
        
        float t = (d - Vector3.Dot(normal, p1)) / denominator;
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
        GameObject planeObj = new GameObject($"TriclinicPlane_{planeName}");
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

    // Custom comparer for Vector3 to avoid floating point issues in HashSet
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