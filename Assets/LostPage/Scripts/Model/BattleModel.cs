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
        private int _doubleAttackProgress;
        private int _areaAttackProgress;
        private int _defenseSupportProgress;
        private int _lastDivineCooldownReduction;

        public BattleModel(
            PlayerState player,
            IEnumerable<EnemyState> enemies,
            Random random)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            Enemies = enemies?.ToList() ??
                      throw new ArgumentNullException(nameof(enemies));
            if (Enemies.Count == 0)
            {
                throw new ArgumentException(
                    "敵を1体以上指定してください。",
                    nameof(enemies));
            }

            foreach (var card in _player.Deck)
            {
                card.PersistentActivated = false;
                card.CooldownRemaining = 0;
                card.GrowthBonus = 0;
                card.UsedThisBattle = false;
            }

            _player.Shield = 0;
            PermanentAttackBonus =
                _player.HasTool(CarryToolKind.AttackBoost) ? 1 : 0;
            PenetrationStacks =
                _player.HasTool(CarryToolKind.FirstAttackPierce) ? 1 : 0;
            PendingAttackExecutions = 1;
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
        public int ReflectionStacks { get; private set; }
        public int ShieldLostTotal { get; private set; }
        public int CardsUsedThisTurn { get; private set; }
        public int AttackCardsUsedThisTurn { get; private set; }
        public int DefenseCardsUsedThisTurn { get; private set; }
        public int PendingAttackExecutions { get; private set; }
        public bool FirstAttackPierceAvailable => PenetrationStacks > 0;
        public int TotalRedEtherCount => Pool.GetTotal(EtherType.Red);
        public string BattleStartMessage { get; private set; }
        public string TurnStartMessage { get; private set; }
        public PlayerState Player => _player;
        public IReadOnlyList<CardInstance> ActivePersistentCards =>
            _player.Deck
                .Where(
                    card =>
                        CardCatalog.IsPersistent(card.Kind) &&
                        card.PersistentActivated)
                .ToList();
        public bool LastAttackIsSequential { get; private set; }
        public IReadOnlyList<int> LastAttackTargetIndices =>
            _lastAttackTargetIndices;
        public IReadOnlyList<AttackHitResult> LastAttackHits =>
            _lastAttackHits;
        public IReadOnlyList<EtherType> LastDrawnEtherTypes
        {
            get;
            private set;
        } = Array.Empty<EtherType>();
        public IReadOnlyList<EtherType> LastRefilledEtherTypes
        {
            get;
            private set;
        } = Array.Empty<EtherType>();
        public int LastEtherRefillDrawIndex { get; private set; } = -1;
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

            if (card.CooldownRemaining > 0 ||
                CardCatalog.IsOncePerBattle(card) && card.UsedThisBattle)
            {
                return false;
            }

            if (card.Kind == CardKind.DivineStrike)
            {
                return Pool.CurrentTotal > 0;
            }

            return Pool.CanPay(CardCatalog.GetCost(card));
        }

        public IReadOnlyList<EtherType> GetDivineStrikeModes()
        {
            if (Pool.CurrentTotal <= 0)
            {
                return Array.Empty<EtherType>();
            }

            var maximum = Pool.Current.Values.Max();
            return Enum.GetValues(typeof(EtherType))
                .Cast<EtherType>()
                .Where(type => Pool.Current[type] == maximum)
                .ToList();
        }

        public int PreviewValue(CardKind kind)
        {
            return PreviewValue(new CardInstance(-1, kind));
        }

        public int PreviewValue(CardInstance card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            var upgraded = card.IsUpgraded;
            switch (card.Kind)
            {
                case CardKind.Attack:
                    return AttackValue(card, upgraded ? 10 : 5);
                case CardKind.HeavyAttack:
                    return AttackValue(card, upgraded ? 24 : 17);
                case CardKind.AreaAttack:
                    return AttackValue(card, upgraded ? 15 : 9);
                case CardKind.GrowthAttack:
                    return AttackValue(
                        card,
                        (upgraded ? 10 : 8) + card.GrowthBonus);
                case CardKind.RandomBarrage:
                    return AttackValue(card, upgraded ? 18 : 10);
                case CardKind.RedPulseAttack:
                    return AttackValue(
                        card,
                        (upgraded ? 15 : 10) + TotalRedEtherCount);
                case CardKind.PiercingAreaAttack:
                    return AttackValue(card, upgraded ? 48 : 36);
                case CardKind.AceAttack:
                    return AttackValue(
                        card,
                        _player.Deck.Count(
                            owned => CardCatalog.IsAttack(owned.Kind)) *
                        (upgraded ? 8 : 4) +
                        TotalRedEtherCount);
                case CardKind.GrowingSword:
                    return AttackValue(card, 1 + card.GrowthBonus);
                case CardKind.FlameStrike:
                    return AttackValue(card, upgraded ? 10 : 7);
                case CardKind.SweepingFlameStrike:
                    return AttackValue(card, upgraded ? 12 : 6);
                case CardKind.SurgingFlame:
                    return AttackValue(card, upgraded ? 14 : 5);
                case CardKind.DefenseConversion:
                    return Math.Max(
                        0,
                        DivideCeiling(_player.Shield, 2) +
                        GetAttackPowerContribution(card) +
                        PendingCharge * 2 -
                        PendingWeaken);
                case CardKind.Defense:
                    return DefenseValue(
                        upgraded ? 12 : 7,
                        GetShieldToolBonus(card.Kind));
                case CardKind.StrongDefense:
                    return DefenseValue(
                        upgraded ? 20 : 12,
                        GetShieldToolBonus(card.Kind));
                case CardKind.AutoDefense:
                    return DefenseValue(upgraded ? 6 : 3);
                case CardKind.GuardContinuance:
                    return DefenseValue(2);
                case CardKind.MirrorShield:
                    return _player.Shield * (upgraded ? 3 : 2);
                case CardKind.FullDefense:
                    return DefenseValue(
                        Pool.GetTotal(EtherType.Blue) *
                        (upgraded ? 2 : 1));
                case CardKind.SpikedShield:
                    return DefenseValue(upgraded ? 12 : 6);
                case CardKind.HeavyArmor:
                    return DefenseValue(14);
                case CardKind.CounterDraw:
                    return DefenseValue(6);
                case CardKind.FullLightAutoDefense:
                    return DefenseValue(upgraded ? 24 : 12);
                case CardKind.RegressionDefense:
                    return DefenseValue(
                        upgraded
                            ? DivideCeiling(ShieldLostTotal * 3, 2)
                            : ShieldLostTotal);
                case CardKind.Barrier:
                    return DefenseValue(30);
                case CardKind.GoldConversion:
                    return DefenseValue(
                        DivideCeiling(_player.Gold * 7, 100) *
                        (upgraded ? 4 : 2));
                case CardKind.Charge:
                    return upgraded ? 8 : 3;
                case CardKind.Resonance:
                    return upgraded
                        ? DivideCeiling(TotalRedEtherCount * 3, 2)
                        : TotalRedEtherCount;
                case CardKind.HealCharge:
                    return upgraded ? 12 : 6;
                case CardKind.EtherConversion:
                    return upgraded ? 4 : 2;
                case CardKind.BlueResonance:
                    return upgraded
                        ? DivideCeiling(
                            Pool.GetTotal(EtherType.Blue) * 3,
                            2)
                        : Pool.GetTotal(EtherType.Blue);
                case CardKind.YellowResonance:
                    return Pool.GetTotal(EtherType.Yellow) *
                           (upgraded ? 6 : 3);
                case CardKind.PenetrationPower:
                    return upgraded ? 10 : 5;
                case CardKind.AttackRecovery:
                    return AttackCardsUsedThisTurn *
                           (upgraded ? 2 : 1);
                case CardKind.ShiftingShadow:
                    return upgraded ? 12 : 6;
                case CardKind.Persistent:
                    return PersistentStacks + 1;
                case CardKind.GuardCyclePersistent:
                    return GuardCycleStacks + 1;
                default:
                    return 0;
            }
        }

        public string UseCard(
            CardInstance card,
            int targetIndex,
            EtherType? divineMode = null)
        {
            if (!CanUse(card))
            {
                throw new InvalidOperationException(
                    "このカードは現在使用できません。");
            }

            var guardCycleActive =
                IsPersistentActive(CardKind.GuardCyclePersistent);
            var cost = CardCatalog.GetCost(card);
            _lastAttackTargetIndices.Clear();
            _lastAttackHits.Clear();
            LastAttackIsSequential = false;
            _lastDivineCooldownReduction = 0;

            IReadOnlyDictionary<EtherType, int> paid;
            if (card.Kind == CardKind.DivineStrike)
            {
                var modes = GetDivineStrikeModes();
                if (modes.Count > 1 && !divineMode.HasValue)
                {
                    throw new InvalidOperationException(
                        "神撃で発動する色を選択してください。");
                }

                if (divineMode.HasValue &&
                    !modes.Contains(divineMode.Value))
                {
                    throw new InvalidOperationException(
                        "最も多いエーテル色だけ選択できます。");
                }

                paid = Pool.PayAllCurrent();
            }
            else
            {
                Pool.Pay(cost);
                paid = cost;
            }

            CardsUsedThisTurn++;
            string message;
            if (CardCatalog.IsAttack(card.Kind))
            {
                AttackCardsUsedThisTurn++;
                message = UseAttackCard(
                    card,
                    targetIndex,
                    paid.Values.Sum());
            }
            else if (CardCatalog.GetCategory(card.Kind) ==
                     CardCategory.Defense)
            {
                DefenseCardsUsedThisTurn++;
                message = UseDefenseCard(card);
                message += RegisterDefenseCardUse();
            }
            else if (CardCatalog.GetCategory(card.Kind) ==
                     CardCategory.Charge)
            {
                message = UseChargeCard(card);
            }
            else if (CardCatalog.IsPersistent(card.Kind))
            {
                message = ActivatePersistent(card);
            }
            else if (card.Kind == CardKind.DivineStrike)
            {
                message = UseDivineStrike(card, targetIndex, divineMode, paid);
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }

            var crystalMessage = ApplyCrystalTools(paid);
            var cycleMessage =
                RegisterCardUseForGuardCycle(guardCycleActive);
            card.UsedThisBattle =
                card.UsedThisBattle || CardCatalog.IsOncePerBattle(card);
            card.CooldownRemaining = CardCatalog.GetCooldown(card);
            if (_lastDivineCooldownReduction > 0)
            {
                ReduceAllCooldowns(_lastDivineCooldownReduction);
            }

            UpdateVictoryState();
            return $"{message}{crystalMessage}{cycleMessage}".Trim();
        }

        public string EndPlayerTurn(IReadOnlyList<EtherType> carried)
        {
            if (Phase != BattlePhase.PlayerTurn)
            {
                throw new InvalidOperationException(
                    "プレイヤーターンではありません。");
            }

            Pool.EndTurn(carried, _player.GetCarryLimit());
            var messages = new List<string>();
            var wristMessage = ApplyWristSupporterAtTurnEnd();
            if (!string.IsNullOrEmpty(wristMessage))
            {
                messages.Add(wristMessage);
            }

            var autoDefenseMessage = ApplyAutoDefenseAtTurnEnd();
            if (!string.IsNullOrEmpty(autoDefenseMessage))
            {
                messages.Add(autoDefenseMessage);
            }

            if (Phase == BattlePhase.Victory)
            {
                return string.Join("\n", messages);
            }

            Phase = BattlePhase.EnemyTurn;
            messages.AddRange(ExecuteEnemyTurn());
            if (_player.Hp <= 0)
            {
                Phase = BattlePhase.Defeat;
                messages.Add("プレイヤーのHPが0になりました。");
                return string.Join("\n", messages);
            }

            if (Phase == BattlePhase.Victory)
            {
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

        private string UseAttackCard(
            CardInstance card,
            int targetIndex,
            int paidEtherCount)
        {
            if (CardCatalog.RequiresEnemyTarget(card.Kind))
            {
                ValidateTarget(targetIndex);
            }

            var forceArea = AdvanceAreaAttackPersistent();
            var executions = Math.Max(1, PendingAttackExecutions);
            PendingAttackExecutions = 1;
            if (AdvanceDoubleAttackPersistent())
            {
                executions *= 2;
            }

            LastAttackIsSequential =
                executions > 1 ||
                card.Kind == CardKind.RandomBarrage ||
                card.Kind == CardKind.SurgingFlame ||
                card.Kind == CardKind.Combustion ||
                card.Kind == CardKind.Heatstroke;
            var messages = new List<string>();
            for (var execution = 0; execution < executions; execution++)
            {
                if (Enemies.All(enemy => !enemy.IsAlive))
                {
                    break;
                }

                messages.Add(
                    ExecuteAttackEffect(card, targetIndex, forceArea));
            }

            ConsumeOneShotModifiers();
            ApplyCrimsonMoon(paidEtherCount);
            GrowGrowingSwords();
            var bonusMessage = RegisterAttackCardUse();
            if (!string.IsNullOrEmpty(bonusMessage))
            {
                messages.Add(bonusMessage);
            }

            return string.Join(" ", messages.Where(text => text.Length > 0));
        }

        private string ExecuteAttackEffect(
            CardInstance card,
            int targetIndex,
            bool forceArea)
        {
            switch (card.Kind)
            {
                case CardKind.Attack:
                case CardKind.HeavyAttack:
                case CardKind.RedPulseAttack:
                case CardKind.AceAttack:
                case CardKind.GrowingSword:
                    return ApplyStandardAttack(
                        card,
                        targetIndex,
                        forceArea,
                        false);
                case CardKind.AreaAttack:
                    return ApplyStandardAttack(card, targetIndex, true, false);
                case CardKind.GrowthAttack:
                    var growthMessage = ApplyStandardAttack(
                        card,
                        targetIndex,
                        forceArea,
                        false);
                    card.GrowthBonus += card.IsUpgraded ? 4 : 3;
                    return growthMessage +
                           $" 基礎ダメージ+{(card.IsUpgraded ? 4 : 3)}。";
                case CardKind.RandomBarrage:
                    return UseRandomBarrage(card, forceArea);
                case CardKind.PiercingAreaAttack:
                    return ApplyStandardAttack(card, targetIndex, true, true);
                case CardKind.FlameStrike:
                    return ApplyFlameAttack(
                        card,
                        targetIndex,
                        forceArea,
                        card.IsUpgraded ? 6 : 3,
                        1);
                case CardKind.SweepingFlameStrike:
                    return ApplyFlameAttack(
                        card,
                        targetIndex,
                        true,
                        card.IsUpgraded ? 5 : 3,
                        1);
                case CardKind.SurgingFlame:
                    return ApplyFlameAttack(
                        card,
                        targetIndex,
                        forceArea,
                        card.IsUpgraded ? 4 : 2,
                        2);
                case CardKind.Combustion:
                    return TriggerFireForAll(3);
                case CardKind.ExplosionFlame:
                    return DoubleFire(targetIndex, forceArea);
                case CardKind.DefenseConversion:
                    return UseDefenseConversion(card, targetIndex, forceArea);
                case CardKind.Heatstroke:
                    return UseHeatstroke(
                        targetIndex,
                        forceArea,
                        card.IsUpgraded ? 6 : 4);
                default:
                    throw new InvalidOperationException(
                        "攻撃カードの効果が定義されていません。");
            }
        }

        private string ApplyStandardAttack(
            CardInstance card,
            int targetIndex,
            bool forceArea,
            bool innatePiercing)
        {
            var damage = PreviewValue(card);
            var pierced = innatePiercing || ConsumePenetration();
            var targets = GetAttackTargets(targetIndex, forceArea);
            foreach (var index in targets)
            {
                ApplyAttackHit(index, damage, pierced);
            }

            return forceArea
                ? $"敵全体に{damage}ダメージ。" +
                  (pierced ? "シールドを貫通。" : string.Empty)
                : $"{Enemies[targetIndex].Name}に{damage}ダメージ。" +
                  (pierced ? "シールドを貫通。" : string.Empty);
        }

        private string ApplyFlameAttack(
            CardInstance card,
            int targetIndex,
            bool forceArea,
            int fire,
            int hitCount)
        {
            var damage = PreviewValue(card);
            for (var hit = 0; hit < hitCount; hit++)
            {
                var pierced = ConsumePenetration();
                foreach (var index in GetAttackTargets(targetIndex, forceArea))
                {
                    ApplyAttackHit(index, damage, pierced);
                    Enemies[index].Fire += fire;
                }
            }

            return forceArea
                ? $"敵全体へ{damage}ダメージと炎{fire}を{hitCount}回。"
                : $"{Enemies[targetIndex].Name}へ{damage}ダメージと炎{fire}を{hitCount}回。";
        }

        private string UseRandomBarrage(
            CardInstance card,
            bool forceArea)
        {
            var damage = PreviewValue(card);
            var hits = new List<string>();
            for (var hit = 0; hit < 4; hit++)
            {
                var living = GetLivingEnemyIndices();
                if (living.Count == 0)
                {
                    break;
                }

                var pierced = ConsumePenetration();
                if (forceArea)
                {
                    foreach (var index in living)
                    {
                        ApplyAttackHit(index, damage, pierced);
                    }

                    hits.Add($"敵全体へ{damage}");
                }
                else
                {
                    var targetIndex = living[_random.Next(living.Count)];
                    ApplyAttackHit(targetIndex, damage, pierced);
                    hits.Add($"{Enemies[targetIndex].Name}へ{damage}");
                }
            }

            return $"乱撃：{string.Join("、", hits)}。";
        }

        private string TriggerFireForAll(int triggerCount)
        {
            var total = 0;
            for (var index = 0; index < Enemies.Count; index++)
            {
                for (var trigger = 0; trigger < triggerCount; trigger++)
                {
                    if (!Enemies[index].IsAlive || Enemies[index].Fire <= 0)
                    {
                        break;
                    }

                    total += TriggerFire(index, true);
                }
            }

            return $"敵全体の炎を{triggerCount}回発動し、合計{total}ダメージ。";
        }

        private string DoubleFire(int targetIndex, bool forceArea)
        {
            foreach (var index in GetAttackTargets(targetIndex, forceArea))
            {
                _lastAttackTargetIndices.Add(index);
                Enemies[index].Fire *= 2;
            }

            return forceArea
                ? "敵全体の炎を2倍にした。"
                : $"{Enemies[targetIndex].Name}の炎を2倍にした。";
        }

        private string UseDefenseConversion(
            CardInstance card,
            int targetIndex,
            bool forceArea)
        {
            var consumed = DivideCeiling(_player.Shield, 2);
            LoseShield(consumed);
            var damage = Math.Max(
                0,
                consumed +
                GetAttackPowerContribution(card) +
                PendingCharge * 2 -
                PendingWeaken);
            var pierced = ConsumePenetration();
            foreach (var index in GetAttackTargets(targetIndex, forceArea))
            {
                ApplyAttackHit(index, damage, pierced);
            }

            return $"シールド{consumed}を消費し、{damage}ダメージ。";
        }

        private string UseHeatstroke(
            int targetIndex,
            bool forceArea,
            int fire)
        {
            var total = 0;
            foreach (var index in GetAttackTargets(targetIndex, forceArea))
            {
                Enemies[index].Fire += fire;
                while (Enemies[index].IsAlive && Enemies[index].Fire > 0)
                {
                    total += TriggerFire(index, true);
                }
            }

            return $"炎{fire}を付与し、炎がなくなるまで合計{total}ダメージ。";
        }

        private string UseDefenseCard(CardInstance card)
        {
            string result;
            switch (card.Kind)
            {
                case CardKind.Defense:
                case CardKind.StrongDefense:
                case CardKind.FullDefense:
                    result = GainShieldMessage(PreviewValue(card));
                    break;
                case CardKind.AutoDefense:
                    result = GainAutoDefenseMessage(PreviewValue(card));
                    break;
                case CardKind.GuardContinuance:
                    result = GainAutoDefenseMessage(PreviewValue(card));
                    DefenseRetentionStacks += 2;
                    result += " 防御維持+2。";
                    break;
                case CardKind.MirrorShield:
                    var multiplier = card.IsUpgraded ? 3 : 2;
                    var added = _player.Shield * (multiplier - 1);
                    result = GainShieldMessage(added);
                    DefenseRetentionStacks += card.IsUpgraded ? 3 : 2;
                    result +=
                        $" 防御維持+{(card.IsUpgraded ? 3 : 2)}。";
                    break;
                case CardKind.SpikedShield:
                    result = GainShieldMessage(PreviewValue(card));
                    ReflectionStacks += card.IsUpgraded ? 4 : 2;
                    result +=
                        $" 反射+{(card.IsUpgraded ? 4 : 2)}。";
                    break;
                case CardKind.ThornArmor:
                    var reflection = Math.Max(
                        0,
                        (card.IsUpgraded ? 12 : 6) +
                        PendingCharge -
                        PendingWeaken);
                    ReflectionStacks += reflection;
                    result = $"反射+{reflection}。";
                    break;
                case CardKind.HeavyArmor:
                    result = GainShieldMessage(PreviewValue(card));
                    if (DefenseRetentionStacks == 0)
                    {
                        DefenseRetentionStacks = 1;
                        result += " 防御維持+1。";
                    }

                    break;
                case CardKind.CounterDraw:
                    result = GainShieldMessage(PreviewValue(card));
                    var attackIntentCount = Enemies.Count(
                        enemy =>
                            enemy.IsAlive &&
                            enemy.NextAction == EnemyActionKind.Attack);
                    var drawn = Pool.Draw(attackIntentCount);
                    result += $" エーテルを{drawn.Count}個ドロー。";
                    break;
                case CardKind.FullLightAutoDefense:
                    result = GainAutoDefenseMessage(PreviewValue(card));
                    DefenseRetentionStacks += card.IsUpgraded ? 8 : 3;
                    result +=
                        $" 防御維持+{(card.IsUpgraded ? 8 : 3)}。";
                    break;
                case CardKind.RegressionDefense:
                    result = GainShieldMessage(PreviewValue(card));
                    DefenseRetentionStacks += card.IsUpgraded ? 6 : 3;
                    result +=
                        $" 防御維持+{(card.IsUpgraded ? 6 : 3)}。";
                    break;
                case CardKind.Barrier:
                    result = GainShieldMessage(PreviewValue(card));
                    DefenseRetentionStacks += card.IsUpgraded ? 8 : 5;
                    result +=
                        $" 防御維持+{(card.IsUpgraded ? 8 : 5)}。";
                    break;
                case CardKind.GoldConversion:
                    var convertedShield = PreviewValue(card);
                    var spentGold = DivideCeiling(_player.Gold * 7, 100);
                    _player.Gold -= spentGold;
                    result = GainShieldMessage(convertedShield);
                    result += $" {spentGold}Gを消費。";
                    break;
                default:
                    throw new InvalidOperationException(
                        "防御カードの効果が定義されていません。");
            }

            ConsumeOneShotModifiers();
            return result;
        }

        private string UseChargeCard(CardInstance card)
        {
            switch (card.Kind)
            {
                case CardKind.Charge:
                    return GainCharge(PreviewValue(card));
                case CardKind.Resonance:
                case CardKind.BlueResonance:
                case CardKind.YellowResonance:
                    return GainCharge(PreviewValue(card));
                case CardKind.HealCharge:
                    var healed = _player.Heal(PreviewValue(card));
                    return $"HPを{healed}回復。";
                case CardKind.EtherConversion:
                    var added = PreviewValue(card);
                    var converted =
                        Pool.ConvertCurrentToRandomTypeAndAdd(added);
                    return
                        $"手番エーテルを{CardCatalog.GetEtherName(converted)}へ変換し、" +
                        $"{CardCatalog.GetEtherName(converted)}を{added}個追加。";
                case CardKind.PurpleResonance:
                    return ActivateRandomPersistent(card.IsUpgraded);
                case CardKind.PenetrationPower:
                    var penetration = PreviewValue(card);
                    PenetrationStacks += penetration;
                    return $"貫通+{penetration}。";
                case CardKind.AttackRecovery:
                    var attackPower = PreviewValue(card);
                    PermanentAttackBonus += attackPower;
                    return $"攻撃力+{attackPower}。";
                case CardKind.RedPaperSummon:
                    return SummonAttackCards();
                case CardKind.ShiftingShadow:
                    var weakness = PreviewValue(card);
                    foreach (var enemy in Enemies.Where(enemy => enemy.IsAlive))
                    {
                        enemy.Weakness += weakness;
                    }

                    return $"敵全体に弱化{weakness}を付与。";
                case CardKind.MagicMirror:
                    var consumed = DivideCeiling(_player.Shield, 2);
                    LoseShield(consumed);
                    PendingCharge += consumed;
                    return
                        $"シールド{consumed}を消費し、チャージ+{consumed}。";
                case CardKind.DuplicateAttack:
                    var executions = card.IsUpgraded ? 3 : 2;
                    PendingAttackExecutions *= executions;
                    return
                        $"次の攻撃カードを合計{PendingAttackExecutions}回発動。";
                default:
                    throw new InvalidOperationException(
                        "チャージカードの効果が定義されていません。");
            }
        }

        private string UseDivineStrike(
            CardInstance card,
            int targetIndex,
            EtherType? selectedMode,
            IReadOnlyDictionary<EtherType, int> paid)
        {
            var availableModes = paid
                .Where(pair => pair.Value == paid.Values.Max())
                .Select(pair => pair.Key)
                .ToList();
            if (!selectedMode.HasValue)
            {
                if (availableModes.Count != 1)
                {
                    throw new InvalidOperationException(
                        "神撃で発動する色を選択してください。");
                }

                selectedMode = availableModes[0];
            }

            if (!availableModes.Contains(selectedMode.Value))
            {
                throw new InvalidOperationException(
                    "最も多く消費したエーテル色だけ選択できます。");
            }

            var red = paid[EtherType.Red];
            var blue = paid[EtherType.Blue];
            var yellow = paid[EtherType.Yellow];
            var purple = paid[EtherType.Purple];
            switch (selectedMode.Value)
            {
                case EtherType.Red:
                    var area = blue % 2 == 1;
                    if (!area)
                    {
                        ValidateTarget(targetIndex);
                    }

                    var damage = Math.Max(
                        0,
                        10 +
                        red * 4 +
                        purple * (PermanentAttackBonus + PendingCharge) -
                        PendingWeaken);
                    var hitCount = Math.Max(1, yellow);
                    LastAttackIsSequential = hitCount > 1;
                    for (var hit = 0; hit < hitCount; hit++)
                    {
                        var pierced = ConsumePenetration();
                        foreach (var index in GetAttackTargets(
                                     targetIndex,
                                     area))
                        {
                            ApplyAttackHit(index, damage, pierced);
                        }
                    }

                    ConsumeOneShotModifiers();
                    return
                        $"神撃・赤：{damage}ダメージを{hitCount}回。";
                case EtherType.Blue:
                    var shield = 10 + blue * 4 +
                                 yellow * DefensePowerBonus;
                    var shieldMessage = GainShieldMessage(shield);
                    ReflectionStacks += red;
                    DefenseRetentionStacks += purple * 6;
                    return
                        $"神撃・青：{shieldMessage} 反射+{red}、" +
                        $"防御維持+{purple * 6}。";
                case EtherType.Yellow:
                    var charge = 10 + yellow * 4;
                    PendingCharge += charge;
                    PermanentAttackBonus += red;
                    DefensePowerBonus += blue;
                    foreach (var enemy in Enemies.Where(enemy => enemy.IsAlive))
                    {
                        enemy.Fire += purple;
                        enemy.Bleed += purple;
                        enemy.Weakness += purple;
                    }

                    return
                        $"神撃・黄：チャージ+{charge}、攻撃力+{red}、" +
                        $"防御力+{blue}、敵全体へ炎・出血・弱化{purple}。";
                case EtherType.Purple:
                    PermanentAttackBonus += purple;
                    DefensePowerBonus += purple;
                    PendingCharge += purple;
                    PendingAttackExecutions *= 1 + red;
                    var multiplier = Math.Max(1, blue);
                    _player.Shield *= multiplier;
                    AutoDefenseStacks *= multiplier;
                    DefenseRetentionStacks *= multiplier;
                    _lastDivineCooldownReduction = yellow;
                    return
                        $"神撃・紫：攻撃力・防御力・チャージ+{purple}、" +
                        $"次の攻撃は合計{PendingAttackExecutions}回、" +
                        $"防御系状態×{multiplier}、全CT-{yellow}。";
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
            var appliedDamage = damage;
            if (pierced &&
                target.Shield > 0 &&
                IsPersistentActive(CardKind.PiercingShield))
            {
                appliedDamage += DivideCeiling(target.Shield, 2);
            }

            if (pierced)
            {
                target.ReceiveDamageIgnoringShield(appliedDamage);
            }
            else
            {
                target.ReceiveDamage(appliedDamage);
            }

            if (target.Bleed > 0)
            {
                target.ReceiveDamage(target.Bleed);
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

        private int TriggerFire(int targetIndex, bool recordHit)
        {
            var target = Enemies[targetIndex];
            if (!target.IsAlive || target.Fire <= 0)
            {
                return 0;
            }

            var hpBefore = target.Hp;
            var shieldBefore = target.Shield;
            var damage = target.Fire;
            target.ReceiveDamage(damage);
            target.Fire--;
            if (recordHit)
            {
                _lastAttackTargetIndices.Add(targetIndex);
                _lastAttackHits.Add(
                    new AttackHitResult(
                        targetIndex,
                        hpBefore,
                        target.Hp,
                        shieldBefore,
                        target.Shield,
                        false));
            }

            return damage;
        }

        private string ActivatePersistent(CardInstance card)
        {
            if (card.PersistentActivated)
            {
                return $"{CardCatalog.GetName(card.Kind)}はすでに発動済み。";
            }

            card.PersistentActivated = true;
            switch (card.Kind)
            {
                case CardKind.Persistent:
                    PersistentStacks = 1;
                    break;
                case CardKind.GuardCyclePersistent:
                    GuardCycleStacks = 1;
                    break;
                case CardKind.CrimsonMoon:
                    PermanentAttackBonus += card.IsUpgraded ? 8 : 5;
                    break;
            }

            return $"{CardCatalog.GetName(card.Kind)}を発動。";
        }

        private string ActivateRandomPersistent(bool excludeActivated)
        {
            var candidates = _player.Deck
                .Where(card => CardCatalog.IsPersistent(card.Kind))
                .Where(card => !excludeActivated || !card.PersistentActivated)
                .ToList();
            if (candidates.Count == 0)
            {
                return "発動できる持続カードがない。";
            }

            var selected = candidates[_random.Next(candidates.Count)];
            return
                $"{CardCatalog.GetName(selected.Kind)}をランダム発動。" +
                ActivatePersistent(selected);
        }

        private string SummonAttackCards()
        {
            var candidates = CardCatalog.AllKinds
                .Where(CardCatalog.IsAttack)
                .Where(
                    kind =>
                        CardCatalog.GetRarity(kind) == CardRarity.Normal ||
                        CardCatalog.GetRarity(kind) == CardRarity.Rare)
                .ToList();
            var names = new List<string>();
            for (var index = 0; index < 2; index++)
            {
                var kind = candidates[_random.Next(candidates.Count)];
                _player.AddCard(kind, true);
                names.Add(CardCatalog.GetName(kind));
            }

            return $"{string.Join("、", names)}を一時生成。";
        }

        private string RegisterAttackCardUse()
        {
            if (!IsPersistentActive(CardKind.Persistent))
            {
                return string.Empty;
            }

            var card = GetActivePersistent(CardKind.Persistent);
            var threshold = card.IsUpgraded ? 2 : 3;
            AttackCardsTowardBonus++;
            if (AttackCardsTowardBonus < threshold)
            {
                return string.Empty;
            }

            AttackCardsTowardBonus -= threshold;
            var bonus = card.IsUpgraded ? 2 : 1;
            PermanentAttackBonus += bonus;
            return $"持続カードにより攻撃力+{bonus}。";
        }

        private string RegisterDefenseCardUse()
        {
            if (!IsPersistentActive(CardKind.DefenseSupport))
            {
                return string.Empty;
            }

            _defenseSupportProgress++;
            if (_defenseSupportProgress < 3)
            {
                return string.Empty;
            }

            _defenseSupportProgress -= 3;
            var bonus =
                GetActivePersistent(CardKind.DefenseSupport).IsUpgraded
                    ? 2
                    : 1;
            DefensePowerBonus += bonus;
            return $" 防御補助により防御力+{bonus}。";
        }

        private string RegisterCardUseForGuardCycle(bool activeBeforeUse)
        {
            if (!activeBeforeUse)
            {
                return string.Empty;
            }

            var card = GetActivePersistent(CardKind.GuardCyclePersistent);
            var threshold = card.IsUpgraded ? 2 : 3;
            CardsTowardAutoDefense++;
            if (CardsTowardAutoDefense < threshold)
            {
                return string.Empty;
            }

            CardsTowardAutoDefense -= threshold;
            var amount = card.IsUpgraded ? 4 : 2;
            var shieldMessage = GainAutoDefense(amount);
            return
                $" 守護循環により自動防御+{amount}。" +
                shieldMessage;
        }

        private bool AdvanceDoubleAttackPersistent()
        {
            if (!IsPersistentActive(CardKind.DoubleAttackPersistent))
            {
                return false;
            }

            var card = GetActivePersistent(CardKind.DoubleAttackPersistent);
            var threshold = card.IsUpgraded ? 2 : 3;
            _doubleAttackProgress++;
            if (_doubleAttackProgress < threshold)
            {
                return false;
            }

            _doubleAttackProgress -= threshold;
            return true;
        }

        private bool AdvanceAreaAttackPersistent()
        {
            if (!IsPersistentActive(CardKind.AreaAttackPersistent))
            {
                return false;
            }

            var card = GetActivePersistent(CardKind.AreaAttackPersistent);
            var threshold = card.IsUpgraded ? 2 : 3;
            _areaAttackProgress++;
            if (_areaAttackProgress < threshold)
            {
                return false;
            }

            _areaAttackProgress -= threshold;
            return true;
        }

        private void GrowGrowingSwords()
        {
            foreach (var growingSword in _player.Deck.Where(
                         card => card.Kind == CardKind.GrowingSword))
            {
                growingSword.GrowthBonus +=
                    growingSword.IsUpgraded ? 4 : 2;
            }
        }

        private void ApplyCrimsonMoon(int paidEtherCount)
        {
            if (!IsPersistentActive(CardKind.CrimsonMoon) ||
                paidEtherCount <= 0)
            {
                return;
            }

            var card = GetActivePersistent(CardKind.CrimsonMoon);
            var bleed = paidEtherCount * (card.IsUpgraded ? 2 : 1);
            foreach (var targetIndex in
                     _lastAttackTargetIndices.Distinct())
            {
                Enemies[targetIndex].Bleed += bleed;
            }
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

        private void ActivateLargeBelt()
        {
            if (!_player.HasTool(CarryToolKind.LargeBelt))
            {
                return;
            }

            var persistentCards = _player.Deck
                .Where(
                    card =>
                        CardCatalog.IsPersistent(card.Kind) &&
                        !card.PersistentActivated)
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

        private string ApplyAutoDefenseAtTurnEnd()
        {
            if (AutoDefenseStacks <= 0)
            {
                return string.Empty;
            }

            var autoDefenseShield =
                AutoDefenseStacks + DefensePowerBonus;
            var judgmentMessage = GainShield(autoDefenseShield);
            AutoDefenseStacks--;
            return
                $"自動防御：シールドを{autoDefenseShield}獲得。" +
                $"残り{AutoDefenseStacks}層。" +
                judgmentMessage;
        }

        private string ApplyWristSupporterAtTurnEnd()
        {
            if (!IsPersistentActive(CardKind.WristSupporter))
            {
                return string.Empty;
            }

            var card = GetActivePersistent(CardKind.WristSupporter);
            var limit = card.IsUpgraded ? 2 : 3;
            if (CardsUsedThisTurn > limit)
            {
                return string.Empty;
            }

            var bonus = card.IsUpgraded ? 2 : 1;
            PermanentAttackBonus += bonus;
            return $"リストサポーター：攻撃力+{bonus}。";
        }

        private string GainShieldMessage(int amount)
        {
            var gained = Math.Max(0, amount);
            var judgment = GainShield(gained);
            return
                $"シールドを{gained}獲得。現在{_player.Shield}。" +
                judgment;
        }

        private string GainAutoDefenseMessage(int amount)
        {
            var shieldMessage = GainAutoDefense(amount);
            return
                $"自動防御を{amount}獲得。現在{AutoDefenseStacks}。" +
                shieldMessage;
        }

        private string GainAutoDefense(int amount)
        {
            var gained = Math.Max(0, amount);
            AutoDefenseStacks += gained;
            if (!IsPersistentActive(CardKind.AdditionalDefense) ||
                gained <= 0)
            {
                return string.Empty;
            }

            return " " + GainShieldMessage(gained);
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

            var targets = GetLivingEnemyIndices();
            var damage = DivideCeiling(_player.Shield * 20, 100);
            if (targets.Count == 0 || damage <= 0)
            {
                return string.Empty;
            }

            var targetIndex = targets[_random.Next(targets.Count)];
            var target = Enemies[targetIndex];
            target.ReceiveDamage(damage);
            UpdateVictoryState();
            return
                $" 守護者の裁き：{target.Name}へ{damage}ダメージ。";
        }

        private string GainCharge(int amount)
        {
            PendingCharge += Math.Max(0, amount);
            return
                $"チャージ+{Math.Max(0, amount)}。現在{PendingCharge}。";
        }

        private void ConsumeOneShotModifiers()
        {
            PendingCharge = _player.HasTool(CarryToolKind.VorpalHeart)
                ? DivideCeiling(PendingCharge, 2)
                : 0;
            PendingWeaken = 0;
        }

        private int AttackValue(CardInstance card, int baseValue)
        {
            return Math.Max(
                0,
                baseValue +
                GetAttackPowerContribution(card) +
                PendingCharge -
                PendingWeaken);
        }

        private int DefenseValue(int baseValue, int extra = 0)
        {
            return Math.Max(
                0,
                baseValue +
                extra +
                DefensePowerBonus +
                PendingCharge -
                PendingWeaken);
        }

        private int GetAttackPowerContribution(CardInstance card)
        {
            if (!_player.HasTool(CarryToolKind.VorpalDagger) ||
                !CardCatalog.IsSingleTargetAttack(card.Kind))
            {
                return PermanentAttackBonus;
            }

            return PermanentAttackBonus *
                   CardCatalog.GetCost(card).Values.Sum();
        }

        private int GetShieldToolBonus(CardKind kind)
        {
            return _player.HasTool(CarryToolKind.ShieldBoost) &&
                   (kind == CardKind.Defense ||
                    kind == CardKind.StrongDefense)
                ? 2
                : 0;
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

        private void ReduceAllCooldowns(int amount)
        {
            foreach (var card in _player.Deck)
            {
                card.CooldownRemaining =
                    Math.Max(0, card.CooldownRemaining - amount);
            }
        }

        private string BeginPlayerTurn()
        {
            Phase = BattlePhase.PlayerTurn;
            TurnNumber++;
            CardsUsedThisTurn = 0;
            AttackCardsUsedThisTurn = 0;
            DefenseCardsUsedThisTurn = 0;
            foreach (var card in _player.Deck)
            {
                if (card.CooldownRemaining > 0)
                {
                    card.CooldownRemaining--;
                }
            }

            var messages = new List<string>();
            if (ReflectionStacks > 0)
            {
                ReflectionStacks--;
                messages.Add($"反射が1減少。残り{ReflectionStacks}。");
            }

            foreach (var enemy in Enemies)
            {
                if (enemy.Bleed > 0)
                {
                    enemy.Bleed--;
                }
            }

            if (DefenseRetentionStacks > 0)
            {
                if (!IsPersistentActive(CardKind.DefensePersistence) ||
                    _player.Shield <= 0)
                {
                    DefenseRetentionStacks--;
                }

                messages.Add(
                    $"防御維持：シールド{_player.Shield}を維持。" +
                    $"残り{DefenseRetentionStacks}。");
            }
            else if (_player.HasTool(CarryToolKind.ShieldStorage))
            {
                var retained = DivideCeiling(_player.Shield, 2);
                LoseShield(_player.Shield - retained);
                messages.Add(
                    $"シールド貯蔵庫：シールドを{_player.Shield}残した。");
            }
            else
            {
                LoseShield(_player.Shield);
            }

            var myojoCharge =
                _player.GetToolCount(CarryToolKind.Myojo) * 50;
            if (myojoCharge > 0)
            {
                PendingCharge += myojoCharge;
                messages.Add($"明星：チャージ+{myojoCharge}。");
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
            LastRefilledEtherTypes = Pool.LastRefilledEtherTypes.ToArray();
            LastEtherRefillDrawIndex = Pool.LastRefillDrawIndex;
            LastUnusedAfterDraw = CopyPool(Pool.Unused);
            LastCurrentAfterDraw = CopyPool(Pool.Current);
            LastSpentAfterDraw = CopyPool(Pool.Spent);
            return string.Join("\n", messages);
        }

        private List<string> ExecuteEnemyTurn()
        {
            var messages = new List<string>();
            for (var index = 0; index < Enemies.Count; index++)
            {
                var enemy = Enemies[index];
                if (!enemy.IsAlive)
                {
                    continue;
                }

                if (enemy.Fire > 0)
                {
                    var fireDamage = enemy.Fire;
                    TriggerFire(index, false);
                    messages.Add(
                        $"{enemy.Name}の炎：{fireDamage}ダメージ。" +
                        $"残り{enemy.Fire}。");
                    if (!enemy.IsAlive)
                    {
                        messages.Add($"{enemy.Name}は行動前に倒れた。");
                        continue;
                    }
                }

                var action = enemy.AdvanceAction();
                switch (action)
                {
                    case EnemyActionKind.Attack:
                        var attack =
                            Math.Max(0, enemy.Attack - enemy.Weakness);
                        enemy.Weakness = 0;
                        var shieldBefore = _player.Shield;
                        var hpDamage = _player.ReceiveDamage(attack);
                        ShieldLostTotal +=
                            Math.Max(0, shieldBefore - _player.Shield);
                        messages.Add(
                            $"{enemy.Name}の攻撃：{attack}ダメージ" +
                            $"（HPへのダメージ{hpDamage}）。");
                        if (ReflectionStacks > 0)
                        {
                            enemy.ReceiveDamage(ReflectionStacks);
                            messages.Add(
                                $"反射：{enemy.Name}へ{ReflectionStacks}ダメージ。");
                        }

                        if (IsPersistentActive(CardKind.Tempering))
                        {
                            var tempering =
                                GetActivePersistent(CardKind.Tempering)
                                    .IsUpgraded
                                    ? 2
                                    : 1;
                            ReflectionStacks += tempering;
                            messages.Add($"焼入れ：反射+{tempering}。");
                        }

                        enemy.Shield = 0;
                        break;
                    case EnemyActionKind.Defense:
                        enemy.Shield = 0;
                        var defense =
                            Math.Max(0, enemy.Defense - enemy.Weakness);
                        enemy.Weakness = 0;
                        enemy.Shield += defense;
                        messages.Add(
                            $"{enemy.Name}はシールドを{defense}獲得。");
                        break;
                    case EnemyActionKind.Weaken:
                        enemy.Shield = 0;
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

            UpdateVictoryState();
            return messages;
        }

        private bool IsPersistentActive(CardKind kind)
        {
            return _player.Deck.Any(
                card => card.Kind == kind && card.PersistentActivated);
        }

        private CardInstance GetActivePersistent(CardKind kind)
        {
            var card = _player.Deck.FirstOrDefault(
                candidate =>
                    candidate.Kind == kind &&
                    candidate.PersistentActivated);
            if (card == null)
            {
                throw new InvalidOperationException(
                    $"{CardCatalog.GetName(kind)}は発動していません。");
            }

            return card;
        }

        private IReadOnlyList<int> GetAttackTargets(
            int targetIndex,
            bool forceArea)
        {
            if (forceArea)
            {
                return GetLivingEnemyIndices();
            }

            if (targetIndex < 0 || targetIndex >= Enemies.Count)
            {
                throw new InvalidOperationException(
                    "攻撃対象を選択してください。");
            }

            if (!Enemies[targetIndex].IsAlive)
            {
                return Array.Empty<int>();
            }

            return new[] { targetIndex };
        }

        private List<int> GetLivingEnemyIndices()
        {
            return Enemies
                .Select((enemy, index) => new { enemy, index })
                .Where(pair => pair.enemy.IsAlive)
                .Select(pair => pair.index)
                .ToList();
        }

        private void ValidateTarget(int targetIndex)
        {
            if (targetIndex < 0 ||
                targetIndex >= Enemies.Count ||
                !Enemies[targetIndex].IsAlive)
            {
                throw new InvalidOperationException(
                    "攻撃対象を選択してください。");
            }
        }

        private void LoseShield(int amount)
        {
            var lost = Math.Min(_player.Shield, Math.Max(0, amount));
            _player.Shield -= lost;
            ShieldLostTotal += lost;
        }

        private void UpdateVictoryState()
        {
            if (Enemies.All(enemy => !enemy.IsAlive))
            {
                Phase = BattlePhase.Victory;
            }
        }

        private static int DivideCeiling(int numerator, int denominator)
        {
            if (denominator <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(denominator));
            }

            return numerator <= 0
                ? 0
                : (numerator + denominator - 1) / denominator;
        }

        private static IReadOnlyDictionary<EtherType, int> CopyPool(
            IReadOnlyDictionary<EtherType, int> source)
        {
            return source.ToDictionary(pair => pair.Key, pair => pair.Value);
        }
    }
}
