using Infrastructure.Configuration;
using Infrastructure.MultiTenancy;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Infrastructure.Configuration.Database;

/// <summary>
/// MongoDB document for configuration entries
/// </summary>
public class ConfigurationDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public object Value { get; set; } = default!;
    public ConfigurationScope Scope { get; set; }
    public string? ScopeIdentifier { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    public ConfigurationEntry ToConfigurationEntry()
    {
        return new ConfigurationEntry
        {
            Key = Key,
            Value = Value,
            Scope = Scope,
            ScopeIdentifier = ScopeIdentifier,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            CreatedBy = CreatedBy,
            UpdatedBy = UpdatedBy
        };
    }

    public static ConfigurationDocument FromConfigurationEntry(ConfigurationEntry entry, string? id = null)
    {
        return new ConfigurationDocument
        {
            Id = !string.IsNullOrEmpty(id) ? id : null,
            Key = entry.Key,
            Value = entry.Value,
            Scope = entry.Scope,
            ScopeIdentifier = entry.ScopeIdentifier,
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt,
            CreatedBy = entry.CreatedBy,
            UpdatedBy = entry.UpdatedBy
        };
    }
}

/// <summary>
/// MongoDB document for feature flags
/// </summary>
public class FeatureFlagDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    [BsonElement("Name")]
    public string Name { get; set; } = string.Empty;
    
    [BsonElement("Enabled")]
    public bool Enabled { get; set; }
    
    [BsonElement("Scope")]
    public ConfigurationScope Scope { get; set; }
    
    [BsonElement("ScopeIdentifier")]
    public string? ScopeIdentifier { get; set; }
    
    [BsonElement("RolloutPercentage")]
    public int RolloutPercentage { get; set; } = 100;
    
    [BsonElement("Variant")]
    public string? Variant { get; set; }
    
    [BsonElement("Rules")]
    public List<FeatureFlagRuleDocument> Rules { get; set; } = new();
    
    [BsonElement("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [BsonElement("UpdatedAt")]
    public DateTime? UpdatedAt { get; set; }
    
    [BsonElement("CreatedBy")]
    public string? CreatedBy { get; set; }
    
    [BsonElement("UpdatedBy")]
    public string? UpdatedBy { get; set; }

    public FeatureFlag ToFeatureFlag()
    {
        return new FeatureFlag
        {
            Name = Name,
            Enabled = Enabled,
            Scope = Scope,
            ScopeIdentifier = ScopeIdentifier,
            RolloutPercentage = RolloutPercentage,
            Variant = Variant,
            Rules = Rules.Select(r => r.ToFeatureFlagRule()).ToList(),
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            CreatedBy = CreatedBy,
            UpdatedBy = UpdatedBy
        };
    }

    public static FeatureFlagDocument FromFeatureFlag(FeatureFlag flag, string? id = null)
    {
        return new FeatureFlagDocument
        {
            Id = !string.IsNullOrEmpty(id) ? id : null,
            Name = flag.Name,
            Enabled = flag.Enabled,
            Scope = flag.Scope,
            ScopeIdentifier = flag.ScopeIdentifier,
            RolloutPercentage = flag.RolloutPercentage,
            Variant = flag.Variant,
            Rules = flag.Rules.Select(FeatureFlagRuleDocument.FromFeatureFlagRule).ToList(),
            CreatedAt = flag.CreatedAt,
            UpdatedAt = flag.UpdatedAt,
            CreatedBy = flag.CreatedBy,
            UpdatedBy = flag.UpdatedBy
        };
    }
}

/// <summary>
/// MongoDB document for feature flag rules
/// </summary>
public class FeatureFlagRuleDocument
{
    public string Attribute { get; set; } = string.Empty;
    public string Operator { get; set; } = "equals";
    public string Value { get; set; } = string.Empty;

    public FeatureFlagRule ToFeatureFlagRule()
    {
        return new FeatureFlagRule
        {
            Attribute = Attribute,
            Operator = Operator,
            Value = Value
        };
    }

    public static FeatureFlagRuleDocument FromFeatureFlagRule(FeatureFlagRule rule)
    {
        return new FeatureFlagRuleDocument
        {
            Attribute = rule.Attribute,
            Operator = rule.Operator,
            Value = rule.Value
        };
    }
}

/// <summary>
/// MongoDB document for tenants
/// </summary>
public class TenantDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    
    [BsonElement("TenantIdentifier")]
    public string TenantIdentifier { get; set; } = string.Empty;
    
    [BsonElement("Name")]
    public string Name { get; set; } = string.Empty;
    
    [BsonElement("Status")]
    public TenantStatus Status { get; set; } = TenantStatus.Pending;
    
    [BsonElement("Comments")]
    public string? Comments { get; set; }
    
    [BsonElement("SalesTerms")]
    public string? SalesTerms { get; set; }
    
    [BsonElement("ContactEmail")]
    public string? ContactEmail { get; set; }
    
    [BsonElement("ContactPhone")]
    public string? ContactPhone { get; set; }
    
    [BsonElement("ContactName")]
    public string? ContactName { get; set; }
    
    [BsonElement("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [BsonElement("UpdatedAt")]
    public DateTime? UpdatedAt { get; set; }
    
    [BsonElement("CreatedBy")]
    public string? CreatedBy { get; set; }
    
    [BsonElement("UpdatedBy")]
    public string? UpdatedBy { get; set; }

    public Tenant ToTenant()
    {
        return new Tenant
        {
            TenantIdentifier = TenantIdentifier,
            Name = Name,
            Status = Status,
            Comments = Comments,
            SalesTerms = SalesTerms,
            ContactEmail = ContactEmail,
            ContactPhone = ContactPhone,
            ContactName = ContactName,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            CreatedBy = CreatedBy,
            UpdatedBy = UpdatedBy
        };
    }

    public static TenantDocument FromTenant(Tenant tenant, string? id = null)
    {
        return new TenantDocument
        {
            Id = !string.IsNullOrEmpty(id) ? id : null,
            TenantIdentifier = tenant.TenantIdentifier,
            Name = tenant.Name,
            Status = tenant.Status,
            Comments = tenant.Comments,
            SalesTerms = tenant.SalesTerms,
            ContactEmail = tenant.ContactEmail,
            ContactPhone = tenant.ContactPhone,
            ContactName = tenant.ContactName,
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt,
            CreatedBy = tenant.CreatedBy,
            UpdatedBy = tenant.UpdatedBy
        };
    }
}

