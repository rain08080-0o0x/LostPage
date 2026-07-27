using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LostPage
{
    public sealed class LostPageGame : MonoBehaviour
    {
        private enum TutorialStep
        {
            None,
            Attack,
            EndTurn,
            Carry,
            Charge
        }

        private enum CardSortMode
        {
            UsableFirst,
            Category,
            Acquired
        }

        private enum CardFilterMode
        {
            All,
            Attack,
            Defense,
            Charge,
            Persistent,
            Special
        }

        private sealed class EtherTransferVisual
        {
            public EtherType Type;
            public RectTransform Rect;
            public Vector3 Start;
            public Vector3 Control;
            public Vector3 End;
            public float Elapsed;
        }

        private static readonly string[] TutorialPageTitles =
        {
            "戦闘画面とエーテル",
            "攻撃と敵の選択",
            "自分に使うカード",
            "ターン終了と持ち越し",
            "マップ・霧とデッキ強化"
        };

        private static readonly string[] TutorialPageDescriptions =
        {
            "左は未使用、右は使用済み、画面下は今の手番で使えるエーテルです。",
            "攻撃カードは対象の敵へドラッグします。敵を選んで使用ボタンを押しても使えます。",
            "防御・チャージ・持続カードは、中央のカード説明欄へドラッグして発動します。",
            "ターンは自動では終わりません。終了時に残ったエーテルを基本2個、鞄があればさらに持ち越せます。",
            "分岐を選んで進みます。移動ごとに下から霧が1行迫り、霧の中へ入るとHPが減ります。"
        };

        private Canvas _canvas;
        private RectTransform _screenRoot;
        private RunSession _session;
        private BattleModel _battle;
        private CardInstance _selectedCard;
        private int _selectedEnemyIndex;
        private string _message;
        private float _mapScrollX = 0.5f;
        private float _mapScrollY;
        private float _cardScrollX;
        private float _shopScrollX;
        private CardCategory _shopCategory = CardCategory.Attack;
        private CardCategory _ownedCardCategory = CardCategory.Attack;
        private RectTransform _dragGhost;
        private RectTransform _tutorialBookOverlay;
        private int _tutorialPageIndex;
        private bool _tutorialOfferedThisSession;
        private bool _tutorialCompletedThisSession;
        private TutorialStep _tutorialStep;
        private int _remainingCardRewardSelections;
        private CardSortMode _cardSortMode = CardSortMode.UsableFirst;
        private CardFilterMode _cardFilterMode = CardFilterMode.All;
        private readonly List<RectTransform> _enemyRects =
            new List<RectTransform>();
        private readonly List<Text> _enemyLabels = new List<Text>();
        private readonly List<RectTransform> _enemyHpBarFills =
            new List<RectTransform>();
        private readonly Dictionary<EtherType, RectTransform>
            _unusedEtherAnchors =
                new Dictionary<EtherType, RectTransform>();
        private readonly Dictionary<EtherType, RectTransform>
            _currentEtherAnchors =
                new Dictionary<EtherType, RectTransform>();
        private readonly Dictionary<EtherType, RectTransform>
            _spentEtherAnchors =
                new Dictionary<EtherType, RectTransform>();
        private readonly Dictionary<EtherType, Text> _unusedEtherCountTexts =
            new Dictionary<EtherType, Text>();
        private readonly Dictionary<EtherType, Text> _currentEtherCountTexts =
            new Dictionary<EtherType, Text>();
        private readonly Dictionary<EtherType, Text> _spentEtherCountTexts =
            new Dictionary<EtherType, Text>();
        private readonly Dictionary<EtherType, int> _visibleUnusedEther =
            new Dictionary<EtherType, int>();
        private readonly Dictionary<EtherType, int> _visibleCurrentEther =
            new Dictionary<EtherType, int>();
        private readonly Dictionary<EtherType, int> _visibleSpentEther =
            new Dictionary<EtherType, int>();
        private Text _unusedEtherTitle;
        private Text _currentEtherHeader;
        private Text _spentEtherTitle;
        private bool _isResolvingAction;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            UiFactory.EnsureEventSystem();
            _canvas = UiFactory.CreateCanvas("LostPageCanvas");
            DontDestroyOnLoad(_canvas.gameObject);
            UiFactory.CreatePanel(
                "Backdrop",
                _canvas.transform,
                Vector2.zero,
                Vector2.one,
                UiFactory.Background);
        }

        private void Start()
        {
            StartNewRun();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            StartCoroutine(CaptureForValidationIfRequested());
#endif
        }

        private void StartNewRun()
        {
            _session = new RunSession();
            _battle = null;
            _selectedCard = null;
            _selectedEnemyIndex = 0;
            _remainingCardRewardSelections = 0;
            ResetCardBrowsingState();
            _isResolvingAction = false;
            _message = "接続されているステージを選択してください。";
            ShowCarryToolSelection();
        }

        private void ShowCarryToolSelection()
        {
            var root = CreateScreen("CarryToolSelection");
            UiFactory.CreateText(
                "Title",
                root,
                new Vector2(0.08f, 0.80f),
                new Vector2(0.92f, 0.94f),
                "持ち込み道具を選択",
                46,
                TextAnchor.MiddleCenter,
                UiFactory.Accent);
            UiFactory.CreateText(
                "Prompt",
                root,
                new Vector2(0.10f, 0.70f),
                new Vector2(0.90f, 0.80f),
                "このランで使用する道具を1つ選んでください",
                28);

            var kinds = _session.CreateStartingToolChoices();
            for (var index = 0; index < kinds.Count; index++)
            {
                var kind = kinds[index];
                var left = 0.08f + index * 0.30f;
                UiFactory.CreateButton(
                    $"CarryTool_{kind}",
                    root,
                    new Vector2(left, 0.22f),
                    new Vector2(left + 0.24f, 0.66f),
                    $"【{CarryToolCatalog.GetRarityLabel(kind)}】\n" +
                    $"{CarryToolCatalog.GetName(kind)}\n\n" +
                    CarryToolCatalog.GetDescription(kind),
                    () =>
                    {
                        _session.ClaimTool(
                            kind,
                            CarryToolRarity.Normal);
                        _message =
                            $"{CarryToolCatalog.GetName(kind)}を持ち込みました。";
                        ShowMap();
                    },
                    GetCarryToolColor(kind),
                    27);
            }
        }

        private RectTransform CreateScreen(string name)
        {
            CancelInvoke();
            _tutorialBookOverlay = null;
            if (_screenRoot != null)
            {
                Destroy(_screenRoot.gameObject);
            }

            _screenRoot = UiFactory.CreateRect(
                name,
                _canvas.transform,
                Vector2.zero,
                Vector2.one);

            var safeArea = Screen.safeArea;
            if (Screen.width > 0 && Screen.height > 0)
            {
                _screenRoot.anchorMin = new Vector2(
                    safeArea.xMin / Screen.width,
                    safeArea.yMin / Screen.height);
                _screenRoot.anchorMax = new Vector2(
                    safeArea.xMax / Screen.width,
                    safeArea.yMax / Screen.height);
                _screenRoot.offsetMin = Vector2.zero;
                _screenRoot.offsetMax = Vector2.zero;
            }

            return _screenRoot;
        }

        private void ShowMap()
        {
            var root = CreateScreen("MapScreen");
            UiFactory.CreateText(
                "Title",
                root,
                new Vector2(0.03f, 0.91f),
                new Vector2(0.58f, 0.99f),
                $"LOST PAGE － {_session.CurrentLayer}層 分岐マップ",
                40,
                TextAnchor.MiddleLeft,
                UiFactory.Accent);

            UiFactory.CreateText(
                "Status",
                root,
                new Vector2(0.58f, 0.91f),
                new Vector2(0.97f, 0.99f),
                $"HP {_session.Player.Hp}/{_session.Player.MaxHp}　" +
                $"所持金 {_session.Player.Gold}G　" +
                $"カード {_session.Player.Deck.Count}枚\n" +
                $"{GetFogStatusText()}　{GetOwnedToolSummary()}",
                22,
                TextAnchor.MiddleRight);

            var scroll = UiFactory.CreateScrollView(
                "MapScroll",
                root,
                new Vector2(0.03f, 0.12f),
                new Vector2(0.97f, 0.90f),
                true,
                true,
                out var content);
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.zero;
            content.pivot = Vector2.zero;
            content.sizeDelta = new Vector2(
                _session.MapContentWidth,
                _session.MapContentHeight);

            DrawMapEdges(content);
            DrawFog(content);
            DrawMapNodes(content);

            scroll.normalizedPosition = new Vector2(_mapScrollX, _mapScrollY);
            scroll.onValueChanged.AddListener(value =>
            {
                _mapScrollX = value.x;
                _mapScrollY = value.y;
            });

            UiFactory.CreateButton(
                "OwnedCards",
                root,
                new Vector2(0.65f, 0.025f),
                new Vector2(0.79f, 0.095f),
                "所持カード",
                ShowOwnedCardsOverlay,
                new Color32(76, 82, 103, 255),
                20);

            UiFactory.CreateButton(
                "OwnedTools",
                root,
                new Vector2(0.80f, 0.025f),
                new Vector2(0.97f, 0.095f),
                "所持道具を見る",
                ShowOwnedToolsOverlay,
                new Color32(76, 82, 103, 255),
                22);

            UiFactory.CreateText(
                "Message",
                root,
                new Vector2(0.21f, 0.015f),
                new Vector2(0.64f, 0.105f),
                _message,
                27,
                TextAnchor.MiddleCenter);

            UiFactory.CreateButton(
                "HowToPlay",
                root,
                new Vector2(0.03f, 0.025f),
                new Vector2(0.20f, 0.095f),
                "遊び方",
                () => ShowTutorialBook(0),
                new Color32(72, 81, 104, 255),
                24);
        }

        private void DrawMapEdges(RectTransform content)
        {
            var nodes = _session.Nodes.ToDictionary(node => node.Id);
            foreach (var node in nodes.Values)
            {
                foreach (var neighborId in node.Neighbors)
                {
                    if (neighborId <= node.Id)
                    {
                        continue;
                    }

                    var neighbor = nodes[neighborId];
                    var from = new Vector2(node.X, node.Y);
                    var to = new Vector2(neighbor.X, neighbor.Y);
                    var direction = to - from;
                    var edge = UiFactory.CreatePanel(
                        $"Edge_{node.Id}_{neighborId}",
                        content,
                        Vector2.zero,
                        Vector2.zero,
                        node.Visited && neighbor.Visited
                            ? new Color32(113, 108, 92, 255)
                            : new Color32(57, 60, 70, 255));
                    edge.pivot = new Vector2(0.5f, 0.5f);
                    edge.anchoredPosition = (from + to) * 0.5f;
                    edge.sizeDelta = new Vector2(direction.magnitude, 10);
                    edge.localEulerAngles = new Vector3(
                        0,
                        0,
                        Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
                }
            }
        }

        private void DrawFog(RectTransform content)
        {
            var boundaryY = Mathf.Clamp(
                _session.FogBoundaryY,
                0f,
                content.sizeDelta.y);
            if (boundaryY <= 0f)
            {
                return;
            }

            var fogArea = UiFactory.CreatePanel(
                "FogArea",
                content,
                Vector2.zero,
                Vector2.zero,
                new Color32(55, 70, 79, 150));
            fogArea.pivot = Vector2.zero;
            fogArea.anchoredPosition = Vector2.zero;
            fogArea.sizeDelta =
                new Vector2(content.sizeDelta.x, boundaryY);
            fogArea.GetComponent<Image>().raycastTarget = false;

            var border = UiFactory.CreatePanel(
                "FogBorder",
                content,
                Vector2.zero,
                Vector2.zero,
                new Color32(181, 205, 211, 235));
            border.pivot = new Vector2(0f, 0.5f);
            border.anchoredPosition = new Vector2(0f, boundaryY);
            border.sizeDelta = new Vector2(content.sizeDelta.x, 14f);
            border.GetComponent<Image>().raycastTarget = false;

            var label = UiFactory.CreateText(
                "FogBorderLabel",
                content,
                Vector2.zero,
                Vector2.zero,
                $"霧の境界　進入時 {_session.CurrentFogDamage}ダメージ",
                22,
                TextAnchor.MiddleCenter,
                new Color32(220, 231, 234, 255));
            label.rectTransform.pivot = new Vector2(0f, 0f);
            label.rectTransform.anchoredPosition =
                new Vector2(20f, boundaryY + 12f);
            label.rectTransform.sizeDelta = new Vector2(430f, 44f);
            label.raycastTarget = false;
        }

        private void DrawMapNodes(RectTransform content)
        {
            foreach (var node in _session.Nodes.OrderBy(node => node.Id))
            {
                var localNode = node;
                var canTravel = _session.CanTravelTo(localNode.Id);
                var background = GetMapNodeColor(localNode, canTravel);
                var button = UiFactory.CreateButton(
                    $"Node_{localNode.Id}",
                    content,
                    Vector2.zero,
                    Vector2.zero,
                    $"{GetStageIcon(localNode.Kind)} {localNode.Name}\n" +
                    GetNodeStateText(localNode),
                    () => OnMapNodePressed(localNode.Id),
                    background,
                    24);
                var rect = button.GetComponent<RectTransform>();
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(localNode.X, localNode.Y);
                rect.sizeDelta = new Vector2(235, 105);
                button.interactable = canTravel;

                if (localNode.Id == _session.CurrentNodeId)
                {
                    var marker = UiFactory.CreateText(
                        "Current",
                        rect,
                        new Vector2(0.30f, 1.02f),
                        new Vector2(0.70f, 1.34f),
                        "現在地",
                        22,
                        TextAnchor.MiddleCenter,
                        UiFactory.Accent);
                    marker.raycastTarget = false;
                }
            }
        }

        private void OnMapNodePressed(int nodeId)
        {
            var firstVisit = _session.TravelTo(nodeId);
            ResetCardBrowsingState();
            var fogDamage = _session.LastTravelFogDamage;
            if (fogDamage <= 0)
            {
                ResolveMapArrival(firstVisit);
                return;
            }

            _message =
                $"霧の中へ入り、HPが{fogDamage}減少しました。" +
                $"（HP {_session.Player.Hp}/{_session.Player.MaxHp}）";
            if (_session.Player.Hp <= 0)
            {
                ShowDefeat(
                    $"霧の中で{fogDamage}ダメージを受け、" +
                    "プレイヤーのHPが0になった");
                return;
            }

            ShowMap();
            StartCoroutine(ShakeThenShowFogDamage(firstVisit, fogDamage));
        }

        private void ResolveMapArrival(bool firstVisit)
        {
            var node = _session.CurrentNode;
            _message = firstVisit
                ? $"{node.Name}へ移動しました。"
                : $"{node.Name}へ戻りました。ここでは何も起こりません。";

            if (!firstVisit)
            {
                if (node.Kind == StageKind.Shop)
                {
                    _message = "商人のもとへ戻りました。";
                    ShowShop();
                    return;
                }

                ShowMap();
                return;
            }

            switch (node.Kind)
            {
                case StageKind.Battle:
                case StageKind.Boss:
                    StartBattle();
                    break;
                case StageKind.Fountain:
                    ShowFountain();
                    break;
                case StageKind.Shop:
                    ShowShop();
                    break;
                case StageKind.RandomEvent:
                    ShowRandomEvent();
                    break;
                case StageKind.Reward:
                    ShowRewardStage();
                    break;
                case StageKind.Tool:
                    ShowToolStage();
                    break;
                default:
                    node.Cleared = true;
                    ShowMap();
                    break;
            }
        }

        private IEnumerator ShakeThenShowFogDamage(
            bool firstVisit,
            int fogDamage)
        {
            var blocker = CreateOverlay("FogDamageBlocker");
            blocker.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            var originalPosition = _screenRoot.anchoredPosition;
            const float duration = 0.25f;
            const float amplitude = 18f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var strength = 1f - Mathf.Clamp01(elapsed / duration);
                _screenRoot.anchoredPosition =
                    originalPosition +
                    new Vector2(
                        Mathf.Sin(elapsed * 91f),
                        Mathf.Cos(elapsed * 73f)) *
                    amplitude *
                    strength;
                yield return null;
            }

            _screenRoot.anchoredPosition = originalPosition;
            if (blocker != null)
            {
                Destroy(blocker.gameObject);
            }

            ShowMessageDialog(
                "霧ダメージ",
                $"霧の中へ入り、HPが{fogDamage}減少しました。\n" +
                $"現在HP {_session.Player.Hp}/{_session.Player.MaxHp}",
                () => ResolveMapArrival(firstVisit));
        }

        private void StartBattle()
        {
            _battle = _session.CreateBattleForCurrentNode();
            _selectedCard = _session.Player.Deck.FirstOrDefault();
            _selectedEnemyIndex = FindFirstAliveEnemy();
            _cardScrollX = 0f;
            var startMessages = new List<string>();
            if (!string.IsNullOrEmpty(_battle.BattleStartMessage))
            {
                startMessages.Add(_battle.BattleStartMessage);
            }

            if (!string.IsNullOrEmpty(_battle.TurnStartMessage))
            {
                startMessages.Add(_battle.TurnStartMessage);
            }

            startMessages.Add(
                $"ターン1：エーテルを" +
                $"{_session.Player.GetEtherDrawCount()}個取得。");
            _message = string.Join("\n", startMessages);
            ShowBattle();
            var showTutorialAfterDraw =
                !_tutorialCompletedThisSession &&
                !_tutorialOfferedThisSession;
            StartTurnEtherDrawAnimation(
                () =>
                {
                    if (showTutorialAfterDraw)
                    {
                        ShowTutorialWelcome();
                    }
                });
        }

        private void ShowBattle()
        {
            EnsureTutorialCardCostForCurrentStep();
            if (_selectedCard == null ||
                !ShouldDisplayCardInCurrentFilter(_selectedCard))
            {
                _selectedCard = GetSortedVisibleCards().FirstOrDefault();
            }

            var root = CreateScreen("BattleScreen");
            ResetBattleVisualReferences();
            UiFactory.CreateText(
                "BattleTitle",
                root,
                new Vector2(0.03f, 0.94f),
                new Vector2(0.52f, 0.995f),
                $"{_session.CurrentLayer}層　{_session.CurrentNode.Name}　" +
                $"ターン {_battle.TurnNumber}",
                34,
                TextAnchor.MiddleLeft,
                UiFactory.Accent);

            _currentEtherHeader = UiFactory.CreateText(
                "CurrentEther",
                root,
                new Vector2(0.52f, 0.94f),
                new Vector2(0.75f, 0.995f),
                $"手番エーテル {_battle.Pool.CurrentTotal}　" +
                GetBattleToolStatus(),
                19,
                TextAnchor.MiddleRight);

            UiFactory.CreateButton(
                "BattleOwnedTools",
                root,
                new Vector2(0.76f, 0.945f),
                new Vector2(0.85f, 0.993f),
                "道具",
                ShowOwnedToolsOverlay,
                new Color32(76, 82, 103, 255),
                18);

            UiFactory.CreateButton(
                "HowToPlay",
                root,
                new Vector2(0.86f, 0.945f),
                new Vector2(0.97f, 0.993f),
                "遊び方",
                () => ShowTutorialBook(0),
                new Color32(72, 81, 104, 255),
                18);

            if (_battle.Phase == BattlePhase.PlayerTurn && !_battle.HasUsableCard)
            {
                _message =
                    "使用できるカードがありません。" +
                    "確認後に「ターン終了」を押してください。";
            }

            DrawEnemies(root);
            DrawCardDetails(root);
            DrawCardRow(root);
            DrawPlayerStatus(root);
            DrawTutorialGuide(root);
        }

        private void DrawEnemies(RectTransform root)
        {
            var panel = UiFactory.CreatePanel(
                "Enemies",
                root,
                new Vector2(0.03f, 0.74f),
                new Vector2(0.97f, 0.935f),
                UiFactory.Panel);

            _unusedEtherTitle = DrawEtherPoolPanel(
                panel,
                "UnusedPool",
                new Vector2(0.015f, 0.08f),
                new Vector2(0.195f, 0.92f),
                "未使用エーテル",
                _battle.Pool.Unused,
                _unusedEtherAnchors,
                _unusedEtherCountTexts,
                _visibleUnusedEther);
            _spentEtherTitle = DrawEtherPoolPanel(
                panel,
                "SpentPool",
                new Vector2(0.805f, 0.08f),
                new Vector2(0.985f, 0.92f),
                "使用済みエーテル",
                _battle.Pool.Spent,
                _spentEtherAnchors,
                _spentEtherCountTexts,
                _visibleSpentEther);

            var count = _battle.Enemies.Count;
            var width = Mathf.Min(0.25f, 0.52f / count);
            for (var index = 0; index < count; index++)
            {
                var enemyIndex = index;
                var enemy = _battle.Enemies[index];
                var center =
                    0.5f +
                    (index - (count - 1) * 0.5f) * (width + 0.025f);
                var selected = index == _selectedEnemyIndex && enemy.IsAlive;
                Color background = !enemy.IsAlive
                    ? UiFactory.Disabled
                    : selected
                        ? (Color)new Color32(139, 93, 63, 255)
                        : new Color32(83, 65, 67, 255);
                var label = enemy.IsAlive
                    ? $"{enemy.Name}\nHP {enemy.Hp}/{enemy.MaxHp}　盾 {enemy.Shield}\n" +
                      $"次：{GetEnemyIntent(enemy)}" +
                      GetEnemyStatusText(enemy)
                    : $"{enemy.Name}\n撃破";
                var button = UiFactory.CreateButton(
                    $"Enemy_{index}",
                    panel,
                    new Vector2(center - width * 0.5f, 0.10f),
                    new Vector2(center + width * 0.5f, 0.92f),
                    label,
                    () =>
                    {
                        _selectedEnemyIndex = enemyIndex;
                        ShowBattle();
                    },
                    background,
                    24);
                button.gameObject
                    .AddComponent<EnemyCardDropTarget>()
                    .Configure(enemyIndex);
                button.interactable = enemy.IsAlive;
                _enemyRects.Add(button.GetComponent<RectTransform>());
                _enemyLabels.Add(
                    button.transform.Find("Label").GetComponent<Text>());
                UiFactory.CreateBar(
                    "HpBar",
                    button.transform,
                    new Vector2(0.08f, 0.05f),
                    new Vector2(0.92f, 0.12f),
                    (float)enemy.Hp / enemy.MaxHp,
                    UiFactory.Red);
                _enemyHpBarFills.Add(
                    button.transform.Find("HpBar/Fill")
                        .GetComponent<RectTransform>());
            }
        }

        private Text DrawEtherPoolPanel(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            string title,
            IReadOnlyDictionary<EtherType, int> pool,
            IDictionary<EtherType, RectTransform> anchors,
            IDictionary<EtherType, Text> countTexts,
            IDictionary<EtherType, int> visibleCounts)
        {
            var panel = UiFactory.CreatePanel(
                name,
                parent,
                anchorMin,
                anchorMax,
                new Color32(45, 48, 61, 255));
            var titleText = UiFactory.CreateText(
                "Title",
                panel,
                new Vector2(0.04f, 0.72f),
                new Vector2(0.96f, 0.98f),
                $"{title}　{pool.Values.Sum()}",
                18);

            var index = 0;
            foreach (EtherType type in Enum.GetValues(typeof(EtherType)))
            {
                var column = index % 2;
                var row = index / 2;
                var left = 0.06f + column * 0.47f;
                var bottom = row == 0 ? 0.39f : 0.06f;
                var icon = UiFactory.CreatePanel(
                    $"{name}_Icon_{type}",
                    panel,
                    new Vector2(left, bottom),
                    new Vector2(left + 0.20f, bottom + 0.26f),
                    UiFactory.GetEtherColor(type));
                var sprite = EtherTextureSet.GetSprite(type);
                var image = icon.GetComponent<Image>();
                if (sprite != null)
                {
                    image.sprite = sprite;
                    image.preserveAspect = true;
                    image.color = Color.white;
                }

                var countText = UiFactory.CreateText(
                    $"{name}_Count_{type}",
                    panel,
                    new Vector2(left + 0.20f, bottom),
                    new Vector2(left + 0.43f, bottom + 0.26f),
                    pool[type].ToString(),
                    22,
                    TextAnchor.MiddleLeft);
                anchors[type] = icon;
                countTexts[type] = countText;
                visibleCounts[type] = pool[type];
                index++;
            }

            return titleText;
        }

        private void DrawCardDetails(RectTransform root)
        {
            var panel = UiFactory.CreatePanel(
                "CardDetails",
                root,
                new Vector2(0.03f, 0.515f),
                new Vector2(0.97f, 0.725f),
                UiFactory.Panel);
            panel.gameObject.AddComponent<CardEffectDropTarget>();

            if (_selectedCard == null)
            {
                UiFactory.CreateText(
                    "NoCard",
                    panel,
                    new Vector2(0.03f, 0.05f),
                    new Vector2(0.97f, 0.95f),
                    "カードを選択してください。",
                    28);
                return;
            }

            var usable = _battle.CanUse(_selectedCard);
            UiFactory.CreateText(
                "CardName",
                panel,
                new Vector2(0.025f, 0.56f),
                new Vector2(0.35f, 0.94f),
                $"{( _selectedCard.IsUpgraded ? "【強化済】" : string.Empty)}" +
                $"【{CardCatalog.GetRarityLabel(_selectedCard.Kind)}】" +
                CardCatalog.GetName(_selectedCard.Kind),
                34,
                TextAnchor.MiddleLeft,
                UiFactory.Accent);

            DrawCardCostIcons(panel, _selectedCard);

            UiFactory.CreateText(
                "CardDescription",
                panel,
                new Vector2(0.025f, 0.08f),
                new Vector2(0.73f, 0.59f),
                GetDetailedCardText(_selectedCard, usable),
                25,
                TextAnchor.MiddleLeft);

            var useButton = UiFactory.CreateButton(
                "UseCard",
                panel,
                new Vector2(0.76f, 0.17f),
                new Vector2(0.97f, 0.83f),
                CardCatalog.RequiresEnemyTarget(_selectedCard.Kind)
                    ? "選択中の敵へ使用"
                    : CardCatalog.IsAttack(_selectedCard.Kind)
                        ? "敵全体へ使用"
                    : "カードを使用",
                UseSelectedCard,
                usable ? UiFactory.Green : UiFactory.Disabled,
                27);
            useButton.interactable =
                usable &&
                (!CardCatalog.RequiresEnemyTarget(_selectedCard.Kind) ||
                 IsSelectedEnemyAlive());
        }

        private void DrawCardCostIcons(
            RectTransform panel,
            CardInstance card)
        {
            UiFactory.CreateText(
                "CostLabel",
                panel,
                new Vector2(0.36f, 0.60f),
                new Vector2(0.435f, 0.90f),
                "コスト",
                22,
                TextAnchor.MiddleLeft);

            var iconIndex = 0;
            foreach (var cost in CardCatalog.GetCost(card))
            {
                for (var count = 0; count < cost.Value; count++)
                {
                    var left = 0.435f + iconIndex * 0.034f;
                    var icon = UiFactory.CreatePanel(
                        $"CostIcon_{cost.Key}_{count}",
                        panel,
                        new Vector2(left, 0.61f),
                        new Vector2(left + 0.027f, 0.89f),
                        UiFactory.GetEtherColor(cost.Key));
                    var sprite = EtherTextureSet.GetSprite(cost.Key);
                    if (sprite != null)
                    {
                        var image = icon.GetComponent<Image>();
                        image.sprite = sprite;
                        image.preserveAspect = true;
                        image.color = Color.white;
                    }

                    iconIndex++;
                }
            }
        }

        private void DrawCardRow(RectTransform root)
        {
            DrawCardFilterButton(
                root,
                "All",
                new Vector2(0.03f, 0.47f),
                new Vector2(0.095f, 0.51f),
                "全て",
                CardFilterMode.All);
            DrawCardFilterButton(
                root,
                "Attack",
                new Vector2(0.10f, 0.47f),
                new Vector2(0.18f, 0.51f),
                "攻撃",
                CardFilterMode.Attack);
            DrawCardFilterButton(
                root,
                "Defense",
                new Vector2(0.185f, 0.47f),
                new Vector2(0.265f, 0.51f),
                "防御",
                CardFilterMode.Defense);
            DrawCardFilterButton(
                root,
                "Charge",
                new Vector2(0.27f, 0.47f),
                new Vector2(0.365f, 0.51f),
                "チャージ",
                CardFilterMode.Charge);
            DrawCardFilterButton(
                root,
                "Persistent",
                new Vector2(0.37f, 0.47f),
                new Vector2(0.465f, 0.51f),
                "持続",
                CardFilterMode.Persistent);
            DrawCardFilterButton(
                root,
                "Special",
                new Vector2(0.47f, 0.47f),
                new Vector2(0.555f, 0.51f),
                "特殊",
                CardFilterMode.Special);

            UiFactory.CreateButton(
                "CardSort",
                root,
                new Vector2(0.565f, 0.47f),
                new Vector2(0.97f, 0.51f),
                $"並び順：{GetCardSortModeLabel()}",
                CycleCardSortMode,
                new Color32(72, 81, 104, 255),
                18);

            var scroll = UiFactory.CreateScrollView(
                "CardScroll",
                root,
                new Vector2(0.03f, 0.235f),
                new Vector2(0.97f, 0.466f),
                true,
                false,
                out var content);

            var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16;
            layout.padding = new RectOffset(16, 16, 12, 12);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var visibleCards = GetSortedVisibleCards();
            for (var index = 0; index < visibleCards.Count; index++)
            {
                var cardIndex = index;
                var card = visibleCards[index];
                var usable = _battle.CanUse(card);
                var selected = _selectedCard != null && _selectedCard.Id == card.Id;
                var cardColor = usable
                    ? UiFactory.GetCardColor(card.Kind)
                    : UiFactory.Disabled;
                if (selected)
                {
                    cardColor = Color.Lerp(cardColor, UiFactory.Accent, 0.34f);
                }

                var cooldownState = card.CooldownRemaining > 0
                    ? $"（残り{card.CooldownRemaining}ターン）"
                    : string.Empty;
                var cooldownLabel = GetCooldownLabel(card);
                var button = UiFactory.CreateButton(
                    $"Card_{card.Id}",
                    content,
                    Vector2.zero,
                    Vector2.zero,
                    $"#{cardIndex + 1}　" +
                    $"{(card.IsUpgraded ? "【強化済】" : string.Empty)}" +
                    $"{CardCatalog.GetName(card.Kind)}\n" +
                    $"【{CardCatalog.GetRarityLabel(card.Kind)}】\n" +
                    $"{CardCatalog.GetShortDescription(card)}\n" +
                    $"コスト：{CardCatalog.GetCostText(card)}\n" +
                    $"{cooldownLabel}{cooldownState}",
                    () =>
                    {
                        _selectedCard = card;
                        ShowBattle();
                    },
                    cardColor,
                    21);
                var layoutElement = button.gameObject.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = 250;
                layoutElement.minWidth = 250;
                layoutElement.preferredHeight = 225;
                button.gameObject.AddComponent<CardDragHandler>().Configure(
                    () => _battle != null && _battle.CanUse(card),
                    eventData => BeginCardDrag(card, eventData),
                    UpdateCardDrag,
                    eventData => EndCardDrag(card, eventData),
                    scroll,
                    scroll.GetComponent<RectTransform>());
            }

            Canvas.ForceUpdateCanvases();
            scroll.horizontalNormalizedPosition = _cardScrollX;
            scroll.onValueChanged.AddListener(value => _cardScrollX = value.x);
        }

        private void DrawCardFilterButton(
            RectTransform root,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            string label,
            CardFilterMode mode)
        {
            UiFactory.CreateButton(
                $"CardFilter_{name}",
                root,
                anchorMin,
                anchorMax,
                label,
                () => SetCardFilterMode(mode),
                _cardFilterMode == mode
                    ? new Color32(65, 117, 86, 255)
                    : new Color32(72, 81, 104, 255),
                18);
        }

        private List<CardInstance> GetSortedVisibleCards()
        {
            var cards = _session.Player.Deck
                .Where(ShouldDisplayCardInCurrentFilter);
            switch (_cardSortMode)
            {
                case CardSortMode.UsableFirst:
                    return cards
                        .OrderBy(card => _battle.CanUse(card) ? 0 : 1)
                        .ThenBy(card => CardCatalog.GetCategory(card.Kind))
                        .ThenBy(card => card.Id)
                        .ToList();
                case CardSortMode.Category:
                    return cards
                        .OrderBy(card => CardCatalog.GetCategory(card.Kind))
                        .ThenBy(card => card.Id)
                        .ToList();
                case CardSortMode.Acquired:
                    return cards
                        .OrderBy(card => card.Id)
                        .ToList();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SetCardFilterMode(CardFilterMode mode)
        {
            _cardFilterMode = mode;
            _cardScrollX = 0f;
            ShowBattle();
        }

        private bool ShouldDisplayCardInCurrentFilter(CardInstance card)
        {
            if (!ShouldDisplayCard(card))
            {
                return false;
            }

            switch (_cardFilterMode)
            {
                case CardFilterMode.All:
                    return true;
                case CardFilterMode.Attack:
                    return CardCatalog.GetCategory(card.Kind) ==
                           CardCategory.Attack;
                case CardFilterMode.Defense:
                    return CardCatalog.GetCategory(card.Kind) ==
                           CardCategory.Defense;
                case CardFilterMode.Charge:
                    return CardCatalog.GetCategory(card.Kind) ==
                           CardCategory.Charge;
                case CardFilterMode.Persistent:
                    return CardCatalog.GetCategory(card.Kind) ==
                           CardCategory.Persistent;
                case CardFilterMode.Special:
                    return CardCatalog.GetCategory(card.Kind) ==
                           CardCategory.Special;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private string GetCardSortModeLabel()
        {
            switch (_cardSortMode)
            {
                case CardSortMode.UsableFirst:
                    return "使用可能優先";
                case CardSortMode.Category:
                    return "種類別";
                case CardSortMode.Acquired:
                    return "入手順";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void CycleCardSortMode()
        {
            _cardSortMode = (CardSortMode)(
                ((int)_cardSortMode + 1) %
                Enum.GetValues(typeof(CardSortMode)).Length);
            _cardScrollX = 0f;
            ShowBattle();
        }

        private void ResetCardBrowsingState()
        {
            _cardFilterMode = CardFilterMode.All;
            _cardSortMode = CardSortMode.UsableFirst;
            _cardScrollX = 0f;
        }

        private static bool ShouldDisplayCard(CardInstance card)
        {
            return !CardCatalog.IsPersistent(card.Kind) ||
                   !card.PersistentActivated;
        }

        private void DrawPlayerStatus(RectTransform root)
        {
            var panel = UiFactory.CreatePanel(
                "PlayerStatus",
                root,
                new Vector2(0.03f, 0.025f),
                new Vector2(0.69f, 0.22f),
                UiFactory.Panel);

            UiFactory.CreateText(
                "PlayerHp",
                panel,
                new Vector2(0.03f, 0.64f),
                new Vector2(0.48f, 0.96f),
                $"HP {_session.Player.Hp}/{_session.Player.MaxHp}　" +
                $"シールド {_session.Player.Shield}",
                27,
                TextAnchor.MiddleLeft);
            UiFactory.CreateBar(
                "PlayerHpBar",
                panel,
                new Vector2(0.03f, 0.54f),
                new Vector2(0.48f, 0.64f),
                (float)_session.Player.Hp / _session.Player.MaxHp,
                UiFactory.Green);

            UiFactory.CreateText(
                "EtherTitle",
                panel,
                new Vector2(0.03f, 0.24f),
                new Vector2(0.12f, 0.49f),
                "エーテル",
                22,
                TextAnchor.MiddleLeft);

            var etherIndex = 0;
            foreach (EtherType type in Enum.GetValues(typeof(EtherType)))
            {
                var left = 0.12f + etherIndex * 0.087f;
                var icon = UiFactory.CreatePanel(
                    $"CurrentEtherIcon_{type}",
                    panel,
                    new Vector2(left, 0.27f),
                    new Vector2(left + 0.026f, 0.45f),
                    UiFactory.GetEtherColor(type));
                var sprite = EtherTextureSet.GetSprite(type);
                var image = icon.GetComponent<Image>();
                if (sprite != null)
                {
                    image.sprite = sprite;
                    image.preserveAspect = true;
                    image.color = Color.white;
                }

                var countText = UiFactory.CreateText(
                    $"CurrentEtherCount_{type}",
                    panel,
                    new Vector2(left + 0.028f, 0.23f),
                    new Vector2(left + 0.082f, 0.49f),
                    $"{CardCatalog.GetEtherName(type)} " +
                    $"{_battle.Pool.Current[type]}",
                    20,
                    TextAnchor.MiddleLeft,
                    UiFactory.GetEtherColor(type));
                _currentEtherAnchors[type] = icon;
                _currentEtherCountTexts[type] = countText;
                _visibleCurrentEther[type] = _battle.Pool.Current[type];
                etherIndex++;
            }

            var statuses = new List<(string Label, Color Color)>();
            if (_battle.PendingCharge > 0)
            {
                statuses.Add((
                    $"チャージ\n+{_battle.PendingCharge}",
                    UiFactory.Yellow));
            }

            if (_battle.PendingWeaken > 0)
            {
                statuses.Add((
                    $"弱体\n-{_battle.PendingWeaken}",
                    UiFactory.Purple));
            }

            if (_battle.PersistentStacks > 0)
            {
                statuses.Add((
                    $"持続 x{_battle.PersistentStacks}\n" +
                    $"{_battle.AttackCardsTowardBonus}/3",
                    UiFactory.Blue));
            }

            if (_battle.PermanentAttackBonus > 0)
            {
                statuses.Add((
                    $"攻撃力\n+{_battle.PermanentAttackBonus}",
                    UiFactory.Red));
            }

            if (_battle.DefensePowerBonus > 0)
            {
                statuses.Add((
                    $"防御力\n+{_battle.DefensePowerBonus}",
                    UiFactory.Blue));
            }

            if (_battle.PenetrationStacks > 0)
            {
                statuses.Add((
                    $"貫通\n{_battle.PenetrationStacks}回",
                    UiFactory.Purple));
            }

            if (_battle.DefenseRetentionStacks > 0)
            {
                statuses.Add((
                    $"防御維持\n{_battle.DefenseRetentionStacks}層",
                    UiFactory.Green));
            }

            if (_battle.AutoDefenseStacks > 0)
            {
                statuses.Add((
                    $"自動防御\n{_battle.AutoDefenseStacks}層",
                    UiFactory.Blue));
            }

            if (_battle.ReflectionStacks > 0)
            {
                statuses.Add((
                    $"反射\n{_battle.ReflectionStacks}",
                    UiFactory.Red));
            }

            if (_battle.PendingAttackExecutions > 1)
            {
                statuses.Add((
                    $"次回攻撃\n{_battle.PendingAttackExecutions}回",
                    UiFactory.Purple));
            }

            if (_battle.GuardCycleStacks > 0)
            {
                statuses.Add((
                    $"守護循環 x{_battle.GuardCycleStacks}\n" +
                    $"{_battle.CardsTowardAutoDefense}/3",
                    UiFactory.Purple));
            }

            foreach (var persistentCard in
                     _battle.ActivePersistentCards.Where(
                         card =>
                             card.Kind != CardKind.Persistent &&
                             card.Kind != CardKind.GuardCyclePersistent))
            {
                statuses.Add((
                    $"{CardCatalog.GetName(persistentCard.Kind)}\n発動中",
                    UiFactory.Purple));
            }

            UiFactory.CreateScrollView(
                "StatusScroll",
                panel,
                new Vector2(0.52f, 0.12f),
                new Vector2(0.98f, 0.92f),
                true,
                false,
                out var statusArea);
            var statusLayout =
                statusArea.gameObject.AddComponent<HorizontalLayoutGroup>();
            statusLayout.spacing = 10;
            statusLayout.childAlignment = TextAnchor.MiddleLeft;
            statusLayout.childControlWidth = true;
            statusLayout.childControlHeight = true;
            statusLayout.childForceExpandWidth = false;
            statusLayout.childForceExpandHeight = true;
            var statusFitter =
                statusArea.gameObject.AddComponent<ContentSizeFitter>();
            statusFitter.horizontalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            statusFitter.verticalFit =
                ContentSizeFitter.FitMode.Unconstrained;
            if (statuses.Count == 0)
            {
                var noStatus = UiFactory.CreateText(
                    "NoStatus",
                    statusArea,
                    Vector2.zero,
                    Vector2.one,
                    "バフ／デバフ：なし",
                    22,
                    TextAnchor.MiddleLeft);
                var noStatusLayout =
                    noStatus.gameObject.AddComponent<LayoutElement>();
                noStatusLayout.preferredWidth = 320;
                noStatusLayout.preferredHeight = 78;
            }
            else
            {
                for (var index = 0; index < statuses.Count; index++)
                {
                    var status = statuses[index];
                    var badge = UiFactory.CreatePanel(
                        $"Status_{index}",
                        statusArea,
                        Vector2.zero,
                        Vector2.zero,
                        status.Color);
                    var element = badge.gameObject.AddComponent<LayoutElement>();
                    element.preferredWidth = 112;
                    element.preferredHeight = 78;
                    UiFactory.CreateText(
                        "Label",
                        badge,
                        new Vector2(0.04f, 0.04f),
                        new Vector2(0.96f, 0.96f),
                        status.Label,
                        19);
                }
            }

            UiFactory.CreateText(
                "BattleLog",
                root,
                new Vector2(0.70f, 0.12f),
                new Vector2(0.97f, 0.22f),
                _message,
                21,
                TextAnchor.MiddleLeft);

            UiFactory.CreateButton(
                "EndTurn",
                root,
                new Vector2(0.73f, 0.025f),
                new Vector2(0.97f, 0.105f),
                "ターン終了",
                OnEndTurnPressed,
                new Color32(117, 87, 55, 255),
                28);
        }

        private void DrawTutorialGuide(RectTransform root)
        {
            if (_tutorialStep == TutorialStep.None)
            {
                return;
            }

            string title;
            string instruction;
            switch (_tutorialStep)
            {
                case TutorialStep.Attack:
                    title = "ガイド 1/4　攻撃";
                    instruction =
                        "攻撃カードを敵へドラッグしてください。\n" +
                        "カード選択後の使用ボタンでも進められます。";
                    break;
                case TutorialStep.EndTurn:
                    title = "ガイド 2/4　ターン終了";
                    instruction =
                        "右下の「ターン終了」を押してください。\n" +
                        "エーテルがなくても自動終了はしません。";
                    break;
                case TutorialStep.Carry:
                    title = "ガイド 3/4　持ち越し";
                    instruction =
                        $"次のターンへ残すエーテルを" +
                        $"{_session.Player.GetCarryLimit()}個まで選びます。";
                    break;
                case TutorialStep.Charge:
                    title = "ガイド 4/4　チャージ";
                    instruction =
                        "チャージカードを中央の説明欄へドラッグしてください。";
                    break;
                default:
                    return;
            }

            var panel = UiFactory.CreatePanel(
                "TutorialGuide",
                root,
                new Vector2(0.70f, 0.11f),
                new Vector2(0.97f, 0.235f),
                new Color32(53, 60, 83, 255));
            UiFactory.CreateText(
                "Title",
                panel,
                new Vector2(0.04f, 0.62f),
                new Vector2(0.72f, 0.96f),
                title,
                20,
                TextAnchor.MiddleLeft,
                UiFactory.Accent);
            UiFactory.CreateText(
                "Instruction",
                panel,
                new Vector2(0.04f, 0.06f),
                new Vector2(0.72f, 0.64f),
                instruction,
                17,
                TextAnchor.MiddleLeft);
            UiFactory.CreateButton(
                "SkipTutorial",
                panel,
                new Vector2(0.75f, 0.18f),
                new Vector2(0.96f, 0.82f),
                "スキップ",
                SkipContextTutorial,
                new Color32(91, 84, 79, 255),
                16);
        }

        private void OnEndTurnPressed()
        {
            ShowCarrySelection();
        }

        private void UseSelectedCard()
        {
            if (_isResolvingAction ||
                _selectedCard == null ||
                !_battle.CanUse(_selectedCard))
            {
                return;
            }

            if (_selectedCard.Kind == CardKind.DivineStrike)
            {
                var modes = _battle.GetDivineStrikeModes();
                if (modes.Count > 1)
                {
                    ShowDivineStrikeModeSelection(modes);
                    return;
                }

                ResolveSelectedCard(modes.FirstOrDefault());
                return;
            }

            ResolveSelectedCard(null);
        }

        private void ResolveSelectedCard(EtherType? divineMode)
        {
            var usedKind = _selectedCard.Kind;
            var paidEther = ExpandEtherCounts(
                usedKind == CardKind.DivineStrike
                    ? _battle.Pool.Current
                    : CardCatalog.GetCost(_selectedCard));
            try
            {
                _message = _battle.UseCard(
                    _selectedCard,
                    _selectedEnemyIndex,
                    divineMode);
            }
            catch (InvalidOperationException exception)
            {
                _message = exception.Message;
                ShowBattle();
                return;
            }

            var attackHits = _battle.LastAttackHits.ToList();
            _isResolvingAction = true;
            StartCoroutine(
                PlayCardResolutionThenFinish(
                    usedKind,
                    paidEther,
                    attackHits));
        }

        private IEnumerator PlayCardResolutionThenFinish(
            CardKind usedKind,
            IReadOnlyList<EtherType> paidEther,
            IReadOnlyList<AttackHitResult> attackHits)
        {
            var actionScreen = _screenRoot;
            var blocker = CreateOverlay("BattleActionBlocker");
            blocker.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            if (paidEther.Count > 0)
            {
                yield return PlayEtherTransferBatch(
                    blocker,
                    paidEther,
                    _currentEtherAnchors,
                    _spentEtherAnchors,
                    type =>
                    {
                        _visibleCurrentEther[type]--;
                        UpdateEtherDisplays();
                    },
                    type =>
                    {
                        _visibleSpentEther[type]++;
                        UpdateEtherDisplays();
                    });
            }

            if (actionScreen != _screenRoot)
            {
                if (blocker != null)
                {
                    Destroy(blocker.gameObject);
                }

                _isResolvingAction = false;
                yield break;
            }

            var frames = AttackEffectSet.GetFrames();
            if (frames.Count > 0 && attackHits.Count > 0)
            {
                if (_battle.LastAttackIsSequential)
                {
                    foreach (var hit in attackHits)
                    {
                        yield return PlayAttackEffect(
                            blocker,
                            frames,
                            new[] { hit });
                    }
                }
                else
                {
                    yield return PlayAttackEffect(
                        blocker,
                        frames,
                        attackHits);
                }
            }

            if (blocker != null)
            {
                Destroy(blocker.gameObject);
            }

            _isResolvingAction = false;
            FinishCardUse(usedKind);
        }

        private void ShowDivineStrikeModeSelection(
            IReadOnlyList<EtherType> modes)
        {
            var overlay = CreateOverlay("DivineStrikeModeSelection");
            var dialog = UiFactory.CreatePanel(
                "DivineStrikeModeDialog",
                overlay,
                new Vector2(0.20f, 0.25f),
                new Vector2(0.80f, 0.75f),
                new Color32(42, 45, 59, 255));
            UiFactory.CreateText(
                "Title",
                dialog,
                new Vector2(0.08f, 0.75f),
                new Vector2(0.92f, 0.93f),
                "神撃の効果色を選択",
                35,
                TextAnchor.MiddleCenter,
                UiFactory.Accent);
            for (var index = 0; index < modes.Count; index++)
            {
                var mode = modes[index];
                var width = 0.80f / modes.Count;
                var left = 0.10f + width * index;
                UiFactory.CreateButton(
                    $"DivineMode_{mode}",
                    dialog,
                    new Vector2(left + 0.01f, 0.30f),
                    new Vector2(left + width - 0.01f, 0.66f),
                    $"{CardCatalog.GetEtherName(mode)}\n効果",
                    () =>
                    {
                        Destroy(overlay.gameObject);
                        ResolveSelectedCard(mode);
                    },
                    UiFactory.GetEtherColor(mode),
                    28);
            }

            UiFactory.CreateButton(
                "Cancel",
                dialog,
                new Vector2(0.35f, 0.08f),
                new Vector2(0.65f, 0.22f),
                "キャンセル",
                () => Destroy(overlay.gameObject),
                new Color32(91, 84, 79, 255),
                24);
        }

        private IEnumerator PlayAttackEffect(
            RectTransform parent,
            IReadOnlyList<Sprite> frames,
            IReadOnlyList<AttackHitResult> hits)
        {
            var images = new List<Image>();
            foreach (var targetIndex in hits
                         .Select(hit => hit.TargetIndex)
                         .Distinct())
            {
                if (targetIndex < 0 || targetIndex >= _enemyRects.Count)
                {
                    continue;
                }

                var effect = UiFactory.CreatePanel(
                    $"AttackEffect_{targetIndex}",
                    parent,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    Color.white);
                effect.sizeDelta = new Vector2(170f, 170f);
                effect.position = _enemyRects[targetIndex].position;
                var image = effect.GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.sprite = frames[0];
                images.Add(image);
            }

            if (images.Count == 0)
            {
                yield break;
            }

            foreach (var hit in hits)
            {
                UpdateEnemyVisual(hit);
            }

            const float frameDuration = 1f / 12f;
            yield return new WaitForSecondsRealtime(frameDuration);
            for (var frameIndex = 1;
                 frameIndex < frames.Count;
                 frameIndex++)
            {
                foreach (var image in images)
                {
                    image.sprite = frames[frameIndex];
                }

                yield return new WaitForSecondsRealtime(frameDuration);
            }

            foreach (var image in images)
            {
                if (image != null)
                {
                    Destroy(image.gameObject);
                }
            }
        }

        private IEnumerator PlayEtherTransferBatch(
            RectTransform parent,
            IReadOnlyList<EtherType> etherTypes,
            IReadOnlyDictionary<EtherType, RectTransform> sources,
            IReadOnlyDictionary<EtherType, RectTransform> destinations,
            Action<EtherType> onDeparture,
            Action<EtherType> onArrival)
        {
            const float duration = 0.38f;
            const float stagger = 0.08f;
            var active = new List<EtherTransferVisual>();
            var nextIndex = 0;
            var batchElapsed = 0f;
            Canvas.ForceUpdateCanvases();

            while (nextIndex < etherTypes.Count || active.Count > 0)
            {
                if (parent == null)
                {
                    yield break;
                }

                var deltaTime = Mathf.Clamp(
                    Time.unscaledDeltaTime,
                    1f / 240f,
                    1f / 30f);
                batchElapsed += deltaTime;
                while (nextIndex < etherTypes.Count &&
                       batchElapsed >= nextIndex * stagger)
                {
                    var type = etherTypes[nextIndex];
                    onDeparture(type);
                    if (sources.TryGetValue(type, out var source) &&
                        destinations.TryGetValue(type, out var destination) &&
                        source != null &&
                        destination != null)
                    {
                        active.Add(
                            CreateEtherTransferVisual(
                                parent,
                                type,
                                source.position,
                                destination.position,
                                nextIndex));
                    }
                    else
                    {
                        onArrival(type);
                    }

                    nextIndex++;
                }

                for (var index = active.Count - 1; index >= 0; index--)
                {
                    var transfer = active[index];
                    transfer.Elapsed += deltaTime;
                    var progress = Mathf.Clamp01(transfer.Elapsed / duration);
                    var remaining = 1f - progress;
                    transfer.Rect.position =
                        remaining * remaining * transfer.Start +
                        2f * remaining * progress * transfer.Control +
                        progress * progress * transfer.End;
                    var scale =
                        1f + Mathf.Sin(progress * Mathf.PI) * 0.18f;
                    transfer.Rect.localScale = Vector3.one * scale;
                    if (progress < 1f)
                    {
                        continue;
                    }

                    onArrival(transfer.Type);
                    if (transfer.Rect != null)
                    {
                        Destroy(transfer.Rect.gameObject);
                    }

                    active.RemoveAt(index);
                }

                yield return null;
            }
        }

        private static EtherTransferVisual CreateEtherTransferVisual(
            RectTransform parent,
            EtherType type,
            Vector3 start,
            Vector3 end,
            int index)
        {
            var rect = UiFactory.CreatePanel(
                $"EtherTransfer_{type}_{index}",
                parent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                UiFactory.GetEtherColor(type));
            rect.sizeDelta = new Vector2(58f, 58f);
            rect.position = start;
            rect.SetAsLastSibling();
            var image = rect.GetComponent<Image>();
            image.raycastTarget = false;
            var sprite = EtherTextureSet.GetSprite(type);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.preserveAspect = true;
                image.color = Color.white;
            }

            var direction = end - start;
            var perpendicular = direction.sqrMagnitude > 0.001f
                ? new Vector3(-direction.y, direction.x, 0f).normalized
                : Vector3.up;
            var curveAmount = Mathf.Clamp(
                direction.magnitude * 0.16f,
                48f,
                120f);
            var curveDirection = index % 2 == 0 ? 1f : -0.72f;
            return new EtherTransferVisual
            {
                Type = type,
                Rect = rect,
                Start = start,
                Control =
                    (start + end) * 0.5f +
                    perpendicular * curveAmount * curveDirection,
                End = end
            };
        }

        private static List<EtherType> ExpandEtherCounts(
            IReadOnlyDictionary<EtherType, int> counts)
        {
            var result = new List<EtherType>();
            foreach (EtherType type in Enum.GetValues(typeof(EtherType)))
            {
                if (!counts.TryGetValue(type, out var count))
                {
                    continue;
                }

                for (var index = 0; index < count; index++)
                {
                    result.Add(type);
                }
            }

            return result;
        }

        private void UpdateEtherDisplays()
        {
            foreach (EtherType type in Enum.GetValues(typeof(EtherType)))
            {
                if (_unusedEtherCountTexts.TryGetValue(
                        type,
                        out var unusedText) &&
                    unusedText != null)
                {
                    unusedText.text = _visibleUnusedEther[type].ToString();
                }

                if (_currentEtherCountTexts.TryGetValue(
                        type,
                        out var currentText) &&
                    currentText != null)
                {
                    currentText.text =
                        $"{CardCatalog.GetEtherName(type)} " +
                        $"{_visibleCurrentEther[type]}";
                }

                if (_spentEtherCountTexts.TryGetValue(
                        type,
                        out var spentText) &&
                    spentText != null)
                {
                    spentText.text = _visibleSpentEther[type].ToString();
                }
            }

            if (_unusedEtherTitle != null)
            {
                _unusedEtherTitle.text =
                    $"未使用エーテル　{_visibleUnusedEther.Values.Sum()}";
            }

            if (_currentEtherHeader != null)
            {
                _currentEtherHeader.text =
                    $"手番エーテル {_visibleCurrentEther.Values.Sum()}　" +
                    GetBattleToolStatus();
            }

            if (_spentEtherTitle != null)
            {
                _spentEtherTitle.text =
                    $"使用済みエーテル　{_visibleSpentEther.Values.Sum()}";
            }
        }

        private void UpdateEnemyVisual(AttackHitResult hit)
        {
            if (hit.TargetIndex < 0 ||
                hit.TargetIndex >= _battle.Enemies.Count ||
                hit.TargetIndex >= _enemyLabels.Count ||
                hit.TargetIndex >= _enemyHpBarFills.Count)
            {
                return;
            }

            var enemy = _battle.Enemies[hit.TargetIndex];
            var label = _enemyLabels[hit.TargetIndex];
            label.text = hit.HpAfter > 0
                ? $"{enemy.Name}\nHP {hit.HpAfter}/{enemy.MaxHp}　" +
                  $"盾 {hit.ShieldAfter}\n次：{GetEnemyIntent(enemy)}"
                : $"{enemy.Name}\n撃破";
            var fill = _enemyHpBarFills[hit.TargetIndex];
            fill.anchorMax = new Vector2(
                Mathf.Clamp01((float)hit.HpAfter / enemy.MaxHp),
                1f);
        }

        private void ResetBattleVisualReferences()
        {
            _enemyRects.Clear();
            _enemyLabels.Clear();
            _enemyHpBarFills.Clear();
            _unusedEtherAnchors.Clear();
            _currentEtherAnchors.Clear();
            _spentEtherAnchors.Clear();
            _unusedEtherCountTexts.Clear();
            _currentEtherCountTexts.Clear();
            _spentEtherCountTexts.Clear();
            _visibleUnusedEther.Clear();
            _visibleCurrentEther.Clear();
            _visibleSpentEther.Clear();
            _unusedEtherTitle = null;
            _currentEtherHeader = null;
            _spentEtherTitle = null;
        }

        private void StartTurnEtherDrawAnimation(Action onComplete = null)
        {
            if (_battle == null ||
                _battle.Phase != BattlePhase.PlayerTurn ||
                _battle.LastDrawnEtherTypes.Count == 0 ||
                ShouldSkipTurnEtherAnimationForValidation())
            {
                onComplete?.Invoke();
                return;
            }

            _isResolvingAction = true;
            StartCoroutine(
                PlayTurnEtherDrawThenFinish(onComplete));
        }

        private IEnumerator PlayTurnEtherDrawThenFinish(Action onComplete)
        {
            var actionScreen = _screenRoot;
            var blocker = CreateOverlay("TurnEtherDrawBlocker");
            blocker.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            PrepareTurnEtherDrawDisplay();
            var refillDrawIndex = _battle.LastEtherRefillDrawIndex;
            if (refillDrawIndex < 0)
            {
                yield return PlayTurnEtherDrawBatch(
                    blocker,
                    _battle.LastDrawnEtherTypes);
            }
            else
            {
                yield return PlayTurnEtherDrawBatch(
                    blocker,
                    _battle.LastDrawnEtherTypes
                        .Take(refillDrawIndex)
                        .ToList());
                yield return PlayEtherTransferBatch(
                    blocker,
                    _battle.LastRefilledEtherTypes,
                    _spentEtherAnchors,
                    _unusedEtherAnchors,
                    type =>
                    {
                        _visibleSpentEther[type]--;
                        UpdateEtherDisplays();
                    },
                    type =>
                    {
                        _visibleUnusedEther[type]++;
                        UpdateEtherDisplays();
                    });
                yield return PlayTurnEtherDrawBatch(
                    blocker,
                    _battle.LastDrawnEtherTypes
                        .Skip(refillDrawIndex)
                        .ToList());
            }

            if (blocker != null)
            {
                Destroy(blocker.gameObject);
            }

            _isResolvingAction = false;
            if (actionScreen == _screenRoot)
            {
                onComplete?.Invoke();
            }
        }

        private IEnumerator PlayTurnEtherDrawBatch(
            RectTransform parent,
            IReadOnlyList<EtherType> etherTypes)
        {
            yield return PlayEtherTransferBatch(
                parent,
                etherTypes,
                _unusedEtherAnchors,
                _currentEtherAnchors,
                type =>
                {
                    _visibleUnusedEther[type]--;
                    UpdateEtherDisplays();
                },
                type =>
                {
                    _visibleCurrentEther[type]++;
                    UpdateEtherDisplays();
                });
        }

        private void PrepareTurnEtherDrawDisplay()
        {
            if (_battle == null)
            {
                return;
            }

            var drawnCounts = _battle.LastDrawnEtherTypes
                .GroupBy(type => type)
                .ToDictionary(group => group.Key, group => group.Count());
            var refillDrawIndex = _battle.LastEtherRefillDrawIndex;
            var drawnBeforeRefillCounts = refillDrawIndex >= 0
                ? _battle.LastDrawnEtherTypes
                    .Take(refillDrawIndex)
                    .GroupBy(type => type)
                    .ToDictionary(group => group.Key, group => group.Count())
                : null;
            var refilledCounts = refillDrawIndex >= 0
                ? _battle.LastRefilledEtherTypes
                    .GroupBy(type => type)
                    .ToDictionary(group => group.Key, group => group.Count())
                : null;
            foreach (EtherType type in Enum.GetValues(typeof(EtherType)))
            {
                var drawnCount = drawnCounts.TryGetValue(
                    type,
                    out var count)
                    ? count
                    : 0;
                _visibleUnusedEther[type] = refillDrawIndex >= 0
                    ? drawnBeforeRefillCounts.TryGetValue(
                        type,
                        out var drawnBeforeRefill)
                        ? drawnBeforeRefill
                        : 0
                    : _battle.LastUnusedAfterDraw[type] + drawnCount;
                _visibleCurrentEther[type] =
                    _battle.LastCurrentAfterDraw[type] - drawnCount;
                _visibleSpentEther[type] = refillDrawIndex >= 0
                    ? refilledCounts.TryGetValue(type, out var refilledCount)
                        ? refilledCount
                        : 0
                    : _battle.LastSpentAfterDraw[type];
            }

            UpdateEtherDisplays();
        }

        private static bool ShouldSkipTurnEtherAnimationForValidation()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            var arguments = Environment.GetCommandLineArgs();
            return Array.IndexOf(arguments, "-lostPageCapture") >= 0 &&
                   Array.IndexOf(
                       arguments,
                       "-lostPageCaptureEtherDrawAnimation") < 0;
#else
            return false;
#endif
        }

        private void FinishCardUse(CardKind usedKind)
        {
            if (_battle.Phase == BattlePhase.Victory)
            {
                HandleBattleVictory();
                return;
            }

            var completedTutorial = AdvanceTutorialAfterCard(usedKind);
            if (!IsSelectedEnemyAlive())
            {
                _selectedEnemyIndex = FindFirstAliveEnemy();
            }

            if (_cardSortMode == CardSortMode.UsableFirst)
            {
                _cardScrollX = 0f;
            }

            ShowBattle();
            if (completedTutorial)
            {
                ShowTutorialCompletion();
            }
        }

        private void BeginCardDrag(
            CardInstance card,
            PointerEventData eventData)
        {
            if (_dragGhost != null)
            {
                Destroy(_dragGhost.gameObject);
            }

            _selectedCard = card;
            _dragGhost = UiFactory.CreatePanel(
                "CardDragGhost",
                _canvas.transform,
                Vector2.zero,
                Vector2.zero,
                UiFactory.GetCardColor(card.Kind));
            _dragGhost.sizeDelta = new Vector2(250, 225);
            _dragGhost.SetAsLastSibling();

            var canvasGroup = _dragGhost.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0.82f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            UiFactory.CreateText(
                "Label",
                _dragGhost,
                new Vector2(0.05f, 0.05f),
                new Vector2(0.95f, 0.95f),
                $"{CardCatalog.GetName(card.Kind)}\n\n" +
                $"{CardCatalog.GetShortDescription(card)}\n" +
                $"コスト：{CardCatalog.GetCostText(card)}",
                22);
            UpdateCardDrag(eventData);
        }

        private void UpdateCardDrag(PointerEventData eventData)
        {
            if (_dragGhost != null)
            {
                _dragGhost.position = eventData.position;
            }
        }

        private void EndCardDrag(
            CardInstance card,
            PointerEventData eventData)
        {
            if (_dragGhost != null)
            {
                Destroy(_dragGhost.gameObject);
                _dragGhost = null;
            }

            _selectedCard = card;
            var dropObject = eventData.pointerCurrentRaycast.gameObject;
            if (CardCatalog.IsAttack(card.Kind))
            {
                var enemyTarget =
                    dropObject?.GetComponentInParent<EnemyCardDropTarget>();
                if (enemyTarget != null)
                {
                    _selectedEnemyIndex = enemyTarget.EnemyIndex;
                    UseSelectedCard();
                    return;
                }

                _message = "攻撃カードは対象の敵へドロップしてください。";
                ShowBattle();
                return;
            }

            var effectTarget =
                dropObject?.GetComponentInParent<CardEffectDropTarget>();
            if (effectTarget != null)
            {
                UseSelectedCard();
                return;
            }

            _message =
                "防御・チャージ・持続カードは中央の説明欄へ" +
                "ドロップしてください。";
            ShowBattle();
        }

        private void ShowCarrySelection()
        {
            CancelInvoke();
            if (_battle == null || _battle.Phase != BattlePhase.PlayerTurn)
            {
                return;
            }

            var tokens = _battle.Pool.GetCurrentTokens().ToList();
            var carryLimit = _session.Player.GetCarryLimit();
            var required = Math.Min(carryLimit, tokens.Count);
            if (tokens.Count <= carryLimit)
            {
                CompleteTurn(tokens);
                return;
            }

            if (_tutorialStep == TutorialStep.EndTurn)
            {
                _tutorialStep = TutorialStep.Carry;
            }

            var overlay = CreateOverlay("CarryOverlay");
            var dialog = UiFactory.CreatePanel(
                "Dialog",
                overlay,
                new Vector2(0.17f, 0.24f),
                new Vector2(0.83f, 0.76f),
                new Color32(42, 45, 59, 255));
            UiFactory.CreateText(
                "Title",
                dialog,
                new Vector2(0.05f, 0.78f),
                new Vector2(0.95f, 0.96f),
                $"次のターンへ持ち越すエーテルを{required}個選択",
                31);

            if (_tutorialStep == TutorialStep.Carry)
            {
                UiFactory.CreateText(
                    "TutorialCarryHint",
                    dialog,
                    new Vector2(0.06f, 0.68f),
                    new Vector2(0.74f, 0.79f),
                    $"ガイド 3/4：残したいエーテルを{required}個選びます。",
                    20,
                    TextAnchor.MiddleLeft,
                    UiFactory.Accent);
                UiFactory.CreateButton(
                    "SkipTutorial",
                    dialog,
                    new Vector2(0.78f, 0.82f),
                    new Vector2(0.95f, 0.95f),
                    "スキップ",
                    SkipContextTutorial,
                    new Color32(91, 84, 79, 255),
                    17);
            }

            var tokenArea = UiFactory.CreateRect(
                "Tokens",
                dialog,
                new Vector2(0.06f, 0.31f),
                new Vector2(0.94f, 0.68f));
            var layout = tokenArea.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 15;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var selected = new bool[tokens.Count];
            Button confirmButton = null;
            for (var index = 0; index < tokens.Count; index++)
            {
                var tokenIndex = index;
                var type = tokens[index];
                var tokenButton = UiFactory.CreateButton(
                    $"Token_{index}",
                    tokenArea,
                    Vector2.zero,
                    Vector2.zero,
                    CardCatalog.GetEtherName(type),
                    null,
                    UiFactory.GetEtherColor(type),
                    27);
                var element = tokenButton.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = 92;
                element.preferredHeight = 92;
                var tokenImage = tokenButton.GetComponent<Image>();
                var tokenSprite = EtherTextureSet.GetSprite(type);
                if (tokenSprite != null)
                {
                    tokenImage.sprite = tokenSprite;
                    tokenImage.preserveAspect = true;
                    tokenImage.color = Color.white;
                }

                var tokenLabel = tokenButton.GetComponentInChildren<Text>();
                tokenButton.onClick.AddListener(() =>
                {
                    var selectedCount = selected.Count(value => value);
                    if (!selected[tokenIndex] && selectedCount >= required)
                    {
                        return;
                    }

                    selected[tokenIndex] = !selected[tokenIndex];
                    tokenButton.transform.localScale = selected[tokenIndex]
                        ? Vector3.one * 1.12f
                        : Vector3.one;
                    tokenLabel.text = selected[tokenIndex]
                        ? $"✓\n{CardCatalog.GetEtherName(type)}"
                        : CardCatalog.GetEtherName(type);
                    if (confirmButton != null)
                    {
                        confirmButton.interactable =
                            selected.Count(value => value) == required;
                    }
                });
            }

            confirmButton = UiFactory.CreateButton(
                "Confirm",
                dialog,
                new Vector2(0.53f, 0.08f),
                new Vector2(0.83f, 0.25f),
                "持ち越して終了",
                () =>
                {
                    var carried = tokens
                        .Where((_, index) => selected[index])
                        .ToList();
                    Destroy(overlay.gameObject);
                    CompleteTurn(carried);
                },
                UiFactory.Green,
                27);
            confirmButton.interactable = false;

            UiFactory.CreateButton(
                "CancelCarry",
                dialog,
                new Vector2(0.17f, 0.08f),
                new Vector2(0.47f, 0.25f),
                "キャンセル",
                () =>
                {
                    if (_tutorialStep == TutorialStep.Carry)
                    {
                        _tutorialStep = TutorialStep.EndTurn;
                    }

                    Destroy(overlay.gameObject);
                    ShowBattle();
                },
                new Color32(91, 84, 79, 255),
                27);
        }

        private void CompleteTurn(IReadOnlyList<EtherType> carried)
        {
            if (_isResolvingAction)
            {
                return;
            }

            var advanceToCharge =
                _tutorialStep == TutorialStep.EndTurn ||
                _tutorialStep == TutorialStep.Carry;
            var hpBeforeEnemyTurn = _session.Player.Hp;
            _message = _battle.EndPlayerTurn(carried);
            if (_session.Player.Hp < hpBeforeEnemyTurn)
            {
                _isResolvingAction = true;
                ShowBattle();
                if (_battle.Phase == BattlePhase.PlayerTurn)
                {
                    PrepareTurnEtherDrawDisplay();
                }

                StartCoroutine(ShakeThenFinishTurn(advanceToCharge));
                return;
            }

            FinishCompletedTurn(advanceToCharge);
        }

        private IEnumerator ShakeThenFinishTurn(bool advanceToCharge)
        {
            var blocker = CreateOverlay("EnemyActionBlocker");
            blocker.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            var originalPosition = _screenRoot.anchoredPosition;
            const float duration = 0.25f;
            const float amplitude = 18f;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var strength = 1f - Mathf.Clamp01(elapsed / duration);
                _screenRoot.anchoredPosition =
                    originalPosition +
                    new Vector2(
                        Mathf.Sin(elapsed * 91f),
                        Mathf.Cos(elapsed * 73f)) *
                    amplitude *
                    strength;
                yield return null;
            }

            _screenRoot.anchoredPosition = originalPosition;
            if (blocker != null)
            {
                Destroy(blocker.gameObject);
            }

            _isResolvingAction = false;
            FinishCompletedTurn(advanceToCharge);
        }

        private void FinishCompletedTurn(bool advanceToCharge)
        {
            if (_battle.Phase == BattlePhase.Defeat)
            {
                ShowDefeat();
                return;
            }

            if (_battle.Phase == BattlePhase.Victory)
            {
                HandleBattleVictory();
                return;
            }

            if (advanceToCharge)
            {
                _tutorialStep = TutorialStep.Charge;
                EnsureTutorialCardCost(CardKind.Charge);
            }

            ShowBattle();
            StartTurnEtherDrawAnimation(
                () =>
                {
                    if (advanceToCharge)
                    {
                        ShowBattle();
                    }
                });
        }

        private void HandleBattleVictory()
        {
            _session.CompleteCurrentBattle(_battle.Enemies.Count);
            if (_session.CurrentNode.Kind == StageKind.Boss)
            {
                if (_session.HasNextLayer)
                {
                    ShowBossCardReward();
                }
                else
                {
                    ShowBossVictory();
                }

                return;
            }

            _remainingCardRewardSelections =
                _session.Player.HasTool(CarryToolKind.PresentBox)
                    ? 2
                    : 1;
            ShowCardReward();
        }

        private void ShowCardReward()
        {
            if (_remainingCardRewardSelections <= 0)
            {
                _remainingCardRewardSelections = 1;
            }

            var isPresentBoxReward =
                _session.Player.HasTool(CarryToolKind.PresentBox) &&
                _remainingCardRewardSelections == 1;
            var root = CreateScreen("RewardScreen");
            UiFactory.CreateText(
                "Title",
                root,
                new Vector2(0.08f, 0.80f),
                new Vector2(0.92f, 0.94f),
                isPresentBoxReward
                    ? "プレゼントボックス　追加カード報酬"
                    : $"戦闘勝利　{_session.LastBattleGoldReward}ゴールド獲得",
                42,
                TextAnchor.MiddleCenter,
                UiFactory.Accent);
            UiFactory.CreateText(
                "Prompt",
                root,
                new Vector2(0.08f, 0.70f),
                new Vector2(0.92f, 0.80f),
                "カードを1枚選んで取得してください",
                29);

            var choices = _session.CreateCardRewardChoices();
            for (var index = 0; index < choices.Count; index++)
            {
                var kind = choices[index];
                var left = 0.13f + index * 0.27f;
                UiFactory.CreateButton(
                    $"Reward_{kind}",
                    root,
                    new Vector2(left, 0.23f),
                    new Vector2(left + 0.20f, 0.67f),
                    $"【{CardCatalog.GetRarityLabel(kind)}】\n" +
                    $"{CardCatalog.GetName(kind)}\n\n" +
                    $"{CardCatalog.GetShortDescription(kind)}\n\n" +
                    $"コスト：{CardCatalog.GetCostText(kind)}\n" +
                    GetCooldownLabel(kind),
                    () =>
                    {
                        _session.Player.AddCard(kind);
                        _remainingCardRewardSelections--;
                        _message =
                            $"{CardCatalog.GetName(kind)}を取得しました。" +
                            "次の戦闘からエーテル構成に反映されます。";
                        if (_remainingCardRewardSelections > 0)
                        {
                            ShowCardReward();
                        }
                        else
                        {
                            ShowMap();
                        }
                    },
                    UiFactory.GetCardColor(kind),
                    25);
            }
        }

        private void ShowFountain()
        {
            var healed = _session.Player.Heal(30);
            _session.CurrentNode.Cleared = true;
            _message = $"回復の泉でHPを{healed}回復しました。";
            ShowMap();
            ShowMessageDialog(
                "回復の泉",
                $"HPを{healed}回復しました。\n" +
                $"現在HP {_session.Player.Hp}/{_session.Player.MaxHp}");
        }

        private void ShowRandomEvent()
        {
            var kind = _session.GetCurrentRandomEventKind();
            if (kind == RandomEventKind.FreeUpgrade)
            {
                ShowFreeUpgradeEvent();
                return;
            }

            if (kind == RandomEventKind.BloodUpgrade)
            {
                ShowBloodUpgradeEvent();
                return;
            }

            _message = _session.ResolveCurrentRandomEvent();
            if (_session.Player.Hp <= 0)
            {
                ShowDefeat();
                return;
            }

            ShowMap();
            ShowMessageDialog("ランダムイベント", _message);
        }

        private void ShowFreeUpgradeEvent()
        {
            var root = CreateScreen("FreeUpgradeEvent");
            DrawUpgradeEventHeader(
                root,
                "古い強化台",
                "カードを最大2枚まで強化できます。1枚強化後に終了することもできます。");
            DrawEventUpgradeableCards(
                root,
                card =>
                {
                    if (_session.TryUpgradeCardForFreeEvent(card.Id))
                    {
                        _message =
                            $"{CardCatalog.GetName(card.Kind)}を強化しました。";
                    }

                    if (_session.CurrentNode.Cleared)
                    {
                        ShowMap();
                    }
                    else
                    {
                        ShowFreeUpgradeEvent();
                    }
                });
            var finish = UiFactory.CreateButton(
                "FinishFreeUpgrade",
                root,
                new Vector2(0.36f, 0.05f),
                new Vector2(0.64f, 0.15f),
                "ここで終了",
                () =>
                {
                    _session.FinishFreeUpgradeEvent();
                    _message = "カード強化を終了しました。";
                    ShowMap();
                },
                new Color32(100, 83, 61, 255),
                25);
            finish.interactable =
                _session.FreeEventUpgrades > 0;
        }

        private void ShowBloodUpgradeEvent()
        {
            if (_session.PendingBloodEventUpgrades > 0)
            {
                var root = CreateScreen("BloodUpgradeCardSelection");
                DrawUpgradeEventHeader(
                    root,
                    "血の強化",
                    $"強化するカードを選択してください。" +
                    $"残り{_session.PendingBloodEventUpgrades}枚");
                DrawEventUpgradeableCards(
                    root,
                    card =>
                    {
                        if (_session.TryUpgradeCardForBloodEvent(card.Id))
                        {
                            _message =
                                $"{CardCatalog.GetName(card.Kind)}を強化しました。";
                        }

                        if (_session.CurrentNode.Cleared)
                        {
                            ShowMap();
                        }
                        else
                        {
                            ShowBloodUpgradeEvent();
                        }
                    });
                return;
            }

            var selectionRoot = CreateScreen("BloodUpgradeEvent");
            DrawUpgradeEventHeader(
                selectionRoot,
                "血の強化",
                "HPを支払い、カードを強化します。HPが0以下になる選択肢は選べません。");
            for (var count = 1; count <= 3; count++)
            {
                var selectedCount = count;
                var left = 0.13f + (count - 1) * 0.27f;
                var button = UiFactory.CreateButton(
                    $"BloodUpgrade_{count}",
                    selectionRoot,
                    new Vector2(left, 0.30f),
                    new Vector2(left + 0.20f, 0.62f),
                    $"HP -{count * 6}\n\nカード{count}枚を強化",
                    () =>
                    {
                        if (_session.StartBloodUpgradeEvent(selectedCount))
                        {
                            ShowBloodUpgradeEvent();
                        }
                    },
                    new Color32(116, 57, 65, 255),
                    27);
                button.interactable =
                    _session.CanChooseBloodUpgradeCount(count);
            }
        }

        private static void DrawUpgradeEventHeader(
            RectTransform root,
            string title,
            string description)
        {
            UiFactory.CreateText(
                "Title",
                root,
                new Vector2(0.08f, 0.83f),
                new Vector2(0.92f, 0.95f),
                title,
                43,
                TextAnchor.MiddleCenter,
                UiFactory.Accent);
            UiFactory.CreateText(
                "Description",
                root,
                new Vector2(0.08f, 0.73f),
                new Vector2(0.92f, 0.83f),
                description,
                26);
        }

        private void DrawEventUpgradeableCards(
            RectTransform root,
            Action<CardInstance> onSelected)
        {
            var scroll = UiFactory.CreateScrollView(
                "EventCards",
                root,
                new Vector2(0.05f, 0.18f),
                new Vector2(0.95f, 0.70f),
                true,
                false,
                out var content);
            var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16;
            layout.padding = new RectOffset(16, 16, 12, 12);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            foreach (var card in _session.GetUpgradeableCards())
            {
                var selectedCard = card;
                var button = UiFactory.CreateButton(
                    $"EventUpgrade_{card.Id}",
                    content,
                    Vector2.zero,
                    Vector2.zero,
                    $"{CardCatalog.GetName(card.Kind)}\n" +
                    $"【{CardCatalog.GetRarityLabel(card.Kind)}】\n\n" +
                    $"現在：{CardCatalog.GetShortDescription(card)}\n\n" +
                    $"強化後：{GetUpgradedCardDescription(card)}",
                    () => onSelected(selectedCard),
                    UiFactory.GetCardColor(card.Kind),
                    21);
                var element = button.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = 320;
                element.minWidth = 320;
                element.preferredHeight = 410;
            }
        }

        private void ShowRewardStage()
        {
            var reward = _session.ClaimCurrentReward();
            var rareToolMessage = reward.RareTool.HasValue
                ? "\nレア道具：" +
                  CarryToolCatalog.GetName(reward.RareTool.Value)
                : "\nレア道具は見つかりませんでした。";
            _message =
                $"{CardCatalog.GetName(reward.Cards[0])}と" +
                $"{CardCatalog.GetName(reward.Cards[1])}を獲得しました。" +
                rareToolMessage;
            ShowMap();
            ShowMessageDialog(
                "報酬の間",
                "カードを2枚獲得しました。\n" +
                $"{CardCatalog.GetName(reward.Cards[0])}\n" +
                CardCatalog.GetName(reward.Cards[1]) +
                rareToolMessage);
        }

        private void ShowToolStage()
        {
            var choices = _session.CreateToolStageChoices();
            if (choices.Count == 0)
            {
                _session.CurrentNode.Cleared = true;
                _message = "取得できる通常道具はありませんでした。";
                ShowMap();
                ShowMessageDialog("道具の間", _message);
                return;
            }

            var root = CreateScreen("ToolStageReward");
            UiFactory.CreateText(
                "Title",
                root,
                new Vector2(0.08f, 0.80f),
                new Vector2(0.92f, 0.94f),
                "道具の間",
                46,
                TextAnchor.MiddleCenter,
                UiFactory.Accent);
            UiFactory.CreateText(
                "Prompt",
                root,
                new Vector2(0.08f, 0.70f),
                new Vector2(0.92f, 0.80f),
                "通常道具を1つ選んで取得してください",
                29);

            for (var index = 0; index < choices.Count; index++)
            {
                var kind = choices[index];
                var left = 0.13f + index * 0.27f;
                UiFactory.CreateButton(
                    $"ToolStage_{kind}",
                    root,
                    new Vector2(left, 0.23f),
                    new Vector2(left + 0.20f, 0.67f),
                    $"【通常】\n{CarryToolCatalog.GetName(kind)}\n\n" +
                    CarryToolCatalog.GetDescription(kind),
                    () =>
                    {
                        _session.ClaimCurrentToolStageReward(kind);
                        _message =
                            $"{CarryToolCatalog.GetName(kind)}を獲得しました。";
                        ShowMap();
                    },
                    GetCarryToolColor(kind),
                    24);
            }
        }

        private void ShowShop()
        {
            ShowMap();
            var price = _session.ShopCardPrice;
            var overlay = CreateOverlay("ShopOverlay");
            var dialog = UiFactory.CreatePanel(
                "ShopDialog",
                overlay,
                new Vector2(0.08f, 0.12f),
                new Vector2(0.92f, 0.88f),
                new Color32(42, 45, 59, 255));

            UiFactory.CreateText(
                "Title",
                dialog,
                new Vector2(0.05f, 0.86f),
                new Vector2(0.65f, 0.98f),
                $"商人 － カード1枚 {price}ゴールド",
                34,
                TextAnchor.MiddleLeft,
                UiFactory.Accent);
            UiFactory.CreateText(
                "Gold",
                dialog,
                new Vector2(0.65f, 0.86f),
                new Vector2(0.95f, 0.98f),
                $"所持金 {_session.Player.Gold}G",
                30,
                TextAnchor.MiddleRight);

            DrawShopCategoryTabs(dialog);

            var shopScroll = UiFactory.CreateScrollView(
                "ShopCards",
                dialog,
                new Vector2(0.04f, 0.25f),
                new Vector2(0.96f, 0.76f),
                true,
                false,
                out var shopContent);
            var shopLayout =
                shopContent.gameObject.AddComponent<HorizontalLayoutGroup>();
            shopLayout.spacing = 16;
            shopLayout.padding = new RectOffset(16, 16, 12, 12);
            shopLayout.childAlignment = TextAnchor.MiddleLeft;
            shopLayout.childControlWidth = true;
            shopLayout.childControlHeight = true;
            shopLayout.childForceExpandWidth = false;
            shopLayout.childForceExpandHeight = true;
            var shopFitter =
                shopContent.gameObject.AddComponent<ContentSizeFitter>();
            shopFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            shopFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var stocks = _session.GetCurrentShopStock(_shopCategory);
            for (var index = 0; index < stocks.Count; index++)
            {
                var stock = stocks[index];
                var kind = stock.Kind;
                var button = UiFactory.CreateButton(
                    $"Shop_{stock.Id}_{kind}",
                    shopContent,
                    Vector2.zero,
                    Vector2.zero,
                    $"【{CardCatalog.GetRarityLabel(kind)}】\n" +
                    $"{CardCatalog.GetName(kind)}\n\n" +
                    $"{CardCatalog.GetShortDescription(kind)}\n\n" +
                    $"{CardCatalog.GetCostText(kind)}\n" +
                    $"{GetCooldownLabel(kind)}\n" +
                    (stock.Purchased ? "売り切れ" : $"{price}G"),
                    () => ShowPurchaseConfirmation(stock),
                    stock.Purchased
                        ? UiFactory.Disabled
                        : UiFactory.GetCardColor(kind),
                    23);
                button.interactable =
                    !stock.Purchased &&
                    _session.Player.Gold >= price &&
                    _session.Player.CanAddCard(kind);
                var element = button.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = 260;
                element.minWidth = 260;
                element.preferredHeight = 400;
            }

            Canvas.ForceUpdateCanvases();
            shopScroll.horizontalNormalizedPosition = _shopScrollX;
            shopScroll.onValueChanged.AddListener(value => _shopScrollX = value.x);

            var upgradeButton = UiFactory.CreateButton(
                "UpgradeCard",
                dialog,
                new Vector2(0.66f, 0.08f),
                new Vector2(0.94f, 0.20f),
                $"{GetCategoryLabel(_shopCategory)}カードを強化\n" +
                $"{_session.ShopUpgradePrice}G",
                ShowShopUpgradeSelection,
                new Color32(87, 75, 118, 255),
                22);
            upgradeButton.interactable =
                !_session.HasUpgradedCategoryAtCurrentShop(_shopCategory) &&
                _session.Player.Gold >= _session.ShopUpgradePrice &&
                _session.Player.Deck.Any(
                    card =>
                        CardCatalog.GetCategory(card.Kind) == _shopCategory &&
                        CardCatalog.CanUpgrade(card));

            UiFactory.CreateButton(
                "Leave",
                dialog,
                new Vector2(0.36f, 0.06f),
                new Vector2(0.64f, 0.18f),
                "店を出る",
                () =>
                {
                    _session.CurrentNode.Cleared = true;
                    _message = "商人のステージを後にしました。";
                    ShowMap();
                },
                new Color32(100, 83, 61, 255),
                27);

            if (!string.IsNullOrWhiteSpace(_message))
            {
                UiFactory.CreateText(
                    "ShopMessage",
                    dialog,
                    new Vector2(0.05f, 0.01f),
                    new Vector2(0.32f, 0.20f),
                    _message,
                    20,
                    TextAnchor.MiddleLeft);
            }
        }

        private void DrawShopCategoryTabs(RectTransform dialog)
        {
            var categories = new[]
            {
                CardCategory.Attack,
                CardCategory.Defense,
                CardCategory.Charge,
                CardCategory.Persistent
            };
            for (var index = 0; index < categories.Length; index++)
            {
                var category = categories[index];
                var left = 0.05f + index * 0.225f;
                UiFactory.CreateButton(
                    $"ShopTab_{category}",
                    dialog,
                    new Vector2(left, 0.78f),
                    new Vector2(left + 0.20f, 0.85f),
                    GetCategoryLabel(category),
                    () =>
                    {
                        _shopCategory = category;
                        _shopScrollX = 0f;
                        ShowShop();
                    },
                    _shopCategory == category
                        ? UiFactory.Green
                        : new Color32(72, 81, 104, 255),
                    22);
            }
        }

        private void ShowShopUpgradeSelection()
        {
            var overlay = CreateOverlay("ShopUpgradeSelection");
            var dialog = UiFactory.CreatePanel(
                "ShopUpgradeDialog",
                overlay,
                new Vector2(0.12f, 0.12f),
                new Vector2(0.88f, 0.88f),
                new Color32(42, 45, 59, 255));
            UiFactory.CreateText(
                "Title",
                dialog,
                new Vector2(0.06f, 0.85f),
                new Vector2(0.94f, 0.97f),
                $"{GetCategoryLabel(_shopCategory)}カード強化 " +
                $"({_session.ShopUpgradePrice}G)",
                34,
                TextAnchor.MiddleCenter,
                UiFactory.Accent);
            var scroll = UiFactory.CreateScrollView(
                "UpgradeableCards",
                dialog,
                new Vector2(0.05f, 0.19f),
                new Vector2(0.95f, 0.82f),
                true,
                false,
                out var content);
            var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16;
            layout.padding = new RectOffset(16, 16, 12, 12);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var cards = _session.Player.Deck
                .Where(
                    card =>
                        CardCatalog.GetCategory(card.Kind) == _shopCategory &&
                        CardCatalog.CanUpgrade(card))
                .ToList();
            foreach (var card in cards)
            {
                var button = UiFactory.CreateButton(
                    $"Upgrade_{card.Id}",
                    content,
                    Vector2.zero,
                    Vector2.zero,
                    $"{CardCatalog.GetName(card.Kind)}\n" +
                    $"【{CardCatalog.GetRarityLabel(card.Kind)}】\n\n" +
                    $"現在：{CardCatalog.GetShortDescription(card)}\n\n" +
                    $"強化後：{GetUpgradedCardDescription(card)}",
                    () =>
                    {
                        if (_session.TryUpgradeCardAtCurrentShop(card.Id))
                        {
                            _message =
                                $"{CardCatalog.GetName(card.Kind)}を強化しました。";
                        }

                        ShowShop();
                    },
                    UiFactory.GetCardColor(card.Kind),
                    21);
                button.interactable =
                    _session.CanUpgradeCardAtCurrentShop(card);
                var element = button.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = 320;
                element.minWidth = 320;
                element.preferredHeight = 430;
            }

            UiFactory.CreateButton(
                "Close",
                dialog,
                new Vector2(0.35f, 0.04f),
                new Vector2(0.65f, 0.14f),
                "戻る",
                () => Destroy(overlay.gameObject),
                new Color32(91, 84, 79, 255),
                24);
        }

        private void ShowPurchaseConfirmation(ShopCardStock stock)
        {
            var kind = stock.Kind;
            var price = _session.ShopCardPrice;
            var goldBefore = _session.Player.Gold;
            var overlay = CreateOverlay("PurchaseConfirmation");
            var dialog = UiFactory.CreatePanel(
                "PurchaseDialog",
                overlay,
                new Vector2(0.22f, 0.19f),
                new Vector2(0.78f, 0.81f),
                new Color32(42, 45, 59, 255));
            UiFactory.CreateText(
                "Title",
                dialog,
                new Vector2(0.07f, 0.82f),
                new Vector2(0.93f, 0.95f),
                "カード購入の確認",
                36,
                TextAnchor.MiddleCenter,
                UiFactory.Accent);
            UiFactory.CreateText(
                "CardDetails",
                dialog,
                new Vector2(0.08f, 0.34f),
                new Vector2(0.92f, 0.80f),
                $"{CardCatalog.GetName(kind)}\n\n" +
                $"{CardCatalog.GetShortDescription(kind)}\n" +
                $"コスト：{CardCatalog.GetCostText(kind)}\n" +
                $"{GetCooldownLabel(kind)}\n\n" +
                $"価格：{price}G\n" +
                $"所持金：{goldBefore}G → {goldBefore - price}G",
                27,
                TextAnchor.MiddleCenter);
            UiFactory.CreateButton(
                "ConfirmPurchase",
                dialog,
                new Vector2(0.53f, 0.09f),
                new Vector2(0.87f, 0.26f),
                "購入する",
                () =>
                {
                    if (_session.TryBuyShopCard(stock.Id))
                    {
                        _message =
                            $"{CardCatalog.GetName(kind)}を{price}Gで購入しました。" +
                            $" 次の価格は{_session.ShopCardPrice}Gです。";
                    }
                    else
                    {
                        _message = "ゴールドが不足しています。";
                    }

                    ShowShop();
                },
                UiFactory.Green,
                27);
            UiFactory.CreateButton(
                "CancelPurchase",
                dialog,
                new Vector2(0.13f, 0.09f),
                new Vector2(0.47f, 0.26f),
                "キャンセル",
                () => Destroy(overlay.gameObject),
                new Color32(91, 84, 79, 255),
                27);
        }

        private void ShowBossCardReward()
        {
            var choices = _session.CreateBossCardRewardChoices();
            if (choices.Count == 0)
            {
                ShowBossToolReward();
                return;
            }

            var root = CreateScreen("BossCardReward");
            UiFactory.CreateText(
                "Title",
                root,
                new Vector2(0.08f, 0.80f),
                new Vector2(0.92f, 0.94f),
                "ボスカード報酬",
                46,
                TextAnchor.MiddleCenter,
                UiFactory.Accent);
            UiFactory.CreateText(
                "Prompt",
                root,
                new Vector2(0.08f, 0.70f),
                new Vector2(0.92f, 0.80f),
                "ボスカードを1枚選んでください",
                29);
            for (var index = 0; index < choices.Count; index++)
            {
                var kind = choices[index];
                var left = 0.13f + index * 0.27f;
                UiFactory.CreateButton(
                    $"BossCard_{kind}",
                    root,
                    new Vector2(left, 0.23f),
                    new Vector2(left + 0.20f, 0.67f),
                    $"【ボス】\n{CardCatalog.GetName(kind)}\n\n" +
                    $"{CardCatalog.GetShortDescription(kind)}\n\n" +
                    $"コスト：{CardCatalog.GetCostText(kind)}\n" +
                    GetCooldownLabel(kind),
                    () =>
                    {
                        _session.Player.AddCard(kind);
                        _message =
                            $"{CardCatalog.GetName(kind)}を獲得しました。";
                        ShowBossToolReward();
                    },
                    UiFactory.GetCardColor(kind),
                    23);
            }
        }

        private void ShowBossToolReward()
        {
            var choices = _session.CreateBossToolChoices();
            if (choices.Count == 0)
            {
                ShowBossVictory();
                return;
            }

            var root = CreateScreen("BossToolReward");
            UiFactory.CreateText(
                "Title",
                root,
                new Vector2(0.08f, 0.80f),
                new Vector2(0.92f, 0.94f),
                "ボス報酬",
                46,
                TextAnchor.MiddleCenter,
                UiFactory.Accent);
            UiFactory.CreateText(
                "Prompt",
                root,
                new Vector2(0.08f, 0.70f),
                new Vector2(0.92f, 0.80f),
                "ボス道具を1つ選んでください",
                29);

            for (var index = 0; index < choices.Count; index++)
            {
                var kind = choices[index];
                var left = 0.13f + index * 0.27f;
                UiFactory.CreateButton(
                    $"BossTool_{kind}",
                    root,
                    new Vector2(left, 0.23f),
                    new Vector2(left + 0.20f, 0.67f),
                    $"【ボス】\n{CarryToolCatalog.GetName(kind)}\n\n" +
                    CarryToolCatalog.GetDescription(kind),
                    () =>
                    {
                        _session.ClaimTool(
                            kind,
                            CarryToolRarity.Boss);
                        _message =
                            $"{CarryToolCatalog.GetName(kind)}を獲得しました。";
                        ShowBossVictory();
                    },
                    GetCarryToolColor(kind),
                    24);
            }
        }

        private void ShowBossVictory()
        {
            var root = CreateScreen("VictoryScreen");
            var finalLayer = !_session.HasNextLayer;
            var bossRewardMessage =
                finalLayer ? string.Empty : $"\n{_message}";
            UiFactory.CreateText(
                "Victory",
                root,
                new Vector2(0.10f, 0.56f),
                new Vector2(0.90f, 0.78f),
                finalLayer
                    ? $"ALL LAYERS CLEARED\n{_session.CurrentLayer}層を制覇した"
                    : $"BOSS DEFEATED\n{_session.CurrentLayer}層をクリアした",
                52,
                TextAnchor.MiddleCenter,
                UiFactory.Accent);
            UiFactory.CreateText(
                "RunSummary",
                root,
                new Vector2(0.20f, 0.38f),
                new Vector2(0.80f, 0.55f),
                $"HP {_session.Player.Hp}/{_session.Player.MaxHp}" +
                "（最大HP+10・全回復）\n" +
                $"所持カード {_session.Player.Deck.Count}枚　" +
                $"所持金 {_session.Player.Gold}G\n" +
                GetOwnedToolSummary() +
                $"\n討伐報酬 {_session.LastBattleGoldReward}G" +
                bossRewardMessage + "\n" +
                $"到達層 {_session.CurrentLayer}/{RunSession.MaxLayer}",
                29);
            if (finalLayer)
            {
                UiFactory.CreateButton(
                    "Restart",
                    root,
                    new Vector2(0.36f, 0.18f),
                    new Vector2(0.64f, 0.31f),
                    "最初から遊ぶ",
                    StartNewRun,
                    UiFactory.Green,
                    30);
            }
            else
            {
                UiFactory.CreateButton(
                    "NextLayer",
                    root,
                    new Vector2(0.36f, 0.18f),
                    new Vector2(0.64f, 0.31f),
                    "次の層へ",
                    AdvanceToNextLayer,
                    UiFactory.Green,
                    30);
            }
        }

        private void AdvanceToNextLayer()
        {
            _session.AdvanceToNextLayer();
            _battle = null;
            _selectedCard = null;
            _selectedEnemyIndex = 0;
            _mapScrollX = 0.5f;
            _mapScrollY = 0f;
            ResetCardBrowsingState();
            _message =
                $"{_session.CurrentLayer}層へ進みました。" +
                "接続されているステージを選択してください。";
            ShowMap();
        }

        private void ShowDefeat()
        {
            ShowDefeat(null);
        }

        private void ShowDefeat(string reason)
        {
            var root = CreateScreen("DefeatScreen");
            UiFactory.CreateText(
                "Defeat",
                root,
                new Vector2(0.10f, 0.55f),
                new Vector2(0.90f, 0.76f),
                "DEFEAT\n" +
                (string.IsNullOrEmpty(reason)
                    ? "プレイヤーのHPが0になった"
                    : reason),
                52,
                TextAnchor.MiddleCenter,
                UiFactory.Red);
            UiFactory.CreateButton(
                "Restart",
                root,
                new Vector2(0.36f, 0.27f),
                new Vector2(0.64f, 0.41f),
                "HP100・初期デッキから再開",
                StartNewRun,
                UiFactory.Green,
                27);
        }

        private void ShowTutorialWelcome()
        {
            _tutorialOfferedThisSession = true;
            var overlay = CreateOverlay("TutorialWelcomeOverlay");
            var dialog = UiFactory.CreatePanel(
                "TutorialWelcome",
                overlay,
                new Vector2(0.23f, 0.24f),
                new Vector2(0.77f, 0.76f),
                new Color32(42, 45, 59, 255));
            UiFactory.CreateText(
                "Title",
                dialog,
                new Vector2(0.08f, 0.72f),
                new Vector2(0.92f, 0.92f),
                "初めての戦闘",
                39,
                TextAnchor.MiddleCenter,
                UiFactory.Accent);
            UiFactory.CreateText(
                "Message",
                dialog,
                new Vector2(0.08f, 0.34f),
                new Vector2(0.92f, 0.70f),
                "実際に操作しながら、攻撃・ターン終了・\n" +
                "エーテル持ち越し・チャージを順に案内します。\n" +
                "操作はガイド中も自由に行えます。",
                25);
            UiFactory.CreateButton(
                "StartTutorial",
                dialog,
                new Vector2(0.52f, 0.10f),
                new Vector2(0.86f, 0.28f),
                "ガイドを始める",
                () =>
                {
                    _tutorialStep = TutorialStep.Attack;
                    EnsureTutorialCardCost(CardKind.Attack);
                    ShowBattle();
                },
                UiFactory.Green,
                25);
            UiFactory.CreateButton(
                "SkipTutorial",
                dialog,
                new Vector2(0.14f, 0.10f),
                new Vector2(0.48f, 0.28f),
                "今回はスキップ",
                SkipContextTutorial,
                new Color32(91, 84, 79, 255),
                25);
        }

        private bool AdvanceTutorialAfterCard(CardKind usedKind)
        {
            if (_tutorialStep == TutorialStep.Attack &&
                usedKind == CardKind.Attack)
            {
                _tutorialStep = TutorialStep.EndTurn;
                return false;
            }

            if (_tutorialStep == TutorialStep.Charge &&
                usedKind == CardKind.Charge)
            {
                _tutorialStep = TutorialStep.None;
                _tutorialCompletedThisSession = true;
                return true;
            }

            return false;
        }

        private void ShowTutorialCompletion()
        {
            ShowMessageDialog(
                "操作ガイド完了",
                "基本操作は完了です。\n" +
                "デッキ内カードのコスト合計が、戦闘で使う\n" +
                "エーテル全体の色と個数になります。\n" +
                "詳しい説明は「遊び方」から確認できます。");
        }

        private void SkipContextTutorial()
        {
            _tutorialStep = TutorialStep.None;
            _tutorialCompletedThisSession = true;
            if (_battle != null && _battle.Phase == BattlePhase.PlayerTurn)
            {
                ShowBattle();
            }
        }

        private void EnsureTutorialCardCostForCurrentStep()
        {
            if (_battle == null ||
                _battle.Phase != BattlePhase.PlayerTurn)
            {
                return;
            }

            if (_tutorialStep == TutorialStep.Attack)
            {
                EnsureTutorialCardCost(CardKind.Attack);
            }
            else if (_tutorialStep == TutorialStep.Charge)
            {
                EnsureTutorialCardCost(CardKind.Charge);
            }
        }

        private bool EnsureTutorialCardCost(CardKind kind)
        {
            var cost = CardCatalog.GetCost(kind);
            if (_battle.Pool.CurrentTotal < cost.Values.Sum())
            {
                return false;
            }

            foreach (var pair in cost)
            {
                while (_battle.Pool.Current[pair.Key] < pair.Value)
                {
                    Dictionary<EtherType, int> source;
                    if (_battle.Pool.Unused[pair.Key] > 0)
                    {
                        source = _battle.Pool.Unused;
                    }
                    else if (_battle.Pool.Spent[pair.Key] > 0)
                    {
                        source = _battle.Pool.Spent;
                    }
                    else
                    {
                        return false;
                    }

                    EtherType? exchangeType = null;
                    foreach (EtherType type in Enum.GetValues(typeof(EtherType)))
                    {
                        var required = cost.TryGetValue(type, out var value)
                            ? value
                            : 0;
                        if (_battle.Pool.Current[type] > required)
                        {
                            exchangeType = type;
                            break;
                        }
                    }

                    if (!exchangeType.HasValue)
                    {
                        return false;
                    }

                    source[pair.Key]--;
                    _battle.Pool.Current[pair.Key]++;
                    _battle.Pool.Current[exchangeType.Value]--;
                    source[exchangeType.Value]++;
                }
            }

            return true;
        }

        private void ShowTutorialBook(int pageIndex)
        {
            var pageCount = Math.Min(
                TutorialPageTitles.Length,
                TutorialPageSet.PageCount);
            if (pageCount <= 0)
            {
                return;
            }

            _tutorialPageIndex = Mathf.Clamp(pageIndex, 0, pageCount - 1);
            if (_tutorialBookOverlay != null)
            {
                Destroy(_tutorialBookOverlay.gameObject);
            }

            _tutorialBookOverlay = CreateOverlay("TutorialBookOverlay");
            var dialog = UiFactory.CreatePanel(
                "TutorialBook",
                _tutorialBookOverlay,
                new Vector2(0.07f, 0.04f),
                new Vector2(0.93f, 0.96f),
                new Color32(42, 45, 59, 255));
            UiFactory.CreateText(
                "BookTitle",
                dialog,
                new Vector2(0.05f, 0.89f),
                new Vector2(0.80f, 0.98f),
                $"遊び方　－　{TutorialPageTitles[_tutorialPageIndex]}",
                32,
                TextAnchor.MiddleLeft,
                UiFactory.Accent);
            UiFactory.CreateButton(
                "CloseTutorialBook",
                dialog,
                new Vector2(0.84f, 0.90f),
                new Vector2(0.96f, 0.97f),
                "閉じる",
                CloseTutorialBook,
                new Color32(91, 84, 79, 255),
                20);

            var pageImage = UiFactory.CreatePanel(
                "TutorialPageImage",
                dialog,
                new Vector2(0.08f, 0.27f),
                new Vector2(0.92f, 0.87f),
                new Color32(18, 20, 27, 255));
            var pageSprite = TutorialPageSet.GetPage(_tutorialPageIndex);
            var image = pageImage.GetComponent<Image>();
            image.sprite = pageSprite;
            image.preserveAspect = true;
            image.color = Color.white;
            pageImage.gameObject
                .AddComponent<TutorialSwipeHandler>()
                .Configure(ChangeTutorialPage);

            UiFactory.CreateText(
                "PageDescription",
                dialog,
                new Vector2(0.08f, 0.15f),
                new Vector2(0.92f, 0.27f),
                TutorialPageDescriptions[_tutorialPageIndex],
                23,
                TextAnchor.MiddleCenter);
            UiFactory.CreateText(
                "PageNumber",
                dialog,
                new Vector2(0.43f, 0.04f),
                new Vector2(0.57f, 0.13f),
                $"{_tutorialPageIndex + 1} / {pageCount}",
                23);

            var previous = UiFactory.CreateButton(
                "PreviousTutorialPage",
                dialog,
                new Vector2(0.08f, 0.04f),
                new Vector2(0.30f, 0.13f),
                "＜ 前のページ",
                () => ChangeTutorialPage(-1),
                new Color32(72, 81, 104, 255),
                22);
            previous.interactable = _tutorialPageIndex > 0;
            var next = UiFactory.CreateButton(
                "NextTutorialPage",
                dialog,
                new Vector2(0.70f, 0.04f),
                new Vector2(0.92f, 0.13f),
                "次のページ ＞",
                () => ChangeTutorialPage(1),
                UiFactory.Green,
                22);
            next.interactable = _tutorialPageIndex < pageCount - 1;
        }

        private void ChangeTutorialPage(int delta)
        {
            var nextPage = _tutorialPageIndex + delta;
            var pageCount = Math.Min(
                TutorialPageTitles.Length,
                TutorialPageSet.PageCount);
            if (nextPage < 0 || nextPage >= pageCount)
            {
                return;
            }

            ShowTutorialBook(nextPage);
        }

        private void CloseTutorialBook()
        {
            if (_tutorialBookOverlay == null)
            {
                return;
            }

            Destroy(_tutorialBookOverlay.gameObject);
            _tutorialBookOverlay = null;
        }

        private RectTransform CreateOverlay(string name)
        {
            return UiFactory.CreatePanel(
                name,
                _screenRoot,
                Vector2.zero,
                Vector2.one,
                new Color(0f, 0f, 0f, 0.78f));
        }

        private void ShowMessageDialog(
            string title,
            string message,
            Action onClose = null)
        {
            var overlay = CreateOverlay("MessageOverlay");
            var dialog = UiFactory.CreatePanel(
                "Dialog",
                overlay,
                new Vector2(0.25f, 0.28f),
                new Vector2(0.75f, 0.72f),
                new Color32(42, 45, 59, 255));
            UiFactory.CreateText(
                "Title",
                dialog,
                new Vector2(0.08f, 0.65f),
                new Vector2(0.92f, 0.92f),
                title,
                37,
                TextAnchor.MiddleCenter,
                UiFactory.Accent);
            UiFactory.CreateText(
                "Message",
                dialog,
                new Vector2(0.08f, 0.32f),
                new Vector2(0.92f, 0.65f),
                message,
                27);
            UiFactory.CreateButton(
                "Close",
                dialog,
                new Vector2(0.34f, 0.09f),
                new Vector2(0.66f, 0.26f),
                "閉じる",
                () =>
                {
                    Destroy(overlay.gameObject);
                    onClose?.Invoke();
                },
                UiFactory.Green,
                27);
        }

        private string GetDetailedCardText(CardInstance card, bool usable)
        {
            if (card != null)
            {
                var state = usable
                    ? "使用可能"
                    : CardCatalog.IsPersistent(card.Kind) &&
                      card.PersistentActivated
                        ? "使用不可：発動済み"
                        : CardCatalog.IsOncePerBattle(card) &&
                          card.UsedThisBattle
                            ? "使用不可：戦闘中1回を使用済み"
                            : card.CooldownRemaining > 0
                                ? $"使用不可：残り{card.CooldownRemaining}ターン"
                                : "使用不可：必要なエーテルが不足";
                var genericDragTarget = CardCatalog.IsAttack(card.Kind)
                    ? "ドラッグ先：対象の敵"
                    : "ドラッグ先：中央の説明欄";
                return
                    $"効果：{CardCatalog.GetShortDescription(card)}\n" +
                    $"{GetCooldownLabel(card)}　{state}　{genericDragTarget}";
            }

            string effect;
            switch (card.Kind)
            {
                case CardKind.Attack:
                    effect =
                        $"選択中の敵単体へ{_battle.PreviewValue(CardKind.Attack)}ダメージ";
                    break;
                case CardKind.HeavyAttack:
                    effect =
                        $"選択中の敵単体へ{_battle.PreviewValue(CardKind.HeavyAttack)}ダメージ";
                    break;
                case CardKind.AreaAttack:
                    effect =
                        $"敵全体へ{_battle.PreviewValue(CardKind.AreaAttack)}ダメージ";
                    break;
                case CardKind.GrowthAttack:
                    effect =
                        $"選択中の敵単体へ{_battle.PreviewValue(card)}ダメージ。" +
                        "使用後、このカードの基礎ダメージ+3";
                    break;
                case CardKind.RandomBarrage:
                    effect =
                        $"ランダムな敵へ{_battle.PreviewValue(card)}ダメージを4回";
                    break;
                case CardKind.RedPulseAttack:
                    effect =
                        $"赤エーテル総数{_battle.TotalRedEtherCount}個を参照し、" +
                        $"選択中の敵単体へ{_battle.PreviewValue(card)}ダメージ";
                    break;
                case CardKind.PiercingAreaAttack:
                    effect =
                        $"敵全体へ{_battle.PreviewValue(card)}貫通ダメージ";
                    break;
                case CardKind.Defense:
                    effect =
                        $"自分へ{_battle.PreviewValue(CardKind.Defense)}シールド";
                    break;
                case CardKind.StrongDefense:
                    effect =
                        $"自分へ{_battle.PreviewValue(CardKind.StrongDefense)}シールド";
                    break;
                case CardKind.AutoDefense:
                    effect =
                        $"自動防御を{_battle.PreviewValue(CardKind.AutoDefense)}層獲得" +
                        "（ターン開始時に層数分の盾を得て1層減少）";
                    break;
                case CardKind.GuardContinuance:
                    effect =
                        $"自動防御を{_battle.PreviewValue(card)}層、" +
                        "防御維持を2層獲得";
                    break;
                case CardKind.MirrorShield:
                    effect =
                        $"現在のシールド{_session.Player.Shield}を" +
                        $"{_battle.PreviewValue(card)}へ倍化し、防御維持を2層獲得";
                    break;
                case CardKind.Charge:
                    effect = "次に使う攻撃または防御の効果量を+3";
                    break;
                case CardKind.Resonance:
                    effect =
                        $"赤エーテル総数{_battle.TotalRedEtherCount}個分、" +
                        "次に使う攻撃または防御を強化";
                    break;
                case CardKind.HealCharge:
                    effect = "HPを6回復";
                    break;
                case CardKind.EtherConversion:
                    effect =
                        "コスト支払い後の手番エーテルをランダムな1色へ統一し、" +
                        "同じ色を2個追加";
                    break;
                case CardKind.Persistent:
                    effect =
                        "この戦闘中、攻撃カード3回使用ごとに攻撃力が永続で+1" +
                        "（同じ効果は重複）";
                    break;
                case CardKind.GuardCyclePersistent:
                    effect =
                        "発動後にカードを3枚使用するごとに自動防御を2追加" +
                        "（同じ効果は重複）";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var availability = usable
                ? "使用可能"
                : CardCatalog.IsPersistent(card.Kind) &&
                  card.PersistentActivated
                    ? "使用不可：このカードは発動済み"
                    : card.CooldownRemaining > 0
                        ? $"使用不可：残り{card.CooldownRemaining}ターン"
                        : "使用不可：必要なエーテルが不足";
            var dragTarget = CardCatalog.IsAttack(card.Kind)
                ? "ドラッグ先：対象の敵"
                : "ドラッグ先：中央の説明欄";
            var cooldownState = card.CooldownRemaining > 0
                ? $"（残り{card.CooldownRemaining}ターン）"
                : string.Empty;
            return $"効果：{effect}\n" +
                   $"{GetCooldownLabel(card.Kind)}{cooldownState}　" +
                   $"{availability}　{dragTarget}";
        }

        private static string GetCooldownLabel(CardKind kind)
        {
            return CardCatalog.IsPersistent(kind) ||
                   CardCatalog.GetCooldown(kind) == 0
                ? "使用回数：戦闘中1回"
                : $"クールタイム：{CardCatalog.GetCooldown(kind)}";
        }

        private static string GetCooldownLabel(CardInstance card)
        {
            if (CardCatalog.IsPersistent(card.Kind))
            {
                return "使用回数：戦闘中1回";
            }

            if (CardCatalog.IsOncePerBattle(card))
            {
                return "使用回数：戦闘中1回";
            }

            return $"クールタイム：{CardCatalog.GetCooldown(card)}";
        }

        private static string GetUpgradedCardDescription(CardInstance card)
        {
            var upgraded = new CardInstance(-1, card.Kind)
            {
                IsUpgraded = true
            };
            return
                $"{CardCatalog.GetShortDescription(upgraded)}\n" +
                $"コスト：{CardCatalog.GetCostText(upgraded)}\n" +
                GetCooldownLabel(upgraded);
        }

        private static string GetCategoryLabel(CardCategory category)
        {
            switch (category)
            {
                case CardCategory.Attack:
                    return "攻撃";
                case CardCategory.Defense:
                    return "防御";
                case CardCategory.Charge:
                    return "チャージ";
                case CardCategory.Persistent:
                    return "持続";
                case CardCategory.Special:
                    return "特殊";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(category),
                        category,
                        null);
            }
        }

        private bool IsSelectedEnemyAlive()
        {
            return _selectedEnemyIndex >= 0 &&
                   _selectedEnemyIndex < _battle.Enemies.Count &&
                   _battle.Enemies[_selectedEnemyIndex].IsAlive;
        }

        private int FindFirstAliveEnemy()
        {
            for (var index = 0; index < _battle.Enemies.Count; index++)
            {
                if (_battle.Enemies[index].IsAlive)
                {
                    return index;
                }
            }

            return -1;
        }

        private static string GetEnemyIntent(EnemyState enemy)
        {
            switch (enemy.NextAction)
            {
                case EnemyActionKind.Attack:
                    return
                        $"攻撃 {Math.Max(0, enemy.Attack - enemy.Weakness)}";
                case EnemyActionKind.Defense:
                    return
                        $"防御 {Math.Max(0, enemy.Defense - enemy.Weakness)}";
                case EnemyActionKind.Weaken:
                    return "弱体 3";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static string GetEnemyStatusText(EnemyState enemy)
        {
            var statuses = new List<string>();
            if (enemy.Fire > 0)
            {
                statuses.Add($"炎{enemy.Fire}");
            }

            if (enemy.Bleed > 0)
            {
                statuses.Add($"出血{enemy.Bleed}");
            }

            if (enemy.Weakness > 0)
            {
                statuses.Add($"弱化{enemy.Weakness}");
            }

            return statuses.Count == 0
                ? string.Empty
                : $"\n状態：{string.Join("・", statuses)}";
        }

        private static string GetStageIcon(StageKind kind)
        {
            switch (kind)
            {
                case StageKind.Start:
                    return "●";
                case StageKind.Battle:
                    return "⚔";
                case StageKind.Fountain:
                    return "♨";
                case StageKind.Shop:
                    return "◆";
                case StageKind.RandomEvent:
                    return "?";
                case StageKind.Reward:
                    return "▣";
                case StageKind.Tool:
                    return "✦";
                case StageKind.Boss:
                    return "★";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        private string GetNodeStateText(MapNode node)
        {
            var fogText = _session.IsNodeInFog(node)
                ? $"・霧-{_session.CurrentFogDamage}HP"
                : string.Empty;
            if (node.Id == _session.CurrentNodeId)
            {
                return $"滞在中{fogText}";
            }

            if (node.Cleared)
            {
                return $"訪問済み{fogText}";
            }

            if (node.Visited)
            {
                return _session.IsNodeInFog(node)
                    ? $"終了{fogText}"
                    : "イベント終了";
            }

            return $"未訪問{fogText}";
        }

        private Color GetMapNodeColor(MapNode node, bool canTravel)
        {
            if (node.Id == _session.CurrentNodeId)
            {
                return _session.IsNodeInFog(node)
                    ? new Color32(74, 91, 99, 255)
                    : UiFactory.Green;
            }

            if (_session.IsNodeInFog(node))
            {
                return canTravel
                    ? new Color32(72, 86, 94, 255)
                    : new Color32(43, 54, 61, 255);
            }

            if (!canTravel)
            {
                return node.Visited
                    ? new Color32(66, 68, 73, 255)
                    : new Color32(51, 54, 64, 255);
            }

            if (node.Visited)
            {
                return new Color32(96, 91, 77, 255);
            }

            switch (node.Kind)
            {
                case StageKind.Battle:
                    return new Color32(119, 63, 67, 255);
                case StageKind.Fountain:
                    return new Color32(55, 111, 139, 255);
                case StageKind.Shop:
                    return new Color32(121, 93, 50, 255);
                case StageKind.RandomEvent:
                    return new Color32(92, 75, 112, 255);
                case StageKind.Reward:
                    return new Color32(127, 105, 47, 255);
                case StageKind.Tool:
                    return new Color32(49, 121, 104, 255);
                case StageKind.Boss:
                    return new Color32(109, 55, 125, 255);
                default:
                    return UiFactory.PanelLight;
            }
        }

        private string GetFogStatusText()
        {
            if (_session.FogDepth < 0)
            {
                return
                    $"霧まで{-_session.FogDepth}行・" +
                    $"進入{_session.CurrentFogDamage}ダメージ";
            }

            return
                $"霧 深度{_session.FogDepth}・" +
                $"進入{_session.CurrentFogDamage}ダメージ";
        }

        private string GetOwnedToolSummary()
        {
            if (_session.Player.Tools.Count == 0)
            {
                return "道具 未所持";
            }

            var total = _session.Player.Tools.Values.Sum();
            return $"道具 {_session.Player.Tools.Count}種・合計{total}個";
        }

        private string GetBattleToolStatus()
        {
            return GetOwnedToolSummary();
        }

        private void ShowOwnedCardsOverlay()
        {
            var overlay = CreateOverlay("OwnedCardsOverlay");
            var dialog = UiFactory.CreatePanel(
                "OwnedCardsDialog",
                overlay,
                new Vector2(0.08f, 0.08f),
                new Vector2(0.92f, 0.92f),
                new Color32(42, 45, 59, 255));
            UiFactory.CreateText(
                "Title",
                dialog,
                new Vector2(0.05f, 0.88f),
                new Vector2(0.95f, 0.98f),
                "所持カード一覧",
                38,
                TextAnchor.MiddleCenter,
                UiFactory.Accent);
            var categories = new[]
            {
                CardCategory.Attack,
                CardCategory.Defense,
                CardCategory.Charge,
                CardCategory.Persistent,
                CardCategory.Special
            };
            for (var index = 0; index < categories.Length; index++)
            {
                var category = categories[index];
                var left = 0.04f + index * 0.19f;
                UiFactory.CreateButton(
                    $"OwnedCardTab_{category}",
                    dialog,
                    new Vector2(left, 0.80f),
                    new Vector2(left + 0.17f, 0.87f),
                    GetCategoryLabel(category),
                    () =>
                    {
                        _ownedCardCategory = category;
                        Destroy(overlay.gameObject);
                        ShowOwnedCardsOverlay();
                    },
                    _ownedCardCategory == category
                        ? UiFactory.Green
                        : new Color32(72, 81, 104, 255),
                    21);
            }

            var scroll = UiFactory.CreateScrollView(
                "OwnedCards",
                dialog,
                new Vector2(0.04f, 0.17f),
                new Vector2(0.96f, 0.78f),
                true,
                false,
                out var content);
            var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16;
            layout.padding = new RectOffset(16, 16, 12, 12);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            var cards = _session.Player.Deck
                .Where(
                    card =>
                        CardCatalog.GetCategory(card.Kind) ==
                        _ownedCardCategory)
                .OrderBy(card => card.Id)
                .ToList();
            foreach (var card in cards)
            {
                var panel = UiFactory.CreatePanel(
                    $"OwnedCard_{card.Id}",
                    content,
                    Vector2.zero,
                    Vector2.zero,
                    UiFactory.GetCardColor(card.Kind));
                var element = panel.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = 310;
                element.minWidth = 310;
                element.preferredHeight = 430;
                UiFactory.CreateText(
                    "Text",
                    panel,
                    new Vector2(0.05f, 0.05f),
                    new Vector2(0.95f, 0.95f),
                    $"{(card.IsUpgraded ? "【強化済】" : string.Empty)}" +
                    $"【{CardCatalog.GetRarityLabel(card.Kind)}】\n" +
                    $"{CardCatalog.GetName(card.Kind)}\n\n" +
                    $"{CardCatalog.GetShortDescription(card)}\n\n" +
                    $"コスト：{CardCatalog.GetCostText(card)}\n" +
                    GetCooldownLabel(card),
                    22);
            }

            UiFactory.CreateButton(
                "CloseOwnedCards",
                dialog,
                new Vector2(0.36f, 0.04f),
                new Vector2(0.64f, 0.13f),
                "閉じる",
                () => Destroy(overlay.gameObject),
                new Color32(91, 84, 79, 255),
                25);
        }

        private void ShowOwnedToolsOverlay()
        {
            var overlay = CreateOverlay("OwnedToolsOverlay");
            var dialog = UiFactory.CreatePanel(
                "OwnedToolsDialog",
                overlay,
                new Vector2(0.12f, 0.10f),
                new Vector2(0.88f, 0.90f),
                new Color32(42, 45, 59, 255));
            UiFactory.CreateText(
                "Title",
                dialog,
                new Vector2(0.06f, 0.86f),
                new Vector2(0.94f, 0.97f),
                "所持道具",
                38,
                TextAnchor.MiddleCenter,
                UiFactory.Accent);

            var tools = _session.Player.Tools
                .OrderBy(
                    pair =>
                        CarryToolCatalog.GetRarity(pair.Key))
                .ThenBy(pair => pair.Key)
                .ToList();
            for (var index = 0; index < tools.Count; index++)
            {
                var pair = tools[index];
                var column = index % 2;
                var row = index / 2;
                var left = column == 0 ? 0.06f : 0.51f;
                var right = column == 0 ? 0.49f : 0.94f;
                var top = 0.83f - row * 0.135f;
                var panel = UiFactory.CreatePanel(
                    $"OwnedTool_{pair.Key}",
                    dialog,
                    new Vector2(left, top - 0.115f),
                    new Vector2(right, top),
                    GetCarryToolColor(pair.Key));
                UiFactory.CreateText(
                    "Text",
                    panel,
                    new Vector2(0.03f, 0.08f),
                    new Vector2(0.97f, 0.92f),
                    $"【{CarryToolCatalog.GetRarityLabel(pair.Key)}】" +
                    $"{CarryToolCatalog.GetName(pair.Key)}" +
                    (pair.Value > 1 ? $" ×{pair.Value}" : string.Empty) +
                    "\n" +
                    CarryToolCatalog.GetDescription(pair.Key),
                    23,
                    TextAnchor.MiddleLeft);
            }

            UiFactory.CreateButton(
                "CloseOwnedTools",
                dialog,
                new Vector2(0.36f, 0.04f),
                new Vector2(0.64f, 0.13f),
                "閉じる",
                () => Destroy(overlay.gameObject),
                UiFactory.Green,
                25);
        }

        private static Color GetCarryToolColor(CarryToolKind kind)
        {
            switch (CarryToolCatalog.GetRarity(kind))
            {
                case CarryToolRarity.Normal:
                    return new Color32(65, 91, 119, 255);
                case CarryToolRarity.Rare:
                    return new Color32(116, 88, 132, 255);
                case CarryToolRarity.Boss:
                    return new Color32(137, 103, 52, 255);
                case CarryToolRarity.Special:
                    return new Color32(176, 142, 55, 255);
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private IEnumerator CaptureForValidationIfRequested()
        {
            var arguments = Environment.GetCommandLineArgs();
            var markerIndex = Array.IndexOf(arguments, "-lostPageCapture");
            if (markerIndex < 0 || markerIndex + 1 >= arguments.Length)
            {
                yield break;
            }

            _tutorialCompletedThisSession = true;
            _tutorialStep = TutorialStep.None;
            var captureTutorialFlow =
                Array.IndexOf(arguments, "-lostPageCaptureTutorialFlow") >= 0;
            var captureTutorialSwipe =
                Array.IndexOf(arguments, "-lostPageCaptureTutorialSwipe") >= 0;
            var captureCardScroll =
                Array.IndexOf(arguments, "-lostPageCaptureCardScroll") >= 0;
            var captureCardFilter =
                Array.IndexOf(arguments, "-lostPageCaptureCardFilter") >= 0;
            var captureShop =
                Array.IndexOf(arguments, "-lostPageCaptureShop") >= 0;
            var captureNextLayer =
                Array.IndexOf(arguments, "-lostPageCaptureNextLayer") >= 0;
            var captureSpecialMap =
                Array.IndexOf(arguments, "-lostPageCaptureSpecialMap") >= 0;
            var captureFogMap =
                Array.IndexOf(arguments, "-lostPageCaptureFogMap") >= 0;
            var captureRandomEvent =
                Array.IndexOf(arguments, "-lostPageCaptureRandomEvent") >= 0;
            var captureRewardStage =
                Array.IndexOf(arguments, "-lostPageCaptureRewardStage") >= 0;
            var captureShopReentry =
                Array.IndexOf(arguments, "-lostPageCaptureShopReentry") >= 0;
            var captureBossToolReward =
                Array.IndexOf(arguments, "-lostPageCaptureBossToolReward") >= 0;
            var captureExpandedBattle =
                Array.IndexOf(arguments, "-lostPageCaptureExpandedBattle") >= 0;
            var captureExpandedStatuses =
                Array.IndexOf(arguments, "-lostPageCaptureExpandedStatuses") >= 0;
            var captureOwnedTools =
                Array.IndexOf(arguments, "-lostPageCaptureOwnedTools") >= 0;
            var captureEtherDrawAnimation =
                Array.IndexOf(
                    arguments,
                    "-lostPageCaptureEtherDrawAnimation") >= 0;
            var captureEtherRefillAnimation =
                Array.IndexOf(
                    arguments,
                    "-lostPageCaptureEtherRefillAnimation") >= 0;
            var captureEtherPaymentAnimation =
                Array.IndexOf(
                    arguments,
                    "-lostPageCaptureEtherPaymentAnimation") >= 0;
            var captureBarrageHitAnimation =
                Array.IndexOf(
                    arguments,
                    "-lostPageCaptureBarrageHitAnimation") >= 0;
            var hiddenPersistentCardId = -1;

            var layerMarker = Array.IndexOf(arguments, "-lostPageCaptureLayer");
            if (layerMarker >= 0 &&
                layerMarker + 1 < arguments.Length &&
                int.TryParse(arguments[layerMarker + 1], out var captureLayer))
            {
                AdvanceToLayerForValidation(captureLayer);
                ShowMap();
            }

            if (Array.IndexOf(arguments, "-lostPageCaptureTutorialWelcome") >= 0)
            {
                _tutorialCompletedThisSession = false;
                _tutorialOfferedThisSession = false;
                TravelToStageForValidation(StageKind.Battle);
                StartBattle();
            }

            if (Array.IndexOf(arguments, "-lostPageCaptureTutorialGuide") >= 0)
            {
                _tutorialCompletedThisSession = false;
                _tutorialOfferedThisSession = true;
                TravelToStageForValidation(StageKind.Battle);
                StartBattle();
                _tutorialStep = TutorialStep.Attack;
                EnsureTutorialCardCost(CardKind.Attack);
                ShowBattle();
            }

            if (captureTutorialFlow)
            {
                _tutorialCompletedThisSession = false;
                _tutorialOfferedThisSession = true;
                TravelToStageForValidation(StageKind.Battle);
                StartBattle();
                _tutorialStep = TutorialStep.Attack;
                EnsureTutorialCardCost(CardKind.Attack);
                _selectedCard = _session.Player.Deck.First(
                    card => card.Kind == CardKind.Attack);
                UseSelectedCard();
                ShowCarrySelection();
            }

            if (captureCardScroll)
            {
                _session.Player.AddCard(CardKind.HeavyAttack);
                _session.Player.AddCard(CardKind.AreaAttack);
                _session.Player.AddCard(CardKind.StrongDefense);
                _session.Player.AddCard(CardKind.AutoDefense);
                _session.Player.AddCard(CardKind.Resonance);
                TravelToStageForValidation(StageKind.Battle);
                StartBattle();
            }

            if (captureCardFilter)
            {
                _session.Player.AddCard(CardKind.HeavyAttack);
                _session.Player.AddCard(CardKind.StrongDefense);
                _session.Player.AddCard(CardKind.Resonance);
                _session.Player.AddCard(CardKind.Persistent);
                _session.Player.AddCard(CardKind.DivineStrike);
                TravelToStageForValidation(StageKind.Battle);
                StartBattle();
                var filterModes = new[]
                {
                    CardFilterMode.Attack,
                    CardFilterMode.Defense,
                    CardFilterMode.Charge,
                    CardFilterMode.Persistent,
                    CardFilterMode.Special
                };
                var filterCategories = new[]
                {
                    CardCategory.Attack,
                    CardCategory.Defense,
                    CardCategory.Charge,
                    CardCategory.Persistent,
                    CardCategory.Special
                };
                for (var index = 0; index < filterModes.Length; index++)
                {
                    SetCardFilterMode(filterModes[index]);
                    var filteredCards = GetSortedVisibleCards();
                    if (filteredCards.Count == 0 ||
                        filteredCards.Any(
                            card =>
                                CardCatalog.GetCategory(card.Kind) !=
                                filterCategories[index]))
                    {
                        throw new InvalidOperationException(
                            $"{filterCategories[index]}フィルターの対象が不正です。");
                    }
                }

                _cardSortMode = CardSortMode.Acquired;
                SetCardFilterMode(CardFilterMode.Attack);
                var attackCard = GetSortedVisibleCards().First();
                EnsureCardCostForValidation(attackCard.Kind);
                _selectedCard = attackCard;
                _battle.UseCard(attackCard, FindFirstAliveEnemy());
                FinishCardUse(attackCard.Kind);
                if (_cardFilterMode != CardFilterMode.Attack ||
                    _cardSortMode != CardSortMode.Acquired)
                {
                    throw new InvalidOperationException(
                        "カード使用後にフィルターまたは並び順が失われました。");
                }

                OnMapNodePressed(_session.CurrentNode.Neighbors[0]);
                if (_cardFilterMode != CardFilterMode.All ||
                    _cardSortMode != CardSortMode.UsableFirst)
                {
                    throw new InvalidOperationException(
                        "ステージ移動時にカード表示設定が初期化されませんでした。");
                }

                TravelToStageForValidation(StageKind.Battle);
                StartBattle();
                _cardSortMode = CardSortMode.Acquired;
                SetCardFilterMode(CardFilterMode.Attack);
            }

            if (captureShop)
            {
                _session.Player.Gold = 40;
                TravelToStageForValidation(StageKind.Shop);
                ShowShop();
            }

            if (captureSpecialMap)
            {
                _session.Player.AddTool(CarryToolKind.AttackBoost);
                _mapScrollY = 0f;
                ShowMap();
            }

            if (captureFogMap)
            {
                for (var move = 0; move < 5; move++)
                {
                    _session.TravelTo(
                        _session.CurrentNodeId == 0 ? 1 : 0);
                }

                ShowMap();
                OnMapNodePressed(0);
                yield return new WaitForSecondsRealtime(0.35f);
                var fogDialogClose = GameObject.Find("Close");
                if (GameObject.Find("MessageOverlay") == null ||
                    fogDialogClose == null)
                {
                    throw new InvalidOperationException(
                        "霧ダメージダイアログが表示されませんでした。");
                }

                fogDialogClose.GetComponent<Button>().onClick.Invoke();
                _message =
                    $"霧検証：6移動、HP " +
                    $"{_session.Player.Hp}/{_session.Player.MaxHp}";
                _mapScrollY = 0f;
                ShowMap();
            }

            if (captureRandomEvent)
            {
                _session.Player.AddTool(CarryToolKind.AttackBoost);
                _session.Player.Hp = 70;
                TravelToStageForValidation(StageKind.RandomEvent);
                ShowRandomEvent();
            }

            if (captureRewardStage)
            {
                _session.Player.AddTool(CarryToolKind.AttackBoost);
                TravelToStageForValidation(StageKind.Reward);
                ShowRewardStage();
            }

            if (captureShopReentry)
            {
                _session.Player.AddTool(CarryToolKind.AttackBoost);
                _session.Player.Gold = 40;
                TravelToStageForValidation(StageKind.Shop);
                var shopNodeId = _session.CurrentNodeId;
                _session.CurrentNode.Cleared = true;
                _session.TravelTo(_session.CurrentNode.Neighbors[0]);
                if (_session.TravelTo(shopNodeId))
                {
                    throw new InvalidOperationException(
                        "ショップ再訪が初回訪問として扱われました。");
                }

                _message = "商人のもとへ戻りました。";
                ShowShop();
            }

            if (captureNextLayer)
            {
                CompleteCurrentLayerForValidation();
                ShowBossVictory();
            }

            if (captureBossToolReward)
            {
                CompleteCurrentLayerForValidation();
                ShowBossToolReward();
            }

            if (captureExpandedBattle)
            {
                var expandedCard =
                    _session.Player.AddCard(CardKind.GrowthAttack);
                _session.Player.AddCard(CardKind.RandomBarrage);
                _session.Player.AddCard(CardKind.GuardContinuance);
                _session.Player.AddCard(CardKind.GuardCyclePersistent);
                _session.Player.AddTool(CarryToolKind.RedCrystal);
                _session.Player.AddTool(CarryToolKind.BlueCrystal);
                TravelToStageForValidation(StageKind.Battle);
                StartBattle();
                _selectedCard = expandedCard;
                EnsureCardCostForValidation(CardKind.GrowthAttack);
                ShowBattle();
            }

            if (captureExpandedStatuses)
            {
                _session.Player.Deck.Clear();
                var statusAttack =
                    _session.Player.AddCard(CardKind.Attack);
                var statusGuard =
                    _session.Player.AddCard(CardKind.GuardContinuance);
                var statusCycle =
                    _session.Player.AddCard(CardKind.GuardCyclePersistent);
                _session.Player.AddTool(CarryToolKind.BlueCrystal);
                _session.Player.AddTool(CarryToolKind.FirstAttackPierce);
                TravelToStageForValidation(StageKind.Battle);
                StartBattle();
                EnsureCardCostForValidation(CardKind.GuardContinuance);
                _battle.UseCard(statusGuard, 0);
                EnsureCardCostForValidation(CardKind.GuardCyclePersistent);
                _battle.UseCard(statusCycle, 0);
                _selectedCard = statusAttack;
                ShowBattle();
            }

            if (captureOwnedTools)
            {
                _session.Player.AddTool(CarryToolKind.SmallBag);
                _session.Player.AddTool(CarryToolKind.BigBag);
                _session.Player.AddTool(CarryToolKind.BigBag);
                _session.Player.AddTool(CarryToolKind.StarMass);
                _session.Player.AddTool(CarryToolKind.EnergyCore);
                ShowMap();
                ShowOwnedToolsOverlay();
            }

            if (captureEtherDrawAnimation)
            {
                TravelToStageForValidation(StageKind.Battle);
                StartBattle();
            }

            if (captureEtherRefillAnimation)
            {
                TravelToStageForValidation(StageKind.Battle);
                StartBattle();
                foreach (EtherType type in Enum.GetValues(typeof(EtherType)))
                {
                    _battle.Pool.Unused[type] = 0;
                    _battle.Pool.Current[type] = 0;
                    _battle.Pool.Spent[type] = 0;
                }

                _battle.Pool.Current[EtherType.Red] = 1;
                _battle.Pool.Current[EtherType.Blue] = 1;
                _battle.Pool.Spent[EtherType.Red] = 3;
                _battle.Pool.Spent[EtherType.Blue] = 2;
                _battle.Pool.Spent[EtherType.Yellow] = 2;
                _battle.Pool.Spent[EtherType.Purple] = 2;
                _session.Player.Shield = 999;
                _message = _battle.EndPlayerTurn(
                    new[] { EtherType.Red, EtherType.Blue });
                ShowBattle();
                _isResolvingAction = true;
                StartCoroutine(PlayTurnEtherDrawThenFinish(null));
            }

            if (captureEtherPaymentAnimation)
            {
                TravelToStageForValidation(StageKind.Battle);
                StartBattle();
                _selectedCard = _session.Player.Deck.First(
                    card => card.Kind == CardKind.Attack);
                EnsureCardCostForValidation(CardKind.Attack);
                ShowBattle();
                UseSelectedCard();
            }

            if (captureBarrageHitAnimation)
            {
                var barrageCard =
                    _session.Player.AddCard(CardKind.RandomBarrage);
                TravelToStageForValidation(StageKind.Battle);
                StartBattle();
                _selectedCard = barrageCard;
                EnsureCardCostForValidation(CardKind.RandomBarrage);
                ShowBattle();
                UseSelectedCard();
            }

            if (Array.IndexOf(arguments, "-lostPageCaptureBattle") >= 0)
            {
                TravelToStageForValidation(StageKind.Battle);
                StartBattle();
            }

            if (Array.IndexOf(
                    arguments,
                    "-lostPageCaptureCarryToolBattle") >= 0)
            {
                _session.Player.AddTool(CarryToolKind.AttackBoost);
                TravelToStageForValidation(StageKind.Battle);
                StartBattle();
                _selectedCard = _session.Player.Deck.First(
                    card => card.Kind == CardKind.Attack);
                EnsureCardCostForValidation(CardKind.Attack);
                ShowBattle();
            }

            if (Array.IndexOf(arguments, "-lostPageCaptureCostIcons") >= 0)
            {
                _session.Player.AddCard(CardKind.HeavyAttack);
                TravelToStageForValidation(StageKind.Battle);
                StartBattle();
                _selectedCard = _session.Player.Deck.First(
                    card => card.Kind == CardKind.HeavyAttack);
                EnsureCardCostForValidation(CardKind.HeavyAttack);
                ShowBattle();
            }

            if (Array.IndexOf(arguments, "-lostPageCaptureCooldown") >= 0)
            {
                var cooldownCard =
                    _session.Player.AddCard(CardKind.HeavyAttack);
                TravelToStageForValidation(StageKind.Battle);
                StartBattle();
                EnsureCardCostForValidation(CardKind.HeavyAttack);
                _selectedCard = cooldownCard;
                _message = _battle.UseCard(
                    cooldownCard,
                    FindFirstAliveEnemy());
                ShowBattle();
            }

            if (Array.IndexOf(
                    arguments,
                    "-lostPageCapturePersistentHidden") >= 0)
            {
                _session.Player.Deck.Clear();
                _session.Player.AddCard(CardKind.Attack);
                var persistentCard =
                    _session.Player.AddCard(CardKind.Persistent);
                TravelToStageForValidation(StageKind.Battle);
                StartBattle();
                EnsureCardCostForValidation(CardKind.Persistent);
                _selectedCard = persistentCard;
                _message = _battle.UseCard(persistentCard, 0);
                hiddenPersistentCardId = persistentCard.Id;
                ShowBattle();
            }

            var captureDragAttack =
                Array.IndexOf(arguments, "-lostPageCaptureDragAttack") >= 0;
            var captureDropAttack =
                Array.IndexOf(arguments, "-lostPageCaptureDropAttack") >= 0;
            var captureDropCharge =
                Array.IndexOf(arguments, "-lostPageCaptureDropCharge") >= 0;
            if (captureDragAttack || captureDropAttack || captureDropCharge)
            {
                if (_battle == null)
                {
                    TravelToStageForValidation(StageKind.Battle);
                    StartBattle();
                }

                EnsureCardCostForValidation(
                    captureDropCharge
                        ? CardKind.Charge
                        : CardKind.Attack);
                ShowBattle();
            }

            if (Array.IndexOf(arguments, "-lostPageCaptureNoAutoEnd") >= 0)
            {
                if (_battle == null)
                {
                    TravelToStageForValidation(StageKind.Battle);
                    StartBattle();
                }

                while (_battle.HasUsableCard &&
                       _battle.Phase == BattlePhase.PlayerTurn)
                {
                    var card = _session.Player.Deck.First(_battle.CanUse);
                    _battle.UseCard(card, FindFirstAliveEnemy());
                }

                ShowBattle();
            }

            if (Array.IndexOf(arguments, "-lostPageCaptureCarry") >= 0)
            {
                if (_battle == null)
                {
                    TravelToStageForValidation(StageKind.Battle);
                    StartBattle();
                }

                ShowCarrySelection();
            }

            yield return null;
            if (Array.IndexOf(arguments, "-lostPageCaptureCarrySelected") >= 0)
            {
                for (var index = 0; index < 2; index++)
                {
                    var token = GameObject.Find($"Token_{index}");
                    token?.GetComponent<Button>()?.onClick.Invoke();
                }
            }

            if (captureNextLayer)
            {
                GameObject.Find("NextLayer")
                    ?.GetComponent<Button>()
                    ?.onClick.Invoke();
            }

            if (Array.IndexOf(arguments, "-lostPageCaptureCarryCancelled") >= 0)
            {
                var cancel = GameObject.Find("CancelCarry");
                cancel?.GetComponent<Button>()?.onClick.Invoke();
            }

            if (Array.IndexOf(arguments, "-lostPageCaptureBossVictory") >= 0)
            {
                ShowBossVictory();
            }

            if (Array.IndexOf(arguments, "-lostPageCaptureTutorialBook") >= 0 ||
                captureTutorialSwipe)
            {
                var page = 0;
                var pageMarker =
                    Array.IndexOf(arguments, "-lostPageTutorialPage");
                if (pageMarker >= 0 &&
                    pageMarker + 1 < arguments.Length &&
                    int.TryParse(arguments[pageMarker + 1], out var pageNumber))
                {
                    page = pageNumber - 1;
                }

                ShowTutorialBook(page);
            }

            if (captureTutorialFlow)
            {
                for (var index = 0; index < 2; index++)
                {
                    var token = GameObject.Find($"Token_{index}");
                    token?.GetComponent<Button>()?.onClick.Invoke();
                }

                GameObject.Find("Confirm")?.GetComponent<Button>()?.onClick.Invoke();
                _selectedCard = _session.Player.Deck.First(
                    card => card.Kind == CardKind.Charge);
                UseSelectedCard();
            }

            if (captureTutorialSwipe)
            {
                var target = GameObject.Find("TutorialPageImage");
                if (target == null)
                {
                    throw new InvalidOperationException(
                        "スワイプ検証用のチュートリアル画像が見つかりません。");
                }

                var swipeData = new PointerEventData(EventSystem.current)
                {
                    position = new Vector2(920f, 400f)
                };
                ExecuteEvents.Execute<IBeginDragHandler>(
                    target,
                    swipeData,
                    ExecuteEvents.beginDragHandler);
                swipeData.position = new Vector2(320f, 400f);
                ExecuteEvents.Execute<IEndDragHandler>(
                    target,
                    swipeData,
                    ExecuteEvents.endDragHandler);
            }

            if (captureCardScroll)
            {
                ExecuteCardRowScrollForValidation();
            }

            if (captureCardFilter &&
                (GameObject.Find("CardFilter_All") == null ||
                 GameObject.Find("CardFilter_Attack") == null ||
                 GameObject.Find("CardFilter_Defense") == null ||
                 GameObject.Find("CardFilter_Charge") == null ||
                 GameObject.Find("CardFilter_Persistent") == null ||
                 GetSortedVisibleCards().Count == 0 ||
                 GetSortedVisibleCards().Any(
                     card =>
                         CardCatalog.GetCategory(card.Kind) !=
                         CardCategory.Attack)))
            {
                throw new InvalidOperationException(
                    "カードカテゴリーフィルターの表示状態が不正です。");
            }

            if (captureDragAttack || captureDropAttack)
            {
                ExecuteCardDragForValidation(
                    CardKind.Attack,
                    "Enemy_0",
                    captureDropAttack);
            }

            if (captureDropCharge)
            {
                ExecuteCardDragForValidation(
                    CardKind.Charge,
                    "CardDetails",
                    true);
            }

            if (captureEtherRefillAnimation)
            {
                yield return new WaitForSecondsRealtime(0.28f);
            }
            else if (captureEtherDrawAnimation)
            {
                yield return new WaitForSecondsRealtime(0.42f);
            }
            else if (captureEtherPaymentAnimation)
            {
                yield return new WaitForSecondsRealtime(0.22f);
            }
            else if (captureBarrageHitAnimation)
            {
                yield return new WaitForSecondsRealtime(1.12f);
            }

            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            if (captureFogMap &&
                (GameObject.Find("FogArea") == null ||
                 GameObject.Find("FogBorder") == null ||
                 !_session.IsNodeInFog(_session.CurrentNode) ||
                 _session.LastTravelFogDamage !=
                    _session.CurrentFogDamage))
            {
                throw new InvalidOperationException(
                    "霧マップの表示またはダメージ状態が不正です。");
            }

            if (captureEtherRefillAnimation &&
                (_battle.LastEtherRefillDrawIndex != 0 ||
                 _battle.LastRefilledEtherTypes.Count != 9 ||
                 _battle.LastSpentAfterDraw.Values.Sum() != 0 ||
                 GameObject.Find("EtherTransfer_Red_0") == null))
            {
                throw new InvalidOperationException(
                    "使用済みエーテル再利用アニメーションの状態が不正です。");
            }

            if (hiddenPersistentCardId >= 0 &&
                GameObject.Find($"Card_{hiddenPersistentCardId}") != null)
            {
                throw new InvalidOperationException(
                    "発動済み持続カードがカード列に残っています。");
            }

            ScreenCapture.CaptureScreenshot(arguments[markerIndex + 1]);
            yield return new WaitForSecondsRealtime(1f);
            Application.Quit();
        }

        private void EnsureCardCostForValidation(CardKind kind)
        {
            foreach (var cost in CardCatalog.GetCost(kind))
            {
                while (_battle.Pool.Current[cost.Key] < cost.Value)
                {
                    if (_battle.Pool.Unused[cost.Key] > 0)
                    {
                        _battle.Pool.Unused[cost.Key]--;
                        _battle.Pool.Current[cost.Key]++;
                        continue;
                    }

                    if (_battle.Pool.Spent[cost.Key] > 0)
                    {
                        _battle.Pool.Spent[cost.Key]--;
                        _battle.Pool.Current[cost.Key]++;
                        continue;
                    }

                    throw new InvalidOperationException(
                        $"検証用エーテルが不足しています：{cost.Key}");
                }
            }
        }

        private void ExecuteCardDragForValidation(
            CardKind kind,
            string targetName,
            bool endDrag)
        {
            var card = _session.Player.Deck.First(item => item.Kind == kind);
            var source = GameObject.Find($"Card_{card.Id}");
            var target = GameObject.Find(targetName);
            if (source == null || target == null)
            {
                throw new InvalidOperationException(
                    $"ドラッグ検証対象が見つかりません：{kind} -> {targetName}");
            }

            Canvas.ForceUpdateCanvases();
            var eventData = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = source.transform.position
            };
            ExecuteEvents.Execute<IBeginDragHandler>(
                source,
                eventData,
                ExecuteEvents.beginDragHandler);

            eventData.position = target.transform.position;
            eventData.pointerCurrentRaycast = new RaycastResult
            {
                gameObject = target
            };
            ExecuteEvents.Execute<IDragHandler>(
                source,
                eventData,
                ExecuteEvents.dragHandler);

            if (endDrag)
            {
                ExecuteEvents.Execute<IEndDragHandler>(
                    source,
                    eventData,
                ExecuteEvents.endDragHandler);
            }
        }

        private void ExecuteCardRowScrollForValidation()
        {
            var firstCard = _session.Player.Deck.First();
            var source = GameObject.Find($"Card_{firstCard.Id}");
            if (source == null)
            {
                throw new InvalidOperationException(
                    "横スクロール検証用カードが見つかりません。");
            }

            Canvas.ForceUpdateCanvases();
            var scroll = source.GetComponentInParent<ScrollRect>();
            if (scroll == null)
            {
                throw new InvalidOperationException(
                    "カード列のScrollRectが見つかりません。");
            }

            var eventData = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = source.transform.position
            };
            ExecuteEvents.Execute<IBeginDragHandler>(
                source,
                eventData,
                ExecuteEvents.beginDragHandler);
            eventData.position += Vector2.left * 900f;
            ExecuteEvents.Execute<IDragHandler>(
                source,
                eventData,
                ExecuteEvents.dragHandler);
            ExecuteEvents.Execute<IEndDragHandler>(
                source,
                eventData,
                ExecuteEvents.endDragHandler);
            Canvas.ForceUpdateCanvases();

            if (_dragGhost != null ||
                scroll.horizontalNormalizedPosition <= 0f)
            {
                throw new InvalidOperationException(
                    "カード列の横スクロールへ入力が渡されませんでした。" +
                    $" position={scroll.horizontalNormalizedPosition:F3}" +
                    $" content={scroll.content.rect.width:F1}" +
                    $" viewport={scroll.viewport.rect.width:F1}");
            }
        }

        private void AdvanceToLayerForValidation(int targetLayer)
        {
            if (targetLayer < 1 || targetLayer > RunSession.MaxLayer)
            {
                throw new InvalidOperationException(
                    $"検証対象の層が範囲外です：{targetLayer}");
            }

            while (_session.CurrentLayer < targetLayer)
            {
                CompleteCurrentLayerForValidation();
                _session.AdvanceToNextLayer();
            }

            _mapScrollX = 0.5f;
            _mapScrollY = 0f;
        }

        private void CompleteCurrentLayerForValidation()
        {
            while (_session.CurrentNode.Kind != StageKind.Boss)
            {
                var nextId = _session.CurrentNode.Neighbors
                    .Select(id => _session.Nodes.First(node => node.Id == id))
                    .Where(node => node.Y > _session.CurrentNode.Y)
                    .OrderBy(node => node.Y)
                    .ThenBy(node => node.Id)
                    .Select(node => node.Id)
                    .First();
                _session.TravelTo(nextId);
            }

            _session.CompleteCurrentBattle(1);
        }

        private void TravelToStageForValidation(StageKind targetKind)
        {
            var nodes = _session.Nodes.ToDictionary(node => node.Id);
            var targetId = nodes.Values
                .Where(node => node.Kind == targetKind)
                .OrderBy(node => node.Y)
                .ThenBy(node => node.Id)
                .First()
                .Id;
            var previous = new Dictionary<int, int>();
            var visited = new HashSet<int> { _session.CurrentNodeId };
            var pending = new Queue<int>();
            pending.Enqueue(_session.CurrentNodeId);
            while (pending.Count > 0 && !visited.Contains(targetId))
            {
                var currentId = pending.Dequeue();
                foreach (var neighborId in nodes[currentId].Neighbors)
                {
                    if (!visited.Add(neighborId))
                    {
                        continue;
                    }

                    previous[neighborId] = currentId;
                    pending.Enqueue(neighborId);
                }
            }

            if (!visited.Contains(targetId))
            {
                throw new InvalidOperationException(
                    $"{targetKind}への検証経路が見つかりません。");
            }

            var path = new List<int>();
            for (var currentId = targetId;
                 currentId != _session.CurrentNodeId;
                 currentId = previous[currentId])
            {
                path.Add(currentId);
            }

            path.Reverse();
            foreach (var nodeId in path)
            {
                _session.TravelTo(nodeId);
            }
        }
#endif
    }

    public static class LostPageBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateGame()
        {
            if (UnityEngine.Object.FindAnyObjectByType<LostPageGame>() != null)
            {
                return;
            }

            new GameObject("LostPageGame").AddComponent<LostPageGame>();
        }
    }
}
