using UnityEngine;

public class EnemyStatsController : CharacterStatsController
{
    public override void Die() {
       Destroy(gameObject, 0);
    }
}