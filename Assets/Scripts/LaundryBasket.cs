using UnityEngine;

public class LaundryBasket : MonoBehaviour
{
    public int capacity = 20;
    public int currentAmount = 0;

    public bool AddLaundry(int amountToAdd)
    {
        if (currentAmount + amountToAdd > capacity) return false;
        currentAmount += amountToAdd;
        return true;
    }

    public bool RemoveLaundry(int amountToRemove)
    {
        if (currentAmount < amountToRemove) return false;
        currentAmount -= amountToRemove;
        return true;
    }
}
