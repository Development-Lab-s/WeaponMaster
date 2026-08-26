using System;
using System.Collections.Generic;
using UnityEngine;

namespace DevLib.TileAstar
{
    [Serializable]
    public struct NodeData  : IEquatable<NodeData>
    {
        public Vector3 worldPosition;
        public Vector3Int cellPosition;
        public List<LinkData> neighbors; //이웃들

        public NodeData(Vector3 worldPosition, Vector3Int cellPosition)
        {
            this.worldPosition = worldPosition;
            this.cellPosition = cellPosition;
            neighbors = new List<LinkData>();
        }
        
        public void AddNeighbor(NodeData nextNode)
        {
            neighbors.Add(new LinkData
            {
                startPosition = worldPosition,
                startCellPosition = cellPosition,
                endPosition = nextNode.worldPosition,
                endCellPosition = nextNode.cellPosition,
                cost = Vector3Int.Distance(cellPosition, nextNode.cellPosition)
            });
        }
        
        public bool Equals(NodeData other) => cellPosition == other.cellPosition;

        public override bool Equals(object obj)
            => obj is NodeData node && Equals(node);

        public override int GetHashCode() => cellPosition.GetHashCode();
        
        public static bool operator == (NodeData lhs, NodeData rhs)
            => lhs.Equals(rhs);
        public static bool operator != (NodeData lhs, NodeData rhs)
            => !(lhs == rhs);
    }
}