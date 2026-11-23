using System.Text.Json;

namespace CardGame.Core;

//фабрики на карты/эффекты и их производные
public class CardDTO // dto классы для передачи данных из json-файла
{
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int ManaCost { get; set; }

    public int? Attack { get; set; }
    public int? Health { get; set; }
    public string? UnitType { get; set; }
    public string? SpellType { get; set; }

    public List<EffectDTO>? Effects { get; set; }
}

public class EffectDTO
{
    public string Type { get; set; } = "";
    public int Damage { get; set; }
    public bool NeedsTarget { get; set; }
}

public class HeroDto
{
    public string HeroClass { get; set; } = "";
    public string Name { get; set; } = "";
    public HeroPowerDto HeroPower { get; set; } = new();
}

public class HeroPowerDto
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int ManaCost { get; set; }
    public bool NeedsTarget { get; set; }
    public List<EffectDTO>? Effects { get; set; }
}

public static class JsonLoader
{
    public static List<T> LoadList<T>(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Файл не найден: {filePath}");

        string jsonStr = File.ReadAllText(filePath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var result = JsonSerializer.Deserialize<List<T>>(jsonStr, options);

        return result ?? new List<T>();
    }
}

public static class EffectFactory
{
    public static IEffect CreateEffect(EffectDTO dto) => dto.Type switch
    { 
        "ThrowTargetDamageEffect" => new ThrowTargetDamageEffect(dto.Damage),   
        _ => throw new ArgumentException($"Неизвестный тип эффекта: {dto.Type}")
    };

    public static List<IEffect> CreateEffects(List<EffectDTO>? effectsDto)
    {                                                                     
        var effects = new List<IEffect>();

        if (effectsDto is null)                                           
            return effects;                                               
                                                                          
        foreach (EffectDTO effectDto in effectsDto) 
        {
            effects.Add(CreateEffect(effectDto));
        }

        return effects;                                                   
    }                                                                     

}

public static class CardFactory
{
    public static List<Card> LoadCards(string filePath)
    {
        UnitType unitType;
        SpellType spellType;

        var dtos = JsonLoader.LoadList<CardDTO>(filePath);
        
        var cards = new List<Card>();
        if (dtos is null) return cards;

        foreach (CardDTO dto in dtos)
        {
            if (dto.Type == "Minion")
            {
                if (!Enum.TryParse<UnitType>(dto.UnitType, out unitType))
                    throw new ArgumentException($"Неизвестный тип юнита: {dto.UnitType}");
                if (dto.Attack is null || dto.Health is null)
                    throw new ArgumentException($"У карты {dto.Name} отсутствуют обязательные поля Attack или Health!");               
           
                AttackProfile attackProfile = unitType switch
                {
                    UnitType.Infantry => new AttackProfile(0, 1),
                    _ => throw new ArgumentException("Неизвестный тип")
                };

                var effects = EffectFactory.CreateEffects(dto.Effects);

                var minionCard = new MinionCard
                (
                    dto.Name, 
                    dto.Description, 
                    dto.ManaCost, 
                    dto.Attack.Value, 
                    dto.Health.Value, 
                    unitType, 
                    attackProfile,
                    effects
                );
                cards.Add(minionCard);
            }
            else if (dto.Type == "Spell")
            {
                if (!Enum.TryParse<SpellType>(dto.SpellType, out spellType))
                    throw new ArgumentException($"Неизвестный тип заклинания: {dto.SpellType}");

                var effects = EffectFactory.CreateEffects(dto.Effects);

                var spellCard = new SpellCard
                (
                    dto.Name,
                    dto.Description,
                    dto.ManaCost,
                    spellType,
                    effects
                );
                cards.Add(spellCard);
            }
            else throw new ArgumentException($"Неизвестный тип карты: {dto.Type}");
        }

        return cards;
    }
}

public static class HeroFactory
{
    public static List<Hero> LoadHeroes(string filePath)
    {
        HeroClass heroClass;

        var dtos = JsonLoader.LoadList<HeroDto>(filePath);
    
        var heroes = new List<Hero>();
    
        foreach (var dto in dtos)
        {
            if (!Enum.TryParse<HeroClass>(dto.HeroClass, out heroClass))
                throw new ArgumentException($"Неизвестный класс героя: {dto.HeroClass}");

            var effects = EffectFactory.CreateEffects(dto.HeroPower.Effects);

            var heroPower = new HeroPower
            (
                dto.HeroPower.Name,
                dto.HeroPower.Description,
                dto.HeroPower.ManaCost,
                dto.HeroPower.NeedsTarget,
                effects
            );

            var hero = new Hero
            (
                dto.Name,
                heroClass,
                heroPower
            );
            heroes.Add(hero);
        }

        return heroes;
    }
}