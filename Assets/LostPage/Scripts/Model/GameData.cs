using System;
using System.Collections.Generic;

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
        Defense,
        StrongDefense,
        AutoDefense,
        GuardContinuance,
        MirrorShield,
        Charge,
        Resonance,
        HealCharge,
        EtherConversion,
        Persistent,
        GuardCyclePersistent
    }

    public enum CardCategory
    {
        Attack,
        Defense,
        Charge,
        Persistent
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
        VorpalDagger
    }

    public enum CarryToolRarity
    {
        Normal,
        Rare,
        Boss
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
    }

    public static class CardCatalog
    {
        public static string GetName(CardKind kind)
        {
            switch (kind)
            {
                case CardKind.Attack:
                    return "攻撃カード";
                case CardKind.HeavyAttack:
                    return "強撃";
                case CardKind.AreaAttack:
                    return "薙ぎ払い";
                case CardKind.GrowthAttack:
                    return "成長撃";
                case CardKind.RandomBarrage:
                    return "乱撃";
                case CardKind.RedPulseAttack:
                    return "紅脈撃";
                case CardKind.PiercingAreaAttack:
                    return "破界撃";
                case CardKind.Defense:
                    return "防御カード";
                case CardKind.StrongDefense:
                    return "堅守";
                case CardKind.AutoDefense:
                    return "自動障壁";
                case CardKind.GuardContinuance:
                    return "守勢継続";
                case CardKind.MirrorShield:
                    return "鏡盾";
                case CardKind.Charge:
                    return "チャージカード";
                case CardKind.Resonance:
                    return "共鳴";
                case CardKind.HealCharge:
                    return "治癒";
                case CardKind.EtherConversion:
                    return "転換";
                case CardKind.Persistent:
                    return "持続カード";
                case CardKind.GuardCyclePersistent:
                    return "守護循環";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        public static string GetShortDescription(CardKind kind)
        {
            switch (kind)
            {
                case CardKind.Attack:
                    return "敵単体に5ダメージ";
                case CardKind.HeavyAttack:
                    return "敵単体に17ダメージ";
                case CardKind.AreaAttack:
                    return "敵全体に9ダメージ";
                case CardKind.GrowthAttack:
                    return "敵単体に8ダメージ。使用後、このカードの攻撃力+3";
                case CardKind.RandomBarrage:
                    return "ランダムな敵へ10ダメージを4回";
                case CardKind.RedPulseAttack:
                    return "赤エーテル総数+10ダメージ";
                case CardKind.PiercingAreaAttack:
                    return "敵全体に36貫通ダメージ";
                case CardKind.Defense:
                    return "自分に7シールド";
                case CardKind.StrongDefense:
                    return "自分に12シールド";
                case CardKind.AutoDefense:
                    return "自動防御を3層獲得";
                case CardKind.GuardContinuance:
                    return "自動防御と防御維持を2ずつ獲得";
                case CardKind.MirrorShield:
                    return "現在のシールドを2倍にし、防御維持を2獲得";
                case CardKind.Charge:
                    return "次の攻撃・防御を+3";
                case CardKind.Resonance:
                    return "赤エーテル総数だけ次の攻撃・防御を強化";
                case CardKind.HealCharge:
                    return "HPを6回復";
                case CardKind.EtherConversion:
                    return "手番エーテルをランダムな1色へ変換し、同色を2個追加";
                case CardKind.Persistent:
                    return "攻撃3回ごとに攻撃力+1";
                case CardKind.GuardCyclePersistent:
                    return "カード3枚ごとに自動防御+2";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        public static IReadOnlyDictionary<EtherType, int> GetCost(CardKind kind)
        {
            switch (kind)
            {
                case CardKind.Attack:
                    return new Dictionary<EtherType, int> { { EtherType.Red, 2 } };
                case CardKind.HeavyAttack:
                    return new Dictionary<EtherType, int>
                    {
                        { EtherType.Red, 2 },
                        { EtherType.Blue, 1 }
                    };
                case CardKind.AreaAttack:
                    return new Dictionary<EtherType, int> { { EtherType.Red, 3 } };
                case CardKind.GrowthAttack:
                    return new Dictionary<EtherType, int> { { EtherType.Red, 3 } };
                case CardKind.RandomBarrage:
                    return new Dictionary<EtherType, int> { { EtherType.Red, 4 } };
                case CardKind.RedPulseAttack:
                    return new Dictionary<EtherType, int>
                    {
                        { EtherType.Red, 3 },
                        { EtherType.Blue, 2 }
                    };
                case CardKind.PiercingAreaAttack:
                    return new Dictionary<EtherType, int> { { EtherType.Red, 5 } };
                case CardKind.Defense:
                    return new Dictionary<EtherType, int>
                    {
                        { EtherType.Red, 1 },
                        { EtherType.Blue, 1 }
                    };
                case CardKind.StrongDefense:
                case CardKind.AutoDefense:
                    return new Dictionary<EtherType, int> { { EtherType.Blue, 2 } };
                case CardKind.GuardContinuance:
                    return new Dictionary<EtherType, int> { { EtherType.Blue, 3 } };
                case CardKind.MirrorShield:
                    return new Dictionary<EtherType, int> { { EtherType.Blue, 4 } };
                case CardKind.Charge:
                    return new Dictionary<EtherType, int> { { EtherType.Yellow, 2 } };
                case CardKind.Resonance:
                    return new Dictionary<EtherType, int>
                    {
                        { EtherType.Red, 2 },
                        { EtherType.Yellow, 1 }
                    };
                case CardKind.HealCharge:
                case CardKind.EtherConversion:
                    return new Dictionary<EtherType, int> { { EtherType.Yellow, 3 } };
                case CardKind.Persistent:
                    return new Dictionary<EtherType, int>
                    {
                        { EtherType.Red, 1 },
                        { EtherType.Purple, 1 }
                    };
                case CardKind.GuardCyclePersistent:
                    return new Dictionary<EtherType, int>
                    {
                        { EtherType.Blue, 1 },
                        { EtherType.Purple, 2 }
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        public static string GetCostText(CardKind kind)
        {
            var parts = new List<string>();
            foreach (var pair in GetCost(kind))
            {
                parts.Add($"{GetEtherName(pair.Key)}{pair.Value}");
            }

            return string.Join("・", parts);
        }

        public static int GetCooldown(CardKind kind)
        {
            switch (kind)
            {
                case CardKind.Attack:
                case CardKind.Defense:
                    return 1;
                case CardKind.HeavyAttack:
                case CardKind.AreaAttack:
                case CardKind.StrongDefense:
                case CardKind.AutoDefense:
                case CardKind.Charge:
                    return 2;
                case CardKind.Resonance:
                    return 3;
                case CardKind.GrowthAttack:
                    return 2;
                case CardKind.RandomBarrage:
                case CardKind.RedPulseAttack:
                case CardKind.PiercingAreaAttack:
                case CardKind.MirrorShield:
                    return 4;
                case CardKind.GuardContinuance:
                case CardKind.EtherConversion:
                    return 3;
                case CardKind.HealCharge:
                    return 2;
                case CardKind.Persistent:
                case CardKind.GuardCyclePersistent:
                    return 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        public static CardCategory GetCategory(CardKind kind)
        {
            switch (kind)
            {
                case CardKind.Attack:
                case CardKind.HeavyAttack:
                case CardKind.AreaAttack:
                case CardKind.GrowthAttack:
                case CardKind.RandomBarrage:
                case CardKind.RedPulseAttack:
                case CardKind.PiercingAreaAttack:
                    return CardCategory.Attack;
                case CardKind.Defense:
                case CardKind.StrongDefense:
                case CardKind.AutoDefense:
                case CardKind.GuardContinuance:
                case CardKind.MirrorShield:
                    return CardCategory.Defense;
                case CardKind.Charge:
                case CardKind.Resonance:
                case CardKind.HealCharge:
                case CardKind.EtherConversion:
                    return CardCategory.Charge;
                case CardKind.Persistent:
                case CardKind.GuardCyclePersistent:
                    return CardCategory.Persistent;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        public static bool IsAttack(CardKind kind)
        {
            return GetCategory(kind) == CardCategory.Attack;
        }

        public static bool IsSingleTargetAttack(CardKind kind)
        {
            return kind == CardKind.Attack ||
                   kind == CardKind.HeavyAttack ||
                   kind == CardKind.GrowthAttack ||
                   kind == CardKind.RedPulseAttack;
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
                default:
                    return CarryToolRarity.Normal;
            }
        }

        public static int GetMaxCount(CarryToolKind kind)
        {
            return kind == CarryToolKind.BigBag ? 2 : 1;
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

        public CardInstance AddCard(CardKind kind)
        {
            var card = new CardInstance(_nextCardId++, kind);
            var category = CardCatalog.GetCategory(kind);
            var insertionIndex =
                Deck.FindLastIndex(
                    existing => CardCatalog.GetCategory(existing.Kind) <= category) + 1;
            Deck.Insert(insertionIndex, card);
            return card;
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

        public void IncreaseMaxHpAndHealToFull(int amount)
        {
            MaxHp += amount;
            Hp = MaxHp;
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
            float x,
            float y,
            params int[] neighbors)
        {
            Id = id;
            Name = name;
            Kind = kind;
            X = x;
            Y = y;
            Neighbors = neighbors ?? Array.Empty<int>();
        }

        public int Id { get; }
        public string Name { get; }
        public StageKind Kind { get; }
        public float X { get; }
        public float Y { get; }
        public int[] Neighbors { get; }
        public bool Visited { get; set; }
        public bool Cleared { get; set; }
    }
}
