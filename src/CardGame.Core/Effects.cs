namespace CardGame.Core;

//классы эффектов
public class ThrowTargetDamageEffect : IEffect
{
    private readonly int _damage;
    
    public ThrowTargetDamageEffect(int damage) => _damage = damage;

    public void Act(GameContext ctx, ITarget? target)
    {
        if (target is null) throw new InvalidOperationException("Нужна цель!");
        target.TakeDamage(_damage);
    }
}

public class ThrowEnemyHeroDamageEffect : IEffect
{
    private readonly int _damage;

    public ThrowEnemyHeroDamageEffect(int damage) => _damage = damage;

    public void Act(GameContext ctx, ITarget? target)
    {
        ctx.Opponent.HeroPlayer.TakeDamage(_damage);
    }
}