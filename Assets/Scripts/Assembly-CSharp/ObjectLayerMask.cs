internal static class ObjectLayerMask
{
	// Computed live from ObjectLayer so these never bake stale values and never
	// call NameToLayer themselves (safe to read from field initializers/ctors).
	// ObjectLayer is populated by ObjectLayer.AutoInitialize (RuntimeInitializeOnLoadMethod).
	public static int Default
	{
		get { return 1 << ObjectLayer.Default; }
	}

	public static int Player
	{
		get { return 1 << ObjectLayer.Player; }
	}

	public static int Enemy
	{
		get { return 1 << ObjectLayer.Enemy; }
	}

	public static int EnemyBox
	{
		get { return 1 << ObjectLayer.EnemyBox; }
	}

	public static int IgnoreEnemy
	{
		get { return 1 << ObjectLayer.IgnoreEnemy; }
	}

	public static int IgnorePlayer
	{
		get { return 1 << ObjectLayer.IgnorePlayer; }
	}

	public static int IgnoreRaycast
	{
		get { return 1 << ObjectLayer.IgnoreRaycast; }
	}

	public static int IgnoreBullets
	{
		get { return 1 << ObjectLayer.IgnoreBullets; }
	}

	public static int Pumpkin
	{
		get { return 1 << ObjectLayer.Pumpkin; }
	}
}
