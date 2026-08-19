# Xenogerm Planner v1.1.1

## Changed

- Improved plan readiness analysis performance, especially for large plans and large heterogeneous genepack inventories.
- Improved genepack relevance queries for both single-offer and repeated batch workloads.
- Improved Gene Coverage table projection performance for inventories with many referenced genepack compositions while preserving the fast path for genes with no available source compositions.
- Reduced managed allocations across readiness analysis, genepack relevance analysis, Gene Assembler candidate search, and effective gene biostat calculations.
- Preserved existing planning, readiness, save-data, trader advisory, and public integration API behavior while applying the performance improvements.