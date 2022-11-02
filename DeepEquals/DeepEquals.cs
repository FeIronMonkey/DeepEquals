using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace DeepEquals
{
    public static class DeepEquals
    {
        public class InequalityException : AssertFailedException
        {
            public struct FailureReasons
            {
                public const string CircularReferenceInequality = "Circular Reference to a checked object found to differ from compared reference.";
                public const string CountMismatch = "Counts are not equal";
                public const string SingleValueIsNull = "Only one value is null";
                public const string TypeMismatch = "Type Mismatch";
                public const string ValueInequality = "Values are not equal";
            }

            public string FailureReason { get; }
            public object FirstValue { get; }
            public object SecondValue { get; }

            internal InequalityException(object firstValue, object secondValue, string failureReason, string message)
                : base(message)
            {
                FirstValue = firstValue;
                SecondValue = secondValue;
                FailureReason = failureReason;
            }
        }

        public static void AssertAreEqual(object obj1, object obj2)
        {
            if (obj1 == null && obj2 == null)
            {
                return;
            }

            if (obj1 is null || obj2 is null)
            {
                throw new InequalityException(obj1, obj2, InequalityException.FailureReasons.SingleValueIsNull,
                    MessageBuilder.BuildMessageForNullInequality(obj1, obj2));
            }

            if (!obj1.GetType().Equals(obj2.GetType()))
            {
                throw new InequalityException(obj1, obj2, InequalityException.FailureReasons.TypeMismatch,
                    MessageBuilder.BuildMessageForTypeInequality(obj1, obj2));
            }

            Type type = obj1.GetType();

            if (type.IsPrimitive || typeof(string) == type)
            {
                if (!obj1.Equals(obj2))
                {
                    string message = MessageBuilder.BuildMessageForValueInequality(obj1, obj2);

                    throw new InequalityException(obj1, obj2, InequalityException.FailureReasons.ValueInequality,
                        message);
                }

                return;
            }

            AssertAreEqual(obj1, obj2, obj1.GetType().Name, new Dictionary<Object, Object>());
        }

        private static class MessageBuilder
        {
            public static string BuildMessageForCircularReferenceInequality(object obj1, object obj2) => $"{InequalityException.FailureReasons.CircularReferenceInequality}\nValue from obj1: {obj1}\nValue from obj2: {obj2}";
            public static string BuildMessageForCircularReferenceInequality(object obj1, object obj2, string path) => $"{InequalityException.FailureReasons.CircularReferenceInequality} at {path}\nValue from obj1: {obj1}\nValue from obj2: {obj2}";

            public static string BuildMessageForCountMismatch(string path) => $"{InequalityException.FailureReasons.CountMismatch} at {path}";

            public static string BuildMessageForNullInequality(object obj1, object obj2) => $"{InequalityException.FailureReasons.SingleValueIsNull}\nValue of obj1: {obj1 ?? "null"}\nValue of obj2: {obj2 ?? "null"}";
            public static string BuildMessageForNullInequality(object obj1, object obj2, string path) => $"{InequalityException.FailureReasons.SingleValueIsNull} at {path}\nValue from obj1: {obj1 ?? "null"}\nValue from obj2: {obj2 ?? "null"}";

            public static string BuildMessageForTypeInequality(object obj1, object obj2) => $"{InequalityException.FailureReasons.TypeMismatch}\nType of obj1: {obj1.GetType().Name}\nType of obj2: {obj2.GetType().Name}";
            public static string BuildMessageForTypeInequality(object obj1, object obj2, string path) => $"{InequalityException.FailureReasons.TypeMismatch} at {path}\nType of obj1: {obj1.GetType().Name}\nType of obj2: {obj2.GetType().Name}";

            public static string BuildMessageForValueInequality(object obj1, object obj2) => $"{InequalityException.FailureReasons.ValueInequality}\nValue of obj1: {obj1}\nValue of obj2: {obj2}";
            public static string BuildMessageForValueInequality(object obj1, object obj2, string path) => $"{InequalityException.FailureReasons.ValueInequality} at {path}\nValue from obj1: {obj1}\nValue from obj2: {obj2}";
        }
        private static void AssertAreEqual(object obj1, object obj2, string path, Dictionary<object, object> compared)
        {
            if (obj1 == null && obj2 == null)
            {
                return;
            }

            if (obj1 is null || obj2 is null)
            {
                throw new InequalityException(obj1, obj2, InequalityException.FailureReasons.SingleValueIsNull,
                    MessageBuilder.BuildMessageForNullInequality(obj1, obj2, path));
            }

            if (!obj1.GetType().Equals(obj2.GetType()))
            {
                throw new InequalityException(obj1, obj2, InequalityException.FailureReasons.TypeMismatch,
                    MessageBuilder.BuildMessageForTypeInequality(obj1, obj2, path));
            }

            Type type = obj1.GetType();

            if (type.IsPrimitive || typeof(string) == type)
            {
                if (!obj1.Equals(obj2))
                {
                    string message = MessageBuilder.BuildMessageForValueInequality(obj1, obj2, path);

                    throw new InequalityException(obj1, obj2, InequalityException.FailureReasons.ValueInequality, message);
                }

                return;
            }

            if (compared.TryGetValue(obj1, out object c))
            {
                if (object.ReferenceEquals(c, obj2))
                {
                    return;
                }

                throw new InequalityException(obj1, obj2, InequalityException.FailureReasons.CircularReferenceInequality,
                    MessageBuilder.BuildMessageForCircularReferenceInequality(obj1, obj2, path));
            }

            compared.Add(obj1, obj2);

            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                IEnumerator first = (obj1 as IEnumerable).GetEnumerator();
                IEnumerator second = (obj2 as IEnumerable).GetEnumerator();

                bool isMoreInFirst = first.MoveNext();
                bool isMoreInSecond = second.MoveNext();
                int index = 0;
                while (isMoreInFirst || isMoreInSecond)
                {
                    string subpath = path + "[" + index + "]";
                    if (!isMoreInFirst || !isMoreInSecond)
                    {
                        throw new InequalityException(obj1, obj2, InequalityException.FailureReasons.CountMismatch,
                            MessageBuilder.BuildMessageForCountMismatch(path));
                    }

                    AssertAreEqual(first.Current, second.Current, subpath, compared);
                    index++;
                    isMoreInFirst = first.MoveNext();
                    isMoreInSecond = second.MoveNext();
                }
            }

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                AssertAreEqual(property.GetValue(obj1), property.GetValue(obj2), string.IsNullOrEmpty(path) ? type.Name : path + "." + property.Name, compared);
            }

            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                AssertAreEqual(field.GetValue(obj1), field.GetValue(obj2), string.IsNullOrEmpty(path) ? type.Name : path + "." + field.Name, compared);
            }
        }
    }
}
