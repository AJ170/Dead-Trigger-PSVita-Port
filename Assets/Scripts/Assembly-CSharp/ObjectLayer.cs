using UnityEngine;

internal class ObjectLayer
{
    public static int Default;

    public static int Player;

    public static int Enemy;

    public static int EnemyBox;

    public static int IgnoreEnemy;

    public static int IgnorePlayer;

    public static int IgnoreRaycast;

    public static int IgnoreBullets;

    public static int Pumpkin;

    // ObjectLayer is not a MonoBehaviour, so Unity never called Awake() and the
    // layer ids stayed 0 -> ObjectLayerMask.IgnoreRaycast/IgnoreBullets resolved
    // to the Default(0) bit, so ~(IgnoreRaycast|IgnoreBullets) excluded layer 0
    // and bullets passed through all Default-layer level geometry.
    // NameToLayer may not be called from a MonoBehaviour ctor/field initializer,
    // so we initialize from RuntimeInitializeOnLoadMethod (a legal time that runs
    // once before the first scene loads).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        Initialize();
    }

    public static void Initialize()
    {
        Default = LayerMask.NameToLayer("Default");
        Player = LayerMask.NameToLayer("Player");
        Enemy = LayerMask.NameToLayer("Enemy");
        EnemyBox = LayerMask.NameToLayer("EnemyBox");
        IgnoreEnemy = LayerMask.NameToLayer("Ignore Enemy");
        IgnorePlayer = LayerMask.NameToLayer("Ignore Player");
        IgnoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
        IgnoreBullets = LayerMask.NameToLayer("Ignore Bullets");
        Pumpkin = LayerMask.NameToLayer("Pumpkin");
    }

    // The physics layer collision matrix was reset during the port (every layer
    // collides with every layer), which defeated the game's layer-based collision
    // filtering. Re-apply the filtering the player relies on: the player must not
    // physically collide with corpse ragdolls (Ignore Player layer), enemy
    // hit-boxes (EnemyBox) or Ignore Raycast objects. Living zombies (Enemy layer)
    // still block the player. Physics.IgnoreLayerCollision persists for the session;
    // ComponentPlayer calls this on each spawn so it is always applied.
    public static void ApplyPlayerCollisionRules()
    {
        Initialize();
        // Dead-zombie ragdolls live on the Ignore Player layer, and enemy hit-boxes
        // on EnemyBox - the player should pass through both.
        Physics.IgnoreLayerCollision(Player, IgnorePlayer, true);
        Physics.IgnoreLayerCollision(Player, EnemyBox, true);
        // NOTE: do NOT disable Player <-> Ignore Raycast. Living zombies block the
        // player with their _Capsule collider, which sits on the Ignore Raycast
        // layer (so bullets pass through it to the _Box hitzone). Disabling it lets
        // the player walk through living zombies. On death the _Capsule is
        // deactivated, so keeping this enabled does not affect corpses.
    }

public void Awake()
    {
        ObjectLayer.Initialize();
    }
}
