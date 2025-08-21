using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float gravity = -9.8f; // Kept for basic grounding, but no jump

    [SerializeField] private TextMeshProUGUI label;

    private CharacterController controller;
    private Vector2 moveInput;

    // Laundry and basket fields
    private int handsCapacity = 5;
    private int handsLaundry = 0;
    private LaundryBasket equippedBasket = null;
    private float interactRange = 2f; // Distance to detect items

    // Awake is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void Take(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        // Raycast forward from roughly waist height to detect interactables
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(rayOrigin, transform.forward, out RaycastHit hit, interactRange))
        {
            if (hit.collider.CompareTag("Laundry"))
            {
                LaundryItem laundry = hit.collider.GetComponent<LaundryItem>();
                if (laundry != null)
                {
                    PickUpLaundry(laundry);
                }
                return; // Picked, so exit
            }
            else if (hit.collider.CompareTag("Basket") && equippedBasket == null)
            {
                LaundryBasket basket = hit.collider.GetComponent<LaundryBasket>();
                if (basket != null)
                {
                    EquipBasket(basket);
                }
                return; // Equipped, so exit
            }
        }

        // No target hit: Do transfer if has basket
        if (equippedBasket != null)
        {
            TransferLaundry();
        }
    }

    private void PickUpLaundry(LaundryItem laundry)
    {
        int amount = laundry.amount;
        bool added = false;

        if (equippedBasket != null)
        {
            added = equippedBasket.AddLaundry(amount);
        }
        else
        {
            if (handsLaundry + amount <= handsCapacity)
            {
                handsLaundry += amount;
                added = true;
            }
        }

        if (added)
        {
            Destroy(laundry.gameObject);
            Debug.Log($"Picked up {amount} laundry. Hands: {handsLaundry}, Basket: {(equippedBasket ? equippedBasket.currentAmount : 0)}");
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

    private void TransferLaundry()
    {
        const int transferAmount = 5;

        // Prioritize putting from hands to basket
        if (handsLaundry > 0)
        {
            int toTransfer = Mathf.Min(transferAmount, handsLaundry, equippedBasket.capacity - equippedBasket.currentAmount);
            if (toTransfer > 0)
            {
                handsLaundry -= toTransfer;
                equippedBasket.AddLaundry(toTransfer);
                Debug.Log($"Transferred {toTransfer} to basket. Hands: {handsLaundry}, Basket: {equippedBasket.currentAmount}");
            }
        }
        // Else take from basket to hands
        else if (handsLaundry < handsCapacity)
        {
            int toTransfer = Mathf.Min(transferAmount, handsCapacity - handsLaundry, equippedBasket.currentAmount);
            if (toTransfer > 0)
            {
                equippedBasket.RemoveLaundry(toTransfer);
                handsLaundry += toTransfer;
                Debug.Log($"Transferred {toTransfer} from basket. Hands: {handsLaundry}, Basket: {equippedBasket.currentAmount}");
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

        if (!controller.isGrounded)
        {
            controller.Move(Vector3.down * -gravity * Time.deltaTime); // Simple downward force if in air (e.g., for ramps)
        }
    }
}
