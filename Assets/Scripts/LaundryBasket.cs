using UnityEngine;
using System.Collections.Generic;

public class LaundryBasket : MonoBehaviour
{
    public int capacity = 20;
    [SerializeField] private Transform holdPoint; // Drag the "BasketHold" child GameObject here in Inspector

    // Note: overflow into basket is allowed visually, but there are no random drops on overload.
    private List<GameObject> containedLaundry = new List<GameObject>();

    void Awake()
    {
        if (holdPoint == null)
        {
            Debug.LogError("HoldPoint transform not assigned in LaundryBasket!");
        }
    }

    public bool AddItem(GameObject item)
    {
        // Add item to basket, parent it to the basket's holdPoint, and stack visually.
        containedLaundry.Add(item);
        if (holdPoint != null)
        {
            item.transform.SetParent(holdPoint);
            item.transform.localPosition = Vector3.up * containedLaundry.Count * 0.2f; // Stack visually
            item.transform.localRotation = Quaternion.identity;
        }
        else
        {
            // Fallback: parent to basket transform
            item.transform.SetParent(transform);
        }

    // Mark as in-basket if LaundyItem component exists
    var li = item.GetComponent<LaundryItem>();
    if (li != null) li.SetState(LaundryItem.ItemState.InBasket);

        // Always report true for normal behavior — basket accepts items (visually it can overflow)
        return true;
    }

    // Helper to check whether this basket already contains the item
    public bool ContainsItem(GameObject item)
    {
        return containedLaundry.Contains(item);
    }

    public GameObject RemoveItem()
    {
        if (containedLaundry.Count == 0) return null;
        int lastIndex = containedLaundry.Count - 1;
        GameObject item = containedLaundry[lastIndex];
        containedLaundry.RemoveAt(lastIndex);
        item.transform.SetParent(null); // Detach
    var li = item.GetComponent<LaundryItem>();
    if (li != null) li.SetState(LaundryItem.ItemState.OnFloor);
        return item;
    }

    public int GetCurrentCount()
    {
        return containedLaundry.Count;
    }
}
