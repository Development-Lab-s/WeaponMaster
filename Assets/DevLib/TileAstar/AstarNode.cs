using System;
using UnityEngine;

namespace DevLib.TileAstar
{
    public class AstarNode : IComparable<AstarNode>
    {
        public Vector3 worldPosition;
        public Vector3Int cellPosition;
        public NodeData nodeData; //나중에 만들 구조체.
        
        public AstarNode parentNode; //부모 노드
        
        public float G;
        public float F;
        
        //H는 직선거리라 그냥 구하면 돼
        
        public int CompareTo(AstarNode other)
        {
            if (Mathf.Approximately(other.F, F))
                return 0;
            return other.F < F ? -1 : 1; //오름차순 정렬을 만든다.
            //Quick sort
        }

        public override bool Equals(object obj)
        {
            if (obj is AstarNode node)
            {
                return Equals(node);
            }

            return false;
        }

        public bool Equals(AstarNode node)
        {
            if (node is null) return false;
            return cellPosition == node.cellPosition; //셀포지션이 같으면 같다고 판단한다.
        }

        public override int GetHashCode() => cellPosition.GetHashCode();

        public static bool operator ==(AstarNode lhs, AstarNode rhs)
        {
            if (lhs is null)
            {
                if (rhs is null) return true;
                return false;
            }
            return lhs.Equals(rhs);
        }
        public static bool operator != (AstarNode lhs, AstarNode rhs) => !(lhs == rhs);
    }
}