using UnityEngine;

[CreateAssetMenu(menuName = "PowerUp/Effect")]
public class PowerUpEffect : ScriptableObject
{
    public string powerUpName;
    public string description;
    public int damageIncrease;
    public int healthChange;
    public int moveSpeed;
    public Sprite itemSprite;
    

    public virtual void Apply(Player player)
    {
        player.damage += damageIncrease;
        if (player.damage <= 1) {
            player.damage = 1;
        }
        player.health += healthChange;
        if (player.health <= 0) {
            player.health = 1;
        }
        Health.Instance.UpdateHealthSprites();
    }
}
