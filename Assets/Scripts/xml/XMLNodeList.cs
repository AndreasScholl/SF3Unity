using UnityEngine;
using System.Collections;

public class XMLNodeList : ArrayList
{
    public XMLNode Pop()
    {
        XMLNode node = this[Count - 1] as XMLNode;
        this.RemoveAt(Count - 1);
        return node;
    }

    public void Push(XMLNode node)
    {
        Add(node);
    }
}
