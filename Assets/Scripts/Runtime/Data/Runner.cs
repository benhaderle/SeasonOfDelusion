using System;
using System.Collections.Generic;
using CreateNeptune;
using Unity.Collections;
using UnityEngine;

/// <summary>
/// Holds variables and functionality relevant to runners
/// </summary>
public class Runner
{
    /// <summary>
    /// Putting these here bc i don't know where else to put it
    /// </summary> 
    private const float MAX_FORM = 100;
    private const float MAX_STRENGTH = 100;
    private const float MAX_SLEEP = 10;
    private const float MIN_SLEEP = 0;
    private const float MAX_SHORT_TERM_CALORIES = 3000;
    public static readonly float INIT_LONG_TERM_CALORIES = 10000;
    /// <summary>
    /// An SO with shared variables between all runners
    /// </summary>
    private RunnerCalculationVariables variables;
    private RunnerSaveDataSO runnerSaveData;
    public string FirstName => runnerSaveData.data.firstName;
    public string LastName => runnerSaveData.data.lastName;
    /// <value>User facing string formatted as "FirstName LastName"</value>
    public string Name => $"{FirstName} {LastName}";
    public string Initials => $"{FirstName[0]}{LastName[0]}";
    public string TeamName => runnerSaveData.data.teamName;

    private Sprite[] characterSprites;

    public int level
    {
        get => runnerSaveData.data.level;
        private set => runnerSaveData.data.level = value;
    }
    public int experience
    {
        get => runnerSaveData.data.experience;
        private set => runnerSaveData.data.experience = value;
    }
    
    // Stats change with each level + are more "permanent"
    #region Stats
    /// <summary>
    /// The runner's current VO2Max.
    /// </summary>    
    public float currentVO2Max
    {
        get => runnerSaveData.data.currentVO2Max;
        private set => runnerSaveData.data.currentVO2Max = value;
    }
    public float currentStrength
    {
        get => runnerSaveData.data.currentStrength;
        private set => runnerSaveData.data.currentStrength = value;
    }
    public float currentForm
    {
        get => runnerSaveData.data.currentForm;
        private set => runnerSaveData.data.currentForm = value;
    }
    /// <summary>
    /// A number >= 1 that represents how tough this runner is. 
    /// Meant to be used as an exponent to make the effects of soreness and other difficulties increase gradually.
    /// </summary>
    public float currentGrit
    {
        get => runnerSaveData.data.currentGrit;
        private set => runnerSaveData.data.currentGrit = value;
    }
    public float currentConfidence
    {
        get => runnerSaveData.data.confidence;
        private set => runnerSaveData.data.confidence = value;
    }
    public float currentRecovery
    {
        get => runnerSaveData.data.currentRecovery;
        private set => runnerSaveData.data.currentRecovery = value;
    }
    public float currentAcademics
    {
        get => runnerSaveData.data.currentAcademics;
        private set => runnerSaveData.data.currentAcademics = value;
    }
    #endregion

    // Variables used when leveling up
    #region Improvement Variables
    private float vo2ImprovementMagnitude => runnerSaveData.data.vo2ImprovementMagnitude;
    private float strengthImprovementMagnitude => runnerSaveData.data.strengthImprovementMagnitude;
    private float formImprovementMagnitude => runnerSaveData.data.strengthImprovementMagnitude;
    private float gritImprovementMagnitude => runnerSaveData.data.strengthImprovementMagnitude;
    private float recoveryImprovementMagnitude => runnerSaveData.data.strengthImprovementMagnitude;
    #endregion

    // Statuses can change at anytime and are more day to day
    #region Status
    public float weight
    {
        get => runnerSaveData.data.weight;
        private set => runnerSaveData.data.weight = value;
    }
    public float sleepStatus
    {
        get => runnerSaveData.data.sleepStatus;
        private set => runnerSaveData.data.sleepStatus = value;
    }
    public float shortTermCalories
    {
        get => runnerSaveData.data.shortTermCalories;
        private set => runnerSaveData.data.shortTermCalories = value;
    }
    public float longTermCalories
    {
        get => runnerSaveData.data.longTermCalories;
        private set => runnerSaveData.data.longTermCalories = value;
    }
    public float hydrationStatus
    {
        get => runnerSaveData.data.hydrationStatus;
        private set => runnerSaveData.data.hydrationStatus = value;
    }
    public float longTermSoreness
    {
        get => runnerSaveData.data.longTermSoreness;
        private set => runnerSaveData.data.longTermSoreness = value;
    }
    public float confidence
    {
        get => runnerSaveData.data.confidence;
        private set => runnerSaveData.data.confidence = value;
    }

    public float GetCurrentVDOTMax()
    {
        return currentVO2Max * CalculateRunEconomy(1, 1);
    }

    // TODO: impromptu list of stats that might get implemented at some point
    // public int wit { get; private set; }
    // public int spirit { get; private set; }
    // public int smarts { get; private set; }

    // private float emotion;
    // private float intuition;
    #endregion

    public Runner()
    {
    }

    /// <summary>
    /// Initializes this Runner with a new RunnerSaveData object with default values
    /// </summary>
    /// <param name="variables">The variables to use for calculating updates to Runner stats</param> 
    public void Initialize(RunnerInitializationSO initializationSO, RunnerCalculationVariables variables, string teamName)
    {
        this.variables = variables;
        runnerSaveData = initializationSO.runnerSaveData;
        characterSprites = initializationSO.characterSprites;

        if (runnerSaveData == null)
        {
            runnerSaveData = ScriptableObject.CreateInstance<RunnerSaveDataSO>();
        }

        if (!runnerSaveData.data.initialized)
        {
            runnerSaveData.Initialize(initializationSO, MAX_SHORT_TERM_CALORIES, teamName);
        }
    }

    public void ChangeStatFromDialogue(string statName, float changeAmount)
    {
        switch (statName)
        {
            case "Aerobics": currentVO2Max += changeAmount; break;
            case "Strength": currentStrength += changeAmount; break;
            case "Form": currentForm += changeAmount; break;
            case "Grit": currentGrit += changeAmount; break;
            case "Recovery": currentRecovery += changeAmount; break;
            case "Academics": currentAcademics += changeAmount; break;
            case "Confidence": currentConfidence += changeAmount; break;
        }
    }

    #region Post Run Functions

    /// <summary>
    /// Updates this Runner's stats given the information in runState. Assumes the run is done.
    /// </summary>
    /// <param name="runState">The state of this Runner after a run is finished.</param>
    public RunnerUpdateRecord PostRunUpdate(RunnerState runState, Route route)
    {
        RunnerUpdateRecord updateRecord = new RunnerUpdateRecord
        {
            startingExperience = experience,
            startingLevelExperienceThreshold = variables.levelExperienceThresholds[level - 1]
        };

        float runVDOT = runState.GetAverageVDOT();
        updateRecord.runVDOT = runVDOT;

        UpdateStatusPostRun(runState, runVDOT, RunController.NORMAL_RUN_TARGET_VDOT);

        updateRecord.experienceChange = UpdateExperience(runVDOT, route.Length + route.ElevationGain * .001f);

        updateRecord.levelUpRecords = new();
        while (experience >= variables.levelExperienceThresholds[level - 1])
        {
            LevelUpRecord record = LevelUp();
            updateRecord.levelUpRecords.Add(record);
        }

        return updateRecord; 
    }

    /// <summary>
    /// Updates this Runner's stats given the information in runState. Assumes the run is done.
    /// </summary>
    /// <param name="runState">The state of this Runner after a run is finished.</param>
    public RunnerUpdateRecord PostWorkoutUpdate(RunnerState runState, Workout workout, float groupTargetVDOT)
    {
        RunnerUpdateRecord updateRecord = new RunnerUpdateRecord
        {
            startingExperience = experience,
            startingLevelExperienceThreshold = variables.levelExperienceThresholds[level - 1],
            statUpRecords = new(),
            levelUpRecords = new()
        };

        float runVDOT = runState.GetAverageVDOT();
        updateRecord.runVDOT = runVDOT;

        UpdateStatusPostRun(runState, runVDOT, groupTargetVDOT);

        // this gives the full effect of the workout the closer you are to the goal vo2 of the workout
        float workoutEffectiveness = .01f * Mathf.Pow(Mathf.Min(1, 1 - Mathf.Abs((runVDOT - groupTargetVDOT) / GetCurrentVDOTMax())), 9);

        for (int i = 0; i < workout.effects.Length; i++)
        {
            float effectAmount = workout.effects[i].amount * workoutEffectiveness;

            StatUpRecord statUpRecord = new StatUpRecord
            {
                statType = workout.effects[i].type
            };

            switch (workout.effects[i].type)
            {
                case WorkoutEffect.Type.Aerobic: currentVO2Max = UpdateStat(currentVO2Max, effectAmount, ref statUpRecord); break;
                case WorkoutEffect.Type.Strength: currentStrength = UpdateStat(currentStrength, effectAmount, ref statUpRecord); break;
                case WorkoutEffect.Type.Grit: currentGrit = UpdateStat(currentGrit, effectAmount, ref statUpRecord); break;
                case WorkoutEffect.Type.Form: currentForm = UpdateStat(currentForm, effectAmount, ref statUpRecord); break;
                case WorkoutEffect.Type.Recovery: currentRecovery = UpdateStat(currentRecovery, effectAmount, ref statUpRecord); break;
            }

            updateRecord.statUpRecords.Add(statUpRecord);
        }

        updateRecord.experienceChange = UpdateExperience(runVDOT, workout.GetTotalLength() * groupTargetVDOT / currentVO2Max);
        while (experience >= variables.levelExperienceThresholds[level - 1])
        {
            LevelUpRecord record = LevelUp();
            updateRecord.levelUpRecords.Add(record);
        }

        return updateRecord;
    }

    private float UpdateStat(float currentStat, float effectAmount, ref StatUpRecord statUpRecord)
    {
        statUpRecord.oldValue = currentStat;
        statUpRecord.newValue = currentStat * (1 + effectAmount);

        Debug.Log($"{Name} Old {statUpRecord.statType}:{statUpRecord.oldValue} New {statUpRecord.statType}:{statUpRecord.newValue}");

        return statUpRecord.newValue;
    }

    private void UpdateStatusPostRun(RunnerState state, float runVDOT, float goalVDOT)
    {
        float timeInMinutes = state.timeInSeconds / 60f;

        // update hydration and calories
        hydrationStatus -= state.hydrationCost;
        float longTermCalorieCost = Mathf.Max(0, state.calorieCost - shortTermCalories);
        shortTermCalories = Mathf.Max(0, shortTermCalories - state.calorieCost);
        longTermCalories = Mathf.Max(0, longTermCalories - longTermCalorieCost);

        // exhaustion changes based off of how far away you were from your recovery VO2
        longTermSoreness += CalculateLongTermSoreness(runVDOT, timeInMinutes);

        confidence += (runVDOT / GetCurrentVDOTMax()) - goalVDOT;
    }

    private int UpdateExperience(float runVO2, float runDifficultyMultiplier)
    {
        float vo2ImprovementGap = (runVO2 / (GetCurrentVDOTMax() * (.02f * level + .58f))) - 1f;

        int experienceChange = Mathf.CeilToInt(vo2ImprovementGap * runDifficultyMultiplier);
        experienceChange = Mathf.Max(experienceChange, -experience);
        if (level == 20)
        {
            experienceChange = Mathf.Min(experienceChange, 0);
        }

        experience += experienceChange;

        Debug.Log($"Name: {Name} \tExperience Change: {experienceChange}\t Experience: {experience}");

        return experienceChange;
    }

    private LevelUpRecord LevelUp()
    {
        // register the old values so we can show how much they changed
        float oldVO2 = currentVO2Max;
        float oldStrength = currentStrength;
        float oldForm = currentForm;
        float oldGrit = currentGrit;
        float oldRecovery = currentRecovery;

        float normalizedLevel = Mathf.InverseLerp(1, 20, level);

        // update our stats
        currentVO2Max *= 1 + (variables.vo2ImprovementCurve.Evaluate(normalizedLevel) * vo2ImprovementMagnitude);
        currentStrength *= 1 + (variables.strengthImprovementCurve.Evaluate(normalizedLevel) * strengthImprovementMagnitude);
        currentForm *= 1 + (variables.formImprovementCurve.Evaluate(normalizedLevel) * formImprovementMagnitude);
        currentGrit *= 1 + (variables.gritImprovementCurve.Evaluate(normalizedLevel) * gritImprovementMagnitude);
        currentRecovery *= 1 + (variables.recoveryImprovementCurve.Evaluate(normalizedLevel) * recoveryImprovementMagnitude);

        experience -= variables.levelExperienceThresholds[level - 1];
        level++;

        Debug.Log($"LEVEL UP! Name: {Name}\t Level {level}\tOld VO2: {oldVO2}\tNew VO2: {currentVO2Max}\tOld Strength: {oldStrength}\tNew Strength: {currentStrength}\t");

        return new LevelUpRecord
        {
            newLevel = level,
            newLevelExperienceThreshold = variables.levelExperienceThresholds[level - 1],
            oldVO2 = oldVO2,
            newVO2 = currentVO2Max,
            oldStrength = oldStrength,
            newStrength = currentStrength,
            oldForm = oldForm,
            newForm = currentForm,
            oldGrit = oldGrit,
            newGrit = currentGrit,
            oldRecovery = oldRecovery,
            newRecovery = currentRecovery 
        };
    }

    #endregion

    #region End of Day Functions

    /// <summary>
    /// Performs any EOD updates to runner stats. Currently effects form, hydration, calories, sleep, and soreness
    /// </summary>
    public void OnEndDay()
    {
        // nutrition roll effects hydration and calorie recovery
        float nutritionRoll = CNExtensions.RandGaussian(currentRecovery, 10);

        // hydration updates linearly based on the roll
        hydrationStatus += 2f * Mathf.Clamp(nutritionRoll / 50f, 0, 2);

        float shortTermCalorieDeficit = shortTermCalories - MAX_SHORT_TERM_CALORIES;
        // recover calories based on the deficit (ie how hungry you are) and the nutrition roll
        float shortTermCaloriesToAdd = shortTermCalorieDeficit * (nutritionRoll / 80f);
        // any calories in excess will be stored as long term calories
        float longTermCaloriesToAdd = (shortTermCaloriesToAdd - shortTermCalorieDeficit) * .5f;
        // add up our new calories
        shortTermCalories += shortTermCaloriesToAdd;
        // if we have long term calories to add, cap shortTermCalories and add the long term ones
        if (longTermCaloriesToAdd > 0)
        {
            shortTermCalories = 3000;
            longTermCalories += longTermCaloriesToAdd;
        }

        // recovery roll effects sleep and soreness
        float recoveryRoll = CNExtensions.RandGaussian(currentRecovery, 10);
        // use the roll to approximate how many hours fo sleep you have
        float hoursOfSleep = recoveryRoll * .1f - 6;
        // recover sleep and clamp between reasonable values
        sleepStatus += hoursOfSleep;
        sleepStatus = Mathf.Clamp(sleepStatus, MIN_SLEEP, MAX_SLEEP);

        // recover soreness a bit over night
        // the recovery roll effects the how much soreness is linearly recovered
        float linearRecoverySlope = 1f - (recoveryRoll * .002f);
        // in addition to the linear amount lost, there's also a constant amount lost
        longTermSoreness = (linearRecoverySlope * longTermSoreness) - variables.DayEndLongTermSorenessRecovery;
        longTermSoreness = Mathf.Max(0, longTermSoreness);
    }

    #endregion

    #region Calculation Methods
    /// <param name="runVDOT">The VO2 for a segment of running</param>
    /// <param name="timeInMinutes">The amount of time spent running for a segment</param>
    /// <returns>The amount of short term soreness for this run segment</returns>
    public float CalculateShortTermSoreness(float runVDOT, float timeInMinutes)
    {
        float value = (100f / GetCurrentVDOTMax()) * timeInMinutes * Mathf.Pow(runVDOT / (GetCurrentVDOTMax() * .65f), 4);
        return value;
    }

    /// <param name="runVO2">The VO2 for a segment of running</param>
    /// <param name="timeInMinutes">The amount of time spent running for a segment</param>
    /// <returns>The amount of long term soreness for this run segment</returns>
    private float CalculateLongTermSoreness(float runVO2, float timeInMinutes)
    {
        // go look at desmos if you want to see the shape of this graph
        // basic idea is that longTermSoreness goes up if you ran harder or longer, goes down if you ran slower or shorter
        float longTermSorenessGap = (runVO2 / (currentVO2Max * variables.LongTermSorenessVO2Threshold)) - 1f;
        float longTermSoreness = (variables.CubicLongTermSorenessSlope * timeInMinutes * Mathf.Pow(longTermSorenessGap, 3))
            + (variables.LinearLongTermSorenessSlope * timeInMinutes * (longTermSorenessGap + variables.LinearLongTermSorenessOffset))
            + (variables.LinearLongTermSorenessTimeSlope * (timeInMinutes + variables.LinearLongTermSorenessTimeOffset));

        return longTermSoreness;
    }

    /// <summary>
    /// Calculates the economy of the current runner under the given circumstances
    /// </summary>
    /// <param name="hydration">How much hydration the runner has between 0 and 1</param>
    /// <param name="calories">How many calories the runner has between 0 and 1</param>
    /// <returns>A number between 0 and 1 representing the runner's economy under the given circumstances</returns>
    private float CalculateRunEconomy(float hydration, float calories)
    {
        // right now everything is equally weighted, but that might change
        float formWeight = .25f;
        float strengthWeight = .25f;
        float hydrationWeight = .25f;
        float calorieWeight = .25f;

        float economy = formWeight * Mathf.Sqrt(currentForm * Mathf.InverseLerp(MIN_SLEEP, MAX_SLEEP, sleepStatus) / MAX_FORM) +
         strengthWeight * Mathf.Sqrt(currentStrength / MAX_STRENGTH) +
         hydrationWeight * Mathf.Clamp01(hydration) +
         calorieWeight * Mathf.Min(1, calories);

        return Mathf.Pow(economy, 1 / currentGrit);
    }

    /// <returns>A number between 0 and 1 representing the runner's economy under the current internal circumstances</returns>
    public float CalculateRunEconomy()
    {
        return CalculateRunEconomy(hydrationStatus, shortTermCalories + longTermCalories);
    }

    /// <returns>A number between 0 and 1 representing the runner's economy under the given circumstances</returns>
    public float CalculateRunEconomy(RunnerState state)
    {
        return CalculateRunEconomy(hydrationStatus - state.hydrationCost, shortTermCalories + longTermCalories - state.calorieCost);
    }

    /// <param name="runVO2">The VO2 for a segment of running</param>
    /// <param name="timeInMinutes">The amount of time spent running for a segment</param>
    /// <returns>The amount of hydration cost for this run segment</returns>
    public float CalculateHydrationCost(float runVO2, float timeInMinutes)
    {
        // .02 is .02L per minute of water used per minute of running on average
        return runVO2 / (currentVO2Max * .7f) * timeInMinutes * .02f;
    }

    /// <param name="runVO2">The VO2 for a segment of running</param>
    /// <param name="timeInMinutes">The amount of time spent running for a segment</param>
    /// <returns>The amount of calories cost for this run segment</returns>
    public float CalculateCalorieCost(float runVO2, float timeInMinutes)
    {
        // 10 is the amount of calories burned per minute at 70% of VO2Max
        return runVO2 / (currentVO2Max * .7f) * timeInMinutes * 10f;
    }

    public int GetCurrentLevelExperienceThreshold()
    {
        return variables.levelExperienceThresholds[level - 1];
    }

    public Sprite GetCurrentConfidenceSprite()
    {
        // TODO: we just return the first sprite for now, but this should return the appropriate sprite when I have them made up
        if (characterSprites != null)
        {
            return characterSprites[0];
        }

        return null;
    }

    public float GetDisplayableCurrentSoreness()
    {
        // go look at the graph on desmos for what the shape of the curve looks like
        // but the general idea is that we want to diplay a number between 0 and 10
        // that gets exponentially bigger the more sore you are
        // this is calibrated around 500 being the max soreness
        return Mathf.Pow(longTermSoreness, 1.00318f) - longTermSoreness;
    }

    public float GetDisplayableCurrentSleep()
    {
        return 10 * (1f - Mathf.InverseLerp(MIN_SLEEP, MAX_SLEEP, sleepStatus));
    }
    public float GetDisplayableCurrentNutrition()
    {
        return 10f - (shortTermCalories / MAX_SHORT_TERM_CALORIES * 2f) - (longTermCalories / INIT_LONG_TERM_CALORIES * 8f);
    }

    public float GetDisplayableCurrentHydration()
    {
        return Mathf.Min(Mathf.Pow(hydrationStatus, -2), 10f);
    }
    #endregion
}

public struct RunnerUpdateRecord
{
    public int startingExperience;
    public int startingLevelExperienceThreshold;
    public int experienceChange;
    public List<LevelUpRecord> levelUpRecords;
    public List<StatUpRecord> statUpRecords;
    public float runVDOT;
}

public struct LevelUpRecord
{
    public int newLevel;
    public int newLevelExperienceThreshold;
    public float oldVO2;
    public float newVO2;
    public float oldStrength;
    public float newStrength;
    public float oldForm;
    public float newForm;
    public float oldGrit;
    public float newGrit;
    public float oldRecovery;
    public float newRecovery;
}

public struct StatUpRecord
{
    public WorkoutEffect.Type statType;
    public float oldValue;
    public float newValue;
}