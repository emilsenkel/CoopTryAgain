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
    private float interactRange = 5f; // Distance to detect items

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

        // Adjusted raycast: Lower origin to knee height and add slight downward angle for floor items
        Vector3 rayOrigin = transform.position + Vector3.up * 0.3f;
        Vector3 rayDirection = transform.forward + Vector3.down * 0.2f; // Slight down tilt
        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, interactRange))
        {
            Debug.Log("Raycast hit something! Name: " + hit.collider.name); // Feedback on hit

            // Check for LaundryItem component
            LaundryItem laundry = hit.collider.GetComponent<LaundryItem>();
            if (laundry != null)
            {
                PickUpLaundry(laundry);
                return; // Picked, so exit
            }

            // Check for LaundryBasket component
            LaundryBasket basket = hit.collider.GetComponent<LaundryBasket>();
            if (basket != null && equippedBasket == null)
            {
                EquipBasket(basket);
                return; // Equipped, so exit
            }
        } else
        {
            Debug.Log("Raycast missed—no hit!"); // Feedback on miss
        }

        // No target hit: Do transfer if has basket
        if (equippedBasket != null)
        {
            if (heldLaundry.Count > 0 || equippedBasket.GetCurrentCount() > 0)
            {
                TransferLaundry(); // Transfer if there's laundry
            }
            else
            {
                DropBasket(); // Drop if no laundry (empty hands and basket)
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
