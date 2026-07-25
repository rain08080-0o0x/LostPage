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

        private Dictionary<int, MapNode> _nodes;

        public RunSession(int randomSeed = 0)
        {
            Random = randomSeed == 0 ? new Random() : new Random(randomSeed);
            Player = new PlayerState();
            CurrentLayer = 1;
            _nodes = CreateMap(CurrentLayer).ToDictionary(node => node.Id);
            CurrentNodeId = 0;
            _nodes[0].Visited = true;
            _nodes[0].Cleared = true;
        }

        public PlayerState Player { get; }
        public Random Random { get; }
        public int CurrentLayer { get; private set; }
        public int CurrentNodeId { get; private set; }
        public IReadOnlyCollection<MapNode> Nodes => _nodes.Values;
        public MapNode CurrentNode => _nodes[CurrentNodeId];
        public bool HasNextLayer => CurrentLayer < MaxLayer;
        public float MapContentHeight => _nodes.Values.Max(node => node.Y) + 200f;

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

            CurrentNodeId = nodeId;
            var firstVisit = !_nodes[nodeId].Visited;
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

        public void CompleteCurrentBattle()
        {
            if (CurrentNode.Kind == StageKind.Boss &&
                !CurrentNode.Cleared)
            {
                Player.IncreaseMaxHpAndHealToFull(10);
            }

            CurrentNode.Cleared = true;
            if (CurrentNode.Kind == StageKind.Battle)
            {
                Player.Gold += 10;
            }
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
            const int price = 20;
            if (Player.Gold < price)
            {
                return false;
            }

            Player.Gold -= price;
            Player.AddCard(kind);
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

        private static IEnumerable<MapNode> CreateMap(int layer)
        {
            const float leftX = 540f;
            const float rightX = 1060f;
            const float startY = 100f;
            const float rowSpacing = 230f;

            var rowCount = 4 + (layer - 1);
            var bossId = rowCount * 2 + 1;
            yield return new MapNode(
                0,
                $"{layer}層開始地点",
                StageKind.Start,
                800f,
                startY,
                1,
                2);

            var utilityRow = rowCount / 2;
            var specialRow = utilityRow - 1;
            var battleNumber = 0;
            for (var row = 0; row < rowCount; row++)
            {
                for (var column = 0; column < 2; column++)
                {
                    var id = 1 + row * 2 + column;
                    StageKind kind;
                    string name;
                    if (row == utilityRow && column == 0)
                    {
                        kind = StageKind.Fountain;
                        name = "回復の泉";
                    }
                    else if (row == utilityRow && column == 1)
                    {
                        kind = StageKind.Shop;
                        name = "商人";
                    }
                    else if (row == specialRow && column == 0)
                    {
                        kind = StageKind.RandomEvent;
                        name = "何もない場所";
                    }
                    else if (row == specialRow && column == 1)
                    {
                        kind = StageKind.Reward;
                        name = "報酬の間";
                    }
                    else
                    {
                        kind = StageKind.Battle;
                        battleNumber++;
                        name = $"第{battleNumber}戦闘";
                    }

                    var neighbors = new List<int>();
                    if (row == 0)
                    {
                        neighbors.Add(0);
                    }
                    else
                    {
                        neighbors.Add(1 + (row - 1) * 2);
                        neighbors.Add(2 + (row - 1) * 2);
                    }

                    if (row == rowCount - 1)
                    {
                        neighbors.Add(bossId);
                    }
                    else
                    {
                        neighbors.Add(1 + (row + 1) * 2);
                        neighbors.Add(2 + (row + 1) * 2);
                    }

                    yield return new MapNode(
                        id,
                        name,
                        kind,
                        column == 0 ? leftX : rightX,
                        startY + (row + 1) * rowSpacing,
                        neighbors.ToArray());
                }
            }

            yield return new MapNode(
                bossId,
                $"{layer}層ボス",
                StageKind.Boss,
                800f,
                startY + (rowCount + 1) * rowSpacing,
                bossId - 2,
                bossId - 1);
        }

        private IEnumerable<EnemyState> CreateEnemies()
        {
            if (CurrentNode.Kind == StageKind.Boss)
            {
                return new[]
                {
                    CreateScaledEnemy(
                        "ロストページの主",
                        80,
                        12,
                        10,
                        EnemyActionKind.Attack,
                        EnemyActionKind.Defense,
                        EnemyActionKind.Weaken,
                        EnemyActionKind.Attack)
                };
            }

            var battleNodes = _nodes.Values
                .Where(node => node.Kind == StageKind.Battle)
                .OrderBy(node => node.Y)
                .ThenBy(node => node.Id)
                .ToList();
            var encounterIndex = battleNodes.FindIndex(
                node => node.Id == CurrentNodeId);
            switch (encounterIndex % 4)
            {
                case 0:
                    return new[]
                    {
                        CreateScaledEnemy(
                            "紙喰らい",
                            30,
                            7,
                            6,
                            EnemyActionKind.Attack,
                            EnemyActionKind.Defense,
                            EnemyActionKind.Weaken)
                    };
                case 1:
                    return new[]
                    {
                        CreateScaledEnemy(
                            "墨の影A",
                            30,
                            6,
                            5,
                            EnemyActionKind.Attack,
                            EnemyActionKind.Weaken,
                            EnemyActionKind.Defense),
                        CreateScaledEnemy(
                            "墨の影B",
                            30,
                            6,
                            5,
                            EnemyActionKind.Defense,
                            EnemyActionKind.Attack,
                            EnemyActionKind.Weaken)
                    };
                case 2:
                    return new[]
                    {
                        CreateScaledEnemy(
                            "失稿の番人",
                            50,
                            9,
                            8,
                            EnemyActionKind.Weaken,
                            EnemyActionKind.Attack,
                            EnemyActionKind.Defense,
                            EnemyActionKind.Attack)
                    };
                case 3:
                    return new[]
                    {
                        CreateScaledEnemy(
                            "綴じ糸の獣",
                            45,
                            8,
                            7,
                            EnemyActionKind.Attack,
                            EnemyActionKind.Attack,
                            EnemyActionKind.Defense),
                        CreateScaledEnemy(
                            "破れた騎士",
                            55,
                            9,
                            8,
                            EnemyActionKind.Defense,
                            EnemyActionKind.Weaken,
                            EnemyActionKind.Attack)
                    };
                default:
                    throw new InvalidOperationException(
                        "戦闘ステージの編成を決定できません。");
            }
        }

        private EnemyState CreateScaledEnemy(
            string name,
            int baseHp,
            int baseAttack,
            int defense,
            params EnemyActionKind[] pattern)
        {
            var hpPercent = 100 + (CurrentLayer - 1) * 20;
            var maxHp = (baseHp * hpPercent + 99) / 100;
            var attackBonus = (CurrentLayer - 1) / 2;
            return new EnemyState(
                name,
                maxHp,
                baseAttack + attackBonus,
                defense,
                pattern);
        }
    }
}
