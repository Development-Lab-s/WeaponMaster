using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace DevLib.TileAstar
{
    public class PathAgent : MonoBehaviour
    {
        [SerializeField] private PathBakeDataSO bakedData;
        
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private Task<(List<AstarNode> nodes, bool isSuccess)> _calculatingTask; //경로 계산 Task
        private bool _isCalculating;
        private Vector3Int _lastDestination;
        
        private readonly Stack<AstarNode> _nodePool = new Stack<AstarNode>();
        private readonly List<AstarNode> _rentedNodes = new List<AstarNode>();
        
        public bool PathPending => _isCalculating; //현재 계산중인가?
        public bool HasPath { get; private set; }
        public bool IsPathStale { get; private set; } //현재 경로는 버려져야하는가?

        private (List<AstarNode> nodes, bool isSuccess) CalculatePath(
            Vector3Int startPosition, Vector3Int destination, CancellationToken ct)
        {
            foreach (AstarNode node in _rentedNodes)
            {
                node.parentNode = null; //참조 순환을 막기 위해서 깨끗하게 밀어버린다.
                _nodePool.Push(node);
            }
            _rentedNodes.Clear();
            
            //초기화 
            PriorityQueue<AstarNode> openList = new PriorityQueue<AstarNode>();
            HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();
            List<AstarNode> path = new List<AstarNode>();
            AstarNode foundNode = null;

            if (bakedData.GetNodeIfExist(startPosition, out NodeData startNode) == false)
                return (path, false);
            if(bakedData.GetNodeIfExist(destination, out NodeData destNode) == false)
                return (path, false);

            AstarNode startAstar = Rent(); //시작 노드 빌려온다.
            startAstar.nodeData = startNode;
            startAstar.cellPosition = startNode.cellPosition;
            startAstar.worldPosition = startNode.worldPosition;
            startAstar.parentNode = null;
            startAstar.G = 0;
            startAstar.F = CalcH(startNode.cellPosition, destNode.cellPosition);
            openList.Push(startAstar); //시작점 설정후에 오픈리스트에 넣는다.
            
            //Astar 메인 루프 시작
            while (openList.Count > 0)
            {
                if(ct.IsCancellationRequested)
                    break;

                AstarNode currentNode = openList.Pop(); //가장 작은 값을 가지는 노드를 꺼낸다.
                
                //꺼낸 노드가 이미 방문한 closed에 들어간 노드라면 이건 취소
                if(closedSet.Contains(currentNode.cellPosition))
                    continue;

                closedSet.Add(currentNode.cellPosition); //현재 노드를 방문처리하고 내려간다.
                
                //새로운 노드를 꺼냈더니 그게 목적지인거 즉 도착한거
                if (currentNode.nodeData == destNode)
                {
                    foundNode = currentNode;
                    break;
                }
                
                //주변에 갈 수 있는 노드들을 다 찾아서 OpenList에 넣어줘야 한다.
                foreach (LinkData link in currentNode.nodeData.neighbors)
                {
                    if(closedSet.Contains(link.endCellPosition)) continue;
                    
                    if(bakedData.GetNodeIfExist(link.endCellPosition, out NodeData nextNode) == false)
                        continue;

                    float newG = link.cost + currentNode.G;

                    AstarNode nextAstar = Rent();
                    nextAstar.nodeData = nextNode;
                    nextAstar.cellPosition = nextNode.cellPosition;
                    nextAstar.worldPosition = nextNode.worldPosition;
                    nextAstar.parentNode = currentNode;
                    nextAstar.G = newG;
                    nextAstar.F = newG + CalcH(nextNode.cellPosition, destNode.cellPosition);
                    
                    //이미 오픈리스트에 있다면 어떤 값이 작은지 체크해야해.
                    AstarNode existInOpenNode = openList.Contains(nextAstar);
                    if (existInOpenNode != null)
                    {
                        if (nextAstar.G < existInOpenNode.G)
                        {
                            existInOpenNode.G = nextAstar.G;
                            existInOpenNode.F = nextAstar.F;
                            existInOpenNode.parentNode = nextAstar.parentNode;
                            openList.DecreaseKey(existInOpenNode); //우선순위 갱신
                        }
                        ReturnLast(); //현재 nextAstar는 사용되지 않으므로 반납한다.
                    }
                    else
                    {
                        openList.Push(nextAstar);
                    }
                }
            }

            if (foundNode != null)
            {
                AstarNode node = foundNode;
                while (node != null)
                {
                    path.Add(node);
                    node = node.parentNode;
                }
                path.Reverse();
            }
            
            return (path, foundNode != null);
        }

        public void CancelPath()
        {
            if(_isCalculating)
                _cts?.Cancel();
        }
        
        public async Task<int> GetPath(Vector3Int startPosition, Vector3Int destination, Vector3[] pointArr)
        {
            if (_isCalculating)
            {
                _cts?.Cancel(); //이전 테스크를 종료시킨다.
                if(_calculatingTask != null)  //이전 태스크가 있다면 종료될때까지 기다려야 한다.
                    try
                    {
                        await _calculatingTask.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) { }
            }

            //경로는 존재하지만 목적지가 다르다면 경로가 상했음을 알린다.
            if (HasPath && destination != _lastDestination)
                IsPathStale = true;
            
            //null이거나 이미 취소된 경우 새로 생성한다. 
            if (_cts is null or { IsCancellationRequested: true })
                _cts = new CancellationTokenSource();
            
            CancellationToken ct = _cts.Token; //토큰을 로컬 변수로 캡쳐

            try
            {
                _isCalculating = true;
                _calculatingTask = Task.Run(() => CalculatePath(startPosition, destination, ct), ct);
                (List<AstarNode> list, bool isSuccess) = await _calculatingTask; //작업 종료 대기

                //취소되었다면 버퍼를 절대 건드리지 않도록.
                if (ct.IsCancellationRequested)
                    return 0;
                
                int cornerIndex = 0;

                if (isSuccess)
                {
                    pointArr[cornerIndex] = list[0].worldPosition; //시작점
                    cornerIndex++;

                    for (int i = 1; i < list.Count - 1; i++)
                    {
                        if (cornerIndex >= pointArr.Length) break;

                        Vector3Int beforeDir = list[i].cellPosition - list[i - 1].cellPosition;
                        Vector3Int nextDir = list[i + 1].cellPosition - list[i].cellPosition;
                        if (beforeDir != nextDir) //꺽인점에서 경로 기록
                        {
                            pointArr[cornerIndex] = list[i].worldPosition;
                            cornerIndex++;
                        }
                    }

                    //시작점과 다른 목표지일 경우 마지막 포인트 추가.
                    if (list.Count > 1 && cornerIndex < pointArr.Length)
                    {
                        pointArr[cornerIndex] = list[^1].worldPosition;
                        cornerIndex++;
                    }

                    HasPath = true;
                    IsPathStale = false;
                    _lastDestination = destination;
                }
                else
                {
                    HasPath = false;
                    IsPathStale = false;
                }

                return cornerIndex;
            } //end of try
            catch (Exception ex)
            {
                Debug.LogError(ex);
                HasPath = false;
                IsPathStale = false;
                return -1;
            }
            finally
            {
                _isCalculating = false;
            }
        }



        #region Helper method

        private AstarNode Rent()
        {
            AstarNode node = _nodePool.Count > 0 ? _nodePool.Pop() : new AstarNode();
            _rentedNodes.Add(node);
            return node;
        }

        //풀에서 꺼낸뒤 Push를 못하거 버려지는 경우에는 반납
        private void ReturnLast()
        {
            int last = _rentedNodes.Count - 1;
            AstarNode node = _rentedNodes[last];
            _rentedNodes.RemoveAt(last);
            node.parentNode = null;
            _nodePool.Push(node);
        }

        private float CalcH(Vector3Int startPoint, Vector3Int destinationPoint)
            => Vector3Int.Distance(startPoint, destinationPoint);
        
        #endregion
    }
}