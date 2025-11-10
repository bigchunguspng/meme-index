using MemeIndex_Core.Controllers;

namespace MemeIndex_Core.Objects;

public record SearchQuery(int MeanId, List<string> Words, LogicalOperator Operator);