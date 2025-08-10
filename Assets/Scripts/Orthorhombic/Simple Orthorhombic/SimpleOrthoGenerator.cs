using System.Collections.Generic;
using UnityEngine;

public class SimpleOrthoGenerator : MonoBehaviour
{
    public GameObject atomPrefab;
    public GameObject cylinderPrefab;
    public float a = 1f; // Lattice constant for x
    public float b = 1.5f; // Lattice constant for y
    public float c = 2f; // Lattice constant for z
    public int nx = 1, ny = 1, nz = 1;
    public Color atomColor = Color.white;
    public float atomScale = 0.2f;
    public Color bondColor = Color.yellow;
    public float bondRadius = 0.02f;

    [Header("Orthorhombic Face Planes")]
    public Material planeMaterial;
    public bool showPlanes = true;
    public Color planeColor = new Color(1f, 0f, 0f, 0.3f); // Semi-transparent red

    private List<GameObject> atoms = new List<GameObject>();
    private List<GameObject> bonds = new List<GameObject>();
    private List<GameObject> planes = new List<GameObject>();

    void Start()
    {
        GenerateOrthorhombic();
    }

    public void GenerateOrthorhombic()
    {
        // Ensure all sides are different (orthorhombic, not cubic or tetragonal)
        if (Mathf.Approximately(a, b)) b = a + 0.5f;
        if (Mathf.Approximately(a, c) || Mathf.Approximately(b, c)) c = Mathf.Max(a, b) + 0.5f;

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
        HashSet<Vector3> atomPositions = new HashSet<Vector3>();

        // Generate only corner atoms (simple orthorhombic lattice)
        for (int i = 0; i <= nx; i++)
            for (int j = 0; j <= ny; j++)
                for (int k = 0; k <= nz; k++)
                {
                    Vector3 pos = new Vector3(i * a, j * b, -k * c);
                    atomPositions.Add(pos);
                }

        // Create atoms
        foreach (var pos in atomPositions)
            atoms.Add(CreateAtom(pos));

        // Only connect bonds between corner atoms along cuboid edges (not diagonals)
        Vector3[] directions = new Vector3[]
        {
            new Vector3(a, 0, 0),
            new Vector3(0, b, 0),
            new Vector3(0, 0, -c)
        };

        HashSet<string> connected = new HashSet<string>();

        foreach (var pos in atomPositions)
        {
            foreach (var dir in directions)
            {
                Vector3 neighbor = pos + dir;
                if (atomPositions.Contains(neighbor))
                {
                    // Avoid duplicate bonds
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

        // Generate orthorhombic planes
        if (showPlanes)
        {
            GenerateOrthorhombicPlanes();
        }
    }

    #region Orthorhombic Planes Generation
    void GenerateOrthorhombicPlanes()
    {
        // Generate the 6 main orthorhombic cell face planes
        GenerateOrthorhombicPlane(1, 0, 0, "(100)"); // Face perpendicular to x-axis (a-direction)
        GenerateOrthorhombicPlane(-1, 0, 0, "(1̄00)"); // Opposite face
        GenerateOrthorhombicPlane(0, 1, 0, "(010)"); // Face perpendicular to y-axis (b-direction)
        GenerateOrthorhombicPlane(0, -1, 0, "(01̄0)"); // Opposite face
        GenerateOrthorhombicPlane(0, 0, 1, "(001)"); // Face perpendicular to z-axis (c-direction)
        GenerateOrthorhombicPlane(0, 0, -1, "(001̄)"); // Opposite face
    }

    void GenerateOrthorhombicPlane(int h, int k, int l, string planeName)
    {
        // Calculate all possible intersection points with orthorhombic cell edges
        List<Vector3> intersectionPoints = GetOrthorhombicPlaneIntersections(h, k, l);
        
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

    List<Vector3> GetOrthorhombicPlaneIntersections(int h, int k, int l)
    {
        List<Vector3> points = new List<Vector3>();
        
        // Define the 8 vertices of the orthorhombic parallelepiped
        Vector3[] vertices = new Vector3[8];
        for (int i = 0; i < 2; i++)
            for (int j = 0; j < 2; j++)
                for (int k_idx = 0; k_idx < 2; k_idx++)
                {
                    int idx = i * 4 + j * 2 + k_idx;
                    vertices[idx] = new Vector3(i * nx * a, j * ny * b, -k_idx * nz * c);
                }

        // Define the 12 edges of the orthorhombic parallelepiped
        int[,] edges = {
            {0,1}, {2,3}, {4,5}, {6,7}, // edges parallel to x-axis (a)
            {0,2}, {1,3}, {4,6}, {5,7}, // edges parallel to y-axis (b)
            {0,4}, {1,5}, {2,6}, {3,7}  // edges parallel to z-axis (c)
        };

        // Calculate plane equation parameters for orthorhombic system
        Vector3 normal = new Vector3(h / a, k / b, l / c).normalized;
        
        // Determine d value based on which face we're creating
        float d = 0;
        if (h > 0) d = Vector3.Dot(normal, new Vector3(nx * a, 0, 0));
        else if (h < 0) d = Vector3.Dot(normal, Vector3.zero);
        else if (k > 0) d = Vector3.Dot(normal, new Vector3(0, ny * b, 0));
        else if (k < 0) d = Vector3.Dot(normal, Vector3.zero);
        else if (l > 0) d = Vector3.Dot(normal, new Vector3(0, 0, -nz * c));
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
        GameObject planeObj = new GameObject($"OrthorhombicPlane_{planeName}");
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
        
        // Apply material
        if (planeMaterial != null)
        {
            meshRenderer.material = planeMaterial;
        }
        else
        {
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