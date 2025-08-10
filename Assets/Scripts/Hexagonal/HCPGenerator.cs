using System.Collections.Generic;
using UnityEngine;

public class HCPGenerator : MonoBehaviour
{
    public GameObject atomPrefab;
    public GameObject cylinderPrefab;
    public float a = 1f; // Lattice constant a
    public float c = 1.633f; // c/a ratio for ideal HCP
    public int nx = 1, ny = 1, nz = 1;
    public Color atomColor = Color.white;
    public float atomScale = 0.2f;
    public Color bondColor = Color.yellow;
    public float bondRadius = 0.02f;

    private List<GameObject> atoms = new List<GameObject>();
    private List<GameObject> bonds = new List<GameObject>();
    private HashSet<string> _bondKeys = new HashSet<string>();

    void Start()
    {
        GenerateHCP();
    }

    public void GenerateHCP()
    {
        foreach (var atom in atoms)
            if (atom != null) DestroyImmediate(atom);
        foreach (var bond in bonds)
            if (bond != null) DestroyImmediate(bond);
        atoms.Clear();
        bonds.Clear();
        _bondKeys.Clear();

        float sqrt3 = Mathf.Sqrt(3f);
        float cval = c;

        // Lattice vectors rotated 90° around x-axis: y becomes z, z becomes +y
        Vector3 a1 = new Vector3(sqrt3 * a, 0, 0);
        Vector3 a2 = new Vector3(sqrt3 * a / 2f, 0, a * 1.5f);
        Vector3 a3 = new Vector3(0, cval, 0);

        // Hexagon positions rotated 90° around x-axis: XY plane becomes XZ plane
        Vector3[] hexXZ = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            float angle = Mathf.Deg2Rad * (60f * i + 30f);
            hexXZ[i] = new Vector3(a * Mathf.Cos(angle), 0, a * Mathf.Sin(angle));
        }

        // HCP center positions (3 atoms forming equilateral triangle in triangular holes)
        Vector3[] centerPositions = new Vector3[3];
        for (int i = 0; i < 3; i++)
        {
            float angle = Mathf.Deg2Rad * (120f * i); // 120° apart for equilateral triangle
            float radius = a * sqrt3 / 3f; // Distance from center to triangular void
            centerPositions[i] = new Vector3(radius * Mathf.Cos(angle), 0, radius * Mathf.Sin(angle));
        }

        // Use a dictionary to avoid duplicates at boundaries
        Dictionary<Vector3, int> posToIndex = new Dictionary<Vector3, int>(new Vector3Comparer());
        List<Vector3> uniquePositions = new List<Vector3>();
        List<int[]> baseIndices = new List<int[]>();
        List<int[]> topIndices = new List<int[]>();
        List<int[]> centerIndices = new List<int[]>();
        List<int[]> hexCenterIndices = new List<int[]>();

        for (int iz = 0; iz < nz; iz++)
        {
            for (int iy = 0; iy < ny; iy++)
            {
                for (int ix = 0; ix < nx; ix++)
                {
                    Vector3 cellOrigin = ix * a1 + iy * a2 + iz * a3;

                    int[] baseIdx = new int[6];
                    int[] topIdx = new int[6];
                    int[] centerIdx = new int[3];
                    int[] hexCenterIdx = new int[2];

                    // 6 base atoms (y=0)
                    for (int i = 0; i < 6; i++)
                    {
                        Vector3 pos = cellOrigin + hexXZ[i];
                        if (!posToIndex.ContainsKey(pos))
                        {
                            posToIndex[pos] = uniquePositions.Count;
                            uniquePositions.Add(pos);
                        }
                        baseIdx[i] = posToIndex[pos];
                    }

                    // 6 top atoms (y=+c)
                    for (int i = 0; i < 6; i++)
                    {
                        Vector3 pos = cellOrigin + hexXZ[i] + a3;
                        if (!posToIndex.ContainsKey(pos))
                        {
                            posToIndex[pos] = uniquePositions.Count;
                            uniquePositions.Add(pos);
                        }
                        topIdx[i] = posToIndex[pos];
                    }

                    // 3 center atoms (y=+c/2) - positioned in triangular holes
                    for (int i = 0; i < 3; i++)
                    {
                        Vector3 pos = cellOrigin + centerPositions[i] + new Vector3(0, cval / 2f, 0);
                        if (!posToIndex.ContainsKey(pos))
                        {
                            posToIndex[pos] = uniquePositions.Count;
                            uniquePositions.Add(pos);
                        }
                        centerIdx[i] = posToIndex[pos];
                    }

                    // 2 hexagon center atoms (one at base y=0, one at top y=+c)
                    // Base hexagon center
                    Vector3 baseCenterPos = cellOrigin;
                    if (!posToIndex.ContainsKey(baseCenterPos))
                    {
                        posToIndex[baseCenterPos] = uniquePositions.Count;
                        uniquePositions.Add(baseCenterPos);
                    }
                    hexCenterIdx[0] = posToIndex[baseCenterPos];

                    // Top hexagon center
                    Vector3 topCenterPos = cellOrigin + a3;
                    if (!posToIndex.ContainsKey(topCenterPos))
                    {
                        posToIndex[topCenterPos] = uniquePositions.Count;
                        uniquePositions.Add(topCenterPos);
                    }
                    hexCenterIdx[1] = posToIndex[topCenterPos];

                    baseIndices.Add(baseIdx);
                    topIndices.Add(topIdx);
                    centerIndices.Add(centerIdx);
                    hexCenterIndices.Add(hexCenterIdx);
                }
            }
        }

        // Create atoms
        foreach (var pos in uniquePositions)
            atoms.Add(CreateAtom(pos));

        // Bonds: hexagon rings
        foreach (var baseIdx in baseIndices)
        {
            for (int i = 0; i < 6; i++)
            {
                int idx1 = baseIdx[i];
                int idx2 = baseIdx[(i + 1) % 6];
                AddBondIfNotExists(uniquePositions, idx1, idx2);
            }
        }
        foreach (var topIdx in topIndices)
        {
            for (int i = 0; i < 6; i++)
            {
                int idx1 = topIdx[i];
                int idx2 = topIdx[(i + 1) % 6];
                AddBondIfNotExists(uniquePositions, idx1, idx2);
            }
        }

        // Vertical bonds connecting base to top hexagons
        for (int cell = 0; cell < baseIndices.Count; cell++)
        {
            int[] baseIdx = baseIndices[cell];
            int[] topIdx = topIndices[cell];

            for (int i = 0; i < 6; i++)
            {
                AddBondIfNotExists(uniquePositions, baseIdx[i], topIdx[i]);
            }
        }

        // Bonds between the 3 center atoms to form a triangle
        for (int cell = 0; cell < centerIndices.Count; cell++)
        {
            int[] centerIdx = centerIndices[cell];

            // Connect the 3 center atoms in a triangle
            for (int i = 0; i < 3; i++)
            {
                int next = (i + 1) % 3;
                AddBondIfNotExists(uniquePositions, centerIdx[i], centerIdx[next]);
            }
        }

        // NO BONDS TO HEXAGON CENTER ATOMS - they are suspended freely
    }

    void AddBondIfNotExists(List<Vector3> uniquePositions, int idx1, int idx2)
    {
        if (idx1 == idx2) return;
        Vector3 pos1 = uniquePositions[idx1];
        Vector3 pos2 = uniquePositions[idx2];
        string key = idx1 < idx2 ? idx1 + "_" + idx2 : idx2 + "_" + idx1;
        if (_bondKeys == null) _bondKeys = new HashSet<string>();
        if (_bondKeys.Contains(key)) return;
        _bondKeys.Add(key);
        bonds.Add(CreateBond(pos1, pos2));
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