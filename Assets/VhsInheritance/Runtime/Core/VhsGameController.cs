using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

namespace VhsInheritance
{
    public sealed class VhsGameController : MonoBehaviour
    {
        private enum InteractionKind
        {
            None,
            Pile,
            Shelf,
            Counter,
            UpgradeStand,
            MysteryPerson,
            Television,
            Door
        }

        private enum AssistantState
        {
            Idle,
            ToShelf,
            ToCounter
        }

        private sealed class ShelfRuntime
        {
            public CassetteGenre genre;
            public GameObject root;
            public Transform interactionPoint;
            public Transform slotRoot;
            public SpriteRenderer renderer;
            public TextMeshPro label;
            public Rect obstacle;
            public BoxCollider2D footprint;
        }

        private sealed class InteractionTarget
        {
            public InteractionKind kind;
            public Transform point;
            public ShelfRuntime shelf;
        }

        private sealed class UpgradeRowUI
        {
            public UnityEngine.UI.Image icon;
            public TextMeshProUGUI title;
            public TextMeshProUGUI level;
            public TextMeshProUGUI effect;
            public UnityEngine.UI.Button buyButton;
            public TextMeshProUGUI buyLabel;
        }

        private static readonly Color UiInk = new Color32(48, 39, 53, 255);
        private static readonly Color UiCream = new Color32(48, 39, 53, 255);
        private static readonly Color UiTeal = new Color32(88, 74, 108, 255);
        private static readonly Color UiPink = new Color32(203, 130, 127, 255);
        private static readonly Color UiGold = new Color32(113, 75, 82, 255);
        private static readonly Color UiPurple = new Color32(153, 124, 177, 255);

        [SerializeField] private VhsGameConfig config;

        [Header("Runtime Debug (read only)")]
        [SerializeField] private GamePhase debugPhase;
        [SerializeField] private int debugPileCount;
        [SerializeField] private int debugMachineOverflow;
        [SerializeField] private int debugQueueCount;

        private readonly List<ShelfRuntime> shelves = new List<ShelfRuntime>();
        private readonly List<InteractionTarget> interactions = new List<InteractionTarget>();
        private readonly List<Rect> navigationObstacles = new List<Rect>();
        private readonly List<GameObject> visitorVisuals = new List<GameObject>();
        private readonly Dictionary<UpgradeType, UpgradeRowUI> upgradeRows = new Dictionary<UpgradeType, UpgradeRowUI>();
        private readonly Queue<KeyValuePair<string, string>> tutorialQueue = new Queue<KeyValuePair<string, string>>();
        private readonly Rect walkableBounds = new Rect(-9f, -4.8f, 18f, 8.05f);

        private VhsSaveData save;
        private GamePhase phase = GamePhase.MainMenu;
        private Camera gameCamera;
        private Light2D globalLight;
        private Transform worldRoot;
        private Transform pileVisualRoot;
        private Transform player;
        private SpriteRenderer playerRenderer;
        private SpriteRenderer handCassetteRenderer;
        private TextMeshPro handWorldLabel;
        private SpriteRenderer nightTintRenderer;
        private SpriteRenderer machineRenderer;
        private Transform pilePoint;
        private SpriteRenderer interactionHalo;
        private Transform counterPoint;
        private Transform upgradePoint;
        private Transform mysteryPoint;
        private Transform televisionPoint;
        private Transform doorPoint;
        private GameObject mysteryObject;
        private GameObject visitorBubble;
        private readonly Dictionary<string, VhsPileCassette> pileObjects = new Dictionary<string, VhsPileCassette>();
        private readonly List<BoxCollider2D> fixtureColliders = new List<BoxCollider2D>();
        private SortingGroup playerDepthGroup;
        private SortingGroup assistantDepthGroup;
        [SerializeField] private int[] debugShelfCounts = new int[4];

        private GameObject assistantObject;
        private SpriteRenderer assistantRenderer;
        private AssistantState assistantState;
        private float assistantCooldown;
        private CassetteData assistantHeldCassette;
        private string assistantTargetVisitorId;
        private readonly List<Vector2> assistantPath = new List<Vector2>();
        private int assistantPathIndex;
        private ShelfRuntime assistantReservedShelf;
        private int assistantReservedSlot = -1;

        private Canvas canvas;
        private GameObject hudRoot;
        private TextMeshProUGUI moneyText;
        private TextMeshProUGUI phaseText;
        private TextMeshProUGUI timerText;
        private TextMeshProUGUI handsText;
        private TextMeshProUGUI orderText;
        private GameObject orderPanelObject;
        private TextMeshProUGUI promptText;
        private GameObject tutorialPanel;
        private TextMeshProUGUI tutorialText;
        private float tutorialHideAt;
        private UnityEngine.UI.Button startDayButton;

        private GameObject mainMenuPanel;
        private UnityEngine.UI.Button continueButton;
        private GameObject saveManagementPanel;
        private TextMeshProUGUI saveInfoText;
        private UnityEngine.UI.Button deleteSaveButton;
        private GameObject settingsPanel;
        private GameObject pausePanel;
        private GameObject upgradePanel;
        private TextMeshProUGUI upgradeMoneyText;
        private GameObject resultsPanel;
        private TextMeshProUGUI resultsText;
        private GameObject dialoguePanel;
        private TextMeshProUGUI dialogueText;
        private GameObject inspectPanel;
        private UnityEngine.UI.Image inspectCover;
        private TextMeshProUGUI inspectTitle;
        private TextMeshProUGUI inspectDescription;
        private GameObject cutscenePanel;
        private TextMeshProUGUI cutsceneLoreText;
        private TextMeshProUGUI cutsceneGlitchText;
        private UnityEngine.UI.Button cutsceneContinueButton;
        private GameObject shelfBrowserPanel;
        private ShelfRuntime browsedShelf;
        private TextMeshProUGUI shelfBrowserTitle;
        private TextMeshProUGUI shelfBrowserStatus;
        private readonly List<UnityEngine.UI.Button> shelfSlotButtons = new List<UnityEngine.UI.Button>();
        private UnityEngine.UI.Image patienceFill;
        private TextMeshProUGUI patienceText;
        private TextMeshProUGUI feedbackText;
        private float feedbackUntil;
        private GameObject topBarObject;
        private UnityEngine.UI.Image[] inventoryIcons;

        private bool paused;
        private float previousTimeScale = 1f;
        private InteractionTarget nearestInteraction;
        private int lastOverflowLogBucket = -1;
        private Coroutine cutsceneRoutine;

        public VhsGameConfig Config
        {
            get => config;
            set => config = value;
        }

        public GamePhase CurrentPhase => phase;
        public VhsSaveData CurrentSave => save;

        private void Awake()
        {
            if (config == null)
            {
                config = Resources.Load<VhsGameConfig>("VhsGameConfig");
            }

            if (config == null)
            {
                config = ScriptableObject.CreateInstance<VhsGameConfig>();
                config.ResetToPrototypeDefaults();
                Debug.LogWarning("[VHS] Баланс-конфиг не найден. Используются временные значения в памяти.");
            }
            else if (config.upgrades == null || config.upgrades.Count < 10)
            {
                config.ResetToPrototypeDefaults();
            }

            Application.targetFrameRate = 60;
            BuildWorld();
            BuildUI();
            ShowMainMenu();
        }

        private void OnApplicationQuit()
        {
            if (save != null && phase != GamePhase.MainMenu)
            {
                SaveNow(false);
            }
        }

        private void Update()
        {
            UpdateTutorialToast();
            PulseMachine();
            if (phase != GamePhase.NightCutscene)
                gameCamera.orthographicSize = Mathf.Max(6.6f, 10.1f / gameCamera.aspect);

            if (phase == GamePhase.MainMenu)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HandleEscape();
            }

            bool closedUpgradeThisFrame = HandleUpgradeToggleInput();
            if (paused)
            {
                UpdateHud();
                return;
            }
            feedbackText.gameObject.SetActive(Time.unscaledTime < feedbackUntil);

            if (phase == GamePhase.EveningPrep && save != null && save.foreignCassetteReceivedToday &&
                Vector2.Distance(player.position, televisionPoint.position) <= 1.35f && !IsBlockingPanelOpen())
            {
                BeginNight();
            }

            HandleInspectInput();
            bool movementBlocked = IsBlockingPanelOpen() || inspectPanel.activeSelf || phase == GamePhase.DayResults || phase == GamePhase.NightCutscene;
            if (!movementBlocked)
            {
                MovePlayer(Time.deltaTime);
                FindNearestInteraction();
                UpdateInteractionPrompt();
                if (!closedUpgradeThisFrame && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    if (nearestInteraction != null && nearestInteraction.kind == InteractionKind.Shelf &&
                        (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed))
                        OpenShelfBrowser(nearestInteraction.shelf);
                    else Interact();
                }
            }
            else if (!inspectPanel.activeSelf)
            {
                promptText.text = string.Empty;
            }

            switch (phase)
            {
                case GamePhase.DayPlay:
                    UpdateDay(Time.deltaTime);
                    break;
                case GamePhase.MorningPrep:
                    SetAtmosphere(0f);
                    break;
                case GamePhase.EveningPrep:
                    SetAtmosphere(0.38f);
                    break;
                case GamePhase.NightPlay:
                case GamePhase.NightCutscene:
                    SetAtmosphere(0.62f);
                    break;
            }

            UpdatePlayerDepth();
            UpdateHud();
            UpdateDebugFields();

#if UNITY_EDITOR
            if (Keyboard.current != null && Keyboard.current.f6Key.wasPressedThisFrame)
            {
                DebugAdvancePhase();
            }
            if (Keyboard.current != null && Keyboard.current.f7Key.wasPressedThisFrame && save != null)
            {
                save.totalMoney += 100;
                UpdateHud();
                RefreshUpgradeMenu();
            }
#endif
        }

        private void BuildWorld()
        {
            worldRoot = new GameObject("World").transform;
            worldRoot.SetParent(transform, false);

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(UniversalAdditionalCameraData));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            gameCamera = cameraObject.GetComponent<Camera>();
            gameCamera.orthographic = true;
            gameCamera.orthographicSize = 5.4f;
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = new Color32(98, 83, 119, 255);

            GameObject lightObject = new GameObject("Global Light 2D", typeof(Light2D));
            lightObject.transform.SetParent(worldRoot, false);
            globalLight = lightObject.GetComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
            globalLight.intensity = 1f;
            globalLight.color = new Color(1f, 0.96f, 0.86f, 1f);

            SpriteRenderer background = CreateSpriteObject("Salon Background", VhsSpriteFactory.Background(), Vector3.zero, -100, worldRoot);
            background.transform.localScale = Vector3.one;
            interactionHalo = CreateSpriteObject("Interaction Marker", VhsSpriteFactory.Shadow(), Vector3.zero, 20, worldRoot);
            interactionHalo.color = UiTeal;
            interactionHalo.transform.localScale = new Vector3(0.9f, 1.5f, 1f);
            interactionHalo.gameObject.SetActive(false);

            SpriteRenderer warmGlow = CreateSpriteObject("Warm Pool Of Light", VhsSpriteFactory.Glow(), new Vector3(1.8f, -1.25f, 0f), -80, worldRoot);
            warmGlow.transform.localScale = new Vector3(2.7f, 1.75f, 1f);
            warmGlow.color = new Color(1f, 0.75f, 0.42f, 0.8f);

            CreateDecoration("Entrance Rug", VhsDecorationKind.Rug, new Vector3(0f, -3.68f, 0f), 8, new Vector3(1.35f, 1.15f, 1f));
            CreateDecoration("Left Poster", VhsDecorationKind.Poster, new Vector3(-8.05f, 2.55f, 0f), 45, Vector3.one * 0.9f);
            CreateDecoration("Left Plant", VhsDecorationKind.Plant, new Vector3(-8.05f, -1.95f, 0f), 76, Vector3.one * 0.95f);
            CreateDecoration("Right Plant", VhsDecorationKind.Plant, new Vector3(8.65f, -1.05f, 0f), 112, Vector3.one * 0.72f);
            CreateDecoration("Back Trash Can", VhsDecorationKind.TrashCan, new Vector3(4.15f, 2.55f, 0f), 73, Vector3.one * 0.78f);
            CreateDecoration("Storage Box", VhsDecorationKind.CardboardBox, new Vector3(-8.5f, 0.3f, 0f), 92, Vector3.one * 0.65f);
            CreateDecoration("Loose Cables", VhsDecorationKind.CableCoil, new Vector3(7.25f, -0.95f, 0f), 54, Vector3.one * 0.9f);

            nightTintRenderer = CreateSpriteObject("Night Tint", VhsSpriteFactory.White(), new Vector3(0f, 0f, 0f), 800, worldRoot);
            nightTintRenderer.transform.localScale = new Vector3(10.2f, 5.7f, 1f);
            nightTintRenderer.color = new Color(0.03f, 0.04f, 0.13f, 0f);

            SpriteRenderer vignette = CreateSpriteObject("Painted Vignette", VhsSpriteFactory.Vignette(), Vector3.zero, 790, worldRoot);
            vignette.color = Color.white;

            SpriteRenderer counter = CreateSpriteObject("Counter", VhsSpriteFactory.Fixture(VhsFixtureKind.Counter), new Vector3(0f, 2.92f, 0f), 70, worldRoot);
            counter.transform.localScale = new Vector3(1f, 0.72f, 1f);
            AddFixtureShadow(counter.transform, new Vector3(0f, -0.2f, 0f), new Vector3(3.2f, 1.05f, 1f), 68);
            counterPoint = CreatePoint("Counter Interaction", new Vector2(0f, 2.4f));
            RegisterFootprint(counter, new Vector2(4.95f, 0.52f), new Vector2(0f, 0.2f));
            interactions.Add(new InteractionTarget { kind = InteractionKind.Counter, point = counterPoint });

            AddShelf(CassetteGenre.Action, new Vector2(-5.72f, 0.72f), new Vector2(-4f, 0.65f), new Rect(-6.9f, -0.2f, 2.35f, 2.5f));
            AddShelf(CassetteGenre.Comedy, new Vector2(5.72f, 0.72f), new Vector2(4f, 0.65f), new Rect(4.55f, -0.2f, 2.35f, 2.5f));
            AddShelf(CassetteGenre.Drama, new Vector2(-6.25f, -1.8f), new Vector2(-6.25f, -2.65f), new Rect(-7.35f, -3.08f, 2.25f, 2.25f));
            AddShelf(CassetteGenre.Audio, new Vector2(-3.45f, -1.8f), new Vector2(-3.45f, -2.65f), new Rect(-4.55f, -3.08f, 2.25f, 2.25f));

            machineRenderer = CreateSpriteObject("Mystic Machine", VhsSpriteFactory.Fixture(VhsFixtureKind.Machine), new Vector3(7.45f, -3.2f, 0f), 64, worldRoot);
            AddFixtureShadow(machineRenderer.transform, new Vector3(0f, -0.18f, 0f), new Vector3(1.75f, 1.2f, 1f), 62);
            RegisterFootprint(machineRenderer, new Vector2(1.82f, 0.62f), new Vector2(0f, 0.19f));

            pileVisualRoot = new GameObject("Individual Cassette Pile").transform;
            pileVisualRoot.SetParent(worldRoot, false);
            pileVisualRoot.gameObject.AddComponent<SortingGroup>().sortingOrder = 134;
            pilePoint = CreatePoint("Pile Interaction", new Vector2(4.7f, -2.85f));
            interactions.Add(new InteractionTarget { kind = InteractionKind.Pile, point = pilePoint });

            SpriteRenderer stand = CreateSpriteObject("Upgrade Stand", VhsSpriteFactory.Fixture(VhsFixtureKind.UpgradeStand), new Vector3(-7.95f, -3.55f, 0f), 66, worldRoot);
            AddFixtureShadow(stand.transform, new Vector3(0f, -0.1f, 0f), new Vector3(1.35f, 0.8f, 1f), 64);
            upgradePoint = CreatePoint("Upgrade Interaction", new Vector2(-7.8f, -2.7f));
            RegisterFootprint(stand, new Vector2(1.56f, 0.48f), new Vector2(0f, 0.1f));
            interactions.Add(new InteractionTarget { kind = InteractionKind.UpgradeStand, point = upgradePoint });

            SpriteRenderer television = CreateSpriteObject("Television", VhsSpriteFactory.Fixture(VhsFixtureKind.Television), new Vector3(2.95f, -3.5f, 0f), 66, worldRoot);
            AddFixtureShadow(television.transform, new Vector3(0f, -0.13f, 0f), new Vector3(1.5f, 0.9f, 1f), 64);
            televisionPoint = CreatePoint("Television Interaction", new Vector2(2.8f, -2.6f));
            RegisterFootprint(television, new Vector2(1.85f, 0.5f), new Vector2(0f, 0.11f));
            interactions.Add(new InteractionTarget { kind = InteractionKind.Television, point = televisionPoint });

            SpriteRenderer door = CreateSpriteObject("Service Door", VhsSpriteFactory.Fixture(VhsFixtureKind.Door), new Vector3(0f, -4.48f, 0f), 62, worldRoot);
            AddFixtureShadow(door.transform, new Vector3(0f, -0.05f, 0f), new Vector3(1.25f, 0.7f, 1f), 60);
            doorPoint = CreatePoint("Door Interaction", new Vector2(0f, -3.82f));
            interactions.Add(new InteractionTarget { kind = InteractionKind.Door, point = doorPoint });

            mysteryObject = new GameObject("Mysterious Person");
            mysteryObject.transform.SetParent(worldRoot, false);
            mysteryObject.transform.position = new Vector3(0.95f, -3.34f, 0f);
            SpriteRenderer mysteryRenderer = mysteryObject.AddComponent<SpriteRenderer>();
            mysteryRenderer.sprite = VhsSpriteFactory.MysteryPerson();
            mysteryRenderer.sortingOrder = 108;
            mysteryPoint = CreatePoint("Mystery Interaction", new Vector2(0.1f, -3.12f));
            interactions.Add(new InteractionTarget { kind = InteractionKind.MysteryPerson, point = mysteryPoint });
            mysteryObject.SetActive(false);

            BuildVisitorVisuals();

            player = new GameObject("Player").transform;
            player.SetParent(worldRoot, false);
            player.position = new Vector3(0f, -0.85f, 0f);
            SpriteRenderer shadow = CreateSpriteObject("Shadow", VhsSpriteFactory.Shadow(), new Vector3(0f, 0.08f, 0f), 79, player);
            shadow.color = Color.white;
            playerDepthGroup = player.gameObject.AddComponent<SortingGroup>();
            var playerBody = player.gameObject.AddComponent<Rigidbody2D>();
            playerBody.bodyType = RigidbodyType2D.Kinematic;
            playerBody.gravityScale = 0;
            playerBody.constraints = RigidbodyConstraints2D.FreezeRotation;
            var feet = player.gameObject.AddComponent<CircleCollider2D>();
            feet.radius = 0.23f;
            feet.offset = new Vector2(0, 0.1f);
            feet.isTrigger = true;
            playerRenderer = player.gameObject.AddComponent<SpriteRenderer>();
            playerRenderer.sprite = VhsSpriteFactory.Player();
            playerRenderer.sortingOrder = 100;
            handCassetteRenderer = CreateSpriteObject("Held Cassette", VhsSpriteFactory.Cassette(CassetteGenre.Action), new Vector3(0f, 1.2f, 0f), 102, player);
            handCassetteRenderer.transform.localScale = Vector3.one * 0.48f;
            handCassetteRenderer.gameObject.SetActive(false);
            handWorldLabel = VhsUIFactory.WorldText("HeldCount", player, string.Empty, 1.7f, UiCream, new Vector3(0f, 1.78f, 0f), new Vector2(2f, 0.4f));

            assistantObject = new GameObject("Counter Assistant");
            assistantObject.transform.SetParent(worldRoot, false);
            assistantObject.transform.position = new Vector3(1.3f, 2.05f, 0f);
            SpriteRenderer assistantShadow = CreateSpriteObject("Shadow", VhsSpriteFactory.Shadow(), new Vector3(0f, 0.08f, 0f), 78, assistantObject.transform);
            assistantShadow.color = Color.white;
            assistantDepthGroup = assistantObject.AddComponent<SortingGroup>();
            assistantRenderer = assistantObject.AddComponent<SpriteRenderer>();
            assistantRenderer.sprite = VhsSpriteFactory.Assistant();
            assistantRenderer.sortingOrder = 99;
            assistantObject.SetActive(false);
        }

        private void AddShelf(CassetteGenre genre, Vector2 position, Vector2 interactionPosition, Rect obstacle)
        {
            GameObject root = new GameObject(VhsNames.Genre(genre) + " Shelf");
            root.transform.SetParent(worldRoot, false);
            root.transform.position = position;
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = VhsSpriteFactory.Fixture(VhsFixtureKind.Shelf);
            renderer.sortingOrder = 60;
            AddFixtureShadow(root.transform, new Vector3(0f, -0.12f, 0f), new Vector3(1.45f, 1.02f, 1f), 58);
            Transform slotRoot = new GameObject("Slots").transform;
            slotRoot.SetParent(root.transform, false);
            TextMeshPro label = VhsUIFactory.WorldText("Label", root.transform, VhsNames.Genre(genre), 2.6f, UiInk, new Vector3(0f, 1.67f, 0f), new Vector2(3f, 0.55f));
            Transform point = CreatePoint(VhsNames.Genre(genre) + " Shelf Interaction", interactionPosition);
            BoxCollider2D footprint = RegisterFootprint(renderer, new Vector2(2.58f, 0.52f), new Vector2(0, 0.14f));
            ShelfRuntime runtime = new ShelfRuntime
            {
                genre = genre,
                root = root,
                interactionPoint = point,
                slotRoot = slotRoot,
                renderer = renderer,
                label = label,
                obstacle = obstacle,
                footprint = footprint
            };
            shelves.Add(runtime);
            interactions.Add(new InteractionTarget { kind = InteractionKind.Shelf, point = point, shelf = runtime });

        }

        private void BuildVisitorVisuals()
        {
            for (int i = 0; i < config.maxVisibleVisitors; i++)
            {
                GameObject visual = new GameObject("Visitor " + (i + 1));
                visual.transform.SetParent(worldRoot, false);
                SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
                renderer.sprite = VhsSpriteFactory.Visitor(i);
                renderer.sortingOrder = 120 - i;
                visual.transform.localScale = Vector3.one * 0.60f;
                visual.SetActive(false);
                visitorVisuals.Add(visual);
            }

            visitorBubble = new GameObject("Order Bubble");
            visitorBubble.transform.SetParent(worldRoot, false);
            SpriteRenderer bubbleRenderer = visitorBubble.AddComponent<SpriteRenderer>();
            bubbleRenderer.sprite = VhsSpriteFactory.SpeechBubble();
            bubbleRenderer.sortingOrder = 145;
            visitorBubble.SetActive(false);
        }

        private SpriteRenderer CreateSpriteObject(string name, Sprite sprite, Vector3 position, int sortingOrder, Transform parent)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = position;
            SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void CreateDecoration(string name, VhsDecorationKind kind, Vector3 position, int sortingOrder, Vector3 scale)
        {
            SpriteRenderer decoration = CreateSpriteObject(name, VhsSpriteFactory.Decoration(kind), position, sortingOrder, worldRoot);
            decoration.transform.localScale = scale;
            if (kind == VhsDecorationKind.Plant) RegisterFootprint(decoration, new Vector2(1.16f, 0.34f), new Vector2(0, 0.13f));
            if (kind == VhsDecorationKind.TrashCan) RegisterFootprint(decoration, new Vector2(0.83f, 0.32f), new Vector2(0, 0.11f));
            if (kind == VhsDecorationKind.CardboardBox) RegisterFootprint(decoration, new Vector2(1.35f, 0.34f), new Vector2(0, 0.1f));
        }

        private void AddFixtureShadow(Transform parent, Vector3 localPosition, Vector3 scale, int sortingOrder)
        {
            SpriteRenderer shadow = CreateSpriteObject("Painted Shadow", VhsSpriteFactory.Shadow(), localPosition, sortingOrder, parent);
            shadow.transform.localScale = scale;
            shadow.color = Color.white;
            shadow.sortingOrder = -1;
        }

        private Transform CreatePoint(string name, Vector2 position)
        {
            Transform point = new GameObject(name).transform;
            point.SetParent(worldRoot, false);
            point.position = position;
            return point;
        }

        private void BuildUI()
        {
            Transform uiRoot = new GameObject("UI").transform;
            uiRoot.SetParent(transform, false);
            canvas = VhsUIFactory.CreateCanvas(uiRoot);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = gameCamera;
            canvas.planeDistance = 1f;
            RectTransform canvasTransform = canvas.GetComponent<RectTransform>();
            BuildHud(canvasTransform);
            BuildMainMenu(canvasTransform);
            BuildPauseMenu(canvasTransform);
            BuildUpgradeMenu(canvasTransform);
            BuildResults(canvasTransform);
            BuildDialogue(canvasTransform);
            BuildInspect(canvasTransform);
            BuildCutscene(canvasTransform);
            BuildShelfBrowser(canvasTransform);
        }

        private void BuildHud(Transform parent)
        {
            RectTransform root = VhsUIFactory.Rect("HUD", parent, Vector2.zero, Vector2.one, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            hudRoot = root.gameObject;
            UnityEngine.UI.Image bar = VhsUIFactory.Panel("TopBar", root, new Color(0, 0, 0, 0),
                new Vector2(0, 1), Vector2.one, new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 106));
            topBarObject = bar.gameObject;
            var wallet = UiBox("Wallet", bar.transform, 24, 20, 232, 74, UiSurface);
            UiLabel(wallet.transform, "Caption", "КАССА САЛОНА", 15, 18, 9, 196, 22, UiMuted);
            moneyText = UiLabel(wallet.transform, "Money", "$0", 31, 18, 31, 196, 35, UiGold, true);
            var shift = VhsUIFactory.Panel("Shift", bar.transform, UiSurface, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(640, 74));
            phaseText = UiLabel(shift.transform, "Phase", "", 22, 22, 9, 460, 56, UiCream, true);
            timerText = UiLabel(shift.transform, "Timer", "", 32, 502, 9, 120, 56, UiTeal, true);
            var inventory = VhsUIFactory.Panel("Inventory", bar.transform, UiSurface, Vector2.one, Vector2.one, Vector2.one,
                new Vector2(-24, -20), new Vector2(326, 74));
            handsText = UiLabel(inventory.transform, "Hands", "", 20, 20, 9, 172, 56, UiCream, true);
            inventoryIcons = new UnityEngine.UI.Image[4];
            for (int i = 0; i < 4; i++)
            {
                var icon = UiBox("Held " + i, inventory.transform, 195 + i * 28, 22, 24, 30, UiMuted);
                icon.sprite = VhsSpriteFactory.Cassette(CassetteGenre.Action);
                icon.type = UnityEngine.UI.Image.Type.Simple;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                inventoryIcons[i] = icon;
            }
            var order = VhsUIFactory.Panel("OrderPanel", root, UiSurface, Vector2.one, Vector2.one, Vector2.one,
                new Vector2(-24, -120), new Vector2(320, 306));
            orderPanelObject = order.gameObject;
            UiLabel(order.transform, "Header", "ЗАКАЗ У ПРИЛАВКА", 17, 22, 19, 276, 30, UiTeal, true);
            orderText = UiLabel(order.transform, "Order", "", 23, 22, 61, 276, 167, UiCream);
            patienceText = UiLabel(order.transform, "Patience", "", 16, 22, 237, 276, 30, UiMuted);
            var track = UiBox("PatienceTrack", order.transform, 22, 280, 276, 5, new Color32(191, 172, 204, 255));
            patienceFill = VhsUIFactory.Panel("PatienceFill", track.transform, UiTeal, Vector2.zero, Vector2.one,
                Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            patienceFill.type = UnityEngine.UI.Image.Type.Filled;
            patienceFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            patienceFill.fillOrigin = 0;
            patienceFill.raycastTarget = false;
            var prompt = VhsUIFactory.Panel("PromptPanel", root, UiSurface,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(900, 58));
            promptText = VhsUIFactory.Text("Prompt", prompt.transform, "", 23, UiCream, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.one * 0.5f, Vector2.zero, new Vector2(-28, -8));
            tutorialPanel = VhsUIFactory.Panel("TutorialToast", root, UiPaper, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0.5f, 1), new Vector2(0, -122), new Vector2(770, 68)).gameObject;
            tutorialText = VhsUIFactory.Text("Tutorial", tutorialPanel.transform, "", 23, UiInk, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.one * 0.5f, Vector2.zero, new Vector2(-38, -12), true);
            tutorialPanel.SetActive(false);
            feedbackText = VhsUIFactory.Text("Feedback", root, "", 24, UiGold, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 100), new Vector2(820, 42), true);
            feedbackText.gameObject.SetActive(false);
            UiLabel(root, "Controls", "WASD  ходьба   ·   ПКМ  осмотр   ·   Esc  пауза", 17, 24, 1040, 475, 26, UiPaper);
            startDayButton = VhsUIFactory.Button("StartDay", root, "Начать смену", UiPink, UiInk,
                new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 22), new Vector2(290, 58), out _);
            startDayButton.onClick.AddListener(StartDay);
        }

        private void BuildMainMenu(Transform parent)
        {
            mainMenuPanel = VhsUIFactory.Panel("MainMenu", parent, new Color(0.32f, 0.26f, 0.37f, 0.45f),
                Vector2.zero, Vector2.one, Vector2.one * 0.5f, Vector2.zero, Vector2.zero).gameObject;
            var card = VhsUIFactory.Panel("MenuCard", mainMenuPanel.transform, UiPaper,
                Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(1420, 890));
            UiLabel(card.transform, "Edition", "1994  /  ВАША НОВАЯ СМЕНА", 18, 70, 50, 630, 30, UiTeal, true);
            UiLabel(card.transform, "Title", "ВИДЕОСАЛОН\nНАСЛЕДСТВО", 64, 70, 118, 650, 176, UiCream, true);
            UiLabel(card.transform, "Subtitle", "Днём — кассеты, посетители и маленький бизнес.\nНочью — записи, которых никто не оставлял.", 25, 74, 323, 590, 95, UiMuted);
            var cassette = UiBox("HeroCassette", card.transform, 105, 455, 460, 275, Color.white);
            cassette.sprite = VhsSpriteFactory.Cassette(CassetteGenre.Comedy);
            cassette.type = UnityEngine.UI.Image.Type.Simple;
            cassette.preserveAspect = true;
            cassette.raycastTarget = false;
            UiLabel(card.transform, "Story", "У каждой записи есть своя история.", 22, 74, 746, 630, 45, UiGold);
            UiBox("Divider", card.transform, 744, 75, 2, 720, new Color32(179, 157, 190, 255)).raycastTarget = false;
            UiLabel(card.transform, "MenuTitle", "Открыть салон", 34, 822, 122, 500, 56, UiCream, true);
            UiLabel(card.transform, "MenuCaption", "Восемь минут до конца дневной смены", 19, 822, 186, 500, 43, UiMuted);
            RectTransform actions = VhsUIFactory.Rect("Actions", card.transform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(0, 1), new Vector2(822, -275), new Vector2(505, 440));
            var layout = actions.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = 17;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            UnityEngine.UI.Button Action(string name, string label, Color color, Color textColor)
            {
                var button = VhsUIFactory.Button(name, actions, label, color, textColor, Vector2.zero, Vector2.zero,
                    Vector2.one * 0.5f, Vector2.zero, new Vector2(505, 70), out var text);
                text.fontSize = 25;
                text.fontSizeMax = 25;
                button.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 70;
                return button;
            }
            Action("NewGame", "Новая игра", UiPink, UiInk).onClick.AddListener(NewGame);
            continueButton = Action("Continue", "Продолжить", UiSurface, UiCream);
            continueButton.onClick.AddListener(ContinueGame);
            Action("Saves", "Управление сохранениями", UiSurface, UiCream).onClick.AddListener(OpenSaveManagement);
            Action("Settings", "Настройки", UiSurface, UiCream).onClick.AddListener(() => settingsPanel.SetActive(true));
            Action("Exit", "Выход", UiSurface, UiMuted).onClick.AddListener(QuitGame);
            UiLabel(card.transform, "Controls", "WASD  движение   /   E  действие   /   ПКМ  осмотр", 17, 822, 752, 510, 54, UiMuted);
            BuildSaveManagement(parent);
            BuildSettings(parent);
        }

        private UnityEngine.UI.Button MenuButton(Transform parent, string label, float y, Color color, out TextMeshProUGUI text)
        {
            return VhsUIFactory.Button(label.Replace(" ", string.Empty) + "Button", parent, label, color, color == UiTeal ? UiPaper : UiInk,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(560f, 78f), out text);
        }

        private void BuildSaveManagement(Transform parent)
        {
            saveManagementPanel = VhsUIFactory.Panel("SaveManagement", parent, new Color(0.26f, 0.21f, 0.32f, 0.55f), Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject;
            UnityEngine.UI.Image card = VhsUIFactory.Panel("Card", saveManagementPanel.transform, UiPaper,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 520f));
            VhsUIFactory.Text("Header", card.transform, "УПРАВЛЕНИЕ СОХРАНЕНИЯМИ", 38f, UiCream, TextAlignmentOptions.Center,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -35f), new Vector2(-40f, 70f), true);
            saveInfoText = VhsUIFactory.Text("Info", card.transform, string.Empty, 24f, UiCream, TextAlignmentOptions.TopLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(-80f, -170f), false);
            deleteSaveButton = VhsUIFactory.Button("DeleteSave", card.transform, "Удалить сохранение", new Color32(145, 54, 73, 255), UiPaper,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(45f, 40f), new Vector2(370f, 78f), out _);
            deleteSaveButton.onClick.AddListener(DeleteSave);
            UnityEngine.UI.Button close = VhsUIFactory.Button("Close", card.transform, "Закрыть", UiTeal, UiPaper,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-45f, 40f), new Vector2(260f, 78f), out _);
            close.onClick.AddListener(() => saveManagementPanel.SetActive(false));
            saveManagementPanel.SetActive(false);
        }

        private void BuildSettings(Transform parent)
        {
            settingsPanel = VhsUIFactory.Panel("Settings", parent, new Color(0.26f, 0.21f, 0.32f, 0.55f), Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject;
            UnityEngine.UI.Image card = VhsUIFactory.Panel("Card", settingsPanel.transform, UiPaper,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 420f));
            VhsUIFactory.Text("Header", card.transform, "НАСТРОЙКИ", 42f, UiCream, TextAlignmentOptions.Center,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -35f), new Vector2(-40f, 70f), true);
            VhsUIFactory.Text("Placeholder", card.transform, "Заглушка прототипа.\nГромкость и графические опции появятся позже.", 27f, UiTeal, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 15f), new Vector2(-70f, -130f), false);
            UnityEngine.UI.Button close = VhsUIFactory.Button("Close", card.transform, "Назад", UiPink, UiCream,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 38f), new Vector2(300f, 76f), out _);
            close.onClick.AddListener(() => settingsPanel.SetActive(false));
            settingsPanel.SetActive(false);
        }

        private void BuildPauseMenu(Transform parent)
        {
            pausePanel = VhsUIFactory.Panel("PauseMenu", parent, new Color(0.26f, 0.21f, 0.32f, 0.55f), Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject;
            UnityEngine.UI.Image card = VhsUIFactory.Panel("Card", pausePanel.transform, UiPaper,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(610f, 540f));
            VhsUIFactory.Text("Header", card.transform, "ПАУЗА", 46f, UiCream, TextAlignmentOptions.Center,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(-40f, 80f), true);
            UnityEngine.UI.Button resume = MenuButton(card.transform, "Продолжить", -155f, UiTeal, out _);
            resume.onClick.AddListener(TogglePause);
            UnityEngine.UI.Button saveButton = MenuButton(card.transform, "Сохранить", -260f, UiPurple, out _);
            saveButton.onClick.AddListener(() =>
            {
                SaveNow(true);
                saveButton.GetComponentInChildren<TextMeshProUGUI>().text = "Сохранено";
            });
            UnityEngine.UI.Button menu = MenuButton(card.transform, "В главное меню", -365f, UiPink, out _);
            menu.onClick.AddListener(ReturnToMainMenu);
            pausePanel.SetActive(false);
        }

        private void BuildUpgradeMenu(Transform parent)
        {
            upgradePanel = VhsUIFactory.Panel("UpgradeMenu", parent, new Color(0.26f, 0.21f, 0.32f, 0.55f),
                Vector2.zero, Vector2.one, Vector2.one * 0.5f, Vector2.zero, Vector2.zero).gameObject;
            var card = VhsUIFactory.Panel("Card", upgradePanel.transform, UiPaper, Vector2.one * 0.5f,
                Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(1580, 950));
            UiLabel(card.transform, "Caption", "УТРЕННЯЯ ПОДГОТОВКА", 17, 48, 26, 650, 28, UiTeal, true);
            UiLabel(card.transform, "Header", "Сделайте салон своим", 39, 48, 66, 940, 54, UiCream, true);
            upgradeMoneyText = UiLabel(card.transform, "Money", "$0", 34, 1250, 51, 275, 62, UiGold, true);
            upgradeMoneyText.alignment = TextAlignmentOptions.MidlineRight;
            RectTransform gridRect = VhsUIFactory.Rect("UpgradeGrid", card.transform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(0, 1), new Vector2(48, -148), new Vector2(1484, 700));
            var grid = gridRect.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            grid.cellSize = new Vector2(730, 124);
            grid.spacing = new Vector2(24, 16);
            grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            for (int i = 0; i < 10; i++)
            {
                UpgradeType type = (UpgradeType)i;
                UpgradeTrack track = config.GetTrack(type);
                var row = VhsUIFactory.Panel(type + "Row", gridRect, UiSurface, Vector2.zero, Vector2.zero,
                    Vector2.one * 0.5f, Vector2.zero, new Vector2(730, 124));
                var tile = UiBox("IconTile", row.transform, 18, 24, 70, 76, new Color32(194, 171, 211, 255));
                var icon = UiBox("Icon", tile.transform, 12, 13, 46, 46, track.iconColor);
                icon.sprite = VhsVectorArt.UpgradeIcon(type);
                icon.type = UnityEngine.UI.Image.Type.Simple;
                icon.raycastTarget = false;
                var title = UiLabel(row.transform, "Title", track.title, 23, 108, 16, 425, 32, UiCream, true);
                var level = UiLabel(row.transform, "Level", "", 17, 108, 49, 425, 28, UiMuted);
                var effect = UiLabel(row.transform, "Effect", "", 19, 108, 81, 425, 27, UiTeal);
                var buy = UiAction(row.transform, "Buy", "$0", 550, 32, 160, 58, UiPink, UiInk, out var buyLabel);
                UpgradeType captured = type;
                buy.onClick.AddListener(() => PurchaseUpgrade(captured));
                upgradeRows[type] = new UpgradeRowUI { icon = icon, title = title, level = level, effect = effect, buyButton = buy, buyLabel = buyLabel };
            }
            UiLabel(card.transform, "Hint", "Покупки доступны утром. E или Esc — вернуться в салон.", 20, 48, 858, 1070, 46, UiMuted);
            UiAction(card.transform, "Close", "Вернуться в салон", 1240, 855, 292, 58, UiTeal, UiPaper, out _)
                .onClick.AddListener(() => upgradePanel.SetActive(false));
            upgradePanel.SetActive(false);
        }

        private static readonly Color UiSurface = new Color32(221, 204, 231, 255);
        private static readonly Color UiPaper = new Color32(255, 242, 204, 255);
        private static readonly Color UiMuted = new Color32(109, 92, 115, 255);

        private static UnityEngine.UI.Image UiBox(string name, Transform parent, float x, float y, float width, float height, Color color)
        {
            return VhsUIFactory.Panel(name, parent, color, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(x, -y), new Vector2(width, height));
        }

        private static TextMeshProUGUI UiLabel(Transform parent, string name, string value, float size,
            float x, float y, float width, float height, Color color, bool bold = false)
        {
            return VhsUIFactory.Text(name, parent, value, size, color, TextAlignmentOptions.MidlineLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y), new Vector2(width, height), bold);
        }

        private static UnityEngine.UI.Button UiAction(Transform parent, string name, string value, float x, float y,
            float width, float height, Color background, Color foreground, out TextMeshProUGUI label)
        {
            return VhsUIFactory.Button(name, parent, value, background, foreground, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(0, 1), new Vector2(x, -y), new Vector2(width, height), out label);
        }

        private void BuildShelfBrowser(Transform parent)
        {
            shelfBrowserPanel = VhsUIFactory.Panel("ShelfBrowser", parent, new Color(0.26f, 0.21f, 0.32f, 0.55f),
                Vector2.zero, Vector2.one, Vector2.one * 0.5f, Vector2.zero, Vector2.zero).gameObject;
            var card = VhsUIFactory.Panel("Card", shelfBrowserPanel.transform, UiPaper, Vector2.one * 0.5f,
                Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(1080, 820));
            shelfBrowserTitle = UiLabel(card.transform, "Title", "Полка", 35, 42, 30, 990, 52, UiCream, true);
            shelfBrowserStatus = UiLabel(card.transform, "Status", "", 21, 42, 92, 990, 52, UiMuted);
            UiLabel(card.transform, "Hint", "Нажмите на кассету, чтобы забрать её. Пустые ячейки затемнены.", 19, 42, 152, 990, 35, UiTeal);
            var slots = VhsUIFactory.Rect("Slots", card.transform, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(0, 1), new Vector2(42, -214), new Vector2(996, 484));
            var grid = slots.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            grid.cellSize = new Vector2(91, 69);
            grid.spacing = new Vector2(9, 11);
            grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 10;
            for (int i = 0; i < 60; i++)
            {
                var button = VhsUIFactory.Button("Slot " + i, slots, "", UiSurface, UiCream, Vector2.zero, Vector2.zero,
                    Vector2.one * 0.5f, Vector2.zero, new Vector2(91, 69), out var label);
                label.fontSize = 16;
                label.fontSizeMax = 16;
                int captured = i;
                button.onClick.AddListener(() => TakeShelfSlot(captured));
                shelfSlotButtons.Add(button);
            }
            UiAction(card.transform, "Close", "Готово", 802, 728, 236, 58, UiTeal, UiPaper, out _)
                .onClick.AddListener(() => shelfBrowserPanel.SetActive(false));
            UiLabel(card.transform, "Shortcut", "Shift + E  —  взять ещё с полки     /     Esc  —  закрыть", 18, 42, 734, 740, 48, UiMuted);
            shelfBrowserPanel.SetActive(false);
        }

        private void OpenShelfBrowser(ShelfRuntime shelf)
        {
            if (phase != GamePhase.DayPlay || shelf == null || !ShelfUnlocked(shelf.genre)) return;
            browsedShelf = shelf;
            RefreshShelfBrowser();
            shelfBrowserPanel.SetActive(true);
            interactionHalo.gameObject.SetActive(false);
        }

        private void RefreshShelfBrowser()
        {
            if (browsedShelf == null) return;
            ShelfData data = GetShelfData(browsedShelf.genre);
            shelfBrowserTitle.text = "Полка · " + VhsNames.Genre(browsedShelf.genre);
            shelfBrowserStatus.text = $"В руках {save.hands.Count}/{CarryCapacity()}  ·  Забирайте кассеты в нужном порядке";
            for (int i = 0; i < shelfSlotButtons.Count; i++)
            {
                var button = shelfSlotButtons[i];
                bool visible = i < data.slots.Count;
                button.gameObject.SetActive(visible);
                if (!visible) continue;
                ShelfSlotData slot = data.slots[i];
                bool occupied = slot.occupied && slot.cassette != null;
                button.interactable = occupied && save.hands.Count < CarryCapacity();
                var image = button.GetComponent<UnityEngine.UI.Image>();
                image.color = occupied ? Color.Lerp(UiSurface, VhsSpriteFactory.GenreColor(slot.cassette.genre), 0.3f) : UiSurface;
                button.GetComponentInChildren<TextMeshProUGUI>().text = occupied
                    ? $"{(slot.cassette.isMisplaced ? "! " : "")}{VhsNames.Genre(slot.cassette.genre)}" : "—";
            }
        }

        private void TakeShelfSlot(int index)
        {
            if (phase != GamePhase.DayPlay || browsedShelf == null || save.hands.Count >= CarryCapacity()) return;
            ShelfData data = GetShelfData(browsedShelf.genre);
            if (index < 0 || index >= data.slots.Count) return;
            ShelfSlotData slot = data.slots[index];
            if (!slot.occupied || slot.cassette == null) return;
            save.hands.Add(slot.cassette);
            slot.occupied = false;
            slot.cassette = null;
            RefreshHandsVisual();
            RefreshShelfVisual(browsedShelf);
            RefreshShelfBrowser();
            if (save.hands.Count >= CarryCapacity()) shelfBrowserPanel.SetActive(false);
        }

        private void Notify(string message, bool success = true)
        {
            feedbackText.text = message;
            feedbackText.color = success ? UiGold : UiPink;
            feedbackUntil = Time.unscaledTime + 3f;
            feedbackText.gameObject.SetActive(true);
        }

        private void BuildResults(Transform parent)
        {
            resultsPanel = VhsUIFactory.Panel("DayResults", parent, new Color(0.26f, 0.21f, 0.32f, 0.55f), Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject;
            UnityEngine.UI.Image card = VhsUIFactory.Panel("Card", resultsPanel.transform, UiPaper,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(850f, 650f));
            VhsUIFactory.Text("Header", card.transform, "ИТОГИ ДНЯ", 48f, UiCream, TextAlignmentOptions.Center,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(-40f, 80f), true);
            resultsText = VhsUIFactory.Text("Results", card.transform, string.Empty, 31f, UiCream, TextAlignmentOptions.TopLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(-130f, -190f), false);
            UnityEngine.UI.Button next = VhsUIFactory.Button("Next", card.transform, "Далее", UiPink, UiCream,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(330f, 82f), out _);
            next.onClick.AddListener(ProceedToEvening);
            resultsPanel.SetActive(false);
        }

        private void BuildDialogue(Transform parent)
        {
            dialoguePanel = VhsUIFactory.Panel("MysteryDialogue", parent, new Color(0.26f, 0.21f, 0.32f, 0.5f), Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject;
            UnityEngine.UI.Image card = VhsUIFactory.Panel("Card", dialoguePanel.transform, UiPaper,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(1240f, 330f));
            dialogueText = VhsUIFactory.Text("Dialogue", card.transform, string.Empty, 30f, UiCream, TextAlignmentOptions.TopLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(-80f, -105f), false);
            UnityEngine.UI.Button take = VhsUIFactory.Button("TakeCassette", card.transform, "Взять чужую кассету", UiPurple, UiCream,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 32f), new Vector2(430f, 72f), out _);
            take.onClick.AddListener(ReceiveForeignCassette);
            dialoguePanel.SetActive(false);
        }

        private void BuildInspect(Transform parent)
        {
            inspectPanel = VhsUIFactory.Panel("CassetteInspect", parent, new Color(0.02f, 0.02f, 0.05f, 0.78f), Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject;
            UnityEngine.UI.Image card = VhsUIFactory.Panel("Card", inspectPanel.transform, UiPaper,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 820f));
            inspectCover = VhsUIFactory.Panel("Cover", card.transform, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(470f, 310f));
            inspectCover.preserveAspect = true;
            inspectTitle = VhsUIFactory.Text("Title", card.transform, string.Empty, 48f, UiInk, TextAlignmentOptions.Center,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -430f), new Vector2(-70f, 86f), true);
            inspectDescription = VhsUIFactory.Text("Description", card.transform, string.Empty, 28f, UiInk, TextAlignmentOptions.TopLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -135f), new Vector2(-110f, -500f), false);
            VhsUIFactory.Text("Hint", card.transform, "Отпустите ПКМ, чтобы закрыть", 21f, UiPurple, TextAlignmentOptions.Center,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(-50f, 45f), false);
            inspectPanel.SetActive(false);
        }

        private void BuildCutscene(Transform parent)
        {
            cutscenePanel = VhsUIFactory.Panel("NightCutscene", parent, new Color(0.01f, 0.015f, 0.025f, 0.99f), Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject;
            UnityEngine.UI.Image screen = VhsUIFactory.Panel("CRT", cutscenePanel.transform, new Color(0.04f, 0.12f, 0.12f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1540f, 850f));
            VhsUIFactory.Text("REC", screen.transform, "● REC", 28f, new Color32(236, 81, 111, 255), TextAlignmentOptions.TopLeft,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(35f, -28f), new Vector2(-80f, -60f), true);
            cutsceneLoreText = VhsUIFactory.Text("Lore", screen.transform, string.Empty, 38f, new Color32(192, 239, 219, 255), TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-220f, -210f), false);
            cutsceneGlitchText = VhsUIFactory.Text("Glitch", screen.transform, "", 75f, UiPink, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-70f, -70f), true);
            cutsceneContinueButton = VhsUIFactory.Button("Continue", screen.transform, "Закончить просмотр", UiPink, UiCream,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(430f, 78f), out _);
            cutsceneContinueButton.onClick.AddListener(FinishNightCutscene);
            cutscenePanel.SetActive(false);
        }

        private void ShowMainMenu()
        {
            paused = false;
            tutorialQueue.Clear();
            tutorialPanel.SetActive(false);
            shelfBrowserPanel.SetActive(false);
            Time.timeScale = 0f;
            phase = GamePhase.MainMenu;
            mainMenuPanel.SetActive(true);
            hudRoot.SetActive(false);
            pausePanel.SetActive(false);
            upgradePanel.SetActive(false);
            resultsPanel.SetActive(false);
            dialoguePanel.SetActive(false);
            inspectPanel.SetActive(false);
            cutscenePanel.SetActive(false);
            continueButton.interactable = VhsSaveSystem.HasSave();
            mysteryObject.SetActive(false);
            assistantObject.SetActive(false);
            UpdateVisitorVisuals();
        }

        public void NewGame()
        {
            VhsSaveSystem.Delete();
            save = CreateNewSave();
            EnterLoadedGame();
            SaveNow(false);
        }

        public void ContinueGame()
        {
            VhsSaveData loaded = VhsSaveSystem.Load();
            if (loaded == null)
            {
                continueButton.interactable = false;
                return;
            }

            save = loaded;
            ValidateSaveData();
            EnterLoadedGame();
        }

        private VhsSaveData CreateNewSave()
        {
            VhsSaveData data = new VhsSaveData
            {
                totalMoney = 0,
                currentDayIndex = 1,
                phase = GamePhase.MorningPrep,
                dayTimeRemaining = config.dayDurationSeconds,
                machineTimer = config.machineStartIntervalSeconds,
                visitorSpawnTimer = config.baseVisitorSpawnInterval,
                playerX = 0f,
                playerY = -0.85f
            };
            save = data;
            EnsureShelfData();
            List<CassetteGenre> genres = UnlockedGenres();
            for (int i = 0; i < config.initialPileCount; i++)
            {
                data.pile.Add(CreateCassette(genres[UnityEngine.Random.Range(0, genres.Count)]));
            }

            return data;
        }

        private void ValidateSaveData()
        {
            if (save.upgradeLevels == null || save.upgradeLevels.Length != 10)
            {
                int[] migrated = new int[10];
                if (save.upgradeLevels != null)
                {
                    Array.Copy(save.upgradeLevels, migrated, Mathf.Min(save.upgradeLevels.Length, migrated.Length));
                }
                save.upgradeLevels = migrated;
            }
            save.shelves ??= new List<ShelfData>();
            save.pile ??= new List<CassetteData>();
            save.machineStoredCassettes ??= new List<CassetteData>();
            save.hands ??= new List<CassetteData>();
            // JsonUtility materializes null inline classes as empty objects. They are not tapes.
            if (save.assistantHeldCassette != null && string.IsNullOrEmpty(save.assistantHeldCassette.id))
                save.assistantHeldCassette = null;
            save.pile.RemoveAll(tape => tape == null || string.IsNullOrEmpty(tape.id));
            save.machineStoredCassettes.RemoveAll(tape => tape == null || string.IsNullOrEmpty(tape.id));
            save.hands.RemoveAll(tape => tape == null || string.IsNullOrEmpty(tape.id));
            save.visitors ??= new List<VisitorData>();
            save.dayStats ??= new DayStatsData();
            save.tutorialFlagsShown ??= new List<string>();
            save.currentDayIndex = Mathf.Max(1, save.currentDayIndex);
            if (save.phase == GamePhase.MainMenu || save.phase == GamePhase.NightCutscene)
            {
                save.phase = save.phase == GamePhase.NightCutscene ? GamePhase.NightPlay : GamePhase.MorningPrep;
            }
            EnsureShelfData();
        }

        private void EnterLoadedGame()
        {
            ValidateSaveData();
            paused = false;
            pausePanel.SetActive(false);
            upgradePanel.SetActive(false);
            shelfBrowserPanel.SetActive(false);
            if (save.assistantHeldCassette != null)
            {
                ReturnCassetteToPile(save.assistantHeldCassette);
                save.assistantHeldCassette = null;
            }
            assistantHeldCassette = null;
            assistantState = AssistantState.Idle;
            Time.timeScale = 1f;
            gameCamera.transform.position = new Vector3(0f, 0f, -10f);
            mainMenuPanel.SetActive(false);
            hudRoot.SetActive(true);
            player.position = new Vector3(save.playerX, save.playerY, 0f);
            SetPhase(save.phase, false);
            RefreshAllShelfVisuals();
            RefreshPileVisual();
            RefreshHandsVisual();
            UpdateVisitorVisuals();
            UpdateHud();
        }

        private void SetPhase(GamePhase newPhase, bool persist = true)
        {
            phase = newPhase;
            debugPhase = newPhase;
            if (save != null && persist && newPhase != GamePhase.MainMenu)
            {
                save.phase = newPhase;
            }

            startDayButton.gameObject.SetActive(newPhase == GamePhase.MorningPrep);
            resultsPanel.SetActive(newPhase == GamePhase.DayResults);
            dialoguePanel.SetActive(false);
            cutscenePanel.SetActive(newPhase == GamePhase.NightCutscene);
            mysteryObject.SetActive(newPhase == GamePhase.EveningPrep && save != null && !save.foreignCassetteReceivedToday);
            assistantObject.SetActive(newPhase == GamePhase.DayPlay && IsUpgradeUnlocked(UpgradeType.CounterAssistant));

            if (newPhase == GamePhase.DayResults)
            {
                UpdateResultsText();
            }
            if (newPhase != GamePhase.DayPlay) shelfBrowserPanel.SetActive(false);
            interactionHalo.gameObject.SetActive(false);
            if (newPhase == GamePhase.MorningPrep)
            {
                promptText.text = "Перед сменой можно купить улучшения у стойки";
            }
            if (newPhase == GamePhase.NightPlay)
            {
                ShowTutorialOnce("night_tv", "Подойди к телевизору и посмотри запись");
            }

            RefreshShelfAvailability();
            UpdateHud();
        }

        public void StartDay()
        {
            if (save == null || phase != GamePhase.MorningPrep)
            {
                return;
            }

            upgradePanel.SetActive(false);
            save.dayStats.Reset();
            save.dayTimeRemaining = config.dayDurationSeconds;
            save.visitors.Clear();
            save.hands.Clear();
            save.visitorSpawnTimer = VisitorSpawnInterval();
            save.foreignCassetteReceivedToday = false;
            save.foreignCassetteWatchedToday = false;
            AddVisitor();
            assistantCooldown = 1.2f;
            assistantState = AssistantState.Idle;
            assistantHeldCassette = null;
            save.assistantHeldCassette = null;
            SetPhase(GamePhase.DayPlay);
            ShowTutorialOnce("day_wasd", "WASD — ходьба");
            RefreshHandsVisual();
            UpdateVisitorVisuals();
        }

        private void EndDay()
        {
            if (phase != GamePhase.DayPlay)
            {
                return;
            }

            if (save.visitors.Count > 0)
            {
                save.dayStats.failedOrders += save.visitors.Count;
                save.visitors.Clear();
            }

            ReturnHandsToPile();
            if (assistantHeldCassette != null) ReturnCassetteToPile(assistantHeldCassette);
            assistantHeldCassette = null;
            save.assistantHeldCassette = null;
            assistantReservedShelf = null;
            assistantState = AssistantState.Idle;
            inspectPanel.SetActive(false);
            shelfBrowserPanel.SetActive(false);
            save.foreignCassetteReceivedToday = false;
            save.foreignCassetteWatchedToday = false;
            SetPhase(GamePhase.DayResults);
            UpdateVisitorVisuals();
            ShowTutorialOnce("results_upgrades", "Трать деньги на улучшения у стойки перед началом дня");
            SaveNow(false);
        }

        private void ProceedToEvening()
        {
            if (phase != GamePhase.DayResults)
            {
                return;
            }

            resultsPanel.SetActive(false);
            player.position = new Vector3(0f, -2.2f, 0f);
            save.playerX = player.position.x;
            save.playerY = player.position.y;
            SetPhase(GamePhase.EveningPrep);
        }

        private void OpenMysteryDialogue()
        {
            if (phase != GamePhase.EveningPrep || save.foreignCassetteReceivedToday)
            {
                return;
            }

            dialogueText.text = "НЕЗНАКОМЕЦ:\n«Эту запись не сдавали в ваш салон. Но она всё равно знает дорогу домой. Посмотрите её сегодня — и не перематывайте.»";
            dialoguePanel.SetActive(true);
        }

        private void ReceiveForeignCassette()
        {
            if (save == null || phase != GamePhase.EveningPrep || save.foreignCassetteReceivedToday)
            {
                dialoguePanel.SetActive(false);
                return;
            }

            save.foreignCassetteReceivedToday = true;
            CassetteData foreign = CreateCassette(CassetteGenre.Action);
            foreign.isForeign = true;
            save.hands.Add(foreign);
            RefreshHandsVisual();
            dialoguePanel.SetActive(false);
            mysteryObject.SetActive(false);
            promptText.text = "Чужая кассета получена. Идите к телевизору.";
            SaveNow(false);
        }

        private void BeginNight()
        {
            if (phase != GamePhase.EveningPrep || !save.foreignCassetteReceivedToday)
            {
                return;
            }

            SetPhase(GamePhase.NightPlay);
        }

        private void InteractWithTelevision()
        {
            if (phase != GamePhase.NightPlay)
            {
                return;
            }
            if (save.foreignCassetteWatchedToday)
            {
                promptText.text = "Запись уже просмотрена. Можно уходить через дверь.";
                return;
            }
            if (!save.foreignCassetteReceivedToday)
            {
                promptText.text = "Нечего вставлять в видеомагнитофон.";
                return;
            }

            SetPhase(GamePhase.NightCutscene);
            cutscenePanel.SetActive(false);
            cutsceneContinueButton.gameObject.SetActive(false);
            if (cutsceneRoutine != null)
            {
                StopCoroutine(cutsceneRoutine);
            }
            cutsceneRoutine = StartCoroutine(PlayNightCutscene());
        }

        private IEnumerator PlayNightCutscene()
        {
            Vector3 initialPosition = gameCamera.transform.position;
            float initialSize = gameCamera.orthographicSize;
            float zoomStarted = Time.unscaledTime;
            while (Time.unscaledTime - zoomStarted < 0.75f)
            {
                float t = Mathf.SmoothStep(0, 1, (Time.unscaledTime - zoomStarted) / 0.75f);
                gameCamera.transform.position = Vector3.Lerp(initialPosition, new Vector3(2.95f, -2.1f, -10f), t);
                gameCamera.orthographicSize = Mathf.Lerp(initialSize, 1.25f, t);
                yield return null;
            }
            cutscenePanel.SetActive(true);
            string lore = config.loreFragments != null && config.loreFragments.Length > 0
                ? save.currentDayIndex <= config.loreFragments.Length ? config.loreFragments[save.currentDayIndex - 1]
                    : string.Format(config.loreAfterLastFragment, save.currentDayIndex)
                : "На экране — пустой салон. В глубине кто-то ставит кассету на полку.";
            cutsceneLoreText.text = string.Empty;
            float started = Time.unscaledTime;
            int characters = 0;
            while (characters < lore.Length)
            {
                characters = Mathf.Min(lore.Length, characters + 2);
                cutsceneLoreText.text = lore.Substring(0, characters);
                cutsceneGlitchText.text = UnityEngine.Random.value < 0.16f ? "///  ---  ///\nTRACKING" : string.Empty;
                yield return new WaitForSecondsRealtime(0.035f);
            }

            while (Time.unscaledTime - started < 3f)
            {
                yield return null;
            }
            cutsceneGlitchText.text = string.Empty;
            cutsceneContinueButton.gameObject.SetActive(true);
        }

        private void FinishNightCutscene()
        {
            if (phase != GamePhase.NightCutscene)
            {
                return;
            }

            if (cutsceneRoutine != null)
            {
                StopCoroutine(cutsceneRoutine);
                cutsceneRoutine = null;
            }
            gameCamera.transform.position = new Vector3(0f, 0f, -10f);
            save.foreignCassetteWatchedToday = true;
            save.hands.RemoveAll(cassette => cassette.isForeign);
            RefreshHandsVisual();
            cutscenePanel.SetActive(false);
            SetPhase(GamePhase.NightPlay);
            SaveNow(false);
        }

        private void InteractWithDoor()
        {
            if (phase != GamePhase.NightPlay)
            {
                return;
            }

            if (!save.foreignCassetteWatchedToday)
            {
                promptText.text = "Сначала посмотрите чужую кассету.";
                return;
            }

            save.currentDayIndex++;
            save.foreignCassetteReceivedToday = false;
            save.foreignCassetteWatchedToday = false;
            save.visitors.Clear();
            save.hands.Clear();
            player.position = new Vector3(0f, -2.7f, 0f);
            save.playerX = player.position.x;
            save.playerY = player.position.y;
            SetPhase(GamePhase.MorningPrep);
            RefreshHandsVisual();
            SaveNow(false);
        }

        private void UpdateDay(float deltaTime)
        {
            save.dayTimeRemaining -= deltaTime;
            if (save.dayTimeRemaining <= 0f)
            {
                save.dayTimeRemaining = 0f;
                EndDay();
                return;
            }

            UpdateMachine(deltaTime);
            UpdateVisitors(deltaTime);
            UpdateAssistant(deltaTime);
            float dusk = config.duskFadeDurationSeconds <= 0f ? 0f : Mathf.Clamp01((config.duskFadeDurationSeconds - save.dayTimeRemaining) / config.duskFadeDurationSeconds);
            SetAtmosphere(dusk * 0.48f);
        }

        private void UpdateMachine(float deltaTime)
        {
            save.machineTimer -= deltaTime;
            float interval = MachineInterval();
            while (save.machineTimer <= 0f)
            {
                ProduceCassette();
                save.machineTimer += interval;
            }

            if ((save.machineOverflow > 0 || save.machineStoredCassettes.Count > 0) && save.pile.Count < PileCapacity())
            {
                save.overflowDrainTimer -= deltaTime;
                if (save.overflowDrainTimer <= 0f)
                {
                    save.overflowDrainTimer = config.overflowDrainIntervalSeconds;
                    if (save.machineStoredCassettes.Count > 0)
                    {
                        save.pile.Add(save.machineStoredCassettes[0]);
                        save.machineStoredCassettes.RemoveAt(0);
                    }
                    else
                    {
                        List<CassetteGenre> genres = UnlockedGenres();
                        save.pile.Add(CreateCassette(genres[UnityEngine.Random.Range(0, genres.Count)]));
                        save.machineOverflow--;
                    }
                    RefreshPileVisual();
                }
            }
        }

        private void ProduceCassette()
        {
            if (save.pile.Count < PileCapacity())
            {
                List<CassetteGenre> genres = UnlockedGenres();
                save.pile.Add(CreateCassette(genres[UnityEngine.Random.Range(0, genres.Count)]));
                RefreshPileVisual();
            }
            else
            {
                save.machineOverflow++;
                int bucket = save.machineOverflow / 10;
                if (save.machineOverflow == 1 || bucket != lastOverflowLogBucket)
                {
                    lastOverflowLogBucket = bucket;
                    Debug.Log($"[VHS DEBUG] Куча заполнена. В аппарате накоплено: {save.machineOverflow}");
                }
            }
        }

        private void UpdateVisitors(float deltaTime)
        {
            save.visitorSpawnTimer -= deltaTime;
            if (save.visitorSpawnTimer <= 0f && save.visitors.Count < config.maxVisibleVisitors)
            {
                AddVisitor();
                save.visitorSpawnTimer = VisitorSpawnInterval();
            }

            bool changed = false;
            for (int i = save.visitors.Count - 1; i >= 0; i--)
            {
                save.visitors[i].patienceRemaining -= deltaTime;
                if (save.visitors[i].patienceRemaining <= 0f)
                {
                    FailVisitor(i, "Посетитель не дождался", true);
                    changed = true;
                }
            }

            if (changed)
            {
                UpdateVisitorVisuals();
            }
        }

        private void AddVisitor()
        {
            if (save.visitors.Count >= config.maxVisibleVisitors)
            {
                return;
            }

            int dayIndex = Mathf.Clamp(save.currentDayIndex - 1, 0, Mathf.Max(0, (config.orderMaximumByDay?.Length ?? 0) - 1));
            int maximum = config.orderMaximumByDay != null && config.orderMaximumByDay.Length > 0 ? config.orderMaximumByDay[dayIndex] : 3;
            int size = UnityEngine.Random.Range(1, Mathf.Max(2, maximum + 1));
            List<CassetteGenre> genres = UnlockedGenres();
            VisitorData visitor = new VisitorData
            {
                id = Guid.NewGuid().ToString("N"),
                patienceRemaining = VisitorPatience(),
                hasReadOrder = false,
                visualVariant = UnityEngine.Random.Range(0, 6)
            };
            for (int i = 0; i < size; i++)
            {
                int genreIndex = (int)genres[UnityEngine.Random.Range(0, genres.Count)];
                visitor.totalByGenre[genreIndex]++;
                visitor.remainingByGenre[genreIndex]++;
            }
            save.visitors.Add(visitor);
            UpdateVisitorVisuals();
        }

        private void FailVisitor(int index, string reason, bool countFailure)
        {
            if (index < 0 || index >= save.visitors.Count)
            {
                return;
            }
            if (countFailure)
            {
                save.dayStats.failedOrders++;
            }
            bool wasActive = index == 0;
            save.visitors.RemoveAt(index);
            if (wasActive)
            {
                promptText.text = reason;
                Notify(reason, false);
            }
            UpdateVisitorVisuals();
        }

        private void CompleteActiveVisitor()
        {
            if (save.visitors.Count == 0)
            {
                return;
            }

            VisitorData visitor = save.visitors[0];
            int payout = Mathf.RoundToInt(config.baseOrderPayout * visitor.TotalCount() *
                (1f + config.uniqueGenrePayoutBonus * visitor.UniqueGenreCount()) * OrderPayoutMultiplier());
            save.totalMoney += payout;
            save.dayStats.moneyEarned += payout;
            save.dayStats.completedOrders++;
            save.visitors.RemoveAt(0);
            promptText.text = $"Заказ выполнен: +${payout}";
            Notify($"Заказ выполнен  +${payout}");
            UpdateVisitorVisuals();
        }

        private void DeliverCassette(CassetteData cassette, string expectedVisitorId, bool fromAssistant = false)
        {
            if (cassette == null || save.visitors.Count == 0)
            {
                ReturnCassetteToPile(cassette);
                return;
            }

            VisitorData active = save.visitors[0];
            if (!string.IsNullOrEmpty(expectedVisitorId) && active.id != expectedVisitorId)
            {
                ReturnCassetteToPile(cassette);
                return;
            }

            int genreIndex = (int)cassette.genre;
            if (fromAssistant && (genreIndex < 0 || genreIndex >= active.remainingByGenre.Length || active.remainingByGenre[genreIndex] <= 0))
            {
                ReturnCassetteToPile(cassette);
                return;
            }
            if (cassette.isForeign || genreIndex < 0 || genreIndex >= active.remainingByGenre.Length || active.remainingByGenre[genreIndex] <= 0)
            {
                FailVisitor(0, "Неверная кассета — заказ провален", true);
                return;
            }

            active.remainingByGenre[genreIndex]--;
            if (active.RemainingCount() <= 0)
            {
                CompleteActiveVisitor();
            }
            else
            {
                promptText.text = "Кассета принята. Нужны остальные позиции.";
            }
        }

        private void UpdateVisitorVisuals()
        {
            int count = save != null ? save.visitors.Count : 0;
            for (int i = 0; i < visitorVisuals.Count; i++)
            {
                GameObject visual = visitorVisuals[i];
                bool active = i < count;
                visual.SetActive(active);
                if (!active)
                {
                    continue;
                }

                int queueIndex = i;
                float side = queueIndex == 0 ? 0f : (queueIndex % 2 == 1 ? 1f : -1f);
                float rank = queueIndex == 0 ? 0f : Mathf.Ceil(queueIndex / 2f);
                visual.transform.position = new Vector3(side * rank * 1.08f, 4.30f, 0f);
                visual.GetComponent<SpriteRenderer>().sprite = VhsSpriteFactory.Visitor(save.visitors[i].visualVariant);
            }

            bool showBubble = count > 0 && phase == GamePhase.DayPlay && !save.visitors[0].hasReadOrder &&
                Vector2.Distance(player != null ? player.position : Vector3.zero, counterPoint.position) < 2.4f;
            visitorBubble.SetActive(showBubble);
            if (showBubble)
            {
                visitorBubble.transform.position = new Vector3(0.7f, 4.25f, 0f);
            }
        }

        private void TakeFromPile()
        {
            int capacity = CarryCapacity();
            int available = capacity - save.hands.Count;
            if (available <= 0)
            {
                promptText.text = "Руки заняты.";
                return;
            }
            if (save.pile.Count == 0)
            {
                promptText.text = "Куча пока пуста.";
                return;
            }

            int take = Mathf.Min(available, save.pile.Count);
            for (int i = 0; i < take; i++)
            {
                CassetteData cassette = save.pile[0];
                save.pile.RemoveAt(0);
                save.hands.Add(cassette);
            }
            RefreshPileVisual();
            RefreshHandsVisual();
            ShowTutorialOnce("day_inspect", "[ПКМ] — рассмотреть кассету вблизи");
        }

        private void InteractWithShelf(ShelfRuntime shelf)
        {
            if (shelf == null || !ShelfUnlocked(shelf.genre))
            {
                return;
            }
            ShelfData data = GetShelfData(shelf.genre);
            if (save.hands.Count > 0)
            {
                int slotIndex = FirstEmptySlot(data);
                if (slotIndex < 0)
                {
                    promptText.text = "На полке нет свободных слотов.";
                    return;
                }
                CassetteData cassette = save.hands[0];
                save.hands.RemoveAt(0);
                bool correct = cassette.genre == shelf.genre && !cassette.isForeign;
                cassette.isMisplaced = !correct;
                data.slots[slotIndex].occupied = true;
                data.slots[slotIndex].cassette = cassette;
                if (correct)
                {
                    save.dayStats.correctlyShelved++;
                    if (!cassette.placementRewardClaimed)
                    {
                        cassette.placementRewardClaimed = true;
                        save.totalMoney += config.correctShelfReward;
                        save.dayStats.moneyEarned += config.correctShelfReward;
                        promptText.text = $"Верно: +${config.correctShelfReward}";
                        Notify($"Верный жанр  +${config.correctShelfReward}");
                    }
                    else
                    {
                        promptText.text = "Кассета возвращена на правильную полку.";
                    }
                }
                else
                {
                    save.dayStats.misplacedShelved++;
                    promptText.text = "Жанр не совпал. Кассета осталась на полке.";
                    Notify("Не тот жанр. Кассету можно забрать обратно.", false);
                }
                RefreshHandsVisual();
                RefreshShelfVisual(shelf);
            }
            else
            {
                OpenShelfBrowser(shelf);
            }
        }

        private void InteractWithCounter()
        {
            if (save.visitors.Count == 0)
            {
                promptText.text = "У прилавка никого нет.";
                return;
            }
            VisitorData active = save.visitors[0];
            if (!active.hasReadOrder)
            {
                active.hasReadOrder = true;
                UpdateVisitorVisuals();
                ShowTutorialOnce("day_delivery", "Принеси все кассеты из списка и отдай посетителю (E)");
                promptText.text = "Список заказа добавлен в HUD.";
                return;
            }
            if (save.hands.Count == 0)
            {
                promptText.text = "Возьмите нужные кассеты с полок.";
                return;
            }

            CassetteData cassette = save.hands[0];
            save.hands.RemoveAt(0);
            DeliverCassette(cassette, active.id);
            RefreshHandsVisual();
        }

        private void ReturnHandsToPile()
        {
            while (save.hands.Count > 0)
            {
                CassetteData cassette = save.hands[0];
                save.hands.RemoveAt(0);
                ReturnCassetteToPile(cassette);
            }
            RefreshHandsVisual();
            RefreshPileVisual();
        }

        private void ReturnCassetteToPile(CassetteData cassette)
        {
            if (cassette == null || cassette.isForeign)
            {
                return;
            }
            cassette.isMisplaced = false;
            if (save.pile.Count < PileCapacity())
            {
                save.pile.Add(cassette);
            }
            else
            {
                save.machineStoredCassettes.Add(cassette);
            }
        }

        private void UpdateAssistant(float deltaTime)
        {
            bool unlocked = IsUpgradeUnlocked(UpgradeType.CounterAssistant);
            assistantObject.SetActive(unlocked && phase == GamePhase.DayPlay);
            if (!unlocked)
            {
                return;
            }

            if (assistantState == AssistantState.Idle)
            {
                assistantCooldown -= deltaTime;
                if (assistantCooldown <= 0f)
                {
                    TryStartAssistantDelivery();
                }
                return;
            }

            if (MoveAssistantAlongPath(deltaTime))
            {
                if (assistantState == AssistantState.ToShelf)
                {
                    ShelfData data = assistantReservedShelf != null ? GetShelfData(assistantReservedShelf.genre) : null;
                    ShelfSlotData slot = data != null && assistantReservedSlot >= 0 && assistantReservedSlot < data.slots.Count
                        ? data.slots[assistantReservedSlot] : null;
                    if (slot == null || !slot.occupied || slot.cassette == null || save.visitors.Count == 0 || save.visitors[0].id != assistantTargetVisitorId)
                    {
                        assistantState = AssistantState.Idle;
                        assistantCooldown = 1f;
                        return;
                    }
                    assistantHeldCassette = slot.cassette;
                    save.assistantHeldCassette = assistantHeldCassette;
                    slot.occupied = false;
                    slot.cassette = null;
                    RefreshShelfVisual(assistantReservedShelf);
                    assistantState = AssistantState.ToCounter;
                    SetAssistantPath(counterPoint.position);
                }
                else if (assistantState == AssistantState.ToCounter)
                {
                    DeliverCassette(assistantHeldCassette, assistantTargetVisitorId, true);
                    assistantHeldCassette = null;
                    save.assistantHeldCassette = null;
                    assistantTargetVisitorId = null;
                    assistantState = AssistantState.Idle;
                    assistantCooldown = config.assistantActionCooldown;
                }
            }

            assistantDepthGroup.sortingOrder = 100 - Mathf.RoundToInt(assistantObject.transform.position.y * 10f);
            assistantRenderer.sortingOrder = 0;
            assistantObject.transform.Find("Shadow").GetComponent<SpriteRenderer>().sortingOrder = -1;
        }

        private void TryStartAssistantDelivery()
        {
            if (save.visitors.Count == 0)
            {
                assistantCooldown = 1f;
                return;
            }
            VisitorData visitor = save.visitors[0];
            visitor.hasReadOrder = true;
            for (int genreIndex = 0; genreIndex < visitor.remainingByGenre.Length; genreIndex++)
            {
                if (visitor.remainingByGenre[genreIndex] <= 0)
                {
                    continue;
                }
                for (int shelfIndex = 0; shelfIndex < shelves.Count; shelfIndex++)
                {
                    ShelfRuntime shelf = shelves[shelfIndex];
                    ShelfData data = GetShelfData(shelf.genre);
                    for (int slotIndex = 0; slotIndex < data.slots.Count; slotIndex++)
                    {
                        ShelfSlotData slot = data.slots[slotIndex];
                        if (!slot.occupied || slot.cassette == null || (int)slot.cassette.genre != genreIndex)
                        {
                            continue;
                        }
                        assistantTargetVisitorId = visitor.id;
                        assistantReservedShelf = shelf;
                        assistantReservedSlot = slotIndex;
                        assistantState = AssistantState.ToShelf;
                        SetAssistantPath(shelf.interactionPoint.position);
                        UpdateVisitorVisuals();
                        return;
                    }
                }
            }
            assistantCooldown = 2f;
        }

        private void SetAssistantPath(Vector2 goal)
        {
            assistantPath.Clear();
            assistantPath.AddRange(VhsGridPathfinder.FindPath(assistantObject.transform.position, goal, walkableBounds,
                config.pathGridCellSize, navigationObstacles));
            assistantPathIndex = assistantPath.Count > 1 ? 1 : 0;
            if (assistantPath.Count == 0)
            {
                if (assistantHeldCassette != null) ReturnCassetteToPile(assistantHeldCassette);
                assistantHeldCassette = null;
                save.assistantHeldCassette = null;
                assistantState = AssistantState.Idle;
                assistantCooldown = 2f;
            }
        }

        private bool MoveAssistantAlongPath(float deltaTime)
        {
            if (assistantPath.Count == 0 || assistantPathIndex >= assistantPath.Count)
            {
                return true;
            }
            Vector2 target = assistantPath[assistantPathIndex];
            assistantObject.transform.position = Vector2.MoveTowards(assistantObject.transform.position, target, config.assistantMoveSpeed * deltaTime);
            if (Vector2.Distance(assistantObject.transform.position, target) < 0.04f)
            {
                assistantPathIndex++;
            }
            return assistantPathIndex >= assistantPath.Count;
        }

        private bool HandleUpgradeToggleInput()
        {
            if (!paused && upgradePanel.activeSelf && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                upgradePanel.SetActive(false);
                return true;
            }
            return false;
        }

        private BoxCollider2D RegisterFootprint(SpriteRenderer renderer, Vector2 worldSize, Vector2 worldOffset)
        {
            var collider = renderer.gameObject.AddComponent<BoxCollider2D>();
            var scale = renderer.transform.lossyScale;
            collider.size = new Vector2(worldSize.x / Mathf.Abs(scale.x), worldSize.y / Mathf.Abs(scale.y));
            collider.offset = new Vector2(worldOffset.x / scale.x, worldOffset.y / scale.y);
            fixtureColliders.Add(collider);
            var group = renderer.gameObject.GetComponent<SortingGroup>();
            if (group == null) group = renderer.gameObject.AddComponent<SortingGroup>();
            group.sortingOrder = 100 - Mathf.RoundToInt(renderer.transform.position.y * 10);
            renderer.sortingOrder = 0;
            return collider;
        }

        private void RebuildNavigationObstacles()
        {
            Physics2D.SyncTransforms();
            navigationObstacles.Clear();
            foreach (BoxCollider2D collider in fixtureColliders)
            {
                if (!collider.enabled || !collider.gameObject.activeInHierarchy) continue;
                Bounds bounds = collider.bounds;
                navigationObstacles.Add(new Rect(bounds.min.x - 0.14f, bounds.min.y - 0.14f,
                    bounds.size.x + 0.28f, bounds.size.y + 0.28f));
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (save == null || pilePoint == null) return;
            UnityEditor.Handles.Label(pilePoint.position, $"Pile {save.pile.Count}/{PileCapacity()} · Machine {save.machineOverflow + save.machineStoredCassettes.Count}");
            foreach (ShelfRuntime shelf in shelves)
                if (shelf.root.activeInHierarchy) UnityEditor.Handles.Label(shelf.root.transform.position,
                    $"{VhsNames.Genre(shelf.genre)} {debugShelfCounts[(int)shelf.genre]}/{ShelfCapacity()}");
        }
#endif

        private void MovePlayer(float deltaTime)
        {
            if (Keyboard.current == null)
            {
                return;
            }
            Vector2 input = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed) input.y -= 1f;
            if (Keyboard.current.aKey.isPressed) input.x -= 1f;
            if (Keyboard.current.dKey.isPressed) input.x += 1f;
            if (input.sqrMagnitude <= 0f)
            {
                return;
            }
            input.Normalize();
            float speed = config.baseWalkSpeed * UpgradeValue(UpgradeType.WalkSpeed);
            Vector2 current = player.position;
            Vector2 delta = input * speed * deltaTime;
            Vector2 candidateX = new Vector2(current.x + delta.x, current.y);
            if (CanPlayerStand(candidateX)) current.x = candidateX.x;
            Vector2 candidateY = new Vector2(current.x, current.y + delta.y);
            if (CanPlayerStand(candidateY)) current.y = candidateY.y;
            player.position = current;
            if (Mathf.Abs(input.x) > 0.05f)
            {
                Vector3 scale = player.localScale;
                scale.x = Mathf.Abs(scale.x) * (input.x < 0f ? -1f : 1f);
                player.localScale = scale;
            }
            save.playerX = current.x;
            save.playerY = current.y;
            UpdateVisitorVisuals();
        }

        private bool CanPlayerStand(Vector2 point)
        {
            if (!walkableBounds.Contains(point)) return false;
            foreach (Collider2D collider in Physics2D.OverlapCircleAll(point + new Vector2(0, 0.1f), 0.23f))
            {
                if (!collider.isTrigger && collider.gameObject != player.gameObject) return false;
            }
            return true;
        }

        private void FindNearestInteraction()
        {
            nearestInteraction = null;
            float bestDistance = config.interactionRadius;
            for (int i = 0; i < interactions.Count; i++)
            {
                InteractionTarget target = interactions[i];
                if (!InteractionAvailable(target))
                {
                    continue;
                }
                float distance = Vector2.Distance(player.position, target.point.position);
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    nearestInteraction = target;
                }
            }

            interactionHalo.gameObject.SetActive(nearestInteraction != null);
            if (nearestInteraction != null) interactionHalo.transform.position = nearestInteraction.point.position;

            if (phase == GamePhase.DayPlay)
            {
                if (Vector2.Distance(player.position, pilePoint.position) <= 1.7f)
                {
                    ShowTutorialOnce("day_pile", "E — взять кассету");
                }
                for (int i = 0; i < shelves.Count; i++)
                {
                    if (ShelfUnlocked(shelves[i].genre) && Vector2.Distance(player.position, shelves[i].interactionPoint.position) <= 1.6f)
                    {
                        ShowTutorialOnce("day_shelf", "Подойди к полке нужного жанра и нажми E");
                        break;
                    }
                }
                if (save.visitors.Count > 0 && Vector2.Distance(player.position, counterPoint.position) <= 1.8f)
                {
                    ShowTutorialOnce("day_customer", "Подойди к прилавку и забери список заказа");
                }
            }
        }

        private bool InteractionAvailable(InteractionTarget target)
        {
            switch (target.kind)
            {
                case InteractionKind.Pile:
                case InteractionKind.Shelf:
                case InteractionKind.Counter:
                    return phase == GamePhase.DayPlay && (target.shelf == null || ShelfUnlocked(target.shelf.genre));
                case InteractionKind.UpgradeStand:
                    return phase == GamePhase.MorningPrep;
                case InteractionKind.MysteryPerson:
                    return phase == GamePhase.EveningPrep && !save.foreignCassetteReceivedToday;
                case InteractionKind.Television:
                    return phase == GamePhase.NightPlay;
                case InteractionKind.Door:
                    return phase == GamePhase.NightPlay;
                default:
                    return false;
            }
        }

        private void UpdateInteractionPrompt()
        {
            if (nearestInteraction == null)
            {
                promptText.text = PhaseHint();
                return;
            }
            switch (nearestInteraction.kind)
            {
                case InteractionKind.Pile:
                    promptText.text = "E — взять кассеты из кучи";
                    break;
                case InteractionKind.Shelf:
                    promptText.text = save.hands.Count > 0
                        ? $"E — поставить · Shift + E — взять · {VhsNames.Genre(nearestInteraction.shelf.genre)}"
                        : $"E — снять кассету с полки «{VhsNames.Genre(nearestInteraction.shelf.genre)}»";
                    break;
                case InteractionKind.Counter:
                    promptText.text = save.visitors.Count > 0 && !save.visitors[0].hasReadOrder
                        ? "E — забрать список заказа"
                        : "E — отдать верхнюю кассету";
                    break;
                case InteractionKind.UpgradeStand:
                    promptText.text = "E — открыть улучшения";
                    break;
                case InteractionKind.MysteryPerson:
                    promptText.text = "E — поговорить";
                    break;
                case InteractionKind.Television:
                    promptText.text = save.foreignCassetteWatchedToday ? "Запись уже просмотрена" : "E — вставить чужую кассету";
                    break;
                case InteractionKind.Door:
                    promptText.text = save.foreignCassetteWatchedToday ? "E — завершить день" : "Сначала посмотрите кассету";
                    break;
            }
        }

        private string PhaseHint()
        {
            switch (phase)
            {
                case GamePhase.MorningPrep: return "Утром доступны улучшения. Справа — начать смену.";
                case GamePhase.DayPlay: return save.hands.Count > 0 ? "ПКМ — рассмотреть верхнюю кассету" : string.Empty;
                case GamePhase.EveningPrep: return save.foreignCassetteReceivedToday ? "Идите к телевизору" : "Найдите загадочного человека";
                case GamePhase.NightPlay: return save.foreignCassetteWatchedToday ? "Идите к двери" : "Вставьте кассету в телевизор";
                default: return string.Empty;
            }
        }

        private void Interact()
        {
            if (nearestInteraction == null)
            {
                return;
            }
            switch (nearestInteraction.kind)
            {
                case InteractionKind.Pile: TakeFromPile(); break;
                case InteractionKind.Shelf: InteractWithShelf(nearestInteraction.shelf); break;
                case InteractionKind.Counter: InteractWithCounter(); break;
                case InteractionKind.UpgradeStand: OpenUpgradeMenu(); break;
                case InteractionKind.MysteryPerson: OpenMysteryDialogue(); break;
                case InteractionKind.Television: InteractWithTelevision(); break;
                case InteractionKind.Door: InteractWithDoor(); break;
            }
        }

        private void HandleInspectInput()
        {
            if (Mouse.current == null || phase != GamePhase.DayPlay || save == null || save.hands.Count == 0)
            {
                inspectPanel.SetActive(false);
                return;
            }
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                if (IsBlockingPanelOpen()) return;
                CassetteData cassette = save.hands[0];
                inspectCover.sprite = VhsSpriteFactory.Cassette(cassette.genre, cassette.isForeign);
                inspectTitle.text = cassette.isForeign ? "ЧУЖАЯ КАССЕТА" : VhsNames.Genre(cassette.genre).ToUpperInvariant();
                inspectDescription.text = CassetteClue(cassette.genre);
                inspectPanel.SetActive(true);
                ShowTutorialOnce("day_genre_clue", "Определи жанр по обложке");
            }
            if (inspectPanel.activeSelf && !Mouse.current.rightButton.isPressed)
            {
                inspectPanel.SetActive(false);
            }
        }

        private static string CassetteClue(CassetteGenre genre)
        {
            switch (genre)
            {
                case CassetteGenre.Action: return "Красные полосы, резкие диагонали и след от условного взрыва — боевик.";
                case CassetteGenre.Comedy: return "Тёплая жёлтая наклейка и нелепая улыбка на обложке — комедия.";
                case CassetteGenre.Drama: return "Фиолетовая рамка, дождь и серьёзный силуэт — драма.";
                case CassetteGenre.Audio: return "Бирюзовая волна и значок кассетного плеера — аудиокассета.";
                default: return string.Empty;
            }
        }

        private void OpenUpgradeMenu()
        {
            if (phase != GamePhase.MorningPrep)
            {
                return;
            }
            RefreshUpgradeMenu();
            upgradePanel.SetActive(true);
        }

        private void RefreshUpgradeMenu()
        {
            if (save == null)
            {
                return;
            }
            upgradeMoneyText.text = $"${save.totalMoney}";
            foreach (KeyValuePair<UpgradeType, UpgradeRowUI> pair in upgradeRows)
            {
                UpgradeTrack track = config.GetTrack(pair.Key);
                UpgradeRowUI row = pair.Value;
                if (track == null)
                {
                    row.buyButton.interactable = false;
                    continue;
                }
                int level = UpgradeLevel(pair.Key);
                int cost = track.CostForNextLevel(level);
                bool maxed = cost < 0 || level >= track.values.Count - 1;
                row.title.text = track.title;
                row.level.text = $"Ступень {level + 1}/{track.values.Count} · {track.EffectAt(level)}";
                row.effect.text = maxed ? "Максимум" : "Далее: " + track.EffectAt(level + 1);
                row.buyLabel.text = maxed ? "МАКС" : "$" + cost;
                row.buyButton.interactable = !maxed && save.totalMoney >= cost;
            }
        }

        private void PurchaseUpgrade(UpgradeType type)
        {
            if (phase != GamePhase.MorningPrep || save == null)
            {
                return;
            }
            UpgradeTrack track = config.GetTrack(type);
            int level = UpgradeLevel(type);
            int cost = track != null ? track.CostForNextLevel(level) : -1;
            if (cost < 0 || save.totalMoney < cost)
            {
                return;
            }
            save.totalMoney -= cost;
            save.upgradeLevels[(int)type] = level + 1;
            EnsureShelfData();
            RefreshShelfAvailability();
            RefreshAllShelfVisuals();
            RefreshPileVisual();
            RefreshHandsVisual();
            RefreshUpgradeMenu();
            SaveNow(false);
        }

        private void EnsureShelfData()
        {
            if (save == null)
            {
                return;
            }
            int capacity = ShelfCapacity();
            for (int genreIndex = 0; genreIndex < 4; genreIndex++)
            {
                CassetteGenre genre = (CassetteGenre)genreIndex;
                ShelfData data = null;
                for (int i = 0; i < save.shelves.Count; i++)
                {
                    if (save.shelves[i].genre == genre)
                    {
                        data = save.shelves[i];
                        break;
                    }
                }
                if (data == null)
                {
                    data = new ShelfData { genre = genre };
                    save.shelves.Add(data);
                }
                data.slots ??= new List<ShelfSlotData>();
                while (data.slots.Count < capacity)
                {
                    data.slots.Add(new ShelfSlotData());
                }
            }
        }

        private ShelfData GetShelfData(CassetteGenre genre)
        {
            EnsureShelfData();
            for (int i = 0; i < save.shelves.Count; i++)
            {
                if (save.shelves[i].genre == genre)
                {
                    return save.shelves[i];
                }
            }
            return null;
        }

        private void RefreshAllShelfVisuals()
        {
            for (int i = 0; i < shelves.Count; i++)
            {
                RefreshShelfVisual(shelves[i]);
            }
            RefreshShelfAvailability();
        }

        private void RefreshShelfVisual(ShelfRuntime shelf)
        {
            if (save == null)
            {
                return;
            }
            for (int i = shelf.slotRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(shelf.slotRoot.GetChild(i).gameObject);
            }
            ShelfData data = GetShelfData(shelf.genre);
            int occupied = 0;
            for (int i = 0; i < data.slots.Count; i++)
            {
                ShelfSlotData slot = data.slots[i];
                if (!slot.occupied || slot.cassette == null)
                {
                    continue;
                }
                occupied++;
                GameObject marker = new GameObject("Slot " + (i + 1));
                marker.transform.SetParent(shelf.slotRoot, false);
                const int columns = 10;
                int column = i % columns;
                int row = i / columns;
                int rows = Mathf.CeilToInt(data.slots.Count / (float)columns);
                int upperRows = Mathf.CeilToInt(rows / 2f);
                bool upper = row < upperRows;
                int sectionRows = upper ? upperRows : rows - upperRows;
                int sectionRow = upper ? row : row - upperRows;
                float sectionCenter = upper ? 1.12f : 0.565f;
                marker.transform.localPosition = new Vector3(-1.01f + column * (2.02f / (columns - 1)),
                    sectionCenter + ((sectionRows - 1) * 0.5f - sectionRow) * (0.32f / sectionRows), 0f);
                marker.transform.localScale = new Vector3(0.59f, 0.4f / sectionRows, 1f);
                SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
                renderer.sprite = VhsSpriteFactory.SlotMarker(slot.cassette.genre, slot.cassette.isMisplaced);
                renderer.sortingOrder = 1;
            }
            debugShelfCounts[(int)shelf.genre] = occupied;
            shelf.label.text = VhsNames.Genre(shelf.genre);
        }

        private void RefreshShelfAvailability()
        {
            for (int i = 0; i < shelves.Count; i++)
            {
                bool unlocked = save != null && ShelfUnlocked(shelves[i].genre);
                shelves[i].root.SetActive(unlocked);
                shelves[i].interactionPoint.gameObject.SetActive(unlocked);
            }
            RebuildNavigationObstacles();
        }

        private void RefreshPileVisual()
        {
            if (save == null || pileVisualRoot == null) return;
            var ids = new HashSet<string>();
            foreach (CassetteData cassette in save.pile) ids.Add(cassette.id);
            var removed = new List<string>();
            foreach (var pair in pileObjects)
                if (!ids.Contains(pair.Key)) { Destroy(pair.Value.gameObject); removed.Add(pair.Key); }
            foreach (string id in removed) pileObjects.Remove(id);
            for (int i = 0; i < save.pile.Count; i++)
            {
                CassetteData cassette = save.pile[i];
                var random = new System.Random(StableHash(cassette.id));
                int layer = i / 7;
                int column = i % 7;
                float jitter = ((float)random.NextDouble() - 0.5f) * 0.09f;
                var position = new Vector3(5.35f + (column - 3) * 0.30f + jitter + (layer % 2) * 0.10f,
                    -3.32f + layer * 0.135f + ((float)random.NextDouble() - 0.5f) * 0.06f, 0);
                if (!pileObjects.TryGetValue(cassette.id, out VhsPileCassette view))
                {
                    view = new GameObject("Cassette").AddComponent<VhsPileCassette>();
                    view.transform.SetParent(pileVisualRoot, false);
                    view.Initialize(cassette, new Vector3(6.9f, -1.75f, 0), position, i, phase == GamePhase.DayPlay);
                    pileObjects.Add(cassette.id, view);
                }
                else view.SetTarget(position, i);
                view.transform.localScale = Vector3.one * (0.37f - Mathf.Min(layer, 10) * 0.003f);
                view.transform.localRotation = Quaternion.Euler(0, 0, (float)random.NextDouble() * 30 - 15);
            }
        }

        private void RefreshHandsVisual()
        {
            if (save == null)
            {
                return;
            }
            bool hasHands = save.hands.Count > 0;
            handCassetteRenderer.gameObject.SetActive(hasHands);
            handWorldLabel.text = hasHands ? save.hands.Count.ToString() : string.Empty;
            if (hasHands)
            {
                CassetteData top = save.hands[0];
                handCassetteRenderer.sprite = VhsSpriteFactory.Cassette(top.genre, top.isForeign);
            }
        }

        private void SetAtmosphere(float amount)
        {
            amount = Mathf.Clamp01(amount);
            nightTintRenderer.color = new Color(0.035f, 0.045f, 0.15f, amount * 0.72f);
            globalLight.intensity = Mathf.Lerp(1f, 0.58f, amount);
            globalLight.color = Color.Lerp(new Color(1f, 0.96f, 0.86f), new Color(0.52f, 0.64f, 0.9f), amount);
        }

        private void UpdatePlayerDepth()
        {
            int order = 100 - Mathf.RoundToInt(player.position.y * 10f);
            playerDepthGroup.sortingOrder = order;
            playerRenderer.sortingOrder = 0;
            handCassetteRenderer.sortingOrder = 2;
            player.Find("Shadow").GetComponent<SpriteRenderer>().sortingOrder = -1;
        }

        private void PulseMachine()
        {
            if (machineRenderer != null)
            {
                float pulse = 0.9f + Mathf.Sin(Time.unscaledTime * 2.8f) * 0.08f;
                machineRenderer.color = new Color(pulse, pulse, 1f, 1f);
            }
        }

        private void UpdateHud()
        {
            if (save == null || !hudRoot.activeSelf)
            {
                return;
            }
            moneyText.text = $"${save.totalMoney}";
            phaseText.text = $"День {save.currentDayIndex}  /  {VhsNames.Phase(phase)}";
            timerText.text = phase == GamePhase.DayPlay ? FormatTime(save.dayTimeRemaining) : string.Empty;
            timerText.color = save.dayTimeRemaining < 90 ? UiPink : UiTeal;
            handsText.text = $"В руках  {save.hands.Count}/{CarryCapacity()}";
            for (int i = 0; i < inventoryIcons.Length; i++)
            {
                bool occupied = i < save.hands.Count;
                inventoryIcons[i].gameObject.SetActive(i < CarryCapacity());
                inventoryIcons[i].color = occupied ? Color.white : new Color(0.5f, 0.6f, 0.65f, 0.25f);
                if (occupied) inventoryIcons[i].sprite = VhsSpriteFactory.Cassette(save.hands[i].genre, save.hands[i].isForeign);
            }
            topBarObject.SetActive(phase != GamePhase.NightPlay && phase != GamePhase.NightCutscene);
            orderText.text = BuildOrderHudText();
            orderPanelObject.SetActive(phase == GamePhase.DayPlay);
            if (save.visitors.Count > 0)
            {
                VisitorData visitor = save.visitors[0];
                patienceFill.fillAmount = Mathf.Clamp01(visitor.patienceRemaining / VisitorPatience());
                patienceFill.color = patienceFill.fillAmount < 0.3f ? UiPink : UiTeal;
                patienceText.text = $"{Mathf.CeilToInt(visitor.patienceRemaining)} сек.  ·  В очереди {save.visitors.Count - 1}/7";
            }
            else
            {
                patienceFill.fillAmount = 0;
                patienceText.text = "Ожидаем посетителя";
            }
            if (shelfBrowserPanel.activeSelf) RefreshShelfBrowser();
            promptText.transform.parent.gameObject.SetActive(phase != GamePhase.NightCutscene && !string.IsNullOrWhiteSpace(promptText.text));
            startDayButton.gameObject.SetActive(phase == GamePhase.MorningPrep && !upgradePanel.activeSelf);
        }

        private string BuildOrderHudText()
        {
            if (phase != GamePhase.DayPlay)
            {
                return phase == GamePhase.MorningPrep ? "Подготовьтесь и начните смену" : string.Empty;
            }
            if (save.visitors.Count == 0)
            {
                return "Очередь пуста\nНовый посетитель скоро придёт";
            }
            VisitorData visitor = save.visitors[0];
            if (!visitor.hasReadOrder)
            {
                return "Посетитель ждёт.\n\nПодойдите к прилавку\nи нажмите E.";
            }
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < visitor.remainingByGenre.Length; i++)
            {
                if (visitor.totalByGenre[i] <= 0)
                {
                    continue;
                }
                builder.Append(VhsNames.Genre((CassetteGenre)i));
                builder.Append(": ");
                builder.Append(visitor.remainingByGenre[i]);
                builder.Append('/');
                builder.Append(visitor.totalByGenre[i]);
                builder.AppendLine();
            }
            return builder.ToString();
        }

        private void UpdateResultsText()
        {
            if (save == null)
            {
                return;
            }
            DayStatsData stats = save.dayStats;
            resultsText.text =
                $"Заказов выполнено:  <color=#52C8C4>{stats.completedOrders}</color>\n" +
                $"Заказов провалено:  <color=#E15A72>{stats.failedOrders}</color>\n\n" +
                $"Заработано за день:  <color=#F0A846>${stats.moneyEarned}</color>\n\n" +
                $"Кассет расставлено верно:  {stats.correctlyShelved}\n" +
                $"Кассет расставлено с ошибкой:  {stats.misplacedShelved}";
        }

        private void UpdateDebugFields()
        {
            debugPhase = phase;
            debugPileCount = save != null ? save.pile.Count : 0;
            debugMachineOverflow = save != null ? save.machineOverflow + save.machineStoredCassettes.Count : 0;
            debugQueueCount = save != null ? save.visitors.Count : 0;
            if (save != null) foreach (ShelfData shelf in save.shelves)
            {
                int count = 0;
                foreach (ShelfSlotData slot in shelf.slots) if (slot.occupied) count++;
                debugShelfCounts[(int)shelf.genre] = count;
            }
        }

        private void ShowTutorialOnce(string key, string text)
        {
            if (save == null || save.tutorialFlagsShown.Contains(key))
            {
                return;
            }
            foreach (var pending in tutorialQueue) if (pending.Key == key) return;
            tutorialQueue.Enqueue(new KeyValuePair<string, string>(key, text));
            if (!tutorialPanel.activeSelf)
            {
                ShowNextTutorialToast();
            }
        }

        private void ShowNextTutorialToast()
        {
            if (tutorialQueue.Count == 0)
            {
                tutorialPanel.SetActive(false);
                return;
            }
            var tutorial = tutorialQueue.Dequeue();
            save.tutorialFlagsShown.Add(tutorial.Key);
            tutorialText.text = tutorial.Value;
            tutorialPanel.SetActive(true);
            tutorialPanel.transform.SetParent(canvas.transform, false);
            tutorialPanel.transform.SetAsLastSibling();
            RectTransform rect = tutorialPanel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -122f);
            rect.sizeDelta = new Vector2(770f, 68f);
            tutorialHideAt = Time.unscaledTime + 4.2f;
        }

        private void UpdateTutorialToast()
        {
            if (tutorialPanel != null && tutorialPanel.activeSelf && Time.unscaledTime >= tutorialHideAt)
            {
                tutorialPanel.SetActive(false);
                ShowNextTutorialToast();
            }
        }

        private void HandleEscape()
        {
            if (shelfBrowserPanel.activeSelf)
            {
                shelfBrowserPanel.SetActive(false);
                return;
            }
            if (saveManagementPanel.activeSelf)
            {
                saveManagementPanel.SetActive(false);
                return;
            }
            if (settingsPanel.activeSelf)
            {
                settingsPanel.SetActive(false);
                return;
            }
            if (upgradePanel.activeSelf)
            {
                upgradePanel.SetActive(false);
                return;
            }
            if (dialoguePanel.activeSelf)
            {
                dialoguePanel.SetActive(false);
                return;
            }
            if (phase == GamePhase.NightCutscene || phase == GamePhase.DayResults)
            {
                return;
            }
            TogglePause();
        }

        private void TogglePause()
        {
            if (phase == GamePhase.MainMenu)
            {
                return;
            }
            paused = !paused;
            pausePanel.SetActive(paused);
            if (paused)
            {
                previousTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = previousTimeScale;
            }
        }

        private void ReturnToMainMenu()
        {
            SaveNow(false);
            paused = false;
            ShowMainMenu();
        }

        private void SaveNow(bool showFeedback)
        {
            if (save == null)
            {
                return;
            }
            if (phase != GamePhase.MainMenu)
            {
                save.phase = phase == GamePhase.NightCutscene ? GamePhase.NightPlay : phase;
            }
            save.playerX = player.position.x;
            save.playerY = player.position.y;
            VhsSaveSystem.Save(save);
            if (showFeedback)
            {
                promptText.text = "Игра сохранена.";
            }
            continueButton.interactable = true;
        }

        private void OpenSaveManagement()
        {
            bool hasSave = VhsSaveSystem.HasSave();
            if (hasSave)
            {
                VhsSaveData data = VhsSaveSystem.Load();
                DateTime modified = File.GetLastWriteTime(VhsSaveSystem.SavePath);
                saveInfoText.text = $"Основное сохранение\nДень {data?.currentDayIndex ?? 1}  ·  ${data?.totalMoney ?? 0}\n{VhsNames.Phase(data?.phase ?? GamePhase.MorningPrep)}\n\nПоследнее сохранение: {modified:g}\nПрогресс, улучшения и кассеты сохранены.";
            }
            else
            {
                saveInfoText.text = "Сохранений пока нет.\n\nНачните новую игру. Прогресс будет сохраняться автоматически между этапами дня.";
            }
            deleteSaveButton.interactable = hasSave;
            saveManagementPanel.SetActive(true);
        }

        private void DeleteSave()
        {
            VhsSaveSystem.Delete();
            continueButton.interactable = false;
            OpenSaveManagement();
        }

        private void QuitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private bool IsBlockingPanelOpen()
        {
            return paused || upgradePanel.activeSelf || dialoguePanel.activeSelf || resultsPanel.activeSelf || cutscenePanel.activeSelf ||
                   saveManagementPanel.activeSelf || settingsPanel.activeSelf || mainMenuPanel.activeSelf || shelfBrowserPanel.activeSelf;
        }

        private int UpgradeLevel(UpgradeType type)
        {
            return save == null ? 0 : config.UpgradeLevel(save.upgradeLevels, type);
        }

        private float UpgradeValue(UpgradeType type)
        {
            return config.UpgradeValue(type, UpgradeLevel(type));
        }

        private bool IsUpgradeUnlocked(UpgradeType type)
        {
            return UpgradeValue(type) > 0.5f;
        }

        private int CarryCapacity()
        {
            return Mathf.Max(1, Mathf.RoundToInt(UpgradeValue(UpgradeType.CarryCapacity)));
        }

        private int PileCapacity()
        {
            return Mathf.Max(1, Mathf.RoundToInt(UpgradeValue(UpgradeType.BasketCapacity)));
        }

        private int ShelfCapacity()
        {
            return Mathf.Max(1, Mathf.RoundToInt(UpgradeValue(UpgradeType.ShelfCapacity)));
        }

        private float VisitorPatience()
        {
            return Mathf.Max(1f, UpgradeValue(UpgradeType.VisitorPatience));
        }

        private float VisitorSpawnInterval()
        {
            return Mathf.Max(2f, config.baseVisitorSpawnInterval * UpgradeValue(UpgradeType.Signage));
        }

        private float OrderPayoutMultiplier()
        {
            return Mathf.Max(0.1f, UpgradeValue(UpgradeType.OrderPayout));
        }

        private float MachineInterval()
        {
            return Mathf.Max(config.machineMinimumIntervalSeconds,
                config.machineStartIntervalSeconds - (save.currentDayIndex - 1) * config.machineIntervalDecreasePerDay);
        }

        private bool ShelfUnlocked(CassetteGenre genre)
        {
            switch (genre)
            {
                case CassetteGenre.Action:
                case CassetteGenre.Comedy:
                    return true;
                case CassetteGenre.Drama:
                    return IsUpgradeUnlocked(UpgradeType.ExtraGenre);
                case CassetteGenre.Audio:
                    return IsUpgradeUnlocked(UpgradeType.AudioCassettes);
                default:
                    return false;
            }
        }

        private List<CassetteGenre> UnlockedGenres()
        {
            List<CassetteGenre> result = new List<CassetteGenre> { CassetteGenre.Action, CassetteGenre.Comedy };
            if (IsUpgradeUnlocked(UpgradeType.ExtraGenre)) result.Add(CassetteGenre.Drama);
            if (IsUpgradeUnlocked(UpgradeType.AudioCassettes)) result.Add(CassetteGenre.Audio);
            return result;
        }

        private static CassetteData CreateCassette(CassetteGenre genre)
        {
            return new CassetteData
            {
                id = Guid.NewGuid().ToString("N"),
                genre = genre,
                isForeign = false,
                isMisplaced = false,
                placementRewardClaimed = false
            };
        }

        private static int FirstEmptySlot(ShelfData data)
        {
            for (int i = 0; i < data.slots.Count; i++)
            {
                if (!data.slots[i].occupied)
                {
                    return i;
                }
            }
            return -1;
        }

        private static int FirstOccupiedSlot(ShelfData data)
        {
            for (int i = 0; i < data.slots.Count; i++)
            {
                if (data.slots[i].occupied && data.slots[i].cassette != null)
                {
                    return i;
                }
            }
            return -1;
        }

        private static string FormatTime(float seconds)
        {
            int value = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{value / 60:00}:{value % 60:00}";
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                int hash = 23;
                if (!string.IsNullOrEmpty(value))
                {
                    for (int i = 0; i < value.Length; i++)
                    {
                        hash = hash * 31 + value[i];
                    }
                }
                return hash;
            }
        }

        public void DebugStartNewGame()
        {
            NewGame();
        }

        public void DebugAdvancePhase()
        {
            if (save == null)
            {
                NewGame();
                return;
            }
            switch (phase)
            {
                case GamePhase.MorningPrep:
                    StartDay();
                    break;
                case GamePhase.DayPlay:
                    EndDay();
                    break;
                case GamePhase.DayResults:
                    ProceedToEvening();
                    break;
                case GamePhase.EveningPrep:
                    if (!save.foreignCassetteReceivedToday)
                    {
                        save.foreignCassetteReceivedToday = true;
                    }
                    BeginNight();
                    break;
                case GamePhase.NightPlay:
                    save.foreignCassetteWatchedToday = true;
                    InteractWithDoor();
                    break;
                case GamePhase.NightCutscene:
                    FinishNightCutscene();
                    break;
            }
        }
    }
}
