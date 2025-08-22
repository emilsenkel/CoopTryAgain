using UnityEngine;
using System.Collections.Generic;

public class LaundryBasket : MonoBehaviour
{
    public int capacity = 20;
    [SerializeField] private Transform holdPoint; // Drag the "BasketHold" child GameObject here in Inspector

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
        if (containedLaundry.Count >= capacity) return false;
        containedLaundry.Add(item);
        item.transform.SetParent(holdPoint);
        item.transform.localPosition = Vector3.up * containedLaundry.Count * 0.2f; // Stack visually
        item.transform.localRotation = Quaternion.identity;
        return true;
    }

    public GameObject RemoveItem()
    {
        if (containedLaundry.Count == 0) return null;
        int lastIndex = containedLaundry.Count - 1;
        GameObject item = containedLaundry[lastIndex];
        containedLaundry.RemoveAt(lastIndex);
        item.transform.SetParent(null); // Detach
        return item;
    }

    public int GetCurrentCount()
    {
        return containedLaundry.Count;
    }
}
