public class SingleLinkedList<T>
{
    public class ListNode
    {
        public T Data { get; private set; }
        public ListNode Next { get; set; }

        public ListNode(T dataValue) : this(dataValue, null) { }
        public ListNode(T dataValue, ListNode nextNode)
        {
            Data = dataValue;
            Next = nextNode;
        }
    }

    public int Count { get; private set; }
    public bool Empty { get { return Count == 0; } }
    public ListNode Head { get; private set; }
    public ListNode Tail { get; private set; }

    public SingleLinkedList() { Head = Tail = null; }

    public void InsertFront(T item)
    {
        if (Empty)
            Head = Tail = new ListNode(item);
        else
            Head = new ListNode(item, Head);

        Count++;
    }

    public void InsertBack(T item)
    {
        if (Empty)
            Head = Tail = new ListNode(item);
        else
            Tail.Next = Tail = new ListNode(item, null);

        Count++;
    }

    public void Append(T item, ListNode target)
    {
        if (target == null)
            return;

        if (target == Tail)
            InsertBack(item);
        else
        {
            target.Next = new ListNode(item, target.Next);
            Count++;
        }
    }

    /// <summary>
    /// Slow operation. Consider using DoubleLinkedList instead.
    /// </summary>
    public void Prepend(T item, ListNode target)
    {
        if (target == null)
            return;

        if (target == Head)
            InsertFront(item);
        else
        {
            ListNode current = Head;
            while (current != null)
            {
                if (current.Next == target)
                {
                    current.Next = new ListNode(item, target);
                    Count++;
                    return;
                }

                current = current.Next;
            }
        }
    }

    public T RemoveHead()
    {
        if (Empty)
            return default(T);

        T selection = Head.Data;
        Head = Head.Next;

        Count--;

        return selection;
    }

    /// <summary>
    /// Slow operation. Consider using DoubleLinkedList instead.
    /// </summary>
    public T RemoveTail()
    {
        if (Empty)
            return default(T);

        T selection = Tail.Data;

        ListNode current = Tail = Head;
        while (current.Next != null)
        {
            Tail = current;
            current = Tail.Next;
        }

        if (Tail != null)
            Tail.Next = null;

        Count--;

        return selection;
    }

    /// <summary>
    /// Sends head to back of the list.
    /// </summary>
    public void Slap()
    {
        if (Count < 2)
            return;

        Tail.Next = Head;
        Tail = Head;
        Head = Tail.Next;
        Tail.Next = null;
    }

    /// <summary>
    /// Brings tail to front of the list.
    /// Slow operation. Consider using DoubleLinkedList instead.
    /// </summary>
    public void Rewind()
    {
        if (Count < 2)
            return;

        Tail.Next = Head;
        Head = Tail;

        ListNode current = Head;
        while (current.Next != Tail)
            current = current.Next;

        Tail = current;
        Tail.Next = null;
    }

    public bool Contains(T item)
    {
        ListNode current = Head;
        while (current != null)
        {
            if (current.Data.Equals(item))
                return true;

            current = current.Next;
        }

        return false;
    }

    public bool DestroyContainingNode(T item)
    {
        ListNode current = Head;
        ListNode previous = current;

        while (current != null)
        {
            if (current.Data.Equals(item))
            {
                previous.Next = current.Next;
                Count--;
                return true;
            }

            previous = current;
            current = current.Next;
        }

        return false;
    }

    /// <summary>
    /// Slow operation. Consider using DoubleLinkedList instead.
    /// </summary>
    public void DestroyNode(ListNode node)
    {
        if (node == Head)
            DestroyHead();
        else if (node == Tail)
            DestroyTail();
        else
        {
            ListNode current = Head;
            while (current != null)
            {
                if (current.Next == node)
                {
                    current.Next = node.Next;
                    Count--;
                    return;
                }

                current = current.Next;
            }
        }
    }

    public void DestroyHead()
    {
        if (Empty)
            return;

        Head = Head.Next;
        Count--;
    }

    /// <summary>
    /// Slow operation. Consider using DoubleLinkedList instead.
    /// </summary>
    public void DestroyTail()
    {
        if (Empty)
            return;

        ListNode current = Tail = Head;
        while (current.Next != null)
        {
            Tail = current;
            current = Tail.Next;
        }

        if (Tail != null)
            Tail.Next = null;

        Count--;
    }

    public void Perform(System.Action<ListNode> action)
    {
        ListNode current = Head;
        while (current != null)
        {
            action(current);
            current = current.Next;
        }
    }

    public void Clear()
    {
        ListNode current = Head;
        while (current != null)
        {
            ListNode next = current.Next;
            current.Next = null;
            current = next;
        }

        Count = 0;
        Head = null;
        Tail = null;
    }

    public T[] ToArray()
    {
        T[] array = new T[Count];

        int i = 0;
        ListNode current = Head;

        while (current != null)
        {
            array[i++] = current.Data;
            current = current.Next;
        }

        return array;
    }

    public SingleLinkedList<T> FromArray(T[] array)
    {
        SingleLinkedList<T> list = new SingleLinkedList<T>();

        for (int i = array.Length - 1; i >= 0; i++)
            list.InsertFront(array[i]);

        return list;
    }

    public override string ToString()
    {
        if (Empty)
            return "List is empty";

        string list = "";

        ListNode current = Head;
        while (current != null)
        {
            list += "[" + current.Data.ToString() + "] ";
            current = current.Next;
        }

        return list;
    }
}
