using System;
using NaughtyAttributes;
using UnityEngine;

public enum OstrichEntityType { Player, Bounder, Hunter, ShadowLord, Pterodactyl }

[CreateAssetMenu(fileName = "NewOstrichEntityData", menuName = "OstrichEntityData")]
public class OstrichEntityData : ScriptableObject
{
    public OstrichEntityType type;

    [BoxGroup("PhysicsStats")] public float maxMoveSpeed;
    [BoxGroup("PhysicsStats")] public float jumpForce;

    [BoxGroup("PhysicsStats")] public float groundAcceleration;
    [BoxGroup("PhysicsStats")] public float groundDeceleration;

    [BoxGroup("PhysicsStats")] public float airAcceleration;
    [BoxGroup("PhysicsStats")] public float airDeceleration;

    [BoxGroup("PhysicsStats")] public float eggRespawnTime;


    // The following fields are only necessary if the entity is an enemy
    [HideIf("type", OstrichEntityType.Player), BoxGroup("EnemyAI")] public float yCheckAccuracy; // How good the enemy is at bonking on the player's head
    [HideIf("type", OstrichEntityType.Player), BoxGroup("EnemyAI")] public float yCheckFrequency; // How often y check is performed (higher value --> harder AI)
    
    [HideIf("type", OstrichEntityType.Player), BoxGroup("EnemyAI"), MinMaxSlider(0, 3)] public Vector2 accurateJumpFrequency;
    [HideIf("type", OstrichEntityType.Player), BoxGroup("EnemyAI"), MinMaxSlider(0, 5)] public Vector2 inaccurateJumpFrequency;
    
    [HideIf("type", OstrichEntityType.Player), BoxGroup("EnemyAI")] public float lavaCheckAccuracy;
    [HideIf("type", OstrichEntityType.Player), BoxGroup("EnemyAI")] public float lavaCheckFrequency;

    [HideIf("type", OstrichEntityType.Player), BoxGroup("EnemyAI")] public float xCheckAccuracy;
    [HideIf("type", OstrichEntityType.Player), BoxGroup("EnemyAI")] public float xCheckFrequency;

    [HideIf("type", OstrichEntityType.Player), BoxGroup("EnemyAI")] public int pointsOnDeath;
}