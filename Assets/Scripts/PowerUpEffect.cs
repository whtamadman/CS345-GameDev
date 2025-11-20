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
        player.health += healthChange;
    }
}
