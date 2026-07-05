using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 课堂场景管理器 - 优化版
/// 改进点：
///   1. 时间事件注册表替换 while(true) 轮询
///   2. 协程事件队列替换回调嵌套链
///   3. TeacherType 枚举 + TeacherProfile 数据驱动替换重复 if-else
///   4. StatDelta 结构体替换具名参数歧义
///   5. DialogueManager 扩展方法简化重复模板代码
/// </summary>
public class ClassroomSceneManager : MonoBehaviour
{
    // ====================================================================
    // 数据结构
    // ====================================================================

    public enum TeacherType { Harsh = 0, Gentle = 1, Smiling = 2 }

    /// <summary>老师对各类事件的反应数据</summary>
    [System.Serializable]
    public struct TeacherReaction
    {
        public string text;
        public StatDelta delta;
    }

    /// <summary>属性变化量，替代容易数错的具名参数</summary>
    public struct StatDelta
    {
        public float mood, academic, social, health;
        public StatDelta(float mood = 0, float academic = 0, float social = 0, float health = 0)
        {
            this.mood = mood; this.academic = academic;
            this.social = social; this.health = health;
        }
    }

    // ====================================================================
    // Inspector
    // ====================================================================

    [Header("UI")]
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI statusText;

    [Header("时间速度")]
    public float normalSpeed = 60f;
    public float fastSpeed = 3f;

    [Header("老师类型（0=刻薄 1=温和 2=笑面虎）")]
    public TeacherType teacherType = TeacherType.Harsh;

    // ====================================================================
    // 老师反应数据表（代替三处重复 if-else）
    // ====================================================================

    // [teacherType][场景] → TeacherReaction
    // 场景索引：0=睡觉被发现  1=回答错误  2=补签拒绝  3=手机响
    private static readonly TeacherReaction[,] TeacherReactions = new TeacherReaction[3, 4]
    {
        // Harsh
        {
            new TeacherReaction { text = "【刻薄老师】同学你醒醒吧！这个年纪你怎么睡得着的？\n【自己】……我服了。",                                    delta = new StatDelta(mood: -5) },
            new TeacherReaction { text = "【刻薄老师】{0}完全没听我讲的课吧！坐下！\n【自己】……肯定不止我一个人不会吧。好烦啊。",                  delta = new StatDelta(mood: -5) },
            new TeacherReaction { text = "【刻薄老师】怎么会没签上到呢，我跟你说……(说教一番)……这次给你签了下次不管了。\n【自己】！！谢谢老师！", delta = new StatDelta(mood: 5, academic: 5) },
            new TeacherReaction { text = "【刻薄老师】有些同学，不好好听课就算了，手机也不静音，影响课堂秩序！\n【自己】。这老师有完没完。",         delta = new StatDelta(mood: -10) },
        },
        // Gentle
        {
            new TeacherReaction { text = "【温和老师】那位同学快醒醒呀…咱们坚持一下好不好呀。\n【自己】…唔…好的老师。",                             delta = new StatDelta(mood: 5) },
            new TeacherReaction { text = "【温和老师】不对哦。那我再讲一遍你要认真听，坐下吧。\n【自己】老师人真好啊…",                             delta = new StatDelta(mood: 5) },
            new TeacherReaction { text = "【温和老师】好哦，下次注意不要错过了哦。\n【自己】谢谢老师！",                                             delta = new StatDelta(mood: 5, academic: 5) },
            new TeacherReaction { text = "好多同学向你投来目光…\n【自己】好尴尬啊。这里是地狱吗。",                                                  delta = new StatDelta(mood: -5) },
        },
        // Smiling
        {
            new TeacherReaction { text = "【笑面虎老师】怎么睡着了？看来很辛苦呢，站一会儿吧。\n(罚站10分钟)\n【自己】……腿好酸，这是惩罚吧。",   delta = new StatDelta(mood: -5, health: -5) },
            new TeacherReaction { text = "【笑面虎老师】{0}那我再讲一遍好了。你站着听。\n(被罚站)\n【自己】……腿好酸，这老师咋这样。",            delta = new StatDelta(mood: -5, health: -5) },
            new TeacherReaction { text = "【笑面虎老师】可是你也没办法证明你这节课真的有来呀同学，不可以补签哦。\n【自己】我……好吧，谢谢老师。",  delta = new StatDelta(mood: -5, academic: -5) },
            new TeacherReaction { text = "好多同学向你投来目光…\n【自己】好尴尬啊。这里是地狱吗。",                                                  delta = new StatDelta(mood: -5) },
        },
    };

    // ====================================================================
    // 运行时状态
    // ====================================================================

    private float arrivalTime = 0f;
    private int seatLocation = 0;   // 0=前排 1=中间 2=后排
    private bool agreedToHelpSignIn = false;
    private bool missedCheckIn = false;
    private bool isPlayingPhone = false;
    private bool isSleeping = false;

    // 时间事件注册表（小时阈值 + 处理函数）
    private struct HourEvent
    {
        public float hour;
        public System.Func<IEnumerator> handler;
        public bool triggered;
    }
    private HourEvent[] _hourEvents;

    // ====================================================================
    // 生命周期
    // ====================================================================

    void Awake()
    {
        // 注册时间桩——只需在这一处维护触发时刻
        _hourEvents = new HourEvent[]
        {
            new HourEvent { hour = 8.0f,  handler = Event_ArrivalAndSeat     },
            new HourEvent { hour = 9.0f,  handler = Event_9AM_ActionChoice   },
            new HourEvent { hour = 10.0f, handler = Event_10AM_TeacherQuestion},
            new HourEvent { hour = 11.0f, handler = Event_11AM_PhoneRing     },
            new HourEvent { hour = 11.9f, handler = Event_12PM_MakeUpCheckIn },
        };
    }

    void Start()
    {
        TimeManager.OnTimeChanged += OnTimeUpdate;
        TimeManager.OnNoon += OnNoon;
        SetStatus("准备上课...");
        StartCoroutine(TimeDispatchLoop());
    }

    void OnDestroy()
    {
        TimeManager.OnTimeChanged -= OnTimeUpdate;
        TimeManager.OnNoon -= OnNoon;
        SetTimeNormal();
    }

    // ====================================================================
    // 时间驱动主循环（只做调度，不含逻辑）
    // ====================================================================

    void OnTimeUpdate(float hour)
    {
        if (timeText != null)
            timeText.text = TimeManager.Instance.GetFormattedTime();
    }

    void OnNoon()
    {
        StopAllCoroutines();
        SetTimeNormal();
        StartCoroutine(EndClass());
    }

    IEnumerator EndClass()
    {
        SetStatus("下课啦！");
        yield return new WaitForSeconds(1f);
        GameManager.Instance?.TransitionToNoonPhase();
    }

    IEnumerator TimeDispatchLoop()
    {
        // 等到进入上课时段
        yield return new WaitUntil(() => TimeManager.Instance.gameHour >= 8.0f);
        SetStatus("上课中...");

        while (true)
        {
            float hour = TimeManager.Instance.gameHour;
            bool dialogueActive = DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;

            // 对话期间暂停时间
            if (dialogueActive)
            {
                SetTimeNormal();
            }
            else
            {
                SetTimeFast();

                // 检查时间桩事件
                for (int i = 0; i < _hourEvents.Length; i++)
                {
                    if (!_hourEvents[i].triggered && hour >= _hourEvents[i].hour)
                    {
                        _hourEvents[i].triggered = true;
                        // 使用局部变量捕获，防止闭包问题
                        var handler = _hourEvents[i].handler;
                        StartCoroutine(handler());
                        break; // 每帧只触发一个，下帧继续检查
                    }
                }
            }

            yield return null;
        }
    }

    // ====================================================================
    // 事件：8:00 选座 + 课前连环
    // ====================================================================

    IEnumerator Event_ArrivalAndSeat()
    {
        arrivalTime = Random.Range(7.66f, 8.08f);

        if (arrivalTime < 7.75f)
        {
            yield return WaitChoice(
                "今天来的可真早啊，都没什么人。\n要坐哪里呢？", "自己",
                Choice("前排", () => { seatLocation = 0; }),
                Choice("中间", () => { seatLocation = 1; }),
                Choice("后排", () => { seatLocation = 2; })
            );
            string[] seatMsg = { "今天坐前面好了，积极上课一次", "坐中间吧，前后都有人很有安全感", "世界上最夯的宝座，没有不选的义务！" };
            yield return ShowMsg(seatMsg[seatLocation], "自己(平静)");
        }
        else if (arrivalTime < 7.91f)
        {
            yield return WaitChoice(
                "啊……今天来晚了点，后排已经没位置了呢。\n要坐哪里呢？", "自己(不高兴)",
                Choice("前排", () => { seatLocation = 0; }),
                Choice("中间", () => { seatLocation = 1; })
            );
            string[] seatMsg = { "坐前排好了，就积极这一次", "那就选中间位置吧…能靠后一点是一点" };
            yield return ShowMsg(seatMsg[seatLocation], "自己(平静)");
        }
        else
        {
            seatLocation = 0;
            yield return ShowMsg("……迟到了，只剩前排可坐了。", "自己(不高兴)");
        }

        // 课前连环事件队列
        yield return Event_HelpSignIn();
        yield return Event_Stomachache();
    }

    // ====================================================================
    // 事件：帮签到
    // ====================================================================

    IEnumerator Event_HelpSignIn()
    {
        bool isLate = arrivalTime >= 7.91f;
        if (isLate || Random.Range(0, 100) >= 30) yield break;

        int choice = -1;
        yield return WaitChoice(
            "手机叮咚一声：\n【同学】在吗在吗能帮我签下到吗，今天有点事到不了", "系统",
            Choice("答应", () => choice = 0),
            Choice("婉拒", () => choice = 1),
            Choice("严词拒绝", () => choice = 2)
        );

        switch (choice)
        {
            case 0:
                agreedToHelpSignIn = true;
                ApplyStats(new StatDelta(mood: 5, social: 5));
                yield return ShowMsg("【微信】可以呀，如果有签到我跟你说，你记得看消息就好。\n(同学：谢谢！我到时候请你吃零食)", "系统");
                break;
            case 1:
                ApplyStats(new StatDelta(mood: 2, social: -2));
                yield return ShowMsg("【微信】今天有点不方便啊不好意思，你找别人吧。\n(同学：好吧)", "系统");
                break;
            case 2:
                ApplyStats(new StatDelta(mood: 5, social: -10));
                yield return ShowMsg("【微信】不行，你有事来不了关我啥事。你自求多福吧。\n(同学：。彳亍)", "系统");
                break;
        }
    }

    // ====================================================================
    // 事件：肚子疼
    // ====================================================================

    IEnumerator Event_Stomachache()
    {
        float health = PlayerStats.Instance != null ? PlayerStats.Instance.health : 100f;
        int triggerProb = health >= 50f ? 25 : 40;
        if (Random.Range(0, 100) >= triggerProb) yield break;

        yield return ShowMsg("嘶……肚子怎么突然这么痛。", "自己(震惊)");

        // 如厕耗时：健康 >= 50 时 75% 快，否则 25% 快
        bool backInTime = Random.Range(0, 100) < (health >= 50f ? 75 : 25);

        if (backInTime)
        {
            yield return ShowMsg("【5 minutes later】\n还好还好，还没上课呢。", "自己(微笑)");
            yield break;
        }

        // 错过了时间
        missedCheckIn = true;

        // 询问邻座
        bool asked = false;
        yield return WaitChoice(
            "【35 minutes later】\n到教室这么早居然还是迟到了。好烦啊。也不知道有没有错过签到。", "自己",
            Choice("询问邻座同学", () => asked = true)
        );

        if (Random.Range(0, 100) < 50)
        {
            missedCheckIn = false;
            yield return ShowMsg("【邻座同学】没有签到呢。\n【自己】太好了没有错过签到！", "系统");
        }
        else
        {
            yield return ShowMsg("【邻座同学】签到了。\n【自己】。。。只能下课去补签了。", "系统");
        }

        // 如果还答应了帮别人签到
        if (missedCheckIn && agreedToHelpSignIn)
        {
            yield return WaitChoice(
                "答应帮别人签到的事也泡汤了。跟ta说一声吧。", "自己",
                Choice("道歉", () => { })
            );

            float social = PlayerStats.Instance != null ? PlayerStats.Instance.social : 50f;
            bool forgive = social >= 50f ? (Random.Range(0, 100) < 75) : (Random.Range(0, 100) < 25);

            if (forgive)
            {
                ApplyStats(new StatDelta(mood: -5, social: -5));
                yield return ShowMsg("【同学】这样…没事没事你也不是故意的嘛。\n【自己】哎……好命苦。", "系统");
            }
            else
            {
                ApplyStats(new StatDelta(mood: -10, social: -10));
                yield return ShowMsg("【同学】你就是找借口不想帮我签到吧？那你答应啥呢装货。\n【自己】？咋骂这么狠。算了懒得和ta掰扯。", "系统");
            }
        }
    }

    // ====================================================================
    // 事件：9:00 课中选择
    // ====================================================================

    IEnumerator Event_9AM_ActionChoice()
    {
        int choice = -1;
        yield return WaitChoice(
            "好无聊啊，要干嘛呢？", "自己(不高兴)",
            Choice("玩手机", () => choice = 0),
            Choice("好好听课", () => choice = 1),
            Choice("睡觉", () => choice = 2)
        );

        switch (choice)
        {
            case 0:
                isPlayingPhone = true;
                ApplyStats(new StatDelta(mood: 5, academic: -5));
                yield return ShowMsg("反正也听不进去，那就玩手机吧，起码开心呢！", "自己(微笑)");
                break;
            case 1:
                ApplyStats(new StatDelta(academic: 5));
                yield return ShowMsg("哎。来都来了还是好好听听课吧。", "自己(平静)");
                break;
            case 2:
                isSleeping = true;
                yield return Event_SleepCaught();
                break;
        }
    }

    IEnumerator Event_SleepCaught()
    {
        // 刻薄老师 100% 发现
        bool caught = teacherType == TeacherType.Harsh || Random.Range(0, 100) < 50;

        if (!caught)
        {
            ApplyStats(new StatDelta(health: 5));
            yield return ShowMsg("…………眼睛闭上真舒服啊。这里是天堂吗……\n(安稳睡到了下课，体力恢复了)", "系统");
            yield break;
        }

        isSleeping = false;
        var reaction = TeacherReactions[(int)teacherType, 0];
        ApplyStats(reaction.delta);
        yield return ShowMsg(reaction.text, "系统");
    }

    // ====================================================================
    // 事件：10:00 被提问
    // ====================================================================

    IEnumerator Event_10AM_TeacherQuestion()
    {
        int[] probBySeat = { 35, 25, 5 };
        if (Random.Range(0, 100) > probBySeat[seatLocation]) yield break;

        int choice = -1;
        yield return WaitChoice(
            "【老师】这道题有点意思，同学你来回答一下吧。\n【自己】？我吗...", "系统",
            Choice("选 A/B 碰运气", () => choice = 0),
            Choice("老师我不会", () => choice = 1)
        );

        if (choice == 0)
        {
            bool distracted = isPlayingPhone || isSleeping;
            bool correct = Random.Range(0, 100) < (distracted ? 25 : 75);

            if (correct)
            {
                ApplyStats(new StatDelta(mood: 5));
                yield return ShowMsg("【老师】回答对了，坐下吧。\n【自己】呼。答对了真是太好了！", "系统");
                yield break;
            }
        }

        // 回答错误或说不会
        string scoldPrefix = choice == 0
            ? "这刚刚才讲过一样的题，怎么会做错呢？"
            : "这刚刚才讲过一样的题，怎么会不会呢？";

        var reaction = TeacherReactions[(int)teacherType, 1];
        ApplyStats(reaction.delta);
        yield return ShowMsg(string.Format(reaction.text, scoldPrefix), "系统");
    }

    // ====================================================================
    // 事件：11:00 手机突然响
    // ====================================================================

    IEnumerator Event_11AM_PhoneRing()
    {
        if (Random.Range(0, 100) > 20) yield break;

        int rand = Random.Range(0, 100);
        string eventMsg = "";

        if (rand < 33) eventMsg = "！！！闹钟怎么响了！(疯狂点击关掉)";
        else if (rand < 66) eventMsg = "！！！我服了谁这个时候打电话！(疯狂点击挂断)";
        else if (isPlayingPhone) eventMsg = "！！！蓝牙怎么突然断开了，视频外放了！(疯狂点击关机)";
        else yield break;

        yield return WaitChoice(eventMsg, "自己(震惊)", Choice("关掉手机", () => { }));

        var reaction = TeacherReactions[(int)teacherType, 3];
        ApplyStats(reaction.delta);
        yield return ShowMsg(reaction.text, "系统");
    }

    // ====================================================================
    // 事件：11:50 课后补签
    // ====================================================================

    IEnumerator Event_12PM_MakeUpCheckIn()
    {
        if (!missedCheckIn) yield break;

        yield return WaitChoice(
            "下课了。去找老师补签到吧...\n【自己】老师我刚刚没签上到，可以帮我补签一下吗？", "系统",
            Choice("硬着头皮去", () => { })
        );

        var reaction = TeacherReactions[(int)teacherType, 2];
        ApplyStats(reaction.delta);
        yield return ShowMsg(reaction.text, "系统");
    }

    // ====================================================================
    // 辅助：属性变化
    // ====================================================================

    void ApplyStats(StatDelta d)
    {
        if (PlayerStats.Instance == null) return;
        if (d.mood != 0) PlayerStats.Instance.ChangeMood(d.mood);
        if (d.academic != 0) PlayerStats.Instance.academic = Mathf.Clamp(PlayerStats.Instance.academic + d.academic, 0f, 100f);
        if (d.social != 0) PlayerStats.Instance.social = Mathf.Clamp(PlayerStats.Instance.social + d.social, 0f, 100f);
        if (d.health != 0) PlayerStats.Instance.health = Mathf.Clamp(PlayerStats.Instance.health + d.health, 0f, 100f);
    }

    // ====================================================================
    // 辅助：对话快捷方法（消除重复模板代码）
    // ====================================================================

    /// <summary>显示一条纯叙事文本，玩家点「继续」后协程恢复</summary>
    IEnumerator ShowMsg(string text, string speaker)
    {
        bool done = false;
        DialogueManager.Instance.ShowWithChoices(
            text,
            new List<DialogueChoice> { new DialogueChoice { label = "继续", onChoose = () => done = true } },
            speaker
        );
        yield return new WaitUntil(() => done);
    }

    /// <summary>显示带选项的对话，阻塞直到玩家做出选择</summary>
    IEnumerator WaitChoice(string text, string speaker, params DialogueChoice[] choices)
    {
        bool done = false;
        // 包装每个 choice，在原有 onChoose 执行后设置 done
        var wrapped = new List<DialogueChoice>();
        foreach (var c in choices)
        {
            var captured = c;
            wrapped.Add(new DialogueChoice
            {
                label = captured.label,
                onChoose = () => { captured.onChoose?.Invoke(); done = true; }
            });
        }
        DialogueManager.Instance.ShowWithChoices(text, wrapped, speaker);
        yield return new WaitUntil(() => done);
    }

    /// <summary>快速构造 DialogueChoice</summary>
    static DialogueChoice Choice(string label, System.Action onChoose)
        => new DialogueChoice { label = label, onChoose = onChoose };

    // ====================================================================
    // 辅助：时间控制 & 状态栏
    // ====================================================================

    void SetTimeFast() { if (TimeManager.Instance != null) TimeManager.Instance.realSecondsPerGameHour = fastSpeed; }
    void SetTimeNormal() { if (TimeManager.Instance != null) TimeManager.Instance.realSecondsPerGameHour = normalSpeed; }

    void SetStatus(string msg)
    {
        if (statusText != null && statusText.text != msg)
        {
            statusText.text = msg;
            Debug.Log($"[Classroom] {msg}");
        }
    }
}