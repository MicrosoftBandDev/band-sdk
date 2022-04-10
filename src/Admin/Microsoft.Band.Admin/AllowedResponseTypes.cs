// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.AllowedResponseTypes
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;

namespace Microsoft.Band.Admin
{
    [Flags]
    internal enum AllowedResponseTypes
    {
        None = 0,
        Keyboard = 1,
        Dictation = 2,
        Voice = 4,
        Smart = 8,
        Canned = 16, // 0x00000010
    }
}
