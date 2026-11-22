using CardGame.Core;

var hero = new Hero("Гаррош");
var enemy = new Hero("Джайна");

var targets = new List<Hero> {hero, enemy};

Console.WriteLine(hero.Name + "\nииииииииииииииии...\n" + enemy.Name + "!");