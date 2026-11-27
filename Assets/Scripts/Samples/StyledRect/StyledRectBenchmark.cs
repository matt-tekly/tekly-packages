using System;
using Tekly.Common.Ui.Styled;
using UnityEngine;
using UnityEngine.UI;

namespace TeklySample.Samples.StyledRects
{
    /// <summary>
    /// Measures how long the GPU spends drawing StyledRects, for comparing shader changes on a device.
    /// Fills the screen with a stack of cards in one of several setups and shows the average GPU and CPU
    /// frame times. The buttons change the setup and the number of stacked cards. It only uses StyledRect
    /// API that older commits have too, so the same scene can measure a build from before a change.
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

        [Tooltip("How many cards are stacked on top of each other. Each covers nearly the whole screen.")]
        [SerializeField] private int m_cardCount = 6;

        [Tooltip("How many frames each average covers.")]
        [SerializeField] private int m_sampleFrames = 120;

        [Tooltip("Frames skipped after the setup changes, while the mesh rebuilds and the GPU settles.")]
        [SerializeField] private int m_warmupFrames = 30;

        private readonly FrameTiming[] m_timings = new FrameTiming[1];

        private Scenario[] m_scenarios;
        private int m_scenarioIndex;
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

            m_scenarios = new[] {
                new Scenario("Empty", null),

                // Unity's own Image with the default UI shader, about the cheapest a UI card can be. What a
                // StyledRect costs above this is the cost of its shader.
                new Scenario("Image", BuildImage),

                new Scenario("Fill", Styled(BuildFill)),
                new Scenario("Fill, wobble", Styled((rect, index) => {
                    BuildFill(rect, index);
                    Wobble(rect, index, 0);
                })),
                new Scenario("Fill, animated wobble", Styled((rect, index) => {
                    BuildFill(rect, index);
                    Wobble(rect, index, 2);
                })),
                new Scenario("Layered", Styled(BuildLayered)),
                new Scenario("Layered, wobble", Styled((rect, index) => {
                    BuildLayered(rect, index);
                    Wobble(rect, index, 0);
                })),
                new Scenario("Layered, animated wobble", Styled((rect, index) => {
                    BuildLayered(rect, index);
                    Wobble(rect, index, 2);
                })),
                new Scenario("Dashed wobbly stroke", Styled(BuildDashedStroke))
            };

            var canvasObject = new GameObject("Benchmark Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, REFERENCE_HEIGHT);
            scaler.matchWidthOrHeight = 0.5f;

            m_root = (RectTransform)canvasObject.transform;

            Rebuild();
        }

        private void Update()
        {
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
            var area = new Rect(40f, 40f, Mathf.Min(900f, width - 80f), 520f);

            GUI.Box(area, GUIContent.none);
            GUILayout.BeginArea(new Rect(area.x + 20f, area.y + 20f, area.width - 40f, area.height - 40f));

            GUILayout.Label(m_report, m_labelStyle);
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("<", m_buttonStyle, GUILayout.Width(100f))) {
                SelectScenario(m_scenarioIndex - 1);
            }

            if (GUILayout.Button(">", m_buttonStyle, GUILayout.Width(100f))) {
                SelectScenario(m_scenarioIndex + 1);
            }

            GUILayout.Space(30f);

            if (GUILayout.Button("Cards -", m_buttonStyle, GUILayout.Width(160f))) {
                SetCardCount(m_cardCount - 1);
            }

            if (GUILayout.Button("Cards +", m_buttonStyle, GUILayout.Width(160f))) {
                SetCardCount(m_cardCount + 1);
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void SelectScenario(int index)
        {
            m_scenarioIndex = (index + m_scenarios.Length) % m_scenarios.Length;
            Rebuild();
        }

        private void SetCardCount(int count)
        {
            count = Mathf.Clamp(count, MIN_CARDS, MAX_CARDS);

            if (count == m_cardCount) {
                return;
            }

            m_cardCount = count;
            Rebuild();
        }

        private void Rebuild()
        {
            for (var i = m_root.childCount - 1; i >= 0; i--) {
                Destroy(m_root.GetChild(i).gameObject);
            }

            var scenario = m_scenarios[m_scenarioIndex];

            if (scenario.Build != null) {
                for (var i = 0; i < m_cardCount; i++) {
                    CreateCard(scenario, i);
                }
            }

            ResetSamples();
        }

        private void CreateCard(Scenario scenario, int index)
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

            m_report = $"{Header()}\n\n" +
                       $"GPU: {gpu}\n" +
                       $"CPU: {m_cpuSum / m_samples:0.00} ms\n" +
                       $"Frame: {frame:0.00} ms ({1000.0 / frame:0} fps)\n\n" +
                       $"{SystemInfo.deviceModel}, {SystemInfo.graphicsDeviceName}\n" +
                       $"{SystemInfo.graphicsDeviceType}, {Screen.width}x{Screen.height}";

            m_samples = 0;
            m_gpuSum = 0;
            m_cpuSum = 0;
            m_frameSum = 0;
        }

        private string Header()
        {
            var scenario = m_scenarios[m_scenarioIndex];
            var cards = scenario.Build != null ? $", {m_cardCount} cards" : "";
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

        private static void Wobble(StyledRect rect, int index, float speed)
        {
            rect.WobbleAmount = 8f;
            rect.WobbleLength = 80f;
            rect.WobbleSeed = index;
            rect.WobbleSpeed = speed;
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

        // The Extended shader's stroke path: dashes and the stroke's own wobble.
        private static void BuildDashedStroke(StyledRect rect, int index)
        {
            rect.AddLayer(StyledLayer.Fill(CardColor(index)));
            rect.AddLayer(StyledLayer.Stroke(6f, Color.white).WithDashes(24f, speed: 20f).WithWobble(2f, 3f, seed: index));
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
    }
}
