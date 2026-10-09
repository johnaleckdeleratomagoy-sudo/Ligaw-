using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button button;

    public void Setup(Recipe recipe, CraftingManager craftingManager)
    {
        if (recipe == null || recipe.result == null) return;

        // Set Recipe Name Text
        if (nameText != null)
            nameText.text = recipe.result.itemName;

        // Display Item Icon if assigned, otherwise hide the image component
        if (iconImage != null)
        {
            if (recipe.result.itemIcon != null)
            {
                iconImage.sprite = recipe.result.itemIcon;
                iconImage.color = Color.white;
                iconImage.enabled = true; // Show sprite
            }
            else
            {
                iconImage.enabled = false; // Hide white square if no icon exists
            }
        }

        // Hook up click listener
        if (button != null && craftingManager != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => craftingManager.DisplayRecipeDetails(recipe));
        }
    }
}