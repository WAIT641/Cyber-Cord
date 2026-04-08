namespace Cyber_Cord.Api.Attributes;

/// <summary>
/// Marks a property as sortable for pagination queries.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class SortableAttribute : Attribute;