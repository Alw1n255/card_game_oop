namespace CardGame.Core;

public interface ITarget
{
    string Name { get; }
    int Health { get; }
    bool IsDead { get; }

    void TakeDamage(int amount);
    void Heal(int amount);
}

public interface IEffect
{
    void Act(GameContext ctx, ITarget? target);
}