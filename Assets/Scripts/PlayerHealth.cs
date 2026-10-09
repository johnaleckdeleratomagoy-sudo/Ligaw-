    using UnityEngine;
    using UnityEngine.UI; // Required to use standard Image components

    public class PlayerHealth : MonoBehaviour
    {
        public int maxHealth = 100;
        public int currentHealth;

        // Target the actual red UI Image component directly
        public Image redHealthBarImage;

        [Header("Death Dependencies")]
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerMovement playerMovement;

        private bool isDead = false;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;

        // Public getter to allow other scripts (like SurvivalUI) to check if player is dead
        public bool IsDead => isDead;

        void Start()
        {
            currentHealth = maxHealth;
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            Time.timeScale = 1f;
            GameOverPanel.HideScenePanel();

            // Auto-fetch references if they aren't assigned in Inspector
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (playerMovement == null)
                playerMovement = GetComponent<PlayerMovement>();

            UpdateHealthUI();
        }

        public void TakeDamage(int damage)
        {
            if (isDead) return; // Prevent taking damage after dying

            currentHealth -= damage;

            if (currentHealth < 0) currentHealth = 0;

            Debug.Log("Player Health: " + currentHealth);

            UpdateHealthUI();

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            Debug.Log("Player has died!");

            // 1. Disable player movement & camera control
            if (playerMovement != null)
            {
                playerMovement.canRotateMouse = false;
                playerMovement.enabled = false;
            }

            // Show the player from outside, otherwise the body is hidden in first person
            var cameraSwitch = FindFirstObjectByType<CameraSwitch>();
            if (cameraSwitch != null) cameraSwitch.ForceThirdPerson();

            // 2. Play Death Animation
            if (animator != null)
            {
                animator.SetBool("IsDead", true);
                // Play the state directly (no trigger: a leftover trigger would restart it)
                animator.ResetTrigger("Die");
                animator.CrossFadeInFixedTime("Die", 0.1f);
            }
            else
            {
                Debug.LogWarning("PlayerHealth: no Animator found, death animation can't play.");
            }

            // 3. Unlock and show cursor (for Game Over UI)
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 4. Show Game Over panel (added at runtime if not already on the player)
            var gameOver = GetComponent<GameOverPanel>();
            if (gameOver == null) gameOver = gameObject.AddComponent<GameOverPanel>();
            gameOver.Show(animator);
        }

        /// <summary>Brings the player back to life at the spawn point with full health and stats.</summary>
        public void Revive()
        {
            isDead = false;
            currentHealth = maxHealth;

            // Back to the spawn point (CharacterController must be off to teleport)
            var controller = GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            if (controller != null) controller.enabled = true;

            if (playerMovement != null)
            {
                playerMovement.enabled = true;
                playerMovement.canRotateMouse = true;
            }

            if (animator != null)
            {
                animator.ResetTrigger("Die");
                animator.SetBool("IsDead", false);
                animator.Rebind();
                animator.Update(0f);
            }

            // Refill hunger / thirst / stamina / health shown by the survival UI
            var survival = FindFirstObjectByType<SurvivalUIManager>();
            if (survival != null)
            {
                survival.SetHealth(100f);
                survival.SetHunger(100f);
                survival.SetThirst(100f);
                survival.SetStamina(100f);
            }

            UpdateHealthUI();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void Heal(int amount)
        {
            if (isDead) return;

            currentHealth += amount;
            if (currentHealth > maxHealth) currentHealth = maxHealth;

            UpdateHealthUI();
        }

        void UpdateHealthUI()
        {
            if (redHealthBarImage != null)
            {
                // Changes the standard Image fill amount natively
                redHealthBarImage.fillAmount = (float)currentHealth / maxHealth;
            }
        }
    }