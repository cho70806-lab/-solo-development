using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlatformerPlayer))]
public class PlayerAnimator : MonoBehaviour
{
    public enum State { Idle, Run, Jump, Fall, Dash, Attack, Hurt, Death }

    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Resources 폴더 안의 스프라이트 시트 이름 (확장자 제외)")]
    [SerializeField] private string resourcePath = "player_sheet";

    [Header("재생 속도 (초당 프레임)")]
    [SerializeField] private float idleFps = 6f;
    [SerializeField] private float runFps = 12f;
    [SerializeField] private float dashFps = 16f;
    [SerializeField] private float attackFps = 16f;
    [SerializeField] private float deathFps = 4f;

    private class Clip
    {
        public Sprite[] frames;
        public float fps;
        public bool loop;
    }

    private readonly Dictionary<State, Clip> clips = new Dictionary<State, Clip>();

    private PlatformerPlayer player;
    private PlayerAttack attack;
    private PlayerHealth health;

    private State current = State.Idle;
    private int frameIndex;
    private float timer;
    private bool finished;
    private bool loaded;

    public State Current => current;
    public bool IsFinished => finished;
    public int FrameIndex => frameIndex;

    // 프레임이 바뀔 때마다 호출 (공격 판정 타이밍용)
    public event Action<int> FrameChanged;

    void Awake()
    {
        player = GetComponent<PlatformerPlayer>();
        attack = GetComponent<PlayerAttack>();
        health = GetComponent<PlayerHealth>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        loaded = LoadClips();
        if (loaded) Play(State.Idle, true);
    }

    private bool LoadClips()
    {
        Sprite[] all = Resources.LoadAll<Sprite>(resourcePath);
        if (all.Length == 0)
        {
            Debug.LogError($"Resources/{resourcePath} 에서 스프라이트를 찾지 못했습니다. 시트가 Resources 폴더에 있고 Multiple로 잘렸는지 확인하세요.");
            return false;
        }

        // 이름 끝의 숫자(player_sheet_12 → 12) 순서로 정렬
        Array.Sort(all, (a, b) => SpriteNumber(a).CompareTo(SpriteNumber(b)));

        if (all.Length != 24)
        {
            Debug.LogError($"스프라이트가 {all.Length}개입니다. 24개여야 해요. Sprite Editor의 Slice에서 'Keep Empty Rects'를 끄고 다시 자르세요.");
            return false;
        }

        // 시트 순서: idle4, run6, jump1, fall1, dash2, attack6, hurt1, death3
        int i = 0;
        clips[State.Idle] = MakeClip(all, ref i, 4, idleFps, true);
        clips[State.Run] = MakeClip(all, ref i, 6, runFps, true);
        clips[State.Jump] = MakeClip(all, ref i, 1, 1f, false);
        clips[State.Fall] = MakeClip(all, ref i, 1, 1f, false);
        clips[State.Dash] = MakeClip(all, ref i, 2, dashFps, true);
        clips[State.Attack] = MakeClip(all, ref i, 6, attackFps, false);
        clips[State.Hurt] = MakeClip(all, ref i, 1, 1f, false);
        clips[State.Death] = MakeClip(all, ref i, 3, deathFps, false);
        return true;
    }

    private static Clip MakeClip(Sprite[] all, ref int start, int count, float fps, bool loop)
    {
        var frames = new Sprite[count];
        Array.Copy(all, start, frames, 0, count);
        start += count;
        return new Clip { frames = frames, fps = fps, loop = loop };
    }

    private static int SpriteNumber(Sprite s)
    {
        int idx = s.name.LastIndexOf('_');
        if (idx >= 0 && int.TryParse(s.name.Substring(idx + 1), out int n)) return n;
        return 0;
    }

    void Update()
    {
        if (!loaded) return;

        Play(DecideState());
        Tick();
    }

    private State DecideState()
    {
        if (health != null && health.IsDead) return State.Death;
        if (player.IsKnockedBack) return State.Hurt;
        if (player.IsDashing) return State.Dash;
        if (attack != null && attack.IsAttacking) return State.Attack;

        if (!player.IsGrounded)
            return player.Velocity.y > 0.1f ? State.Jump : State.Fall;

        return Mathf.Abs(player.Velocity.x) > 0.1f ? State.Run : State.Idle;
    }

    public void Play(State state, bool restart = false)
    {
        if (!loaded) return;
        if (state == current && !restart) return;

        current = state;
        frameIndex = 0;
        timer = 0f;
        finished = false;
        ApplyFrame();
    }

    private void Tick()
    {
        Clip clip = clips[current];
        if (finished || clip.frames.Length <= 1 && !clip.loop) return;

        timer += Time.deltaTime;
        float frameTime = 1f / clip.fps;

        while (timer >= frameTime && !finished)
        {
            timer -= frameTime;
            frameIndex++;

            if (frameIndex >= clip.frames.Length)
            {
                if (clip.loop)
                {
                    frameIndex = 0;
                }
                else
                {
                    frameIndex = clip.frames.Length - 1;
                    finished = true;
                    return;
                }
            }
            ApplyFrame();
        }
    }

    private void ApplyFrame()
    {
        spriteRenderer.sprite = clips[current].frames[frameIndex];
        FrameChanged?.Invoke(frameIndex);
    }
}