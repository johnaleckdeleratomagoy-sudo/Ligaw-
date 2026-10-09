using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class HDRPDayNightCycle : MonoBehaviour
{
    public enum WeatherType
    {
        Clear,
        Rainy,
        Foggy,
        Stormy
    }

    [Serializable]
    public struct DayWeatherConfig
    {
        [Tooltip("Day number from 1 to 60")]
        public int dayNumber;
        public WeatherType weather;
    }

    [Header("Calendar System (60 Days)")]
    [Tooltip("Current active day (1 to 60)")]
    [Range(1, 60)]
    public int currentDay = 1;

    [Tooltip("Duration of a full 24-hour cycle in real seconds")]
    public float dayDurationInSeconds = 120f;

    [Range(0f, 1f)]
    [Tooltip("0.0 = Midnight | 0.25 = Sunrise (6 AM) | 0.5 = Noon (12 PM) | 0.75 = Sunset (6 PM)")]
    public float currentTime = 0.25f;

    [Header("Daily Weather Control")]
    [Tooltip("If checked, any day NOT defined in the Schedule below will pick a random weather automatically!")]
    public bool randomizeUnscheduledDays = false;

    [Tooltip("Default weather to use for days not listed in the schedule (if Randomize is disabled).")]
    public WeatherType defaultUnscheduledWeather = WeatherType.Clear;

    [Header("Custom 60-Day Schedule")]
    [Tooltip("Define specific weather for individual days here!")]
    public DayWeatherConfig[] weatherSchedule;

    [Header("Light References")]
    public Light sunLight;
    public Light moonLight;

    [Header("Lux Intensities")]
    public float maxSunIntensity = 100000f;
    public float maxMoonIntensity = 50f;

    [Header("Daylight Color Grading")]
    public Gradient sunColorGradient;

    [Header("Global Wind Settings (Clouds & Foliage)")]
    public Volume globalVolume;
    public WindZone sceneWindZone;

    [Tooltip("Wind speed in km/h for clouds")]
    public float globalWindSpeed = 30f;

    [Range(0f, 360f)]
    [Tooltip("Direction in degrees the wind blows toward")]
    public float globalWindOrientation = 45f;

    [Range(0f, 2f)]
    [Tooltip("Strength applied to the WindZone for tree sway")]
    public float treeWindStrength = 0.4f;

    [Header("Weather System References")]
    [Tooltip("Reference to the RainController script on your GLOBAL SYSTEM or RainSystem object.")]
    public RainController rainController;

    [Header("Wet Surface Material Settings")]
    [Tooltip("The main ground/terrain material to make wet during rain")]
    public Material groundMaterial;
    [Range(0f, 1f)]
    [Tooltip("Default dryness level when clear (usually 0.1 - 0.3)")]
    public float drySmoothness = 0.2f;
    [Range(0f, 1f)]
    [Tooltip("Max glossiness when completely soaked (usually 0.85 - 0.95)")]
    public float wetSmoothness = 0.9f;
    [Tooltip("How fast ground gets wet / dries off")]
    public float wetTransitionSpeed = 0.1f;

    private VisualEnvironment visualEnv;
    private VolumetricClouds volumetricClouds;
    private Fog fogComponent;

    private int lastProcessedDay = -1;
    private WeatherType currentTodayWeather = WeatherType.Clear;
    private float currentWetness = 0f;

    private void Start()
    {
        if (globalVolume != null && globalVolume.profile != null)
        {
            if (globalVolume.profile.TryGet(out visualEnv))
            {
                visualEnv.windSpeed.overrideState = true;
                visualEnv.windOrientation.overrideState = true;
                visualEnv.windSpeed.value = globalWindSpeed;
                visualEnv.windOrientation.value = globalWindOrientation;
            }

            if (globalVolume.profile.TryGet(out volumetricClouds))
            {
                volumetricClouds.globalWindSpeed.overrideState = true;
                volumetricClouds.cloudMapSpeedMultiplier.overrideState = true;
                volumetricClouds.shapeSpeedMultiplier.overrideState = true;
                volumetricClouds.cloudMapSpeedMultiplier.value = 1f;
                volumetricClouds.shapeSpeedMultiplier.value = 1f;
            }

            globalVolume.profile.TryGet(out fogComponent);
        }

        // Force-apply initial day weather immediately
        ApplyWeatherForCurrentDay();
    }

    private void Update()
    {
        // 1. Advance Time & Day Progression
        currentTime += Time.deltaTime / dayDurationInSeconds;
        if (currentTime >= 1f)
        {
            currentTime = 0f;
            currentDay++;
            if (currentDay > 60) currentDay = 1;
        }

        // 2. Detect Day Change (or Inspector manual change during Play Mode)
        if (currentDay != lastProcessedDay)
        {
            ApplyWeatherForCurrentDay();
        }

        // 3. Rotate Sun & Moon
        float sunAngle = (currentTime * 360f) - 90f;

        if (sunLight)
        {
            sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
        }

        if (moonLight)
        {
            moonLight.transform.rotation = Quaternion.Euler(sunAngle + 180f, 170f, 0f);
        }

        // 4. Update Light Intensities & Colors
        if (sunLight)
        {
            float sunDot = Vector3.Dot(sunLight.transform.forward, Vector3.down);

            if (sunDot > 0f) // Daytime
            {
                sunLight.intensity = Mathf.Clamp01(sunDot) * maxSunIntensity;

                if (sunColorGradient != null)
                {
                    sunLight.color = sunColorGradient.Evaluate(currentTime);
                }

                if (moonLight) moonLight.intensity = 0f;
            }
            else // Nighttime
            {
                sunLight.intensity = 0f;

                if (moonLight)
                {
                    moonLight.intensity = Mathf.Clamp01(-sunDot) * maxMoonIntensity;
                }
            }
        }

        // 5. Update Wind
        if (visualEnv != null)
        {
            visualEnv.windSpeed.value = globalWindSpeed;
            visualEnv.windOrientation.value = globalWindOrientation;
        }

        if (sceneWindZone != null)
        {
            sceneWindZone.transform.rotation = Quaternion.Euler(0f, globalWindOrientation, 0f);
            sceneWindZone.windMain = treeWindStrength;
        }

        // 6. Smoothly Transition Material Wetness
        UpdateGroundWetness();
    }

    public void ApplyWeatherForCurrentDay()
    {
        lastProcessedDay = currentDay;
        bool foundScheduledWeather = false;

        // Check if current day exists in custom schedule
        if (weatherSchedule != null)
        {
            foreach (var config in weatherSchedule)
            {
                if (config.dayNumber == currentDay)
                {
                    currentTodayWeather = config.weather;
                    foundScheduledWeather = true;
                    break;
                }
            }
        }

        // If not in schedule, pick random or default fallback weather
        if (!foundScheduledWeather)
        {
            if (randomizeUnscheduledDays)
            {
                Array values = Enum.GetValues(typeof(WeatherType));
                currentTodayWeather = (WeatherType)values.GetValue(UnityEngine.Random.Range(0, values.Length));
            }
            else
            {
                currentTodayWeather = defaultUnscheduledWeather;
            }
        }

        // Apply visual & audio settings according to weather type
        switch (currentTodayWeather)
        {
            case WeatherType.Clear:
                SetRain(0f);
                SetFog(200f);
                break;

            case WeatherType.Rainy:
                SetRain(0.6f); // Moderate rain
                SetFog(60f);
                break;

            case WeatherType.Foggy:
                SetRain(0f);
                SetFog(15f);
                break;

            case WeatherType.Stormy:
                SetRain(1.0f); // Heavy rain storm
                SetFog(30f);
                break;
        }

        Debug.Log($"[Calendar] Day {currentDay} Weather Applied: {currentTodayWeather}");
    }

    private void UpdateGroundWetness()
    {
        bool isWetWeather = (currentTodayWeather == WeatherType.Rainy || currentTodayWeather == WeatherType.Stormy);
        float targetWetness = isWetWeather ? 1f : 0f;

        currentWetness = Mathf.MoveTowards(currentWetness, targetWetness, Time.deltaTime * wetTransitionSpeed);

        if (groundMaterial != null)
        {
            float blendedSmoothness = Mathf.Lerp(drySmoothness, wetSmoothness, currentWetness);

            if (groundMaterial.HasProperty("_Smoothness"))
            {
                groundMaterial.SetFloat("_Smoothness", blendedSmoothness);
            }
        }
    }

    private void SetRain(float intensity)
    {
        if (rainController == null)
        {
            Debug.LogWarning("[HDRPDayNightCycle] RainController reference is missing in the Inspector!");
            return;
        }

        if (intensity > 0f)
        {
            rainController.StartRain(intensity);
        }
        else
        {
            rainController.StopRain();
        }
    }

    private void SetFog(float attenuationDistance)
    {
        if (fogComponent != null)
        {
            fogComponent.meanFreePath.overrideState = true;
            fogComponent.meanFreePath.value = attenuationDistance;
        }
    }

    // Call this from a button or UI script to manually trigger a specific weather type immediately
    public void ForceSetWeather(WeatherType weather)
    {
        currentTodayWeather = weather;
        ApplyWeatherForCurrentDay();
    }
}