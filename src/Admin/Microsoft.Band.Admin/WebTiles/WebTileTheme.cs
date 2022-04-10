// Decompiled with JetBrains decompiler
// Type: Microsoft.Band.Admin.WebTiles.WebTileTheme
// Assembly: Microsoft.Band.Admin, Version=1.3.31002.2, Culture=neutral, PublicKeyToken=null
// MVID: 366705DD-0763-47F9-B6A9-5EDF88598091
// Assembly location: D:\Documents\REProj\MicrosoftBand\HealthApp.WindowsPhone_1.3.31002.2_ARM\Microsoft.Band.Admin.dll

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;

namespace Microsoft.Band.Admin.WebTiles
{
    [DataContract]
    public class WebTileTheme
    {
        private string _base;
        private string highlight;
        private string lowlight;
        private string secondaryText;
        private string highContrast;
        private string muted;
        private WebTilePropertyValidator validator;

        public WebTileTheme() => this.validator = new WebTilePropertyValidator();

        public bool AllowInvalidValues
        {
            get => this.validator.AllowInvalidValues;
            set => this.validator.AllowInvalidValues = value;
        }

        public Dictionary<string, string> PropertyErrors => this.validator.PropertyErrors;

        public bool IsValidColor(string color) => color.Length == 6 && int.TryParse(color, NumberStyles.HexNumber, (IFormatProvider)CultureInfo.InvariantCulture, out int _);

        private void SetColorProperty(ref string storage, string value, string propertyName) => this.validator.SetProperty<string>(ref storage, value, propertyName, (this.IsValidColor(value) ? 1 : 0) != 0, string.Format(CommonSR.WTPropertyColorInvalid, new object[1]
        {
      (object) value
        }));

        [DataMember(IsRequired = true, Name = "base")]
        public string Base
        {
            get => this._base;
            set => this.SetColorProperty(ref this._base, value, nameof(Base));
        }

        [DataMember(IsRequired = true, Name = "highlight")]
        public string Highlight
        {
            get => this.highlight;
            set => this.SetColorProperty(ref this.highlight, value, nameof(Highlight));
        }

        [DataMember(IsRequired = true, Name = "lowlight")]
        public string Lowlight
        {
            get => this.lowlight;
            set => this.SetColorProperty(ref this.lowlight, value, nameof(Lowlight));
        }

        [DataMember(IsRequired = true, Name = "secondary")]
        public string SecondaryText
        {
            get => this.secondaryText;
            set => this.SetColorProperty(ref this.secondaryText, value, nameof(SecondaryText));
        }

        [DataMember(IsRequired = true, Name = "highContrast")]
        public string HighContrast
        {
            get => this.highContrast;
            set => this.SetColorProperty(ref this.highContrast, value, nameof(HighContrast));
        }

        [DataMember(IsRequired = true, Name = "muted")]
        public string Muted
        {
            get => this.muted;
            set => this.SetColorProperty(ref this.muted, value, nameof(Muted));
        }
    }
}
