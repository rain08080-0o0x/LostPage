using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LostPage.Editor
{
    public static class LostPageProjectTools
    {
        private const string MainScenePath = "Assets/LostPage/Scenes/Main.unity";
        private const string EtherTextureRoot =
            "Assets/LostPage/Resource/Texture";
        private const string TutorialTextureRoot =
            "Assets/LostPage/Resource/Tutorial";
        private const string AttackEffectTexturePath =
            "Assets/LostPage/Resource/Texture/bom.png";
        private const string AttackEffectTextureObjectName =
            "AttackEffectTexture";

        [MenuItem("Lost Page/Setup Project")]
        public static void SetupProject()
        {
            Directory.CreateDirectory("Assets/LostPage/Scenes");

            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            scene.name = "Main";
            CreateEtherTextureSet();
            CreateTutorialPageSet();
            CreateAttackEffectSet();
            EditorSceneManager.SaveScene(scene, MainScenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainScenePath, true)
            };

            PlayerSettings.companyName = "LostPagePrototype";
            PlayerSettings.productName = "Lost Page";
            PlayerSettings.bundleVersion = "1.2.3";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.SetApplicationIdentifier(
                UnityEditor.Build.NamedBuildTarget.Android,
                "com.lostpage.prototype");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.bundleVersionCode = 9;

            AssetDatabase.SaveAssets();
            Debug.Log("LOSTPAGE_SETUP_OK");
        }

        private static void CreateEtherTextureSet()
        {
            var red = ImportSprite($"{EtherTextureRoot}/Triangle.png");
            var blue = ImportSprite($"{EtherTextureRoot}/Hexagon.png");
            var yellow = ImportSprite($"{EtherTextureRoot}/Square.png");
            var purple = ImportSprite($"{EtherTextureRoot}/Rhombus.png");

            var textureObject = new GameObject("EtherTextureSet");
            CreateSpriteReference(textureObject.transform, "EtherTexture_Red", red);
            CreateSpriteReference(textureObject.transform, "EtherTexture_Blue", blue);
            CreateSpriteReference(textureObject.transform, "EtherTexture_Yellow", yellow);
            CreateSpriteReference(textureObject.transform, "EtherTexture_Purple", purple);
        }

        private static void CreateTutorialPageSet()
        {
            var pages = new[]
            {
                ImportSprite($"{TutorialTextureRoot}/Page01_Battle.png"),
                ImportSprite($"{TutorialTextureRoot}/Page02_Attack.png"),
                ImportSprite($"{TutorialTextureRoot}/Page03_Effects.png"),
                ImportSprite($"{TutorialTextureRoot}/Page04_Carry.png"),
                ImportSprite($"{TutorialTextureRoot}/Page05_Map.png")
            };

            var pageObject = new GameObject("TutorialPageSet");
            for (var index = 0; index < pages.Length; index++)
            {
                CreateSpriteReference(
                    pageObject.transform,
                    $"TutorialPage_{index + 1:00}",
                    pages[index]);
            }
        }

        private static void CreateAttackEffectSet()
        {
            var importer =
                AssetImporter.GetAtPath(AttackEffectTexturePath) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException(
                    $"攻撃エフェクト画像が見つかりません：{AttackEffectTexturePath}",
                    AttackEffectTexturePath);
            }

            if (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single ||
                !importer.alphaIsTransparency ||
                importer.mipmapEnabled ||
                importer.wrapMode != TextureWrapMode.Clamp ||
                importer.npotScale != TextureImporterNPOTScale.None ||
                importer.textureCompression !=
                    TextureImporterCompression.Uncompressed)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression =
                    TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            var sprite =
                AssetDatabase.LoadAssetAtPath<Sprite>(AttackEffectTexturePath);
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"攻撃エフェクト画像をSpriteとして読み込めません：{AttackEffectTexturePath}");
            }

            if (sprite.texture.width != 600 ||
                sprite.texture.height != 120 ||
                sprite.texture.width % 5 != 0)
            {
                throw new InvalidOperationException(
                    "攻撃エフェクト画像は600x120・横5フレームである必要があります。");
            }

            var effectObject = new GameObject("AttackEffectSet");
            CreateSpriteReference(
                effectObject.transform,
                AttackEffectTextureObjectName,
                sprite);
        }

        private static void CreateSpriteReference(
            Transform parent,
            string objectName,
            Sprite sprite)
        {
            var referenceObject = new GameObject(objectName);
            referenceObject.transform.SetParent(parent, false);
            var spriteRenderer = referenceObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.enabled = false;
        }

        private static Sprite ImportSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                throw new FileNotFoundException(
                    $"画像が見つかりません：{path}",
                    path);
            }

            if (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single ||
                importer.mipmapEnabled)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"画像をSpriteとして読み込めません：{path}");
            }

            return sprite;
        }

        [MenuItem("Lost Page/Validate Model")]
        public static void ValidateModel()
        {
            var attackEffectTexture =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    AttackEffectTexturePath);
            Require(
                attackEffectTexture != null &&
                attackEffectTexture.width == 600 &&
                attackEffectTexture.height == 120,
                "攻撃エフェクト画像の原寸読み込み");
            var attackEffectFrame = Sprite.Create(
                attackEffectTexture,
                new Rect(0f, 0f, 120f, 120f),
                new Vector2(0.5f, 0.5f),
                100f);
            Require(
                attackEffectFrame != null,
                "攻撃エフェクトの横5フレーム分割");
            UnityEngine.Object.DestroyImmediate(attackEffectFrame);

            var session = new RunSession(12345);
            Require(session.Player.MaxHp == 100, "プレイヤー最大HP");
            Require(session.Player.Hp == 100, "プレイヤー初期HP");
            Require(session.Player.Gold == 0, "初期ゴールド");
            Require(
                session.Player.Tools.Count == 0,
                "持ち込み道具の初期未選択");
            Require(session.Player.Deck.Count == 7, "初期デッキ枚数");
            Require(
                session.Player.Deck.Count(card => card.Kind == CardKind.Attack) == 3,
                "初期攻撃カード");
            Require(
                session.Player.Deck.Count(card => card.Kind == CardKind.Defense) == 3,
                "初期防御カード");
            Require(
                session.Player.Deck.Count(card => card.Kind == CardKind.Charge) == 1,
                "初期チャージカード");
            Require(
                session.Player.Deck.All(
                    card =>
                        card.Kind == CardKind.Attack ||
                        card.Kind == CardKind.Defense ||
                        card.Kind == CardKind.Charge),
                "追加カードは初期未所持");
            Require(
                CardCatalog.GetCooldown(CardKind.Attack) == 1 &&
                CardCatalog.GetCooldown(CardKind.Defense) == 1,
                "通常カードのクールタイム");
            Require(
                CardCatalog.GetCooldown(CardKind.HeavyAttack) == 2 &&
                CardCatalog.GetCooldown(CardKind.AreaAttack) == 2 &&
                CardCatalog.GetCooldown(CardKind.StrongDefense) == 2 &&
                CardCatalog.GetCooldown(CardKind.AutoDefense) == 2 &&
                CardCatalog.GetCooldown(CardKind.Charge) == 2,
                "追加カードのクールタイム2");
            Require(
                CardCatalog.GetCooldown(CardKind.Resonance) == 3 &&
                CardCatalog.GetCooldown(CardKind.Persistent) == 0,
                "共鳴と持続カードのクールタイム");
            Require(
                CardCatalog.GetCooldown(CardKind.GrowthAttack) == 2 &&
                CardCatalog.GetCooldown(CardKind.RandomBarrage) == 4 &&
                CardCatalog.GetCooldown(CardKind.RedPulseAttack) == 4 &&
                CardCatalog.GetCooldown(CardKind.PiercingAreaAttack) == 4,
                "拡張攻撃カードのクールタイム");
            Require(
                CardCatalog.GetCooldown(CardKind.GuardContinuance) == 3 &&
                CardCatalog.GetCooldown(CardKind.MirrorShield) == 4 &&
                CardCatalog.GetCooldown(CardKind.HealCharge) == 2 &&
                CardCatalog.GetCooldown(CardKind.EtherConversion) == 3 &&
                CardCatalog.GetCooldown(CardKind.GuardCyclePersistent) == 0,
                "拡張防御・チャージ・持続カードのクールタイム");

            var initialPool = new EtherPool(
                session.Player.Deck,
                new System.Random(1));
            Require(initialPool.GetTotal(EtherType.Red) == 9, "赤エーテル総数");
            Require(initialPool.GetTotal(EtherType.Blue) == 3, "青エーテル総数");
            Require(initialPool.GetTotal(EtherType.Yellow) == 2, "黄エーテル総数");
            Require(initialPool.GetTotal(EtherType.Purple) == 0, "紫エーテル総数");

            var sortedPlayer = new PlayerState();
            sortedPlayer.AddCard(CardKind.Persistent);
            sortedPlayer.AddCard(CardKind.AutoDefense);
            var firstAddedAttack = sortedPlayer.AddCard(CardKind.Attack);
            sortedPlayer.AddCard(CardKind.Resonance);
            var secondAddedAttack = sortedPlayer.AddCard(CardKind.Attack);
            sortedPlayer.AddCard(CardKind.AreaAttack);
            sortedPlayer.AddCard(CardKind.Defense);
            sortedPlayer.AddCard(CardKind.Charge);
            Require(
                sortedPlayer.Deck
                    .Zip(
                        sortedPlayer.Deck.Skip(1),
                        (left, right) =>
                            CardCatalog.GetCategory(left.Kind) <=
                            CardCatalog.GetCategory(right.Kind))
                    .All(isSorted => isSorted),
                "取得カードの種類別ソート");
            Require(
                sortedPlayer.Deck.IndexOf(firstAddedAttack) <
                sortedPlayer.Deck.IndexOf(secondAddedAttack),
                "同種カードの取得順維持");

            var attackPlayer = new PlayerState();
            attackPlayer.Deck.Clear();
            attackPlayer.AddCard(CardKind.Attack);
            attackPlayer.AddCard(CardKind.Attack);
            attackPlayer.AddCard(CardKind.Attack);
            attackPlayer.AddCard(CardKind.Attack);
            var target = new EnemyState(
                "検証用",
                30,
                1,
                0,
                EnemyActionKind.Attack);
            var attackBattle = new BattleModel(
                attackPlayer,
                new[] { target },
                new System.Random(2));
            Require(
                attackBattle.LastDrawnEtherTypes.Count ==
                    attackPlayer.GetEtherDrawCount() &&
                attackBattle.LastCurrentAfterDraw.Values.Sum() ==
                    attackBattle.LastDrawnEtherTypes.Count &&
                attackBattle.LastUnusedAfterDraw.Values.Sum() +
                    attackBattle.LastCurrentAfterDraw.Values.Sum() +
                    attackBattle.LastSpentAfterDraw.Values.Sum() ==
                    attackBattle.Pool.Unused.Values.Sum() +
                    attackBattle.Pool.Current.Values.Sum() +
                    attackBattle.Pool.Spent.Values.Sum(),
                "ターン開始時の配布エーテル列とプール記録");
            Require(
                attackBattle.CanUse(attackPlayer.Deck[0]),
                "攻撃カード使用条件");
            attackBattle.UseCard(attackPlayer.Deck[0], 0);
            Require(
                target.Hp == 25 &&
                attackBattle.LastAttackTargetIndices.SequenceEqual(
                    new[] { 0 }),
                "攻撃カードの5ダメージと演出対象");
            Require(
                attackBattle.LastAttackHits.Count == 1 &&
                attackBattle.LastAttackHits[0].TargetIndex == 0 &&
                attackBattle.LastAttackHits[0].HpBefore == 30 &&
                attackBattle.LastAttackHits[0].HpAfter == 25,
                "単体攻撃のヒット単位HP記録");
            Require(
                attackPlayer.Deck[0].CooldownRemaining == 1 &&
                !attackBattle.CanUse(attackPlayer.Deck[0]),
                "使用した攻撃カードは同じターンに再使用不可");
            Require(
                attackPlayer.Deck[1].CooldownRemaining == 0 &&
                attackBattle.CanUse(attackPlayer.Deck[1]),
                "同種カードのクールタイムはカードごとに管理");

            var heavyPlayer = new PlayerState();
            heavyPlayer.Deck.Clear();
            var heavyCard = heavyPlayer.AddCard(CardKind.HeavyAttack);
            var heavyTarget = new EnemyState(
                "強撃検証用",
                30,
                1,
                0,
                EnemyActionKind.Attack);
            var heavyBattle = new BattleModel(
                heavyPlayer,
                new[] { heavyTarget },
                new System.Random(3));
            heavyBattle.UseCard(heavyCard, 0);
            Require(heavyTarget.Hp == 13, "強撃の17ダメージ");
            Require(
                heavyCard.CooldownRemaining == 2 &&
                !heavyBattle.CanUse(heavyCard),
                "強撃使用直後のクールタイム");
            heavyBattle.EndPlayerTurn(
                heavyBattle.Pool.GetCurrentTokens()
                    .Take(Math.Min(2, heavyBattle.Pool.CurrentTotal))
                    .ToList());
            Require(
                heavyCard.CooldownRemaining == 1 &&
                !heavyBattle.CanUse(heavyCard),
                "強撃は次のターン使用不可");
            heavyBattle.EndPlayerTurn(
                heavyBattle.Pool.GetCurrentTokens()
                    .Take(Math.Min(2, heavyBattle.Pool.CurrentTotal))
                    .ToList());
            Require(
                heavyCard.CooldownRemaining == 0 &&
                heavyBattle.CanUse(heavyCard),
                "強撃は2回目の次ターンに再使用可能");

            var areaPlayer = new PlayerState();
            areaPlayer.Deck.Clear();
            var areaCard = areaPlayer.AddCard(CardKind.AreaAttack);
            var areaTargets = new[]
            {
                new EnemyState(
                    "全体検証A",
                    30,
                    1,
                    0,
                    EnemyActionKind.Attack),
                new EnemyState(
                    "全体検証B",
                    30,
                    1,
                    0,
                    EnemyActionKind.Attack)
            };
            var areaBattle = new BattleModel(
                areaPlayer,
                areaTargets,
                new System.Random(4));
            areaBattle.UseCard(areaCard, -1);
            Require(
                areaTargets.All(enemy => enemy.Hp == 21) &&
                areaBattle.LastAttackTargetIndices.SequenceEqual(
                    new[] { 0, 1 }),
                "薙ぎ払いの全体9ダメージと同時演出対象");
            Require(
                areaBattle.LastAttackHits.Count == 2 &&
                areaBattle.LastAttackHits.All(
                    hit => hit.HpBefore == 30 && hit.HpAfter == 21),
                "全体攻撃の対象別HP記録");

            var strongDefensePlayer = new PlayerState();
            strongDefensePlayer.Deck.Clear();
            var strongDefenseCard =
                strongDefensePlayer.AddCard(CardKind.StrongDefense);
            var strongDefenseBattle = new BattleModel(
                strongDefensePlayer,
                new[]
                {
                    new EnemyState(
                        "堅守検証用",
                        30,
                        1,
                        0,
                        EnemyActionKind.Attack)
                },
                new System.Random(5));
            strongDefenseBattle.UseCard(strongDefenseCard, 0);
            Require(
                strongDefensePlayer.Shield == 12,
                "堅守の12シールド");

            var resonancePlayer = new PlayerState();
            resonancePlayer.Deck.Clear();
            var resonanceAttack =
                resonancePlayer.AddCard(CardKind.Attack);
            var resonanceCard =
                resonancePlayer.AddCard(CardKind.Resonance);
            var resonanceTarget = new EnemyState(
                "共鳴検証用",
                30,
                1,
                0,
                EnemyActionKind.Attack);
            var resonanceBattle = new BattleModel(
                resonancePlayer,
                new[] { resonanceTarget },
                new System.Random(6));
            Require(
                resonanceBattle.TotalRedEtherCount == 4,
                "共鳴が参照する赤エーテル総数");
            resonanceBattle.UseCard(resonanceCard, 0);
            Require(
                resonanceBattle.PendingCharge == 4,
                "共鳴による赤エーテル総数分の強化");
            resonanceBattle.UseCard(resonanceAttack, 0);
            Require(
                resonanceTarget.Hp == 21,
                "共鳴を適用した攻撃");

            var autoDefensePlayer = new PlayerState();
            autoDefensePlayer.Deck.Clear();
            autoDefensePlayer.AddCard(CardKind.Attack);
            var autoDefenseCard =
                autoDefensePlayer.AddCard(CardKind.AutoDefense);
            var autoDefenseBattle = new BattleModel(
                autoDefensePlayer,
                new[]
                {
                    new EnemyState(
                        "自動防御検証用",
                        30,
                        1,
                        0,
                        EnemyActionKind.Attack)
                },
                new System.Random(7));
            autoDefenseBattle.UseCard(autoDefenseCard, 0);
            Require(
                autoDefenseBattle.AutoDefenseStacks == 3,
                "自動防御3層の獲得");
            var firstAutoDefenseTurn =
                autoDefenseBattle.EndPlayerTurn(
                autoDefenseBattle.Pool.GetCurrentTokens());
            Require(
                autoDefensePlayer.Hp == 100 &&
                autoDefensePlayer.Shield == 0 &&
                autoDefenseBattle.AutoDefenseStacks == 2,
                "自動防御が自ターン終了時に敵攻撃を防ぐ");
            var autoDefenseMessageIndex =
                firstAutoDefenseTurn.IndexOf(
                    "自動防御：",
                    StringComparison.Ordinal);
            var enemyActionMessageIndex =
                firstAutoDefenseTurn.IndexOf(
                    "自動防御検証用の攻撃",
                    StringComparison.Ordinal);
            Require(
                autoDefenseMessageIndex >= 0 &&
                enemyActionMessageIndex >= 0 &&
                autoDefenseMessageIndex < enemyActionMessageIndex,
                "自動防御は敵行動より先に発動");
            autoDefenseBattle.EndPlayerTurn(
                autoDefenseBattle.Pool.GetCurrentTokens()
                    .Take(2)
                    .ToList());
            Require(
                autoDefensePlayer.Hp == 100 &&
                autoDefensePlayer.Shield == 0 &&
                autoDefenseBattle.AutoDefenseStacks == 1,
                "自動防御はターン終了ごとに1層減少");

            var autoJudgmentPlayer = new PlayerState();
            autoJudgmentPlayer.Deck.Clear();
            autoJudgmentPlayer.AddTool(CarryToolKind.GuardianJudgment);
            var autoJudgmentCard =
                autoJudgmentPlayer.AddCard(CardKind.AutoDefense);
            var autoJudgmentTarget = new EnemyState(
                "自動防御裁き検証用",
                2,
                100,
                0,
                EnemyActionKind.Attack);
            var autoJudgmentBattle = new BattleModel(
                autoJudgmentPlayer,
                new[] { autoJudgmentTarget },
                new System.Random(71));
            SetCurrentEther(
                autoJudgmentBattle.Pool,
                (EtherType.Blue, 2));
            autoJudgmentBattle.UseCard(autoJudgmentCard, 0);
            autoJudgmentPlayer.Shield = 7;
            autoJudgmentBattle.EndPlayerTurn(
                autoJudgmentBattle.Pool.GetCurrentTokens());
            Require(
                autoJudgmentBattle.Phase == BattlePhase.Victory &&
                autoJudgmentPlayer.Hp == 100 &&
                !autoJudgmentTarget.IsAlive,
                "自動防御の守護者の裁きで勝利した場合は敵が行動しない");

            var persistentPlayer = new PlayerState();
            persistentPlayer.Deck.Clear();
            var persistentCard =
                persistentPlayer.AddCard(CardKind.Persistent);
            var persistentBattle = new BattleModel(
                persistentPlayer,
                new[]
                {
                    new EnemyState(
                        "持続検証用",
                        30,
                        1,
                        0,
                        EnemyActionKind.Attack)
                },
                new System.Random(8));
            persistentBattle.UseCard(persistentCard, 0);
            Require(
                persistentCard.PersistentActivated &&
                !persistentBattle.CanUse(persistentCard),
                "持続カードは発動後に再使用不可");

            var attackToolPlayer = new PlayerState();
            attackToolPlayer.Deck.Clear();
            attackToolPlayer.AddTool(CarryToolKind.AttackBoost);
            var attackToolCard =
                attackToolPlayer.AddCard(CardKind.Attack);
            var attackToolTarget = new EnemyState(
                "攻撃道具検証用",
                30,
                1,
                0,
                EnemyActionKind.Attack);
            var attackToolBattle = new BattleModel(
                attackToolPlayer,
                new[] { attackToolTarget },
                new System.Random(9));
            attackToolBattle.UseCard(attackToolCard, 0);
            Require(
                attackToolTarget.Hp == 24,
                "刃の欠片による攻撃ダメージ+1");

            var shieldToolPlayer = new PlayerState();
            shieldToolPlayer.Deck.Clear();
            shieldToolPlayer.AddTool(CarryToolKind.ShieldBoost);
            var shieldToolCard =
                shieldToolPlayer.AddCard(CardKind.Defense);
            var shieldToolBattle = new BattleModel(
                shieldToolPlayer,
                new[]
                {
                    new EnemyState(
                        "防御道具検証用",
                        30,
                        1,
                        0,
                        EnemyActionKind.Attack)
                },
                new System.Random(10));
            shieldToolBattle.UseCard(shieldToolCard, 0);
            Require(
                shieldToolPlayer.Shield == 9 &&
                shieldToolBattle.PreviewValue(CardKind.StrongDefense) == 14 &&
                shieldToolBattle.PreviewValue(CardKind.AutoDefense) == 3,
                "守護の札による直接シールド+2");

            var pierceToolPlayer = new PlayerState();
            pierceToolPlayer.Deck.Clear();
            pierceToolPlayer.AddTool(CarryToolKind.FirstAttackPierce);
            var firstPierceAttack =
                pierceToolPlayer.AddCard(CardKind.Attack);
            var secondPierceAttack =
                pierceToolPlayer.AddCard(CardKind.Attack);
            var pierceTarget = new EnemyState(
                "貫通道具検証用",
                30,
                1,
                0,
                EnemyActionKind.Attack)
            {
                Shield = 8
            };
            var pierceBattle = new BattleModel(
                pierceToolPlayer,
                new[] { pierceTarget },
                new System.Random(11));
            pierceBattle.UseCard(firstPierceAttack, 0);
            Require(
                pierceTarget.Hp == 25 &&
                pierceTarget.Shield == 8 &&
                !pierceBattle.FirstAttackPierceAvailable,
                "貫通の針による最初の攻撃");
            pierceBattle.UseCard(secondPierceAttack, 0);
            Require(
                pierceTarget.Hp == 25 &&
                pierceTarget.Shield == 3,
                "貫通の針は2回目の攻撃に適用されない");

            var growthPlayer = new PlayerState();
            growthPlayer.Deck.Clear();
            var growthCard =
                growthPlayer.AddCard(CardKind.GrowthAttack);
            var growthTarget = new EnemyState(
                "成長撃検証用",
                100,
                1,
                0,
                EnemyActionKind.Attack);
            var growthBattle = new BattleModel(
                growthPlayer,
                new[] { growthTarget },
                new System.Random(12));
            SetCurrentEther(
                growthBattle.Pool,
                (EtherType.Red, 3));
            growthBattle.UseCard(growthCard, 0);
            Require(
                growthTarget.Hp == 92 &&
                growthCard.GrowthBonus == 3,
                "成長撃の初回8ダメージと個体成長");
            growthCard.CooldownRemaining = 0;
            SetCurrentEther(
                growthBattle.Pool,
                (EtherType.Red, 3));
            growthBattle.UseCard(growthCard, 0);
            Require(
                growthTarget.Hp == 81 &&
                growthCard.GrowthBonus == 6,
                "成長撃は使用ごとに8・11・14と累積");
            new BattleModel(
                growthPlayer,
                new[]
                {
                    new EnemyState(
                        "成長リセット検証用",
                        30,
                        1,
                        0,
                        EnemyActionKind.Attack)
                },
                new System.Random(13));
            Require(growthCard.GrowthBonus == 0, "成長撃は戦闘ごとにリセット");

            var barragePlayer = new PlayerState();
            barragePlayer.Deck.Clear();
            var barrageCard =
                barragePlayer.AddCard(CardKind.RandomBarrage);
            var barrageTarget = new EnemyState(
                "乱撃検証用",
                100,
                1,
                0,
                EnemyActionKind.Attack);
            var barrageBattle = new BattleModel(
                barragePlayer,
                new[] { barrageTarget },
                new System.Random(14));
            SetCurrentEther(
                barrageBattle.Pool,
                (EtherType.Red, 4));
            barrageBattle.UseCard(barrageCard, -1);
            Require(
                barrageTarget.Hp == 60 &&
                barrageBattle.LastAttackTargetIndices.Count == 4 &&
                barrageBattle.LastAttackTargetIndices.All(
                    index => index == 0),
                "乱撃の10ダメージ4回と連続演出対象");
            Require(
                barrageBattle.LastAttackHits
                    .Select(hit => hit.HpBefore)
                    .SequenceEqual(new[] { 100, 90, 80, 70 }) &&
                barrageBattle.LastAttackHits
                    .Select(hit => hit.HpAfter)
                    .SequenceEqual(new[] { 90, 80, 70, 60 }),
                "乱撃のヒットごとのHP推移記録");

            var barragePiercePlayer = new PlayerState();
            barragePiercePlayer.Deck.Clear();
            barragePiercePlayer.AddTool(CarryToolKind.FirstAttackPierce);
            var barragePierceCard =
                barragePiercePlayer.AddCard(CardKind.RandomBarrage);
            var barragePierceTarget = new EnemyState(
                "乱撃貫通検証用",
                100,
                1,
                0,
                EnemyActionKind.Attack)
            {
                Shield = 50
            };
            var barragePierceBattle = new BattleModel(
                barragePiercePlayer,
                new[] { barragePierceTarget },
                new System.Random(141));
            SetCurrentEther(
                barragePierceBattle.Pool,
                (EtherType.Red, 4));
            barragePierceBattle.UseCard(barragePierceCard, -1);
            Require(
                barragePierceTarget.Hp == 90 &&
                barragePierceTarget.Shield == 20 &&
                barragePierceBattle.PenetrationStacks == 0,
                "乱撃は1撃ごとに貫通を1消費");

            var redPulsePlayer = new PlayerState();
            redPulsePlayer.Deck.Clear();
            var redPulseCard =
                redPulsePlayer.AddCard(CardKind.RedPulseAttack);
            var redPulseTarget = new EnemyState(
                "紅脈撃検証用",
                100,
                1,
                0,
                EnemyActionKind.Attack);
            var redPulseBattle = new BattleModel(
                redPulsePlayer,
                new[] { redPulseTarget },
                new System.Random(15));
            SetCurrentEther(
                redPulseBattle.Pool,
                (EtherType.Red, 3),
                (EtherType.Blue, 2));
            redPulseBattle.UseCard(redPulseCard, 0);
            Require(
                redPulseTarget.Hp == 87,
                "紅脈撃は全プールの赤3個+10ダメージ");

            var piercingAreaPlayer = new PlayerState();
            piercingAreaPlayer.Deck.Clear();
            piercingAreaPlayer.AddTool(CarryToolKind.FirstAttackPierce);
            var piercingAreaCard =
                piercingAreaPlayer.AddCard(CardKind.PiercingAreaAttack);
            var piercingAreaTarget = new EnemyState(
                "破界撃検証用",
                100,
                1,
                0,
                EnemyActionKind.Attack)
            {
                Shield = 50
            };
            var piercingAreaBattle = new BattleModel(
                piercingAreaPlayer,
                new[] { piercingAreaTarget },
                new System.Random(16));
            SetCurrentEther(
                piercingAreaBattle.Pool,
                (EtherType.Red, 5));
            piercingAreaBattle.UseCard(piercingAreaCard, -1);
            Require(
                piercingAreaTarget.Hp == 64 &&
                piercingAreaTarget.Shield == 50 &&
                piercingAreaBattle.PenetrationStacks == 1,
                "破界撃は固有貫通で貫通バフを消費しない");

            var expandedDefensePlayer = new PlayerState();
            expandedDefensePlayer.Deck.Clear();
            expandedDefensePlayer.AddTool(CarryToolKind.BlueCrystal);
            var guardContinuanceCard =
                expandedDefensePlayer.AddCard(CardKind.GuardContinuance);
            var mirrorShieldCard =
                expandedDefensePlayer.AddCard(CardKind.MirrorShield);
            var expandedDefenseBattle = new BattleModel(
                expandedDefensePlayer,
                new[]
                {
                    new EnemyState(
                        "拡張防御検証用",
                        100,
                        1,
                        0,
                        EnemyActionKind.Attack)
                },
                new System.Random(17));
            SetCurrentEther(
                expandedDefenseBattle.Pool,
                (EtherType.Blue, 7));
            expandedDefenseBattle.UseCard(
                guardContinuanceCard,
                0);
            Require(
                expandedDefenseBattle.AutoDefenseStacks == 2 &&
                expandedDefenseBattle.DefenseRetentionStacks == 2 &&
                expandedDefenseBattle.DefensePowerBonus == 1,
                "守勢継続と青結晶");
            expandedDefensePlayer.Shield = 10;
            expandedDefenseBattle.UseCard(mirrorShieldCard, 0);
            Require(
                expandedDefensePlayer.Shield == 20 &&
                expandedDefenseBattle.DefensePowerBonus == 2,
                "鏡盾は防御力を加算せずシールドを倍化");
            expandedDefenseBattle.EndPlayerTurn(
                expandedDefenseBattle.Pool.GetCurrentTokens());
            Require(
                expandedDefensePlayer.Shield == 23 &&
                expandedDefenseBattle.DefenseRetentionStacks == 3 &&
                expandedDefenseBattle.AutoDefenseStacks == 1,
                "防御維持と防御力付き自動防御");

            var healPlayer = new PlayerState();
            healPlayer.Deck.Clear();
            var healCard = healPlayer.AddCard(CardKind.HealCharge);
            healPlayer.Hp = 50;
            var healBattle = new BattleModel(
                healPlayer,
                new[]
                {
                    new EnemyState(
                        "治癒検証用",
                        30,
                        1,
                        0,
                        EnemyActionKind.Attack)
                },
                new System.Random(18));
            SetCurrentEther(
                healBattle.Pool,
                (EtherType.Yellow, 3));
            healBattle.UseCard(healCard, 0);
            Require(healPlayer.Hp == 56, "治癒のHP6回復");

            var conversionPlayer = new PlayerState();
            conversionPlayer.Deck.Clear();
            var conversionCard =
                conversionPlayer.AddCard(CardKind.EtherConversion);
            var conversionBattle = new BattleModel(
                conversionPlayer,
                new[]
                {
                    new EnemyState(
                        "転換検証用",
                        30,
                        1,
                        0,
                        EnemyActionKind.Attack)
                },
                new System.Random(19));
            SetCurrentEther(
                conversionBattle.Pool,
                (EtherType.Red, 2),
                (EtherType.Blue, 1),
                (EtherType.Yellow, 3));
            conversionBattle.UseCard(conversionCard, 0);
            Require(
                conversionBattle.Pool.CurrentTotal == 5 &&
                conversionBattle.Pool.Current.Count(pair => pair.Value > 0) == 1,
                "転換は支払い後の手番エーテルを1色へ統一して2個追加");

            var guardCyclePlayer = new PlayerState();
            guardCyclePlayer.Deck.Clear();
            var guardCycleCard =
                guardCyclePlayer.AddCard(CardKind.GuardCyclePersistent);
            var guardCycleUses = new[]
            {
                guardCyclePlayer.AddCard(CardKind.HealCharge),
                guardCyclePlayer.AddCard(CardKind.HealCharge),
                guardCyclePlayer.AddCard(CardKind.HealCharge)
            };
            var guardCycleBattle = new BattleModel(
                guardCyclePlayer,
                new[]
                {
                    new EnemyState(
                        "守護循環検証用",
                        30,
                        1,
                        0,
                        EnemyActionKind.Attack)
                },
                new System.Random(20));
            SetCurrentEther(
                guardCycleBattle.Pool,
                (EtherType.Blue, 1),
                (EtherType.Purple, 2),
                (EtherType.Yellow, 9));
            guardCycleBattle.UseCard(guardCycleCard, 0);
            Require(
                guardCycleBattle.CardsTowardAutoDefense == 0,
                "守護循環自身は3枚カウントに含めない");
            foreach (var card in guardCycleUses)
            {
                guardCycleBattle.UseCard(card, 0);
            }

            Require(
                guardCycleBattle.AutoDefenseStacks == 2 &&
                guardCycleBattle.CardsTowardAutoDefense == 0,
                "守護循環は発動後3枚ごとに自動防御+2");

            var redCrystalPlayer = new PlayerState();
            redCrystalPlayer.Deck.Clear();
            redCrystalPlayer.AddTool(CarryToolKind.RedCrystal);
            var redCrystalCard =
                redCrystalPlayer.AddCard(CardKind.GrowthAttack);
            var redCrystalBattle = new BattleModel(
                redCrystalPlayer,
                new[]
                {
                    new EnemyState(
                        "赤結晶検証用",
                        30,
                        1,
                        0,
                        EnemyActionKind.Attack)
                },
                new System.Random(21));
            SetCurrentEther(
                redCrystalBattle.Pool,
                (EtherType.Red, 3));
            redCrystalBattle.UseCard(redCrystalCard, 0);
            Require(
                redCrystalBattle.PermanentAttackBonus == 1,
                "赤3個以上のカード使用後に攻撃力+1");

            var bagPlayer = new PlayerState();
            bagPlayer.AddTool(CarryToolKind.SmallBag);
            bagPlayer.AddTool(CarryToolKind.BigBag);
            bagPlayer.AddTool(CarryToolKind.BigBag);
            Require(
                bagPlayer.GetCarryLimit() == 7 &&
                !bagPlayer.CanAddTool(CarryToolKind.BigBag),
                "鞄の加算と大きな鞄2個上限");
            bagPlayer.AddTool(CarryToolKind.AttackBoost);
            Require(
                !bagPlayer.CanAddTool(CarryToolKind.AttackBoost),
                "大きな鞄以外の道具は1個上限");
            var bagBattle = new BattleModel(
                bagPlayer,
                new[]
                {
                    new EnemyState(
                        "鞄検証用",
                        30,
                        1,
                        0,
                        EnemyActionKind.Attack)
                },
                new System.Random(28));
            var bagCarry = bagBattle.Pool.GetCurrentTokens();
            bagBattle.EndPlayerTurn(bagCarry);
            Require(
                bagBattle.Pool.CurrentTotal == 10,
                "持ち越し上限7では手番5個をすべて持ち越す");

            var starPlayer = new PlayerState();
            starPlayer.Deck.Clear();
            starPlayer.AddTool(CarryToolKind.StarFragment);
            starPlayer.AddTool(CarryToolKind.StarMass);
            starPlayer.AddTool(CarryToolKind.StarOrb);
            starPlayer.AddTool(CarryToolKind.EnergyCore);
            for (var index = 0; index < 4; index++)
            {
                starPlayer.AddCard(CardKind.Attack);
            }

            var starBattle = new BattleModel(
                starPlayer,
                new[]
                {
                    new EnemyState(
                        "星道具検証用",
                        30,
                        1,
                        0,
                        EnemyActionKind.Attack)
                },
                new System.Random(22));
            Require(
                starBattle.PendingCharge == 50 &&
                starBattle.Pool.CurrentTotal == 6 &&
                starPlayer.GetToolCount(CarryToolKind.Myojo) == 1 &&
                !starPlayer.HasTool(CarryToolKind.StarFragment) &&
                !starPlayer.HasTool(CarryToolKind.StarMass) &&
                !starPlayer.HasTool(CarryToolKind.StarOrb),
                "星3種は明星へ変換され、明星とエネルギーコアが発動");

            var beltPlayer = new PlayerState();
            beltPlayer.Deck.Clear();
            beltPlayer.AddTool(CarryToolKind.LargeBelt);
            var beltPersistent =
                beltPlayer.AddCard(CardKind.GuardCyclePersistent);
            var beltBattle = new BattleModel(
                beltPlayer,
                new[]
                {
                    new EnemyState(
                        "大型ベルト検証用",
                        30,
                        1,
                        0,
                        EnemyActionKind.Attack)
                },
                new System.Random(23));
            Require(
                beltPersistent.PersistentActivated &&
                beltBattle.GuardCycleStacks == 1,
                "大型ベルトによる持続カード無料発動");

            var judgmentPlayer = new PlayerState();
            judgmentPlayer.Deck.Clear();
            judgmentPlayer.AddTool(CarryToolKind.GuardianJudgment);
            var judgmentCard =
                judgmentPlayer.AddCard(CardKind.Defense);
            var judgmentTarget = new EnemyState(
                "守護者の裁き検証用",
                30,
                1,
                0,
                EnemyActionKind.Attack);
            var judgmentBattle = new BattleModel(
                judgmentPlayer,
                new[] { judgmentTarget },
                new System.Random(24));
            SetCurrentEther(
                judgmentBattle.Pool,
                (EtherType.Red, 1),
                (EtherType.Blue, 1));
            judgmentBattle.UseCard(judgmentCard, 0);
            Require(
                judgmentPlayer.Shield == 7 &&
                judgmentTarget.Hp == 28,
                "守護者の裁きは総シールド20%を切り上げて与える");

            var daggerPlayer = new PlayerState();
            daggerPlayer.Deck.Clear();
            daggerPlayer.AddTool(CarryToolKind.AttackBoost);
            daggerPlayer.AddTool(CarryToolKind.VorpalDagger);
            var daggerCard = daggerPlayer.AddCard(CardKind.Attack);
            var daggerTarget = new EnemyState(
                "ヴォーパルダガー検証用",
                30,
                1,
                0,
                EnemyActionKind.Attack);
            var daggerBattle = new BattleModel(
                daggerPlayer,
                new[] { daggerTarget },
                new System.Random(25));
            SetCurrentEther(
                daggerBattle.Pool,
                (EtherType.Red, 2));
            daggerBattle.UseCard(daggerCard, 0);
            Require(
                daggerTarget.Hp == 23,
                "ヴォーパルダガーは攻撃力を総コスト2倍で適用");

            var storagePlayer = new PlayerState();
            storagePlayer.Deck.Clear();
            storagePlayer.AddTool(CarryToolKind.ShieldStorage);
            var storageCard = storagePlayer.AddCard(CardKind.Defense);
            var storageBattle = new BattleModel(
                storagePlayer,
                new[]
                {
                    new EnemyState(
                        "シールド貯蔵庫検証用",
                        30,
                        1,
                        0,
                        EnemyActionKind.Attack)
                },
                new System.Random(26));
            SetCurrentEther(
                storageBattle.Pool,
                (EtherType.Red, 1),
                (EtherType.Blue, 1));
            storageBattle.UseCard(storageCard, 0);
            storageBattle.EndPlayerTurn(
                storageBattle.Pool.GetCurrentTokens());
            Require(
                storagePlayer.Shield == 3,
                "シールド貯蔵庫は敵攻撃後の残量を半分維持");

            var heartPlayer = new PlayerState();
            heartPlayer.Deck.Clear();
            heartPlayer.AddTool(CarryToolKind.VorpalHeart);
            var heartCharge = heartPlayer.AddCard(CardKind.Charge);
            var heartAttack = heartPlayer.AddCard(CardKind.Attack);
            var heartTarget = new EnemyState(
                "ヴォーパルハート検証用",
                30,
                1,
                0,
                EnemyActionKind.Attack);
            var heartBattle = new BattleModel(
                heartPlayer,
                new[] { heartTarget },
                new System.Random(27));
            SetCurrentEther(
                heartBattle.Pool,
                (EtherType.Red, 2),
                (EtherType.Yellow, 2));
            heartBattle.UseCard(heartCharge, 0);
            heartBattle.UseCard(heartAttack, 0);
            Require(
                heartTarget.Hp == 22 &&
                heartBattle.PendingCharge == 2,
                "ヴォーパルハートは全チャージを適用し、半分を切り上げて残す");

            var currentTokens = attackBattle.Pool.GetCurrentTokens();
            Require(currentTokens.Count == 3, "攻撃後の手番エーテル");
            attackBattle.EndPlayerTurn(
                new List<EtherType>
                {
                    currentTokens[0],
                    currentTokens[1]
                });
            Require(
                attackBattle.Pool.CurrentTotal == 7 &&
                attackBattle.LastEtherRefillDrawIndex == 3 &&
                attackBattle.LastRefilledEtherTypes.Count == 3 &&
                attackBattle.LastRefilledEtherTypes.All(
                    type => type == EtherType.Red) &&
                attackBattle.LastUnusedAfterDraw[EtherType.Red] == 1 &&
                attackBattle.LastSpentAfterDraw.Values.Sum() == 0,
                "持ち越し2個・新規取得5個・使用済みエーテル再利用記録");
            Require(
                attackPlayer.Deck[0].CooldownRemaining == 0 &&
                attackBattle.CanUse(attackPlayer.Deck[0]),
                "通常攻撃は次のターンに再使用可能");

            var choices = session.CreateCardRewardChoices();
            Require(choices.Count == 3, "カード報酬の選択肢数");
            Require(choices.Distinct().Count() == 3, "カード報酬は重複なし");
            Require(
                Enum.GetValues(typeof(CardKind)).Length == 55 &&
                CardCatalog.AllKinds.Count() == 55,
                "報酬・ショップのカード候補数");
            var rewardCandidates = new HashSet<CardKind>();
            var rewardCandidateCount = CardCatalog.AllKinds.Count(
                kind =>
                    CardCatalog.GetRarity(kind) == CardRarity.Normal ||
                    CardCatalog.GetRarity(kind) == CardRarity.Rare);
            for (var seed = 1;
                 seed <= 1000 &&
                 rewardCandidates.Count < rewardCandidateCount;
                 seed++)
            {
                foreach (var kind in new RunSession(seed).CreateCardRewardChoices())
                {
                    rewardCandidates.Add(kind);
                }
            }

            Require(
                CardCatalog.AllKinds
                    .Where(
                        kind =>
                            CardCatalog.GetRarity(kind) ==
                            CardRarity.Normal ||
                            CardCatalog.GetRarity(kind) ==
                            CardRarity.Rare)
                    .All(rewardCandidates.Contains),
                "通常・レアカードを含む報酬抽選");

            Require(
                Enum.GetValues(typeof(CarryToolKind)).Length == 18,
                "持ち込み道具の総数");
            var toolSession = new RunSession(31415);
            var startingToolChoices =
                toolSession.CreateStartingToolChoices();
            Require(
                startingToolChoices.Count == 3 &&
                startingToolChoices.Distinct().Count() == 3 &&
                startingToolChoices.All(
                    kind =>
                        CarryToolCatalog.GetRarity(kind) ==
                        CarryToolRarity.Normal),
                "開始時は通常道具の重複なし3択");
            var bossToolChoices = toolSession.CreateBossToolChoices();
            Require(
                bossToolChoices.Count == 3 &&
                bossToolChoices.Distinct().Count() == 3 &&
                bossToolChoices.All(
                    kind =>
                        CarryToolCatalog.GetRarity(kind) ==
                        CarryToolRarity.Boss),
                "ボス道具の重複なし3択");
            toolSession.ClaimTool(
                CarryToolKind.BigBag,
                CarryToolRarity.Boss);
            toolSession.ClaimTool(
                CarryToolKind.BigBag,
                CarryToolRarity.Boss);
            Require(
                toolSession.Player.GetToolCount(CarryToolKind.BigBag) == 2 &&
                !toolSession.CreateBossToolChoices()
                    .Contains(CarryToolKind.BigBag),
                "大きな鞄は2個取得後にボス候補から除外");
            toolSession.ClaimTool(
                CarryToolKind.EnergyCore,
                CarryToolRarity.Boss);
            Require(
                !toolSession.Player.CanAddTool(CarryToolKind.EnergyCore),
                "大きな鞄以外のボス道具は1個上限");

            var toolStageSession = new RunSession(27182);
            MoveToStage(toolStageSession, StageKind.Tool);
            var toolStageChoices =
                toolStageSession.CreateToolStageChoices();
            Require(
                toolStageChoices.Count == 3 &&
                toolStageChoices.Distinct().Count() == 3 &&
                toolStageChoices.All(
                    kind =>
                        CarryToolCatalog.GetRarity(kind) ==
                        CarryToolRarity.Normal),
                "道具マスは通常道具の重複なし3択");
            var claimedTool = toolStageChoices[0];
            toolStageSession.ClaimCurrentToolStageReward(claimedTool);
            Require(
                toolStageSession.Player.GetToolCount(claimedTool) == 1 &&
                toolStageSession.CurrentNode.Cleared,
                "道具マスの道具取得と完了状態");

            var rareRewardFound = false;
            for (var seed = 1;
                 seed <= 500 && !rareRewardFound;
                 seed++)
            {
                var rareSession = new RunSession(seed);
                MoveToStage(rareSession, StageKind.Reward);
                var reward = rareSession.ClaimCurrentReward();
                if (!reward.RareTool.HasValue)
                {
                    continue;
                }

                rareRewardFound = true;
                Require(
                    CarryToolCatalog.GetRarity(reward.RareTool.Value) ==
                        CarryToolRarity.Rare &&
                    rareSession.Player.GetToolCount(
                        reward.RareTool.Value) == 1,
                    "報酬ステージのレア道具取得");
            }

            Require(rareRewardFound, "報酬ステージの30%レア抽選");

            var eventOutcomes = new HashSet<int>();
            for (var seed = 1;
                  seed <= 200 && eventOutcomes.Count < 5;
                  seed++)
            {
                var eventSession = new RunSession(seed);
                eventSession.Player.Hp = 50;
                MoveToStage(eventSession, StageKind.RandomEvent);
                var eventKind =
                    eventSession.GetCurrentRandomEventKind();
                if (eventKind == RandomEventKind.FreeUpgrade)
                {
                    var card =
                        eventSession.GetUpgradeableCards().First();
                    Require(
                        eventSession.TryUpgradeCardForFreeEvent(card.Id),
                        "無料強化イベントのカード選択");
                    eventSession.FinishFreeUpgradeEvent();
                    eventOutcomes.Add(3);
                }
                else if (eventKind == RandomEventKind.BloodUpgrade)
                {
                    var card =
                        eventSession.GetUpgradeableCards().First();
                    Require(
                        eventSession.StartBloodUpgradeEvent(1) &&
                        eventSession.TryUpgradeCardForBloodEvent(card.Id) &&
                        eventSession.Player.Hp == 44,
                        "HP消費強化イベント");
                    eventOutcomes.Add(4);
                }
                else
                {
                    eventSession.ResolveCurrentRandomEvent();
                    eventOutcomes.Add((int)eventKind);
                }

                Require(
                    eventSession.CurrentNode.Cleared,
                    "ランダムイベントの完了状態");
            }

            Require(eventOutcomes.Count == 5, "ランダムイベント5種類");

            var eventBagSession = new RunSession(24601);
            var eventBagOutcomes = new List<RandomEventKind>();
            while (eventBagOutcomes.Count < 5)
            {
                var eventNodeIds = eventBagSession.Nodes
                    .Where(
                        node =>
                            node.Kind == StageKind.RandomEvent &&
                            !node.Cleared)
                    .OrderBy(node => node.Id)
                    .Select(node => node.Id)
                    .ToList();
                foreach (var eventNodeId in eventNodeIds)
                {
                    MoveToNode(eventBagSession, eventNodeId);
                    var eventKind =
                        eventBagSession.GetCurrentRandomEventKind();
                    eventBagOutcomes.Add(eventKind);
                    if (eventKind == RandomEventKind.FreeUpgrade)
                    {
                        var card =
                            eventBagSession.GetUpgradeableCards().First();
                        Require(
                            eventBagSession.TryUpgradeCardForFreeEvent(
                                card.Id),
                            "抽選袋の無料強化イベント");
                        eventBagSession.FinishFreeUpgradeEvent();
                    }
                    else if (eventKind == RandomEventKind.BloodUpgrade)
                    {
                        var card =
                            eventBagSession.GetUpgradeableCards().First();
                        Require(
                            eventBagSession.StartBloodUpgradeEvent(1) &&
                            eventBagSession.TryUpgradeCardForBloodEvent(
                                card.Id),
                            "抽選袋のHP消費強化イベント");
                    }
                    else
                    {
                        eventBagSession.ResolveCurrentRandomEvent();
                    }

                    if (eventBagOutcomes.Count >= 5)
                    {
                        break;
                    }
                }

                if (eventBagOutcomes.Count >= 5)
                {
                    break;
                }

                MoveToBoss(eventBagSession);
                eventBagSession.CompleteCurrentBattle(1);
                eventBagSession.AdvanceToNextLayer();
            }

            Require(
                eventBagOutcomes.Distinct().Count() == 5,
                "ランダムイベント抽選袋の5種類一巡");
            Require(
                !eventBagOutcomes
                    .Zip(
                        eventBagOutcomes.Skip(1),
                        (current, next) =>
                            IsUpgradeRandomEvent(current) &&
                            IsUpgradeRandomEvent(next))
                    .Any(adjacent => adjacent),
                "強化系ランダムイベントの連続防止");

            var stageRewardSession = new RunSession(101);
            MoveToStage(stageRewardSession, StageKind.Reward);
            var deckCountBeforeReward =
                stageRewardSession.Player.Deck.Count;
            var stageRewards = stageRewardSession.ClaimCurrentReward();
            Require(
                stageRewards.Cards.Count == 2 &&
                stageRewards.Cards.Distinct().Count() == 2 &&
                stageRewardSession.Player.Deck.Count ==
                deckCountBeforeReward + 2,
                "報酬マスの重複なしカード2枚");
            Require(
                stageRewardSession.Player.Deck
                    .Zip(
                        stageRewardSession.Player.Deck.Skip(1),
                        (left, right) =>
                            CardCatalog.GetCategory(left.Kind) <=
                            CardCatalog.GetCategory(right.Kind))
                    .All(isSorted => isSorted),
                "報酬マス取得後のカードソート");

            var shopSession = new RunSession(99);
            MoveToStage(shopSession, StageKind.Shop);
            var shopNodeId = shopSession.CurrentNodeId;
            shopSession.CurrentNode.Cleared = true;
            var adjacentNodeId = shopSession.CurrentNode.Neighbors[0];
            shopSession.TravelTo(adjacentNodeId);
            Require(
                !shopSession.TravelTo(shopNodeId),
                "訪問済みショップへの再訪");
            shopSession.Player.Gold = 100;
            Require(
                shopSession.ShopCardPrice ==
                    RunSession.InitialShopCardPrice &&
                shopSession.TryBuyCard(CardKind.HeavyAttack) &&
                shopSession.ShopCardPrice == 25 &&
                shopSession.Player.Gold == 80 &&
                shopSession.Player.Deck.Any(
                    card => card.Kind == CardKind.HeavyAttack),
                "ショップ初回購入と5G値上げ");
            Require(
                shopSession.TryBuyCard(CardKind.StrongDefense) &&
                shopSession.ShopCardPrice == 30 &&
                shopSession.Player.Gold == 55,
                "ショップ購入ごとの累積値上げ");
            shopSession.Player.Gold = 29;
            Require(
                !shopSession.TryBuyCard(CardKind.Charge) &&
                shopSession.ShopCardPrice == 30 &&
                shopSession.Player.Gold == 29,
                "購入失敗時は価格を据え置く");

            Require(session.CanTravelTo(1), "最初のマップ列への移動");
            Require(!session.CanTravelTo(2), "未接続ノードへの移動禁止");
            var firstLayerBossId = session.Nodes
                .Single(node => node.Kind == StageKind.Boss)
                .Id;
            Require(
                !session.CanTravelTo(firstLayerBossId),
                "未接続ボスへの移動禁止");

            var horizontalTravelSession = new RunSession(54320);
            horizontalTravelSession.TravelTo(1);
            horizontalTravelSession.TravelTo(2);
            var movesBeforeHorizontalTravel =
                horizontalTravelSession.MapMoveCount;
            var fogBeforeHorizontalTravel =
                horizontalTravelSession.FogDepth;
            Require(
                horizontalTravelSession.Nodes.Single(node => node.Id == 2)
                    .Depth ==
                horizontalTravelSession.Nodes.Single(node => node.Id == 3)
                    .Depth &&
                horizontalTravelSession.CanTravelTo(3),
                "同じ行の右隣マスへの横移動");
            horizontalTravelSession.TravelTo(3);
            Require(
                horizontalTravelSession.CurrentNodeId == 3 &&
                horizontalTravelSession.CanTravelTo(2) &&
                horizontalTravelSession.MapMoveCount ==
                    movesBeforeHorizontalTravel + 1 &&
                horizontalTravelSession.FogDepth ==
                    fogBeforeHorizontalTravel + 1,
                "横移動の双方向接続と霧進行");

            var fogSession = new RunSession(54321);
            var fogStart = fogSession.CurrentNode;
            var fogFirstRow = fogSession.Nodes.Single(node => node.Id == 1);
            Require(
                fogSession.FogDepth == RunSession.InitialFogDepth &&
                fogSession.MapMoveCount == 0 &&
                fogSession.CurrentFogDamage == 5 &&
                fogStart.Depth == 0 &&
                fogFirstRow.Depth == 1,
                "霧の初期深度・移動回数・マス深度");
            var invalidFogTravelRejected = false;
            try
            {
                fogSession.TravelTo(
                    fogSession.Nodes.Single(
                        node => node.Kind == StageKind.Boss).Id);
            }
            catch (InvalidOperationException)
            {
                invalidFogTravelRejected = true;
            }

            Require(
                invalidFogTravelRejected &&
                fogSession.FogDepth == RunSession.InitialFogDepth &&
                fogSession.MapMoveCount == 0,
                "不正な移動では霧が進行しない");
            var directFogSession = new RunSession(54322);
            MoveToBoss(directFogSession);
            Require(
                directFogSession.CurrentNode.Kind == StageKind.Boss &&
                directFogSession.Player.Hp == 100 &&
                directFogSession.LastTravelFogDamage == 0,
                "最短経路で前進した場合は霧ダメージなし");
            fogSession.Player.Shield = 23;
            for (var move = 0; move < 5; move++)
            {
                fogSession.TravelTo(
                    fogSession.CurrentNodeId == 0 ? 1 : 0);
            }

            Require(
                fogSession.MapMoveCount == 5 &&
                fogSession.FogDepth == 0 &&
                fogSession.Player.Hp == 100 &&
                fogSession.Player.Shield == 23 &&
                fogSession.IsNodeInFog(fogStart) &&
                !fogSession.IsNodeInFog(fogFirstRow),
                "5移動で霧が開始地点へ到達");
            fogSession.TravelTo(0);
            Require(
                fogSession.MapMoveCount == 6 &&
                fogSession.FogDepth == 1 &&
                fogSession.LastTravelFogDamage == 5 &&
                fogSession.Player.Hp == 95 &&
                fogSession.Player.Shield == 23,
                "6移動目の開始地点再進入・シールド無視霧ダメージ");
            fogSession.Player.Hp = 5;
            fogSession.TravelTo(1);
            Require(
                fogSession.Player.Hp == 0 &&
                fogSession.LastTravelFogDamage == 5,
                "霧ダメージによるHP0");
            var cappedFogSession = new RunSession(54323);
            for (var move = 0; move < 20; move++)
            {
                cappedFogSession.TravelTo(
                    cappedFogSession.CurrentNodeId == 0 ? 1 : 0);
            }

            var cappedFogBoss = cappedFogSession.Nodes.Single(
                node => node.Kind == StageKind.Boss);
            Require(
                cappedFogSession.FogDepth ==
                    cappedFogSession.MaximumFogDepth &&
                cappedFogSession.MaximumFogDepth ==
                    cappedFogBoss.Depth - 1 &&
                !cappedFogSession.IsNodeInFog(cappedFogBoss),
                "霧はボス手前で停止しボスを覆わない");
            var fogDepthAtCap = cappedFogSession.FogDepth;
            cappedFogSession.TravelTo(
                cappedFogSession.CurrentNodeId == 0 ? 1 : 0);
            Require(
                cappedFogSession.FogDepth == fogDepthAtCap,
                "上限到達後の移動で霧が進行しない");

            var layeredSession = new RunSession(24680);
            layeredSession.Player.Hp = 73;
            layeredSession.Player.Gold = 25;
            layeredSession.Player.AddCard(CardKind.HeavyAttack);
            var carriedDeckCount = layeredSession.Player.Deck.Count;
            var bossHpByLayer = new[] { 120, 240, 480, 800, 1200 };
            var normalHpPercentByLayer =
                new[] { 100, 150, 250, 400, 700 };
            var minimumEnemyCountByLayer =
                new[] { 1, 1, 1, 2, 2 };
            var maximumEnemyCountByLayer =
                new[] { 2, 2, 3, 3, 3 };
            var minimumRandomEventsByLayer =
                new[] { 2, 4, 7, 11, 15 };
            var maximumRandomEventsByLayer =
                new[] { 3, 5, 9, 13, 17 };
            var fogDamageByLayer = new[] { 5, 7, 9, 11, 13 };
            for (var layer = 1; layer <= RunSession.MaxLayer; layer++)
            {
                var rowCount = layer + 3;
                var intermediateNodeCount =
                    rowCount * (rowCount + 1) / 2;
                var randomEventCount = layeredSession.Nodes.Count(
                    node => node.Kind == StageKind.RandomEvent);
                var battleNodeCount = layeredSession.Nodes.Count(
                    node => node.Kind == StageKind.Battle);
                Require(
                    layeredSession.CurrentLayer == layer,
                    $"{layer}層の層番号");
                Require(
                    layeredSession.CurrentFogDamage ==
                        fogDamageByLayer[layer - 1] &&
                    layeredSession.Nodes.Single(
                        node => node.Kind == StageKind.Start).Depth == 0 &&
                    layeredSession.Nodes.Single(
                        node => node.Kind == StageKind.Boss).Depth ==
                        rowCount + 1 &&
                    layeredSession.MaximumFogDepth == rowCount,
                    $"{layer}層の霧ダメージ・開始・ボス深度");
                Require(
                    layeredSession.Nodes.Count ==
                    intermediateNodeCount + 2,
                    $"{layer}層のステージ数");
                Require(
                    HasExpandingMapRows(layeredSession, rowCount),
                    $"{layer}層の1列ずつ広がるマップ構造");
                Require(
                    layeredSession.Nodes.Count(
                        node => node.Kind == StageKind.Fountain) == 1 &&
                    layeredSession.Nodes.Count(
                        node => node.Kind == StageKind.Shop) == 1 &&
                    layeredSession.Nodes.Count(
                        node => node.Kind == StageKind.Reward) == 1 &&
                    layeredSession.Nodes.Count(
                        node => node.Kind == StageKind.Tool) == 1 &&
                    layeredSession.Nodes.Count(
                        node => node.Kind == StageKind.Boss) == 1,
                    $"{layer}層の固定特殊ステージ構成");
                Require(
                    randomEventCount >=
                        minimumRandomEventsByLayer[layer - 1] &&
                    randomEventCount <=
                        maximumRandomEventsByLayer[layer - 1],
                    $"{layer}層のランダムイベント数");
                Require(
                    randomEventCount + 4 > battleNodeCount,
                    $"{layer}層はイベント系マスが戦闘マスより多い");
                Require(
                    IsCurrentMapConnected(layeredSession),
                    $"{layer}層のマップ接続");

                MoveToStage(layeredSession, StageKind.Battle);
                var scaledBattle = layeredSession.CreateBattleForCurrentNode();
                Require(
                    scaledBattle.Enemies.Count >=
                        minimumEnemyCountByLayer[layer - 1] &&
                    scaledBattle.Enemies.Count <=
                        maximumEnemyCountByLayer[layer - 1] &&
                    scaledBattle.Enemies
                        .Select(enemy => enemy.Name)
                        .Distinct()
                        .Count() == scaledBattle.Enemies.Count,
                    $"{layer}層の通常敵1～3体・重複なし編成");
                Require(
                    scaledBattle.Enemies.All(
                        enemy =>
                            enemy.MaxHp ==
                            Math.Min(
                                400,
                                (GetBaseEnemyHp(enemy.Name) *
                                 normalHpPercentByLayer[layer - 1] +
                                 99) /
                                100) &&
                            enemy.MaxHp <= 400 &&
                            enemy.Attack ==
                            GetBaseEnemyAttack(enemy.Name) +
                            (layer - 1) / 2),
                    $"{layer}層の通常敵HP補正・400上限・攻撃補正");
                var goldBeforeRegularBattle =
                    layeredSession.Player.Gold;
                var regularReward =
                    layeredSession.CompleteCurrentBattle(
                        scaledBattle.Enemies.Count);
                Require(
                    regularReward >= scaledBattle.Enemies.Count * 10 &&
                    regularReward <= scaledBattle.Enemies.Count * 15 &&
                    layeredSession.Player.Gold ==
                        goldBeforeRegularBattle + regularReward,
                    $"{layer}層の敵1体ごとの10～15G報酬");

                MoveToBoss(layeredSession);
                var bossBattle = layeredSession.CreateBattleForCurrentNode();
                Require(
                    bossBattle.Enemies.Count == 1 &&
                    bossBattle.Enemies[0].MaxHp ==
                        bossHpByLayer[layer - 1],
                    $"{layer}層のボスHP");
                var hpBeforeBossClear =
                    Math.Max(1, layeredSession.Player.MaxHp - 17);
                layeredSession.Player.Hp = hpBeforeBossClear;
                var goldBeforeBoss = layeredSession.Player.Gold;
                var bossReward =
                    layeredSession.CompleteCurrentBattle(
                        bossBattle.Enemies.Count);
                Require(
                    bossReward >= 110 &&
                    bossReward <= 165 &&
                    layeredSession.LastBattleGoldReward == bossReward &&
                    layeredSession.Player.Gold ==
                        goldBeforeBoss + bossReward,
                    $"{layer}層のボス追加100～150G報酬");
                var expectedMaxHp = 100 + layer * 10;
                Require(
                    layeredSession.Player.MaxHp == expectedMaxHp &&
                    layeredSession.Player.Hp ==
                    (layer < RunSession.MaxLayer
                        ? expectedMaxHp
                        : hpBeforeBossClear),
                    layer < RunSession.MaxLayer
                        ? $"{layer}層クリア時の最大HP増加と全回復"
                        : "5層クリア時の最大HP増加と回復なし");

                if (layer < RunSession.MaxLayer)
                {
                    layeredSession.AdvanceToNextLayer();
                    Require(
                        layeredSession.CurrentNodeId == 0 &&
                        layeredSession.CurrentNode.Kind == StageKind.Start &&
                        layeredSession.MapMoveCount == 0 &&
                        layeredSession.FogDepth ==
                            RunSession.InitialFogDepth &&
                        layeredSession.LastTravelFogDamage == 0,
                        $"{layer + 1}層の開始地点");
                    Require(
                        layeredSession.Player.Hp == expectedMaxHp &&
                        layeredSession.Player.MaxHp == expectedMaxHp &&
                        layeredSession.Player.Deck.Count == carriedDeckCount,
                        $"{layer + 1}層へのプレイヤー状態引き継ぎ");
                }
                else
                {
                    Require(
                        !layeredSession.HasNextLayer,
                        "5層でラン完了");
                }
            }

            ValidateCardExpansion();
            Debug.Log("LOSTPAGE_VALIDATION_OK");
        }

        private static void ValidateCardExpansion()
        {
            Require(
                CardCatalog.AllKinds.Count() == 55 &&
                CardCatalog.AllKinds.All(
                    kind =>
                        !string.IsNullOrWhiteSpace(
                            CardCatalog.GetName(kind)) &&
                        !string.IsNullOrWhiteSpace(
                            CardCatalog.GetShortDescription(kind))),
                "追加カードを含む全55種の定義");
            var expectedRare = new[]
            {
                CardKind.GrowthAttack,
                CardKind.RandomBarrage,
                CardKind.PiercingAreaAttack,
                CardKind.MirrorShield,
                CardKind.Resonance,
                CardKind.EtherConversion,
                CardKind.GuardCyclePersistent
            };
            Require(
                expectedRare.All(
                    kind =>
                        CardCatalog.GetRarity(kind) == CardRarity.Rare) &&
                CardCatalog.GetRarity(CardKind.RedPulseAttack) ==
                    CardRarity.Boss &&
                CardCatalog.GetRarity(CardKind.DivineStrike) ==
                    CardRarity.Special,
                "既存カードのレア度設定");

            var upgradedAttack = new CardInstance(9000, CardKind.Attack)
            {
                IsUpgraded = true
            };
            Require(
                CardCatalog.GetCost(upgradedAttack)[EtherType.Red] == 1 &&
                CardCatalog.GetCooldown(upgradedAttack) == 1 &&
                CardCatalog.GetShortDescription(upgradedAttack)
                    .Contains("10ダメージ"),
                "カード個体の強化後定義");

            var persistentOwner = new PlayerState();
            persistentOwner.Deck.Clear();
            persistentOwner.AddCard(CardKind.AdditionalDefense);
            Require(
                !persistentOwner.CanAddCard(CardKind.AdditionalDefense) &&
                persistentOwner.CanAddCard(CardKind.DefenseSupport),
                "同種持続カードの複数所持禁止");

            var specialOwner = new PlayerState();
            specialOwner.AddTool(CarryToolKind.SmallBag);
            specialOwner.AddTool(CarryToolKind.BigBag);
            specialOwner.AddTool(CarryToolKind.BigBag);
            specialOwner.AddTool(CarryToolKind.EnergyCore);
            Require(
                specialOwner.Deck.Count(
                    card => card.Kind == CardKind.DivineStrike) == 1,
                "鞄とエネルギーコアによる神撃の自動入手");
            for (var set = 0; set < 2; set++)
            {
                specialOwner.AddTool(CarryToolKind.StarFragment);
                specialOwner.AddTool(CarryToolKind.StarMass);
                specialOwner.AddTool(CarryToolKind.StarOrb);
            }

            Require(
                specialOwner.GetToolCount(CarryToolKind.Myojo) == 2 &&
                specialOwner.CanAddTool(CarryToolKind.StarFragment),
                "明星の再入手と星道具の再取得");
            specialOwner.Deck.RemoveAll(
                card => card.Kind != CardKind.DivineStrike);
            var myojoBattle = new BattleModel(
                specialOwner,
                new[]
                {
                    new EnemyState(
                        "明星検証用",
                        100,
                        0,
                        0,
                        EnemyActionKind.Attack)
                },
                new System.Random(600));
            Require(
                myojoBattle.PendingCharge == 100,
                "明星2個で自ターン開始時チャージ+100");

            var flamePlayer = new PlayerState();
            flamePlayer.Deck.Clear();
            var heatstroke =
                flamePlayer.AddCard(CardKind.Heatstroke);
            var flameEnemy = new EnemyState(
                "炎検証用",
                100,
                0,
                0,
                EnemyActionKind.Attack);
            var flameBattle = new BattleModel(
                flamePlayer,
                new[] { flameEnemy },
                new System.Random(601));
            SetCurrentEther(
                flameBattle.Pool,
                (EtherType.Red, 2),
                (EtherType.Yellow, 1));
            flameBattle.UseCard(heatstroke, 0);
            Require(
                flameEnemy.Hp == 90 &&
                flameEnemy.Fire == 0 &&
                flameBattle.LastAttackHits.Count == 4,
                "熱射病は炎4を4・3・2・1で連続発動");

            var fireTurnPlayer = new PlayerState();
            fireTurnPlayer.Deck.Clear();
            var fireTurnEnemy = new EnemyState(
                "炎行動順検証用",
                3,
                20,
                0,
                EnemyActionKind.Attack)
            {
                Fire = 4
            };
            var fireTurnBattle = new BattleModel(
                fireTurnPlayer,
                new[] { fireTurnEnemy },
                new System.Random(602));
            fireTurnBattle.EndPlayerTurn(Array.Empty<EtherType>());
            Require(
                !fireTurnEnemy.IsAlive &&
                fireTurnBattle.Player.Hp == 100 &&
                fireTurnBattle.Phase == BattlePhase.Victory,
                "炎で敵が行動直前に倒れた場合は行動しない");

            var bleedPlayer = new PlayerState();
            bleedPlayer.Deck.Clear();
            var surging =
                bleedPlayer.AddCard(CardKind.SurgingFlame);
            var bleedEnemy = new EnemyState(
                "出血検証用",
                100,
                0,
                0,
                EnemyActionKind.Attack)
            {
                Shield = 100
            };
            var bleedBattle = new BattleModel(
                bleedPlayer,
                new[] { bleedEnemy },
                new System.Random(603));
            bleedEnemy.Bleed = 3;
            SetCurrentEther(
                bleedBattle.Pool,
                (EtherType.Red, 2));
            bleedBattle.UseCard(surging, 0);
            Require(
                bleedEnemy.Hp == 100 &&
                bleedEnemy.Shield == 84 &&
                bleedBattle.LastAttackHits.Count == 2,
                "出血はシールドで防がれた複数ヒットでもヒットごとに発動" +
                $"（HP={bleedEnemy.Hp}, 盾={bleedEnemy.Shield}, " +
                $"hit={bleedBattle.LastAttackHits.Count}）");

            var defeatedTargetPlayer = new PlayerState();
            defeatedTargetPlayer.Deck.Clear();
            var defeatedTargetSurging =
                defeatedTargetPlayer.AddCard(CardKind.SurgingFlame);
            var defeatedTarget = new EnemyState(
                "途中撃破検証用",
                5,
                0,
                0,
                EnemyActionKind.Attack);
            var survivingTarget = new EnemyState(
                "生存検証用",
                20,
                0,
                0,
                EnemyActionKind.Attack);
            var defeatedTargetBattle = new BattleModel(
                defeatedTargetPlayer,
                new[] { defeatedTarget, survivingTarget },
                new System.Random(611));
            SetCurrentEther(
                defeatedTargetBattle.Pool,
                (EtherType.Red, 2));
            defeatedTargetBattle.UseCard(defeatedTargetSurging, 0);
            Require(
                !defeatedTarget.IsAlive &&
                survivingTarget.Hp == 20 &&
                defeatedTargetBattle.LastAttackHits.Count == 1,
                "複数ヒット途中で対象を倒しても残り処理で例外にならない");

            var crimsonPlayer = new PlayerState();
            crimsonPlayer.Deck.Clear();
            var crimson =
                crimsonPlayer.AddCard(CardKind.CrimsonMoon);
            var explosion =
                crimsonPlayer.AddCard(CardKind.ExplosionFlame);
            var crimsonEnemy = new EnemyState(
                "赤い紅い月検証用",
                50,
                0,
                0,
                EnemyActionKind.Attack)
            {
                Fire = 3
            };
            var crimsonBattle = new BattleModel(
                crimsonPlayer,
                new[] { crimsonEnemy },
                new System.Random(612));
            SetCurrentEther(
                crimsonBattle.Pool,
                (EtherType.Red, 5),
                (EtherType.Purple, 1));
            crimsonBattle.UseCard(crimson, 0);
            crimsonBattle.UseCard(explosion, 0);
            Require(
                crimsonEnemy.Fire == 6 &&
                crimsonEnemy.Bleed == 2,
                "赤い紅い月は直接ダメージのない攻撃カードの対象にも出血付与");

            var reflectionPlayer = new PlayerState();
            reflectionPlayer.Deck.Clear();
            var spiked =
                reflectionPlayer.AddCard(CardKind.SpikedShield);
            var reflectionEnemy = new EnemyState(
                "反射検証用",
                50,
                1,
                0,
                EnemyActionKind.Attack);
            var reflectionBattle = new BattleModel(
                reflectionPlayer,
                new[] { reflectionEnemy },
                new System.Random(604));
            SetCurrentEther(
                reflectionBattle.Pool,
                (EtherType.Red, 1),
                (EtherType.Blue, 2));
            reflectionBattle.UseCard(spiked, 0);
            reflectionEnemy.Shield = 1;
            reflectionBattle.EndPlayerTurn(Array.Empty<EtherType>());
            Require(
                reflectionEnemy.Hp == 49 &&
                reflectionBattle.ReflectionStacks == 1,
                "反射は攻撃を防いでも発動し、敵シールドで防御され、" +
                "次の自ターン開始時に1減少");

            var duplicatePlayer = new PlayerState();
            duplicatePlayer.Deck.Clear();
            var duplicate =
                duplicatePlayer.AddCard(CardKind.DuplicateAttack);
            var duplicatedAttack =
                duplicatePlayer.AddCard(CardKind.Attack);
            var duplicateEnemy = new EnemyState(
                "複製検証用",
                100,
                0,
                0,
                EnemyActionKind.Attack);
            var duplicateBattle = new BattleModel(
                duplicatePlayer,
                new[] { duplicateEnemy },
                new System.Random(605));
            SetCurrentEther(
                duplicateBattle.Pool,
                (EtherType.Red, 3),
                (EtherType.Yellow, 2));
            duplicateBattle.UseCard(duplicate, 0);
            duplicateBattle.UseCard(duplicatedAttack, 0);
            Require(
                duplicateEnemy.Hp == 90 &&
                duplicateBattle.LastAttackHits.Count == 2,
                "複製攻撃は次の攻撃効果を合計2回実行");

            var additionalDefensePlayer = new PlayerState();
            additionalDefensePlayer.Deck.Clear();
            var additionalDefense =
                additionalDefensePlayer.AddCard(
                    CardKind.AdditionalDefense);
            var autoDefense =
                additionalDefensePlayer.AddCard(CardKind.AutoDefense);
            additionalDefensePlayer.AddTool(
                CarryToolKind.GuardianJudgment);
            var additionalDefenseEnemy = new EnemyState(
                "追加防御検証用",
                100,
                0,
                0,
                EnemyActionKind.Attack);
            var additionalDefenseBattle = new BattleModel(
                additionalDefensePlayer,
                new[] { additionalDefenseEnemy },
                new System.Random(606));
            SetCurrentEther(
                additionalDefenseBattle.Pool,
                (EtherType.Blue, 4),
                (EtherType.Purple, 1));
            additionalDefenseBattle.UseCard(additionalDefense, 0);
            additionalDefenseBattle.UseCard(autoDefense, 0);
            Require(
                additionalDefenseBattle.AutoDefenseStacks == 3 &&
                additionalDefensePlayer.Shield == 3 &&
                additionalDefenseEnemy.Hp == 99,
                "追加防御は自動防御を減らさず同量のシールドを獲得し、" +
                "守護者の裁きは20%を切り上げる");

            var divinePlayer = new PlayerState();
            divinePlayer.Deck.Clear();
            var divine =
                divinePlayer.AddCard(CardKind.DivineStrike);
            var divineEnemy = new EnemyState(
                "神撃検証用",
                100,
                0,
                0,
                EnemyActionKind.Attack);
            var divineBattle = new BattleModel(
                divinePlayer,
                new[] { divineEnemy },
                new System.Random(607));
            SetCurrentEther(
                divineBattle.Pool,
                (EtherType.Red, 2),
                (EtherType.Yellow, 2),
                (EtherType.Purple, 1));
            Require(
                divineBattle.GetDivineStrikeModes().SequenceEqual(
                    new[] { EtherType.Red, EtherType.Yellow }),
                "神撃の最多色同数選択");
            divineBattle.UseCard(divine, 0, EtherType.Red);
            Require(
                divineEnemy.Hp == 64 &&
                divineBattle.Pool.CurrentTotal == 0 &&
                divineBattle.Pool.Spent.Values.Sum() == 5 &&
                divine.CooldownRemaining == 8,
                "神撃・赤の式、複数回攻撃、手番全消費");

            var upgradedPoolPlayer = new PlayerState();
            upgradedPoolPlayer.Deck.Clear();
            var upgradedPoolAttack =
                upgradedPoolPlayer.AddCard(CardKind.Attack);
            upgradedPoolAttack.IsUpgraded = true;
            var upgradedPool = new EtherPool(
                upgradedPoolPlayer.Deck,
                new System.Random(608));
            Require(
                upgradedPool.GetTotal(EtherType.Red) == 1,
                "強化後コストを戦闘エーテル構成へ反映");

            var shopSession = new RunSession(609);
            MoveToStage(shopSession, StageKind.Shop);
            var firstStock =
                shopSession.GetCurrentShopStock(CardCategory.Attack);
            var repeatedStock =
                shopSession.GetCurrentShopStock(CardCategory.Attack);
            Require(
                firstStock.Count(
                    stock =>
                        CardCatalog.GetRarity(stock.Kind) ==
                        CardRarity.Normal) == 3 &&
                firstStock.Count(
                    stock =>
                        CardCatalog.GetRarity(stock.Kind) ==
                        CardRarity.Rare) == 2 &&
                firstStock.Select(stock => stock.Kind)
                    .SequenceEqual(
                        repeatedStock.Select(stock => stock.Kind)),
                "ショップのカテゴリ別通常3枚・レア2枚と固定在庫");
            shopSession.Player.Gold = 200;
            var shopAttack =
                shopSession.Player.Deck.First(
                    card => card.Kind == CardKind.Attack);
            var secondShopAttack =
                shopSession.Player.Deck.Last(
                    card => card.Kind == CardKind.Attack);
            var shopDefense =
                shopSession.Player.Deck.First(
                    card => card.Kind == CardKind.Defense);
            Require(
                shopSession.TryUpgradeCardAtCurrentShop(shopAttack.Id) &&
                !shopSession.TryUpgradeCardAtCurrentShop(
                    secondShopAttack.Id) &&
                shopSession.TryUpgradeCardAtCurrentShop(shopDefense.Id) &&
                shopSession.Player.Gold == 140,
                "ショップごとに各カテゴリ1回・1層30Gの個体強化");

            var bossChoices =
                new RunSession(610).CreateBossCardRewardChoices();
            Require(
                bossChoices.Count == 3 &&
                bossChoices.All(
                    kind =>
                        CardCatalog.GetRarity(kind) ==
                        CardRarity.Boss),
                "ボスカード報酬3択");
        }

        private static bool IsCurrentMapConnected(RunSession session)
        {
            var nodes = session.Nodes.ToDictionary(node => node.Id);
            var visited = new HashSet<int> { 0 };
            var pending = new Queue<int>();
            pending.Enqueue(0);
            while (pending.Count > 0)
            {
                var node = nodes[pending.Dequeue()];
                foreach (var neighborId in node.Neighbors)
                {
                    if (nodes.ContainsKey(neighborId) &&
                        visited.Add(neighborId))
                    {
                        pending.Enqueue(neighborId);
                    }
                }
            }

            return visited.Count == nodes.Count;
        }

        private static bool HasExpandingMapRows(
            RunSession session,
            int expectedRowCount)
        {
            var start = session.Nodes.Single(
                node => node.Kind == StageKind.Start);
            var boss = session.Nodes.Single(
                node => node.Kind == StageKind.Boss);
            var rows = session.Nodes
                .Where(
                    node =>
                        node.Kind != StageKind.Start &&
                        node.Kind != StageKind.Boss)
                .GroupBy(node => node.Y)
                .OrderBy(group => group.Key)
                .Select(group => group.OrderBy(node => node.X).ToList())
                .ToList();
            if (rows.Count != expectedRowCount ||
                start.Neighbors.Length != 1 ||
                !start.Neighbors.Contains(rows[0][0].Id) ||
                boss.Neighbors.Length != expectedRowCount)
            {
                return false;
            }

            for (var rowIndex = 0;
                 rowIndex < rows.Count;
                 rowIndex++)
            {
                var row = rows[rowIndex];
                if (row.Count != rowIndex + 1)
                {
                    return false;
                }

                for (var column = 0; column < row.Count; column++)
                {
                    if ((column > 0 &&
                         !row[column].Neighbors.Contains(
                             row[column - 1].Id)) ||
                        (column < row.Count - 1 &&
                         !row[column].Neighbors.Contains(
                             row[column + 1].Id)))
                    {
                        return false;
                    }
                }

                if (rowIndex == rows.Count - 1)
                {
                    if (row.Any(node => !node.Neighbors.Contains(boss.Id)))
                    {
                        return false;
                    }

                    continue;
                }

                var nextRow = rows[rowIndex + 1];
                for (var column = 0; column < row.Count; column++)
                {
                    if (!row[column].Neighbors.Contains(nextRow[column].Id) ||
                        !row[column].Neighbors.Contains(
                            nextRow[column + 1].Id))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static int GetBaseEnemyHp(string enemyName)
        {
            switch (enemyName)
            {
                case "紙喰らい":
                case "墨の影A":
                case "墨の影B":
                    return 30;
                case "失稿の番人":
                    return 50;
                case "綴じ糸の獣":
                    return 45;
                case "破れた騎士":
                    return 55;
                default:
                    throw new InvalidOperationException(
                        $"未知の通常敵です：{enemyName}");
            }
        }

        private static int GetBaseEnemyAttack(string enemyName)
        {
            switch (enemyName)
            {
                case "紙喰らい":
                    return 7;
                case "墨の影A":
                case "墨の影B":
                    return 6;
                case "失稿の番人":
                case "破れた騎士":
                    return 9;
                case "綴じ糸の獣":
                    return 8;
                default:
                    throw new InvalidOperationException(
                        $"未知の通常敵です：{enemyName}");
            }
        }

        private static void SetCurrentEther(
            EtherPool pool,
            params (EtherType Type, int Count)[] values)
        {
            foreach (EtherType type in Enum.GetValues(typeof(EtherType)))
            {
                pool.Unused[type] = 0;
                pool.Current[type] = 0;
                pool.Spent[type] = 0;
            }

            foreach (var value in values)
            {
                pool.Current[value.Type] = value.Count;
            }
        }

        private static void MoveToBoss(RunSession session)
        {
            while (session.CurrentNode.Kind != StageKind.Boss)
            {
                var nextId = session.CurrentNode.Neighbors
                    .Select(id => session.Nodes.First(node => node.Id == id))
                    .Where(node => node.Y > session.CurrentNode.Y)
                    .OrderBy(node => node.Y)
                    .ThenBy(node => node.Id)
                    .Select(node => node.Id)
                    .First();
                session.TravelTo(nextId);
            }
        }

        private static void MoveToStage(
            RunSession session,
            StageKind targetKind)
        {
            var nodes = session.Nodes.ToDictionary(node => node.Id);
            var targetId = nodes.Values
                .Where(node => node.Kind == targetKind)
                .OrderBy(node => node.Y)
                .ThenBy(node => node.Id)
                .First()
                .Id;
            MoveToNode(session, targetId);
        }

        private static void MoveToNode(RunSession session, int targetId)
        {
            var nodes = session.Nodes.ToDictionary(node => node.Id);
            var previous = new Dictionary<int, int>();
            var visited = new HashSet<int> { session.CurrentNodeId };
            var pending = new Queue<int>();
            pending.Enqueue(session.CurrentNodeId);
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
                    $"ノード{targetId}への経路が見つかりません。");
            }

            var path = new List<int>();
            for (var currentId = targetId;
                 currentId != session.CurrentNodeId;
                 currentId = previous[currentId])
            {
                path.Add(currentId);
            }

            path.Reverse();
            foreach (var nodeId in path)
            {
                session.TravelTo(nodeId);
            }
        }

        private static bool IsUpgradeRandomEvent(RandomEventKind kind)
        {
            return kind == RandomEventKind.FreeUpgrade ||
                   kind == RandomEventKind.BloodUpgrade;
        }

        [MenuItem("Lost Page/Build Windows")]
        public static void BuildWindows()
        {
            SetupProject();
            EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.Standalone,
                BuildTarget.StandaloneWindows64);
            Directory.CreateDirectory("Builds/Windows");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { MainScenePath },
                locationPathName = "Builds/Windows/LostPage.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            EnsureBuildSucceeded(BuildPipeline.BuildPlayer(options), "Windows");
        }

        public static void BuildWindowsValidation()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.Standalone,
                BuildTarget.StandaloneWindows64);
            Directory.CreateDirectory("Builds/FogValidation");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { MainScenePath },
                locationPathName = "Builds/FogValidation/LostPage.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };
            EnsureBuildSucceeded(
                BuildPipeline.BuildPlayer(options),
                "WindowsValidation");
        }

        [MenuItem("Lost Page/Build Android")]
        public static void BuildAndroid()
        {
            SetupProject();
            EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.Android,
                BuildTarget.Android);
            EnsureMainSceneHasNoMissingScripts();
            EditorUserBuildSettings.buildAppBundle = false;
            Directory.CreateDirectory("Builds/Android");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { MainScenePath },
                locationPathName = "Builds/Android/LostPage.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            EnsureBuildSucceeded(BuildPipeline.BuildPlayer(options), "Android");
        }

        private static void EnsureMainSceneHasNoMissingScripts()
        {
            var scene = EditorSceneManager.OpenScene(
                MainScenePath,
                OpenSceneMode.Single);
            var missingScriptObjects = scene
                .GetRootGameObjects()
                .SelectMany(
                    root =>
                        root.GetComponentsInChildren<Transform>(true))
                .Select(transform => transform.gameObject)
                .Where(
                    gameObject =>
                        GameObjectUtility
                            .GetMonoBehavioursWithMissingScriptCount(
                                gameObject) > 0)
                .Select(gameObject => gameObject.name)
                .ToList();
            if (missingScriptObjects.Count > 0)
            {
                throw new InvalidOperationException(
                    "Mainシーンに参照切れスクリプトがあります：" +
                    string.Join(", ", missingScriptObjects));
            }
        }

        private static void Require(bool condition, string label)
        {
            if (!condition)
            {
                throw new InvalidOperationException($"検証失敗：{label}");
            }
        }

        private static void EnsureBuildSucceeded(BuildReport report, string target)
        {
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"{target}ビルド失敗：" +
                    $"{report.summary.totalErrors} errors, " +
                    $"{report.summary.totalWarnings} warnings");
            }

            Debug.Log(
                $"LOSTPAGE_{target.ToUpperInvariant()}_BUILD_OK " +
                $"{report.summary.outputPath}");
        }
    }
}
