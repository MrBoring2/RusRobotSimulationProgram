using UnityEngine;

public class HierarchyItem 
{
    public int Id { get; private set;}
    public string DisplayName => Reference.name;
    public GameObject Reference { get; private set; }
    public HierarchyItem ChildItem { get; private set; }

    public HierarchyItem(int id, GameObject reference, HierarchyItem childItem = null)
    {
        Id = id;
        Reference = reference;
        ChildItem = childItem;
    }
}
