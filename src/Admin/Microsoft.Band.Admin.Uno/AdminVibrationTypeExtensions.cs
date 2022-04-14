using System;
using Microsoft.Band.Notifications;

namespace Microsoft.Band.Admin;

internal static class AdminVibrationTypeExtensions
{
    public static BandVibrationType ToBandVibrationType(this AdminVibrationType vibrationType)
    {
        return vibrationType switch
        {
            AdminVibrationType.SystemBatteryCharging => BandVibrationType.SystemBatteryCharging, 
            AdminVibrationType.SystemBatteryFull => BandVibrationType.SystemBatteryFull, 
            AdminVibrationType.SystemBatteryLow => BandVibrationType.SystemBatteryLow, 
            AdminVibrationType.SystemBatteryCritical => BandVibrationType.SystemBatteryCritical, 
            AdminVibrationType.SystemShutDown => BandVibrationType.SystemShutDown, 
            AdminVibrationType.SystemStartUp => BandVibrationType.SystemStartUp, 
            AdminVibrationType.SystemButtonFeedback => BandVibrationType.SystemButtonFeedback, 
            AdminVibrationType.ToastTextMessage => BandVibrationType.ToastTextMessage, 
            AdminVibrationType.ToastMissedCall => BandVibrationType.ToastMissedCall, 
            AdminVibrationType.ToastVoiceMail => BandVibrationType.ToastVoiceMail, 
            AdminVibrationType.ToastFacebook => BandVibrationType.ToastFacebook, 
            AdminVibrationType.ToastTwitter => BandVibrationType.ToastTwitter, 
            AdminVibrationType.ToastMeInsights => BandVibrationType.ToastMeInsights, 
            AdminVibrationType.ToastWeather => BandVibrationType.ToastWeather, 
            AdminVibrationType.ToastFinance => BandVibrationType.ToastFinance, 
            AdminVibrationType.ToastSports => BandVibrationType.ToastSports, 
            AdminVibrationType.AlertIncomingCall => BandVibrationType.AlertIncomingCall, 
            AdminVibrationType.AlertAlarm => BandVibrationType.AlertAlarm, 
            AdminVibrationType.AlertTimer => BandVibrationType.AlertTimer, 
            AdminVibrationType.AlertCalendar => BandVibrationType.AlertCalendar, 
            AdminVibrationType.VoiceListen => BandVibrationType.VoiceListen, 
            AdminVibrationType.VoiceDone => BandVibrationType.VoiceDone, 
            AdminVibrationType.VoiceAlert => BandVibrationType.VoiceAlert, 
            AdminVibrationType.ExerciseRunLap => BandVibrationType.ExerciseRunLap, 
            AdminVibrationType.ExerciseRunGpsLock => BandVibrationType.ExerciseRunGpsLock, 
            AdminVibrationType.ExerciseRunGpsError => BandVibrationType.ExerciseRunGpsError, 
            AdminVibrationType.ExerciseWorkoutTimer => BandVibrationType.ExerciseWorkoutTimer, 
            AdminVibrationType.ExerciseGuidedWorkoutTimer => BandVibrationType.ExerciseGuidedWorkoutTimer, 
            AdminVibrationType.ExerciseGuidedWorkoutComplete => BandVibrationType.ExerciseGuidedWorkoutComplete, 
            AdminVibrationType.ExerciseGuidedWorkoutCircuitComplete => BandVibrationType.ExerciseGuidedWorkoutCircuitComplete, 
            _ => throw new ArgumentException("Unknown AdminVibrationType value."), 
        };
    }
}
