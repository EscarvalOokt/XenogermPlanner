using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using XenogermPlanner.Api;

namespace XenogermPlanner.Tests.Api
{
    [TestFixture]
    public sealed class XenogermPlannerApiContractTests
    {
        private const string FacadeAssemblyQualifiedName = "XenogermPlanner.Api.XenogermPlannerApi, XenogermPlanner";

        [Test]
        public void Facade_CanBeResolvedByStableAssemblyQualifiedName()
        {
            var facadeType = Type.GetType(FacadeAssemblyQualifiedName, throwOnError: false);

            Assert.That(facadeType, Is.EqualTo(typeof(XenogermPlannerApi)));
        }

        [Test]
        public void ApiVersion_IsPublicStaticAndIndependentFromModVersion()
        {
            PropertyInfo property = typeof(XenogermPlannerApi).GetProperty(
                nameof(XenogermPlannerApi.ApiVersion),
                BindingFlags.Public | BindingFlags.Static);

            Assert.That(property, Is.Not.Null);
            Assert.That(property.PropertyType, Is.EqualTo(typeof(int)));
            Assert.That(property.GetValue(null), Is.EqualTo(1));
        }

        [Test]
        public void QueryMethod_HasStablePublicStaticBatchSignature()
        {
            MethodInfo method = typeof(XenogermPlannerApi).GetMethod(
                nameof(XenogermPlannerApi.QueryGenepackRelevance),
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: [typeof(IReadOnlyList<GenepackRelevanceRequest>)],
                modifiers: null);

            Assert.That(method, Is.Not.Null);
            Assert.That(method.ReturnType, Is.EqualTo(typeof(GenepackRelevanceBatchResult)));
        }

        [Test]
        public void PublicRequestAndMatchTypes_HaveStableConstructors()
        {
            ConstructorInfo requestConstructor = typeof(GenepackRelevanceRequest).GetConstructor(
            [
                typeof(IEnumerable<string>)
            ]);

            ConstructorInfo matchConstructor = typeof(GenepackRelevancePlanMatch).GetConstructor(
            [
                typeof(string), typeof(string)
            ]);

            Assert.That(requestConstructor, Is.Not.Null);
            Assert.That(matchConstructor, Is.Not.Null);
        }

        [Test]
        public void PublicDtoProperties_HaveStableNamesAndTypes()
        {
            AssertPublicInstanceProperty(
                typeof(GenepackRelevanceRequest),
                nameof(GenepackRelevanceRequest.GeneDefNames),
                typeof(IReadOnlyList<string>));

            AssertPublicInstanceProperty(
                typeof(GenepackRelevancePlanMatch),
                nameof(GenepackRelevancePlanMatch.PlanId),
                typeof(string));

            AssertPublicInstanceProperty(
                typeof(GenepackRelevancePlanMatch),
                nameof(GenepackRelevancePlanMatch.DisplayName),
                typeof(string));

            AssertPublicInstanceProperty(
                typeof(GenepackRelevanceItemResult),
                nameof(GenepackRelevanceItemResult.Status),
                typeof(GenepackRelevanceItemStatus));

            AssertPublicInstanceProperty(
                typeof(GenepackRelevanceItemResult),
                nameof(GenepackRelevanceItemResult.Matches),
                typeof(IReadOnlyList<GenepackRelevancePlanMatch>));

            AssertPublicInstanceProperty(
                typeof(GenepackRelevanceBatchResult),
                nameof(GenepackRelevanceBatchResult.Status),
                typeof(GenepackRelevanceBatchStatus));

            AssertPublicInstanceProperty(
                typeof(GenepackRelevanceBatchResult),
                nameof(GenepackRelevanceBatchResult.UnavailableReason),
                typeof(GenepackRelevanceUnavailableReason));

            AssertPublicInstanceProperty(
                typeof(GenepackRelevanceBatchResult),
                nameof(GenepackRelevanceBatchResult.Results),
                typeof(IReadOnlyList<GenepackRelevanceItemResult>));
        }

        [Test]
        public void PublicEnums_HaveStableVersionOneNamesAndValues()
        {
            AssertEnumContract(
                typeof(GenepackRelevanceBatchStatus),
                ["Success", "InvalidRequest", "Unavailable", "Failed"],
                [0, 1, 2, 3]);

            AssertEnumContract(
                typeof(GenepackRelevanceItemStatus),
                ["Success", "InvalidInput", "UnknownGeneDef", "Failed"],
                [0, 1, 2, 3]);

            AssertEnumContract(
                typeof(GenepackRelevanceUnavailableReason),
                ["None", "NoGame", "NoActiveMap", "PlannerStateUnavailable"],
                [0, 1, 2, 3]);
        }

        [Test]
        public void SoftBinding_CanReadVersionBeforeResolvingAndInvokingQuery()
        {
            var facadeType = Type.GetType(FacadeAssemblyQualifiedName, throwOnError: false);
            Assert.That(facadeType, Is.Not.Null);

            PropertyInfo versionProperty = facadeType.GetProperty(
                nameof(XenogermPlannerApi.ApiVersion),
                BindingFlags.Public | BindingFlags.Static);

            Assert.That(versionProperty, Is.Not.Null);
            Assert.That(versionProperty.GetValue(null), Is.EqualTo(1));

            MethodInfo queryMethod = facadeType.GetMethod(
                nameof(XenogermPlannerApi.QueryGenepackRelevance),
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: [typeof(IReadOnlyList<GenepackRelevanceRequest>)],
                modifiers: null);

            Assert.That(queryMethod, Is.Not.Null);

            var result = (GenepackRelevanceBatchResult)queryMethod.Invoke(null, [null]);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Status, Is.EqualTo(GenepackRelevanceBatchStatus.InvalidRequest));
            Assert.That(result.UnavailableReason, Is.EqualTo(GenepackRelevanceUnavailableReason.None));
            Assert.That(result.Results, Is.Empty);
        }

        [Test]
        public void Facade_EmptyBatch_ReturnsSuccessfulEmptyResultWithoutRuntimeContext()
        {
            GenepackRelevanceBatchResult result = XenogermPlannerApi.QueryGenepackRelevance([]);

            Assert.That(result.Status, Is.EqualTo(GenepackRelevanceBatchStatus.Success));
            Assert.That(result.UnavailableReason, Is.EqualTo(GenepackRelevanceUnavailableReason.None));
            Assert.That(result.Results, Is.Empty);
        }

        [Test]
        public void PublicApiSurface_DoesNotExposeRuntimeOrPlannerImplementationTypes()
        {
            Type[] publicApiTypes = typeof(XenogermPlannerApi).Assembly.GetExportedTypes()
                .Where(type => type.Namespace == "XenogermPlanner.Api")
                .ToArray();

            Assert.That(publicApiTypes, Is.Not.Empty);

            foreach (Type publicApiType in publicApiTypes)
            {
                foreach (Type referencedType in GetPublicSurfaceTypes(publicApiType))
                {
                    Assert.That(
                        IsForbiddenType(referencedType),
                        Is.False,
                        $"Public API type '{publicApiType.FullName}' exposes forbidden type " +
                        $"'{referencedType.FullName}'.");
                }
            }
        }

        private static void AssertPublicInstanceProperty(Type declaringType, string propertyName, Type propertyType)
        {
            PropertyInfo property = declaringType.GetProperty(
                propertyName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            Assert.That(property, Is.Not.Null, $"Missing public property '{declaringType.FullName}.{propertyName}'.");
            Assert.That(property.PropertyType, Is.EqualTo(propertyType));
            Assert.That(property.CanRead, Is.True);
            Assert.That(property.GetMethod, Is.Not.Null);
            Assert.That(property.GetMethod.IsPublic, Is.True);
            Assert.That(property.SetMethod, Is.Null);
        }

        private static void AssertEnumContract(Type enumType, string[] expectedNames, int[] expectedValues)
        {
            Assert.That(enumType.IsEnum, Is.True);
            Assert.That(Enum.GetNames(enumType), Is.EqualTo(expectedNames));

            int[] actualValues = Enum.GetValues(enumType).Cast<object>().Select(Convert.ToInt32).ToArray();
            Assert.That(actualValues, Is.EqualTo(expectedValues));
        }

        private static IEnumerable<Type> GetPublicSurfaceTypes(Type type)
        {
            yield return type;

            foreach (ConstructorInfo constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                foreach (ParameterInfo parameter in constructor.GetParameters())
                {
                    foreach (Type referencedType in FlattenType(parameter.ParameterType))
                        yield return referencedType;
                }
            }

            foreach (PropertyInfo property in type.GetProperties(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                foreach (Type referencedType in FlattenType(property.PropertyType))
                    yield return referencedType;
            }

            foreach (MethodInfo method in type.GetMethods(
                         BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (method.IsSpecialName)
                    continue;

                foreach (Type referencedType in FlattenType(method.ReturnType))
                    yield return referencedType;

                foreach (ParameterInfo parameter in method.GetParameters())
                {
                    foreach (Type referencedType in FlattenType(parameter.ParameterType))
                        yield return referencedType;
                }
            }
        }

        private static IEnumerable<Type> FlattenType(Type type)
        {
            if (type.IsArray || type.IsByRef || type.IsPointer)
            {
                foreach (Type referencedType in FlattenType(type.GetElementType()))
                    yield return referencedType;

                yield break;
            }

            yield return type;

            if (!type.IsGenericType)
                yield break;

            foreach (Type genericArgument in type.GetGenericArguments())
            {
                foreach (Type referencedType in FlattenType(genericArgument))
                    yield return referencedType;
            }
        }

        private static bool IsForbiddenType(Type type)
        {
            string typeNamespace = type.Namespace ?? string.Empty;
            bool belongsToPlannerImplementation = typeNamespace == "XenogermPlanner" ||
                                                  typeNamespace.StartsWith(
                                                      "XenogermPlanner.",
                                                      StringComparison.Ordinal) &&
                                                  typeNamespace != "XenogermPlanner.Api";

            return typeNamespace == "Verse" ||
                   typeNamespace.StartsWith("Verse.", StringComparison.Ordinal) ||
                   typeNamespace == "RimWorld" ||
                   typeNamespace.StartsWith("RimWorld.", StringComparison.Ordinal) ||
                   belongsToPlannerImplementation;
        }
    }
}