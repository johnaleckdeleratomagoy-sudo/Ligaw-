using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CraftingManager : MonoBehaviour
{
    [Header("Controls")]
    [Tooltip("Select the key used to open and close the crafting panel.")]
    [SerializeField] private KeyCode toggleKey = KeyCode.B;

    [Header("Player Movement Reference")]
    [Tooltip("Drag your Player GameObject here, or leave empty to auto-find by 'Player' tag.")]
    [SerializeField] private GameObject playerObject;

    private PlayerMovement playerMovement;

    [Header("UI Panel Reference")]
    [SerializeField] private GameObject craftingPanel;

    [Header("Recipes & Grid Settings")]
    [SerializeField] private List<Recipe> recipes;
    [SerializeField] private Transform gridContainer;
    [SerializeField] private GameObject recipeButtonPrefab;

    [Header("Details Panel UI")]
    [SerializeField] private TextMeshProUGUI recipeTitleText;
    [SerializeField] private Image recipeIconImage;
    [SerializeField] private TextMeshProUGUI ingredientsListText;
    [SerializeField] private Button craftButton;

    private Recipe selectedRecipe;
    private bool cachedHoldingAxeState;

    private void Start()
    {
        if (craftingPanel != null)
            craftingPanel.SetActive(false);

        if (craftButton != null)
            craftButton.onClick.AddListener(CraftSelectedItem);

        // Auto-find player if not manually assigned in Inspector
        if (playerObject == null)
            playerObject = GameObject.FindWithTag("Player");

        if (playerObject != null)
        {
            playerMovement = playerObject.GetComponent<PlayerMovement>();
        }

        PopulateGrid();
    }

    private void Update()
    {
        if (GameInput.Pressed("Crafting", toggleKey))
        {
            ToggleCraftingPanel();
        }
    }

    public void ToggleCraftingPanel()
    {
        bool isActive = !craftingPanel.activeSelf;
        craftingPanel.SetActive(isActive);

        // 1. Toggle Cursor Visibility and Lock State
        Cursor.lockState = isActive ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isActive;

        // 2. Restrict attack and camera look while keeping movement active
        SetPlayerActionState(isActive);

        if (isActive)
        {
            PopulateGrid();
            if (selectedRecipe != null)
                DisplayRecipeDetails(selectedRecipe);
        }
    }

    /// <summary>
    /// Restricts/Restores camera rotation and attacks without disabling CharacterController or movement.
    /// </summary>
    private void SetPlayerActionState(bool menuOpen)
    {
        if (playerMovement == null) return;

        if (menuOpen)
        {
            // Lock mouse look so moving the cursor on UI doesn't spin the camera
            playerMovement.canRotateMouse = false;

            // Cache current axe state and temporarily disable axe actions/chopping
            cachedHoldingAxeState = playerMovement.isHoldingAxe;
            playerMovement.isHoldingAxe = false;
        }
        else
        {
            // Restore mouse look and previous axe holding state
            playerMovement.canRotateMouse = true;
            playerMovement.isHoldingAxe = cachedHoldingAxeState;
        }
    }

    private void PopulateGrid()
    {
        if (gridContainer == null || recipeButtonPrefab == null) return;

        foreach (Transform child in gridContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (Recipe recipe in recipes)
        {
            if (recipe == null || recipe.result == null) continue;

            GameObject buttonObj = Instantiate(recipeButtonPrefab, gridContainer);

            CraftingSlotUI slotUI = buttonObj.GetComponent<CraftingSlotUI>();
            if (slotUI != null)
            {
                slotUI.Setup(recipe, this);
            }
            else
            {
                TextMeshProUGUI btnText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = recipe.result.itemName;

                Recipe currentRecipe = recipe;
                Button btn = buttonObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() => DisplayRecipeDetails(currentRecipe));
                }
            }
        }
    }

    public void DisplayRecipeDetails(Recipe recipe)
    {
        selectedRecipe = recipe;
        if (recipe == null || recipe.result == null) return;

        if (recipeTitleText != null)
            recipeTitleText.text = $"{recipe.result.itemName} (x{recipe.resultAmount})";

        if (recipeIconImage != null)
        {
            if (recipe.result.itemIcon != null)
            {
                recipeIconImage.sprite = recipe.result.itemIcon;
                recipeIconImage.enabled = true;
            }
            else
            {
                recipeIconImage.enabled = false;
            }
        }

        if (ingredientsListText != null)
        {
            ingredientsListText.text = "<b>Required Items:</b>\n\n";

            if (recipe.ingredients != null)
            {
                foreach (Ingredients ingredient in recipe.ingredients)
                {
                    if (ingredient.item == null) continue;

                    int owned = PlayerInventory.Instance != null ? PlayerInventory.Instance.GetItemCount(ingredient.item) : 0;
                    string colorHex = owned >= ingredient.amount ? "<color=#00FF00>" : "<color=#FF0000>";

                    ingredientsListText.text += $"� {ingredient.item.itemName}: {colorHex}{owned}/{ingredient.amount}</color>\n";
                }
            }
        }

        if (craftButton != null)
        {
            bool canCraft = PlayerInventory.Instance != null && PlayerInventory.Instance.HasIngredients(recipe.ingredients);
            craftButton.interactable = canCraft;
        }
    }

    private void CraftSelectedItem()
    {
        Debug.Log("[CraftingManager] Craft button pressed.");

        if (selectedRecipe == null)
        {
            Debug.LogWarning("[CraftingManager] No recipe is currently selected.");
            return;
        }

        if (PlayerInventory.Instance == null)
        {
            Debug.LogError("[CraftingManager] PlayerInventory.Instance is NULL.");
            return;
        }

        if (selectedRecipe.result == null)
        {
            Debug.LogError("[CraftingManager] Selected recipe has no Result ItemDataSO.");
            return;
        }

        Debug.Log("[CraftingManager] Attempting to craft: "
            + selectedRecipe.result.itemName
            + " x"
            + selectedRecipe.resultAmount);

        Debug.Log("[CraftingManager] Result Item ID: "
            + selectedRecipe.result.itemID);

        if (selectedRecipe.result.physicalPrefab != null)
        {
            Debug.Log("[CraftingManager] Result Physical Prefab: "
                + selectedRecipe.result.physicalPrefab.name);
        }
        else
        {
            Debug.LogWarning("[CraftingManager] Result Physical Prefab is NULL.");
        }

        if (selectedRecipe.result.pickupInHand != null)
        {
            Debug.Log("[CraftingManager] Result Pickup In Hand: "
                + selectedRecipe.result.pickupInHand.name);
        }
        else
        {
            Debug.LogWarning("[CraftingManager] Result Pickup In Hand is NULL.");
        }

        if (selectedRecipe.ingredients == null ||
            selectedRecipe.ingredients.Count == 0)
        {
            Debug.LogError("[CraftingManager] Recipe has no ingredients assigned.");
            return;
        }

        // Check every ingredient
        foreach (Ingredients ingredient in selectedRecipe.ingredients)
        {
            if (ingredient == null)
            {
                Debug.LogError("[CraftingManager] An ingredient entry is NULL.");
                return;
            }

            if (ingredient.item == null)
            {
                Debug.LogError("[CraftingManager] An ingredient has no ItemDataSO assigned.");
                return;
            }

            int owned = PlayerInventory.Instance.GetItemCount(ingredient.item);

            Debug.Log(
                "[CraftingManager] Ingredient: "
                + ingredient.item.itemName
                + " | Required: "
                + ingredient.amount
                + " | Owned: "
                + owned
                + " | Item ID: "
                + ingredient.item.itemID
            );

            if (owned < ingredient.amount)
            {
                Debug.LogWarning(
                    "[CraftingManager] Cannot craft. Not enough "
                    + ingredient.item.itemName
                    + ". Required: "
                    + ingredient.amount
                    + ", Owned: "
                    + owned
                );

                DisplayRecipeDetails(selectedRecipe);
                return;
            }
        }

        Debug.Log("[CraftingManager] All ingredients are available.");

        // Consume ingredients
        Debug.Log("[CraftingManager] Consuming ingredients...");

        PlayerInventory.Instance.ConsumeIngredients(
            selectedRecipe.ingredients
        );

        Debug.Log("[CraftingManager] Ingredients consumed.");

        // Add crafted item to inventory/hotbar
        Debug.Log(
            "[CraftingManager] Adding crafted item to inventory: "
            + selectedRecipe.result.itemName
        );

        PlayerInventory.Instance.DirectPickUp(
            null,
            selectedRecipe.result,
            selectedRecipe.resultAmount
        );

        Debug.Log(
            "[CraftingManager] Successfully crafted "
            + selectedRecipe.resultAmount
            + "x "
            + selectedRecipe.result.itemName
            + "!"
        );

        // The crafted item may have landed in the selected slot
        if (PlayerInventory.Instance.hotbarUI != null)
            PlayerInventory.Instance.hotbarUI.UpdateHandVisual();

        // Refresh recipe information
        DisplayRecipeDetails(selectedRecipe);
    }
}