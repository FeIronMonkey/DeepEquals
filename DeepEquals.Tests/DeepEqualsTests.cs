using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;

namespace DeepEquals.Tests
{
    [TestClass]
    public class DeepEqualsTests
    {

        protected class Parent
        {
            public int PublicParentProperty { get; set; }
        }

        protected class FirstChild : Parent
        {
            public FirstChild() { }

            public FirstChild(bool privateValue)
            {
                this.PrivateChildProperty = privateValue;
            }

            public string PublicChildProperty { get; set; }
            private bool PrivateChildProperty { get; set; }
        }

        protected class SecondChild : Parent { }

        protected class WithList
        {
            public List<FirstChild> List { get; set; }
        }

        public class FieldsWithoutAccessorMethods
        {
            public FieldsWithoutAccessorMethods() { }

            public FieldsWithoutAccessorMethods(string privateField)
            {
                this.privateField = privateField;
            }

            public string publicField;
            private string privateField;
            public string HasAccessorMethods { get; set; }
        }

        public class LightLinkedList
        {
            public LightLinkedList Next { get; set; }
            public string Name { get; set; }
        }

        public class EnumerableLightLinkedList : LightLinkedList, IEnumerable<string>
        {
            public string ExtraValue { get; set; }

            public IEnumerator<string> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

            public class Enumerator : IEnumerator<string>
            {
                public Enumerator(LightLinkedList firstNode)
                {
                    this.firstNode = firstNode;
                    this.currentNode = new LightLinkedList
                    {
                        Next = firstNode,
                    };
                }

                private LightLinkedList firstNode;
                private LightLinkedList currentNode;

                public string Current()
                {
                    if (this.currentNode is null
                        || object.ReferenceEquals(this.currentNode.Next, this.firstNode))
                    {
                        throw new InvalidOperationException();
                    }

                    return currentNode.Name;
                }

                string IEnumerator<string>.Current => this.Current();
                object IEnumerator.Current => this.Current();

                public void Dispose() { }

                public bool MoveNext()
                {
                    this.currentNode = this.currentNode.Next;
                    return !(this.currentNode is null);
                }

                public void Reset() => this.currentNode = new LightLinkedList
                {
                    Next = firstNode,
                };
            }
        }

        public class DifferentReferenceFromStaticPropertyEachTime
        {
            public int publicField;
            public int publicProperty { get; set; }
            public static DifferentReferenceFromStaticPropertyEachTime GetNewInstance => new DifferentReferenceFromStaticPropertyEachTime();
        }

        [TestClass]
        public class AssertAreEqual : DeepEqualsTests
        {
            [TestMethod]
            [DataRow(1, 1),
             DataRow(0, 0),
             DataRow(-5, -5)]
            [DataRow(1.3f, 1.3f),
             DataRow(0f, 0f),
             DataRow(-5.1f, -5.1f)]
            [DataRow('c', 'c'),
             DataRow((char)35, (char)35)]
            [DataRow(true, true),
             DataRow(false, false)]
            public void ShouldNotThrowIfValueTypesAreEqual(object a, object b)
            {
                DeepEquals.AssertAreEqual(a, b);
            }

            [TestMethod]
            public void ShouldNotThrowIfGuidsAreEqual()
            {
                Guid a = new Guid("11112222-3333-4444-5555-666677778888");
                Guid b = new Guid("11112222-3333-4444-5555-666677778888");

                DeepEquals.AssertAreEqual(a, b);
            }

            [TestMethod]
            [DataRow("aaaaaaaa",
                     "aaaaaaaa")]
            [DataRow("....::::",
                     "....::::")]
            public void ShouldNotThrowIfStringsAreEqual(string a, string b)
            {
                DeepEquals.AssertAreEqual(a, b);
            }

            [TestMethod]
            [DataRow(1, 0),
             DataRow(0, 1),
             DataRow(-31, 31)]
            [DataRow(1.35f, 1.3f),
             DataRow(0f, 0.00000000001f),
             DataRow(-5.2f, -5.1f),
             DataRow(-5.1f, 5.1f)]
            [DataRow('c', 'd'),
             DataRow((char)34, (char)35)]
            [DataRow(true, false),
             DataRow(false, true)]
            public void ShouldThrowIfValueTypesAreNotEqual(object a, object b)
            {
                DeepEquals.InequalityException exception = Assert
                    .ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(a, b));

                Assert.AreEqual(a, exception.FirstValue);
                Assert.AreEqual(b, exception.SecondValue);
            }

            [TestMethod]
            [DataRow("aaaaaaaM",
                     "aaaaaaaa")]
            [DataRow("....::::",
                     "........")]
            public void ShouldThrowIfStringsAreNotEqual(string a, string b)
            {
                DeepEquals.InequalityException exception = Assert
                    .ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(a, b));

                Assert.AreEqual(a, exception.FirstValue);
                Assert.AreEqual(b, exception.SecondValue);
            }

            [TestMethod]
            public void ShouldThrowIfTypeIsDifferent()
            {
                Assert.ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(1, 1f));
                Assert.ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(1.0, 1f));
                Assert.ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(1.0, "1.0"));
                Assert.ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(new object(), "1.0"));
                Assert.ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(new Parent(), new FirstChild()));
                Assert.ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(new FirstChild(), new SecondChild()));
            }

            [TestMethod]
            public void ShouldNotThrowIfStaticPropertyReturnsDifferentValues()
            {
                DifferentReferenceFromStaticPropertyEachTime instanceA = new DifferentReferenceFromStaticPropertyEachTime();
                DifferentReferenceFromStaticPropertyEachTime instanceB = new DifferentReferenceFromStaticPropertyEachTime();

                DeepEquals.AssertAreEqual(instanceA, instanceB);
            }

            [TestMethod]
            public void ShouldProvideFailureReasonIfNotEqual()
            {
                DeepEquals.InequalityException exception = Assert
                    .ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(1, 1f));

                Assert.AreEqual(
                    DeepEquals.InequalityException.FailureReasons.TypeMismatch,
                    exception.FailureReason);

                exception = Assert
                    .ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(new object(), null));

                Assert.AreEqual(
                    DeepEquals.InequalityException.FailureReasons.SingleValueIsNull,
                    exception.FailureReason);


                exception = Assert
                    .ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(2, 3));

                Assert.AreEqual(
                    DeepEquals.InequalityException.FailureReasons.ValueInequality,
                    exception.FailureReason);


                exception = Assert
                    .ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(new List<int> { 1, 2, 3 }, new List<int> { 1, 2 }));

                Assert.AreEqual(
                    DeepEquals.InequalityException.FailureReasons.CountMismatch,
                    exception.FailureReason);


                exception = Assert
                    .ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(new List<int> { 1, 2 }, new List<int> { 1, 2, 3 }));

                Assert.AreEqual(
                    DeepEquals.InequalityException.FailureReasons.CountMismatch,
                    exception.FailureReason);


                exception = Assert
                    .ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(new int[4] { 1, 2, 3, 4 }, new int[3] { 1, 2, 3 }));

                Assert.AreEqual(
                    DeepEquals.InequalityException.FailureReasons.CountMismatch,
                    exception.FailureReason);


                exception = Assert
                    .ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(new int[3] { 1, 2, 3 }, new int[4] { 1, 2, 3, 4 }));

                Assert.AreEqual(
                    DeepEquals.InequalityException.FailureReasons.CountMismatch,
                    exception.FailureReason);
            }

            [TestMethod]
            public void ShouldThrowIfOnlyFirstIsNull()
            {
                object secondArgument = new object();
                DeepEquals.InequalityException exception = Assert
                    .ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(null, secondArgument));

                Assert.AreEqual(null, exception.FirstValue);
                Assert.AreEqual(secondArgument, exception.SecondValue);
            }

            [TestMethod]
            public void ShouldThrowIfOnlySecondIsNull()
            {
                object firstArgument = new object();
                DeepEquals.InequalityException exception = Assert
                    .ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(firstArgument, null));

                Assert.AreEqual(firstArgument, exception.FirstValue);
                Assert.AreEqual(null, exception.SecondValue);
            }

            [TestMethod]
            public void ShouldNotThrowIfBothAreNull()
            {
                DeepEquals.AssertAreEqual(null, null);
            }

            [TestMethod]
            public void ShouldThrowIfElementsInCollectionAreNotEqual()
            {
                DeepEquals.InequalityException exception = Assert
                    .ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(new int[4] { 1, 2, 3, 5 }, new int[4] { 1, 2, 3, 4 }));

                Assert.AreEqual(
                    DeepEquals.InequalityException.FailureReasons.ValueInequality,
                    exception.FailureReason);
            }

            [TestMethod]
            public void ShouldNotThrowIfElementsInCollectionAreEqual()
            {
                DeepEquals.AssertAreEqual(new int[4] { 1, 2, 3, 4 }, new int[4] { 1, 2, 3, 4 });
            }

            [TestMethod]
            public void ShouldThrowIfObjectPropertiesAreNotEqual()
            {
                FirstChild a = new FirstChild
                {
                    PublicChildProperty = "a",
                };

                FirstChild b = new FirstChild
                {
                    PublicChildProperty = "b",
                };

                DeepEquals.InequalityException exception = Assert
                    .ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(a, b));

                Assert.AreEqual(
                    DeepEquals.InequalityException.FailureReasons.ValueInequality,
                    exception.FailureReason);


                a = new FirstChild
                {
                    PublicParentProperty = 98,
                };

                b = new FirstChild
                {
                    PublicParentProperty = 97,
                };

                exception = Assert
                    .ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(a, b));

                Assert.AreEqual(
                    DeepEquals.InequalityException.FailureReasons.ValueInequality,
                    exception.FailureReason);
            }

            [TestMethod]
            public void ShouldNotThrowIfObjectPropertiesAreEqual()
            {
                FirstChild a = new FirstChild(true)
                {
                    PublicChildProperty = "a",
                    PublicParentProperty = 98,
                };

                FirstChild b = new FirstChild(true)
                {
                    PublicChildProperty = "a",
                    PublicParentProperty = 98,
                };

                DeepEquals.AssertAreEqual(a, b);


                a = new FirstChild(true);

                b = new FirstChild(false);

                DeepEquals.AssertAreEqual(a, b);
            }

            [TestMethod]
            public void ShouldNotThrowIfPrivatePropertiesAreTheOnlyDifference()
            {
                FirstChild a = new FirstChild(true);

                FirstChild b = new FirstChild(false);

                DeepEquals.AssertAreEqual(a, b);
            }

            [TestMethod]
            public void ShouldThrowExceptionWithErrorInfoInMessage()
            {
                FirstChild a = new FirstChild
                {
                    PublicChildProperty = "this is a good string",
                };

                FirstChild b = new FirstChild
                {
                    PublicChildProperty = "this is different",
                };

                DeepEquals.InequalityException exception = Assert
                    .ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(a, b));

                Assert.AreEqual($"{DeepEquals.InequalityException.FailureReasons.ValueInequality} at {nameof(FirstChild)}.{nameof(FirstChild.PublicChildProperty)}\nValue from obj1: {a.PublicChildProperty}\nValue from obj2: {b.PublicChildProperty}", exception.Message);
            }

            [TestMethod]
            public void ShouldSpecifyMultilevelPathInExceptionMessage()
            {
                LightLinkedList list1 = new LightLinkedList
                {
                    Next = new LightLinkedList
                    {
                        Next = new LightLinkedList
                        {
                            Next = new LightLinkedList
                            {
                                Name = "innermost next class",
                            }
                        }
                    }
                };

                LightLinkedList list2 = new LightLinkedList
                {
                    Next = new LightLinkedList
                    {
                        Next = new LightLinkedList
                        {
                            Next = new LightLinkedList
                            {
                                Name = "other innermost next class",
                            }
                        }
                    }
                };

                DeepEquals.InequalityException exception = Assert
                    .ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(list1, list2));

                Assert.AreEqual($"{DeepEquals.InequalityException.FailureReasons.ValueInequality} at {nameof(LightLinkedList)}.{nameof(LightLinkedList.Next)}.{nameof(LightLinkedList.Next)}.{nameof(LightLinkedList.Next)}.{nameof(LightLinkedList.Name)}\nValue from obj1: {list1.Next.Next.Next.Name}\nValue from obj2: {list2.Next.Next.Next.Name}", exception.Message);
            }

            [TestMethod]
            public void ShouldSpecifyMultilevelPathInExceptionMessageToWherePropertyFromSecondItemIsNull()
            {
                LightLinkedList list1 = new LightLinkedList
                {
                    Next = new LightLinkedList
                    {
                        Name = "innermost next class",
                    }
                };

                LightLinkedList list2 = new LightLinkedList
                {
                    Next = new LightLinkedList
                    {
                        Name = null,
                    }
                };

                DeepEquals.InequalityException exception = Assert
                    .ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(list1, list2));

                Assert.AreEqual($"{DeepEquals.InequalityException.FailureReasons.SingleValueIsNull} at {nameof(LightLinkedList)}.{nameof(LightLinkedList.Next)}.{nameof(LightLinkedList.Name)}\nValue from obj1: {list1.Next.Name}\nValue from obj2: null", exception.Message);
            }

            [TestMethod]
            public void ShouldSpecifyMultilevelPathInExceptionMessageToWherePropertyFromFirstItemIsNull()
            {
                LightLinkedList list1 = new LightLinkedList
                {
                    Next = new LightLinkedList
                    {
                        Name = null,
                    }
                };

                LightLinkedList list2 = new LightLinkedList
                {
                    Next = new LightLinkedList
                    {
                        Name = "innermost next class",
                    }
                };

                DeepEquals.InequalityException exception = Assert
                    .ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(list1, list2));

                Assert.AreEqual(
                    $"{DeepEquals.InequalityException.FailureReasons.SingleValueIsNull} at {nameof(LightLinkedList)}.{nameof(LightLinkedList.Next)}.{nameof(LightLinkedList.Name)}\nValue from obj1: null\nValue from obj2: {list2.Next.Name}",
                    exception.Message);
            }

            [TestMethod]
            public void ShouldNotGetCaughtInCyclycalReferences()
            {
                LightLinkedList list1 = new LightLinkedList
                {
                    Next = new LightLinkedList
                    {
                        Name = null,
                    }
                };

                list1.Next.Next = list1;

                LightLinkedList list2 = new LightLinkedList
                {
                    Next = new LightLinkedList
                    {
                        Name = null,
                    }
                };

                list2.Next.Next = list2;

                DeepEquals.AssertAreEqual(list1, list2);
            }

            [TestMethod]
            public void ShouldThrowWhenCyclicalReferencesAreNotEqual()
            {
                LightLinkedList list1 = new LightLinkedList
                {
                    Next = new LightLinkedList
                    {
                        Name = null,
                    }
                };

                list1.Next.Next = list1;

                LightLinkedList list2 = new LightLinkedList
                {
                    Next = new LightLinkedList
                    {
                        Name = null,
                    }
                };

                list2.Next.Next = new LightLinkedList
                {
                    Next = list2,
                };

                DeepEquals.InequalityException exception = Assert
                   .ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(list1, list2));

                Assert.AreEqual($"{DeepEquals.InequalityException.FailureReasons.CircularReferenceInequality} at {nameof(LightLinkedList)}.{nameof(LightLinkedList.Next)}.{nameof(LightLinkedList.Next)}\nValue from obj1: {list1.Next.Next}\nValue from obj2: {list2.Next.Next}", exception.Message);
            }

            [TestMethod]
            public void ShouldThrowWhenEnumerableHasUniqueUnenumeratedValue()
            {
                EnumerableLightLinkedList list1 = new EnumerableLightLinkedList
                {
                    ExtraValue = "first extra value",
                    Name = "first name",
                    Next = new LightLinkedList
                    {
                        Name = "second name",
                        Next = new LightLinkedList
                        {
                            Name = "third name",
                        }
                    }
                };

                EnumerableLightLinkedList list2 = new EnumerableLightLinkedList
                {
                    ExtraValue = "OTHER VALUE",
                    Name = "first name",
                    Next = new LightLinkedList
                    {
                        Name = "second name",
                        Next = new LightLinkedList
                        {
                            Name = "third name",
                        }
                    }
                };

                DeepEquals.InequalityException exception = Assert
                   .ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(list1, list2));

                Assert.AreEqual($"{DeepEquals.InequalityException.FailureReasons.ValueInequality} at {nameof(EnumerableLightLinkedList)}.{nameof(EnumerableLightLinkedList.ExtraValue)}\nValue from obj1: {list1.ExtraValue}\nValue from obj2: {list2.ExtraValue}", exception.Message);
            }

            [TestMethod]
            public void ShouldThrowWhenPublicFieldWithoutAccessorMethodIsDifferent()
            {
                FieldsWithoutAccessorMethods a = new FieldsWithoutAccessorMethods
                {
                    publicField = "same",
                    HasAccessorMethods = "other same",
                };

                FieldsWithoutAccessorMethods b = new FieldsWithoutAccessorMethods
                {
                    publicField = "different",
                    HasAccessorMethods = "other same",
                };

                DeepEquals.InequalityException exception = Assert
                   .ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(a, b));

                Assert.AreEqual($"{DeepEquals.InequalityException.FailureReasons.ValueInequality} at {nameof(FieldsWithoutAccessorMethods)}.{nameof(FieldsWithoutAccessorMethods.publicField)}\nValue from obj1: {a.publicField}\nValue from obj2: {b.publicField}", exception.Message);
            }

            [TestMethod]
            public void ShouldNotThrowWhenPrivateFieldWithoutAccessorMethodIsDifferent()
            {
                FieldsWithoutAccessorMethods a = new FieldsWithoutAccessorMethods("same");

                FieldsWithoutAccessorMethods b = new FieldsWithoutAccessorMethods("different");

                DeepEquals.AssertAreEqual(a, b);
            }

            [TestMethod]
            public void ShouldIncludeIndexOfEnumerableInPath()
            {
                WithList a = new WithList
                {
                    List = new List<FirstChild>
                    {
                        new FirstChild
                        {
                            PublicChildProperty = "first",
                        },
                        new FirstChild
                        {
                            PublicChildProperty = "second",
                        },
                        new FirstChild
                        {
                            PublicChildProperty = "third",
                        },
                        new FirstChild
                        {
                            PublicChildProperty = "fourth",
                        },
                    },
                };

                WithList b = new WithList
                {
                    List = new List<FirstChild>
                    {
                        new FirstChild
                        {
                            PublicChildProperty = "first",
                        },
                        new FirstChild
                        {
                            PublicChildProperty = "second",
                        },
                        new FirstChild
                        {
                            PublicChildProperty = "-- DIFFERENT --",
                        },
                        new FirstChild
                        {
                            PublicChildProperty = "fourth",
                        },
                    },
                };

                DeepEquals.InequalityException exception = Assert
                   .ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(a, b));

                Assert.AreEqual($"{DeepEquals.InequalityException.FailureReasons.ValueInequality} at {nameof(WithList)}.{nameof(WithList.List)}[2].{nameof(FirstChild.PublicChildProperty)}\nValue from obj1: {a.List[2].PublicChildProperty}\nValue from obj2: {b.List[2].PublicChildProperty}", exception.Message);
            }

            [TestMethod]
            public void ShouldIncludePathToEnumerableWithDifferentCountsInExceptionMessage()
            {
                WithList a = new WithList
                {
                    List = new List<FirstChild>
                    {
                        new FirstChild
                        {
                            PublicChildProperty = "first",
                        },
                        new FirstChild
                        {
                            PublicChildProperty = "second",
                        },
                        new FirstChild
                        {
                            PublicChildProperty = "third",
                        },
                        new FirstChild
                        {
                            PublicChildProperty = "fourth",
                        },
                    },
                };

                WithList b = new WithList
                {
                    List = new List<FirstChild>
                    {
                        new FirstChild
                        {
                            PublicChildProperty = "first",
                        },
                        new FirstChild
                        {
                            PublicChildProperty = "second",
                        },
                        new FirstChild
                        {
                            PublicChildProperty = "third",
                        },
                    },
                };

                DeepEquals.InequalityException exception = Assert
                   .ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(a, b));

                Assert.AreEqual($"{DeepEquals.InequalityException.FailureReasons.CountMismatch} at {nameof(WithList)}.{nameof(WithList.List)}", exception.Message);
            }

            [TestMethod]
            public void ShouldNotThrowWhenObjectsOfAnonymousTypesHaveEqualProperties()
            {
                var a = new { banana = "yellow", apple = "red" };

                var b = new { banana = "yellow", apple = "red" };

                DeepEquals.AssertAreEqual(a, b);
            }

            [TestMethod]
            public void ShouldThrowWhenObjectsOfAnonymousTypesHaveSameButUnequalProperties()
            {
                var a = new { banana = "yellow", apple = "red" };

                var b = new { banana = "yellow", apple = "yellow" };

                DeepEquals.InequalityException exception = Assert
                   .ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(a, b));

                Type type = a.GetType();

                Assert.AreEqual($"{DeepEquals.InequalityException.FailureReasons.ValueInequality} at {type.Name}.{nameof(a.apple)}\nValue from obj1: {a.apple}\nValue from obj2: {b.apple}", exception.Message);
            }

            [TestMethod]
            public void ShouldSpecifyTypesWhenArgumentsHaveDifferentTypes()
            {
                FirstChild a = new FirstChild();

                SecondChild b = new SecondChild();

                DeepEquals.InequalityException exception = Assert
                   .ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(a, b));

                Assert.AreEqual($"{DeepEquals.InequalityException.FailureReasons.TypeMismatch}\nType of obj1: {nameof(FirstChild)}\nType of obj2: {nameof(SecondChild)}", exception.Message);
            }

            [TestMethod]
            public void ShouldThrowWhenObjectsOfAnonymousTypesHaveDifferentProperties()
            {
                var a = new { banana = "yellow", apple = "red" };

                var b = new { banana = "yellow", apple = "red", pear = "green" };

                DeepEquals.InequalityException exception = Assert
                   .ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(a, b));

                Type type = a.GetType();

                Assert.AreEqual($"{DeepEquals.InequalityException.FailureReasons.TypeMismatch}\nType of obj1: {a.GetType().Name}\nType of obj2: {b.GetType().Name}", exception.Message);
            }

            [TestMethod]
            public void ShouldNotIncludePathIfSingleValueIsNull()
            {
                FirstChild a = new FirstChild();
                FirstChild b = null;

                DeepEquals.InequalityException exception = Assert
                   .ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(a, b));

                Assert.AreEqual($"{DeepEquals.InequalityException.FailureReasons.SingleValueIsNull}\nValue of obj1: {a}\nValue of obj2: null", exception.Message);
            }

            [TestMethod]
            public void ShouldNotIncludePathIfComparedObjectsArePrimitives()
            {
                int a = 1;
                int b = 2;

                DeepEquals.InequalityException exception = Assert
                   .ThrowsException<DeepEquals.InequalityException>(() => DeepEquals.AssertAreEqual(a, b));

                Assert.AreEqual($"{DeepEquals.InequalityException.FailureReasons.ValueInequality}\nValue of obj1: {a}\nValue of obj2: {b}", exception.Message);
            }
        }
    }
}
