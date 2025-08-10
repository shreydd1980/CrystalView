using System.Collections.Generic;
using UnityEngine;

public class HCPPlane : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject atomPrefab;
    public GameObject cylinderPrefab;
    
    [Header("Lattice Parameters")]
    public float a = 1f; // Lattice constant a
    public float c = 1.633f; // c/a ratio for ideal HCP
    
    [Header("Atom Settings")]
    public Color atomColor = Color.white;
    public float atomScale = 0.2f;
    
    [Header("Bond Settings")]
    public Color bondColor = Color.yellow;
    public float bondRadius = 0.02f;

    [Header("Miller-Bravais Planes")]
    public bool showPlanes = true;
    public Material planeMaterial; // Use the material you provide
    public Color planeColor = new Color(0f, 1f, 0f, 0.4f);
    
    [System.Serializable]
    public class MillerBravaisPlane
    {
        public int h, k, i, l;
        public bool enabled = true;
        
        public MillerBravaisPlane(int h, int k, int i, int l)
        {
            this.h = h;
            this.k = k;
            this.i = i;
            this.l = l;
        }
        
        public bool IsValid()
        {
            return h + k + i == 0;
        }
    }
    
    public List<MillerBravaisPlane> millerBravaisPlanes = new List<MillerBravaisPlane>()
    {
        new MillerBravaisPlane(0, 0, 0, 1) { enabled = true },      // Basal plane (0001)
        new MillerBravaisPlane(1, 0, -1, 0) { enabled = true },     // Prismatic plane (10-10)
        new MillerBravaisPlane(1, -1, 0, 0) { enabled = false },    // Prismatic plane (1-100)
    };

    private List<GameObject> atoms = new List<GameObject>();
    private List<GameObject> bonds = new List<GameObject>();
    private List<GameObject> planes = new List<GameObject>();
    private HashSet<string> bondKeys = new HashSet<string>();

    void Start()
    {
        GenerateHCP();
    }

    public void GenerateHCP()
    {
        ClearAll();
        CreateAtoms();
        CreateBonds();
        
        if (showPlanes)
        {
            CreatePlanes();
        }
    }

    void ClearAll()
    {
        foreach (var atom in atoms)
            if (atom != null) DestroyImmediate(atom);
        foreach (var bond in bonds)
            if (bond != null) DestroyImmediate(bond);
        foreach (var plane in planes)
            if (plane != null) DestroyImmediate(plane);
        
        atoms.Clear();
        bonds.Clear();
        planes.Clear();
        bondKeys.Clear();
    }

    void CreateAtoms()
    {
        // HCP has atoms at:
        // Base layer (y=0): hexagon vertices + center
        // Top layer (y=c): hexagon vertices + center  
        // Middle layer (y=c/2): 3 atoms in triangular arrangement
        
        float sqrt3 = Mathf.Sqrt(3f);
        
        // Base hexagon vertices (y=0)
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(a * Mathf.Cos(angle), 0, a * Mathf.Sin(angle));
            atoms.Add(CreateAtom(pos));
        }
        
        // Base center (y=0)
        atoms.Add(CreateAtom(Vector3.zero));
        
        // Top hexagon vertices (y=c)
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(a * Mathf.Cos(angle), c, a * Mathf.Sin(angle));
            atoms.Add(CreateAtom(pos));
        }
        
        // Top center (y=c)
        atoms.Add(CreateAtom(new Vector3(0, c, 0)));
        
        // Middle layer atoms (y=c/2) - positioned in triangular holes
        for (int i = 0; i < 3; i++)
        {
            float angle = i * 120f * Mathf.Deg2Rad + 30f * Mathf.Deg2Rad; // Offset by 30 degrees
            float radius = a * sqrt3 / 3f;
            Vector3 pos = new Vector3(radius * Mathf.Cos(angle), c/2f, radius * Mathf.Sin(angle));
            atoms.Add(CreateAtom(pos));
        }
    }

    void CreateBonds()
    {
        // Base hexagon bonds (atoms 0-5)
        for (int i = 0; i < 6; i++)
        {
            int next = (i + 1) % 6;
            CreateBondBetweenAtoms(i, next);
        }
        
        // Top hexagon bonds (atoms 7-12)
        for (int i = 7; i < 13; i++)
        {
            int next = i + 1;
            if (next == 13) next = 7;
            CreateBondBetweenAtoms(i, next);
        }
        
        // Vertical bonds connecting base to top hexagons
        for (int i = 0; i < 6; i++)
        {
            CreateBondBetweenAtoms(i, i + 7);
        }
        
        // Middle layer triangle bonds (atoms 14-16)
        for (int i = 14; i < 17; i++)
        {
            int next = i + 1;
            if (next == 17) next = 14;
            CreateBondBetweenAtoms(i, next);
        }
    }

    void CreatePlanes()
    {
        foreach (var millerPlane in millerBravaisPlanes)
        {
            if (millerPlane.enabled && millerPlane.IsValid())
            {
                CreateMillerBravaisPlane(millerPlane.h, millerPlane.k, millerPlane.i, millerPlane.l);
            }
        }
    }

    void CreateMillerBravaisPlane(int h, int k, int i, int l)
    {
        if (h + k + i != 0) return;
        if (h == 0 && k == 0 && i == 0 && l == 0) return;

        Debug.Log($"Creating Miller-Bravais plane ({h},{k},{i},{l})");
        
        List<Vector3> planePoints = GetPlanePoints(h, k, i, l);
        
        Debug.Log($"Generated {planePoints.Count} points for plane ({h},{k},{i},{l})");
        
        if (planePoints.Count >= 3)
        {
            string planeName = $"({h}{k}{i}{l})";
            CreatePlaneMesh(planePoints, planeName);
            Debug.Log($"Successfully created plane mesh {planeName}");
        }
        else
        {
            Debug.LogWarning($"Not enough points ({planePoints.Count}) to create plane ({h},{k},{i},{l})");
        }
    }

    List<Vector3> GetPlanePoints(int h, int k, int i, int l)
    {
        List<Vector3> points = new List<Vector3>();
        
        // Special case: Basal plane (0001)
        if (h == 0 && k == 0 && i == 0 && l != 0)
        {
            float height = c / l;
            if (height >= 0 && height <= c)
            {
                // Create hexagon at specific height
                for (int j = 0; j < 6; j++)
                {
                    float angle = j * 60f * Mathf.Deg2Rad;
                    points.Add(new Vector3(a * Mathf.Cos(angle), height, a * Mathf.Sin(angle)));
                }
            }
            return points;
        }
        
        // For prismatic planes (10-10), create a simple vertical plane
        if (h == 1 && k == 0 && i == -1 && l == 0)
        {
            points.Add(new Vector3(a, 0, 0));
            points.Add(new Vector3(0, 0, 0));
            points.Add(new Vector3(0, c, 0));
            points.Add(new Vector3(a, c, 0));
            return points;
        }
        
        // For prismatic planes (1-100), create another vertical plane
        if (h == 1 && k == -1 && i == 0 && l == 0)
        {
            float sqrt3 = Mathf.Sqrt(3f);
            points.Add(new Vector3(a/2f, 0, a * sqrt3/2f));
            points.Add(new Vector3(-a/2f, 0, -a * sqrt3/2f));
            points.Add(new Vector3(-a/2f, c, -a * sqrt3/2f));
            points.Add(new Vector3(a/2f, c, a * sqrt3/2f));
            return points;
        }
        
        // Fallback: create a simple plane if no specific case matches
        if (points.Count == 0)
        {
            points.Add(new Vector3(a/2f, 0, 0));
            points.Add(new Vector3(-a/2f, 0, a * 0.866f));
            points.Add(new Vector3(-a/2f, c, a * 0.866f));
            points.Add(new Vector3(a/2f, c, 0));
        }
        
        return points;
    }

    List<Vector3> CreateHexagonalPlane(float height)
    {
        List<Vector3> points = new List<Vector3>();
        float sqrt3 = Mathf.Sqrt(3f);
        
        // HCP unit cell hexagonal boundary points at given height
        // Using the actual HCP lattice vectors to define the boundary
        Vector3[] hexBoundary = new Vector3[]
        {
            new Vector3(a, 0, 0),                                           // Point on a1 axis
            new Vector3(a/2f, 0, a * sqrt3/2f),                           // 60° from a1
            new Vector3(-a/2f, 0, a * sqrt3/2f),                          // 120° from a1
            new Vector3(-a, 0, 0),                                          // 180° from a1
            new Vector3(-a/2f, 0, -a * sqrt3/2f),                         // 240° from a1
            new Vector3(a/2f, 0, -a * sqrt3/2f)                           // 300° from a1
        };
        
        // Set the height for all boundary points
        for (int i = 0; i < hexBoundary.Length; i++)
        {
            points.Add(new Vector3(hexBoundary[i].x, height, hexBoundary[i].z));
        }
        
        return points;
    }

    List<Vector3> FindPlaneHCPIntersection(int h, int k, int i, int l)
    {
        List<Vector3> points = new List<Vector3>();
        float sqrt3 = Mathf.Sqrt(3f);
        
        // Calculate intercepts with HCP lattice vectors
        Vector3 a1 = new Vector3(sqrt3 * a, 0, 0);
        Vector3 a2 = new Vector3(-sqrt3 * a / 2f, 0, 1.5f * a);
        Vector3 a3 = new Vector3(0, c, 0);
        
        // Collect intercepts within unit cell
        List<Vector3> intercepts = new List<Vector3>();
        
        if (h != 0)
        {
            Vector3 intercept = a1 / h;
            if (IsInHCPBounds(intercept))
                intercepts.Add(intercept);
        }
        
        if (k != 0)
        {
            Vector3 intercept = a2 / k;
            if (IsInHCPBounds(intercept))
                intercepts.Add(intercept);
        }
        
        if (l != 0)
        {
            Vector3 intercept = a3 / l;
            if (IsInHCPBounds(intercept))
                intercepts.Add(intercept);
        }
        
        // If we have enough intercepts, calculate plane normal and find boundary intersections
        if (intercepts.Count >= 2)
        {
            Vector3 normal = CalculatePlaneNormal(h, k, i, l);
            float d = Vector3.Dot(normal, intercepts[0]);
            
            // Find intersections with HCP unit cell edges
            points = FindHCPBoundaryIntersections(normal, d);
        }
        
        // For prismatic planes (l=0), ensure they extend vertically within the unit cell
        if (l == 0 && points.Count >= 2)
        {
            List<Vector3> verticalPoints = new List<Vector3>();
            foreach (var point in points)
            {
                if (point.y <= c/2f) // Only add bottom points
                {
                    verticalPoints.Add(point);
                    verticalPoints.Add(new Vector3(point.x, c, point.z));
                }
            }
            return verticalPoints;
        }
        
        return points;
    }

    Vector3 CalculatePlaneNormal(int h, int k, int i, int l)
    {
        // Calculate normal vector for Miller-Bravais plane
        float sqrt3 = Mathf.Sqrt(3f);
        
        // Reciprocal lattice vectors for HCP
        Vector3 b1 = new Vector3(2f/(sqrt3*a), 2f/(3f*a), 0);
        Vector3 b2 = new Vector3(0, 4f/(3f*a), 0);
        Vector3 b3 = new Vector3(0, 0, 1f/c);
        
        Vector3 normal = h * b1 + k * b2 + l * b3;
        return normal.normalized;
    }

    List<Vector3> FindHCPBoundaryIntersections(Vector3 normal, float d)
    {
        List<Vector3> intersections = new List<Vector3>();
        float sqrt3 = Mathf.Sqrt(3f);
        
        // Define HCP unit cell edges
        Vector3[] vertices = new Vector3[]
        {
            // Bottom hexagon (y=0)
            new Vector3(a, 0, 0),
            new Vector3(a/2f, 0, a * sqrt3/2f),
            new Vector3(-a/2f, 0, a * sqrt3/2f),
            new Vector3(-a, 0, 0),
            new Vector3(-a/2f, 0, -a * sqrt3/2f),
            new Vector3(a/2f, 0, -a * sqrt3/2f),
            
            // Top hexagon (y=c)
            new Vector3(a, c, 0),
            new Vector3(a/2f, c, a * sqrt3/2f),
            new Vector3(-a/2f, c, a * sqrt3/2f),
            new Vector3(-a, c, 0),
            new Vector3(-a/2f, c, -a * sqrt3/2f),
            new Vector3(a/2f, c, -a * sqrt3/2f)
        };
        
        // Check intersections with hexagonal edges (bottom face)
        for (int i = 0; i < 6; i++)
        {
            Vector3 p1 = vertices[i];
            Vector3 p2 = vertices[(i + 1) % 6];
            AddLinePlaneIntersection(p1, p2, normal, d, intersections);
        }
        
        // Check intersections with hexagonal edges (top face)
        for (int i = 6; i < 12; i++)
        {
            Vector3 p1 = vertices[i];
            Vector3 p2 = vertices[6 + ((i - 6 + 1) % 6)];
            AddLinePlaneIntersection(p1, p2, normal, d, intersections);
        }
        
        // Check intersections with vertical edges
        for (int i = 0; i < 6; i++)
        {
            Vector3 p1 = vertices[i];
            Vector3 p2 = vertices[i + 6];
            AddLinePlaneIntersection(p1, p2, normal, d, intersections);
        }
        
        // Remove duplicates and sort
        intersections = RemoveDuplicatePoints(intersections);
        
        if (intersections.Count >= 3)
        {
            intersections = SortPointsCounterClockwise(intersections);
        }
        
        return intersections;
    }

    void AddLinePlaneIntersection(Vector3 p1, Vector3 p2, Vector3 normal, float d, List<Vector3> intersections)
    {
        Vector3 direction = p2 - p1;
        float denominator = Vector3.Dot(normal, direction);
        
        if (Mathf.Abs(denominator) > 0.0001f) // Line is not parallel to plane
        {
            float t = (d - Vector3.Dot(normal, p1)) / denominator;
            
            if (t >= 0 && t <= 1) // Intersection is within line segment
            {
                Vector3 intersection = p1 + t * direction;
                if (IsInHCPBounds(intersection))
                {
                    intersections.Add(intersection);
                }
            }
        }
    }

    List<Vector3> RemoveDuplicatePoints(List<Vector3> points)
    {
        List<Vector3> unique = new List<Vector3>();
        float tolerance = 0.001f;
        
        foreach (var point in points)
        {
            bool isDuplicate = false;
            foreach (var existing in unique)
            {
                if (Vector3.Distance(point, existing) < tolerance)
                {
                    isDuplicate = true;
                    break;
                }
            }
            if (!isDuplicate)
            {
                unique.Add(point);
            }
        }
        
        return unique;
    }

    List<Vector3> SortPointsCounterClockwise(List<Vector3> points)
    {
        if (points.Count < 3) return points;
        
        // Calculate center point
        Vector3 center = Vector3.zero;
        foreach (var point in points)
            center += point;
        center /= points.Count;
        
        // Calculate normal of the polygon
        Vector3 normal = Vector3.Cross(points[1] - points[0], points[2] - points[0]).normalized;
        
        // Sort by angle around center
        points.Sort((a, b) => {
            Vector3 dirA = (a - center).normalized;
            Vector3 dirB = (b - center).normalized;
            
            Vector3 reference = (points[0] - center).normalized;
            
            float angleA = Vector3.SignedAngle(reference, dirA, normal);
            float angleB = Vector3.SignedAngle(reference, dirB, normal);
            
            if (angleA < 0) angleA += 360f;
            if (angleB < 0) angleB += 360f;
            
            return angleA.CompareTo(angleB);
        });
        
        return points;
    }

    bool IsInHCPBounds(Vector3 point)
    {
        // Simplified bounds check - just ensure it's within reasonable limits
        return point.y >= -0.1f && point.y <= c + 0.1f &&
               Mathf.Abs(point.x) <= a * 2f &&
               Mathf.Abs(point.z) <= a * 2f;
    }

    void CreatePlaneMesh(List<Vector3> points, string planeName)
    {
        if (points.Count < 3) return;
        
        Debug.Log($"Creating mesh for plane {planeName} with {points.Count} points");
        foreach (var point in points)
        {
            Debug.Log($"  Point: {point}");
        }
        
        GameObject planeObj = new GameObject($"Plane_{planeName}");
        planeObj.transform.parent = transform;
        
        MeshFilter meshFilter = planeObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = planeObj.AddComponent<MeshRenderer>();
        
        Mesh mesh = new Mesh();
        
        // Sort points to form proper polygon
        Vector3 center = Vector3.zero;
        foreach (var point in points) center += point;
        center /= points.Count;
        
        points.Sort((a, b) => {
            float angleA = Mathf.Atan2(a.z - center.z, a.x - center.x);
            float angleB = Mathf.Atan2(b.z - center.z, b.x - center.x);
            return angleA.CompareTo(angleB);
        });
        
        // Create vertices (double for both faces)
        Vector3[] vertices = new Vector3[points.Count * 2];
        for (int i = 0; i < points.Count; i++)
        {
            vertices[i] = planeObj.transform.InverseTransformPoint(points[i]);
            vertices[i + points.Count] = vertices[i]; // Duplicate for back face
        }
        
        // Create triangles for both front and back faces
        List<int> triangles = new List<int>();
        
        // Front face triangles (counter-clockwise)
        for (int i = 1; i < points.Count - 1; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);
        }
        
        // Back face triangles (clockwise for proper normal)
        for (int i = 1; i < points.Count - 1; i++)
        {
            triangles.Add(points.Count); // First vertex of back face
            triangles.Add(points.Count + i + 1);
            triangles.Add(points.Count + i);
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
        
        // Apply your provided material
        if (planeMaterial != null)
        {
            meshRenderer.material = planeMaterial;
            Debug.Log($"Applied planeMaterial to {planeName}");
        }
        else
        {
            // Fallback material if none provided
            Material fallbackMaterial = new Material(Shader.Find("Standard"));
            fallbackMaterial.color = planeColor;
            meshRenderer.material = fallbackMaterial;
            Debug.Log($"Applied fallback material to {planeName}");
        }
        
        planes.Add(planeObj);
        Debug.Log($"Added plane {planeName} to scene. Total planes: {planes.Count}");
    }

    void CreateBondBetweenAtoms(int atomIndex1, int atomIndex2)
    {
        if (atomIndex1 >= atoms.Count || atomIndex2 >= atoms.Count) return;
        
        string key = atomIndex1 < atomIndex2 ? $"{atomIndex1}_{atomIndex2}" : $"{atomIndex2}_{atomIndex1}";
        if (bondKeys.Contains(key)) return;
        bondKeys.Add(key);
        
        Vector3 pos1 = atoms[atomIndex1].transform.position;
        Vector3 pos2 = atoms[atomIndex2].transform.position;
        bonds.Add(CreateBond(pos1, pos2));
    }

    GameObject CreateAtom(Vector3 position)
    {
        if (atomPrefab == null) return null;
        
        GameObject atom = Instantiate(atomPrefab, position, Quaternion.identity, transform);
        atom.transform.localScale = Vector3.one * atomScale;
        
        Renderer renderer = atom.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(renderer.material);
            renderer.material.color = atomColor;
        }
        
        return atom;
    }

    GameObject CreateBond(Vector3 start, Vector3 end)
    {
        if (cylinderPrefab == null) return null;
        
        Vector3 direction = end - start;
        float length = direction.magnitude;
        Vector3 midpoint = (start + end) * 0.5f;
        
        GameObject bond = Instantiate(cylinderPrefab, midpoint, Quaternion.identity, transform);
        bond.transform.up = direction.normalized;
        bond.transform.localScale = new Vector3(bondRadius * 2, length * 0.5f, bondRadius * 2);
        
        Renderer renderer = bond.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(renderer.material);
            renderer.material.color = bondColor;
        }
        
        return bond;
    }

    // Public methods for runtime control
    public void AddPlane(int h, int k, int i, int l)
    {
        if (h + k + i != 0) return;
        
        MillerBravaisPlane newPlane = new MillerBravaisPlane(h, k, i, l) { enabled = true };
        millerBravaisPlanes.Add(newPlane);
        
        if (showPlanes)
        {
            CreateMillerBravaisPlane(h, k, i, l);
        }
    }

    public void ClearPlanes()
    {
        foreach (var plane in planes)
            if (plane != null) DestroyImmediate(plane);
        planes.Clear();
        millerBravaisPlanes.Clear();
    }

    public void TogglePlanes()
    {
        showPlanes = !showPlanes;
        
        if (showPlanes)
        {
            CreatePlanes();
        }
        else
        {
            foreach (var plane in planes)
                if (plane != null) plane.SetActive(false);
        }
    }
}