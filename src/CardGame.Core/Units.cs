namespace CardGame.Core;

//блок с енумами и рекордами
public enum HeroClass { Mage, Warrior, Roague }
public enum UnitType { Infantry, Cavalry, Archer }
public enum SpellType { Fire, Freeze, Moon, Holy, NonType}

public record AttackProfile(int Reach, int Lateral);

//тут герои и юниты

public abstract class Entity : ITarget
{
    public string Name { get; }
    public int Health { get; private set; }
    public int MaxHealth { get; }
    public bool IsDead => Health <= 0;

    public Entity(string name, int max_health) 
    {
        Name = name; 
        MaxHealth = max_health; 
        Health = max_health;
    }

    public virtual void TakeDamage(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Health = Math.Max(0, Health - amount);
    }

    public virtual void Heal(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Health = Math.Min(MaxHealth, Health + amount);
    }
}

public class HeroPower
{
    private readonly List<IEffect> _effects;

    public string Name { get; }
    public string Description { get; }
    public bool NeedsTarget { get; }
    public int ManaCost { get; }
    public bool UsedThisTurn { get; private set; }

    public HeroPower(string name, string descr, int cost, bool needsTarget, IEnumerable<IEffect> effects)
    {
        Name = name;
        Description = descr;
        ManaCost = cost;
        NeedsTarget = needsTarget;
        _effects = effects.ToList();
    }

    public void Use(GameContext ctx, ITarget? target)
    {
        if (UsedThisTurn) 
            throw new InvalidOperationException("Сила героя уже была использована на этом ходу");

        foreach (var effect in _effects)
            effect.Act(ctx, target);

        UsedThisTurn = true;       
    }

    public void ResetUsability() => UsedThisTurn = false;
}

public class Hero : Entity 
{
    public HeroClass Class { get; }
    public HeroPower? Power { get; } // я тут ебнул свойство нуллабельным для тестов, потом тут и ниже нужно ? снести

    public Hero(string name, HeroClass heroClass = HeroClass.Warrior, HeroPower? heroPower = null) : base(name, 30) 
    {
        Class = heroClass;
        Power = heroPower;
    }
}

public class Minion : Entity
{
    public int Attack { get; }

    public UnitType UnitType { get; }
    public AttackProfile AttackProfile { get; }
    
    public Minion(string name, int attack, int health, UnitType unitType, AttackProfile attackProfile) 
        : base(name, health) 
    {
        Attack = attack;
        UnitType = unitType;
        AttackProfile = attackProfile;
    }
}

// тут карты

public class CardPlayContext 
{
    public GameContext Context { get; }
    public ITarget? Target { get; }

    public int? Row { get; }
    public int? Column { get; }

    public CardPlayContext(GameContext ctx, int? row = null, int? column = null, ITarget? target = null)
    {
        Context = ctx;
        Target = target;
        Row = row;
        Column = column;
    }
}

public abstract class Card 
{
    private readonly List<IEffect> _effects;

    public string Name { get; }
    public string Description { get; }
    public int ManaCost { get; }
    
    public Card(string name, string descr, int cost, IEnumerable<IEffect>? effects)
    {
        Name = name;
        ManaCost = cost;
        Description = descr;
        _effects = effects?.ToList() ?? new List<IEffect>();
    }

    public abstract void PlayCard(CardPlayContext cardCtx);

    protected void ApplyEffects(GameContext ctx, ITarget? target)
    {
        foreach (var effect in _effects)
            effect.Act(ctx, target);
    }
}

public class MinionCard : Card 
{
    public UnitType UnitType { get; }
    public AttackProfile AttackProfile { get; }

    public int Attack { get; }
    public int Health { get; }

    public MinionCard(string name, string descr, int cost, int attack, int health, UnitType unitType, AttackProfile attackProfile, IEnumerable<IEffect>? effects = null) 
        : base(name, descr, cost, effects) 
    {
        Attack = attack;
        Health = health;
        UnitType = unitType;
        AttackProfile = attackProfile;
    }

    public override void PlayCard(CardPlayContext cardCtx)
    {
        if (cardCtx.Row is null || cardCtx.Column is null) throw new InvalidOperationException("Для розыгрыша существа нужны координаты (row, column)!");
        
        var minion = new Minion(Name, Attack, Health, UnitType, AttackProfile);
        var rule = PlacementRules.GetRule(UnitType);

        cardCtx.Context.Caster.BoardPlayer.Place(minion, cardCtx.Row.Value, cardCtx.Column.Value, rule);
        ApplyEffects(cardCtx.Context, cardCtx.Target);
        Console.WriteLine($"карта {Name} разыграна!");
    }
}

public class SpellCard : Card
{
    public SpellType SpellType { get; }

    public SpellCard(string name, string descr, int cost, SpellType spellType, IEnumerable<IEffect>? effects = null)
        : base(name, descr, cost, effects)
    {
        SpellType = spellType;
    }   

    public override void PlayCard(CardPlayContext cardCtx)
    {
        ApplyEffects(cardCtx.Context, cardCtx.Target);
        Console.WriteLine($"карта {Name} разыграна!"); // заглушка
    }
}

//тут стол

public class Board
{
    const int BaseRows = 3;
    const int BaseColumn = 5;

    private readonly Minion?[,] _cells = new Minion?[BaseRows, BaseColumn];

    public Minion? this[int row, int column]
    {
        get
        {
            IsValid(row, column);
            return _cells[row, column];
        }
        private set
        {
            IsValid(row, column);
            _cells[row, column] = value;
        }
    }

    public Minion? GetCell(int row, int column) => _cells[row, column];

    public void Place(Minion entity, int row, int column, Func<int, int, bool> canPlace)
    {
        if (entity is null) throw new ArgumentNullException();
        if (this[row, column] != null) throw new InvalidOperationException("Клетка уже занята!");
        if (!canPlace(row, column)) throw new InvalidOperationException("Юнита этого типа нельзя поставить в эту клетку!");

        this[row, column] = entity;
    }

    public void Remove(int row, int column) => this[row, column] = null;

    public int GetCover(int row, int column)
    {
        if ((this[row, column] is null) || row == 0) throw new ArgumentNullException("Клетка пуста или является авангардом");
        int coverLevel = this[row - 1, column] != null
            ? (row == 2 && this[row - 2, column] != null ? 2 : 1)
            : 0;
        return coverLevel;
    }

    public void IsValid(int row, int column)
    {
        if (!(row >= 0 && row < BaseRows && column >= 0 && column < BaseColumn)) throw new ArgumentOutOfRangeException();
    }
}

//тут игрок и связанные сущности

public class DeckEmptyEventArgs : EventArgs
{
    public int FatigueDamage;
    public DeckEmptyEventArgs(int damage) => FatigueDamage = damage;
}

public class Deck
{
    private const int MaxCardCount = 20;
    private int _fatigueDamage = 1;
    
    private readonly List<Card> _cardList = new List<Card>();

    public event EventHandler<DeckEmptyEventArgs>? DeckEmpty;

    public Deck(IEnumerable<Card> deck)
    {
        if (deck is null) throw new ArgumentNullException(nameof(deck));
        _cardList = deck.Take(MaxCardCount).Shuffle().ToList();
    }

    public List<Card> Draw(int cardCount) //player1.hand.Add(player1.deck.Draw(1))
    {        
        if (cardCount > MaxCardCount || 0 > cardCount) throw new ArgumentOutOfRangeException("Нельзя взять больше карт, чем существует в колоде"); 
        
        List<Card> drawnCards = new List<Card>();

        for (int i = 0; i < cardCount; i++) 
        {
            if (_cardList.Count == 0)
            {
                DeckEmpty?.Invoke(this, new DeckEmptyEventArgs(_fatigueDamage));
                _fatigueDamage *= 2;
            }                                          

            else
            {
                Card card = _cardList[0];
                _cardList.RemoveAt(0);
                drawnCards.Add(card);
            }
        }

        return drawnCards;
    }
}

public class Hand
{
    private const int MaxCardCount = 10;

    private readonly List<Card> _hand = new List<Card>();

    //public Hand(); //var deck1 = new Deck(cardListPlayer1); var player1 = new Player(deck1, new Hand()); player1.DrawCards(3);
    
    public void Add(IEnumerable<Card> cardList)
    {
        foreach(Card card in cardList)
        {
            if (_hand.Count == MaxCardCount) 
                throw new InvalidOperationException("Рука переполнена, нельзя взять еще одну карту.");

            _hand.Add(card);
        }
    }

    public Card Remove(int cardIndex)
    {        
        Card currentCard = Get(cardIndex);
        _hand.RemoveAt(cardIndex);

        return currentCard;
    }

    public Card Get(int cardIndex)
    {
        if (cardIndex < 0 || cardIndex >= _hand.Count)
            throw new ArgumentOutOfRangeException(nameof(cardIndex), "Недопустимый индекс карты.");

        return _hand[cardIndex];
    } 
}

public class Player
{
    public Hero HeroPlayer { get; }
    public Board BoardPlayer { get; }
    private Deck _deckPlayer;
    private Hand _handPlayer;

    public int CurrentMana { get; private set; }
    public int MaxMana { get; private set; }
    public bool CanAfford { get; private set; }

    public Player(Hero hero, Deck deck, Hand hand, Board board)
    {
        HeroPlayer = hero;
        _deckPlayer = deck;
        _handPlayer = hand;
        BoardPlayer = board;

        CurrentMana = 0;
        MaxMana = CurrentMana;
        CanAfford = true;

        _handPlayer.Add(_deckPlayer.Draw(3));
    }

    public void DrawCard(int cardCount)
    {
        try
        {
            List<Card> cards = _deckPlayer.Draw(cardCount);
            if (cards.Count == 0) return;

            _handPlayer.Add(cards);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine(ex.Message);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public void PlayCard(int cardIndex, CardPlayContext ctx)
    {
        Card currentCard = _handPlayer.Get(cardIndex);
        if (!(currentCard.ManaCost <= CurrentMana)) throw new ArgumentOutOfRangeException("Недостаточно маны чтобы сыграть карту!");
        try
        {
            currentCard.PlayCard(ctx);
             _handPlayer.Remove(cardIndex);
             CurrentMana -= currentCard.ManaCost;
        }
        catch(InvalidOperationException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    public void OnDeckEmpty(object? sender, DeckEmptyEventArgs e)
    {
        Console.WriteLine($"Игрок {HeroPlayer.Name} получает {e.FatigueDamage} ед. урона от усталости!");
    }

    public void StartTurn()
    {
        MaxMana = Math.Min(MaxMana + 1, 10);
        CurrentMana = MaxMana;
        HeroPlayer.Power?.ResetUsability();
        DrawCard(1);
    }
}

//тут будет игровой процесс