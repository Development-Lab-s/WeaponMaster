#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using UnityEngine.Tilemaps;

namespace DevLib.TileAstar
{
    public class PathBaker : MonoBehaviour
    {
        [SerializeField] private Tilemap groundMap;
        [SerializeField] private Tilemap obstacleMap;
        [SerializeField] private PathBakeDataSO bakedData;

        [SerializeField] private bool isDrawGizmo = true;
        [SerializeField] private bool isCornerCheck = true;
        [SerializeField] private Color nodeColor, edgeColor;

        [ContextMenu("Bake Map")]
        private void BakeMap()
        {
            if (groundMap == null || obstacleMap == null || bakedData == null)
            {
                Debug.LogError("필수 에셋이 할당되어 있지 않습니다.");
                return;
            }

            WritePointData();
            RecordNeighbors(); //이웃들을 기록할거고
            WriteIfInUnityEditor(); //에디터 타임이라면 SO에 값을 새롭게 저장해준다.
        }

        private void Awake()
        {
            bakedData?.InitializeBakeData(); //이거 안하면 딕셔너리 안만들어진다. 
        }

        private void WritePointData()
        {
            bakedData.ClearPoints();
            groundMap.CompressBounds();
            
            BoundsInt mapBounds = groundMap.cellBounds;

            for (int x = mapBounds.xMin; x < mapBounds.xMax; x++)
            {
                for (int y = mapBounds.yMin; y < mapBounds.yMax; y++)
                {
                    Vector3Int cellPosition = new Vector3Int(x, y, 0);

                    if (CanMovePosition(cellPosition))
                    {
                        AddPoint(cellPosition);
                    }
                }
            }

            bakedData.InitializeBakeData(); //이거 해서 딕셔너리 만들어줘야 한다.
        }
        private void AddPoint(Vector3Int cellPosition)
        {
            Vector3 worldPosition = groundMap.GetCellCenterWorld(cellPosition);
            bakedData.AddPoint(worldPosition, cellPosition);
        }
        
        private bool CanMovePosition(Vector3Int cellPosition)
        {
            bool hasObstacle = obstacleMap.HasTile(cellPosition);//해당 셀에 장애물이 있는가?
            bool hasGround = groundMap.HasTile(cellPosition);//해당 셀에 땅이 있는가?
            
            return !hasObstacle && hasGround;
        }

        private void RecordNeighbors()
        {
            foreach (NodeData node in bakedData.points)
            {
                node.neighbors.Clear();

                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y ==0) continue; //자기 자신은 더할 필요 없다.
                        
                        //인접노드가 존재한다면
                        Vector3Int nextPoint = new Vector3Int(x, y) + node.cellPosition;
                        if (bakedData.GetNodeIfExist(nextPoint, out NodeData adjacentNode))
                        {
                            if (CheckCorner(nextPoint, node.cellPosition))
                            {
                                node.AddNeighbor(adjacentNode); //인접 노드 추가.
                            }
                        }
                    }
                }
            }
        }

        private bool CheckCorner(Vector3Int nextPoint, Vector3Int currentPoint)
        {
            if (!isCornerCheck) return true;
            
            return CanMovePosition(new Vector3Int(nextPoint.x, currentPoint.y)) 
                && CanMovePosition(new Vector3Int(currentPoint.x, nextPoint.y));
        }

        private void WriteIfInUnityEditor()
        {
            #if UNITY_EDITOR
            EditorUtility.SetDirty(bakedData);
            AssetDatabase.SaveAssets();
            #endif
        }


        private void OnDrawGizmosSelected()
        {
            if (!isDrawGizmo) return;
            if (bakedData == null) return;

            foreach (NodeData nodeData in bakedData.points)
            {
                Gizmos.color = nodeColor;
                Gizmos.DrawWireSphere(nodeData.worldPosition, 0.2f);

                foreach (LinkData link in nodeData.neighbors)
                {
                    Gizmos.color = edgeColor;
                    DrawArrowGizmo(link.startPosition, link.endPosition);
                }
            }
        }

        private void DrawArrowGizmo(Vector3 start, Vector3 end)
        {
            Vector3 dir = end - start;
            Vector3 normalDir = dir.normalized;
            
            Vector3 arrowStart = end - normalDir * 0.25f;
            Vector3 arrowEnd = end - normalDir * 0.15f;
            const float arrowSize = 0.05f;

            Vector3 triangleA = arrowStart + (Quaternion.Euler(0, 0, -90f) * normalDir) * arrowSize;
            Vector3 triangleB = arrowStart + (Quaternion.Euler(0, 0, 90f) * normalDir) * arrowSize;
            
            Gizmos.DrawLine(start, arrowStart);
            Gizmos.DrawLine(triangleA, arrowEnd);
            Gizmos.DrawLine(triangleB, arrowEnd);
            Gizmos.DrawLine(triangleA, triangleB);
            
        }
    }
}