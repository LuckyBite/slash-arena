// Единый интерфейс получения урона.
// Реализуют: PlayerHealth, EnemyHealth. Бьют через него: PlayerAttack, EnemyAI.
public interface IDamageable
{
    void TakeDamage(int amount);
}
