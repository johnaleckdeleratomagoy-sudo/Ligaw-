using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Ingredients
{
    public ItemDataSO item; // Your Item Data ScriptableObject
    public int amount;
    public bool requiresWorkbench = false;
}

[CreateAssetMenu(fileName = "New Recipe", menuName = "Crafting/Recipe")]
public class Recipe : ScriptableObject
{
    public List<Ingredients> ingredients;
    public ItemDataSO result;
    public int resultAmount = 1;
}