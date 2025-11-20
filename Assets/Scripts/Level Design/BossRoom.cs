using UnityEngine;

public class BossRoom : Room
{
    // Boss room specific functionality can be added here
    
    protected override void Awake()
    {
        base.Awake();
        
        // Set room type to Boss
        roomType = RoomType.Boss;
    }
    
    public override void MarkCleared()
    {
        if (isCleared) return;
        
        // Call base room clearing logic
        base.MarkCleared();

    }
    

    
    protected override void CheckRoomClearCondition()
    {
        // Boss room specific clear condition
        // This could be modified to check for boss-specific conditions
        // For example, checking if a specific boss enemy is defeated
        
        base.CheckRoomClearCondition();
    }
    
    public override void EnterRoom()
    {
        base.EnterRoom();
        
        // Special boss room entry effects could go here
        // For example: dramatic music, boss introduction, etc.
        Debug.Log("Entered the boss room! Prepare for battle!");
    }
    

    
    // Override to handle boss room specific behavior
    public override void LockExits()
    {
        base.LockExits();
        
        // Boss rooms might have additional locking behavior
        // For example, sealing the room with special effects
    }
    
    public override void UnlockExits()
    {
        base.UnlockExits();
        
        // Boss rooms might have additional unlocking behavior
    }
}