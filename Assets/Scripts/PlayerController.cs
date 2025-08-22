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
    // Hand carry slowdown
    [Header("Hand carry slowdown")]
    [SerializeField] private int slowdownThreshold = 5; // number of pieces before slowdown starts (0..5 = no slowdown)
    [SerializeField] private float penaltyPerExtra = 0.2f; // 20% reduction per extra piece (multiplicative)
    [SerializeField] private float minMovementSpeed = 1f; // absolute minimum movement speed (units/sec)
    private List<GameObject> heldLaundry = new List<GameObject>();
    private LaundryBasket equippedBasket = null;
    // For full-stack drop/pick toggle
    private GameObject lastDroppedStack = null;
    // Debounce to prevent input spam
    [SerializeField] private float takeCooldown = 0.25f;
    private float lastTakeTime = -10f;

    [Header("Pickup Area (capsule)")]
    [SerializeField] private float pickupRadius = 0.7f; // capsule radius (tweak in Inspector)
    [SerializeField] private float pickupTopOffset = 1.6f; // from transform.position upwards
    [SerializeField] private float pickupBottomOffset = 0.0f; // from transform.position upwards
    [SerializeField] private float pickupForwardOffset = 0.5f; // capsule shifted slightly forward from player
    [SerializeField] private float handPickupMultiplier = 2f; // how much larger the pickup area when holding laundry in hand
    [SerializeField] private float basketExtraMultiplier = 2f; // additional multiplier when holding a basket (applied on top of handPickupMultiplier)
    // (Random drops removed per request)

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

        // Debounce rapid presses
        if (Time.time - lastTakeTime < takeCooldown)
        {
            Debug.Log("Take pressed too fast; ignoring to prevent spam.");
            return;
        }
        lastTakeTime = Time.time;

        Debug.Log("Take button pressed!"); // Feedback to confirm press

    // Wide area pickup: use a capsule from head to feet, slightly in front of the player so items
    // on the floor and near the body are easy to grab.
    Vector3 top = transform.position + Vector3.up * pickupTopOffset + transform.forward * pickupForwardOffset;
    Vector3 bottom = transform.position + Vector3.up * pickupBottomOffset + transform.forward * pickupForwardOffset;
    // Determine effective pickup multiplier:
    // - always apply handPickupMultiplier
    // - if holding a basket, scale further by basketExtraMultiplier
    float multiplier = handPickupMultiplier;
    if (equippedBasket != null)
    {
        multiplier *= basketExtraMultiplier;
    }
    float effectiveRadius = pickupRadius * multiplier;
    Collider[] hits = Physics.OverlapCapsule(top, bottom, effectiveRadius);

    // Visualize the pickup area (capsule) in Scene view
    DrawWireCapsule(top, bottom, effectiveRadius, Color.green, 2f);

    // Note: don't drop an equipped basket immediately if the player is standing near it.
    // We'll scan the hits first and prefer picking up any LaundryItem into the equipped basket.

        // Collect interactable laundry items and baskets within range (use GetComponentInParent and dedupe)
        List<LaundryItem> nearbyLaundry = new List<LaundryItem>();
        List<LaundryBasket> nearbyBaskets = new List<LaundryBasket>();
        var seenLaundry = new HashSet<GameObject>();
        var seenBaskets = new HashSet<GameObject>();
        foreach (Collider c in hits)
        {
            if (c == null) continue;
            var li = c.GetComponentInParent<LaundryItem>();
            if (li != null && li.gameObject != null && !seenLaundry.Contains(li.gameObject))
            {
                // Only consider items that are actually on the floor for pickup.
                if (li.State == LaundryItem.ItemState.OnFloor)
                {
                    nearbyLaundry.Add(li);
                    seenLaundry.Add(li.gameObject);
                }
            }
            var lb = c.GetComponentInParent<LaundryBasket>();
            if (lb != null && lb.gameObject != null && !seenBaskets.Contains(lb.gameObject) && lb != equippedBasket)
            {
                nearbyBaskets.Add(lb);
                seenBaskets.Add(lb.gameObject);
            }
        }

        // Priority 1: laundry pieces
    if (nearbyLaundry.Count > 0)
        {
            foreach (var li in nearbyLaundry)
            {
                PickUpLaundry(li);
            }
            Debug.Log("Picked up laundry items in area!");
            return;
        }

        // Priority 2: baskets (only if no laundry pieces nearby)
        if (nearbyBaskets.Count > 0)
        {
            // choose nearest basket
            LaundryBasket nearest = null;
            float bestDist = float.MaxValue;
            foreach (var b in nearbyBaskets)
            {
                float d = Vector3.Distance(b.transform.position, transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    nearest = b;
                }
            }
            if (nearest != null)
            {
                if (equippedBasket == null)
                {
                    if (heldLaundry.Count == 0)
                    {
                        EquipBasket(nearest);
                        Debug.Log("Picked up basket");
                        return;
                    }
                    else
                    {
                        // Transfer held laundry into the nearby basket (do not equip)
                        TransferHeldToBasket(nearest);
                        Debug.Log("Transferred held laundry into nearby basket");
                        return;
                    }
                }
                else
                {
                    // Already carrying a basket: interacting with another basket does nothing
                    Debug.Log("Already carrying a basket; ignoring nearby basket.");
                    return;
                }
            }
        }

        // Nothing interactable nearby: handle drop/toggle
        Debug.Log("No items or baskets in pickup area.");
        if (equippedBasket != null)
        {
            DropBasket();
            return;
        }
        if (heldLaundry.Count > 0)
        {
            if (lastDroppedStack != null)
            {
                float dist = Vector3.Distance(lastDroppedStack.transform.position, transform.position);
                if (dist <= effectiveRadius)
                {
                    PickupFullStackFromLastDropped();
                    return;
                }
            }
            // Drop at player's position so it's easy to pick back up
            DropHeldLaundryToStack();
            return;
        }
    }

    private void PickUpLaundry(LaundryItem laundry)
    {
        GameObject item = laundry.gameObject;
        bool added = false;

        // Only pick up items that are on the floor
        LaundryItem li = item.GetComponent<LaundryItem>();
        if (li != null)
        {
            if (li.State != LaundryItem.ItemState.OnFloor)
            {
                Debug.Log("Item not on floor, skipping pickup.");
                return;
            }
        }

        if (equippedBasket != null)
        {
            added = equippedBasket.AddItem(item);
            // If we successfully added to basket, ensure item is not parented to hand
            if (added && item.transform.IsChildOf(handHold))
            {
                item.transform.SetParent(equippedBasket.transform); // basket will reparent to its holdPoint inside AddItem
            }
            // set state
            if (li != null) li.SetState(LaundryItem.ItemState.InBasket);
        }
        else
        {
            // Allow unlimited holding, but apply visual parenting and potential overload behavior
            heldLaundry.Add(item);
            item.transform.SetParent(handHold);
            item.transform.localPosition = Vector3.up * heldLaundry.Count * 0.2f; // Stack visually
            item.transform.localRotation = Quaternion.identity;
            added = true;
            if (li != null) li.SetState(LaundryItem.ItemState.InHand);
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
        // If we already have a basket, drop it first
        if (equippedBasket != null)
        {
            DropBasket();
        }

        equippedBasket = basket;
        basket.transform.SetParent(transform);
        basket.transform.localPosition = new Vector3(0, 1f, 1f); // In front of player
        basket.transform.localRotation = Quaternion.identity;

        // Try to transfer any held laundry into the basket immediately (respecting capacity)
        if (heldLaundry.Count > 0)
        {
            // Copy to avoid modification during iteration
            var copy = new List<GameObject>(heldLaundry);
            foreach (var item in copy)
            {
                if (equippedBasket.GetCurrentCount() >= equippedBasket.capacity) break;
                // Remove from hands and add to basket
                heldLaundry.Remove(item);
                bool added = equippedBasket.AddItem(item);
                if (!added)
                {
                    // If add failed, put back into hands
                    heldLaundry.Add(item);
                var li = item.GetComponent<LaundryItem>();
                if (li != null && added) li.SetState(LaundryItem.ItemState.InBasket);
                }
            }
        }

        Debug.Log("Equipped basket and transferred held laundry if space available.");
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
        else if (heldLaundry.Count < slowdownThreshold)
        {
            int toTransfer = Mathf.Min(transferAmount, slowdownThreshold - heldLaundry.Count, equippedBasket.GetCurrentCount());
            for (int i = 0; i < toTransfer; i++)
            {
                GameObject item = equippedBasket.RemoveItem();
                if (item == null) break;
                heldLaundry.Add(item);
                item.transform.SetParent(handHold);
                item.transform.localPosition = Vector3.up * heldLaundry.Count * 0.2f;
                item.transform.localRotation = Quaternion.identity;
                var li = item.GetComponent<LaundryItem>();
                if (li != null) li.SetState(LaundryItem.ItemState.InHand);
            }
            if (toTransfer > 0)
            {
                Debug.Log($"Transferred {toTransfer} from basket. Hands: {heldLaundry.Count}, Basket: {equippedBasket.GetCurrentCount()}");
            }
        }
    }

    private void DropHeldLaundryToStack()
    {
        // Create a parent GameObject to represent the dropped stack
        GameObject stack = new GameObject("DroppedLaundryStack");
        stack.transform.position = transform.position + transform.forward * 1f;

        // Move all held laundry under the stack
        while (heldLaundry.Count > 0)
        {
            int lastIndex = heldLaundry.Count - 1;
            GameObject item = heldLaundry[lastIndex];
            heldLaundry.RemoveAt(lastIndex);
            item.transform.SetParent(stack.transform);
            // Scatter slightly around the stack center
            Vector3 dropPosition = stack.transform.position + Random.insideUnitSphere * 0.5f;
            item.transform.position = new Vector3(dropPosition.x, 0f, dropPosition.z);
            item.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var li = item.GetComponent<LaundryItem>();
            if (li != null) li.SetState(LaundryItem.ItemState.OnFloor);
        }

        lastDroppedStack = stack;
        Debug.Log("Dropped full held laundry as a stack.");
    }

    private void PickupFullStackFromLastDropped()
    {
        if (lastDroppedStack == null) return;
        // Collect all children back into hands
        var items = new List<GameObject>();
        for (int i = 0; i < lastDroppedStack.transform.childCount; i++)
        {
            items.Add(lastDroppedStack.transform.GetChild(i).gameObject);
        }

        foreach (var item in items)
        {
            item.transform.SetParent(handHold);
            heldLaundry.Add(item);
            item.transform.localPosition = Vector3.up * heldLaundry.Count * 0.2f;
            item.transform.localRotation = Quaternion.identity;
            var li = item.GetComponent<LaundryItem>();
            if (li != null) li.SetState(LaundryItem.ItemState.InHand);
        }

        Destroy(lastDroppedStack);
        lastDroppedStack = null;
        Debug.Log("Picked up full dropped laundry stack back into hands.");
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

    

    private void TransferHeldToBasket(LaundryBasket basket)
    {
        if (basket == null) return;

        // Move everything from hands into the provided basket up to its capacity
        var copy = new List<GameObject>(heldLaundry);
        foreach (var item in copy)
        {
            // Attempt to add; if AddItem returns true, remove from hands
            bool added = basket.AddItem(item);
            if (added)
            {
                heldLaundry.Remove(item);
                var li = item.GetComponent<LaundryItem>();
                if (li != null) li.SetState(LaundryItem.ItemState.InBasket);
            }
            else
            {
                // If basket can't accept more (shouldn't happen since AddItem always true), stop
                break;
            }
        }
        Debug.Log($"Transferred held laundry into nearby basket. Remaining hands: {heldLaundry.Count}, Basket: {basket.GetCurrentCount()}");
    }


    private float GetLoadSpeedMultiplier()
    {
        // If holding a basket, no hand slowdown applies
        if (equippedBasket != null) return 1f;

        int count = heldLaundry.Count;
        if (count <= slowdownThreshold) return 1f;

        int extra = count - slowdownThreshold; // pieces beyond threshold
        // multiplicative penalty: each extra multiplies speed by (1 - penaltyPerExtra)
        float mul = Mathf.Pow(1f - penaltyPerExtra, extra);

        // Enforce absolute minimum movement speed (units/sec)
        float currentSpeed = speed * mul;
        if (currentSpeed < minMovementSpeed)
        {
            mul = minMovementSpeed / speed;
        }

        return mul;
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

    // Draw a wireframe capsule between two points for debug visualization
    private void DrawWireCapsule(Vector3 start, Vector3 end, float radius, Color color, float duration)
    {
        // Approximate by drawing several circles along the capsule axis
        int steps = 6;
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector3 center = Vector3.Lerp(start, end, t);
            DrawWireSphere(center, radius, color, duration, 3);
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
    float loadMul = GetLoadSpeedMultiplier();
    controller.Move(move * speed * loadMul * Time.deltaTime);

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