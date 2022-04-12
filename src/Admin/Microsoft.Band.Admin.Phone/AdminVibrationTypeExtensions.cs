// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.AdminVibrationTypeExtensions
// Assembly: Microsoft.Band.Admin.Phone, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 8CA93721-E39E-407D-B5BF-4FCE9A5E47B1
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.Phone.dll

using Microsoft.Band.Notifications;
using System;

namespace Microsoft.Band.Admin
{
  internal static class AdminVibrationTypeExtensions
  {
    public static BandVibrationType ToBandVibrationType(
      this AdminVibrationType vibrationType)
    {
      switch (vibrationType)
      {
        case AdminVibrationType.SystemBatteryCharging:
          return BandVibrationType.SystemBatteryCharging;
        case AdminVibrationType.SystemBatteryFull:
          return BandVibrationType.SystemBatteryFull;
        case AdminVibrationType.SystemBatteryLow:
          return BandVibrationType.SystemBatteryLow;
        case AdminVibrationType.SystemBatteryCritical:
          return BandVibrationType.SystemBatteryCritical;
        case AdminVibrationType.SystemShutDown:
          return BandVibrationType.SystemShutDown;
        case AdminVibrationType.SystemStartUp:
          return BandVibrationType.SystemStartUp;
        case AdminVibrationType.SystemButtonFeedback:
          return BandVibrationType.SystemButtonFeedback;
        case AdminVibrationType.ToastTextMessage:
          return BandVibrationType.ToastTextMessage;
        case AdminVibrationType.ToastMissedCall:
          return BandVibrationType.ToastMissedCall;
        case AdminVibrationType.ToastVoiceMail:
          return BandVibrationType.ToastVoiceMail;
        case AdminVibrationType.ToastFacebook:
          return BandVibrationType.ToastFacebook;
        case AdminVibrationType.ToastTwitter:
          return BandVibrationType.ToastTwitter;
        case AdminVibrationType.ToastMeInsights:
          return BandVibrationType.ToastMeInsights;
        case AdminVibrationType.ToastWeather:
          return BandVibrationType.ToastWeather;
        case AdminVibrationType.ToastFinance:
          return BandVibrationType.ToastFinance;
        case AdminVibrationType.ToastSports:
          return BandVibrationType.ToastSports;
        case AdminVibrationType.AlertIncomingCall:
          return BandVibrationType.AlertIncomingCall;
        case AdminVibrationType.AlertAlarm:
          return BandVibrationType.AlertAlarm;
        case AdminVibrationType.AlertTimer:
          return BandVibrationType.AlertTimer;
        case AdminVibrationType.AlertCalendar:
          return BandVibrationType.AlertCalendar;
        case AdminVibrationType.VoiceListen:
          return BandVibrationType.VoiceListen;
        case AdminVibrationType.VoiceDone:
          return BandVibrationType.VoiceDone;
        case AdminVibrationType.VoiceAlert:
          return BandVibrationType.VoiceAlert;
        case AdminVibrationType.ExerciseRunLap:
          return BandVibrationType.ExerciseRunLap;
        case AdminVibrationType.ExerciseRunGpsLock:
          return BandVibrationType.ExerciseRunGpsLock;
        case AdminVibrationType.ExerciseRunGpsError:
          return BandVibrationType.ExerciseRunGpsError;
        case AdminVibrationType.ExerciseWorkoutTimer:
          return BandVibrationType.ExerciseWorkoutTimer;
        case AdminVibrationType.ExerciseGuidedWorkoutTimer:
          return BandVibrationType.ExerciseGuidedWorkoutTimer;
        case AdminVibrationType.ExerciseGuidedWorkoutComplete:
          return BandVibrationType.ExerciseGuidedWorkoutComplete;
        case AdminVibrationType.ExerciseGuidedWorkoutCircuitComplete:
          return BandVibrationType.ExerciseGuidedWorkoutCircuitComplete;
        default:
          throw new ArgumentException("Unknown AdminVibrationType value.");
      }
    }
  }
}
