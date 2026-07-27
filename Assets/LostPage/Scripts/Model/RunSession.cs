using System;
using System.Collections.Generic;
using System.Linq;

namespace LostPage
{
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
            FogDepth++;
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

        public int CompleteCurrentBattle(int defeatedEnemyCount)
        {
            if (CurrentNode.Kind == StageKind.Boss &&
                !CurrentNode.Cleared)
            {
                Player.IncreaseMaxHpAndHealToFull(10);
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
        }

        public IReadOnlyList<CardKind> CreateCardRewardChoices()
        {
            var candidates = Enum.GetValues(typeof(CardKind))
                .Cast<CardKind>()
                .OrderBy(_ => Random.Next())
                .Take(3)
                .ToList();
            return candidates;
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
            switch (Random.Next(3))
            {
                case 0:
                    Player.Gold += 15;
                    result = "落とし物から15ゴールドを獲得しました。";
                    break;
                case 1:
                    var healed = Player.Heal(20);
                    result =
                        $"休息できる場所を見つけ、HPを{healed}回復しました。";
                    break;
                case 2:
                    var lostHp = Player.LoseHp(10);
                    Player.Gold += 25;
                    result =
                        $"罠でHPを{lostHp}失いましたが、" +
                        "25ゴールドを獲得しました。";
                    break;
                default:
                    throw new InvalidOperationException(
                        "ランダムイベントの抽選結果が不正です。");
            }

            CurrentNode.Cleared = true;
            return result;
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

        public bool TryBuyCard(CardKind kind)
        {
            if (Player.Gold < ShopCardPrice)
            {
                return false;
            }

            Player.Gold -= ShopCardPrice;
            Player.AddCard(kind);
            ShopCardPrice += ShopCardPriceIncrease;
            return true;
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
