# Xenogerm Planner integration API

This document is the normative guide for the public Xenogerm Planner integration API version `1`.

The API is a read-only, consumer-neutral boundary for asking whether external genepack compositions are relevant to current Xenogerm Planner plans. It is designed for optional soft binding: a consumer does not need a compile-time reference to Xenogerm Planner and must continue to work when the mod is absent, unavailable or exposes an unsupported API version.

For architecture ownership and testing policy, see [architecture.md](architecture.md) and [testing.md](testing.md).

## Versioning policy

The API version is independent from the Xenogerm Planner mod version.

Current contract:

```text
ApiVersion = 1
```

Version `1` is the current public API contract. `ApiVersion` changes only as part of a released Xenogerm Planner build.

Consumers must support only explicitly known API versions. A consumer that implements version `1` must require:

```text
ApiVersion == 1
```

A value greater than `1` must not be assumed to be backward-compatible automatically.

## Discovery

The public facade is compiled into `XenogermPlanner.dll`.

Stable assembly-qualified type name:

```text
XenogermPlanner.Api.XenogermPlannerApi, XenogermPlanner
```

Public version property:

```csharp
public static int ApiVersion { get; }
```

Public query method:

```csharp
public static GenepackRelevanceBatchResult QueryGenepackRelevance(
    IReadOnlyList<GenepackRelevanceRequest> requests)
```

The expected discovery order is:

1. Resolve the facade by its assembly-qualified name.
2. Read the public static `ApiVersion` property.
3. Continue only when the version is explicitly supported.
4. Resolve the public static `QueryGenepackRelevance` method and public DTO types.
5. Invoke the query and interpret only the documented public result surface.

Expected absence of the facade or an unsupported version is a normal optional-integration state, not an error condition.

## Public types

All public API types are in namespace:

```text
XenogermPlanner.Api
```

The contract contains no public `Thing`, `Genepack`, `GeneDef`, `GeneSet`, `Tradeable`, persistence, readiness-result or UI types. No separate runtime DTO assembly is required.

### `GenepackRelevanceRequest`

```csharp
public sealed class GenepackRelevanceRequest
{
    public GenepackRelevanceRequest(IEnumerable<string> geneDefNames);

    public IReadOnlyList<string> GeneDefNames { get; }
}
```

`GeneDefNames` represents one offered genepack composition through `GeneDef.defName` values.

The constructor makes a defensive copy. Passing a `null` collection throws `ArgumentNullException`. Structural and definition validation of the copied entries occurs when the query is executed.

### `GenepackRelevanceBatchResult`

```csharp
public sealed class GenepackRelevanceBatchResult
{
    public GenepackRelevanceBatchStatus Status { get; }
    public GenepackRelevanceUnavailableReason UnavailableReason { get; }
    public IReadOnlyList<GenepackRelevanceItemResult> Results { get; }
}
```

A successful batch contains one item result for every request, in the same order. Non-successful batches contain no partial item results.

### `GenepackRelevanceItemResult`

```csharp
public sealed class GenepackRelevanceItemResult
{
    public GenepackRelevanceItemStatus Status { get; }
    public IReadOnlyList<GenepackRelevancePlanMatch> Matches { get; }
}
```

Only a successful item can contain matches. A successful item with no relevant plans has an empty `Matches` collection.

### `GenepackRelevancePlanMatch`

```csharp
public sealed class GenepackRelevancePlanMatch
{
    public string PlanId { get; }
    public string DisplayName { get; }
}
```

`PlanId` is the stable programmatic identity. `DisplayName` is the current user-facing normalized plan name and must not replace `PlanId` as identity. The public constructor rejects null, empty or whitespace identity values, although ordinary consumers receive matches from query results rather than constructing them.

## Status values

### Batch status

| Name | Value | Meaning |
|---|---:|---|
| `Success` | `0` | The batch was processed. Inspect each item result. |
| `InvalidRequest` | `1` | The batch reference itself was `null`. |
| `Unavailable` | `2` | The current game, map or Planner state cannot answer the batch. |
| `Failed` | `3` | Batch-level setup failed unexpectedly. |

### Unavailable reason

| Name | Value | Meaning |
|---|---:|---|
| `None` | `0` | The batch is not unavailable. |
| `NoGame` | `1` | No active RimWorld game exists. |
| `NoActiveMap` | `2` | No active map exists. |
| `PlannerStateUnavailable` | `3` | Required Planner components or an available product-inventory snapshot are unavailable. |

`UnavailableReason` is non-`None` only when batch status is `Unavailable`.

### Item status

| Name | Value | Meaning |
|---|---:|---|
| `Success` | `0` | The composition was evaluated; `Matches` may be empty. |
| `InvalidInput` | `1` | The request or its composition is structurally invalid. |
| `UnknownGeneDef` | `2` | At least one supplied def name cannot be resolved in the active definition set. |
| `Failed` | `3` | Resolution or evaluation of this item failed unexpectedly. |

An item-level failure does not block unrelated items in the same successful batch.

## Request rules

### Batch rules

* A `null` request list returns batch status `InvalidRequest`.
* An empty request list returns `Success` with an empty `Results` collection and does not require an active game or map.
* A non-empty batch requires an active game, an active map, Planner components and an available current product-inventory snapshot.
* A successful non-empty batch preserves request-to-result correspondence by index.

### Composition rules

A request is `InvalidInput` when:

* the request entry itself is `null`;
* `GeneDefNames` is empty;
* any entry is `null`, empty or whitespace.

Before definition resolution, duplicate def names are removed with `StringComparer.Ordinal`, preserving the first occurrence. The comparison is case-sensitive; differently cased strings are not treated as the same def name.

If any effective def name cannot be resolved through the active `GeneDef` database, the complete item returns `UnknownGeneDef`. The query does not evaluate a partial subset of the composition.

## Relevance semantics

Relevance is computed by Xenogerm Planner from current saved plans and the current Planner product-inventory snapshot.

Only plans whose current top-level readiness status is `NotReady` can match.

The following plans are excluded:

* `Ready`;
* `EmptyTarget`;
* `Degraded`;
* `Unavailable`.

### Coverage mode

An offered composition is relevant when it contains at least one target gene currently classified as `Missing`.

Additional offered genes are allowed.

### Exact payload mode

An offered composition is relevant only when both conditions hold:

1. every offered gene belongs to the plan target;
2. at least one offered target gene is currently classified as `Missing` or `ExactPayloadConflict`.

A composition containing any gene outside the target is not relevant to that Exact payload plan.

### Additional rules

* Prerequisite-only compositions are not relevant.
* A plan already ready through current product inventory is not returned.
* The offer is not added to product inventory and does not satisfy readiness.
* The query does not calculate or persist a hypothetical post-purchase state.
* The query does not reserve genepacks or alter physical genepacks, plan state or notification cursors.

Relevance uses product-level readiness. A selected assembler's metabolism, infrastructure blockers or `SearchIncomplete` result do not change API relevance.

## Ordering and determinism

Matches for one item are ordered by:

1. `DisplayName` using `StringComparer.OrdinalIgnoreCase`;
2. `PlanId` using `StringComparer.Ordinal` as the tie-breaker.

Equivalent effective input sets produce equivalent ordered public results. Batch item order always follows request order.

## Read-only lifecycle

The API reads:

* the current saved plan collection;
* the current active-map Planner product-inventory snapshot.

From these inputs, it computes plan readiness and target-gene diagnostics for the query.

A query does not mutate:

* plan IDs, names, targets or readiness modes;
* physical genepacks or their ownership;
* readiness-notification settings;
* notification delivery cursors;
* save data or consumer state.

Obtaining the current inventory snapshot can refresh Planner's derived cache when it is invalidated, the active map changes or its refresh interval expires. Read-only does not mean that the cached snapshot reference remains unchanged. The offered composition is never inserted into that inventory.

Invoke the query synchronously on the game's main thread. It reads live game components, map inventory and definitions; the runtime-free DTO surface does not make those operations independent of the game lifecycle.

The API does not publish change events. A consumer decides when to query again.

## Minimal soft-binding example

The following example has no compile-time reference to Xenogerm Planner. It demonstrates discovery, exact version checking, request construction and invocation through public reflection only.

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

internal sealed class XenogermPlannerApiV1Binding
{
    private const int SupportedApiVersion = 1;
    private const string FacadeTypeName =
        "XenogermPlanner.Api.XenogermPlannerApi, XenogermPlanner";
    private const string RequestTypeName =
        "XenogermPlanner.Api.GenepackRelevanceRequest, XenogermPlanner";

    private readonly Type _requestType;
    private readonly ConstructorInfo _requestConstructor;
    private readonly MethodInfo _queryMethod;

    private XenogermPlannerApiV1Binding(
        Type requestType,
        ConstructorInfo requestConstructor,
        MethodInfo queryMethod)
    {
        _requestType = requestType;
        _requestConstructor = requestConstructor;
        _queryMethod = queryMethod;
    }

    internal static bool TryCreate(out XenogermPlannerApiV1Binding binding)
    {
        binding = null;

        Type facadeType = Type.GetType(FacadeTypeName, throwOnError: false);
        if (facadeType == null)
            return false;

        PropertyInfo versionProperty = facadeType.GetProperty(
            "ApiVersion",
            BindingFlags.Public | BindingFlags.Static);

        if (versionProperty == null || versionProperty.PropertyType != typeof(int))
            return false;

        var apiVersion = (int)versionProperty.GetValue(null);
        if (apiVersion != SupportedApiVersion)
            return false;

        Type requestType = Type.GetType(RequestTypeName, throwOnError: false);
        if (requestType == null)
            return false;

        ConstructorInfo requestConstructor = requestType.GetConstructor(
            new[] { typeof(IEnumerable<string>) });

        MethodInfo queryMethod = facadeType.GetMethod(
            "QueryGenepackRelevance",
            BindingFlags.Public | BindingFlags.Static);

        if (requestConstructor == null || queryMethod == null)
            return false;

        binding = new XenogermPlannerApiV1Binding(
            requestType,
            requestConstructor,
            queryMethod);

        return true;
    }

    internal object Query(IReadOnlyList<IReadOnlyList<string>> compositions)
    {
        if (compositions == null)
            throw new ArgumentNullException(nameof(compositions));

        Type listType = typeof(List<>).MakeGenericType(_requestType);
        var requests = (IList)Activator.CreateInstance(listType);

        foreach (IReadOnlyList<string> composition in compositions)
        {
            object request = _requestConstructor.Invoke(
                new object[] { composition });

            requests.Add(request);
        }

        return _queryMethod.Invoke(null, new object[] { requests });
    }
}
```

The returned object is `GenepackRelevanceBatchResult`. Read only the documented public properties:

```text
Status
UnavailableReason
Results[*].Status
Results[*].Matches[*].PlanId
Results[*].Matches[*].DisplayName
```

`MethodInfo.Invoke` wraps exceptions thrown by the target in `TargetInvocationException`. Consumers should treat unexpected binding or invocation failures as an unavailable optional integration and preserve their core behavior.

## Consumer behavior

A consumer should:

* cache only the resolved version-specific binding, not Planner plan or inventory state;
* pass only runtime-free def-name compositions;
* preserve batch index correspondence;
* treat `Success` with an empty match list as a valid irrelevant composition;
* treat expected absence, unsupported version and `Unavailable` as neutral states;
* isolate item-level failures where practical;
* display `DisplayName` to the user while retaining `PlanId` as identity;
* decide explicitly when to requery rather than assuming push invalidation.

A consumer must not:

* access private Planner types or fields;
* infer or duplicate Planner readiness semantics;
* assume an unknown future API version is compatible;
* add trade offers to Planner product inventory;
* persist matches as authoritative Planner state;
* require Xenogerm Planner for unrelated core behavior.

## Compatibility expectations and non-goals

The API version `1` contract applies to the matching Xenogerm Planner DLL in the supported RimWorld 1.6/1.7 package, with Biotech and the standard genetics boundaries in [Compatibility](compatibility.md). Current source targets RimWorld 1.7 and `net10.0`; the package preserves the released 1.6 DLL. API versioning is independent from both the game version and target framework.

The API does not guarantee compatibility with third-party mods that replace or substantially patch Planner-owned or vanilla genetics behavior.

The API is not:

* a trading or purchase API;
* a plan mutation API;
* a readiness-state export for every plan;
* a reservation mechanism;
* an event or notification channel;
* a cross-map, caravan, Passing Ship or prerequisite-acquisition query;
* a native RimWorld genetics UI extension point.