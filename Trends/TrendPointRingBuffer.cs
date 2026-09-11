using System;
using System.Collections;
using System.Collections.Generic;

namespace DiplomWork_Ivan_2026.Trends
{
    internal sealed class TrendPointRingBuffer : IReadOnlyList<TrendPoint>
    {
        private readonly TrendPoint?[] _items;
        private int _startIndex;

        public TrendPointRingBuffer(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            _items = new TrendPoint[capacity];
        }

        public int Count { get; private set; }

        public TrendPoint this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                int physicalIndex = (_startIndex + index) % _items.Length;
                return _items[physicalIndex]!;
            }
        }

        public void Add(TrendPoint point)
        {
            if (Count < _items.Length)
            {
                int insertIndex = (_startIndex + Count) % _items.Length;
                _items[insertIndex] = point;
                Count++;
                return;
            }

            _items[_startIndex] = point;
            _startIndex = (_startIndex + 1) % _items.Length;
        }

        public void Clear()
        {
            Array.Clear(_items, 0, _items.Length);
            _startIndex = 0;
            Count = 0;
        }

        public IEnumerator<TrendPoint> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
