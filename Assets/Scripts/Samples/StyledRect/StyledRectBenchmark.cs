using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tekly.Common.Ui.Styled;
using UnityEngine;
using UnityEngine.UI;

namespace TeklySample.Samples.StyledRects
{
    /// <summary>
    /// Measures how long the GPU spends drawing StyledRects, for comparing shader changes on a device.
    /// Fills the screen with cards in one of several setups and shows the average GPU and CPU frame times.
    /// The cards are either stacked, each covering nearly the whole screen, or laid out side by side in a
    /// grid of button sized cards that do not overlap, which is closer to a real screen. The buttons change
    /// the setup, the layout and the number of cards. It only uses StyledRect API that older commits have
    /// too, so the same scene can measure a build from before a change.
    ///
    /// The cards are built once, so the CPU time is the frame's own cost and not the cards' mesh. Rebuild
    /// marks every card's mesh dirty each frame, as if all of them were animating, so the CPU time includes
    /// building their meshes and batching the canvas again. Compare it against Image cards rebuilding too.
    ///
    /// Run all measures every setup stacked and in a grid, built once and rebuilding every frame, at the
    /// current card counts. It shows the results as a table when done, logs them and writes them to
    /// styledrect_benchmark.txt in Application.persistentDataPath.
    ///
    /// The GPU time needs Frame Timing Stats on in the Player Settings. Compare builds at the same setup and
    /// card count, and with enough cards that the GPU is busy: a mobile GPU lowers its clock when it has
    /// little to do, so a light load reads slower than it is. Empty is the cost of the frame without any
    /// StyledRects, to subtract from the others.
    /// </summary>
    public class StyledRectBenchmark : MonoBehaviour
    {
        private const float CARD_MARGIN = 24f;
        private const float CARD_STEP = 14f;
        private const float CARD_RADIUS = 32f;
        private const int MIN_CARDS = 1;
        private const int MAX_CARDS = 32;
        private const float REFERENCE_HEIGHT = 1080f;
        private static readonly Vector2 GRID_CARD_SIZE = new Vector2(400f, 150f);
        private const float GRID_GAP = 24f;

        // The names of BuildLayered's layers, in the order it adds them.
        private static readonly string[] LAYERED_NAMES = { "drop shadow", "fill", "inner shadow", "bevel", "stroke" };

        [Tooltip("How many cards are stacked on top of each other. Each covers nearly the whole screen.")]
        [SerializeField] private int m_cardCount = 6;

        [Tooltip("How many cards the grid layout shows. It holds at most as many as fit on the screen.")]
        [SerializeField] private int m_gridCardCount = 32;

        [Tooltip("Lay the cards out side by side in a grid instead of stacking them.")]
        [SerializeField] private bool m_grid;

        [Tooltip("Mark every card's mesh dirty each frame, so the CPU time includes rebuilding them.")]
        [SerializeField] private bool m_rebuildEveryFrame;

        [Tooltip("How many frames each average covers.")]
        [SerializeField] private int m_sampleFrames = 120;

        [Tooltip("Frames skipped after the setup changes, while the mesh rebuilds and the GPU settles.")]
        [SerializeField] private int m_warmupFrames = 30;

        private readonly FrameTiming[] m_timings = new FrameTiming[1];
        private readonly List<Graphic> m_cards = new List<Graphic>();
        private readonly List<SweepStep> m_sweep = new List<SweepStep>();
        private readonly StringBuilder m_sweepReport = new StringBuilder();
        private int m_sweepIndex = -1;
        private string m_lastSweep;
        private Vector2 m_reportScroll;

        private Scenario[] m_scenarios;
        private int m_scenarioIndex;
        private Canvas m_canvas;
        private RectTransform m_root;

        private int m_warmup;
        private int m_samples;
        private double m_gpuSum;
        private double m_cpuSum;
        private double m_frameSum;
        private bool m_hasGpuTime;
        private string m_report = "Measuring...";

        private GUIStyle m_labelStyle;
        private GUIStyle m_buttonStyle;

        private void Awake()
        {
            // Android runs at 30 by default. Ask for the display's rate so the GPU is not left idle.
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            var scenarios = new List<Scenario> {
                new Scenario("Empty", null),

                // Unity's own Image with the default UI shader, about the cheapest a UI card can be. What a
                // StyledRect costs above this is the cost of its shader.
                new Scenario("Image", BuildImage),

                new Scenario("Fill", Styled(BuildFill)),
                new Scenario("Fill, wobble", Styled((rect, index) => {
                    BuildFill(rect, index);
                    Wobble(rect, index);
                })),
                new Scenario("Layered", Styled(BuildLayered)),
                new Scenario("Layered, wobble", Styled((rect, index) => {
                    BuildLayered(rect, index);
                    Wobble(rect, index);
                })),
                new Scenario("Dashed stroke, wobble", Styled((rect, index) => {
                    BuildDashedStroke(rect, index);
                    Wobble(rect, index);
                }))
            };

            // Layered with one layer left out at a time, to see what each layer costs.
            for (var i = 0; i < LAYERED_NAMES.Length; i++) {
                var removed = i;
                scenarios.Add(new Scenario($"Layered, no {LAYERED_NAMES[i]}", Styled((rect, index) => {
                    BuildLayered(rect, index);
                    // AddLayer puts each layer in front, so the last one added is at index 0.
                    rect.RemoveLayer(rect.LayerCount - 1 - removed);
                })));
            }

            m_scenarios = scenarios.ToArray();

            var canvasObject = new GameObject("Benchmark Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);

            m_canvas = canvasObject.GetComponent<Canvas>();
            m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, REFERENCE_HEIGHT);
            scaler.matchWidthOrHeight = 0.5f;

            m_root = (RectTransform)canvasObject.transform;

            Rebuild();
        }

        private void Update()
        {
            // Before the canvas updates later this frame, so the meshes rebuild in the frame being measured.
            if (m_rebuildEveryFrame) {
                foreach (var card in m_cards) {
                    card.SetVerticesDirty();
                }
            }

            FrameTimingManager.CaptureFrameTimings();

            if (m_warmup > 0) {
                m_warmup--;
                return;
            }

            // The timings arrive a few frames late, which does not matter for an average.
            if (FrameTimingManager.GetLatestTimings(1, m_timings) > 0) {
                m_gpuSum += m_timings[0].gpuFrameTime;
                m_cpuSum += m_timings[0].cpuFrameTime;
                m_hasGpuTime |= m_timings[0].gpuFrameTime > 0;
            }

            m_frameSum += Time.unscaledDeltaTime * 1000.0;
            m_samples++;

            if (m_samples >= m_sampleFrames) {
                UpdateReport();
            }
        }

        private void OnGUI()
        {
            EnsureStyles();

            // Sized for a 1080 high screen and scaled to the real one, so it stays readable on a tablet.
            var scale = Screen.height / REFERENCE_HEIGHT;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            var width = Screen.width / scale;
            var height = Screen.height / scale;
            var area = new Rect(40f, 40f, Mathf.Min(1400f, width - 80f), Mathf.Max(520f, height - 80f));

            GUI.Box(area, GUIContent.none);
            GUILayout.BeginArea(new Rect(area.x + 20f, area.y + 20f, area.width - 40f, area.height - 40f));

            m_reportScroll = GUILayout.BeginScrollView(m_reportScroll);
            GUILayout.Label(m_report, m_labelStyle);
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("<", m_buttonStyle, GUILayout.Width(100f))) {
                SelectScenario(m_scenarioIndex - 1);
            }

            if (GUILayout.Button(">", m_buttonStyle, GUILayout.Width(100f))) {
                SelectScenario(m_scenarioIndex + 1);
            }

            GUILayout.Space(30f);

            if (GUILayout.Button("Cards -", m_buttonStyle, GUILayout.Width(160f))) {
                SetCardCount(CardCount - 1);
            }

            if (GUILayout.Button("Cards +", m_buttonStyle, GUILayout.Width(160f))) {
                SetCardCount(CardCount + 1);
            }

            GUILayout.Space(30f);

            if (GUILayout.Button(m_grid ? "Grid" : "Stacked", m_buttonStyle, GUILayout.Width(180f))) {
                m_grid = !m_grid;
                Rebuild();
            }

            GUILayout.Space(20f);

            if (GUILayout.Button(m_rebuildEveryFrame ? "Rebuild" : "Static", m_buttonStyle, GUILayout.Width(150f))) {
                m_rebuildEveryFrame = !m_rebuildEveryFrame;
                ResetSamples();
            }

            GUILayout.Space(20f);

            if (GUILayout.Button(IsSweeping ? "Stop" : "Run all", m_buttonStyle, GUILayout.Width(160f))) {
                if (IsSweeping) {
                    m_sweepIndex = -1;
                    ResetSamples();
                } else {
                    StartSweep();
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void SelectScenario(int index)
        {
            m_scenarioIndex = (index + m_scenarios.Length) % m_scenarios.Length;
            Rebuild();
        }

        private int CardCount => m_grid ? Mathf.Min(m_gridCardCount, GridCapacity()) : m_cardCount;

        private void SetCardCount(int count)
        {
            count = Mathf.Clamp(count, MIN_CARDS, m_grid ? GridCapacity() : MAX_CARDS);

            if (count == CardCount) {
                return;
            }

            if (m_grid) {
                m_gridCardCount = count;
            } else {
                m_cardCount = count;
            }

            Rebuild();
        }

        // The canvas size in canvas units. The scaler sets the scale factor as soon as it is enabled.
        private Vector2 CanvasSize()
        {
            return new Vector2(Screen.width, Screen.height) / m_canvas.scaleFactor;
        }

        private Vector2Int GridShape()
        {
            var size = CanvasSize();
            var columns = Mathf.Max(1, Mathf.FloorToInt((size.x - GRID_GAP) / (GRID_CARD_SIZE.x + GRID_GAP)));
            var rows = Mathf.Max(1, Mathf.FloorToInt((size.y - GRID_GAP) / (GRID_CARD_SIZE.y + GRID_GAP)));
            return new Vector2Int(columns, rows);
        }

        private int GridCapacity()
        {
            var shape = GridShape();
            return shape.x * shape.y;
        }

        private void Rebuild()
        {
            for (var i = m_root.childCount - 1; i >= 0; i--) {
                Destroy(m_root.GetChild(i).gameObject);
            }

            m_cards.Clear();
            var scenario = m_scenarios[m_scenarioIndex];

            if (scenario.Build != null) {
                var count = CardCount;

                for (var i = 0; i < count; i++) {
                    var cardObject = m_grid ? CreateGridCard(scenario, i) : CreateCard(scenario, i);
                    m_cards.Add(cardObject.GetComponent<Graphic>());
                }
            }

            ResetSamples();
        }

        private GameObject CreateCard(Scenario scenario, int index)
        {
            var cardObject = new GameObject($"Card {index}", typeof(RectTransform));
            var cardTransform = (RectTransform)cardObject.transform;
            cardTransform.SetParent(m_root, false);

            // Each card steps in a little, so the edges of the stack do not all land on the same pixels.
            var inset = CARD_MARGIN + index * CARD_STEP;
            cardTransform.anchorMin = Vector2.zero;
            cardTransform.anchorMax = Vector2.one;
            cardTransform.offsetMin = new Vector2(inset, inset);
            cardTransform.offsetMax = new Vector2(-inset, -inset);

            scenario.Build(cardObject, index);
            return cardObject;
        }

        // A button sized card in a grid cell, filled from the top left. The grid is centered on the screen and
        // the gaps are wide enough that the drop shadows do not reach the next card.
        private GameObject CreateGridCard(Scenario scenario, int index)
        {
            var cardObject = new GameObject($"Card {index}", typeof(RectTransform));
            var cardTransform = (RectTransform)cardObject.transform;
            cardTransform.SetParent(m_root, false);

            var shape = GridShape();
            var step = GRID_CARD_SIZE + Vector2.one * GRID_GAP;
            var gridSize = new Vector2(shape.x, shape.y) * step - Vector2.one * GRID_GAP;
            var column = index % shape.x;
            var row = index / shape.x;

            cardTransform.anchorMin = cardTransform.anchorMax = new Vector2(0.5f, 0.5f);
            cardTransform.pivot = new Vector2(0f, 1f);
            cardTransform.sizeDelta = GRID_CARD_SIZE;
            cardTransform.anchoredPosition = new Vector2(-gridSize.x * 0.5f + column * step.x, gridSize.y * 0.5f - row * step.y);

            scenario.Build(cardObject, index);
            return cardObject;
        }

        // Adds a StyledRect card with no layers, then lets the setup add its own.
        private static Action<GameObject, int> Styled(Action<StyledRect, int> build)
        {
            return (cardObject, index) => {
                var rect = cardObject.AddComponent<StyledRect>();
                rect.raycastTarget = false;
                rect.Radius = CARD_RADIUS;

                // A new StyledRect starts with one fill.
                while (rect.LayerCount > 0) {
                    rect.RemoveLayer(0);
                }

                build(rect, index);
            };
        }

        private static void BuildImage(GameObject cardObject, int index)
        {
            var image = cardObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.color = CardColor(index);
        }

        private void ResetSamples()
        {
            m_warmup = m_warmupFrames;
            m_samples = 0;
            m_gpuSum = 0;
            m_cpuSum = 0;
            m_frameSum = 0;
            m_report = $"{Header()}\n\nMeasuring...";
        }

        private void UpdateReport()
        {
            var gpu = m_hasGpuTime ? $"{m_gpuSum / m_samples:0.00} ms" : "not available (turn on Frame Timing Stats)";
            var frame = m_frameSum / m_samples;

            if (IsSweeping) {
                AddSweepRow(m_hasGpuTime ? $"{m_gpuSum / m_samples:0.00}" : "n/a", m_cpuSum / m_samples, frame);
                return;
            }

            m_report = $"{Header()}\n\n" +
                       $"GPU: {gpu}\n" +
                       $"CPU: {m_cpuSum / m_samples:0.00} ms\n" +
                       $"Frame: {frame:0.00} ms ({1000.0 / frame:0} fps)\n\n" +
                       $"{SystemInfo.deviceModel}, {SystemInfo.graphicsDeviceName}\n" +
                       $"{SystemInfo.graphicsDeviceType}, {Screen.width}x{Screen.height}";

            if (m_lastSweep != null) {
                m_report += $"\n\n{m_lastSweep}";
            }

            m_samples = 0;
            m_gpuSum = 0;
            m_cpuSum = 0;
            m_frameSum = 0;
        }

        private bool IsSweeping => m_sweepIndex >= 0;

        // Every setup, stacked then in a grid, each built once and then rebuilding every frame.
        private void StartSweep()
        {
            m_sweep.Clear();

            foreach (var grid in new[] { false, true }) {
                for (var scenario = 0; scenario < m_scenarios.Length; scenario++) {
                    foreach (var rebuild in new[] { false, true }) {
                        // Without cards there is nothing to rebuild.
                        if (rebuild && m_scenarios[scenario].Build == null) {
                            continue;
                        }

                        m_sweep.Add(new SweepStep(scenario, grid, rebuild));
                    }
                }
            }

            m_sweepReport.Clear();
            m_sweepReport.AppendLine($"{SystemInfo.deviceModel}, {SystemInfo.graphicsDeviceName}, {SystemInfo.graphicsDeviceType}, {Screen.width}x{Screen.height}");
            m_sweepReport.AppendLine($"{m_cardCount} cards stacked, {Mathf.Min(m_gridCardCount, GridCapacity())} cards in the grid, {m_sampleFrames} frames each");
            m_sweepReport.AppendLine();
            m_sweepReport.AppendLine($"{"Layout",-8} {"Setup",-26} {"Mesh",-8} {"GPU ms",7} {"CPU ms",7} {"Frame ms",9}");

            m_sweepIndex = 0;
            ApplySweepStep();
        }

        private void ApplySweepStep()
        {
            var step = m_sweep[m_sweepIndex];
            m_scenarioIndex = step.Scenario;
            m_grid = step.Grid;
            m_rebuildEveryFrame = step.Rebuild;
            Rebuild();
            m_report = $"Running {m_sweepIndex + 1}/{m_sweep.Count}: {Header()}\n\n{m_sweepReport}";
        }

        private void AddSweepRow(string gpu, double cpu, double frame)
        {
            var step = m_sweep[m_sweepIndex];
            var layout = step.Grid ? "Grid" : "Stacked";
            var mesh = step.Rebuild ? "Rebuild" : "Static";
            m_sweepReport.AppendLine($"{layout,-8} {m_scenarios[step.Scenario].Name,-26} {mesh,-8} {gpu,7} {cpu,7:0.00} {frame,9:0.00}");

            m_sweepIndex++;

            if (m_sweepIndex < m_sweep.Count) {
                ApplySweepStep();
                return;
            }

            m_sweepIndex = -1;
            var report = m_sweepReport.ToString();
            var path = Path.Combine(Application.persistentDataPath, "styledrect_benchmark.txt");

            try {
                File.WriteAllText(path, report);
            } catch (IOException e) {
                Debug.LogWarning($"Could not write the benchmark report to [{path}]: [{e.Message}]");
            }

            Debug.Log($"StyledRect benchmark, written to [{path}]\n{report}");
            m_lastSweep = $"Last run, written to {path}\n{report}";
            m_report = m_lastSweep;
            m_samples = 0;
        }

        private string Header()
        {
            var scenario = m_scenarios[m_scenarioIndex];
            var layout = m_grid ? "grid" : "stacked";
            var rebuild = m_rebuildEveryFrame ? ", rebuilding every frame" : "";
            var cards = scenario.Build != null ? $", {CardCount} cards {layout}{rebuild}" : "";
            return $"[{m_scenarioIndex + 1}/{m_scenarios.Length}] {scenario.Name}{cards}";
        }

        private void EnsureStyles()
        {
            if (m_labelStyle != null) {
                return;
            }

            m_labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, wordWrap = true };
            m_labelStyle.normal.textColor = Color.white;
            m_buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 30, fixedHeight = 80f };
        }

        private static Color CardColor(int index)
        {
            return Color.HSVToRGB(index * 0.13f % 1f, 0.45f, 0.85f);
        }

        private static StyledPaint CardPaint(int index)
        {
            var color = CardColor(index);
            var darker = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f);
            return StyledPaint.Linear(color, darker, 160f);
        }

        private static void Wobble(StyledRect rect, int index)
        {
            rect.WobbleAmount = 8f;
            rect.WobbleLength = 80f;
            rect.WobbleSeed = index;
        }

        private static void BuildFill(StyledRect rect, int index)
        {
            rect.AddLayer(StyledLayer.Fill(CardPaint(index)));
        }

        // A typical game card: a drop shadow, a gradient body, an inner shadow, a bevel and an outline.
        // AddLayer puts each layer in front of the last.
        private static void BuildLayered(StyledRect rect, int index)
        {
            rect.AddLayer(StyledLayer.DropShadow(new Vector2(0f, -8f), 24f, new Color(0f, 0f, 0f, 0.4f)));
            rect.AddLayer(StyledLayer.Fill(CardPaint(index)));
            rect.AddLayer(StyledLayer.InnerShadow(new Vector2(0f, -4f), 16f, new Color(0f, 0f, 0f, 0.3f)));
            rect.AddLayer(StyledLayer.Bevel(4f, 4f, new Color(1f, 1f, 1f, 0.5f), new Color(0f, 0f, 0f, 0.3f)));
            rect.AddLayer(StyledLayer.Stroke(4f, new Color(0.1f, 0.1f, 0.15f)));
        }

        // The Extended shader's stroke path: moving dashes.
        private static void BuildDashedStroke(StyledRect rect, int index)
        {
            rect.AddLayer(StyledLayer.Fill(CardColor(index)));
            rect.AddLayer(StyledLayer.Stroke(6f, Color.white).WithDashes(24f, speed: 20f));
        }

        private readonly struct Scenario
        {
            public readonly string Name;
            public readonly Action<GameObject, int> Build;

            public Scenario(string name, Action<GameObject, int> build)
            {
                Name = name;
                Build = build;
            }
        }

        private readonly struct SweepStep
        {
            public readonly int Scenario;
            public readonly bool Grid;
            public readonly bool Rebuild;

            public SweepStep(int scenario, bool grid, bool rebuild)
            {
                Scenario = scenario;
                Grid = grid;
                Rebuild = rebuild;
            }
        }
    }
}
