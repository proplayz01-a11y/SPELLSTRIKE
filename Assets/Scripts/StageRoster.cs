public struct StageMetadata
{
    public string stageName;
    public string[] enemies;
    public string boss;
    public string flavour;
}

public static class StageRoster
{
    public static readonly StageMetadata[] Stages = new StageMetadata[]
    {
        new StageMetadata
        {
            stageName = "Stage 1: Enchanted Kingdom",
            enemies = new string[] { "Bramble Sprite", "Stone Sentinel", "Cursed Jester" },
            boss = "Hollow Knight",
            flavour = "Tutorial stage — no debuffs. Your journey into the living book begins."
        },
        new StageMetadata
        {
            stageName = "Stage 2: Sunken Seas",
            enemies = new string[] { "Barnacle Husk", "Corsair Phantom", "Tide Wraith" },
            boss = "Drowned Captain",
            flavour = "Debuffs start here: Tile Locking, Tile Cracking, Timed Pressure."
        },
        new StageMetadata
        {
            stageName = "Stage 3: Mythic Ruins",
            enemies = new string[] { "Runic Golem", "Specter Archer", "Minotaur Shade" },
            boss = "Titan Colossus",
            flavour = "Three-phase boss fight and rune shield challenge."
        },
        new StageMetadata
        {
            stageName = "Stage 4: Cursed Gothic",
            enemies = new string[] { "Plague Wraith", "Vampiric Shade", "Gargoyle Sentinel" },
            boss = "Crimson Duchess",
            flavour = "Shadow dragon appears at 25% health."
        },
        new StageMetadata
        {
            stageName = "Stage 5: The Final Chapter",
            enemies = new string[] { "Ember Wyrm", "Void Herald", "Eternal Shade" },
            boss = "Master Wizard",
            flavour = "The final duel for SpellStrike itself."
        }
    };
}
