using System;
using System.Collections.Generic;
using System.Linq;

namespace LostPage
{
    public sealed class EtherPool
    {
        private readonly Random _random;
        private readonly List<EtherType> _lastRefilledEtherTypes =
            new List<EtherType>();

        public EtherPool(IEnumerable<CardInstance> deck, Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));

            foreach (EtherType type in Enum.GetValues(typeof(EtherType)))
            {
                Unused[type] = 0;
                Current[type] = 0;
                Spent[type] = 0;
            }

            foreach (var card in deck)
            {
                foreach (var cost in CardCatalog.GetCost(card))
                {
                    Unused[cost.Key] += cost.Value;
                }
            }
        }

        public Dictionary<EtherType, int> Unused { get; } =
            new Dictionary<EtherType, int>();

        public Dictionary<EtherType, int> Current { get; } =
            new Dictionary<EtherType, int>();

        public Dictionary<EtherType, int> Spent { get; } =
            new Dictionary<EtherType, int>();

        public IReadOnlyList<EtherType> LastRefilledEtherTypes =>
            _lastRefilledEtherTypes;

        public int LastRefillDrawIndex { get; private set; } = -1;

        public int CurrentTotal => Current.Values.Sum();

        public int GetTotal(EtherType type)
        {
            return Unused[type] + Current[type] + Spent[type];
        }

        public bool CanPay(IReadOnlyDictionary<EtherType, int> cost)
        {
            return cost.All(pair => Current[pair.Key] >= pair.Value);
        }

        public void Pay(IReadOnlyDictionary<EtherType, int> cost)
        {
            if (!CanPay(cost))
            {
                throw new InvalidOperationException("必要なエーテルが不足しています。");
            }

            foreach (var pair in cost)
            {
                Current[pair.Key] -= pair.Value;
                Spent[pair.Key] += pair.Value;
            }
        }

        public IReadOnlyDictionary<EtherType, int> PayAllCurrent()
        {
            var paid = Current.ToDictionary(pair => pair.Key, pair => pair.Value);
            foreach (EtherType type in Enum.GetValues(typeof(EtherType)))
            {
                Spent[type] += Current[type];
                Current[type] = 0;
            }

            return paid;
        }

        public List<EtherType> Draw(int count)
        {
            _lastRefilledEtherTypes.Clear();
            LastRefillDrawIndex = -1;
            var drawn = new List<EtherType>();
            for (var index = 0; index < count; index++)
            {
                if (Unused.Values.Sum() == 0)
                {
                    RecordRefill(drawn.Count);
                    RefillUnused();
                }

                var unusedTotal = Unused.Values.Sum();
                if (unusedTotal == 0)
                {
                    break;
                }

                var selectedIndex = _random.Next(unusedTotal);
                foreach (EtherType type in Enum.GetValues(typeof(EtherType)))
                {
                    if (selectedIndex < Unused[type])
                    {
                        Unused[type]--;
                        Current[type]++;
                        drawn.Add(type);
                        break;
                    }

                    selectedIndex -= Unused[type];
                }
            }

            return drawn;
        }

        public EtherType ConvertCurrentToRandomTypeAndAdd(int addedCount)
        {
            var selected = (EtherType)_random.Next(
                Enum.GetValues(typeof(EtherType)).Length);
            var total = CurrentTotal + Math.Max(0, addedCount);
            foreach (EtherType type in Enum.GetValues(typeof(EtherType)))
            {
                Current[type] = type == selected ? total : 0;
            }

            return selected;
        }

        public void EndTurn(
            IReadOnlyList<EtherType> carried,
            int carryLimit)
        {
            var requiredCarryCount = Math.Min(
                Math.Max(0, carryLimit),
                CurrentTotal);
            if (carried == null || carried.Count != requiredCarryCount)
            {
                throw new InvalidOperationException(
                    $"持ち越すエーテルを{requiredCarryCount}個選択してください。");
            }

            var selectedCounts = Enum.GetValues(typeof(EtherType))
                .Cast<EtherType>()
                .ToDictionary(type => type, _ => 0);

            foreach (var type in carried)
            {
                selectedCounts[type]++;
                if (selectedCounts[type] > Current[type])
                {
                    throw new InvalidOperationException(
                        "所持数を超えるエーテルは持ち越せません。");
                }
            }

            foreach (EtherType type in Enum.GetValues(typeof(EtherType)))
            {
                var discarded = Current[type] - selectedCounts[type];
                Current[type] = selectedCounts[type];
                Spent[type] += discarded;
            }
        }

        public IReadOnlyList<EtherType> GetCurrentTokens()
        {
            var result = new List<EtherType>();
            foreach (EtherType type in Enum.GetValues(typeof(EtherType)))
            {
                for (var index = 0; index < Current[type]; index++)
                {
                    result.Add(type);
                }
            }

            return result;
        }

        private void RecordRefill(int drawIndex)
        {
            if (Spent.Values.Sum() == 0)
            {
                return;
            }

            LastRefillDrawIndex = drawIndex;
            foreach (EtherType type in Enum.GetValues(typeof(EtherType)))
            {
                for (var index = 0; index < Spent[type]; index++)
                {
                    _lastRefilledEtherTypes.Add(type);
                }
            }
        }

        private void RefillUnused()
        {
            foreach (EtherType type in Enum.GetValues(typeof(EtherType)))
            {
                Unused[type] += Spent[type];
                Spent[type] = 0;
            }
        }
    }
}
