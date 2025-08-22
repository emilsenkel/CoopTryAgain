using UnityEngine;

public class LaundryItem : MonoBehaviour
{
    public enum ItemState
    {
        OnFloor,
        InHand,
        InBasket
    }

    [SerializeField] private ItemState state = ItemState.OnFloor;

    public ItemState State => state;

    public void SetState(ItemState newState)
    {
        state = newState;
    }
}
