using System.Collections.Generic;
using UnityEngine;

public class HierarchyItem 
{
    public int Id { get; private set;}
    public string DisplayName => Reference.name;
    public GameObject Reference { get; private set; }
    public List<HierarchyItem> Children { get; } = new();

    public HierarchyItem(int id, GameObject reference)
    {
        Id = id;
        Reference = reference;
    }
    public void AddChild(HierarchyItem child)
    {
        Children.Add(child);
    }
}
