using System.Collections.Generic;
using GodOfWar.Core.StateMachines;
using GodOfWar.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GodOfWar.Player.Diagnostics
{
    /// <summary>
    /// 직전 동작과 현재 동작을 진행 바로 그려, 동작의 어느 구간에서 캔슬·연계가 열렸는지 화면 하단에 보여 준다.
    /// 구간은 상태가 미리 알려 주는 값이 아니라 액션 라우터가 판정에 쓰는 값을 매 프레임 관찰해 칠하므로 실제 동작과 어긋나지 않는다.
    /// <see cref="IActionProgress"/>를 구현한 상태만 막대가 생긴다. 표시 중에는 슬로 모션 배율을 바꿀 수 있다.
    /// 에디터와 개발 빌드에서만 동작한다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public sealed class ActionDebugOverlay : MonoBehaviour
    {
        private const float Margin = 8f;
        private const float HeaderHeight = 18f;
        private const float BarBlockHeight = 58f;
        private const float BarHeight = 20f;

        private static readonly float[] TimeScales = { 1f, 0.25f, 0.1f };

        private static readonly Color BackgroundColor = new Color(0f, 0f, 0f, 0.65f);
        private static readonly Color OutlineColor = new Color(1f, 1f, 1f, 0.3f);
        private static readonly Color ClosedColor = new Color32(0xB4, 0xB2, 0xA9, 0xFF);
        private static readonly Color CancelColor = new Color32(0x5D, 0xCA, 0xA5, 0xFF);
        private static readonly Color CancelTextColor = new Color32(0x08, 0x50, 0x41, 0xFF);
        private static readonly Color ComboWindowColor = new Color32(0xED, 0x93, 0xB1, 0xFF);
        private static readonly Color QueuedColor = new Color32(0x99, 0x35, 0x56, 0xFF);
        private static readonly Color BufferColor = new Color32(0x7F, 0x77, 0xDD, 0xFF);
        private static readonly Color PressColor = new Color32(0xD8, 0x5A, 0x30, 0xFF);

        [Tooltip("진행 바를 화면에 그린다. 꺼져 있어도 기록은 계속한다.")]
        [SerializeField] private bool _visible;

        [Tooltip("플레이 중 표시를 켜고 끄는 키.")]
        [SerializeField] private Key _toggleKey = Key.F1;

        [Tooltip("표시 중 슬로 모션 배율(1x → 0.25x → 0.1x)을 바꾸는 키. 표시를 끄면 1x로 돌아온다.")]
        [SerializeField] private Key _timeScaleKey = Key.F2;

        // 막대 두 개를 번갈아 써서 동작마다 새로 할당하지 않는다.
        private readonly ActionBar _barA = new ActionBar();
        private readonly ActionBar _barB = new ActionBar();
        private ActionBar _current;
        private ActionBar _previous;
        private IState _observedState;

        private PlayerController _controller;
        private IPlayerInput _input;
        private int _timeScaleIndex;
        private bool _ownsTimeScale;
        private GUIStyle _labelStyle;
        private Color _tint = Color.white;

        private void Awake()
        {
            // 에디터에서는 항상 isDebugBuild가 참이다.
            if (!Debug.isDebugBuild)
            {
                enabled = false;
                return;
            }

            _controller = GetComponent<PlayerController>();
            _input = _controller.GetRequiredComponent<IPlayerInput>();
        }

        private void OnEnable()
        {
            if (_input != null)
            {
                _input.ActionTriggered += OnActionTriggered;
            }
        }

        private void OnDisable()
        {
            if (_input != null)
            {
                _input.ActionTriggered -= OnActionTriggered;
            }

            RestoreTimeScale();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (WasPressed(keyboard, _toggleKey))
            {
                _visible = !_visible;
            }

            if (_visible && WasPressed(keyboard, _timeScaleKey))
            {
                _timeScaleIndex = (_timeScaleIndex + 1) % TimeScales.Length;
                Time.timeScale = TimeScales[_timeScaleIndex];
                _ownsTimeScale = true;
            }

            // 인스펙터로 표시를 꺼도 느린 배율이 남지 않게 한다.
            if (!_visible)
            {
                RestoreTimeScale();
            }
        }

        private void LateUpdate()
        {
            // 모든 Update가 끝난 뒤라 이번 프레임의 상태·라우터 갱신 결과를 관찰한다.
            IState state = _controller.CurrentState;
            if (state == null)
            {
                return;
            }

            if (state is IActionProgress progress)
            {
                // 경과 시간이 줄었으면 같은 상태로 재진입한 것이다 (콤보의 다음 타).
                if (state != _observedState || _current == null || progress.Elapsed < _current.Elapsed)
                {
                    _previous = _current;
                    _current = _previous == _barA ? _barB : _barA;
                    _current.Begin(progress.ActionName, progress.Duration);
                }

                var combo = state as IComboWindow;
                _current.Record(progress.Elapsed, new Sample
                {
                    IsCancelWindowOpen = state is ICancellable cancellable && cancellable.IsCancelWindowOpen,
                    IsComboWindowOpen = combo != null && combo.IsComboWindowOpen,
                    HasQueuedFollowUp = combo != null && combo.HasQueuedFollowUp,
                    HasBufferedInput = _controller.Router.HasBufferedInput
                });
            }

            _observedState = state;
        }

        private void OnGUI()
        {
            if (!_visible || Event.current.type != EventType.Repaint)
            {
                return;
            }

            _labelStyle ??= new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                clipping = TextClipping.Clip,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(0, 0, 0, 0),
                normal = { textColor = Color.white }
            };

            float height = HeaderHeight + BarBlockHeight * 2f + Margin;
            var panel = new Rect(Margin, Screen.height - Margin - height, Screen.width - Margin * 2f, height);
            Fill(panel, BackgroundColor);

            float left = panel.x + Margin;
            float width = panel.width - Margin * 2f;
            DrawHeader(new Rect(left, panel.y + 2f, width, HeaderHeight));

            float firstY = panel.y + HeaderHeight + 4f;
            if (_current == null)
            {
                Label(new Rect(left, firstY, width, 16f), "Press attack or dodge.");
                return;
            }

            // 시간 순서대로 읽히도록 직전 동작을 위에, 현재 동작을 아래에 그린다.
            if (_previous != null)
            {
                _tint = new Color(1f, 1f, 1f, 0.5f);
                DrawBar(_previous, left, firstY, width, "  (prev)", false);
                _tint = Color.white;
            }

            DrawBar(_current, left, firstY + BarBlockHeight, width, string.Empty, _observedState is IActionProgress);
        }

        private void DrawHeader(Rect rect)
        {
            ActionRouter router = _controller.Router;
            string buffered = router.HasBufferedInput ? $"   buffered: {router.BufferedAction}" : string.Empty;
            string stateName = _controller.CurrentState?.GetType().Name ?? "-";
            Label(rect, $"state: {stateName}{buffered}");

            var scaleRect = new Rect(rect.xMax - 60f, rect.y, 60f, rect.height);
            Label(scaleRect, $"x{TimeScales[_timeScaleIndex]:0.##}");
        }

        private void DrawBar(ActionBar bar, float left, float y, float width, string suffix, bool isLive)
        {
            // 한 막대의 글자는 모두 자기 블록 안에 둔다: 제목 줄 → 누름 이름 줄 → 막대 → 연계 띠.
            // 경과 시간은 커서를 따라다니면 아래 블록의 제목 줄과 겹치므로 제목 줄 오른쪽에 길이와 함께 적는다.
            Label(new Rect(left, y, width - 110f, 14f), bar.Name + suffix);
            Label(new Rect(left + width - 110f, y, 110f, 14f), $"{bar.Elapsed:0.00} / {bar.Duration:0.00}s");

            float barY = y + 28f;
            Outline(new Rect(left, barY, width, BarHeight));

            List<Segment> segments = bar.Segments;
            for (int i = 0; i < segments.Count; i++)
            {
                Segment segment = segments[i];
                float end = i + 1 < segments.Count ? segments[i + 1].Start : bar.Elapsed;
                float x0 = ToX(bar, segment.Start, left, width);
                float span = Mathf.Max(ToX(bar, end, left, width) - x0, 1f);
                Sample sample = segment.Sample;

                Fill(new Rect(x0, barY, span, BarHeight), sample.IsCancelWindowOpen ? CancelColor : ClosedColor);

                if (sample.HasBufferedInput)
                {
                    Fill(new Rect(x0, barY - 6f, span, 4f), BufferColor);
                }

                if (sample.IsComboWindowOpen || sample.HasQueuedFollowUp)
                {
                    Fill(new Rect(x0, barY + BarHeight + 3f, span, 5f),
                        sample.HasQueuedFollowUp ? QueuedColor : ComboWindowColor);
                }
            }

            // 원래 알고 싶은 답인 캔슬 시작 시각을 숫자로 적는다.
            if (bar.CancelStart >= 0f)
            {
                float x = ToX(bar, bar.CancelStart, left, width);
                Label(new Rect(x + 4f, barY, width, BarHeight), $"cancel {bar.CancelStart:0.00}s~", CancelTextColor);
            }

            foreach (Press press in bar.Presses)
            {
                float x = ToX(bar, press.Elapsed, left, width);
                Fill(new Rect(x - 1f, barY - 10f, 2f, BarHeight + 10f), PressColor);
                Label(new Rect(x + 3f, y + 14f, 80f, 12f), press.Action.ToString());
            }

            if (isLive)
            {
                float cursorX = ToX(bar, bar.Elapsed, left, width);
                Fill(new Rect(cursorX - 1f, barY - 2f, 2f, BarHeight + 12f), Color.white);
            }
        }

        private static float ToX(ActionBar bar, float elapsed, float left, float width)
        {
            return left + Mathf.Clamp01(elapsed / bar.Duration) * width;
        }

        private void Label(Rect rect, string text)
        {
            Label(rect, text, Color.white);
        }

        private void Label(Rect rect, string text, Color color)
        {
            if (rect.width <= 0f)
            {
                return;
            }

            Color previous = GUI.color;
            GUI.color = color * _tint;
            GUI.Label(rect, text, _labelStyle);
            GUI.color = previous;
        }

        private void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color * _tint;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void Outline(Rect rect)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, 1f), OutlineColor);
            Fill(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), OutlineColor);
            Fill(new Rect(rect.x, rect.y, 1f, rect.height), OutlineColor);
            Fill(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), OutlineColor);
        }

        private void RestoreTimeScale()
        {
            // 이 도구가 바꾼 배율만 되돌린다.
            if (_ownsTimeScale)
            {
                Time.timeScale = 1f;
                _ownsTimeScale = false;
            }

            _timeScaleIndex = 0;
        }

        private static bool WasPressed(Keyboard keyboard, Key key)
        {
            return keyboard != null && key != Key.None && keyboard[key].wasPressedThisFrame;
        }

        private void OnActionTriggered(PlayerActionEvent e)
        {
            // 라우터가 먼저 이 입력을 처리해 상태가 이미 바뀌었을 수 있으므로,
            // 상태를 다시 읽지 않고 누른 순간 화면에 있던 막대와 그 막대의 마지막 경과 시간에 기록한다.
            if (e.Phase == ActionPhase.Pressed && _current != null && _observedState is IActionProgress)
            {
                _current.AddPress(e.Action);
            }
        }

        private struct Sample
        {
            public bool IsCancelWindowOpen;
            public bool IsComboWindowOpen;
            public bool HasQueuedFollowUp;
            public bool HasBufferedInput;

            public bool HasSameValues(in Sample other)
            {
                return IsCancelWindowOpen == other.IsCancelWindowOpen
                    && IsComboWindowOpen == other.IsComboWindowOpen
                    && HasQueuedFollowUp == other.HasQueuedFollowUp
                    && HasBufferedInput == other.HasBufferedInput;
            }
        }

        /// <summary>경과 시간 <see cref="Start"/>부터 다음 구간 전까지 관찰된 값.</summary>
        private struct Segment
        {
            public float Start;
            public Sample Sample;
        }

        private struct Press
        {
            public float Elapsed;
            public PlayerAction Action;
        }

        /// <summary>동작 하나의 관찰 기록. 값이 바뀐 지점만 저장한다.</summary>
        private sealed class ActionBar
        {
            // 비정상적으로 긴 동작에서도 메모리가 늘지 않게 막는 상한. 정상 동작은 수십 개 안에서 끝난다.
            private const int MaxSegments = 256;
            private const int MaxPresses = 32;

            public string Name { get; private set; }
            public float Duration { get; private set; }
            public float Elapsed { get; private set; }

            /// <summary>캔슬 윈도우가 처음 열린 것을 관찰한 경과 시간. 열린 적이 없으면 음수다.</summary>
            public float CancelStart { get; private set; }

            public List<Segment> Segments { get; } = new List<Segment>(MaxSegments);
            public List<Press> Presses { get; } = new List<Press>(MaxPresses);

            public void Begin(string name, float duration)
            {
                Name = name;
                Duration = Mathf.Max(duration, 0.0001f);
                Elapsed = 0f;
                CancelStart = -1f;
                Segments.Clear();
                Presses.Clear();
            }

            public void Record(float elapsed, in Sample sample)
            {
                Elapsed = elapsed;
                if (sample.IsCancelWindowOpen && CancelStart < 0f)
                {
                    CancelStart = elapsed;
                }

                int count = Segments.Count;
                if (count > 0 && Segments[count - 1].Sample.HasSameValues(sample))
                {
                    return;
                }

                if (count < MaxSegments)
                {
                    // 첫 구간은 동작 시작부터 칠한다.
                    Segments.Add(new Segment { Start = count == 0 ? 0f : elapsed, Sample = sample });
                }
            }

            public void AddPress(PlayerAction action)
            {
                if (Presses.Count < MaxPresses)
                {
                    Presses.Add(new Press { Elapsed = Elapsed, Action = action });
                }
            }
        }
    }
}
