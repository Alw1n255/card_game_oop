using CardGame.Core;

namespace CardGame.Tests;

public class UnitTest1
{
    [Fact]
    public void TakeDamage_Hero()
    {
        var hero = new Hero("Artas");

        hero.TakeDamage(40);

        Assert.Equal(0, hero.Health);
        Assert.True(hero.IsDead);
    }

    [Fact]
    public void TakeHeal_Hero()
    {
        var hero = new Hero("Artas");

        hero.Heal(10);
        Assert.Equal(hero.Health, hero.MaxHealth);

        hero.TakeDamage(15);
        Assert.Equal(15, hero.Health);

        hero.Heal(10);
        Assert.Equal(25, hero.Health);
    }

    [Fact]
    public void TakeNegativeDamage()
    {
        var hero = new Hero("Artas");

       Assert.Throws<ArgumentOutOfRangeException>(() => hero.TakeDamage(-1)); 
    }
}
