using System.Collections.Generic;
using UnityEngine;

public class PlaneSC : MonoBehaviour
{
    public GameObject atomPrefab;
    public GameObject cylinderPrefab;
    public float a = 1f;
    public Color atomColor = Color.white;
    public float atomScale = 0.2f;
    public Color bondColor = Color.yellow;
    public float bondRadius = 0.02f;

    [Header("Cube Face Planes")]
    public Material planeMaterial;
    public bool showPlanes = true;
    public Color planeColor = new Color(1f, 0f, 0f, 0.3f); // Semi-transparent red

    [Header("Miller Index Planes")]
    public bool showMillerPlanes = true;
    public Material millerPlaneMaterial;
    public Color millerPlaneColor = new Color(0f, 1f, 0f, 0.4f); // Semi-transparent green
    
    [System.Serializable]
    public class MillerPlane
    {
        public int h, k, l;
        public Color color = Color.green;
        public bool enabled = true;
        
        public MillerPlane(int h, int k, int l)
        {
            this.h = h;
            this.k = k;
            this.l = l;
        }
    }
    
    public List<MillerPlane> millerPlanes = new List<MillerPlane>();

    private List<GameObject> atoms = new List<GameObject>();
    private List<GameObject> bonds = new List<GameObject>();
    private List<GameObject> planes = new List<GameObject>();
    private List<GameObject> millerPlaneObjects = new List<GameObject>();

    void Start()
    {
        GenerateSC();
    }

    public void GenerateSC()
    {
        // Destroy previous atoms, bonds, and planes
        foreach (var atom in atoms)
            if (atom != null) DestroyImmediate(atom);
        foreach (var bond in bonds)
            if (bond != null) DestroyImmediate(bond);
        foreach (var plane in planes)
            if (plane != null) DestroyImmediate(plane);
        foreach (var millerPlane in millerPlaneObjects)
            if (millerPlane != null) DestroyImmediate(millerPlane);
        
        atoms.Clear();
        bonds.Clear();
        planes.Clear();
        millerPlaneObjects.Clear();

        // Store atom positions for fast lookup
        HashSet<Vector3> atomPositions = new HashSet<Vector3>();

        // Generate atoms at lattice points only (Simple Cubic - single unit cell)
        for (int i = 0; i <= 1; i++)
            for (int j = 0; j <= 1; j++)
                for (int k = 0; k <= 1; k++)
                {
                    Vector3 pos = new Vector3(i * a, j * a, -k * a);
                    atomPositions.Add(pos);
                }

        // Create atoms
        foreach (var pos in atomPositions)
            atoms.Add(CreateAtom(pos));

        // Connect bonds between adjacent atoms along cube edges
        Vector3[] directions = new Vector3[]
        {
            new Vector3(a, 0, 0),
            new Vector3(0, a, 0),
            new Vector3(0, 0, -a)
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

        // Generate Miller planes
        if (showPlanes)
        {
            GenerateMillerPlanes();
        }
        
        // Generate custom Miller index planes
        if (showMillerPlanes)
        {
            GenerateCustomMillerPlanes();
        }
    }

    #region Miller Planes Generation
    void GenerateMillerPlanes()
    {
        // Generate the 6 main crystallographic planes for the cube
        GenerateMillerPlane(1, 0, 0, "(100)"); // Right face - X = a
        GenerateMillerPlane(-1, 0, 0, "(1̄00)"); // Left face - X = 0
        GenerateMillerPlane(0, 1, 0, "(010)"); // Top face - Y = a
        GenerateMillerPlane(0, -1, 0, "(01̄0)"); // Bottom face - Y = 0
        GenerateMillerPlane(0, 0, 1, "(001)"); // Back face - Z = a
        GenerateMillerPlane(0, 0, -1, "(001̄)"); // Front face - Z = 0
    }

    void GenerateMillerPlane(int h, int k, int l, string planeName)
    {
        // Calculate cube dimensions for single unit cell
        float cubeWidth = a;
        float cubeHeight = a;
        float cubeDepth = a;

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
            CreatePlaneMesh(validPoints, planeName, planeMaterial, planeColor);
        }
    }

    List<Vector3> GetPlaneEdgeIntersections(int h, int k, int l, float cubeWidth, float cubeHeight, float cubeDepth)
    {
        List<Vector3> points = new List<Vector3>();
        
        // Determine plane equation constant based on Miller indices
        float d = 0;
        if (h > 0) d = h * cubeWidth;
        else if (h < 0) d = 0;
        else if (k > 0) d = k * cubeHeight;
        else if (k < 0) d = 0;
        else if (l > 0) d = l * cubeDepth;
        else if (l < 0) d = 0;
        
        // Define cube vertices
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
        // For flipped Z coordinate system: hx + ky - lz = d
        float denominator = h * direction.x + k * direction.y - l * direction.z;
        
        if (Mathf.Abs(denominator) < 0.0001f) // Line is parallel to plane
            return false;
        
        float t = (d - (h * p1.x + k * p1.y - l * p1.z)) / denominator;
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
               point.z >= -cubeDepth - tolerance && point.z <= tolerance;
    }

    void CreatePlaneMesh(List<Vector3> points, string planeName, Material material, Color color)
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
        if (material != null)
        {
            meshRenderer.material = material;
        }
        else
        {
            // Create default material
            Material defaultMaterial = new Material(Shader.Find("Standard"));
            defaultMaterial.color = color;
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
    #endregion

    #region Custom Miller Index Planes
    /// <summary>
    /// Generates all custom Miller index planes from the millerPlanes list
    /// </summary>
    void GenerateCustomMillerPlanes()
    {
        for (int i = 0; i < millerPlanes.Count; i++)
        {
            if (millerPlanes[i].enabled)
            {
                GeneratePlane(millerPlanes[i].h, millerPlanes[i].k, millerPlanes[i].l, millerPlanes[i].color);
            }
        }
    }

    /// <summary>
    /// Generates a crystallographic plane based on Miller indices (h, k, l)
    /// </summary>
    /// <param name="h">Miller index h</param>
    /// <param name="k">Miller index k</param>
    /// <param name="l">Miller index l</param>
    /// <param name="planeColor">Color of the plane (optional)</param>
    public void GeneratePlane(int h, int k, int l, Color? planeColor = null)
    {
        if (h == 0 && k == 0 && l == 0)
        {
            Debug.LogWarning("Invalid Miller indices: (0,0,0) does not define a plane");
            return;
        }

        // Use provided color or default
        Color color = planeColor ?? millerPlaneColor;
        
        // Calculate intercepts with coordinate axes
        List<Vector3> intercepts = CalculateIntercepts(h, k, l);
        
        if (intercepts.Count < 3)
        {
            Debug.LogWarning($"Cannot create plane for Miller indices ({h},{k},{l}): insufficient intercepts");
            return;
        }

        // Create plane name
        string planeName = $"({FormatMillerIndex(h)}{FormatMillerIndex(k)}{FormatMillerIndex(l)})";
        
        // Generate the plane mesh
        CreateMillerPlaneMesh(intercepts, planeName, color);
        
        // Create intercept markers (small spheres)
        CreateInterceptMarkers(intercepts, planeName, color);
    }

    /// <summary>
    /// Calculates intercept points for a plane with given Miller indices
    /// Correct logic: intercept = a / hkl, with origin shift for negative indices
    /// </summary>
    List<Vector3> CalculateIntercepts(int h, int k, int l)
    {
        List<Vector3> intercepts = new List<Vector3>();
        
        // Step 1: Determine origin shift based on negative indices
        Vector3 origin = Vector3.zero;
        if (h < 0) origin.x = a; // Shift to positive x if h is negative
        if (k < 0) origin.y = a; // Shift to positive y if k is negative  
        if (l < 0) origin.z = -a; // Shift to negative z if l is negative
        
        // Step 2: Calculate intercepts using a/hkl formula
        // X-intercept: x = a/h (from origin)
        if (h != 0)
        {
            float x_intercept = origin.x + (a / h);
            intercepts.Add(new Vector3(x_intercept, origin.y, origin.z));
        }

        // Y-intercept: y = a/k (from origin)
        if (k != 0)
        {
            float y_intercept = origin.y + (a / k);
            intercepts.Add(new Vector3(origin.x, y_intercept, origin.z));
        }

        // Z-intercept: z = -a/l (from origin) - flipped coordinate system
        if (l != 0)
        {
            float z_intercept = origin.z - (a / l);
            intercepts.Add(new Vector3(origin.x, origin.y, z_intercept));
        }

        // Step 3: Handle special cases where indices are zero (parallel to axis)
        if (intercepts.Count < 3)
        {
            intercepts = HandleSpecialCases(h, k, l, intercepts, origin);
        }
        
        // Step 4: For planes parallel to coordinate planes, return the special case points directly
        if ((h == 0 && k == 0) || (h == 0 && l == 0) || (k == 0 && l == 0))
        {
            return intercepts;
        }
        
        // Step 5: Find additional intersection points with cube edges for complete plane
        if (intercepts.Count >= 3)
        {
            List<Vector3> allPoints = new List<Vector3>(intercepts);
            
            // Find where the plane intersects cube edges
            List<Vector3> edgeIntersections = FindPlaneEdgeIntersections(h, k, l, origin);
            
            foreach (Vector3 point in edgeIntersections)
            {
                if (!ContainsPoint(allPoints, point))
                {
                    allPoints.Add(point);
                }
            }
            
            return allPoints;
        }

        return intercepts;
    }

    /// <summary>
    /// Finds intersection points between the Miller plane and cube edges
    /// Uses plane equation derived from the three intercepts
    /// </summary>
    List<Vector3> FindPlaneEdgeIntersections(int h, int k, int l, Vector3 origin)
    {
        List<Vector3> intersections = new List<Vector3>();
        
        // Define cube vertices
        Vector3[] cubeVertices = {
            new Vector3(0, 0, 0), new Vector3(a, 0, 0), new Vector3(a, a, 0), new Vector3(0, a, 0),
            new Vector3(0, 0, -a), new Vector3(a, 0, -a), new Vector3(a, a, -a), new Vector3(0, a, -a)
        };
        
        // Define cube edges (pairs of vertex indices)
        int[,] edges = {
            {0,1}, {1,2}, {2,3}, {3,0}, // bottom face
            {4,5}, {5,6}, {6,7}, {7,4}, // top face
            {0,4}, {1,5}, {2,6}, {3,7}  // vertical edges
        };
        
        // Calculate plane equation constant using the intercept method
        // For flipped Z coordinate system: hx + ky - lz = a
        // The plane passes through points: (a/h, 0, 0), (0, a/k, 0), (0, 0, -a/l)
        float d = 0;
        
        if (h != 0 && k != 0 && l != 0)
        {
            // Calculate d using the X-intercept point for consistency with origin shift
            Vector3 interceptPoint = new Vector3(origin.x + a/h, origin.y, origin.z);
            d = h * interceptPoint.x + k * interceptPoint.y - l * interceptPoint.z;
        }
        else if (h == 0 && k != 0 && l != 0)
        {
            // Plane parallel to x-axis
            Vector3 interceptPoint = new Vector3(origin.x, origin.y + a/k, origin.z);
            d = h * interceptPoint.x + k * interceptPoint.y - l * interceptPoint.z;
        }
        else if (h != 0 && k == 0 && l != 0)
        {
            // Plane parallel to y-axis
            Vector3 interceptPoint = new Vector3(origin.x + a/h, origin.y, origin.z);
            d = h * interceptPoint.x + k * interceptPoint.y - l * interceptPoint.z;
        }
        else if (h != 0 && k != 0 && l == 0)
        {
            // Plane parallel to z-axis
            Vector3 interceptPoint = new Vector3(origin.x + a/h, origin.y, origin.z);
            d = h * interceptPoint.x + k * interceptPoint.y - l * interceptPoint.z;
        }
        
        for (int i = 0; i < edges.GetLength(0); i++)
        {
            Vector3 p1 = cubeVertices[edges[i, 0]];
            Vector3 p2 = cubeVertices[edges[i, 1]];
            
            Vector3 intersection;
            if (GetLinePlaneIntersection(p1, p2, h, k, l, d, out intersection))
            {
                if (IsPointOnSegment(p1, p2, intersection))
                {
                    intersections.Add(intersection);
                }
            }
        }
        
        return intersections;
    }

    /// <summary>
    /// Checks if point is within extended cube bounds for plane visibility
    /// </summary>
    bool IsPointInExtendedCube(Vector3 point)
    {
        float tolerance = 0.1f;
        return point.x >= -tolerance && point.x <= a + tolerance && 
               point.y >= -tolerance && point.y <= a + tolerance && 
               point.z >= -a - tolerance && point.z <= tolerance;
    }

    /// <summary>
    /// Handles special cases where Miller indices contain zeros (parallel to axes)
    /// </summary>
    List<Vector3> HandleSpecialCases(int h, int k, int l, List<Vector3> existingIntercepts, Vector3 origin)
    {
        List<Vector3> points = new List<Vector3>(existingIntercepts);
        
        // Case: plane parallel to two axes (two indices are zero)
        if ((h == 0 && k == 0) || (h == 0 && l == 0) || (k == 0 && l == 0))
        {
            if (h == 0 && k == 0 && l != 0) // Plane parallel to XY plane, intersects Z-axis
            {
                float z = origin.z - a / l;
                points.AddRange(new Vector3[] {
                    new Vector3(0, 0, z), new Vector3(a, 0, z),
                    new Vector3(a, a, z), new Vector3(0, a, z)
                });
            }
            else if (h == 0 && l == 0 && k != 0) // Plane parallel to XZ plane, intersects Y-axis
            {
                float y = origin.y + a / k;
                points.AddRange(new Vector3[] {
                    new Vector3(0, y, 0), new Vector3(a, y, 0),
                    new Vector3(a, y, -a), new Vector3(0, y, -a)
                });
            }
            else if (k == 0 && l == 0 && h != 0) // Plane parallel to YZ plane, intersects X-axis
            {
                float x = origin.x + a / h;
                points.AddRange(new Vector3[] {
                    new Vector3(x, 0, 0), new Vector3(x, a, 0),
                    new Vector3(x, a, -a), new Vector3(x, 0, -a)
                });
            }
        }
        // Case: plane parallel to one axis (one index is zero) - need to find plane intersection with cube
        else if ((h == 0 && k != 0 && l != 0) || (h != 0 && k == 0 && l != 0) || (h != 0 && k != 0 && l == 0))
        {
            // Calculate plane equation constant d using one of the intercepts
            float d = 0;
            if (h == 0 && k != 0 && l != 0) // Parallel to x-axis
            {
                Vector3 interceptPoint = new Vector3(origin.x, origin.y + a/k, origin.z);
                d = h * interceptPoint.x + k * interceptPoint.y - l * interceptPoint.z;
            }
            else if (k == 0 && h != 0 && l != 0) // Parallel to y-axis
            {
                Vector3 interceptPoint = new Vector3(origin.x + a/h, origin.y, origin.z);
                d = h * interceptPoint.x + k * interceptPoint.y - l * interceptPoint.z;
            }
            else if (l == 0 && h != 0 && k != 0) // Parallel to z-axis
            {
                Vector3 interceptPoint = new Vector3(origin.x + a/h, origin.y, origin.z);
                d = h * interceptPoint.x + k * interceptPoint.y - l * interceptPoint.z;
            }
            
            // Find cube corner points that lie on the plane
            Vector3[] cubeCorners = {
                new Vector3(0, 0, 0), new Vector3(a, 0, 0), new Vector3(a, a, 0), new Vector3(0, a, 0),
                new Vector3(0, 0, -a), new Vector3(a, 0, -a), new Vector3(a, a, -a), new Vector3(0, a, -a)
            };

            foreach (Vector3 corner in cubeCorners)
            {
                float planeValue = h * corner.x + k * corner.y - l * corner.z;
                if (Mathf.Abs(planeValue - d) < 0.01f) // Point is on the plane
                {
                    if (!ContainsPoint(points, corner))
                    {
                        points.Add(corner);
                    }
                }
            }
            
            // If still not enough points, use edge intersections
            if (points.Count < 3)
            {
                List<Vector3> edgeIntersections = FindPlaneEdgeIntersections(h, k, l, origin);
                foreach (Vector3 point in edgeIntersections)
                {
                    if (!ContainsPoint(points, point))
                    {
                        points.Add(point);
                    }
                }
            }
        }

        return points;
    }

    /// <summary>
    /// Checks if a point lies on the plane defined by Miller indices
    /// </summary>
    bool IsPointOnPlane(Vector3 point, int h, int k, int l)
    {
        float tolerance = 0.0001f;
        float planeValue = h * point.x + k * point.y - l * point.z;
        
        // Calculate expected value using origin shift logic
        Vector3 origin = Vector3.zero;
        if (h < 0) origin.x = a;
        if (k < 0) origin.y = a;  
        if (l < 0) origin.z = -a;
        
        Vector3 interceptPoint = new Vector3(origin.x + a/h, origin.y, origin.z);
        float expectedValue = h * interceptPoint.x + k * interceptPoint.y - l * interceptPoint.z;
        
        return Mathf.Abs(planeValue - expectedValue) < tolerance;
    }

    /// <summary>
    /// Creates the actual mesh for a Miller index plane
    /// Allows planes to extend slightly beyond cube for better visualization
    /// </summary>
    void CreateMillerPlaneMesh(List<Vector3> points, string planeName, Color color)
    {
        if (points.Count < 3) return;

        // Keep points that are within reasonable bounds (allow slight extension for visibility)
        List<Vector3> validPoints = new List<Vector3>();
        foreach (Vector3 point in points)
        {
            // Allow points slightly outside cube for better plane visibility
            if (point.x >= -0.1f && point.x <= a + 0.1f &&
                point.y >= -0.1f && point.y <= a + 0.1f &&
                point.z >= -a - 0.1f && point.z <= 0.1f)
            {
                validPoints.Add(point);
            }
        }

        if (validPoints.Count < 3) return;

        CreatePlaneMesh(validPoints, planeName, millerPlaneMaterial, color);
        millerPlaneObjects.Add(planes[planes.Count - 1]); // Add to Miller plane objects list
    }

    /// <summary>
    /// Creates small sphere markers at intercept points
    /// </summary>
    void CreateInterceptMarkers(List<Vector3> intercepts, string planeName, Color color)
    {
        for (int i = 0; i < intercepts.Count; i++)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = $"Intercept_{planeName}_{i}";
            marker.transform.parent = transform;
            marker.transform.position = intercepts[i];
            marker.transform.localScale = Vector3.one * 0.1f;

            Renderer renderer = marker.GetComponent<Renderer>();
            Material markerMaterial = new Material(Shader.Find("Standard"));
            markerMaterial.color = color;
            renderer.material = markerMaterial;

            millerPlaneObjects.Add(marker);
        }
    }

    /// <summary>
    /// Formats Miller index for display (handles negative indices with bar notation)
    /// </summary>
    string FormatMillerIndex(int index)
    {
        if (index < 0)
            return $"{Mathf.Abs(index)}̄"; // Bar notation for negative
        return index.ToString();
    }

    /// <summary>
    /// Public method to add a new Miller plane at runtime
    /// </summary>
    public void AddMillerPlane(int h, int k, int l, Color color)
    {
        millerPlanes.Add(new MillerPlane(h, k, l) { color = color });
        if (showMillerPlanes)
        {
            GeneratePlane(h, k, l, color);
        }
    }

    /// <summary>
    /// Public method to clear all Miller planes
    /// </summary>
    public void ClearMillerPlanes()
    {
        foreach (var plane in millerPlaneObjects)
        {
            if (plane != null) DestroyImmediate(plane);
        }
        millerPlaneObjects.Clear();
        millerPlanes.Clear();
    }
    #endregion

    public void TogglePlanes()
    {
        showPlanes = !showPlanes;
        foreach (var plane in planes)
        {
            if (plane != null)
                plane.SetActive(showPlanes);
        }
    }

    public void ToggleMillerPlanes()
    {
        showMillerPlanes = !showMillerPlanes;
        foreach (var plane in millerPlaneObjects)
        {
            if (plane != null)
                plane.SetActive(showMillerPlanes);
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