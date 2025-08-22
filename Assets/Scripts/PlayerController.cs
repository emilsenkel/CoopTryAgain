using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float gravity = -9.8f; // Kept for basic grounding, but no jump

    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Transform handHold; // Drag the "HandHold" child GameObject here in Inspector

    private CharacterController controller;
    private Vector2 moveInput;

    // Laundry and basket fields
    private int handsCapacity = 5;
    private List<GameObject> heldLaundry = new List<GameObject>();
    private LaundryBasket equippedBasket = null;

    [SerializeField] private float pickupRadius = 2f; // Big radius for easy collection around player (tweak in Inspector)

    // Awake is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (handHold == null)
        {
            Debug.LogError("HandHold transform not assigned in PlayerController!");
        }
    }

    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void Take(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        Debug.Log("Take button pressed!"); // Feedback to confirm press

        // Wide area pickup: Use a big sphere around the player to detect and grab all nearby laundry/baskets (forgiving vacuum mode)
        Vector3 pickupCenter = transform.position + Vector3.up * 0.5f + transform.forward * 0.5f; // Slightly up and forward from feet
        Collider[] hits = Physics.OverlapSphere(pickupCenter, pickupRadius); // Get ALL colliders in the sphere

        // Visualize the pickup area (big green sphere in Scene view)
        DrawWireSphere(pickupCenter, pickupRadius, Color.green, 2f);

        bool pickedSomething = false;
        foreach (Collider colliderHit in hits)
        {
            // Check for LaundryItem
            LaundryItem laundry = colliderHit.GetComponent<LaundryItem>();
            if (laundry != null)
            {
                PickUpLaundry(laundry);
                pickedSomething = true;
                continue; // Keep checking for more
            }

            // Check for LaundryBasket (only if not equipped)
            LaundryBasket basket = colliderHit.GetComponent<LaundryBasket>();
            if (basket != null && equippedBasket == null)
            {
                EquipBasket(basket);
                pickedSomething = true;
                // Don't break—could pick laundry too, but basket is single
            }
        }

        if (pickedSomething)
        {
            Debug.Log("Picked up items in area!");
        }
        else
        {
            Debug.Log("No items in pickup area!");
        }

        // Only drop if nothing was picked up (no target in area)
        if (!pickedSomething)
        {
            if (equippedBasket != null)
            {
                DropBasket(); // Drops the entire basket with laundry inside
            }
            else if (heldLaundry.Count > 0)
            {
                DropHeldLaundry(); // Drops all held laundry on the floor
            }
        }
    }

    private void PickUpLaundry(LaundryItem laundry)
    {
        GameObject item = laundry.gameObject;
        bool added = false;

        if (equippedBasket != null)
        {
            added = equippedBasket.AddItem(item);
        }
        else
        {
            if (heldLaundry.Count < handsCapacity)
            {
                heldLaundry.Add(item);
                item.transform.SetParent(handHold);
                item.transform.localPosition = Vector3.up * heldLaundry.Count * 0.2f; // Stack visually
                item.transform.localRotation = Quaternion.identity;
                added = true;
            }
        }

        if (added)
        {
            Debug.Log($"Picked up laundry. Hands: {heldLaundry.Count}, Basket: {(equippedBasket ? equippedBasket.GetCurrentCount() : 0)}");
        }
        else
        {
            Debug.Log("No space to pick up laundry!");
        }
    }

    private void EquipBasket(LaundryBasket basket)
    {
        equippedBasket = basket;
        basket.transform.SetParent(transform);
        basket.transform.localPosition = new Vector3(0, 1f, 1f); // In front of player
        basket.transform.localRotation = Quaternion.identity;
        Debug.Log("Equipped basket!");
    }

    private void DropBasket()
    {
        if (equippedBasket == null) return;

        equippedBasket.transform.SetParent(null);
        equippedBasket.transform.position = transform.position + transform.forward * 1f; // Place in front
        equippedBasket = null;
        Debug.Log("Dropped basket!");
    }

    private void TransferLaundry()
    {
        const int transferAmount = 5;

        // Prioritize putting from hands to basket
        if (heldLaundry.Count > 0)
        {
            int toTransfer = Mathf.Min(transferAmount, heldLaundry.Count, equippedBasket.capacity - equippedBasket.GetCurrentCount());
            for (int i = 0; i < toTransfer; i++)
            {
                int lastIndex = heldLaundry.Count - 1;
                GameObject item = heldLaundry[lastIndex];
                heldLaundry.RemoveAt(lastIndex);
                equippedBasket.AddItem(item);
            }
            if (toTransfer > 0)
            {
                Debug.Log($"Transferred {toTransfer} to basket. Hands: {heldLaundry.Count}, Basket: {equippedBasket.GetCurrentCount()}");
            }
        }
        // Else take from basket to hands
        else if (heldLaundry.Count < handsCapacity)
        {
            int toTransfer = Mathf.Min(transferAmount, handsCapacity - heldLaundry.Count, equippedBasket.GetCurrentCount());
            for (int i = 0; i < toTransfer; i++)
            {
                GameObject item = equippedBasket.RemoveItem();
                if (item == null) break;
                heldLaundry.Add(item);
                item.transform.SetParent(handHold);
                item.transform.localPosition = Vector3.up * heldLaundry.Count * 0.2f;
                item.transform.localRotation = Quaternion.identity;
            }
            if (toTransfer > 0)
            {
                Debug.Log($"Transferred {toTransfer} from basket. Hands: {heldLaundry.Count}, Basket: {equippedBasket.GetCurrentCount()}");
            }
        }
    }

    private void DropHeldLaundry()
    {
        while (heldLaundry.Count > 0)
        {
            int lastIndex = heldLaundry.Count - 1;
            GameObject item = heldLaundry[lastIndex];
            heldLaundry.RemoveAt(lastIndex);
            item.transform.SetParent(null);
            // Drop in front with a slight random scatter for chaos
            Vector3 dropPosition = transform.position + transform.forward * 1f + Random.insideUnitSphere * 0.5f;
            item.transform.position = new Vector3(dropPosition.x, 0f, dropPosition.z); // Snap to floor y=0, adjust if your floor is different
            item.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f); // Random rotation for fun
        }
        Debug.Log("Dropped all held laundry!");
    }

    private void DrawWireSphere(Vector3 center, float radius, Color color, float duration, int quality = 3)
    {
        quality = Mathf.Clamp(quality, 1, 10);

        int segments = quality << 2;
        int subdivisions = quality << 3;
        int halfSegments = segments >> 1;
        float strideAngle = 360F / subdivisions;
        float segmentStride = 180F / segments;

        Vector3 first;
        Vector3 next;
        for (int i = 0; i < segments; i++)
        {
            first = (Vector3.forward * radius);
            first = Quaternion.AngleAxis(segmentStride * (i - halfSegments), Vector3.right) * first;

            for (int j = 0; j < subdivisions; j++)
            {
                next = Quaternion.AngleAxis(strideAngle, Vector3.up) * first;
                Debug.DrawLine(first + center, next + center, color, duration);
                first = next;
            }
        }

        Vector3 axis;
        for (int i = 0; i < segments; i++)
        {
            first = (Vector3.forward * radius);
            first = Quaternion.AngleAxis(segmentStride * (i - halfSegments), Vector3.up) * first;
            axis = Quaternion.AngleAxis(90F, Vector3.up) * first;

            for (int j = 0; j < subdivisions; j++)
            {
                next = Quaternion.AngleAxis(strideAngle, axis) * first;
                Debug.DrawLine(first + center, next + center, color, duration);
                first = next;
            }
        }
    }

    public void SetLabel(string label)
    {
        this.label.text = label;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);
        controller.Move(move * speed * Time.deltaTime);

        // Face movement direction like Overcooked/PlateUp
        if (move != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(move);
        }

        if (!controller.isGrounded)
        {
            controller.Move(Vector3.down * -gravity * Time.deltaTime); // Simple downward force if in air (e.g., for ramps)
        }
    }
}
