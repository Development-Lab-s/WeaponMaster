using System;
using UnityEngine;

namespace DevLib.TileAstar
{
    [Serializable]
    public struct LinkData
    {
        public Vector3 startPosition;
        public Vector3Int startCellPosition;
        public Vector3 endPosition;
        public Vector3Int endCellPosition;
        public float cost; //이 링크를 건너는 실제 값
    }
}