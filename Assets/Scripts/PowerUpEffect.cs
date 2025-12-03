using UnityEngine;

[CreateAssetMenu(menuName = "PowerUp/Effect")]
public class PowerUpEffect : ScriptableObject
{
    public string powerUpName;
    public string description;
    public float moveSpeed, damage, meleeCooldown, hitboxRange, health, maxHealth;
    public Sprite itemSprite;
    
    public virtual void Apply(Player player)
    {
        player.damage += damage;
        if (player.damage <= 1) {
            player.damage = 1;
        }
        player.health += (int)health;
        if (player.health <= 0) {
            player.health = 1;
        } else if (player.health > player.maxHealth) {
            player.health = player.maxHealth;
        }
        Health.Instance.UpdateHealthSprites();
    }
}
