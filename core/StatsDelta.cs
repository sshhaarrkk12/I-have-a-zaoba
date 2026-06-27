/// <summary>
/// 属性变化结构体，填 0 = 不变
/// 全局唯一定义，所有系统共用
/// </summary>
[System.Serializable]
public class StatsDelta
{
    [UnityEngine.Tooltip("心情变化")]  public float mood;
    [UnityEngine.Tooltip("体力变化")]  public float stamina;
    [UnityEngine.Tooltip("压力变化")]  public float stress;
    [UnityEngine.Tooltip("疲劳变化")]  public float fatigue;
    [UnityEngine.Tooltip("学业变化")]  public float academic;
    [UnityEngine.Tooltip("社交变化")]  public float social;

    public bool IsEmpty() =>
        mood == 0 && stamina == 0 && stress == 0 &&
        fatigue == 0 && academic == 0 && social == 0;
}
