using System.Collections.Generic;
using UnityEngine;

public class BaCMonoGenerator : MonoBehaviour
{
    public GameObject atomPrefab;
    public GameObject cylinderPrefab;
    public float a = 1f; // Lattice constant for x
    public float b = 1.5f; // Lattice constant for y
    public float c = 2f; // Lattice constant for z
    [Range(1, 89)] public int gamma = 75; // Angle between a and b (in degrees), never 90
    public int nx = 1, ny = 1, nz = 1;
    public Color atomColor = Color.white;
    public float atomScale = 0.2f;
    public Color bondColor = Color.yellow;
    public float bondRadius = 0.02f;

    [Header("Monoclinic Face Planes")]
    public Material planeMaterial;
    public bool showPlanes = true;
    public Color planeColor = new Color(1f, 0f, 0f, 0.3f); // Semi-transparent red

    private List<GameObject> atoms = new List<GameObject>();
    private List<GameObject> bonds = new List<GameObject>();
    private List<GameObject> planes = new List<GameObject>();

    void Start()
    {
        GenerateBaseCenteredMonoclinic();
    }

    public void GenerateBaseCenteredMonoclinic()
    {
        // Ensure gamma is never 90
        if (gamma == 90) gamma = 89;

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

        // Fixed angles
        float gammaRad = Mathf.Deg2Rad * gamma; // gamma ≠ 90°

        // Lattice vectors for simple monoclinic cell
        Vector3 a1 = new Vector3(a, 0, 0);
        Vector3 a2 = new Vector3(
            b * Mathf.Cos(gammaRad),
            b * Mathf.Sin(gammaRad),
            0
        );
        Vector3 a3 = new Vector3(0, 0, -c); // Since alpha=beta=90, a3 is along z

        // Store atom positions for fast lookup
        HashSet<Vector3> atomPositions = new HashSet<Vector3>(new Vector3Comparer());
        HashSet<Vector3> baseCenterPositions = new HashSet<Vector3>(new Vector3Comparer());

        // Corner atoms (monoclinic lattice)
        for (int i = 0; i <= nx; i++)
            for (int j = 0; j <= ny; j++)
                for (int k = 0; k <= nz; k++)
                {
                    Vector3 pos = i * a1 + j * a2 + k * a3;
                    atomPositions.Add(pos);
                }

        // Base-centered atoms (center of xz faces: y = 0 and y = max y for each cell)
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < ny; j++)
                for (int k = 0; k < nz; k++)
                {
                    Vector3 cellOrigin = i * a1 + j * a2 + k * a3;
                    // y = 0 face center (bottom face of this cell)
                    baseCenterPositions.Add(cellOrigin + 0.5f * a1 + 0.5f * a3);
                    // y = max face center (top face of this cell, which is the bottom face of the next cell in y)
                    baseCenterPositions.Add(cellOrigin + a2 + 0.5f * a1 + 0.5f * a3);
                }

        // Create atoms (corners and base centers)
        foreach (var pos in atomPositions)
            atoms.Add(CreateAtom(pos));
        foreach (var pos in baseCenterPositions)
            atoms.Add(CreateAtom(pos));

        // Only connect bonds between corner atoms along cell edges (not diagonals, not base centers)
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
        // Do NOT connect base center atoms to anything

        // Generate monoclinic planes
        if (showPlanes)
        {
            GenerateMonoclinicPlanes();
        }
    }

    #region Monoclinic Planes Generation
    void GenerateMonoclinicPlanes()
    {
        // Generate the 6 main monoclinic cell face planes
        GenerateMonoclinicPlane(1, 0, 0, "(100)"); // Face perpendicular to a-axis
        GenerateMonoclinicPlane(-1, 0, 0, "(1̄00)"); // Opposite face
        GenerateMonoclinicPlane(0, 1, 0, "(010)"); // Face perpendicular to b-axis
        GenerateMonoclinicPlane(0, -1, 0, "(01̄0)"); // Opposite face
        GenerateMonoclinicPlane(0, 0, 1, "(001)"); // Face perpendicular to c-axis
        GenerateMonoclinicPlane(0, 0, -1, "(001̄)"); // Opposite face
    }

    void GenerateMonoclinicPlane(int h, int k, int l, string planeName)
    {
        // Calculate all possible intersection points with monoclinic cell edges
        List<Vector3> intersectionPoints = GetMonoclinicPlaneIntersections(h, k, l);
        
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

    List<Vector3> GetMonoclinicPlaneIntersections(int h, int k, int l)
    {
        List<Vector3> points = new List<Vector3>();
        
        // Calculate lattice vectors
        float gammaRad = Mathf.Deg2Rad * gamma;
        Vector3 a1 = new Vector3(a, 0, 0);
        Vector3 a2 = new Vector3(b * Mathf.Cos(gammaRad), b * Mathf.Sin(gammaRad), 0);
        Vector3 a3 = new Vector3(0, 0, -c);

        // Define the 8 vertices of the monoclinic parallelepiped
        Vector3[] vertices = new Vector3[8];
        int idx = 0;
        for (int i = 0; i < 2; i++)
            for (int j = 0; j < 2; j++)
                for (int k_idx = 0; k_idx < 2; k_idx++)
                {
                    vertices[idx] = i * nx * a1 + j * ny * a2 + k_idx * nz * a3;
                    idx++;
                }

        // Define the 12 edges of the monoclinic parallelepiped
        int[,] edges = {
            {0,1}, {2,3}, {4,5}, {6,7}, // edges parallel to a-axis
            {0,2}, {1,3}, {4,6}, {5,7}, // edges parallel to b-axis
            {0,4}, {1,5}, {2,6}, {3,7}  // edges parallel to c-axis
        };

        // Calculate reciprocal lattice vectors for monoclinic system
        Vector3 recipA = CalculateReciprocalVector(a1, a2, a3, true, false, false);
        Vector3 recipB = CalculateReciprocalVector(a1, a2, a3, false, true, false);
        Vector3 recipC = CalculateReciprocalVector(a1, a2, a3, false, false, true);
        
        Vector3 normal = h * recipA + k * recipB + l * recipC;
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

    Vector3 CalculateReciprocalVector(Vector3 a1, Vector3 a2, Vector3 a3, bool isA, bool isB, bool isC)
    {
        float volume = Vector3.Dot(a1, Vector3.Cross(a2, a3));
        
        if (isA) return Vector3.Cross(a2, a3) / volume;
        if (isB) return Vector3.Cross(a3, a1) / volume;
        if (isC) return Vector3.Cross(a1, a2) / volume;
        
        return Vector3.zero;
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
        GameObject planeObj = new GameObject($"MonoclinicPlane_{planeName}");
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