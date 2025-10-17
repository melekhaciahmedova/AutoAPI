namespace AutoAPI.Domain.Models
{
    public class ClassDefinition
    {
        public string ClassName { get; set; }
        public List<PropertyDefinition> Properties { get; set; }
    }

    public class PropertyDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsNullable { get; set; }
        public bool IsRequired { get; set; }
        public bool IsKey { get; set; }
        public int? MaxLength { get; set; }
        public string? ColumnType { get; set; }
    }
}
