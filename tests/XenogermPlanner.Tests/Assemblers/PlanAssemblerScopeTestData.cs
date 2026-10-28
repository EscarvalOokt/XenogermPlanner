using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;
using XenogermPlanner.Assemblers;
using XenogermPlanner.Tests.Genes;

namespace XenogermPlanner.Tests.Assemblers
{
    internal static class PlanAssemblerScopeTestData
    {
        internal static Building CreateOrdinaryBuilding()
        {
            return CreateUninitialized<Building>();
        }

        internal static Genepack CreateGenepack()
        {
            return GenepackInventoryTestData.CreateGenepack();
        }

        internal static FacilityFixture CreateFacility(params Genepack[] genepacks)
        {
            return new FacilityFixture(true, genepacks);
        }

        internal static FacilityFixture CreateFacilityWithoutContainer(params Genepack[] genepacks)
        {
            return new FacilityFixture(false, genepacks);
        }

        internal static AssemblerFixture CreateAssembler(params FacilityFixture[] connectedFacilities)
        {
            return new AssemblerFixture(connectedFacilities);
        }

        internal static IReadOnlyList<Building_GeneAssembler> GetSelectableAssemblers(params Building[] buildings)
        {
            return PlanAssemblerScopeScanner.GetSelectableAssemblers(buildings);
        }

        internal static IReadOnlyList<Genepack> Scan(AssemblerFixture assemblerFixture)
        {
            return ScanSnapshot(assemblerFixture).VisibleGenepacks;
        }

        internal static PlanAssemblerScopeSnapshot ScanSnapshot(AssemblerFixture assemblerFixture)
        {
            if (assemblerFixture == null)
            {
                throw new ArgumentNullException(nameof(assemblerFixture));
            }

            return PlanAssemblerScopeScanner.Scan(
                assemblerFixture.Assembler,
                // Reject a different assembler to verify the scanner's input.
                // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
                assembler =>
                {
                    if (!ReferenceEquals(assembler, assemblerFixture.Assembler))
                    {
                        throw new InvalidOperationException("Assembler is not associated with the test fixture.");
                    }

                    return assemblerFixture.ConnectedFacilities.Select(fixture => fixture.Facility);
                },
                facility =>
                {
                    FacilityFixture fixture = FindFacilityFixture(assemblerFixture.ConnectedFacilities, facility);

                    return fixture.HasGenepackContainer ? fixture.ContainedGenepacks : null;
                },
                facility => FindFacilityFixture(assemblerFixture.ConnectedFacilities, facility).PowerOn);
        }

        private static FacilityFixture FindFacilityFixture(IEnumerable<FacilityFixture> fixtures, Thing facility)
        {
            foreach (FacilityFixture fixture in fixtures)
            {
                if (ReferenceEquals(fixture.Facility, facility))
                {
                    return fixture;
                }
            }

            throw new InvalidOperationException("Facility is not associated with a test fixture.");
        }

        private static T CreateUninitialized<T>() where T : class
        {
            return (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        }

        internal sealed class FacilityFixture
        {
            internal FacilityFixture(bool hasGenepackContainer, IEnumerable<Genepack> containedGenepacks)
            {
                Facility = CreateUninitialized<ThingWithComps>();
                HasGenepackContainer = hasGenepackContainer;
                PowerOn = true;
                ContainedGenepacks = containedGenepacks == null ? [] : [..containedGenepacks];
            }

            internal Thing Facility { get; }
            internal bool HasGenepackContainer { get; set; }
            internal bool PowerOn { get; set; }
            internal List<Genepack> ContainedGenepacks { get; }
        }

        internal sealed class AssemblerFixture
        {
            internal AssemblerFixture(IEnumerable<FacilityFixture> connectedFacilities)
            {
                Assembler = CreateUninitialized<Building_GeneAssembler>();
                ConnectedFacilities = connectedFacilities == null ? [] : [..connectedFacilities];
            }

            internal Building_GeneAssembler Assembler { get; }
            internal List<FacilityFixture> ConnectedFacilities { get; }
        }
    }
}