using UnityEngine;

public class HierarchyItem 
{
    public string DisplayName => Reference.name;
    public GameObject Reference { get; private set; }
    public HierarchyItem ChildItem { get; private set; }

    public HierarchyItem(GameObject reference, HierarchyItem childItem = null)
    {
        Reference = reference;
        ChildItem = childItem;
    }
}
