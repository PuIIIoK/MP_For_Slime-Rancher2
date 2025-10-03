namespace NewSR2MP.Component;

[RegisterTypeInIl2Cpp(false)]
public class TimeSmoother : MonoBehaviour
{
    public static TimeSmoother Instance
    {
        get; private set;
    }
    void Awake()
    {
        Instance = this;
        
        timeDir = sceneContext.TimeDirector;
    }
    private TimeDirector timeDir;
    public double currTime => timeDir._worldModel.worldTime;
    private float smoothTime;
    public double nextTime;

    public float interpolPeriod = TimeSyncTimer;
    void Update()
    {
        float t = 1.0f - ((smoothTime - Time.unscaledTime) / interpolPeriod);
        timeDir._worldModel.worldTime = Lerp(currTime, nextTime, t);
        
        smoothTime = Time.unscaledTime + interpolPeriod;
    }
    private double Lerp(double a, double b, double t)
    {
        return a + (b - a) * t;
    }
}