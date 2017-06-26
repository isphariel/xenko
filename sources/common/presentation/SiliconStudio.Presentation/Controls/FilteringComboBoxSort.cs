// Copyright (c) 2011-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.
using System;
using System.Collections;
using SiliconStudio.Core.Annotations;

namespace SiliconStudio.Presentation.Controls
{
    public class FilteringComboBoxSort : IComparer
    {
        private string token;
        private string tokenLowercase;

        public string Token { get { return token; } set { token = value; tokenLowercase = (value ?? "").ToLowerInvariant(); } }

        public FilteringComboBoxSort()
        {
        }

        public virtual int Compare([NotNull] object x, [NotNull] object y)
        {
            var a = x.ToString();
            var b = y.ToString();

            if (string.IsNullOrWhiteSpace(token))
                return string.Compare(a, b, StringComparison.InvariantCultureIgnoreCase);

            var indexA = a.IndexOf(tokenLowercase, StringComparison.InvariantCultureIgnoreCase);
            var indexB = b.IndexOf(tokenLowercase, StringComparison.InvariantCultureIgnoreCase);

            if (indexA == 0 && indexB > 0)
                return -1;
            if (indexB == 0 && indexA > 0)
                return 1;

            return string.Compare(a, b, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
