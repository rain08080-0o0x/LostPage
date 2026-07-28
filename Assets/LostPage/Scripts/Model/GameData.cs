using System;
using System.Collections.Generic;
using System.Linq;

namespace LostPage
{
    public enum EtherType
    {
        Red,
        Blue,
        Yellow,
        Purple
    }

    public enum CardKind
    {
        Attack,
        HeavyAttack,
        AreaAttack,
        GrowthAttack,
        RandomBarrage,
        RedPulseAttack,
        PiercingAreaAttack,
        AceAttack,
        GrowingSword,
        FlameStrike,
        SweepingFlameStrike,
        SurgingFlame,
        Combustion,
        ExplosionFlame,
        DefenseConversion,
        Heatstroke,
        Defense,
        StrongDefense,
        AutoDefense,
        GuardContinuance,
        MirrorShield,
        FullDefense,
        SpikedShield,
        ThornArmor,
        HeavyArmor,
        CounterDraw,
        FullLightAutoDefense,
        RegressionDefense,
        Barrier,
        GoldConversion,
        Charge,
        Resonance,
        HealCharge,
        EtherConversion,
        BlueResonance,
        YellowResonance,
        PurpleResonance,
        PenetrationPower,
        AttackRecovery,
        RedPaperSummon,
        ShiftingShadow,
        MagicMirror,
        DuplicateAttack,
        Persistent,
        GuardCyclePersistent,
        DefensePersistence,
        CrimsonMoon,
        PiercingShield,
        AdditionalDefense,
        DefenseSupport,
        DoubleAttackPersistent,
        AreaAttackPersistent,
        WristSupporter,
        Tempering,
        DivineStrike
    }

    public enum CardCategory
    {
        Attack,
        Defense,
        Charge,
        Persistent,
        Special
    }

    public enum CardRarity
    {
        Normal,
        Rare,
        Boss,
        Special
    }

    public enum EnemyActionKind
    {
        Attack,
        Defense,
        Weaken
    }

    public enum StageKind
    {
        Start,
        Battle,
        Fountain,
        Shop,
        RandomEvent,
        Reward,
        Tool,
        Boss
    }

    public enum CarryToolKind
    {
        AttackBoost,
        ShieldBoost,
        FirstAttackPierce,
        SmallBag,
        RedCrystal,
        BlueCrystal,
        StarFragment,
        StarMass,
        ShieldStorage,
        VorpalHeart,
        PresentBox,
        BigBag,
        StarOrb,
        EnergyCore,
        LargeBelt,
        GuardianJudgment,
        VorpalDagger,
        Myojo
    }

    public enum CarryToolRarity
    {
        Normal,
        Rare,
        Boss,
        Special
    }

    public enum BattlePhase
    {
        PlayerTurn,
        EnemyTurn,
        Victory,
        Defeat
    }

    public sealed class CardInstance
    {
        public CardInstance(int id, CardKind kind)
        {
            Id = id;
            Kind = kind;
        }

        public int Id { get; }
        public CardKind Kind { get; }
        public bool PersistentActivated { get; set; }
        public int CooldownRemaining { get; set; }
        public int GrowthBonus { get; set; }
        public bool IsUpgraded { get; set; }
        public bool IsTemporary { get; set; }
        public bool UsedThisBattle { get; set; }
    }

    public sealed class CardDefinition
    {
        public CardDefinition(
            string name,
            CardCategory category,
            CardRarity rarity,
            string description,
            IReadOnlyDictionary<EtherType, int> cost,
            int cooldown,
            string upgradedDescription,
            IReadOnlyDictionary<EtherType, int> upgradedCost,
            int upgradedCooldown,
            bool oncePerBattle = false,
            bool upgradedOncePerBattle = false,
            bool canUpgrade = true)
        {
            Name = name;
            Category = category;
            Rarity = rarity;
            Description = description;
            Cost = cost;
            Cooldown = cooldown;
            UpgradedDescription = upgradedDescription;
            UpgradedCost = upgradedCost;
            UpgradedCooldown = upgradedCooldown;
            OncePerBattle = oncePerBattle;
            UpgradedOncePerBattle = upgradedOncePerBattle;
            CanUpgrade = canUpgrade;
        }

        public string Name { get; }
        public CardCategory Category { get; }
        public CardRarity Rarity { get; }
        public string Description { get; }
        public IReadOnlyDictionary<EtherType, int> Cost { get; }
        public int Cooldown { get; }
        public string UpgradedDescription { get; }
        public IReadOnlyDictionary<EtherType, int> UpgradedCost { get; }
        public int UpgradedCooldown { get; }
        public bool OncePerBattle { get; }
        public bool UpgradedOncePerBattle { get; }
        public bool CanUpgrade { get; }
    }

    public static class CardCatalog
    {
        private static readonly IReadOnlyDictionary<CardKind, CardDefinition>
            Definitions = CreateDefinitions();

        public static IEnumerable<CardKind> AllKinds => Definitions.Keys;

        public static string GetName(CardKind kind)
        {
            return Get(kind).Name;
        }

        public static string GetShortDescription(CardKind kind)
        {
            return Get(kind).Description;
        }

        public static string GetShortDescription(CardInstance card)
        {
            return card.IsUpgraded
                ? Get(card.Kind).UpgradedDescription
                : Get(card.Kind).Description;
        }

        public static IReadOnlyDictionary<EtherType, int> GetCost(CardKind kind)
        {
            return Get(kind).Cost;
        }

        public static IReadOnlyDictionary<EtherType, int> GetCost(
            CardInstance card)
        {
            return card.IsUpgraded
                ? Get(card.Kind).UpgradedCost
                : Get(card.Kind).Cost;
        }

        public static string GetCostText(CardKind kind)
        {
            return GetCostText(GetCost(kind));
        }

        public static string GetCostText(CardInstance card)
        {
            return card.Kind == CardKind.DivineStrike
                ? "手番エーテルすべて"
                : GetCostText(GetCost(card));
        }

        public static int GetCooldown(CardKind kind)
        {
            return Get(kind).Cooldown;
        }

        public static int GetCooldown(CardInstance card)
        {
            return card.IsUpgraded
                ? Get(card.Kind).UpgradedCooldown
                : Get(card.Kind).Cooldown;
        }

        public static bool IsOncePerBattle(CardInstance card)
        {
            return card.IsUpgraded
                ? Get(card.Kind).UpgradedOncePerBattle
                : Get(card.Kind).OncePerBattle;
        }

        public static CardCategory GetCategory(CardKind kind)
        {
            return Get(kind).Category;
        }

        public static CardRarity GetRarity(CardKind kind)
        {
            return Get(kind).Rarity;
        }

        public static string GetRarityLabel(CardKind kind)
        {
            switch (GetRarity(kind))
            {
                case CardRarity.Normal:
                    return "通常";
                case CardRarity.Rare:
                    return "レア";
                case CardRarity.Boss:
                    return "ボス";
                case CardRarity.Special:
                    return "特殊";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static bool CanUpgrade(CardInstance card)
        {
            return card != null && Get(card.Kind).CanUpgrade && !card.IsUpgraded;
        }

        public static bool IsAttack(CardKind kind)
        {
            return GetCategory(kind) == CardCategory.Attack;
        }

        public static bool IsSingleTargetAttack(CardKind kind)
        {
            switch (kind)
            {
                case CardKind.Attack:
                case CardKind.HeavyAttack:
                case CardKind.GrowthAttack:
                case CardKind.RedPulseAttack:
                case CardKind.AceAttack:
                case CardKind.GrowingSword:
                case CardKind.FlameStrike:
                case CardKind.SurgingFlame:
                case CardKind.DefenseConversion:
                case CardKind.Heatstroke:
                    return true;
                default:
                    return false;
            }
        }

        public static bool RequiresEnemyTarget(CardKind kind)
        {
            return IsSingleTargetAttack(kind) ||
                   kind == CardKind.ExplosionFlame;
        }

        public static bool IsPersistent(CardKind kind)
        {
            return GetCategory(kind) == CardCategory.Persistent;
        }

        public static string GetEtherName(EtherType type)
        {
            switch (type)
            {
                case EtherType.Red:
                    return "赤";
                case EtherType.Blue:
                    return "青";
                case EtherType.Yellow:
                    return "黄";
                case EtherType.Purple:
                    return "紫";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private static CardDefinition Get(CardKind kind)
        {
            if (!Definitions.TryGetValue(kind, out var definition))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }

            return definition;
        }

        private static string GetCostText(
            IReadOnlyDictionary<EtherType, int> cost)
        {
            var parts = new List<string>();
            foreach (var pair in cost)
            {
                parts.Add($"{GetEtherName(pair.Key)}{pair.Value}");
            }

            return parts.Count == 0 ? "なし" : string.Join("・", parts);
        }

        private static IReadOnlyDictionary<EtherType, int> Cost(
            int red = 0,
            int blue = 0,
            int yellow = 0,
            int purple = 0)
        {
            var result = new Dictionary<EtherType, int>();
            if (red > 0)
            {
                result[EtherType.Red] = red;
            }

            if (blue > 0)
            {
                result[EtherType.Blue] = blue;
            }

            if (yellow > 0)
            {
                result[EtherType.Yellow] = yellow;
            }

            if (purple > 0)
            {
                result[EtherType.Purple] = purple;
            }

            return result;
        }

        private static CardDefinition D(
            string name,
            CardCategory category,
            CardRarity rarity,
            string normal,
            IReadOnlyDictionary<EtherType, int> normalCost,
            int normalCooldown,
            string upgraded,
            IReadOnlyDictionary<EtherType, int> upgradedCost,
            int upgradedCooldown,
            bool once = false,
            bool upgradedOnce = false,
            bool canUpgrade = true)
        {
            return new CardDefinition(
                name,
                category,
                rarity,
                normal,
                normalCost,
                normalCooldown,
                upgraded,
                upgradedCost,
                upgradedCooldown,
                once,
                upgradedOnce,
                canUpgrade);
        }

        private static IReadOnlyDictionary<CardKind, CardDefinition>
            CreateDefinitions()
        {
            var attack = CardCategory.Attack;
            var defense = CardCategory.Defense;
            var charge = CardCategory.Charge;
            var persistent = CardCategory.Persistent;
            var normal = CardRarity.Normal;
            var rare = CardRarity.Rare;
            var boss = CardRarity.Boss;
            return new Dictionary<CardKind, CardDefinition>
            {
                [CardKind.Attack] = D("攻撃カード", attack, normal,
                    "敵単体に5ダメージ", Cost(red: 2), 1,
                    "敵単体に10ダメージ", Cost(red: 1), 1),
                [CardKind.HeavyAttack] = D("強撃", attack, normal,
                    "敵単体に17ダメージ", Cost(red: 2, blue: 1), 2,
                    "敵単体に24ダメージ", Cost(red: 2), 2),
                [CardKind.AreaAttack] = D("薙ぎ払い", attack, normal,
                    "敵全体に9ダメージ", Cost(red: 3), 2,
                    "敵全体に15ダメージ", Cost(red: 2), 1),
                [CardKind.GrowthAttack] = D("成長撃", attack, rare,
                    "敵単体に8ダメージ。使用後、このカード個体の基礎ダメージ+3", Cost(red: 3), 2,
                    "敵単体に10ダメージ。使用後、このカード個体の基礎ダメージ+4", Cost(red: 2), 2),
                [CardKind.RandomBarrage] = D("乱撃", attack, rare,
                    "生存する敵へランダムに10ダメージを4回", Cost(red: 4), 4,
                    "生存する敵へランダムに18ダメージを4回", Cost(red: 3), 3),
                [CardKind.RedPulseAttack] = D("紅脈撃", attack, boss,
                    "敵単体に総赤エーテル数+10ダメージ", Cost(red: 3, blue: 2), 4,
                    "敵単体に総赤エーテル数+15ダメージ", Cost(red: 2, blue: 1), 3),
                [CardKind.PiercingAreaAttack] = D("破界撃", attack, rare,
                    "敵全体に36貫通ダメージ", Cost(red: 5), 4,
                    "敵全体に48貫通ダメージ", Cost(red: 4), 3),
                [CardKind.AceAttack] = D("切り札", attack, boss,
                    "攻撃カード所持数×4+総赤エーテル数の単体ダメージ", Cost(red: 5), 5,
                    "攻撃カード所持数×8+総赤エーテル数の単体ダメージ", Cost(red: 4), 4),
                [CardKind.GrowingSword] = D("成長する刀", attack, rare,
                    "敵単体に1ダメージ。攻撃カード使用ごとにこのカードの攻撃力+2", Cost(red: 2, blue: 1), 2,
                    "敵単体に1ダメージ。攻撃カード使用ごとにこのカードの攻撃力+4", Cost(red: 2), 2),
                [CardKind.FlameStrike] = D("炎撃", attack, normal,
                    "敵単体に7ダメージ、炎3を付与", Cost(red: 2, blue: 1), 1,
                    "敵単体に10ダメージ、炎6を付与", Cost(red: 2), 1),
                [CardKind.SweepingFlameStrike] = D("薙ぎ炎撃", attack, normal,
                    "敵全体に6ダメージ、炎3を付与", Cost(red: 3), 2,
                    "敵全体に12ダメージ、炎5を付与", Cost(red: 2), 2),
                [CardKind.SurgingFlame] = D("湧き上がる炎", attack, rare,
                    "敵単体に5ダメージと炎2付与を2回", Cost(red: 2), 3,
                    "敵単体に14ダメージと炎4付与を2回", Cost(red: 2), 2),
                [CardKind.Combustion] = D("燃焼", attack, normal,
                    "敵全体の炎を3回発動", Cost(red: 2), 3,
                    "敵全体の炎を3回発動", Cost(red: 2), 2),
                [CardKind.ExplosionFlame] = D("爆炎", attack, rare,
                    "敵単体の炎を2倍", Cost(red: 2), 0,
                    "敵単体の炎を2倍", Cost(red: 1), 0, true, true),
                [CardKind.DefenseConversion] = D("防御変換", attack, boss,
                    "シールドの半分を消費して同量の単体ダメージ。チャージを2倍適用", Cost(red: 2, blue: 1, yellow: 1), 2,
                    "シールドの半分を消費して同量の単体ダメージ。チャージを2倍適用", Cost(red: 1, blue: 1, yellow: 1), 2),
                [CardKind.Heatstroke] = D("熱射病", attack, boss,
                    "敵単体に炎4を付与し、炎がなくなるまで発動", Cost(red: 2, yellow: 1), 0,
                    "敵単体に炎6を付与し、炎がなくなるまで発動", Cost(red: 1, yellow: 1), 3, true, false),

                [CardKind.Defense] = D("防御カード", defense, normal,
                    "自分に7シールド", Cost(red: 1, blue: 1), 1,
                    "自分に12シールド", Cost(blue: 1), 1),
                [CardKind.StrongDefense] = D("堅守", defense, normal,
                    "自分に12シールド", Cost(blue: 2), 2,
                    "自分に20シールド", Cost(blue: 2), 1),
                [CardKind.AutoDefense] = D("自動障壁", defense, normal,
                    "自動防御を3獲得", Cost(blue: 2), 2,
                    "自動防御を6獲得", Cost(blue: 2), 2),
                [CardKind.GuardContinuance] = D("守勢継続", defense, normal,
                    "自動防御と防御維持を2ずつ獲得", Cost(blue: 3), 3,
                    "自動防御と防御維持を2ずつ獲得", Cost(blue: 2), 2),
                [CardKind.MirrorShield] = D("鏡盾", defense, rare,
                    "現在のシールドを2倍にし、防御維持2を獲得", Cost(blue: 4), 4,
                    "現在のシールドを3倍にし、防御維持3を獲得", Cost(blue: 2), 3),
                [CardKind.FullDefense] = D("全力防御", defense, rare,
                    "総青エーテル数だけシールドを獲得", Cost(blue: 3), 2,
                    "総青エーテル数×2のシールドを獲得", Cost(blue: 3), 2),
                [CardKind.SpikedShield] = D("棘付き盾", defense, normal,
                    "シールド6、反射2を獲得", Cost(red: 1, blue: 2), 2,
                    "シールド12、反射4を獲得", Cost(red: 1, blue: 1), 2),
                [CardKind.ThornArmor] = D("茨の鎧", defense, normal,
                    "反射6を獲得", Cost(red: 1, blue: 2), 3,
                    "反射12を獲得", Cost(red: 1, blue: 1), 2),
                [CardKind.HeavyArmor] = D("重厚鎧", defense, rare,
                    "シールド14を獲得し、防御維持がなければ防御維持1を獲得", Cost(blue: 3), 2,
                    "シールド14を獲得し、防御維持がなければ防御維持1を獲得", Cost(blue: 2), 2),
                [CardKind.CounterDraw] = D("カウンタードロー", defense, rare,
                    "シールド6を獲得。攻撃予定の敵数だけエーテルをドロー", Cost(blue: 1, yellow: 1), 3,
                    "シールド6を獲得。攻撃予定の敵数だけエーテルをドロー", Cost(blue: 1, yellow: 1), 2),
                [CardKind.FullLightAutoDefense] = D("フルライト自動防御", defense, boss,
                    "自動防御12、防御維持3を獲得", Cost(blue: 3), 8,
                    "自動防御24、防御維持8を獲得", Cost(blue: 2), 4),
                [CardKind.RegressionDefense] = D("回帰防御", defense, boss,
                    "戦闘中に失ったシールド合計を獲得し、防御維持3を獲得", Cost(red: 1, blue: 2, yellow: 1), 0,
                    "戦闘中に失ったシールド合計×1.5を獲得し、防御維持6を獲得", Cost(red: 1, blue: 1, yellow: 1), 5, true, false),
                [CardKind.Barrier] = D("防壁", defense, boss,
                    "シールド30、防御維持5を獲得", Cost(blue: 4), 0,
                    "シールド30、防御維持8を獲得", Cost(blue: 3), 3, true, false),
                [CardKind.GoldConversion] = D("金貨変換", defense, rare,
                    "所持ゴールドの7%を消費し、その2倍のシールドを獲得", Cost(blue: 2, yellow: 1), 3,
                    "所持ゴールドの7%を消費し、その4倍のシールドを獲得", Cost(blue: 1, yellow: 1), 2),

                [CardKind.Charge] = D("チャージカード", charge, normal,
                    "チャージ3を獲得", Cost(yellow: 2), 2,
                    "チャージ8を獲得", Cost(yellow: 1), 2),
                [CardKind.Resonance] = D("共鳴", charge, rare,
                    "総赤エーテル数だけチャージを獲得", Cost(red: 2, yellow: 1), 3,
                    "総赤エーテル数×1.5のチャージを獲得", Cost(red: 1, yellow: 1), 2),
                [CardKind.HealCharge] = D("治癒", charge, normal,
                    "HPを6回復", Cost(yellow: 3), 2,
                    "HPを12回復", Cost(yellow: 2), 2),
                [CardKind.EtherConversion] = D("転換", charge, rare,
                    "手番エーテルをランダムな1色へ変換し、その色を2個追加", Cost(yellow: 3), 3,
                    "手番エーテルをランダムな1色へ変換し、その色を4個追加", Cost(yellow: 2), 2),
                [CardKind.BlueResonance] = D("共鳴-青-", charge, normal,
                    "総青エーテル数だけチャージを獲得", Cost(blue: 2, yellow: 1), 2,
                    "総青エーテル数×1.5のチャージを獲得", Cost(blue: 1, yellow: 1), 2),
                [CardKind.YellowResonance] = D("共鳴-黄-", charge, normal,
                    "総黄エーテル数×3のチャージを獲得", Cost(yellow: 3), 3,
                    "総黄エーテル数×6のチャージを獲得", Cost(yellow: 2), 2),
                [CardKind.PurpleResonance] = D("共鳴-紫-", charge, normal,
                    "所持する持続カードをランダムに1枚発動", Cost(purple: 3), 3,
                    "未発動の持続カードをランダムに1枚発動", Cost(yellow: 1, purple: 1), 2),
                [CardKind.PenetrationPower] = D("貫通力", charge, normal,
                    "貫通5を獲得", Cost(red: 1, yellow: 1), 5,
                    "貫通10を獲得", Cost(yellow: 1), 3),
                [CardKind.AttackRecovery] = D("攻撃回収", charge, normal,
                    "このターンに使用した攻撃カード数だけ攻撃力を獲得", Cost(red: 1, yellow: 1), 2,
                    "このターンに使用した攻撃カード数×2の攻撃力を獲得", Cost(red: 1, yellow: 1), 2),
                [CardKind.RedPaperSummon] = D("赤紙召喚", charge, normal,
                    "ボス・特殊を除く攻撃カードを一時的に2枚生成", Cost(red: 1, yellow: 1), 0,
                    "ボス・特殊を除く攻撃カードを一時的に2枚生成", Cost(red: 1, yellow: 1), 5, true, false),
                [CardKind.ShiftingShadow] = D("揺らぐ影", charge, normal,
                    "敵全体に弱化6を付与", Cost(yellow: 3), 3,
                    "敵全体に弱化12を付与", Cost(yellow: 2), 2),
                [CardKind.MagicMirror] = D("マジックミラー", charge, boss,
                    "シールドの半分を消費し、その数だけチャージを獲得", Cost(blue: 2, yellow: 2), 5,
                    "シールドの半分を消費し、その数だけチャージを獲得", Cost(blue: 1, yellow: 1), 3),
                [CardKind.DuplicateAttack] = D("複製攻撃", charge, rare,
                    "次の攻撃カードを合計2回発動", Cost(red: 1, yellow: 2), 3,
                    "次の攻撃カードを合計3回発動", Cost(red: 1, yellow: 1), 2),

                [CardKind.Persistent] = D("持続カード", persistent, normal,
                    "攻撃カード3枚ごとに攻撃力+1", Cost(red: 1, purple: 1), 0,
                    "攻撃カード2枚ごとに攻撃力+2", Cost(purple: 1), 0),
                [CardKind.GuardCyclePersistent] = D("守護循環", persistent, rare,
                    "カード3枚ごとに自動防御+2", Cost(blue: 1, purple: 2), 0,
                    "カード2枚ごとに自動防御+4", Cost(purple: 1), 0),
                [CardKind.DefensePersistence] = D("防御持続", persistent, boss,
                    "シールドがある間、防御維持が減らない", Cost(blue: 3, purple: 1), 0,
                    "シールドがある間、防御維持が減らない", Cost(blue: 1, purple: 1), 0),
                [CardKind.CrimsonMoon] = D("赤い紅い月", persistent, boss,
                    "攻撃力+5。攻撃カードの消費エーテル数だけ対象へ出血を付与", Cost(red: 3, purple: 1), 0,
                    "攻撃力+8。攻撃カードの消費エーテル数×2の出血を対象へ付与", Cost(red: 2, purple: 1), 0),
                [CardKind.PiercingShield] = D("貫盾", persistent, rare,
                    "貫通時、対象のシールド半分をダメージへ追加", Cost(red: 1, blue: 1, purple: 1), 0,
                    "貫通時、対象のシールド半分をダメージへ追加", Cost(red: 1, purple: 1), 0),
                [CardKind.AdditionalDefense] = D("追加防御", persistent, rare,
                    "自動防御獲得時、同量のシールドも獲得", Cost(blue: 2, purple: 1), 0,
                    "自動防御獲得時、同量のシールドも獲得", Cost(blue: 1, purple: 1), 0),
                [CardKind.DefenseSupport] = D("防御補助", persistent, normal,
                    "防御カード3枚ごとに防御力+1", Cost(blue: 1, purple: 1), 0,
                    "防御カード3枚ごとに防御力+2", Cost(blue: 1, purple: 1), 0),
                [CardKind.DoubleAttackPersistent] = D("二回攻撃", persistent, normal,
                    "攻撃カード3枚ごとに、その攻撃を2回発動", Cost(red: 2, purple: 1), 0,
                    "攻撃カード2枚ごとに、その攻撃を2回発動", Cost(red: 1, purple: 1), 0),
                [CardKind.AreaAttackPersistent] = D("全体攻撃", persistent, normal,
                    "攻撃カード3枚ごとに、その攻撃を全体化", Cost(red: 2, purple: 1), 0,
                    "攻撃カード2枚ごとに、その攻撃を全体化", Cost(red: 1, purple: 1), 0),
                [CardKind.WristSupporter] = D("リストサポーター", persistent, normal,
                    "ターン終了時、使用カードが3枚以下なら攻撃力+1", Cost(red: 1, purple: 1), 0,
                    "ターン終了時、使用カードが2枚以下なら攻撃力+2", Cost(red: 1, purple: 1), 0),
                [CardKind.Tempering] = D("焼入れ", persistent, normal,
                    "敵の攻撃を受けた後に反射+1", Cost(blue: 1, purple: 1), 0,
                    "敵の攻撃を受けた後に反射+2", Cost(blue: 1, purple: 1), 0),

                [CardKind.DivineStrike] = D("神撃", CardCategory.Special, CardRarity.Special,
                    "手番エーテルをすべて消費し、最も多い色に応じた効果を発動", Cost(), 8,
                    "強化対象外", Cost(), 8, false, false, false)
            };
        }
    }

    public static class CarryToolCatalog
    {
        public static string GetName(CarryToolKind kind)
        {
            switch (kind)
            {
                case CarryToolKind.AttackBoost:
                    return "刃の欠片";
                case CarryToolKind.ShieldBoost:
                    return "守護の札";
                case CarryToolKind.FirstAttackPierce:
                    return "貫通の針";
                case CarryToolKind.SmallBag:
                    return "小さな鞄";
                case CarryToolKind.RedCrystal:
                    return "エーテル赤結晶";
                case CarryToolKind.BlueCrystal:
                    return "エーテル青結晶";
                case CarryToolKind.StarFragment:
                    return "星の欠片";
                case CarryToolKind.StarMass:
                    return "星の塊";
                case CarryToolKind.ShieldStorage:
                    return "シールド貯蔵庫";
                case CarryToolKind.VorpalHeart:
                    return "ヴォーパルハート";
                case CarryToolKind.PresentBox:
                    return "不思議なプレゼントボックス";
                case CarryToolKind.BigBag:
                    return "大きな鞄";
                case CarryToolKind.StarOrb:
                    return "星の宝玉";
                case CarryToolKind.EnergyCore:
                    return "エネルギーコア";
                case CarryToolKind.LargeBelt:
                    return "大型ベルト";
                case CarryToolKind.GuardianJudgment:
                    return "守護者の裁き";
                case CarryToolKind.VorpalDagger:
                    return "ヴォーパルダガー";
                case CarryToolKind.Myojo:
                    return "明星";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        public static string GetDescription(CarryToolKind kind)
        {
            switch (kind)
            {
                case CarryToolKind.AttackBoost:
                    return "攻撃カードのダメージが常時+1";
                case CarryToolKind.ShieldBoost:
                    return "防御カードと堅守で得るシールドが+2";
                case CarryToolKind.FirstAttackPierce:
                    return "各戦闘で最初に使う攻撃カードが敵シールドを無視";
                case CarryToolKind.SmallBag:
                    return "持ち越せるエーテルの数が+1";
                case CarryToolKind.RedCrystal:
                    return "赤3個以上のカード使用後、戦闘中の攻撃力+1";
                case CarryToolKind.BlueCrystal:
                    return "青3個以上のカード使用後、戦闘中の防御力+1";
                case CarryToolKind.StarFragment:
                    return "ターン開始時、チャージがなければチャージ+6";
                case CarryToolKind.StarMass:
                    return "ターン開始時、チャージがなければチャージ+10";
                case CarryToolKind.ShieldStorage:
                    return "ターン開始時に消えるシールドを半分残す";
                case CarryToolKind.VorpalHeart:
                    return "チャージ使用後、その半分を残す";
                case CarryToolKind.PresentBox:
                    return "通常戦闘のカード報酬を追加でもう1回取得";
                case CarryToolKind.BigBag:
                    return "持ち越せるエーテルの数が+2";
                case CarryToolKind.StarOrb:
                    return "ターン開始時、チャージがなければチャージ+18";
                case CarryToolKind.EnergyCore:
                    return "毎ターン取得するエーテルが+1";
                case CarryToolKind.LargeBelt:
                    return "戦闘開始時、持続カード1枚をランダムで無料発動";
                case CarryToolKind.GuardianJudgment:
                    return "シールド獲得時、総量の20%をランダムな敵へ与える";
                case CarryToolKind.VorpalDagger:
                    return "敵単体カードでは攻撃力を総エーテルコスト倍で適用";
                case CarryToolKind.Myojo:
                    return "自ターン開始時、1個につきチャージ+50";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        public static CarryToolRarity GetRarity(CarryToolKind kind)
        {
            switch (kind)
            {
                case CarryToolKind.StarMass:
                case CarryToolKind.ShieldStorage:
                case CarryToolKind.VorpalHeart:
                case CarryToolKind.PresentBox:
                    return CarryToolRarity.Rare;
                case CarryToolKind.BigBag:
                case CarryToolKind.StarOrb:
                case CarryToolKind.EnergyCore:
                case CarryToolKind.LargeBelt:
                case CarryToolKind.GuardianJudgment:
                case CarryToolKind.VorpalDagger:
                    return CarryToolRarity.Boss;
                case CarryToolKind.Myojo:
                    return CarryToolRarity.Special;
                default:
                    return CarryToolRarity.Normal;
            }
        }

        public static int GetMaxCount(CarryToolKind kind)
        {
            if (kind == CarryToolKind.BigBag)
            {
                return 2;
            }

            return kind == CarryToolKind.Myojo ? int.MaxValue : 1;
        }

        public static string GetRarityLabel(CarryToolKind kind)
        {
            switch (GetRarity(kind))
            {
                case CarryToolRarity.Normal:
                    return "通常";
                case CarryToolRarity.Rare:
                    return "レア";
                case CarryToolRarity.Boss:
                    return "ボス";
                case CarryToolRarity.Special:
                    return "特殊";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public sealed class PlayerState
    {
        private int _nextCardId;

        public PlayerState()
        {
            Reset();
        }

        public int MaxHp { get; private set; }
        public int Hp { get; set; }
        public int Shield { get; set; }
        public int Gold { get; set; }
        public List<CardInstance> Deck { get; } = new List<CardInstance>();
        public IReadOnlyDictionary<CarryToolKind, int> Tools => _toolCounts;

        private readonly Dictionary<CarryToolKind, int> _toolCounts =
            new Dictionary<CarryToolKind, int>();

        public void Reset()
        {
            MaxHp = 100;
            Hp = MaxHp;
            Shield = 0;
            Gold = 0;
            _toolCounts.Clear();
            Deck.Clear();
            _nextCardId = 0;

            AddCard(CardKind.Attack);
            AddCard(CardKind.Attack);
            AddCard(CardKind.Attack);
            AddCard(CardKind.Defense);
            AddCard(CardKind.Defense);
            AddCard(CardKind.Defense);
            AddCard(CardKind.Charge);
        }

        public bool CanAddCard(CardKind kind)
        {
            return !CardCatalog.IsPersistent(kind) ||
                   Deck.All(card => card.Kind != kind);
        }

        public CardInstance AddCard(
            CardKind kind,
            bool isTemporary = false)
        {
            if (!CanAddCard(kind))
            {
                throw new InvalidOperationException(
                    $"{CardCatalog.GetName(kind)}はすでに所持しています。");
            }

            var card = new CardInstance(_nextCardId++, kind);
            card.IsTemporary = isTemporary;
            var category = CardCatalog.GetCategory(kind);
            var insertionIndex =
                Deck.FindLastIndex(
                    existing => CardCatalog.GetCategory(existing.Kind) <= category) + 1;
            Deck.Insert(insertionIndex, card);
            return card;
        }

        public void RemoveTemporaryCards()
        {
            Deck.RemoveAll(card => card.IsTemporary);
        }

        public int GetToolCount(CarryToolKind kind)
        {
            return _toolCounts.TryGetValue(kind, out var count) ? count : 0;
        }

        public bool HasTool(CarryToolKind kind)
        {
            return GetToolCount(kind) > 0;
        }

        public bool CanAddTool(CarryToolKind kind)
        {
            return GetToolCount(kind) < CarryToolCatalog.GetMaxCount(kind);
        }

        public void AddTool(CarryToolKind kind)
        {
            if (!CanAddTool(kind))
            {
                throw new InvalidOperationException(
                    $"{CarryToolCatalog.GetName(kind)}はこれ以上所持できません。");
            }

            _toolCounts[kind] = GetToolCount(kind) + 1;
            ResolveSpecialAcquisitions();
        }

        public int GetCarryLimit()
        {
            return 2 +
                   GetToolCount(CarryToolKind.SmallBag) +
                   GetToolCount(CarryToolKind.BigBag) * 2;
        }

        public int GetEtherDrawCount()
        {
            return 5 + GetToolCount(CarryToolKind.EnergyCore);
        }

        public int GetTurnStartCharge()
        {
            if (HasTool(CarryToolKind.StarOrb))
            {
                return 18;
            }

            if (HasTool(CarryToolKind.StarMass))
            {
                return 10;
            }

            return HasTool(CarryToolKind.StarFragment) ? 6 : 0;
        }

        private void ResolveSpecialAcquisitions()
        {
            if (HasTool(CarryToolKind.SmallBag) &&
                GetToolCount(CarryToolKind.BigBag) >= 2 &&
                HasTool(CarryToolKind.EnergyCore) &&
                Deck.All(card => card.Kind != CardKind.DivineStrike))
            {
                AddCard(CardKind.DivineStrike);
            }

            while (HasTool(CarryToolKind.StarFragment) &&
                   HasTool(CarryToolKind.StarMass) &&
                   HasTool(CarryToolKind.StarOrb))
            {
                RemoveTool(CarryToolKind.StarFragment);
                RemoveTool(CarryToolKind.StarMass);
                RemoveTool(CarryToolKind.StarOrb);
                _toolCounts[CarryToolKind.Myojo] =
                    GetToolCount(CarryToolKind.Myojo) + 1;
            }
        }

        private void RemoveTool(CarryToolKind kind)
        {
            var count = GetToolCount(kind);
            if (count <= 1)
            {
                _toolCounts.Remove(kind);
            }
            else
            {
                _toolCounts[kind] = count - 1;
            }
        }

        public void IncreaseMaxHpAndHealToFull(int amount)
        {
            IncreaseMaxHp(amount);
            Hp = MaxHp;
        }

        public void IncreaseMaxHp(int amount)
        {
            MaxHp += amount;
        }

        public int Heal(int amount)
        {
            var previous = Hp;
            Hp = Math.Min(MaxHp, Hp + Math.Max(0, amount));
            return Hp - previous;
        }

        public int LoseHp(int amount)
        {
            var previous = Hp;
            Hp = Math.Max(0, Hp - Math.Max(0, amount));
            return previous - Hp;
        }

        public int ReceiveDamage(int damage)
        {
            var remainingDamage = Math.Max(0, damage);
            var absorbed = Math.Min(Shield, remainingDamage);
            Shield -= absorbed;
            remainingDamage -= absorbed;
            Hp = Math.Max(0, Hp - remainingDamage);
            return remainingDamage;
        }

    }

    public sealed class EnemyState
    {
        private readonly EnemyActionKind[] _pattern;

        public EnemyState(
            string name,
            int maxHp,
            int attack,
            int defense,
            params EnemyActionKind[] pattern)
        {
            Name = name;
            MaxHp = maxHp;
            Hp = maxHp;
            Attack = attack;
            Defense = defense;
            _pattern = pattern != null && pattern.Length > 0
                ? pattern
                : new[] { EnemyActionKind.Attack };
        }

        public string Name { get; }
        public int MaxHp { get; }
        public int Hp { get; private set; }
        public int Shield { get; set; }
        public int Attack { get; }
        public int Defense { get; }
        public int ActionIndex { get; private set; }
        public bool IsAlive => Hp > 0;
        public EnemyActionKind NextAction => _pattern[ActionIndex % _pattern.Length];
        public int Fire { get; set; }
        public int Bleed { get; set; }
        public int Weakness { get; set; }

        public int ReceiveDamage(int damage)
        {
            var remainingDamage = Math.Max(0, damage);
            var absorbed = Math.Min(Shield, remainingDamage);
            Shield -= absorbed;
            remainingDamage -= absorbed;
            Hp = Math.Max(0, Hp - remainingDamage);
            return remainingDamage;
        }

        public int ReceiveDamageIgnoringShield(int damage)
        {
            var appliedDamage = Math.Max(0, damage);
            Hp = Math.Max(0, Hp - appliedDamage);
            return appliedDamage;
        }

        public EnemyActionKind AdvanceAction()
        {
            var action = NextAction;
            ActionIndex++;
            return action;
        }
    }

    public sealed class MapNode
    {
        public MapNode(
            int id,
            string name,
            StageKind kind,
            int depth,
            float x,
            float y,
            params int[] neighbors)
        {
            Id = id;
            Name = name;
            Kind = kind;
            Depth = depth;
            X = x;
            Y = y;
            Neighbors = neighbors ?? Array.Empty<int>();
        }

        public int Id { get; }
        public string Name { get; }
        public StageKind Kind { get; }
        public int Depth { get; }
        public float X { get; }
        public float Y { get; }
        public int[] Neighbors { get; }
        public bool Visited { get; set; }
        public bool Cleared { get; set; }
    }
}
