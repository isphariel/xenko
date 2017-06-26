// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.
using System;
using System.Globalization;
using System.Windows.Data;
using SiliconStudio.Core.Annotations;

namespace SiliconStudio.Presentation.ValueConverters
{
    /// <summary>
    /// This converter will compare a given numeric value with a numeric value passed as parameter.
    /// </summary>
    public abstract class CompareNum<T> : OneWayValueConverter<T> where T : class, IValueConverter, new()
    {
        /// <inheritdoc/>
        [NotNull]
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var doubleValue = ConverterHelper.ConvertToDouble(value, culture);
            var doubleParameter = ConverterHelper.ConvertToDouble(parameter, culture);
            return Compare(doubleValue, doubleParameter);
        }

        protected abstract bool Compare(double left, double right);
    }

    /// <summary>
    /// This converter will return <c>true</c> if the numeric value is greater than the numeric parameter.
    /// </summary>
    public class IsGreater : CompareNum<IsGreater>
    {
        protected override bool Compare(double left, double right)
        {
            return left > right;
        }
    }

    /// <summary>
    /// This converter will return <c>true</c> if the numeric value is lower than the numeric parameter.
    /// </summary>
    public class IsLower : CompareNum<IsLower>
    {
        protected override bool Compare(double left, double right)
        {
            return left < right;
        }
    }

    /// <summary>
    /// This converter will return <c>true</c> if the numeric value is greater than or equal to the numeric parameter.
    /// </summary>
    public class IsGreaterOrEqual : CompareNum<IsGreaterOrEqual>
    {
        protected override bool Compare(double left, double right)
        {
            return left >= right;
        }
    }

    /// <summary>
    /// This converter will return <c>true</c> if the numeric value is lower than or equal to the numeric parameter.
    /// </summary>
    public class IsLowerOrEqual : CompareNum<IsLowerOrEqual>
    {
        protected override bool Compare(double left, double right)
        {
            return left <= right;
        }
    }

    /// <summary>
    /// This converter will return <c>true</c> if the numeric value is equal to the numeric parameter.
    /// </summary>
    public class IsEqual : CompareNum<IsEqual>
    {
        protected override bool Compare(double left, double right)
        {
            return Math.Abs(left - right) <= double.Epsilon;
        }
    }

    /// <summary>
    /// This converter will return <c>true</c> if the numeric value is different from the numeric parameter.
    /// </summary>
    public class IsDifferent : CompareNum<IsDifferent>
    {
        protected override bool Compare(double left, double right)
        {
            return Math.Abs(left - right) > double.Epsilon;
        }
    }
}
