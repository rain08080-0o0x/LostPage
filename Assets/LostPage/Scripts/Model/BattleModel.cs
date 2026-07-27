using System;
using System.Collections.Generic;
using System.Linq;

namespace LostPage
{
    public sealed class AttackHitResult
    {
        public AttackHitResult(
            int targetIndex,
            int hpBefore,
            int hpAfter,
            int shieldBefore,
            int shieldAfter,
            bool pierced)
        {
            TargetIndex = targetIndex;
            HpBefore = hpBefore;
            HpAfter = hpAfter;
            ShieldBefore = shieldBefore;
            ShieldAfter = shieldAfter;
            Pierced = pierced;
        }

        public int TargetIndex { get; }
        public int HpBefore { get; }
        public int HpAfter { get; }
        public int ShieldBefore { get; }
        public int ShieldAfter { get; }
        public bool Pierced { get; }
    }

    public sealed class BattleModel
    {
        private readonly PlayerState _player;
        private readonly Random _random;
        private readonly List<int> _lastAttackTargetIndices =
            new List<int>();
        private readonly List<AttackHitResult> _lastAttackHits =
            new List<AttackHitResult>();

        public BattleModel(
            PlayerState player,
            IEnumerable<EnemyState> enemies,
            Random random)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            Enemies = enemies?.ToList() ?? throw new ArgumentNullException(nameof(enemies));
            if (Enemies.Count == 0)
            {
                throw new ArgumentException("敵を1体以上指定してください。", nameof(enemies));
            }

            foreach (var card in _player.Deck)
            {
                card.PersistentActivated = false;
                card.CooldownRemaining = 0;
                card.GrowthBonus = 0;
            }

            _player.Shield = 0;
            PermanentAttackBonus =
                _player.HasTool(CarryToolKind.AttackBoost) ? 1 : 0;
            PenetrationStacks =
                _player.HasTool(CarryToolKind.FirstAttackPierce) ? 1 : 0;
            Pool = new EtherPool(_player.Deck, _random);
            ActivateLargeBelt();
            TurnStartMessage = BeginPlayerTurn();
        }

        public IReadOnlyList<EnemyState> Enemies { get; }
        public EtherPool Pool { get; }
        public BattlePhase Phase { get; private set; }
        public int TurnNumber { get; private set; }
        public int PendingCharge { get; private set; }
        public int PendingWeaken { get; private set; }
        public int PersistentStacks { get; private set; }
        public int AttackCardsTowardBonus { get; private set; }
        public int PermanentAttackBonus { get; private set; }
        public int DefensePowerBonus { get; private set; }
        public int PenetrationStacks { get; private set; }
        public int DefenseRetentionStacks { get; private set; }
        public int AutoDefenseStacks { get; private set; }
        public int GuardCycleStacks { get; private set; }
        public int CardsTowardAutoDefense { get; private set; }
        public bool FirstAttackPierceAvailable => PenetrationStacks > 0;
        public int TotalRedEtherCount => Pool.GetTotal(EtherType.Red);
        public string BattleStartMessage { get; private set; }
        public string TurnStartMessage { get; private set; }
        public PlayerState Player => _player;
        public IReadOnlyList<int> LastAttackTargetIndices =>
            _lastAttackTargetIndices;
        public IReadOnlyList<AttackHitResult> LastAttackHits =>
            _lastAttackHits;
        public IReadOnlyList<EtherType> LastDrawnEtherTypes
        {
            get;
            private set;
        } = Array.Empty<EtherType>();
        public IReadOnlyDictionary<EtherType, int> LastUnusedAfterDraw
        {
            get;
            private set;
        } = new Dictionary<EtherType, int>();
        public IReadOnlyDictionary<EtherType, int> LastCurrentAfterDraw
        {
            get;
            private set;
        } = new Dictionary<EtherType, int>();
        public IReadOnlyDictionary<EtherType, int> LastSpentAfterDraw
        {
            get;
            private set;
        } = new Dictionary<EtherType, int>();

        public bool HasUsableCard =>
            Phase == BattlePhase.PlayerTurn && _player.Deck.Any(CanUse);

        public bool CanUse(CardInstance card)
        {
            if (Phase != BattlePhase.PlayerTurn || card == null)
            {
                return false;
            }

            if (CardCatalog.IsPersistent(card.Kind) &&
                card.PersistentActivated)
            {
                return false;
            }

            if (card.CooldownRemaining > 0)
            {
                return false;
            }

            return Pool.CanPay(CardCatalog.GetCost(card.Kind));
        }

        public int PreviewValue(CardKind kind)
        {
            return PreviewValue(kind, 0);
        }

        public int PreviewValue(CardInstance card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            return PreviewValue(card.Kind, card.GrowthBonus);
        }

        private int PreviewValue(CardKind kind, int growthBonus)
        {
            switch (kind)
            {
                case CardKind.Attack:
                    return Math.Max(
                        0,
                        5 + GetAttackPowerContribution(kind) +
                        PendingCharge - PendingWeaken);
                case CardKind.HeavyAttack:
                    return Math.Max(
                        0,
                        17 + GetAttackPowerContribution(kind) +
                        PendingCharge - PendingWeaken);
                case CardKind.AreaAttack:
                    return Math.Max(
                        0,
                        9 + GetAttackPowerContribution(kind) +
                        PendingCharge - PendingWeaken);
                case CardKind.GrowthAttack:
                    return Math.Max(
                        0,
                        8 + growthBonus + GetAttackPowerContribution(kind) +
                        PendingCharge - PendingWeaken);
                case CardKind.RandomBarrage:
                    return Math.Max(
                        0,
                        10 + GetAttackPowerContribution(kind) +
                        PendingCharge - PendingWeaken);
                case CardKind.RedPulseAttack:
                    return Math.Max(
                        0,
                        10 + TotalRedEtherCount +
                        GetAttackPowerContribution(kind) +
                        PendingCharge - PendingWeaken);
                case CardKind.PiercingAreaAttack:
                    return Math.Max(
                        0,
                        36 + GetAttackPowerContribution(kind) +
                        PendingCharge - PendingWeaken);
                case CardKind.Defense:
                    return Math.Max(
                        0,
                        7 + GetShieldToolBonus() +
                        DefensePowerBonus + PendingCharge - PendingWeaken);
                case CardKind.StrongDefense:
                    return Math.Max(
                        0,
                        12 + GetShieldToolBonus() +
                        DefensePowerBonus + PendingCharge - PendingWeaken);
                case CardKind.AutoDefense:
                    return Math.Max(0, 3 + PendingCharge - PendingWeaken);
                case CardKind.GuardContinuance:
                    return Math.Max(0, 2 + PendingCharge - PendingWeaken);
                case CardKind.MirrorShield:
                    return _player.Shield * 2;
                case CardKind.Charge:
                    return 3;
                case CardKind.Resonance:
                    return TotalRedEtherCount;
                case CardKind.HealCharge:
                    return 6;
                case CardKind.EtherConversion:
                    return 2;
                case CardKind.Persistent:
                    return PersistentStacks + 1;
                case CardKind.GuardCyclePersistent:
                    return GuardCycleStacks + 1;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        public string UseCard(CardInstance card, int targetIndex)
        {
            if (!CanUse(card))
            {
                throw new InvalidOperationException("このカードは現在使用できません。");
            }

            var cost = CardCatalog.GetCost(card.Kind);
            var guardCycleStacksBeforeUse = GuardCycleStacks;
            _lastAttackTargetIndices.Clear();
            _lastAttackHits.Clear();
            Pool.Pay(cost);

            string message;
            switch (card.Kind)
            {
                case CardKind.Attack:
                case CardKind.HeavyAttack:
                case CardKind.RedPulseAttack:
                    message = UseAttack(card, targetIndex);
                    break;
                case CardKind.AreaAttack:
                    message = UseAreaAttack(CardKind.AreaAttack, false);
                    break;
                case CardKind.GrowthAttack:
                    message = UseAttack(card, targetIndex);
                    card.GrowthBonus += 3;
                    message += $" 次回の基礎ダメージは{8 + card.GrowthBonus}。";
                    break;
                case CardKind.RandomBarrage:
                    message = UseRandomBarrage();
                    break;
                case CardKind.PiercingAreaAttack:
                    message = UseAreaAttack(
                        CardKind.PiercingAreaAttack,
                        true);
                    break;
                case CardKind.Defense:
                case CardKind.StrongDefense:
                    message = UseDefense(card.Kind);
                    break;
                case CardKind.AutoDefense:
                    message = UseAutoDefense();
                    break;
                case CardKind.GuardContinuance:
                    message = UseGuardContinuance();
                    break;
                case CardKind.MirrorShield:
                    message = UseMirrorShield();
                    break;
                case CardKind.Charge:
                    PendingCharge += 3;
                    message =
                        $"チャージ+3。次の攻撃・防御が合計+{PendingCharge}。";
                    break;
                case CardKind.Resonance:
                    var resonance = TotalRedEtherCount;
                    PendingCharge += resonance;
                    message =
                        $"共鳴+{resonance}。" +
                        $"次の攻撃・防御が合計+{PendingCharge}。";
                    break;
                case CardKind.HealCharge:
                    var healed = _player.Heal(6);
                    message = $"HPを{healed}回復。";
                    break;
                case CardKind.EtherConversion:
                    var convertedType =
                        Pool.ConvertCurrentToRandomTypeAndAdd(2);
                    message =
                        $"手番エーテルを{CardCatalog.GetEtherName(convertedType)}へ変換し、" +
                        $"{CardCatalog.GetEtherName(convertedType)}を2個追加。";
                    break;
                case CardKind.Persistent:
                case CardKind.GuardCyclePersistent:
                    message = ActivatePersistent(card);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var crystalMessage = ApplyCrystalTools(cost);
            var cycleMessage =
                RegisterCardUseForGuardCycle(guardCycleStacksBeforeUse);
            card.CooldownRemaining = CardCatalog.GetCooldown(card.Kind);
            return $"{message}{crystalMessage}{cycleMessage}".Trim();
        }

        public string EndPlayerTurn(IReadOnlyList<EtherType> carried)
        {
            if (Phase != BattlePhase.PlayerTurn)
            {
                throw new InvalidOperationException("プレイヤーターンではありません。");
            }

            Pool.EndTurn(carried, _player.GetCarryLimit());
            Phase = BattlePhase.EnemyTurn;
            var messages = ExecuteEnemyTurn();

            if (_player.Hp <= 0)
            {
                Phase = BattlePhase.Defeat;
                messages.Add("プレイヤーのHPが0になりました。");
                return string.Join("\n", messages);
            }

            TurnStartMessage = BeginPlayerTurn();
            if (!string.IsNullOrEmpty(TurnStartMessage))
            {
                messages.Add(TurnStartMessage);
            }

            messages.Add(
                $"ターン{TurnNumber}：エーテルを" +
                $"{_player.GetEtherDrawCount()}個取得。");
            return string.Join("\n", messages);
        }

        private string UseAttack(CardInstance card, int targetIndex)
        {
            if (targetIndex < 0 ||
                targetIndex >= Enemies.Count ||
                !Enemies[targetIndex].IsAlive)
            {
                throw new InvalidOperationException("攻撃対象を選択してください。");
            }

            var damage = PreviewValue(card);
            ConsumeOneShotModifiers();
            var target = Enemies[targetIndex];
            var pierced = ConsumePenetration();
            ApplyAttackHit(targetIndex, damage, pierced);

            var bonusMessage = RegisterAttackCardUse();
            UpdateVictoryState();
            var pierceMessage = pierced ? "シールドを貫通。" : string.Empty;
            return
                $"{target.Name}に{damage}ダメージ。" +
                $"{pierceMessage}{bonusMessage}".Trim();
        }

        private string UseAreaAttack(
            CardKind kind,
            bool innatePiercing)
        {
            var damage = PreviewValue(kind);
            ConsumeOneShotModifiers();
            var pierced = innatePiercing || ConsumePenetration();
            for (var index = 0; index < Enemies.Count; index++)
            {
                var enemy = Enemies[index];
                if (!enemy.IsAlive)
                {
                    continue;
                }

                ApplyAttackHit(index, damage, pierced);
            }

            var bonusMessage = RegisterAttackCardUse();
            UpdateVictoryState();
            var pierceMessage = pierced ? "シールドを貫通。" : string.Empty;
            return
                $"敵全体に{damage}ダメージ。" +
                $"{pierceMessage}{bonusMessage}".Trim();
        }

        private string UseRandomBarrage()
        {
            var damage = PreviewValue(CardKind.RandomBarrage);
            ConsumeOneShotModifiers();
            var hits = new List<string>();
            for (var hit = 0; hit < 4; hit++)
            {
                var targetIndices = Enemies
                    .Select((enemy, index) => (Enemy: enemy, Index: index))
                    .Where(pair => pair.Enemy.IsAlive)
                    .Select(pair => pair.Index)
                    .ToList();
                if (targetIndices.Count == 0)
                {
                    break;
                }

                var targetIndex =
                    targetIndices[_random.Next(targetIndices.Count)];
                var target = Enemies[targetIndex];
                var pierced = ConsumePenetration();
                ApplyAttackHit(targetIndex, damage, pierced);

                hits.Add(
                    $"{target.Name}へ{damage}" +
                    (pierced ? "（貫通）" : string.Empty));
            }

            var bonusMessage = RegisterAttackCardUse();
            UpdateVictoryState();
            return $"乱撃：{string.Join("、", hits)}。{bonusMessage}".Trim();
        }

        private void ApplyAttackHit(
            int targetIndex,
            int damage,
            bool pierced)
        {
            var target = Enemies[targetIndex];
            var hpBefore = target.Hp;
            var shieldBefore = target.Shield;
            _lastAttackTargetIndices.Add(targetIndex);
            if (pierced)
            {
                target.ReceiveDamageIgnoringShield(damage);
            }
            else
            {
                target.ReceiveDamage(damage);
            }

            _lastAttackHits.Add(
                new AttackHitResult(
                    targetIndex,
                    hpBefore,
                    target.Hp,
                    shieldBefore,
                    target.Shield,
                    pierced));
        }

        private string RegisterAttackCardUse()
        {
            if (PersistentStacks > 0)
            {
                AttackCardsTowardBonus++;
                if (AttackCardsTowardBonus >= 3)
                {
                    AttackCardsTowardBonus -= 3;
                    PermanentAttackBonus += PersistentStacks;
                    return $"永続攻撃力が+{PersistentStacks}された。";
                }
            }

            return string.Empty;
        }

        private string RegisterCardUseForGuardCycle(int activeStacks)
        {
            if (activeStacks <= 0)
            {
                return string.Empty;
            }

            CardsTowardAutoDefense++;
            if (CardsTowardAutoDefense < 3)
            {
                return string.Empty;
            }

            CardsTowardAutoDefense -= 3;
            var addedStacks = activeStacks * 2;
            AutoDefenseStacks += addedStacks;
            return $" 守護循環により自動防御+{addedStacks}。";
        }

        private string ApplyCrystalTools(
            IReadOnlyDictionary<EtherType, int> cost)
        {
            var messages = new List<string>();
            if (_player.HasTool(CarryToolKind.RedCrystal) &&
                cost.TryGetValue(EtherType.Red, out var redCost) &&
                redCost >= 3)
            {
                PermanentAttackBonus++;
                messages.Add("攻撃力+1。");
            }

            if (_player.HasTool(CarryToolKind.BlueCrystal) &&
                cost.TryGetValue(EtherType.Blue, out var blueCost) &&
                blueCost >= 3)
            {
                DefensePowerBonus++;
                messages.Add("防御力+1。");
            }

            return messages.Count == 0
                ? string.Empty
                : $" {string.Join(string.Empty, messages)}";
        }

        private string ActivatePersistent(CardInstance card)
        {
            card.PersistentActivated = true;
            switch (card.Kind)
            {
                case CardKind.Persistent:
                    PersistentStacks++;
                    return
                        $"持続効果を起動。" +
                        $"攻撃3回ごとに攻撃力+{PersistentStacks}。";
                case CardKind.GuardCyclePersistent:
                    GuardCycleStacks++;
                    return
                        $"守護循環を起動。" +
                        $"カード3枚ごとに自動防御+{GuardCycleStacks * 2}。";
                default:
                    throw new InvalidOperationException(
                        "持続カードではありません。");
            }
        }

        private void ActivateLargeBelt()
        {
            if (!_player.HasTool(CarryToolKind.LargeBelt))
            {
                return;
            }

            var persistentCards = _player.Deck
                .Where(card => CardCatalog.IsPersistent(card.Kind))
                .ToList();
            if (persistentCards.Count == 0)
            {
                BattleStartMessage =
                    "大型ベルト：発動できる持続カードがありません。";
                return;
            }

            var selected =
                persistentCards[_random.Next(persistentCards.Count)];
            BattleStartMessage =
                $"大型ベルト：{CardCatalog.GetName(selected.Kind)}を無料発動。" +
                ActivatePersistent(selected);
        }

        private string GainShield(int amount)
        {
            var gained = Math.Max(0, amount);
            _player.Shield += gained;
            if (gained <= 0 ||
                !_player.HasTool(CarryToolKind.GuardianJudgment))
            {
                return string.Empty;
            }

            var targets = Enemies
                .Where(enemy => enemy.IsAlive)
                .ToList();
            var damage = _player.Shield * 20 / 100;
            if (targets.Count == 0 || damage <= 0)
            {
                return string.Empty;
            }

            var target = targets[_random.Next(targets.Count)];
            target.ReceiveDamage(damage);
            UpdateVictoryState();
            return
                $" 守護者の裁き：{target.Name}へ{damage}ダメージ。";
        }

        private void UpdateVictoryState()
        {
            if (Enemies.All(enemy => !enemy.IsAlive))
            {
                Phase = BattlePhase.Victory;
            }
        }

        private string UseDefense(CardKind kind)
        {
            var shield = PreviewValue(kind);
            ConsumeOneShotModifiers();
            var judgmentMessage = GainShield(shield);
            return
                $"シールドを{shield}獲得。現在{_player.Shield}。" +
                judgmentMessage;
        }

        private string UseAutoDefense()
        {
            var stacks = PreviewValue(CardKind.AutoDefense);
            ConsumeOneShotModifiers();
            AutoDefenseStacks += stacks;
            return $"自動防御を{stacks}層獲得。現在{AutoDefenseStacks}層。";
        }

        private string UseGuardContinuance()
        {
            var autoDefense = PreviewValue(CardKind.GuardContinuance);
            ConsumeOneShotModifiers();
            AutoDefenseStacks += autoDefense;
            DefenseRetentionStacks += 2;
            return
                $"自動防御を{autoDefense}層、防御維持を2層獲得。";
        }

        private string UseMirrorShield()
        {
            var addedShield = _player.Shield;
            ConsumeOneShotModifiers();
            var judgmentMessage = GainShield(addedShield);
            DefenseRetentionStacks += 2;
            return
                $"シールドを{_player.Shield}へ倍化し、防御維持を2層獲得。" +
                judgmentMessage;
        }

        private void ConsumeOneShotModifiers()
        {
            PendingCharge = _player.HasTool(CarryToolKind.VorpalHeart)
                ? PendingCharge / 2
                : 0;
            PendingWeaken = 0;
        }

        private int GetAttackPowerContribution(CardKind kind)
        {
            if (!_player.HasTool(CarryToolKind.VorpalDagger) ||
                !CardCatalog.IsSingleTargetAttack(kind))
            {
                return PermanentAttackBonus;
            }

            return PermanentAttackBonus *
                   CardCatalog.GetCost(kind).Values.Sum();
        }

        private int GetShieldToolBonus()
        {
            return _player.HasTool(CarryToolKind.ShieldBoost) ? 2 : 0;
        }

        private bool ConsumePenetration()
        {
            if (PenetrationStacks <= 0)
            {
                return false;
            }

            PenetrationStacks--;
            return true;
        }

        private string BeginPlayerTurn()
        {
            Phase = BattlePhase.PlayerTurn;
            TurnNumber++;
            foreach (var card in _player.Deck)
            {
                if (card.CooldownRemaining > 0)
                {
                    card.CooldownRemaining--;
                }
            }

            var messages = new List<string>();
            if (DefenseRetentionStacks > 0)
            {
                DefenseRetentionStacks--;
                messages.Add(
                    $"防御維持：シールド{_player.Shield}を維持。" +
                    $"残り{DefenseRetentionStacks}層。");
            }
            else if (_player.HasTool(CarryToolKind.ShieldStorage))
            {
                _player.Shield /= 2;
                messages.Add(
                    $"シールド貯蔵庫：シールドを{_player.Shield}残した。");
            }
            else
            {
                _player.Shield = 0;
            }

            if (AutoDefenseStacks > 0)
            {
                var autoDefenseShield =
                    AutoDefenseStacks + DefensePowerBonus;
                var judgmentMessage = GainShield(autoDefenseShield);
                AutoDefenseStacks--;
                messages.Add(
                    $"自動防御：シールドを{autoDefenseShield}獲得。" +
                    $"残り{AutoDefenseStacks}層。" +
                    judgmentMessage);
            }

            if (PendingCharge == 0)
            {
                var startCharge = _player.GetTurnStartCharge();
                if (startCharge > 0)
                {
                    PendingCharge = startCharge;
                    messages.Add($"星の道具：チャージ+{startCharge}。");
                }
            }

            var drawn = Pool.Draw(_player.GetEtherDrawCount());
            LastDrawnEtherTypes = drawn;
            LastUnusedAfterDraw = CopyPool(Pool.Unused);
            LastCurrentAfterDraw = CopyPool(Pool.Current);
            LastSpentAfterDraw = CopyPool(Pool.Spent);
            return string.Join("\n", messages);
        }

        private static IReadOnlyDictionary<EtherType, int> CopyPool(
            IReadOnlyDictionary<EtherType, int> source)
        {
            return source.ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        private List<string> ExecuteEnemyTurn()
        {
            var messages = new List<string>();
            foreach (var enemy in Enemies)
            {
                if (!enemy.IsAlive)
                {
                    continue;
                }

                enemy.Shield = 0;
                var action = enemy.AdvanceAction();
                switch (action)
                {
                    case EnemyActionKind.Attack:
                        var hpDamage = _player.ReceiveDamage(enemy.Attack);
                        messages.Add(
                            $"{enemy.Name}の攻撃：{enemy.Attack}ダメージ" +
                            $"（HPへのダメージ{hpDamage}）。");
                        break;
                    case EnemyActionKind.Defense:
                        enemy.Shield += enemy.Defense;
                        messages.Add(
                            $"{enemy.Name}はシールドを{enemy.Defense}獲得。");
                        break;
                    case EnemyActionKind.Weaken:
                        PendingWeaken += 3;
                        messages.Add(
                            $"{enemy.Name}の弱体：次の攻撃・防御-{PendingWeaken}。");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (_player.Hp <= 0)
                {
                    break;
                }
            }

            return messages;
        }
    }
}
