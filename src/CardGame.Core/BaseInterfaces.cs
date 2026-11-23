namespace CardGame.Core;

//базовые интерфейсы и общие классы
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

public static class PlacementRules
{
    public static Func<int, int, bool> GetRule(UnitType type)
    {
        return type switch
        {
            UnitType.Archer => (r, c) => r >= 1,
            UnitType.Infantry => (r, c) => r < 2 && (c >= 1 && c <= 3),
            UnitType.Cavalry => (r, c) => c <= 1 || c >= 3,
            _ => throw new ArgumentException("Неизвестный тип юнита")
        };
    }
}

public class GameContext
{
    public Player Caster { get; }
    public Player Opponent { get; }

    public GameContext(Player caster, Player opponent)
    { 
        Caster = caster; 
        Opponent = opponent; 
    }
}