using System;
using System.Collections.Generic;

namespace DevLib.TileAstar
{
    public class PriorityQueue<T> where T : IComparable<T>
    {
        public List<T> heap = new List<T>();
        public int Count => heap.Count;
        
        public void Clear() => heap?.Clear();

        public T Contains(T t)
        {
            int idx = heap.IndexOf(t);
            if (idx < 0) return default(T);
            return heap[idx];
        }

        public void Push(T data)
        {
            heap.Add(data); //맨 끝에 데이터를 넣어주고
            HeapifyUp(heap.Count - 1); //맨 끝 인덱스부터 올라가면서 자리를 찾는다.
        }

        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parentIdx = (index - 1) / 2;
                if (heap[index].CompareTo(heap[parentIdx]) < 0)
                    break;
                
                (heap[index], heap[parentIdx]) = (heap[parentIdx], heap[index]);
                index = parentIdx;
            }
        }

        public void DecreaseKey(T item)
        {
            int index = heap.IndexOf(item);
            if(index < 0) return;
            
            HeapifyUp(index); //해당 인덱스부터 위로 올리면서 검사한다.
        }

        public T Pop()
        {
            T ret = heap[0];
            
            int lastIdx = heap.Count - 1;
            heap[0] = heap[lastIdx]; //마지막 원소를 앞으로 가져온다.
            heap.RemoveAt(lastIdx); //마지막 원소는 제거
            lastIdx--;

            int now = 0;

            while (true)
            {
                int left = 2 * now + 1;
                int right = 2 * now + 2;

                int next = now; //다음에다가 자기자신을 넣어두고
                if(left <= lastIdx && heap[next].CompareTo(heap[left]) < 0)
                    next = left; //만약 왼쪽이 더 작다면 다음에 갈곳은 왼쪽이다.
                if (right <= lastIdx && heap[next].CompareTo(heap[right]) < 0)
                    next = right;
                
                if(next == now)
                    break;
                
                (heap[now], heap[next]) = (heap[next], heap[now]);
                now = next;
            }
            
            return ret;
        }
        
        public T Peek()
        {
            return heap.Count == 0 ? default(T) : heap[0];    
        }
    }
    
}