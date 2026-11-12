using Godot;

public partial class DayNightCycle : Node
{
    private const float MaximumSunElevationDegrees = 52f;
    private const float MaximumMoonElevationDegrees = 48f;
    private readonly GameBootstrap owner;
    private float timeOfDay;
    private float lightingUpdateTimer;
    private Godot.Environment environment;
    private ProceduralSkyMaterial skyMaterial;
    private DirectionalLight3D sun;
    private DirectionalLight3D moon;

    public static StandardMaterial3D WaterMaterial { get; private set; }
    public static float NightFactor { get; private set; } = 1f;

    public DayNightCycle(GameBootstrap owner)
    {
        this.owner = owner;
    }

    public override void _Ready()
    {
        skyMaterial = new ProceduralSkyMaterial
        {
            SkyTopColor = new Color("071329"),
            SkyHorizonColor = new Color("17253d"),
            SkyCurve = 0.18f,
            GroundBottomColor = new Color("101827"),
            GroundHorizonColor = new Color("263044"),
            GroundCurve = 0.08f,
            EnergyMultiplier = 1f,
            SunAngleMax = 1.2f,
            SunCurve = 0.08f,
            UseDebanding = true
        };
        environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Sky,
            Sky = new Sky { SkyMaterial = skyMaterial, RadianceSize = Sky.RadianceSizeEnum.Size128 },
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color("52617d"),
            AmbientLightEnergy = 0.4f,
            ReflectedLightSource = Godot.Environment.ReflectionSource.Sky
        };
        AddChild(new WorldEnvironment { Environment = environment });

        sun = new DirectionalLight3D
        {
            Name = "SunLight",
            LightColor = new Color("fff0d0"),
            ShadowEnabled = true,
            SkyMode = DirectionalLight3D.SkyModeEnum.LightAndSky
        };
        AddChild(sun);

        moon = new DirectionalLight3D
        {
            Name = "MoonLight",
            LightColor = new Color("9eb9e8"),
            ShadowEnabled = true,
            SkyMode = DirectionalLight3D.SkyModeEnum.LightOnly
        };
        AddChild(moon);

        WaterMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color("397f98", 0.82f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            Roughness = 0.15f,
            Metallic = 0.1f
        };

        UpdateLighting();
        GameLogger.Info($"昼夜循环已初始化；周期 {GameBootstrap.DayNightCycleDurationSeconds:0} 秒");
    }

    public override void _Process(double delta)
    {
        if (!owner.CanInteract) return;
        timeOfDay = (timeOfDay + (float)delta / GameBootstrap.DayNightCycleDurationSeconds) % 1f;
        lightingUpdateTimer += (float)delta;
        if (lightingUpdateTimer < 0.05f) return;
        lightingUpdateTimer = 0f;
        UpdateLighting();
    }

    private void UpdateLighting()
    {
        float dayProgress = (timeOfDay - 0.25f + 1f) % 1f / 0.5f;
        float nightProgress = (timeOfDay - 0.75f + 1f) % 1f / 0.5f;
        bool isDay = dayProgress <= 1f;
        float sunElevation = isDay ? Mathf.Sin(dayProgress * Mathf.Pi) : 0f;
        float moonElevation = isDay ? 0f : Mathf.Sin(nightProgress * Mathf.Pi);

        Vector3 sunPosition = GetCelestialDirection(sunElevation * Mathf.DegToRad(MaximumSunElevationDegrees), dayProgress * Mathf.Pi);
        Vector3 moonPosition = GetCelestialDirection(moonElevation * Mathf.DegToRad(MaximumMoonElevationDegrees), nightProgress * Mathf.Pi);
        sun.Visible = isDay && sunElevation > 0.015f;
        moon.Visible = !isDay && moonElevation > 0.015f;
        if (sun.Visible) sun.LookAt(-sunPosition, Vector3.Up);
        if (moon.Visible) moon.LookAt(-moonPosition, Vector3.Up);

        float daylight = isDay ? Mathf.SmoothStep(0f, 0.35f, sunElevation) : 0f;
        NightFactor = 1f - daylight;
        float twilight = isDay ? 1f - Mathf.SmoothStep(0f, 0.28f, sunElevation) : 1f - Mathf.SmoothStep(0f, 0.28f, moonElevation);
        sun.LightEnergy = Mathf.Lerp(0f, 1.35f, daylight);
        sun.LightColor = new Color("ff9b68").Lerp(new Color("fff0d0"), daylight);
        moon.LightEnergy = 0.32f * moonElevation;

        Color dayTop = new("2865a0");
        Color nightTop = new("071329");
        Color dayHorizon = new("c5dce8");
        Color nightHorizon = new("17253d");
        float skyDaylight = isDay ? daylight : 0f;
        skyMaterial.SkyTopColor = nightTop.Lerp(dayTop, skyDaylight).Lerp(new Color("e77f62"), twilight * 0.5f);
        skyMaterial.SkyHorizonColor = nightHorizon.Lerp(dayHorizon, skyDaylight).Lerp(new Color("f1a06d"), twilight * 0.65f);
        skyMaterial.GroundBottomColor = new Color("101827").Lerp(new Color("3c4a49"), skyDaylight);
        skyMaterial.GroundHorizonColor = new Color("263044").Lerp(new Color("b8c7bb"), skyDaylight).Lerp(new Color("c87564"), twilight * 0.3f);

        environment.AmbientLightColor = new Color("52617d").Lerp(new Color("b9d6e8"), daylight);
        environment.AmbientLightEnergy = Mathf.Lerp(0.4f, 0.58f, daylight);
        WaterMaterial.AlbedoColor = new Color("172e49", 0.88f).Lerp(new Color("397f98", 0.82f), daylight);
    }

    private static Vector3 GetCelestialDirection(float elevation, float azimuth)
    {
        float horizontal = Mathf.Cos(elevation);
        return new Vector3(horizontal * Mathf.Cos(azimuth), Mathf.Sin(elevation), horizontal * Mathf.Sin(azimuth));
    }
}
