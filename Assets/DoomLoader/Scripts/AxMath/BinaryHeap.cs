using UnityEngine;

/// <summary>
/// Items that are stored in binary heap need to have this interface to access their total order
/// </summary>
public interface IBinaryHeapItem<T> : System.IComparable<T>
{
    int HeapIndex
    {
        get;
        set;
    }
}

/// <summary>
/// Max-heap datastructure that stores items in order
/// </summary>
public class BinaryHeap<T> where T : IBinaryHeapItem<T>
{
    private T[] items;
    public int ItemCount { get; private set; }

    public BinaryHeap(int maxHeapSize) { items = new T[maxHeapSize]; }

    public T Peek { get { return items[0]; } }
    public void UpdateItem(T item) { SortUp(item); }
    
    //adds an item to the end and moves it up the heap
    public void Add(T item)
    {
        item.HeapIndex = ItemCount;
        items[ItemCount] = item;
        SortUp(item);
        ItemCount++;
    }

    //clears content of first element and moves the item down the heap
    public T RemoveFirst()
    {
        T firstItem = items[0];
        ItemCount--;
        items[0] = items[ItemCount];
        items[0].HeapIndex = 0;
        SortDown(items[0]);
        return firstItem;
    }

    //as long as item's compare value is lower, it moves to the right side or down the heap
    private void SortDown(T item)
    {
        while (true)
        {
            int childIndexLeft = item.HeapIndex * 2 + 1;
            int childIndexRight = item.HeapIndex * 2 + 2;
            int swapIndex = 0;

            if (childIndexLeft < ItemCount)
            {
                swapIndex = childIndexLeft;

                if (childIndexRight < ItemCount)
                    if (items[childIndexLeft].CompareTo(items[childIndexRight]) < 0)
                        swapIndex = childIndexRight;

                if (item.CompareTo(items[swapIndex]) < 0)
                    Swap(item, items[swapIndex]);
                else
                    return;
            }
            else
                return;
        }
    }

    //as long as item's compare value is higher, it moves to the left side or up the heap
    private void SortUp(T item)
    {
        int parentIndex = (item.HeapIndex - 1) / 2;

        while (true)
        {
            T parentItem = items[parentIndex];

            if (item.CompareTo(parentItem) > 0)
                Swap(item, parentItem);
            else
                break;

            parentIndex = (item.HeapIndex - 1) / 2;
        }
    }

    //switch contents of two nodes
    private void Swap(T itemA, T itemB)
    {
        items[itemA.HeapIndex] = itemB;
        items[itemB.HeapIndex] = itemA;
        int itemAIndex = itemA.HeapIndex;
        itemA.HeapIndex = itemB.HeapIndex;
        itemB.HeapIndex = itemAIndex;
    }

    //print out the contents
    public override string ToString()
    {
        string list = "[0]--> ";

        int prevdepth = 0;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
                break;

            int depth = Mathf.FloorToInt(Mathf.Log(i + 1, 2));
            if (depth != prevdepth)
            {
                list += "\r\n";
                list += "[" + depth.ToString() + "]--> ";
                prevdepth = depth;
            }

            list += "(" + items[i].ToString() + ")";
        }

        return list;
    }
}
