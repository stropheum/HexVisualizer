using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

public class HexGridGenerator : MonoBehaviour
{
    private const float PointToPointLength = 1.5f;
    private const float TileOffsetY = 0.435f;
    
    [SerializeField] private GameObject _hexTilePrefab;
    [SerializeField] private Vector2Int _hexTileSize;
    
    private ObjectPool<GameObject> _pool;

    private void Awake()
    {
        Debug.Assert(_hexTilePrefab != null, nameof(_hexTilePrefab) + " != null", this);
    }

    private IEnumerator Start()
    {
        yield return GenerateGrid();
    }
    
    private IEnumerator GenerateGrid()
    {
        _pool ??= new ObjectPool<GameObject>(CreateFunc, ActionOnGet, ActionOnRelease, ActionOnDestroy);
        Vector2 origin = new Vector2(_hexTileSize.x * PointToPointLength, _hexTileSize.y * TileOffsetY) / -2f;

        for (int i = 0; i < _hexTileSize.y; i++)
        {
            for (int j = 0; j < _hexTileSize.x; j++)
            {
                GameObject go = _pool.Get();
                float x = (j * PointToPointLength) + 0.75f * (i % 2);
                float y = i * TileOffsetY;
                go.transform.position = new Vector3(origin.x + x, 0, origin.y + y);
                yield return null;
            }
        }
    }

    private GameObject CreateFunc()
    {
        GameObject go = Instantiate(_hexTilePrefab, transform);
        go.gameObject.SetActive(false);
        return go;
    }
    
    private void ActionOnGet(GameObject obj)
    {
        obj.SetActive(true);
    }
    
    private void ActionOnRelease(GameObject obj)
    {
        obj.SetActive(false);
    }
    
    private void ActionOnDestroy(GameObject obj)
    {
        
    }
}
