using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class JellyRenderer : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private JellySoftBody body;

    [Header("Visual")]
    [SerializeField] private Color color = new Color(0.15f, 1f, 0.25f, 1f);
    [SerializeField] private Material overrideMaterial;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField, Range(-1000, 1000)] private int sortingOrder = 0;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Mesh _mesh;
    private Material _runtimeMaterial;
    private Vector3[] _vertices;
    private Vector2[] _uv;
    private int[] _triangles;

    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        if (_meshFilter == null)
        {
            _meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        _meshRenderer = GetComponent<MeshRenderer>();
        if (_meshRenderer == null)
        {
            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        _mesh = new Mesh
        {
            name = "JellySkin"
        };
        _mesh.MarkDynamic();
        _meshFilter.sharedMesh = _mesh;

        if (body == null)
        {
            body = GetComponent<JellySoftBody>();
        }
    }

    private void OnEnable()
    {
        ApplyMaterial();
    }

    private void Update()
    {
        RebuildMesh();
    }

    private void OnDestroy()
    {
        if (_runtimeMaterial != null)
        {
            Destroy(_runtimeMaterial);
            _runtimeMaterial = null;
        }
    }

    private void ApplyMaterial()
    {
        if (_meshRenderer == null)
        {
            return;
        }

        if (overrideMaterial != null)
        {
            _meshRenderer.sharedMaterial = overrideMaterial;
        }
        else
        {
            if (_runtimeMaterial == null)
            {
                _runtimeMaterial = CreateJellyMaterial();
            }

            if (_runtimeMaterial != null)
            {
                _meshRenderer.sharedMaterial = _runtimeMaterial;
            }
        }

        if (!string.IsNullOrEmpty(sortingLayerName))
        {
            _meshRenderer.sortingLayerName = sortingLayerName;
        }

        _meshRenderer.sortingOrder = sortingOrder;
    }

    private Material CreateJellyMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader == null)
        {
            Debug.LogWarning("JellyRenderer: No suitable 2D unlit shader was found.", this);
            return null;
        }

        Material material = new Material(shader)
        {
            name = "JellyGreen"
        };
        material.color = color;
        return material;
    }

    private void RebuildMesh()
    {
        if (_mesh == null || body == null || body.EdgeNodeCount < 3)
        {
            return;
        }

        int edgeCount = body.EdgeNodeCount;
        int vertexCount = edgeCount + 1;
        int triangleCount = edgeCount;

        EnsureBuffers(vertexCount, triangleCount);

        _vertices[0] = Vector3.zero;
        _uv[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < edgeCount; i++)
        {
            Vector3 edgeLocal = body.GetEdgeLocalPosition(i);
            _vertices[i + 1] = edgeLocal;
            _uv[i + 1] = new Vector2(
                Mathf.Clamp01((edgeLocal.x / Mathf.Max(0.001f, body.EdgeNodeCount)) + 0.5f),
                Mathf.Clamp01((edgeLocal.y / Mathf.Max(0.001f, body.EdgeNodeCount)) + 0.5f));
        }

        for (int i = 0; i < edgeCount; i++)
        {
            int triangleIndex = i * 3;
            _triangles[triangleIndex] = 0;
            _triangles[triangleIndex + 1] = i + 1;
            _triangles[triangleIndex + 2] = ((i + 1) % edgeCount) + 1;
        }

        _mesh.Clear();
        _mesh.vertices = _vertices;
        _mesh.uv = _uv;
        _mesh.triangles = _triangles;
        _mesh.RecalculateBounds();
    }

    private void EnsureBuffers(int vertexCount, int triangleCount)
    {
        if (_vertices == null || _vertices.Length != vertexCount)
        {
            _vertices = new Vector3[vertexCount];
        }

        if (_uv == null || _uv.Length != vertexCount)
        {
            _uv = new Vector2[vertexCount];
        }

        int triangleIndexCount = triangleCount * 3;
        if (_triangles == null || _triangles.Length != triangleIndexCount)
        {
            _triangles = new int[triangleIndexCount];
        }
    }
}
