using System;
using System.Collections.Generic;
using System.Linq;

namespace LostPage
{
    public enum RandomEventKind
    {
        FoundGold,
        Rest,
        TrapTreasure,
        FreeUpgrade,
        BloodUpgrade
    }

    public sealed class ShopCardStock
    {
        public ShopCardStock(int id, CardKind kind)
        {
            Id = id;
            Kind = kind;
        }

        public int Id { get; }
        public CardKind Kind { get; }
        public bool Purchased { get; set; }
    }

    public sealed class RewardStageResult
    {
        public RewardStageResult(
            IReadOnlyList<CardKind> cards,
            CarryToolKind? rareTool)
        {
            Cards = cards;
            RareTool = rareTool;
        }

        public IReadOnlyList<CardKind> Cards { get; }
        public CarryToolKind? RareTool { get; }
    }

    public sealed class RunSession
    {
        public const int MaxLayer = 5;
        public const int InitialShopCardPrice = 20;
        public const int ShopCardPriceIncrease = 5;
        public const int InitialFogDepth = -5;

        private const float MapStartY = 100f;
        private const float MapRowSpacing = 230f;

        private static readonly int[] BossHpByLayer =
        {
            120,
            240,
            480,
            800,
            1200
        };

        private static readonly int[] NormalHpPercentByLayer =
        {
            100,
            150,
            250,
            400,
            700
        };

        private static readonly int[] MinimumEnemyCountByLayer =
        {
            1,
            1,
            1,
            2,
            2
        };

        private static readonly int[] MaximumEnemyCountByLayer =
        {
            2,
            2,
            3,
            3,
            3
        };

        private static readonly int[] MinimumRandomEventCountByLayer =
        {
            2,
            4,
            7,
            11,
            15
        };

        private static readonly int[] MaximumRandomEventCountByLayer =
        {
            3,
            5,
            9,
            13,
            17
        };

        private static readonly EnemyTemplate[] EnemyTemplates =
        {
            new EnemyTemplate(
                "紙喰らい",
                30,
                7,
                6,
                EnemyActionKind.Attack,
                EnemyActionKind.Defense,
                EnemyActionKind.Weaken),
            new EnemyTemplate(
                "墨の影A",
                30,
                6,
                5,
                EnemyActionKind.Attack,
                EnemyActionKind.Weaken,
                EnemyActionKind.Defense),
            new EnemyTemplate(
                "墨の影B",
                30,
                6,
                5,
                EnemyActionKind.Defense,
                EnemyActionKind.Attack,
                EnemyActionKind.Weaken),
            new EnemyTemplate(
                "失稿の番人",
                50,
                9,
                8,
                EnemyActionKind.Weaken,
                EnemyActionKind.Attack,
                EnemyActionKind.Defense,
                EnemyActionKind.Attack),
            new EnemyTemplate(
                "綴じ糸の獣",
                45,
                8,
                7,
                EnemyActionKind.Attack,
                EnemyActionKind.Attack,
                EnemyActionKind.Defense),
            new EnemyTemplate(
                "破れた騎士",
                55,
                9,
                8,
                EnemyActionKind.Defense,
                EnemyActionKind.Weaken,
                EnemyActionKind.Attack)
        };

        private Dictionary<int, MapNode> _nodes;
        private readonly Dictionary<int, List<ShopCardStock>> _shopStocks =
            new Dictionary<int, List<ShopCardStock>>();
        private readonly Dictionary<int, HashSet<CardCategory>>
            _shopUpgradedCategories =
                new Dictionary<int, HashSet<CardCategory>>();
        private readonly Dictionary<int, RandomEventKind> _randomEvents =
            new Dictionary<int, RandomEventKind>();
        private readonly List<RandomEventKind> _remainingRandomEventKinds =
            new List<RandomEventKind>();
        private RandomEventKind? _lastRandomEventKind;
        private int _freeEventUpgrades;
        private int _pendingBloodEventUpgrades;

        private sealed class EnemyTemplate
        {
            public EnemyTemplate(
                string name,
                int baseHp,
                int baseAttack,
                int defense,
                params EnemyActionKind[] pattern)
            {
                Name = name;
                BaseHp = baseHp;
                BaseAttack = baseAttack;
                Defense = defense;
                Pattern = pattern;
            }

            public string Name { get; }
            public int BaseHp { get; }
            public int BaseAttack { get; }
            public int Defense { get; }
            public EnemyActionKind[] Pattern { get; }
        }

        public RunSession(int randomSeed = 0)
        {
            Random = randomSeed == 0 ? new Random() : new Random(randomSeed);
            Player = new PlayerState();
            CurrentLayer = 1;
            _nodes = CreateMap(CurrentLayer).ToDictionary(node => node.Id);
            CurrentNodeId = 0;
            _nodes[0].Visited = true;
            _nodes[0].Cleared = true;
            FogDepth = InitialFogDepth;
        }

        public PlayerState Player { get; }
        public Random Random { get; }
        public int CurrentLayer { get; private set; }
        public int CurrentNodeId { get; private set; }
        public int ShopCardPrice { get; private set; } = InitialShopCardPrice;
        public int LastBattleGoldReward { get; private set; }
        public int MapMoveCount { get; private set; }
        public int FogDepth { get; private set; }
        public int LastTravelFogDamage { get; private set; }
        public IReadOnlyCollection<MapNode> Nodes => _nodes.Values;
        public MapNode CurrentNode => _nodes[CurrentNodeId];
        public bool HasNextLayer => CurrentLayer < MaxLayer;
        public float MapContentWidth => _nodes.Values.Max(node => node.X) + 300f;
        public float MapContentHeight => _nodes.Values.Max(node => node.Y) + 200f;
        public int CurrentFogDamage => 5 + (CurrentLayer - 1) * 2;
        public int MaximumFogDepth =>
            _nodes.Values.Single(node => node.Kind == StageKind.Boss).Depth - 1;
        public int ShopUpgradePrice => 30 + (CurrentLayer - 1) * 5;
        public int PendingBloodEventUpgrades =>
            _pendingBloodEventUpgrades;
        public int FreeEventUpgrades => _freeEventUpgrades;
        public float FogBoundaryY =>
            MapStartY + FogDepth * MapRowSpacing;

        public bool IsNodeInFog(MapNode node)
        {
            return node.Depth <= FogDepth;
        }

        public bool CanTravelTo(int nodeId)
        {
            return _nodes.ContainsKey(nodeId) &&
                   CurrentNode.Neighbors.Contains(nodeId);
        }

        public bool TravelTo(int nodeId)
        {
            if (!CanTravelTo(nodeId))
            {
                throw new InvalidOperationException("接続されていないステージへは移動できません。");
            }

            var firstVisit = !_nodes[nodeId].Visited;
            MapMoveCount++;
            FogDepth = Math.Min(FogDepth + 1, MaximumFogDepth);
            CurrentNodeId = nodeId;
            LastTravelFogDamage = IsNodeInFog(CurrentNode)
                ? Player.LoseHp(CurrentFogDamage)
                : 0;
            _nodes[nodeId].Visited = true;
            return firstVisit;
        }

        public BattleModel CreateBattleForCurrentNode()
        {
            if (CurrentNode.Kind != StageKind.Battle &&
                CurrentNode.Kind != StageKind.Boss)
            {
                throw new InvalidOperationException("現在地は戦闘ステージではありません。");
            }

            return new BattleModel(
                Player,
                CreateEnemies(),
                Random);
        }

        internal BattleModel CreateTutorialBattleForCurrentNode()
        {
            if (CurrentNode.Kind != StageKind.Battle)
            {
                throw new InvalidOperationException(
                    "チュートリアル戦闘は通常戦闘ステージで開始してください。");
            }

            var enemies = new[]
            {
                new EnemyState(
                    "紙喰らい",
                    20,
                    7,
                    6,
                    EnemyActionKind.Attack,
                    EnemyActionKind.Defense,
                    EnemyActionKind.Weaken),
                new EnemyState(
                    "墨の影A",
                    20,
                    6,
                    5,
                    EnemyActionKind.Attack,
                    EnemyActionKind.Weaken,
                    EnemyActionKind.Defense)
            };
            var initialDraw = new[]
            {
                EtherType.Red,
                EtherType.Red,
                EtherType.Red,
                EtherType.Blue,
                EtherType.Yellow
            };
            return new BattleModel(Player, enemies, Random, initialDraw);
        }

        public int CompleteCurrentBattle(int defeatedEnemyCount)
        {
            if (CurrentNode.Kind == StageKind.Boss &&
                !CurrentNode.Cleared)
            {
                if (HasNextLayer)
                {
                    Player.IncreaseMaxHpAndHealToFull(10);
                }
                else
                {
                    Player.IncreaseMaxHp(10);
                }
            }

            var reward = 0;
            var rewardedEnemyCount = Math.Max(0, defeatedEnemyCount);
            for (var index = 0; index < rewardedEnemyCount; index++)
            {
                reward += Random.Next(10, 16);
            }

            if (CurrentNode.Kind == StageKind.Boss)
            {
                reward += Random.Next(100, 151);
            }

            LastBattleGoldReward = reward;
            Player.Gold += reward;
            Player.RemoveTemporaryCards();
            CurrentNode.Cleared = true;
            return reward;
        }

        public void AdvanceToNextLayer()
        {
            if (CurrentNode.Kind != StageKind.Boss ||
                !CurrentNode.Cleared)
            {
                throw new InvalidOperationException(
                    "現在の層のボスを倒してから次の層へ進んでください。");
            }

            if (!HasNextLayer)
            {
                throw new InvalidOperationException("最終層をクリア済みです。");
            }

            CurrentLayer++;
            _nodes = CreateMap(CurrentLayer).ToDictionary(node => node.Id);
            CurrentNodeId = 0;
            _nodes[0].Visited = true;
            _nodes[0].Cleared = true;
            MapMoveCount = 0;
            FogDepth = InitialFogDepth;
            LastTravelFogDamage = 0;
            _shopStocks.Clear();
            _shopUpgradedCategories.Clear();
            _randomEvents.Clear();
        }

        public IReadOnlyList<CardKind> CreateCardRewardChoices()
        {
            var choices = new List<CardKind>();
            while (choices.Count < 3)
            {
                var rarity = Random.NextDouble() < 0.25
                    ? CardRarity.Rare
                    : CardRarity.Normal;
                var candidate = SelectCardCandidate(rarity, choices);
                if (!candidate.HasValue)
                {
                    candidate = SelectCardCandidate(
                        rarity == CardRarity.Rare
                            ? CardRarity.Normal
                            : CardRarity.Rare,
                        choices);
                }

                if (!candidate.HasValue)
                {
                    break;
                }

                choices.Add(candidate.Value);
            }

            return choices;
        }

        public IReadOnlyList<CardKind> CreateBossCardRewardChoices()
        {
            return CreateCardChoices(CardRarity.Boss, 3);
        }

        public IReadOnlyList<CarryToolKind> CreateStartingToolChoices()
        {
            return CreateToolChoices(CarryToolRarity.Normal, 3);
        }

        public IReadOnlyList<CarryToolKind> CreateBossToolChoices()
        {
            return CreateToolChoices(CarryToolRarity.Boss, 3);
        }

        public IReadOnlyList<CarryToolKind> CreateToolStageChoices()
        {
            if (CurrentNode.Kind != StageKind.Tool ||
                CurrentNode.Cleared)
            {
                throw new InvalidOperationException(
                    "現在地では道具を選択できません。");
            }

            return CreateToolChoices(CarryToolRarity.Normal, 3);
        }

        public void ClaimCurrentToolStageReward(CarryToolKind kind)
        {
            if (CurrentNode.Kind != StageKind.Tool ||
                CurrentNode.Cleared)
            {
                throw new InvalidOperationException(
                    "現在地では道具を受け取れません。");
            }

            ClaimTool(kind, CarryToolRarity.Normal);
            CurrentNode.Cleared = true;
        }

        public void ClaimTool(
            CarryToolKind kind,
            CarryToolRarity requiredRarity)
        {
            if (CarryToolCatalog.GetRarity(kind) != requiredRarity)
            {
                throw new InvalidOperationException(
                    "指定された報酬区分の道具ではありません。");
            }

            Player.AddTool(kind);
        }

        public string ResolveCurrentRandomEvent()
        {
            if (CurrentNode.Kind != StageKind.RandomEvent ||
                CurrentNode.Cleared)
            {
                throw new InvalidOperationException(
                    "現在地ではランダムイベントを実行できません。");
            }

            string result;
            switch (GetCurrentRandomEventKind())
            {
                case RandomEventKind.FoundGold:
                    Player.Gold += 15;
                    result = "落とし物から15ゴールドを獲得しました。";
                    break;
                case RandomEventKind.Rest:
                    var healed = Player.Heal(20);
                    result =
                        $"休息できる場所を見つけ、HPを{healed}回復しました。";
                    break;
                case RandomEventKind.TrapTreasure:
                    var lostHp = Player.LoseHp(10);
                    Player.Gold += 25;
                    result =
                        $"罠でHPを{lostHp}失いましたが、" +
                        "25ゴールドを獲得しました。";
                    break;
                case RandomEventKind.FreeUpgrade:
                case RandomEventKind.BloodUpgrade:
                    throw new InvalidOperationException(
                        "このイベントはカードを選択して解決してください。");
                default:
                    throw new InvalidOperationException(
                        "ランダムイベントの抽選結果が不正です。");
            }

            CurrentNode.Cleared = true;
            return result;
        }

        public RandomEventKind GetCurrentRandomEventKind()
        {
            if (CurrentNode.Kind != StageKind.RandomEvent ||
                CurrentNode.Cleared)
            {
                throw new InvalidOperationException(
                    "現在地ではランダムイベントを実行できません。");
            }

            if (_randomEvents.TryGetValue(
                    CurrentNodeId,
                    out var selected))
            {
                return selected;
            }

            var upgradableCount =
                Player.Deck.Count(CardCatalog.CanUpgrade);
            var eligibleKinds = new HashSet<RandomEventKind>
            {
                RandomEventKind.FoundGold,
                RandomEventKind.Rest,
                RandomEventKind.TrapTreasure
            };

            if (upgradableCount > 0)
            {
                eligibleKinds.Add(RandomEventKind.FreeUpgrade);
                if (Player.Hp > 6)
                {
                    eligibleKinds.Add(RandomEventKind.BloodUpgrade);
                }
            }

            _remainingRandomEventKinds.RemoveAll(
                kind => !eligibleKinds.Contains(kind));
            if (_remainingRandomEventKinds.Count == 0)
            {
                RefillRandomEventBag(eligibleKinds);
            }

            selected = _remainingRandomEventKinds[0];
            _remainingRandomEventKinds.RemoveAt(0);
            _lastRandomEventKind = selected;
            _randomEvents[CurrentNodeId] = selected;
            _freeEventUpgrades = 0;
            _pendingBloodEventUpgrades = 0;
            return selected;
        }

        private void RefillRandomEventBag(
            IReadOnlyCollection<RandomEventKind> eligibleKinds)
        {
            var basicKinds = new List<RandomEventKind>
            {
                RandomEventKind.FoundGold,
                RandomEventKind.Rest,
                RandomEventKind.TrapTreasure
            };
            basicKinds.RemoveAll(kind => !eligibleKinds.Contains(kind));
            Shuffle(basicKinds);

            var upgradeKinds = new List<RandomEventKind>
            {
                RandomEventKind.FreeUpgrade,
                RandomEventKind.BloodUpgrade
            };
            upgradeKinds.RemoveAll(kind => !eligibleKinds.Contains(kind));
            Shuffle(upgradeKinds);

            var gaps = Enumerable.Range(0, basicKinds.Count + 1).ToList();
            if (_lastRandomEventKind.HasValue &&
                IsUpgradeRandomEvent(_lastRandomEventKind.Value))
            {
                gaps.Remove(0);
            }

            Shuffle(gaps);
            var upgradesByGap = new Dictionary<int, RandomEventKind>();
            for (var index = 0; index < upgradeKinds.Count; index++)
            {
                upgradesByGap[gaps[index]] = upgradeKinds[index];
            }

            for (var gap = 0; gap <= basicKinds.Count; gap++)
            {
                if (upgradesByGap.TryGetValue(gap, out var upgradeKind))
                {
                    _remainingRandomEventKinds.Add(upgradeKind);
                }

                if (gap < basicKinds.Count)
                {
                    _remainingRandomEventKinds.Add(basicKinds[gap]);
                }
            }
        }

        private void Shuffle<T>(IList<T> items)
        {
            for (var index = items.Count - 1; index > 0; index--)
            {
                var swapIndex = Random.Next(index + 1);
                var value = items[index];
                items[index] = items[swapIndex];
                items[swapIndex] = value;
            }
        }

        private static bool IsUpgradeRandomEvent(RandomEventKind kind)
        {
            return kind == RandomEventKind.FreeUpgrade ||
                   kind == RandomEventKind.BloodUpgrade;
        }

        public IReadOnlyList<CardInstance> GetUpgradeableCards()
        {
            return Player.Deck
                .Where(CardCatalog.CanUpgrade)
                .ToList();
        }

        public bool TryUpgradeCardForFreeEvent(int cardId)
        {
            if (GetCurrentRandomEventKind() !=
                    RandomEventKind.FreeUpgrade ||
                _freeEventUpgrades >= 2)
            {
                return false;
            }

            var card = GetUpgradeableCard(cardId);
            if (card == null)
            {
                return false;
            }

            card.IsUpgraded = true;
            _freeEventUpgrades++;
            if (_freeEventUpgrades >= 2 ||
                !Player.Deck.Any(CardCatalog.CanUpgrade))
            {
                CurrentNode.Cleared = true;
            }

            return true;
        }

        public void FinishFreeUpgradeEvent()
        {
            if (GetCurrentRandomEventKind() !=
                    RandomEventKind.FreeUpgrade ||
                _freeEventUpgrades <= 0)
            {
                throw new InvalidOperationException(
                    "カードを1枚以上強化してください。");
            }

            CurrentNode.Cleared = true;
        }

        public bool CanChooseBloodUpgradeCount(int count)
        {
            return count >= 1 &&
                   count <= 3 &&
                   Player.Hp - count * 6 >= 1 &&
                   GetUpgradeableCards().Count >= count;
        }

        public bool StartBloodUpgradeEvent(int count)
        {
            if (GetCurrentRandomEventKind() !=
                    RandomEventKind.BloodUpgrade ||
                _pendingBloodEventUpgrades > 0 ||
                !CanChooseBloodUpgradeCount(count))
            {
                return false;
            }

            Player.LoseHp(count * 6);
            _pendingBloodEventUpgrades = count;
            return true;
        }

        public bool TryUpgradeCardForBloodEvent(int cardId)
        {
            if (_pendingBloodEventUpgrades <= 0)
            {
                return false;
            }

            var card = GetUpgradeableCard(cardId);
            if (card == null)
            {
                return false;
            }

            card.IsUpgraded = true;
            _pendingBloodEventUpgrades--;
            if (_pendingBloodEventUpgrades == 0)
            {
                CurrentNode.Cleared = true;
            }

            return true;
        }

        public RewardStageResult ClaimCurrentReward()
        {
            if (CurrentNode.Kind != StageKind.Reward ||
                CurrentNode.Cleared)
            {
                throw new InvalidOperationException(
                    "現在地では報酬を受け取れません。");
            }

            var rewards = CreateCardRewardChoices()
                .Take(2)
                .ToList();
            foreach (var kind in rewards)
            {
                Player.AddCard(kind);
            }

            CarryToolKind? rareTool = null;
            var rareCandidates = Enum.GetValues(typeof(CarryToolKind))
                .Cast<CarryToolKind>()
                .Where(
                    kind =>
                        CarryToolCatalog.GetRarity(kind) ==
                            CarryToolRarity.Rare &&
                        Player.CanAddTool(kind))
                .ToList();
            if (rareCandidates.Count > 0 && Random.NextDouble() < 0.30)
            {
                rareTool = rareCandidates[Random.Next(rareCandidates.Count)];
                Player.AddTool(rareTool.Value);
            }

            CurrentNode.Cleared = true;
            return new RewardStageResult(rewards, rareTool);
        }

        public IReadOnlyList<ShopCardStock> GetCurrentShopStock(
            CardCategory category)
        {
            EnsureCurrentShop();
            return GetOrCreateShopStock()
                .Where(stock => CardCatalog.GetCategory(stock.Kind) == category)
                .ToList();
        }

        public bool TryBuyShopCard(int stockId)
        {
            EnsureCurrentShop();
            var stock = GetOrCreateShopStock()
                .FirstOrDefault(candidate => candidate.Id == stockId);
            if (stock == null ||
                stock.Purchased ||
                !Player.CanAddCard(stock.Kind) ||
                Player.Gold < ShopCardPrice)
            {
                return false;
            }

            Player.Gold -= ShopCardPrice;
            Player.AddCard(stock.Kind);
            stock.Purchased = true;
            ShopCardPrice += ShopCardPriceIncrease;
            return true;
        }

        public bool TryBuyCard(CardKind kind)
        {
            var rarity = CardCatalog.GetRarity(kind);
            if (rarity != CardRarity.Normal &&
                rarity != CardRarity.Rare)
            {
                return false;
            }

            if (Player.Gold < ShopCardPrice)
            {
                return false;
            }

            if (!Player.CanAddCard(kind))
            {
                return false;
            }

            Player.Gold -= ShopCardPrice;
            Player.AddCard(kind);
            ShopCardPrice += ShopCardPriceIncrease;
            return true;
        }

        public bool CanUpgradeCardAtCurrentShop(CardInstance card)
        {
            EnsureCurrentShop();
            var category = card == null
                ? CardCategory.Special
                : CardCatalog.GetCategory(card.Kind);
            return card != null &&
                   category != CardCategory.Special &&
                   CardCatalog.CanUpgrade(card) &&
                   !GetShopUpgradedCategories().Contains(category) &&
                   Player.Gold >= ShopUpgradePrice;
        }

        public bool TryUpgradeCardAtCurrentShop(int cardId)
        {
            EnsureCurrentShop();
            var card = GetUpgradeableCard(cardId);
            if (!CanUpgradeCardAtCurrentShop(card))
            {
                return false;
            }

            var category = CardCatalog.GetCategory(card.Kind);
            Player.Gold -= ShopUpgradePrice;
            card.IsUpgraded = true;
            GetShopUpgradedCategories().Add(category);
            return true;
        }

        public bool HasUpgradedCategoryAtCurrentShop(
            CardCategory category)
        {
            EnsureCurrentShop();
            return GetShopUpgradedCategories().Contains(category);
        }

        private IReadOnlyList<CardKind> CreateCardChoices(
            CardRarity rarity,
            int count)
        {
            var choices = new List<CardKind>();
            while (choices.Count < count)
            {
                var candidate = SelectCardCandidate(rarity, choices);
                if (!candidate.HasValue)
                {
                    break;
                }

                choices.Add(candidate.Value);
            }

            return choices;
        }

        private CardKind? SelectCardCandidate(
            CardRarity rarity,
            IReadOnlyCollection<CardKind> excluded)
        {
            var candidates = CardCatalog.AllKinds
                .Where(kind => CardCatalog.GetRarity(kind) == rarity)
                .Where(kind => !excluded.Contains(kind))
                .Where(
                    kind =>
                        !CardCatalog.IsPersistent(kind) ||
                        Player.CanAddCard(kind))
                .ToList();
            return candidates.Count == 0
                ? (CardKind?)null
                : candidates[Random.Next(candidates.Count)];
        }

        private List<ShopCardStock> GetOrCreateShopStock()
        {
            if (_shopStocks.TryGetValue(
                    CurrentNodeId,
                    out var existing))
            {
                return existing;
            }

            var stock = new List<ShopCardStock>();
            var nextId = 0;
            foreach (var category in new[]
                     {
                         CardCategory.Attack,
                         CardCategory.Defense,
                         CardCategory.Charge,
                         CardCategory.Persistent
                     })
            {
                foreach (var rarityAndCount in new[]
                         {
                             new
                             {
                                 Rarity = CardRarity.Normal,
                                 Count = 3
                             },
                             new
                             {
                                 Rarity = CardRarity.Rare,
                                 Count = 2
                             }
                         })
                {
                    var selected = CardCatalog.AllKinds
                        .Where(
                            kind =>
                                CardCatalog.GetCategory(kind) == category &&
                                CardCatalog.GetRarity(kind) ==
                                rarityAndCount.Rarity)
                        .Where(
                            kind =>
                                !CardCatalog.IsPersistent(kind) ||
                                Player.CanAddCard(kind))
                        .OrderBy(_ => Random.Next())
                        .Take(rarityAndCount.Count);
                    foreach (var kind in selected)
                    {
                        stock.Add(new ShopCardStock(nextId++, kind));
                    }
                }
            }

            _shopStocks[CurrentNodeId] = stock;
            return stock;
        }

        private HashSet<CardCategory> GetShopUpgradedCategories()
        {
            if (!_shopUpgradedCategories.TryGetValue(
                    CurrentNodeId,
                    out var categories))
            {
                categories = new HashSet<CardCategory>();
                _shopUpgradedCategories[CurrentNodeId] = categories;
            }

            return categories;
        }

        private CardInstance GetUpgradeableCard(int cardId)
        {
            return Player.Deck.FirstOrDefault(
                card =>
                    card.Id == cardId &&
                    CardCatalog.CanUpgrade(card));
        }

        private void EnsureCurrentShop()
        {
            if (CurrentNode.Kind != StageKind.Shop)
            {
                throw new InvalidOperationException(
                    "現在地はショップではありません。");
            }
        }

        private IReadOnlyList<CarryToolKind> CreateToolChoices(
            CarryToolRarity rarity,
            int count)
        {
            return Enum.GetValues(typeof(CarryToolKind))
                .Cast<CarryToolKind>()
                .Where(
                    kind =>
                        CarryToolCatalog.GetRarity(kind) == rarity &&
                        Player.CanAddTool(kind))
                .OrderBy(_ => Random.Next())
                .Take(count)
                .ToList();
        }

        private IEnumerable<MapNode> CreateMap(int layer)
        {
            const float columnSpacing = 270f;
            const float horizontalMargin = 350f;

            var rowCount = 4 + (layer - 1);
            var intermediateNodeCount = rowCount * (rowCount + 1) / 2;
            var bossId = intermediateNodeCount + 1;
            var contentWidth =
                (rowCount - 1) * columnSpacing + horizontalMargin * 2f;
            var centerX = contentWidth * 0.5f;

            yield return new MapNode(
                0,
                $"{layer}層開始地点",
                StageKind.Start,
                0,
                centerX,
                MapStartY,
                GetRowStartId(0));

            var stageKinds = CreateIntermediateStageKinds(
                layer,
                intermediateNodeCount);
            var battleNumber = 0;
            for (var row = 0; row < rowCount; row++)
            {
                var rowWidth = row + 1;
                var rowStartId = GetRowStartId(row);
                for (var column = 0; column < rowWidth; column++)
                {
                    var id = rowStartId + column;
                    var kind = stageKinds[id - 1];
                    if (kind == StageKind.Battle)
                    {
                        battleNumber++;
                    }

                    var neighbors = new List<int>();
                    if (row == 0)
                    {
                        neighbors.Add(0);
                    }
                    else
                    {
                        var previousRowStart = GetRowStartId(row - 1);
                        if (column > 0)
                        {
                            neighbors.Add(previousRowStart + column - 1);
                        }

                        if (column < row)
                        {
                            neighbors.Add(previousRowStart + column);
                        }
                    }

                    if (column > 0)
                    {
                        neighbors.Add(id - 1);
                    }

                    if (column < rowWidth - 1)
                    {
                        neighbors.Add(id + 1);
                    }

                    if (row == rowCount - 1)
                    {
                        neighbors.Add(bossId);
                    }
                    else
                    {
                        var nextRowStart = GetRowStartId(row + 1);
                        neighbors.Add(nextRowStart + column);
                        neighbors.Add(nextRowStart + column + 1);
                    }

                    var x =
                        centerX +
                        (column - (rowWidth - 1) * 0.5f) *
                        columnSpacing;
                    yield return new MapNode(
                        id,
                        GetStageName(kind, battleNumber),
                        kind,
                        row + 1,
                        x,
                        MapStartY + (row + 1) * MapRowSpacing,
                        neighbors.Distinct().ToArray());
                }
            }

            var finalRowStartId = GetRowStartId(rowCount - 1);
            yield return new MapNode(
                bossId,
                $"{layer}層ボス",
                StageKind.Boss,
                rowCount + 1,
                centerX,
                MapStartY + (rowCount + 1) * MapRowSpacing,
                Enumerable.Range(finalRowStartId, rowCount).ToArray());
        }

        private List<StageKind> CreateIntermediateStageKinds(
            int layer,
            int nodeCount)
        {
            var kinds = new List<StageKind>
            {
                StageKind.Fountain,
                StageKind.Shop,
                StageKind.Reward,
                StageKind.Tool
            };
            var eventCount = Random.Next(
                MinimumRandomEventCountByLayer[layer - 1],
                MaximumRandomEventCountByLayer[layer - 1] + 1);
            for (var index = 0; index < eventCount; index++)
            {
                kinds.Add(StageKind.RandomEvent);
            }

            while (kinds.Count < nodeCount)
            {
                kinds.Add(StageKind.Battle);
            }

            return kinds.OrderBy(_ => Random.Next()).ToList();
        }

        private static int GetRowStartId(int row)
        {
            return 1 + row * (row + 1) / 2;
        }

        private static string GetStageName(StageKind kind, int battleNumber)
        {
            switch (kind)
            {
                case StageKind.Battle:
                    return $"第{battleNumber}戦闘";
                case StageKind.Fountain:
                    return "回復の泉";
                case StageKind.Shop:
                    return "商人";
                case StageKind.RandomEvent:
                    return "何もない場所";
                case StageKind.Reward:
                    return "報酬の間";
                case StageKind.Tool:
                    return "道具の間";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(kind),
                        kind,
                        null);
            }
        }

        private IEnumerable<EnemyState> CreateEnemies()
        {
            if (CurrentNode.Kind == StageKind.Boss)
            {
                return new[]
                {
                    new EnemyState(
                        "ロストページの主",
                        BossHpByLayer[CurrentLayer - 1],
                        12 + (CurrentLayer - 1) / 2,
                        10,
                        EnemyActionKind.Attack,
                        EnemyActionKind.Defense,
                        EnemyActionKind.Weaken,
                        EnemyActionKind.Attack)
                };
            }

            var enemyCount = Random.Next(
                MinimumEnemyCountByLayer[CurrentLayer - 1],
                MaximumEnemyCountByLayer[CurrentLayer - 1] + 1);
            return EnemyTemplates
                .OrderBy(_ => Random.Next())
                .Take(enemyCount)
                .Select(CreateScaledEnemy)
                .ToList();
        }

        private EnemyState CreateScaledEnemy(EnemyTemplate template)
        {
            var hpPercent = NormalHpPercentByLayer[CurrentLayer - 1];
            var maxHp = Math.Min(
                400,
                (template.BaseHp * hpPercent + 99) / 100);
            var attackBonus = (CurrentLayer - 1) / 2;
            return new EnemyState(
                template.Name,
                maxHp,
                template.BaseAttack + attackBonus,
                template.Defense,
                template.Pattern);
        }
    }
}
